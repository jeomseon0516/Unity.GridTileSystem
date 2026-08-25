namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>Surface parameterization 한 번에서 측정한 성장 제한·폐합·거리·왜곡 진단입니다.</summary>
    public readonly struct SurfacePatchDiagnostics
    {
        public float MaximumClosureError { get; }
        public bool WasTruncated { get; }
        public bool ClosureToleranceExceeded { get; }
        public float MaximumGraphGeodesicDistance { get; }
        public float MaximumMetricDistortion { get; }
        public float AverageMetricDistortion { get; }

        public SurfacePatchDiagnostics(
            float maximumClosureError,
            bool wasTruncated,
            bool closureToleranceExceeded,
            float maximumGraphGeodesicDistance,
            float maximumMetricDistortion,
            float averageMetricDistortion)
        {
            MaximumClosureError = maximumClosureError;
            WasTruncated = wasTruncated;
            ClosureToleranceExceeded = closureToleranceExceeded;
            MaximumGraphGeodesicDistance = maximumGraphGeodesicDistance;
            MaximumMetricDistortion = maximumMetricDistortion;
            AverageMetricDistortion = averageMetricDistortion;
        }
    }
}
