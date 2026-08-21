using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>
    /// GameObject가 특정 종류의 Surface 입력을 제공하는지 판별하고 그에 맞는 Adapter를 만듭니다.
    /// 사용자는 Surface 종류를 고르지 않으며, Resolver가 이 계약으로 자동 판별합니다.
    /// </summary>
    public interface ISurfaceAdapterFactory
    {
        /// <summary>
        /// 선택 우선순위입니다. 값이 클수록 더 구체적인 입력으로 간주해 먼저 선택합니다.
        /// 같은 값이 둘 이상 매칭되면 Resolver는 임의로 고르지 않고 모호성 진단을 냅니다.
        /// </summary>
        int Priority { get; }

        /// <summary>이 factory가 대상 GameObject를 Surface로 다룰 수 있는지 검사합니다.</summary>
        bool CanCreate(GameObject target);

        /// <summary>부여받은 handle로 대상 GameObject의 Adapter를 생성합니다.</summary>
        ISurfaceAdapter Create(GameObject target, SurfaceHandle handle);
    }
}
