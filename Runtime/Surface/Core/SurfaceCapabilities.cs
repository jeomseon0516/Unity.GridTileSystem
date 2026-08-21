using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Surface가 제공하는 성질입니다. 동작을 분기하는 도메인 모델이 아니라 질의 필터와 진단에
    /// 사용합니다. 소비자가 이 값으로 Unity 입력 종류를 역추론하도록 의도하지 않습니다.
    /// </summary>
    [Flags]
    public enum SurfaceCapabilities
    {
        /// <summary>알려진 성질이 없습니다.</summary>
        None = 0,
        /// <summary>변형되지 않아 topology와 위치를 한 번만 계산하면 됩니다.</summary>
        Static = 1 << 0,
        /// <summary>매 프레임 vertex 위치가 바뀔 수 있습니다.</summary>
        Deformable = 1 << 1,
        /// <summary>규칙적인 heightfield에서 유도되어 index를 계산식으로 표현할 수 있습니다.</summary>
        HeightField = 1 << 2,
        /// <summary>CPU에서 원본 geometry를 읽을 수 있습니다.</summary>
        CpuReadable = 1 << 3,
        /// <summary>bone 가중치로 변형을 따라갈 수 있습니다.</summary>
        HasSkinning = 1 << 4
    }
}
