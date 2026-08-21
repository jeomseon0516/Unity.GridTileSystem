using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;

namespace Jeomseon.Unity.GridTileSystem.Surface.Query
{
    /// <summary>Seed 질의가 찾아낸 Surface 지점과 그것을 제공한 Adapter입니다.</summary>
    public readonly struct SurfaceQueryHit
    {
        /// <summary>Grid seed로 사용할 intrinsic 지점입니다.</summary>
        public SurfacePoint Point { get; }
        /// <summary>해당 Surface의 topology와 월드 변환을 제공하는 Adapter입니다.</summary>
        public ISurfaceAdapter Adapter { get; }
        /// <summary>구축된 topology snapshot입니다.</summary>
        public SurfaceTopology Topology { get; }
        /// <summary>질의 위치와 표면 사이의 거리입니다.</summary>
        public float Distance { get; }

        /// <summary>질의 결과를 생성합니다.</summary>
        public SurfaceQueryHit(
            in SurfacePoint point,
            ISurfaceAdapter adapter,
            SurfaceTopology topology,
            float distance)
        {
            Point = point;
            Adapter = adapter;
            Topology = topology;
            Distance = distance;
        }
    }
}
