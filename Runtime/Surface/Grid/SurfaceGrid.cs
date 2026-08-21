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

        /// <summary>Grid를 생성한 local Surface Patch를 가져옵니다.</summary>
        public SurfacePatch Patch { get; }
        /// <summary>Tile 중심·polygon 및 역좌표 변환에 사용한 intrinsic Hex layout을 가져옵니다.</summary>
        public IntrinsicHexLayout Layout { get; }
        /// <summary>실제 Surface와 겹치는 Logical Tile Region을 가져옵니다.</summary>
        public IReadOnlyList<SurfaceGridTileRegion> Tiles => _tilesView;

        /// <summary>Patch와 해당 Patch에 정합된 Tile Region 배열로 Grid snapshot을 생성합니다.</summary>
        internal SurfaceGrid(
            SurfacePatch patch,
            in IntrinsicHexLayout layout,
            SurfaceGridTileRegion[] tiles)
        {
            Patch = patch ?? throw new ArgumentNullException(nameof(patch));
            Layout = layout;
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _tilesView = Array.AsReadOnly(_tiles);
            _lookup = new Dictionary<HexCoordinates, SurfaceGridTileRegion>(_tiles.Length);
            foreach (SurfaceGridTileRegion tile in _tiles) _lookup.Add(tile.Coordinates, tile);
        }

        /// <summary>Logical Hex 좌표와 일치하는 Surface Tile Region을 찾습니다.</summary>
        public bool TryGetTile(in HexCoordinates coordinates, out SurfaceGridTileRegion tile) =>
            _lookup.TryGetValue(coordinates, out tile);
    }
}
