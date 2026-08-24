using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    /// <summary>직렬화 Tile 목록과 좌표 lookup 및 시각 상태 변경 알림을 관리합니다.</summary>
    public sealed class HexTileStore : IHexTileStore
    {
        /// <summary>직렬화된 사용자 Tile 상태 목록입니다.</summary>
        private readonly List<HexTile> _tiles;
        /// <summary>Axial 좌표 기반 빠른 Tile lookup입니다.</summary>
        private readonly Dictionary<AxialCoordinates, HexTile> _lookup = new();

        /// <summary>타일 색상 또는 활성 상태로 Backend 시각 데이터가 무효화될 때 발생합니다.</summary>
        public event Action TileVisualsChanged;

        /// <summary>Controller가 소유한 직렬화 목록을 복사하지 않고 저장소를 생성합니다.</summary>
        public HexTileStore(List<HexTile> tiles)
        {
            _tiles = tiles;
        }

        /// <summary>현재 타일 목록을 읽기 전용으로 가져옵니다.</summary>
        public IReadOnlyList<HexTile> Tiles => _tiles;

        /// <summary>Cube 좌표와 같은 q, r을 가진 타일을 O(1) 평균 시간에 찾습니다.</summary>
        public bool TryGetTile(in HexCoordinates coordinates, out HexTile tile)
            => _lookup.TryGetValue(coordinates, out tile);

        /// <summary>좌표가 존재하면 활성 상태를 변경합니다.</summary>
        public void SetActive(in AxialCoordinates coordinates, bool isActive)
        {
            if (!_lookup.TryGetValue(coordinates, out HexTile hex)) return;

            hex.IsActive = isActive;
        }

        /// <summary>Surface Grid snapshot으로 Tile 목록을 재구축하면서 기존 사용자 상태를 보존합니다.</summary>
        public void Bake(SurfaceTopology topology, SurfaceGrid grid, Transform surfaceTransform)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (surfaceTransform == null) throw new ArgumentNullException(nameof(surfaceTransform));
            Bake(new SingleSurfaceProvider(topology), new FixedSurfaceTransform(topology.Handle, surfaceTransform), grid);
        }

        /// <summary>
        /// 여러 Surface에 걸친 Grid로 Tile 목록을 재구축합니다. Tile 중심은 Region vertex마다 자기
        /// Surface의 topology와 변환으로 월드 위치를 구한 뒤 평균한 값입니다.
        /// </summary>
        public void Bake(ISurfaceProvider surfaces, ISurfaceTransformSource transforms, SurfaceGrid grid)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (transforms == null) throw new ArgumentNullException(nameof(transforms));
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            RebuildLookup();
            Dictionary<AxialCoordinates, HexTile> previousTiles = new(_lookup);
            UnsubscribeFromTiles();
            _tiles.Clear();
            _lookup.Clear();

            foreach (SurfaceGridTileRegion tileRegion in grid.Tiles)
            {
                // Logical 중심이 여러 Face에 걸칠 수 있으므로 Region vertex의 월드 위치 평균을 표시용
                // TilePosition으로 사용합니다. Picking과 identity는 이 근사 위치에 의존하지 않습니다.
                Vector3 worldCenter = Vector3.zero;
                foreach (SurfaceRegionVertex vertex in tileRegion.Region.Vertices)
                {
                    SurfaceHandle surface = vertex.SurfacePoint.Surface;
                    if (!surfaces.TryGetTopology(surface, out SurfaceTopology vertexTopology))
                        throw new ArgumentException($"Surface {surface} is not available from the provider.", nameof(surfaces));
                    if (!transforms.TryGetSurfaceToWorld(surface, out Matrix4x4 surfaceToWorld))
                        throw new ArgumentException($"Surface {surface} has no world transform.", nameof(transforms));
                    worldCenter += surfaceToWorld.MultiplyPoint3x4(vertexTopology.Evaluate(vertex.SurfacePoint));
                }
                worldCenter /= tileRegion.Region.Vertices.Count;
                HexCoordinates coordinates = tileRegion.Coordinates;
                AxialCoordinates key = coordinates;
                // Bake 순서 인덱스는 생성 Geometry의 Tile index와 같은 순서를 유지해야 합니다.
                // Grid의 Tile Region 배열 순서를 그대로 따르므로 두 첨자가 항상 일치합니다.
                HexTile tile = new(
                    coordinates.Q,
                    coordinates.R,
                    worldCenter,
                    tileRegion.IntrinsicCenter,
                    _tiles.Count);

                if (previousTiles.TryGetValue(key, out HexTile previousTile)) tile.CopyStateFrom(previousTile);
                SubscribeToTile(tile);
                _tiles.Add(tile);
                _lookup.Add(key, tile);
            }

            TileVisualsChanged?.Invoke();
        }

        /// <summary>단일 Surface Transform을 변환 조회 계약으로 감쌉니다.</summary>
        private sealed class FixedSurfaceTransform : ISurfaceTransformSource
        {
            private readonly SurfaceHandle _surface;
            private readonly Transform _transform;

            public FixedSurfaceTransform(SurfaceHandle surface, Transform transform)
            {
                _surface = surface;
                _transform = transform;
            }

            public bool TryGetSurfaceToWorld(SurfaceHandle surface, out Matrix4x4 surfaceToWorld)
            {
                surfaceToWorld = Matrix4x4.identity;
                if (surface != _surface || _transform == null) return false;
                surfaceToWorld = _transform.localToWorldMatrix;
                return true;
            }
        }

        /// <summary>모든 타일과 lookup을 비우고 시각 상태 변경을 알립니다.</summary>
        public void Clear()
        {
            UnsubscribeFromTiles();
            _tiles.Clear();
            _lookup.Clear();
            TileVisualsChanged?.Invoke();
        }

        /// <summary>역직렬화된 목록에서 lookup과 타일별 변경 이벤트 구독을 복원합니다.</summary>
        public void RebuildLookup()
        {
            _lookup.Clear();

            foreach (HexTile tile in _tiles)
            {
                if (tile is null) continue;
                _lookup[tile.Coordinates] = tile;
                SubscribeToTile(tile);
            }
        }

        /// <summary>중복 구독 없이 한 타일의 시각 변경 이벤트를 연결합니다.</summary>
        private void SubscribeToTile(HexTile tile)
        {
            tile.OnChangedActive -= HandleTileActiveChanged;
            tile.OnChangedColor -= HandleTileColorChanged;
            tile.OnChangedActive += HandleTileActiveChanged;
            tile.OnChangedColor += HandleTileColorChanged;
        }

        /// <summary>현재 목록의 모든 타일에서 저장소 이벤트를 분리합니다.</summary>
        private void UnsubscribeFromTiles()
        {
            foreach (HexTile tile in _tiles)
            {
                if (tile is null) continue;

                tile.OnChangedActive -= HandleTileActiveChanged;
                tile.OnChangedColor -= HandleTileColorChanged;
            }
        }

        private void HandleTileActiveChanged(IHexTile _, bool __) => TileVisualsChanged?.Invoke();

        private void HandleTileColorChanged(IHexTile _, Color __) => TileVisualsChanged?.Invoke();
    }
}
