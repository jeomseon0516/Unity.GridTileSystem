using System;
using System.Collections.Generic;
using System.Linq;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>두 Grid snapshot 사이에 추가·제거·변경된 Logical Tile identity를 보존합니다.</summary>
    public sealed class SurfaceGridDelta
    {
        private readonly HexCoordinates[] _added;
        private readonly HexCoordinates[] _removed;
        private readonly HexCoordinates[] _changed;

        /// <summary>이전 snapshot 버전을 가져옵니다.</summary>
        public long FromVersion { get; }
        /// <summary>새 snapshot 버전을 가져옵니다.</summary>
        public long ToVersion { get; }
        /// <summary>새로 생긴 Tile 좌표를 가져옵니다.</summary>
        public IReadOnlyList<HexCoordinates> Added => _added;
        /// <summary>사라진 Tile 좌표를 가져옵니다.</summary>
        public IReadOnlyList<HexCoordinates> Removed => _removed;
        /// <summary>identity는 유지됐지만 Surface binding이 달라진 Tile 좌표를 가져옵니다.</summary>
        public IReadOnlyList<HexCoordinates> Changed => _changed;
        /// <summary>실제 변경이 하나라도 있는지 가져옵니다.</summary>
        public bool HasChanges => _added.Length > 0 || _removed.Length > 0 || _changed.Length > 0;

        private SurfaceGridDelta(
            long fromVersion,
            long toVersion,
            HexCoordinates[] added,
            HexCoordinates[] removed,
            HexCoordinates[] changed)
        {
            FromVersion = fromVersion;
            ToVersion = toVersion;
            _added = added;
            _removed = removed;
            _changed = changed;
        }

        internal static SurfaceGridDelta Create(SurfaceGridSnapshot previous, SurfaceGridSnapshot newer)
        {
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            if (newer == null) throw new ArgumentNullException(nameof(newer));
            if (newer.Version <= previous.Version)
                throw new ArgumentException("The compared snapshot must have a newer version.", nameof(newer));

            Dictionary<HexCoordinates, SurfaceGridTileRegion> before =
                previous.Grid.Tiles.ToDictionary(tile => tile.Coordinates);
            Dictionary<HexCoordinates, SurfaceGridTileRegion> after =
                newer.Grid.Tiles.ToDictionary(tile => tile.Coordinates);
            HexCoordinates[] added = after.Keys.Where(key => !before.ContainsKey(key)).ToArray();
            HexCoordinates[] removed = before.Keys.Where(key => !after.ContainsKey(key)).ToArray();
            HexCoordinates[] changed = after.Keys
                .Where(key => before.TryGetValue(key, out SurfaceGridTileRegion oldTile) &&
                              !HasSameBinding(oldTile, after[key]))
                .ToArray();
            return new SurfaceGridDelta(previous.Version, newer.Version, added, removed, changed);
        }

        private static bool HasSameBinding(SurfaceGridTileRegion first, SurfaceGridTileRegion second)
        {
            if (first.IntrinsicCenter != second.IntrinsicCenter ||
                first.Region.Vertices.Count != second.Region.Vertices.Count ||
                first.Region.TriangleIndices.Count != second.Region.TriangleIndices.Count)
                return false;

            for (int i = 0; i < first.Region.Vertices.Count; i++)
            {
                if (first.Region.Vertices[i].IntrinsicPosition != second.Region.Vertices[i].IntrinsicPosition ||
                    !first.Region.Vertices[i].SurfacePoint.Equals(second.Region.Vertices[i].SurfacePoint))
                    return false;
            }
            for (int i = 0; i < first.Region.TriangleIndices.Count; i++)
            {
                if (first.Region.TriangleIndices[i] != second.Region.TriangleIndices[i]) return false;
            }
            return true;
        }
    }
}
