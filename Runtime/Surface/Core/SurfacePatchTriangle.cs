using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 원본 Triangle 하나와 corner 세 개의 local intrinsic 2D 위치를 저장합니다.
    /// chart가 여러 Surface에 걸칠 수 있으므로 각 Face는 자신이 속한 Surface를 함께 보존합니다.
    /// </summary>
    public readonly struct SurfacePatchTriangle
    {
        /// <summary>이 Face가 속한 원본 Surface를 가져옵니다.</summary>
        public SurfaceHandle Surface { get; }
        /// <summary>원본 topology의 Triangle index를 가져옵니다.</summary>
        public int TriangleIndex { get; }
        /// <summary>원본 Triangle corner A에 대응하는 2D 위치를 가져옵니다.</summary>
        public Vector2 A { get; }
        /// <summary>원본 Triangle corner B에 대응하는 2D 위치를 가져옵니다.</summary>
        public Vector2 B { get; }
        /// <summary>원본 Triangle corner C에 대응하는 2D 위치를 가져옵니다.</summary>
        public Vector2 C { get; }
        /// <summary>
        /// Seed에서 이 Face 중심까지 adjacency graph를 따라 누적한 intrinsic 거리 상한을 가져옵니다.
        /// 직선 chart 거리와 달리 Surface를 가로지르는 실제 순회 경로 길이를 반영합니다.
        /// </summary>
        public float GraphGeodesicDistance { get; }

        /// <summary>corner 순서가 원본 winding과 일치하는 Face별 intrinsic embedding을 생성합니다.</summary>
        public SurfacePatchTriangle(
            SurfaceHandle surface,
            int triangleIndex,
            in Vector2 a,
            in Vector2 b,
            in Vector2 c,
            float graphGeodesicDistance = 0f)
        {
            Surface = surface;
            TriangleIndex = triangleIndex;
            A = a;
            B = b;
            C = c;
            GraphGeodesicDistance = graphGeodesicDistance;
        }

        /// <summary>corner 배치를 유지하고 graph geodesic 거리만 지정한 복사본을 만듭니다.</summary>
        public SurfacePatchTriangle WithGraphGeodesicDistance(float distance) =>
            new(Surface, TriangleIndex, A, B, C, distance);

        /// <summary>이 Face가 지정한 Surface의 지정한 Triangle인지 검사합니다.</summary>
        public bool Matches(SurfaceHandle surface, int triangleIndex) =>
            Surface == surface && TriangleIndex == triangleIndex;

        /// <summary>원본 Triangle corner 0, 1, 2 중 하나의 intrinsic 좌표를 가져옵니다.</summary>
        public Vector2 GetCorner(int corner) => corner switch
        {
            0 => A,
            1 => B,
            2 => C,
            _ => throw new System.ArgumentOutOfRangeException(nameof(corner))
        };
    }
}
