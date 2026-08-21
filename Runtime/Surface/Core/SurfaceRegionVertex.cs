using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>Surface Region의 intrinsic 2D 위치와 원본 Surface binding을 함께 저장합니다.</summary>
    public readonly struct SurfaceRegionVertex
    {
        /// <summary>local Surface Patch 안의 intrinsic 2D 위치를 가져옵니다.</summary>
        public Vector2 IntrinsicPosition { get; }
        /// <summary>원본 Surface Triangle에 대한 barycentric binding을 가져옵니다.</summary>
        public SurfacePoint SurfacePoint { get; }

        /// <summary>intrinsic 위치와 원본 Surface identity를 결합한 Region vertex를 생성합니다.</summary>
        public SurfaceRegionVertex(in Vector2 intrinsicPosition, in SurfacePoint surfacePoint)
        {
            IntrinsicPosition = intrinsicPosition;
            SurfacePoint = surfacePoint;
        }
    }
}
