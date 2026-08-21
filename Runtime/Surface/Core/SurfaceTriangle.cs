using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>vertex index 세 개를 방향성 winding 순서로 저장합니다.</summary>
    public readonly struct SurfaceTriangle : IEquatable<SurfaceTriangle>
    {
        /// <summary>첫 번째 방향성 vertex index를 가져옵니다.</summary>
        public int A { get; }
        /// <summary>두 번째 방향성 vertex index를 가져옵니다.</summary>
        public int B { get; }
        /// <summary>세 번째 방향성 vertex index를 가져옵니다.</summary>
        public int C { get; }

        /// <summary>원본 vertex index 세 개로 방향성 Triangle을 생성합니다.</summary>
        public SurfaceTriangle(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        /// <summary>[0,2] 범위의 방향성 corner에 있는 원본 vertex index를 가져옵니다.</summary>
        public int GetVertex(int corner) => corner switch
        {
            0 => A,
            1 => B,
            2 => C,
            _ => throw new ArgumentOutOfRangeException(nameof(corner))
        };

        /// <summary>
        /// 방향성 Edge를 가져옵니다. Edge 0은 A→B, 1은 B→C, 2는 C→A입니다.
        /// winding이 일관된 인접 Triangle은 공유 Edge를 서로 반대 방향으로 순회해야 합니다.
        /// </summary>
        public void GetEdge(int edge, out int start, out int end)
        {
            (start, end) = edge switch
            {
                0 => (A, B),
                1 => (B, C),
                2 => (C, A),
                _ => throw new ArgumentOutOfRangeException(nameof(edge))
            };
        }

        /// <inheritdoc />
        public bool Equals(SurfaceTriangle other) => A == other.A && B == other.B && C == other.C;
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SurfaceTriangle other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(A, B, C);
    }
}
