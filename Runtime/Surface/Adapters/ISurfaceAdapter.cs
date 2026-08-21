using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>
    /// 하나의 Unity 표면을 Surface Space 표현으로 변환하는 경계입니다. Unity 타입 접근은 이 계층에서
    /// 끝나며, Surface Core와 Grid는 <see cref="SurfaceTopology"/>와 <see cref="SurfaceHandle"/>만 봅니다.
    /// </summary>
    /// <remarks>
    /// Adapter는 Grid나 Tile 상태를 소유하지 않습니다. topology 구축과 월드 좌표 변환까지만 책임집니다.
    /// </remarks>
    public interface ISurfaceAdapter : IDisposable
    {
        /// <summary>Registry가 이 Surface에 부여한 식별자를 가져옵니다.</summary>
        SurfaceHandle Handle { get; }
        /// <summary>질의 필터와 진단에 사용하는 Surface 성질을 가져옵니다.</summary>
        SurfaceCapabilities Capabilities { get; }
        /// <summary>Surface local 좌표를 월드로 옮기는 기준 Transform을 가져옵니다.</summary>
        Transform SurfaceTransform { get; }
        /// <summary>
        /// Pointer picking에 사용할 Collider를 가져옵니다. Physics는 필수가 아니므로 null일 수 있으며,
        /// null이면 논리 Grid와 표현은 그대로 동작하고 pointer picking만 비활성화됩니다.
        /// </summary>
        Collider PickingCollider { get; }
        /// <summary>공간 질의 후보를 좁히는 데 사용할 월드 경계를 가져옵니다.</summary>
        Bounds WorldBounds { get; }

        /// <summary>이 Surface의 topology snapshot을 구축합니다.</summary>
        SurfaceTopology BuildTopology();
        /// <summary>intrinsic 점을 현재 월드 위치로 평가합니다.</summary>
        bool TryEvaluateWorldPosition(in SurfacePoint point, out Vector3 worldPosition);
    }
}
