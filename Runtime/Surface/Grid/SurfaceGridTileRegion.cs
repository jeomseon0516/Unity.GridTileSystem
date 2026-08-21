using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>Logical Hex identity와 해당 Hex가 차지하는 Surface Geometry 영역을 연결합니다.</summary>
    public sealed class SurfaceGridTileRegion
    {
        /// <summary>Logical Tile의 Axial/Cube 좌표를 가져옵니다.</summary>
        public HexCoordinates Coordinates { get; }
        /// <summary>local Surface Patch에 있는 Logical Tile 중심을 가져옵니다.</summary>
        public Vector2 IntrinsicCenter { get; }
        /// <summary>여러 원본 Triangle 조각으로 구성될 수 있는 Surface Region을 가져옵니다.</summary>
        public SurfaceRegion Region { get; }

        /// <summary>Logical Hex 좌표, 중심과 정합된 Surface Region을 결합합니다.</summary>
        public SurfaceGridTileRegion(
            in HexCoordinates coordinates,
            in Vector2 intrinsicCenter,
            SurfaceRegion region)
        {
            Coordinates = coordinates;
            IntrinsicCenter = intrinsicCenter;
            Region = region ?? throw new ArgumentNullException(nameof(region));
        }
    }
}
