using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>Surface 사이의 연결을 인정하는 기준입니다.</summary>
    public readonly struct SurfaceConnectivityOptions
    {
        /// <summary>지정하지 않았을 때 두 Edge 끝점이 같은 위치라고 볼 최대 거리입니다.</summary>
        public const float DefaultPositionTolerance = 0.001f;
        /// <summary>지정하지 않았을 때 두 Face 법선 사이에 허용할 최대 각도(도)입니다.</summary>
        public const float DefaultMaximumNormalAngle = 90f;

        /// <summary>두 Edge 끝점이 같은 위치라고 볼 최대 월드 거리입니다.</summary>
        public float PositionTolerance { get; }
        /// <summary>
        /// 두 Face 법선 사이에 허용할 최대 각도(도)입니다. 이 값을 넘으면 위치가 맞아도 연결로 보지
        /// 않습니다. 벽과 그 뒷면처럼 같은 경계를 공유하지만 반대를 향하는 표면을 걸러냅니다.
        /// </summary>
        public float MaximumNormalAngle { get; }

        /// <summary>연결 후보 표면을 찾을 때 사용할 layer mask입니다.</summary>
        public LayerMask LayerMask { get; }

        /// <summary>기본 위치 허용 오차, 법선 각도 제한과 모든 layer를 가져옵니다.</summary>
        public static SurfaceConnectivityOptions Default =>
            new(DefaultPositionTolerance, DefaultMaximumNormalAngle, ~0);

        /// <summary>위치 허용 오차와 법선 각도 제한을 지정하고 모든 layer를 대상으로 합니다.</summary>
        public SurfaceConnectivityOptions(float positionTolerance, float maximumNormalAngle)
            : this(positionTolerance, maximumNormalAngle, ~0)
        {
        }

        /// <summary>위치 허용 오차, 법선 각도 제한과 후보 탐색 layer mask를 지정합니다.</summary>
        public SurfaceConnectivityOptions(float positionTolerance, float maximumNormalAngle, LayerMask layerMask)
        {
            LayerMask = layerMask;
            PositionTolerance = positionTolerance > 0f && !float.IsNaN(positionTolerance)
                ? positionTolerance
                : DefaultPositionTolerance;
            MaximumNormalAngle = maximumNormalAngle >= 0f && maximumNormalAngle <= 180f
                ? maximumNormalAngle
                : DefaultMaximumNormalAngle;
        }

        /// <summary>두 Face 법선이 연결로 인정할 만큼 정합하는지 검사합니다.</summary>
        public bool IsNormalCompatible(in Vector3 first, in Vector3 second)
        {
            if (first.sqrMagnitude <= 0f || second.sqrMagnitude <= 0f) return false;
            float cosine = Vector3.Dot(first.normalized, second.normalized);
            return cosine >= Mathf.Cos(Mathf.Deg2Rad * MaximumNormalAngle) - 0.000001f;
        }
    }
}
