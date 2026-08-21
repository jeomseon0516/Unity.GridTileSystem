using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Query
{
    /// <summary>Seed Surface 탐색 범위와 선택 기준입니다.</summary>
    public readonly struct SurfaceQueryOptions
    {
        /// <summary>지정하지 않았을 때 사용하는 탐색 반경입니다.</summary>
        public const float DefaultSearchRadius = 5f;

        /// <summary>Seed 주변에서 후보 geometry를 모을 반경입니다.</summary>
        public float SearchRadius { get; }
        /// <summary>
        /// 후보가 여럿일 때 선호하는 방향입니다. 지면 위 Grid가 일반적이므로 기본값은 아래쪽이며,
        /// 이 방향에 놓인 표면이 같은 거리의 다른 표면보다 우선합니다.
        /// </summary>
        public Vector3 PreferredDirection { get; }
        /// <summary>Physics 후보 수집에 사용할 layer mask입니다.</summary>
        public LayerMask LayerMask { get; }

        /// <summary>탐색 반경, 선호 방향과 layer mask를 지정합니다.</summary>
        public SurfaceQueryOptions(float searchRadius, in Vector3 preferredDirection, LayerMask layerMask)
        {
            SearchRadius = searchRadius > 0f ? searchRadius : DefaultSearchRadius;
            PreferredDirection = preferredDirection.sqrMagnitude > 0f ? preferredDirection.normalized : Vector3.down;
            LayerMask = layerMask;
        }

        /// <summary>모든 layer를 대상으로 하는 기본 옵션을 만듭니다.</summary>
        public static SurfaceQueryOptions Default =>
            new(DefaultSearchRadius, Vector3.down, ~0);
    }
}
