using System;
using System.Collections.Generic;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 하나의 seed 영역을 덮는 local Surface Patch들의 불변 집합입니다. 곡률이나 크기 때문에 단일 chart
    /// 허용치를 넘으면 여러 Patch를 보존하며, 첫 구현은 호환성을 위해 하나의 Triangle Unfolding Patch를
    /// 반환합니다.
    /// </summary>
    public sealed class SurfacePatchSet
    {
        private readonly SurfacePatch[] _patches;
        private readonly IReadOnlyList<SurfacePatch> _patchesView;

        /// <summary>요청의 원본 seed를 가져옵니다.</summary>
        public SurfacePoint Seed { get; }
        /// <summary>local chart 목록을 가져옵니다.</summary>
        public IReadOnlyList<SurfacePatch> Patches => _patchesView;
        /// <summary>Seed가 들어 있는 기본 Patch를 가져옵니다.</summary>
        public SurfacePatch PrimaryPatch => _patches[0];
        /// <summary>모든 Patch의 최대 closure error를 가져옵니다.</summary>
        public float MaximumClosureError { get; }
        /// <summary>모든 Patch의 최대 metric distortion을 가져옵니다.</summary>
        public float MaximumMetricDistortion { get; }

        /// <summary>Parameterizer가 만든 Patch 배열을 불변 집합으로 봉인합니다.</summary>
        public SurfacePatchSet(in SurfacePoint seed, SurfacePatch[] patches)
        {
            if (!seed.IsValid) throw new ArgumentException("A valid seed is required.", nameof(seed));
            if (patches == null) throw new ArgumentNullException(nameof(patches));
            if (patches.Length == 0) throw new ArgumentException("A patch set must contain its primary patch.", nameof(patches));
            if (patches[0] == null || patches[0].Surface != seed.Surface ||
                patches[0].SeedTriangleIndex != seed.TriangleIndex)
            {
                throw new ArgumentException("The primary patch must contain the requested seed face.", nameof(patches));
            }

            Seed = seed;
            _patches = (SurfacePatch[])patches.Clone();
            _patchesView = Array.AsReadOnly(_patches);
            foreach (SurfacePatch patch in _patches)
            {
                if (patch == null) throw new ArgumentException("Patch sets cannot contain null entries.", nameof(patches));
                MaximumClosureError = Math.Max(MaximumClosureError, patch.MaximumClosureError);
                MaximumMetricDistortion = Math.Max(MaximumMetricDistortion, patch.MaximumMetricDistortion);
            }
        }
    }
}
