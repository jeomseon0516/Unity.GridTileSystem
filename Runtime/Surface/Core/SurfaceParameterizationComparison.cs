using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>동일 Surface 영역에서 두 parameterizer의 Patch 수와 왜곡 진단을 비교한 결과입니다.</summary>
    public readonly struct SurfaceParameterizationComparison
    {
        /// <summary>기준 구현의 Patch 수를 가져옵니다.</summary>
        public int BaselinePatchCount { get; }
        /// <summary>후보 구현의 Patch 수를 가져옵니다.</summary>
        public int CandidatePatchCount { get; }
        /// <summary>기준 구현의 최대 metric distortion을 가져옵니다.</summary>
        public float BaselineMaximumMetricDistortion { get; }
        /// <summary>후보 구현의 최대 metric distortion을 가져옵니다.</summary>
        public float CandidateMaximumMetricDistortion { get; }
        /// <summary>후보 구현이 최대 metric distortion을 줄였는지 가져옵니다.</summary>
        public bool CandidateHasLowerDistortion =>
            CandidateMaximumMetricDistortion < BaselineMaximumMetricDistortion;

        private SurfaceParameterizationComparison(SurfacePatchSet baseline, SurfacePatchSet candidate)
        {
            BaselinePatchCount = baseline.Patches.Count;
            CandidatePatchCount = candidate.Patches.Count;
            BaselineMaximumMetricDistortion = baseline.MaximumMetricDistortion;
            CandidateMaximumMetricDistortion = candidate.MaximumMetricDistortion;
        }

        /// <summary>두 parameterizer를 동일 입력으로 실행해 진단을 비교합니다.</summary>
        public static SurfaceParameterizationComparison Compare(
            ISurfaceParameterizer baseline,
            ISurfaceParameterizer candidate,
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            in SurfacePatchBuildSettings settings,
            ISurfaceConnectivity connectivity = null)
        {
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            SurfacePatchSet baselineResult = baseline.Parameterize(surfaces, seed, settings, connectivity);
            SurfacePatchSet candidateResult = candidate.Parameterize(surfaces, seed, settings, connectivity);
            return new SurfaceParameterizationComparison(baselineResult, candidateResult);
        }
    }
}
