using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>Physics hit의 Triangle/barycentric identity를 intrinsic Tile 좌표로 변환합니다.</summary>
    public sealed class SurfaceGridPicker
    {
        /// <summary>Picking이 허용되는 원본 Surface Collider입니다.</summary>
        private readonly Collider _surfaceCollider;
        /// <summary>Raycast Triangle identity가 속하는 topology snapshot입니다.</summary>
        private readonly SurfaceTopology _topology;
        /// <summary>Patch mapper와 Logical Tile lookup을 제공하는 Grid snapshot입니다.</summary>
        private readonly SurfaceGrid _grid;

        /// <summary>원본 Collider, topology와 Grid snapshot으로 picker를 생성합니다.</summary>
        public SurfaceGridPicker(Collider surfaceCollider, SurfaceTopology topology, SurfaceGrid grid)
        {
            _surfaceCollider = surfaceCollider != null
                ? surfaceCollider
                : throw new ArgumentNullException(nameof(surfaceCollider));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            if (_grid.Patch.Surface != _topology.Handle)
                throw new ArgumentException("Grid belongs to another surface topology.", nameof(grid));
        }

        /// <summary>Ray가 원본 Surface의 활성 Tile과 교차하면 해당 hit와 Tile Region을 반환합니다.</summary>
        public bool TryPick(
            in Ray ray,
            LayerMask layerMask,
            out RaycastHit hit,
            out SurfaceGridTileRegion tile)
        {
            hit = default;
            tile = null;
            int surfaceLayerBit = 1 << _surfaceCollider.gameObject.layer;
            if ((layerMask.value & surfaceLayerBit) == 0 ||
                !_surfaceCollider.Raycast(ray, out hit, Mathf.Infinity) || hit.triangleIndex < 0)
                return false;

            // MeshCollider hit는 원본 Triangle index와 barycentricCoordinate를 직접 제공합니다.
            // 따라서 world position을 평면에 재투영하지 않고 정확한 Surface identity를 복원할 수 있습니다.
            SurfacePoint point = new(_topology.Handle, hit.triangleIndex, hit.barycentricCoordinate);
            if (!SurfacePatchMapper.TryGetIntrinsicPosition(_grid.Patch, point, out Vector2 intrinsic)) return false;
            HexCoordinates coordinates = _grid.Layout.GetCoordinates(intrinsic);
            return _grid.TryGetTile(coordinates, out tile);
        }
    }
}
