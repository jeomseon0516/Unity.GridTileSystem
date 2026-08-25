using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>하나의 local Surface Patch에 생성된 Logical Hex Grid의 불변 snapshot입니다.</summary>
    public sealed class SurfaceGrid
    {
        /// <summary>좌표 lookup과 렌더링에서 함께 사용하는 Tile Region 배열입니다.</summary>
        private readonly SurfaceGridTileRegion[] _tiles;
        /// <summary>Axial 좌표에서 Tile Region으로 접근하는 immutable snapshot lookup입니다.</summary>
        private readonly Dictionary<HexCoordinates, SurfaceGridTileRegion> _lookup;
        /// <summary>내부 Tile 배열의 외부 변경을 차단하는 view입니다.</summary>
        private readonly IReadOnlyList<SurfaceGridTileRegion> _tilesView;
        private readonly IReadOnlyList<SurfacePatch> _patches;
        private readonly IReadOnlyList<SurfaceHandle> _surfaces;

        /// <summary>Grid를 생성한 local Surface Patch를 가져옵니다.</summary>
        public SurfacePatch Patch { get; }
        /// <summary>공통 intrinsic 좌표계에서 Grid가 소비한 모든 local Patch를 가져옵니다.</summary>
        public IReadOnlyList<SurfacePatch> Patches => _patches;
        /// <summary>전체 Patch가 참조하는 중복 없는 Surface identity를 가져옵니다.</summary>
        public IReadOnlyList<SurfaceHandle> Surfaces => _surfaces;
        /// <summary>전체 Patch 집합이 둘 이상의 Surface에 걸쳐 있는지 가져옵니다.</summary>
        public bool SpansMultipleSurfaces => _surfaces.Count > 1;
        /// <summary>Tile 중심·polygon 및 역좌표 변환에 사용한 intrinsic Hex layout을 가져옵니다.</summary>
        public IntrinsicHexLayout Layout { get; }
        /// <summary>실제 Surface와 겹치는 Logical Tile Region을 가져옵니다.</summary>
        public IReadOnlyList<SurfaceGridTileRegion> Tiles => _tilesView;
        /// <summary>Patch AABB에서 열거한 보수적 Hex 후보 개수를 가져옵니다.</summary>
        public int CandidateTileCount { get; }
        /// <summary>볼록 껍질 빠른 탈락 뒤 실제 Region clipping을 수행한 후보 개수를 가져옵니다.</summary>
        public int RegionBuildCount { get; }

        /// <summary>Patch와 해당 Patch에 정합된 Tile Region 배열로 Grid snapshot을 생성합니다.</summary>
        internal SurfaceGrid(
            SurfacePatch patch,
            in IntrinsicHexLayout layout,
            SurfaceGridTileRegion[] tiles,
            int candidateTileCount,
            int regionBuildCount)
        {
            Patch = patch ?? throw new ArgumentNullException(nameof(patch));
            _patches = Array.AsReadOnly(new[] { Patch });
            _surfaces = CollectSurfaces(_patches);
            Layout = layout;
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _tilesView = Array.AsReadOnly(_tiles);
            CandidateTileCount = candidateTileCount;
            RegionBuildCount = regionBuildCount;
            _lookup = new Dictionary<HexCoordinates, SurfaceGridTileRegion>(_tiles.Length);
            foreach (SurfaceGridTileRegion tile in _tiles) _lookup.Add(tile.Coordinates, tile);
        }

        /// <summary>공통 좌표계로 정렬된 Patch 집합과 Tile로 Grid snapshot을 생성합니다.</summary>
        internal SurfaceGrid(
            SurfacePatchSet patchSet,
            in IntrinsicHexLayout layout,
            SurfaceGridTileRegion[] tiles,
            int candidateTileCount,
            int regionBuildCount)
        {
            if (patchSet == null) throw new ArgumentNullException(nameof(patchSet));
            Patch = patchSet.PrimaryPatch;
            _patches = patchSet.Patches;
            _surfaces = CollectSurfaces(_patches);
            Layout = layout;
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _tilesView = Array.AsReadOnly(_tiles);
            CandidateTileCount = candidateTileCount;
            RegionBuildCount = regionBuildCount;
            _lookup = new Dictionary<HexCoordinates, SurfaceGridTileRegion>(_tiles.Length);
            foreach (SurfaceGridTileRegion tile in _tiles) _lookup.Add(tile.Coordinates, tile);
        }

        /// <summary>원본 Grid의 Patch/layout identity를 유지하며 Tile 부분집합 snapshot을 생성합니다.</summary>
        internal SurfaceGrid(
            SurfaceGrid source,
            SurfaceGridTileRegion[] tiles,
            int candidateTileCount,
            int regionBuildCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Patch = source.Patch;
            _patches = source.Patches;
            _surfaces = source.Surfaces;
            Layout = source.Layout;
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _tilesView = Array.AsReadOnly(_tiles);
            CandidateTileCount = candidateTileCount;
            RegionBuildCount = regionBuildCount;
            _lookup = new Dictionary<HexCoordinates, SurfaceGridTileRegion>(_tiles.Length);
            foreach (SurfaceGridTileRegion tile in _tiles) _lookup.Add(tile.Coordinates, tile);
        }

        /// <summary>Logical Hex 좌표와 일치하는 Surface Tile Region을 찾습니다.</summary>
        public bool TryGetTile(in HexCoordinates coordinates, out SurfaceGridTileRegion tile) =>
            _lookup.TryGetValue(coordinates, out tile);

        private static IReadOnlyList<SurfaceHandle> CollectSurfaces(IReadOnlyList<SurfacePatch> patches)
        {
            List<SurfaceHandle> surfaces = new();
            HashSet<SurfaceHandle> unique = new();
            foreach (SurfacePatch patch in patches)
            {
                foreach (SurfacePatchTriangle triangle in patch.Triangles)
                {
                    if (unique.Add(triangle.Surface)) surfaces.Add(triangle.Surface);
                }
            }
            return surfaces.AsReadOnly();
        }
    }
}
