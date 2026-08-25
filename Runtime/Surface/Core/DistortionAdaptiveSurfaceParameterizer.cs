using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 후보 parameterizer의 metric distortion이 임계값을 넘으면 Patch당 Triangle 한계를 절반씩 줄여
    /// 자동 재분할합니다. 단일 Face에서도 임계값을 만족하지 못하면 진단값을 보존한 채 종료합니다.
    /// </summary>
    public sealed class DistortionAdaptiveSurfaceParameterizer : ISurfaceParameterizer
    {
        private readonly ISurfaceParameterizer _inner;

        /// <summary>허용할 최대 Edge 상대 길이 오차를 가져옵니다.</summary>
        public float MaximumMetricDistortion { get; }

        /// <summary>비교할 parameterizer와 양수 distortion 임계값을 생성합니다.</summary>
        public DistortionAdaptiveSurfaceParameterizer(
            ISurfaceParameterizer inner,
            float maximumMetricDistortion)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (maximumMetricDistortion <= 0f || float.IsNaN(maximumMetricDistortion) ||
                float.IsInfinity(maximumMetricDistortion))
                throw new ArgumentOutOfRangeException(nameof(maximumMetricDistortion));
            MaximumMetricDistortion = maximumMetricDistortion;
        }

        /// <inheritdoc />
        public SurfacePatchSet Parameterize(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            in SurfacePatchBuildSettings settings,
            ISurfaceConnectivity connectivity)
        {
            SurfacePatchSet result = _inner.Parameterize(surfaces, seed, settings, connectivity);
            int maximumTriangles = settings.MaximumTriangleCount == int.MaxValue
                ? GetLargestPatchTriangleCount(result)
                : settings.MaximumTriangleCount;
            while (result.MaximumMetricDistortion > MaximumMetricDistortion && maximumTriangles > 1)
            {
                maximumTriangles = Math.Max(1, maximumTriangles / 2);
                SurfacePatchBuildSettings splitSettings = new(
                    maximumTriangles,
                    settings.MaximumIntrinsicRadius,
                    settings.MaximumClosureError,
                    true);
                result = _inner.Parameterize(surfaces, seed, splitSettings, connectivity);
            }
            return result;
        }

        private static int GetLargestPatchTriangleCount(SurfacePatchSet patchSet)
        {
            int maximum = 1;
            foreach (SurfacePatch patch in patchSet.Patches)
                maximum = Math.Max(maximum, patch.Triangles.Count);
            return maximum;
        }
    }
}
