using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>
    /// Grid 생성 결과입니다. 성공 시에는 Grid와 그것을 만든 Surface 문맥을, 실패 시에는 원인을 담습니다.
    /// </summary>
    public readonly struct SurfaceGridBuildResult
    {
        /// <summary>생성 시도가 어느 단계에서 끝났는지 가져옵니다.</summary>
        public SurfaceGridBuildStatus Status { get; }
        /// <summary>생성된 Grid입니다. 실패하면 <see langword="null"/>입니다.</summary>
        public SurfaceGrid Grid { get; }
        /// <summary>Grid가 놓인 표면의 Adapter입니다. 월드 변환과 picking Collider를 제공합니다.</summary>
        public ISurfaceAdapter Adapter { get; }
        /// <summary>Grid가 놓인 표면의 topology snapshot입니다.</summary>
        public SurfaceTopology Topology { get; }
        /// <summary>시스템이 스스로 찾아낸 seed 지점입니다.</summary>
        public SurfacePoint Seed { get; }
        /// <summary>실패 원인을 사람이 읽을 수 있게 설명합니다. 성공하면 <see langword="null"/>입니다.</summary>
        public string Diagnostic { get; }

        /// <summary>Grid를 실제로 사용할 수 있는 상태인지 가져옵니다.</summary>
        public bool IsSuccess => Status == SurfaceGridBuildStatus.Success;

        /// <summary>성공 결과를 생성합니다.</summary>
        private SurfaceGridBuildResult(
            SurfaceGridBuildStatus status,
            SurfaceGrid grid,
            ISurfaceAdapter adapter,
            SurfaceTopology topology,
            in SurfacePoint seed,
            string diagnostic)
        {
            Status = status;
            Grid = grid;
            Adapter = adapter;
            Topology = topology;
            Seed = seed;
            Diagnostic = diagnostic;
        }

        /// <summary>완전한 Tile을 가진 Grid 결과를 만듭니다.</summary>
        public static SurfaceGridBuildResult Success(
            SurfaceGrid grid,
            ISurfaceAdapter adapter,
            SurfaceTopology topology,
            in SurfacePoint seed) =>
            new(SurfaceGridBuildStatus.Success, grid, adapter, topology, seed, null);

        /// <summary>
        /// Grid 자체는 만들었지만 완전한 Tile이 없는 결과를 만듭니다. Tile 해상도가 표면보다 큰 경우가
        /// 대표적이며, 진단을 위해 Grid와 Surface 문맥을 그대로 전달합니다.
        /// </summary>
        public static SurfaceGridBuildResult Empty(
            SurfaceGrid grid,
            ISurfaceAdapter adapter,
            SurfaceTopology topology,
            in SurfacePoint seed,
            string diagnostic) =>
            new(SurfaceGridBuildStatus.NoCompleteTiles, grid, adapter, topology, seed, diagnostic);

        /// <summary>Grid를 만들지 못한 결과를 만듭니다.</summary>
        public static SurfaceGridBuildResult Failure(SurfaceGridBuildStatus status, string diagnostic) =>
            new(status, null, null, null, default, diagnostic);
    }
}
