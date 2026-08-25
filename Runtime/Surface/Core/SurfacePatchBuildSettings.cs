using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>Triangle Unfolding Patch의 성장 범위와 곡률 오차 허용치를 정의합니다.</summary>
    public readonly struct SurfacePatchBuildSettings
    {
        /// <summary>제한을 적용하지 않고 연결 성분 전체를 펼치는 prototype 설정을 가져옵니다.</summary>
        public static SurfacePatchBuildSettings Unlimited => new(
            int.MaxValue, float.PositiveInfinity, float.PositiveInfinity, false);

        /// <summary>Patch에 포함할 최대 Triangle 개수를 가져옵니다.</summary>
        public int MaximumTriangleCount { get; }
        /// <summary>Seed에서 Triangle 중심까지 adjacency graph를 따라 누적할 최대 intrinsic 거리를 가져옵니다.</summary>
        public float MaximumIntrinsicRadius { get; }
        /// <summary>서로 다른 펼침 경로 사이에서 허용할 최대 closure error를 가져옵니다.</summary>
        public float MaximumClosureError { get; }
        /// <summary>성장 제한에서 잘린 이웃 Face를 새 Patch seed로 이어서 전체 연결 영역을 분할할지 가져옵니다.</summary>
        public bool SplitWhenLimitReached { get; }

        /// <summary>Patch 성장 제한을 생성합니다. 모든 값은 양수여야 하며 거리는 무한대일 수 있습니다.</summary>
        public SurfacePatchBuildSettings(
            int maximumTriangleCount,
            float maximumIntrinsicRadius,
            float maximumClosureError,
            bool splitWhenLimitReached = false)
        {
            if (maximumTriangleCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumTriangleCount));
            if (maximumIntrinsicRadius <= 0f || float.IsNaN(maximumIntrinsicRadius))
                throw new ArgumentOutOfRangeException(nameof(maximumIntrinsicRadius));
            if (maximumClosureError <= 0f || float.IsNaN(maximumClosureError))
                throw new ArgumentOutOfRangeException(nameof(maximumClosureError));

            MaximumTriangleCount = maximumTriangleCount;
            MaximumIntrinsicRadius = maximumIntrinsicRadius;
            MaximumClosureError = maximumClosureError;
            SplitWhenLimitReached = splitWhenLimitReached;
        }
    }
}
