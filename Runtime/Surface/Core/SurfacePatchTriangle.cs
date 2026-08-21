using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>원본 Triangle 하나와 corner 세 개의 local intrinsic 2D 위치를 저장합니다.</summary>
    public readonly struct SurfacePatchTriangle
    {
        /// <summary>원본 topology의 Triangle index를 가져옵니다.</summary>
        public int TriangleIndex { get; }
        /// <summary>원본 Triangle corner A에 대응하는 2D 위치를 가져옵니다.</summary>
        public Vector2 A { get; }
        /// <summary>원본 Triangle corner B에 대응하는 2D 위치를 가져옵니다.</summary>
        public Vector2 B { get; }
        /// <summary>원본 Triangle corner C에 대응하는 2D 위치를 가져옵니다.</summary>
        public Vector2 C { get; }

        /// <summary>corner 순서가 원본 winding과 일치하는 Face별 intrinsic embedding을 생성합니다.</summary>
        public SurfacePatchTriangle(int triangleIndex, in Vector2 a, in Vector2 b, in Vector2 c)
        {
            TriangleIndex = triangleIndex;
            A = a;
            B = b;
            C = c;
        }

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
