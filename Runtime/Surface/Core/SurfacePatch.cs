using System;
using System.Collections.Generic;
using UnityEngine;

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
        /// 펼친 모든 Face 꼭짓점을 포함하는 intrinsic 2D 축 정렬 경계입니다. Grid 생성이 이 범위에서
        /// 덮어야 할 Tile 좌표 구간을 직접 산출하므로, 사용자가 별도의 Grid 범위를 지정할 필요가 없습니다.
        /// Patch에는 항상 Seed Face가 하나 이상 있으므로 이 값은 비어 있지 않습니다.
        /// </summary>
        public Rect IntrinsicBounds { get; }
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
            if (_triangles.Length == 0)
                throw new ArgumentException("A patch must contain at least its seed face.", nameof(triangles));
            _trianglesView = Array.AsReadOnly(_triangles);
            IntrinsicBounds = CalculateBounds(_triangles);
            MaximumClosureError = maximumClosureError;
            WasTruncated = wasTruncated;
            ClosureToleranceExceeded = closureToleranceExceeded;
        }

        /// <summary>펼친 Face 꼭짓점 전체를 감싸는 축 정렬 경계를 계산합니다.</summary>
        private static Rect CalculateBounds(SurfacePatchTriangle[] triangles)
        {
            float minimumX = float.PositiveInfinity;
            float minimumY = float.PositiveInfinity;
            float maximumX = float.NegativeInfinity;
            float maximumY = float.NegativeInfinity;

            foreach (SurfacePatchTriangle triangle in triangles)
            {
                Accumulate(triangle.A, ref minimumX, ref minimumY, ref maximumX, ref maximumY);
                Accumulate(triangle.B, ref minimumX, ref minimumY, ref maximumX, ref maximumY);
                Accumulate(triangle.C, ref minimumX, ref minimumY, ref maximumX, ref maximumY);
            }

            return Rect.MinMaxRect(minimumX, minimumY, maximumX, maximumY);
        }

        /// <summary>단일 꼭짓점을 경계 누적값에 반영합니다.</summary>
        private static void Accumulate(
            in Vector2 corner,
            ref float minimumX,
            ref float minimumY,
            ref float maximumX,
            ref float maximumY)
        {
            if (corner.x < minimumX) minimumX = corner.x;
            if (corner.y < minimumY) minimumY = corner.y;
            if (corner.x > maximumX) maximumX = corner.x;
            if (corner.y > maximumY) maximumY = corner.y;
        }
    }
}
