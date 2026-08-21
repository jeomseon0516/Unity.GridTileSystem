using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 각 방향성 Edge 반대편의 인접 Triangle을 저장합니다. -1은 Surface 경계 Edge를 뜻합니다.
    /// </summary>
    public readonly struct SurfaceTriangleAdjacency : IEquatable<SurfaceTriangleAdjacency>
    {
        /// <summary>Triangle Edge A→B 반대편의 이웃을 가져옵니다.</summary>
        public int Edge0 { get; }
        /// <summary>Triangle Edge B→C 반대편의 이웃을 가져옵니다.</summary>
        public int Edge1 { get; }
        /// <summary>Triangle Edge C→A 반대편의 이웃을 가져옵니다.</summary>
        public int Edge2 { get; }

        /// <summary>Triangle index 또는 경계를 뜻하는 -1로 인접 정보 레코드를 생성합니다.</summary>
        public SurfaceTriangleAdjacency(int edge0, int edge1, int edge2)
        {
            Edge0 = edge0;
            Edge1 = edge1;
            Edge2 = edge2;
        }

        /// <summary>[0,2] 범위의 Edge 반대편에 있는 인접 Triangle을 가져옵니다.</summary>
        public int GetNeighbor(int edge) => edge switch
        {
            0 => Edge0,
            1 => Edge1,
            2 => Edge2,
            _ => throw new ArgumentOutOfRangeException(nameof(edge))
        };

        /// <summary>지정한 Edge를 주어진 Triangle과 연결한 복사본을 반환합니다.</summary>
        internal SurfaceTriangleAdjacency WithNeighbor(int edge, int triangleIndex) => edge switch
        {
            0 => new SurfaceTriangleAdjacency(triangleIndex, Edge1, Edge2),
            1 => new SurfaceTriangleAdjacency(Edge0, triangleIndex, Edge2),
            2 => new SurfaceTriangleAdjacency(Edge0, Edge1, triangleIndex),
            _ => throw new ArgumentOutOfRangeException(nameof(edge))
        };

        /// <inheritdoc />
        public bool Equals(SurfaceTriangleAdjacency other) =>
            Edge0 == other.Edge0 && Edge1 == other.Edge1 && Edge2 == other.Edge2;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SurfaceTriangleAdjacency other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Edge0, Edge1, Edge2);
    }
}
