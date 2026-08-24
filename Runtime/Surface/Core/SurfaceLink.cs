using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 한 Surface의 boundary Edge와 다른 Surface의 boundary Edge가 실제로 이어져 있다는 사실입니다.
    /// vertex 대응이 아니라 Edge 파라미터 대응으로 표현하므로 두 Surface의 해상도가 달라도 됩니다.
    /// </summary>
    public readonly struct SurfaceLink : IEquatable<SurfaceLink>
    {
        /// <summary>연결이 시작되는 Surface입니다.</summary>
        public SurfaceHandle FromSurface { get; }
        /// <summary>연결이 시작되는 Triangle index입니다.</summary>
        public int FromTriangleIndex { get; }
        /// <summary>연결이 시작되는 Edge입니다. 0은 A→B, 1은 B→C, 2는 C→A입니다.</summary>
        public int FromEdge { get; }
        /// <summary>연결이 도착하는 Surface입니다.</summary>
        public SurfaceHandle ToSurface { get; }
        /// <summary>연결이 도착하는 Triangle index입니다.</summary>
        public int ToTriangleIndex { get; }
        /// <summary>연결이 도착하는 Edge입니다.</summary>
        public int ToEdge { get; }
        /// <summary>
        /// From Edge의 파라미터 t를 To Edge의 파라미터로 옮길 때 방향이 뒤집히는지 나타냅니다.
        /// <see langword="false"/>면 From의 시작점이 To의 시작점에, <see langword="true"/>면 To의 끝점에 대응합니다.
        /// </summary>
        public bool ReverseParameter { get; }

        /// <summary>이 link가 실제 연결을 가리키는지 가져옵니다.</summary>
        public bool IsValid => FromSurface.IsValid && ToSurface.IsValid &&
                               FromTriangleIndex >= 0 && ToTriangleIndex >= 0 &&
                               (uint)FromEdge < 3u && (uint)ToEdge < 3u;

        /// <summary>두 boundary Edge 사이의 연결을 생성합니다.</summary>
        public SurfaceLink(
            SurfaceHandle fromSurface,
            int fromTriangleIndex,
            int fromEdge,
            SurfaceHandle toSurface,
            int toTriangleIndex,
            int toEdge,
            bool reverseParameter)
        {
            if ((uint)fromEdge >= 3u) throw new ArgumentOutOfRangeException(nameof(fromEdge));
            if ((uint)toEdge >= 3u) throw new ArgumentOutOfRangeException(nameof(toEdge));
            FromSurface = fromSurface;
            FromTriangleIndex = fromTriangleIndex;
            FromEdge = fromEdge;
            ToSurface = toSurface;
            ToTriangleIndex = toTriangleIndex;
            ToEdge = toEdge;
            ReverseParameter = reverseParameter;
        }

        /// <summary>같은 연결을 반대 방향에서 본 link를 만듭니다.</summary>
        public SurfaceLink Reversed() => new(
            ToSurface, ToTriangleIndex, ToEdge,
            FromSurface, FromTriangleIndex, FromEdge,
            ReverseParameter);

        /// <inheritdoc />
        public bool Equals(SurfaceLink other) =>
            FromSurface == other.FromSurface && FromTriangleIndex == other.FromTriangleIndex &&
            FromEdge == other.FromEdge && ToSurface == other.ToSurface &&
            ToTriangleIndex == other.ToTriangleIndex && ToEdge == other.ToEdge &&
            ReverseParameter == other.ReverseParameter;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SurfaceLink other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(
            FromSurface, FromTriangleIndex, FromEdge, ToSurface, ToTriangleIndex, ToEdge, ReverseParameter);

        /// <inheritdoc />
        public override string ToString() =>
            $"{FromSurface}#{FromTriangleIndex}e{FromEdge} -> {ToSurface}#{ToTriangleIndex}e{ToEdge}" +
            (ReverseParameter ? " (reversed)" : string.Empty);
    }
}
