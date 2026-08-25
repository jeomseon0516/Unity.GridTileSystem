using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>버전과 immutable Grid를 결합해 Backend가 이전 결과와 안전하게 비교할 수 있게 합니다.</summary>
    public sealed class SurfaceGridSnapshot
    {
        /// <summary>동일 생산자 안에서 단조 증가하는 snapshot 버전을 가져옵니다.</summary>
        public long Version { get; }
        /// <summary>이 버전이 봉인한 immutable Grid를 가져옵니다.</summary>
        public SurfaceGrid Grid { get; }

        /// <summary>양수 버전과 Grid로 snapshot을 생성합니다.</summary>
        public SurfaceGridSnapshot(long version, SurfaceGrid grid)
        {
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
            Version = version;
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        /// <summary>이 snapshot과 더 최신 snapshot 사이의 변경 집합을 계산합니다.</summary>
        public SurfaceGridDelta Diff(SurfaceGridSnapshot newer) => SurfaceGridDelta.Create(this, newer);
    }
}
