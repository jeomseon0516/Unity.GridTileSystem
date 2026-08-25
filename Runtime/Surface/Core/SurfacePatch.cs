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
        /// <summary>모든 펼친 corner의 볼록 껍질입니다.</summary>
        private readonly Vector2[] _intrinsicHull;
        /// <summary>볼록 껍질의 외부 변경을 차단하는 view입니다.</summary>
        private readonly IReadOnlyList<Vector2> _intrinsicHullView;

        /// <summary>
        /// 펼침을 시작한 seed Surface identity를 가져옵니다. chart가 연결을 건너 다른 Surface까지
        /// 확장될 수 있으므로 이 값은 Patch 전체의 Surface가 아니라 <b>seed의 Surface</b>입니다.
        /// 각 Face가 속한 Surface는 <see cref="SurfacePatchTriangle.Surface"/>에 있습니다.
        /// </summary>
        public SurfaceHandle Surface { get; }
        /// <summary>이 chart가 둘 이상의 Surface에 걸쳐 있는지 가져옵니다.</summary>
        public bool SpansMultipleSurfaces { get; }
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
        /// 펼친 Face corner 전체의 반시계 볼록 껍질을 가져옵니다. 실제 Patch가 오목하면 여백을 포함하는
        /// 보수적 경계이며, Grid 후보의 빠른 탈락 판정에 사용해도 유효 Tile을 누락하지 않습니다.
        /// </summary>
        public IReadOnlyList<Vector2> IntrinsicHull => _intrinsicHullView;
        /// <summary>이 parameterization에서 수집한 성장·정확도 진단을 가져옵니다.</summary>
        public SurfacePatchDiagnostics Diagnostics { get; }
        /// <summary>
        /// 이미 방문한 Face에 다른 Graph 경로로 도달했을 때 관측된 최대 위치 불일치를 가져옵니다.
        /// 0에 가까우면 chart의 cycle이 일관되게 닫힌다는 뜻입니다.
        /// </summary>
        public float MaximumClosureError => Diagnostics.MaximumClosureError;
        /// <summary>Triangle 개수 또는 intrinsic radius 제한 때문에 성장이 중단됐는지 가져옵니다.</summary>
        public bool WasTruncated => Diagnostics.WasTruncated;
        /// <summary>관측된 closure error가 요청한 허용치를 초과했는지 가져옵니다.</summary>
        public bool ClosureToleranceExceeded => Diagnostics.ClosureToleranceExceeded;
        /// <summary>Patch 안 Face 중심까지의 graph geodesic 거리 상한 중 최댓값을 가져옵니다.</summary>
        public float MaximumGraphGeodesicDistance => Diagnostics.MaximumGraphGeodesicDistance;
        /// <summary>원본 3D Edge 길이와 펼친 2D Edge 길이의 최대 상대 오차를 가져옵니다.</summary>
        public float MaximumMetricDistortion => Diagnostics.MaximumMetricDistortion;
        /// <summary>원본 3D Edge 길이와 펼친 2D Edge 길이의 평균 상대 오차를 가져옵니다.</summary>
        public float AverageMetricDistortion => Diagnostics.AverageMetricDistortion;

        /// <summary>Parameterizer가 소유한 결과 데이터로 완성된 local chart를 생성합니다.</summary>
        internal SurfacePatch(
            SurfaceHandle surface,
            int seedTriangleIndex,
            SurfacePatchTriangle[] triangles,
            in SurfacePatchDiagnostics diagnostics)
        {
            if (!surface.IsValid) throw new ArgumentException("A valid surface handle is required.", nameof(surface));
            Surface = surface;
            SeedTriangleIndex = seedTriangleIndex;
            _triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
            if (_triangles.Length == 0)
                throw new ArgumentException("A patch must contain at least its seed face.", nameof(triangles));
            _trianglesView = Array.AsReadOnly(_triangles);
            IntrinsicBounds = CalculateBounds(_triangles);
            _intrinsicHull = CalculateConvexHull(_triangles);
            _intrinsicHullView = Array.AsReadOnly(_intrinsicHull);
            SpansMultipleSurfaces = HasMultipleSurfaces(_triangles);
            Diagnostics = diagnostics;
        }

        /// <summary>펼친 Face 중 seed와 다른 Surface에 속한 것이 있는지 검사합니다.</summary>
        private static bool HasMultipleSurfaces(SurfacePatchTriangle[] triangles)
        {
            SurfaceHandle first = triangles[0].Surface;
            foreach (SurfacePatchTriangle triangle in triangles)
            {
                if (triangle.Surface != first) return true;
            }
            return false;
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

        /// <summary>Andrew monotone chain으로 중복 corner를 제거한 반시계 볼록 껍질을 계산합니다.</summary>
        private static Vector2[] CalculateConvexHull(SurfacePatchTriangle[] triangles)
        {
            List<Vector2> points = new(triangles.Length * 3);
            foreach (SurfacePatchTriangle triangle in triangles)
            {
                points.Add(triangle.A);
                points.Add(triangle.B);
                points.Add(triangle.C);
            }
            points.Sort(ComparePoints);

            List<Vector2> unique = new(points.Count);
            foreach (Vector2 point in points)
            {
                if (unique.Count == 0 || unique[^1] != point) unique.Add(point);
            }
            if (unique.Count <= 2) return unique.ToArray();

            List<Vector2> hull = new(unique.Count * 2);
            foreach (Vector2 point in unique)
            {
                while (hull.Count >= 2 && Cross(hull[^1] - hull[^2], point - hull[^1]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }

            int lowerCount = hull.Count;
            for (int i = unique.Count - 2; i >= 0; i--)
            {
                Vector2 point = unique[i];
                while (hull.Count > lowerCount && Cross(hull[^1] - hull[^2], point - hull[^1]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
            return hull.ToArray();
        }

        private static int ComparePoints(Vector2 left, Vector2 right)
        {
            int x = left.x.CompareTo(right.x);
            return x != 0 ? x : left.y.CompareTo(right.y);
        }

        private static float Cross(in Vector2 left, in Vector2 right) => left.x * right.y - left.y * right.x;

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
