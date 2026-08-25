using System;
using System.Collections.Generic;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>Axial 좌표를 고정 크기 영역으로 묶는 Geometry 재생성 단위입니다.</summary>
    public readonly struct SurfaceGridChunk : IEquatable<SurfaceGridChunk>
    {
        /// <summary>q 축 Chunk 좌표를 가져옵니다.</summary>
        public int Q { get; }
        /// <summary>r 축 Chunk 좌표를 가져옵니다.</summary>
        public int R { get; }

        /// <summary>Chunk 좌표를 생성합니다.</summary>
        public SurfaceGridChunk(int q, int r) => (Q, R) = (q, r);

        /// <summary>Tile 좌표를 음수에서도 대칭적인 floor division으로 Chunk에 매핑합니다.</summary>
        public static SurfaceGridChunk FromTile(in HexCoordinates coordinates, int chunkSize)
        {
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            return new SurfaceGridChunk(FloorDivide(coordinates.Q, chunkSize), FloorDivide(coordinates.R, chunkSize));
        }

        /// <summary>Delta가 영향을 준 중복 없는 dirty Chunk 집합을 만듭니다.</summary>
        public static IReadOnlyCollection<SurfaceGridChunk> CollectDirty(
            SurfaceGridDelta delta,
            int chunkSize)
        {
            if (delta == null) throw new ArgumentNullException(nameof(delta));
            HashSet<SurfaceGridChunk> chunks = new();
            Add(delta.Added, chunkSize, chunks);
            Add(delta.Removed, chunkSize, chunks);
            Add(delta.Changed, chunkSize, chunks);
            return chunks;
        }

        private static void Add(
            IReadOnlyList<HexCoordinates> coordinates,
            int chunkSize,
            ISet<SurfaceGridChunk> chunks)
        {
            foreach (HexCoordinates coordinate in coordinates) chunks.Add(FromTile(coordinate, chunkSize));
        }

        private static int FloorDivide(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            return remainder < 0 ? quotient - 1 : quotient;
        }

        /// <inheritdoc />
        public bool Equals(SurfaceGridChunk other) => Q == other.Q && R == other.R;
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SurfaceGridChunk other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Q, R);
    }
}
