using System;
using System.Collections.Generic;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 연결된 원본 Triangle 집합의 불변 local 2D parameterization입니다. 전역적으로 유일한 UV map이
    /// 아니라 local chart이므로 곡률이 있는 cycle에서는 0이 아닌 closure error가 발생할 수 있습니다.
    /// </summary>
    public sealed class SurfacePatch
    {
        /// <summary>원본 Triangle index 순서로 저장한 Face별 intrinsic 좌표입니다.</summary>
        private readonly SurfacePatchTriangle[] _triangles;
        /// <summary>내부 배열의 외부 변경을 차단하는 Face view입니다.</summary>
        private readonly IReadOnlyList<SurfacePatchTriangle> _trianglesView;

        /// <summary>이 Patch를 생성한 원본 Surface identity를 가져옵니다.</summary>
        public SurfaceHandle Surface { get; }
        /// <summary>펼침을 시작한 Seed Triangle index를 가져옵니다.</summary>
        public int SeedTriangleIndex { get; }
        /// <summary>이 local chart에서 펼친 모든 Face를 가져옵니다.</summary>
        public IReadOnlyList<SurfacePatchTriangle> Triangles => _trianglesView;
        /// <summary>
        /// 이미 방문한 Face에 다른 Graph 경로로 도달했을 때 관측된 최대 위치 불일치를 가져옵니다.
        /// 0에 가까우면 chart의 cycle이 일관되게 닫힌다는 뜻입니다.
        /// </summary>
        public float MaximumClosureError { get; }
        /// <summary>Triangle 개수 또는 intrinsic radius 제한 때문에 성장이 중단됐는지 가져옵니다.</summary>
        public bool WasTruncated { get; }
        /// <summary>관측된 closure error가 요청한 허용치를 초과했는지 가져옵니다.</summary>
        public bool ClosureToleranceExceeded { get; }

        /// <summary>Parameterizer가 소유한 결과 데이터로 완성된 local chart를 생성합니다.</summary>
        internal SurfacePatch(
            SurfaceHandle surface,
            int seedTriangleIndex,
            SurfacePatchTriangle[] triangles,
            float maximumClosureError,
            bool wasTruncated,
            bool closureToleranceExceeded)
        {
            if (!surface.IsValid) throw new ArgumentException("A valid surface handle is required.", nameof(surface));
            Surface = surface;
            SeedTriangleIndex = seedTriangleIndex;
            _triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
            _trianglesView = Array.AsReadOnly(_triangles);
            MaximumClosureError = maximumClosureError;
            WasTruncated = wasTruncated;
            ClosureToleranceExceeded = closureToleranceExceeded;
        }
    }
}
