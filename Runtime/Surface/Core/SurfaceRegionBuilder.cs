using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>볼록 intrinsic polygon을 펼쳐진 Surface Triangle과 clipping하여 Surface Region을 생성합니다.</summary>
    public static class SurfaceRegionBuilder
    {
        /// <summary>경계 위의 점을 부동소수점 오차로 외부 판정하지 않기 위한 거리성 허용 오차입니다.</summary>
        private const float BoundaryTolerance = 0.000001f;

        /// <summary>
        /// 반시계 또는 시계 방향의 볼록 polygon과 Patch Triangle의 교집합을 계산합니다.
        /// 결과 vertex는 원본 Triangle의 barycentric 좌표를 가지므로 3D Surface로 정확히 복원됩니다.
        /// </summary>
        public static SurfaceRegion Build(
            SurfaceTopology topology,
            SurfacePatch patch,
            IReadOnlyList<Vector2> convexPolygon)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (patch == null) throw new ArgumentNullException(nameof(patch));
            if (patch.Surface != topology.Handle)
                throw new ArgumentException("Patch belongs to another surface.", nameof(patch));
            if (convexPolygon == null) throw new ArgumentNullException(nameof(convexPolygon));
            if (convexPolygon.Count < 3)
                throw new ArgumentException("Clipping polygon must contain at least three vertices.", nameof(convexPolygon));

            Vector2[] clipPolygon = CopyCounterClockwise(convexPolygon);
            List<SurfaceRegionVertex> vertices = new();
            List<int> indices = new();

            foreach (SurfacePatchTriangle patchTriangle in patch.Triangles)
            {
                List<Vector2> clipped = new() { patchTriangle.A, patchTriangle.B, patchTriangle.C };
                for (int clipEdge = 0; clipEdge < clipPolygon.Length && clipped.Count > 0; clipEdge++)
                {
                    Vector2 edgeStart = clipPolygon[clipEdge];
                    Vector2 edgeEnd = clipPolygon[(clipEdge + 1) % clipPolygon.Length];
                    clipped = ClipAgainstHalfPlane(clipped, edgeStart, edgeEnd);
                }

                if (clipped.Count < 3) continue;
                int firstVertex = vertices.Count;
                foreach (Vector2 point in clipped)
                {
                    Vector3 barycentric = CalculateBarycentric(patchTriangle, point);
                    SurfacePoint surfacePoint = new(topology.Handle, patchTriangle.TriangleIndex, barycentric);
                    vertices.Add(new SurfaceRegionVertex(point, surfacePoint));
                }

                // 볼록 polygon은 첫 vertex를 중심으로 fan triangulation할 수 있습니다.
                // clipping 결과의 순서를 보존하므로 새 대각선은 polygon 외부로 나가지 않습니다.
                for (int corner = 1; corner < clipped.Count - 1; corner++)
                {
                    indices.Add(firstVertex);
                    indices.Add(firstVertex + corner);
                    indices.Add(firstVertex + corner + 1);
                }
            }

            return new SurfaceRegion(vertices.ToArray(), indices.ToArray());
        }

        /// <summary>polygon을 복사하고 clipping 내부 판정이 기대하는 반시계 방향으로 정규화합니다.</summary>
        private static Vector2[] CopyCounterClockwise(IReadOnlyList<Vector2> polygon)
        {
            Vector2[] result = new Vector2[polygon.Count];
            for (int i = 0; i < result.Length; i++) result[i] = polygon[i];

            // Shoelace 공식의 두 배 signed area입니다. 음수면 입력이 시계 방향이므로 뒤집습니다.
            float signedAreaTwice = 0f;
            for (int i = 0; i < result.Length; i++)
            {
                signedAreaTwice += CrossZ(result[i], result[(i + 1) % result.Length]);
            }

            if (Mathf.Abs(signedAreaTwice) <= BoundaryTolerance)
                throw new ArgumentException("Clipping polygon must have non-zero area.", nameof(polygon));
            if (signedAreaTwice < 0f) Array.Reverse(result);

            for (int i = 0; i < result.Length; i++)
            {
                Vector2 current = result[i];
                Vector2 next = result[(i + 1) % result.Length];
                Vector2 following = result[(i + 2) % result.Length];
                if (!IsFinite(current))
                    throw new ArgumentException($"Clipping polygon vertex {i} contains NaN or Infinity.", nameof(polygon));
                if ((next - current).sqrMagnitude <= BoundaryTolerance * BoundaryTolerance)
                    throw new ArgumentException("Clipping polygon must not contain zero-length edges.", nameof(polygon));

                // 반시계 polygon의 연속 Edge는 모두 왼쪽으로 돌거나 일직선이어야 합니다.
                // 음수 회전이 하나라도 있으면 concave이므로 Sutherland-Hodgman 볼록 clip 계약이 깨집니다.
                if (CrossZ(next - current, following - next) < -BoundaryTolerance)
                    throw new ArgumentException("Clipping polygon must be convex.", nameof(polygon));
            }
            return result;
        }

        /// <summary>
        /// Sutherland-Hodgman 단계 하나를 수행하여 방향성 clip Edge의 왼쪽 반평면만 보존합니다.
        /// </summary>
        private static List<Vector2> ClipAgainstHalfPlane(
            IReadOnlyList<Vector2> subject,
            in Vector2 clipStart,
            in Vector2 clipEnd)
        {
            List<Vector2> output = new();
            Vector2 previous = subject[subject.Count - 1];
            bool previousInside = IsInside(clipStart, clipEnd, previous);

            for (int i = 0; i < subject.Count; i++)
            {
                Vector2 current = subject[i];
                bool currentInside = IsInside(clipStart, clipEnd, current);
                if (currentInside != previousInside)
                {
                    output.Add(IntersectSegmentWithLine(previous, current, clipStart, clipEnd));
                }
                if (currentInside) output.Add(current);

                previous = current;
                previousInside = currentInside;
            }

            return output;
        }

        /// <summary>점이 반시계 clip Edge의 왼쪽 또는 허용 오차 내 경계에 있는지 검사합니다.</summary>
        private static bool IsInside(in Vector2 edgeStart, in Vector2 edgeEnd, in Vector2 point) =>
            CrossZ(edgeEnd - edgeStart, point - edgeStart) >= -BoundaryTolerance;

        /// <summary>유한 선분과 무한 clip 직선의 교점을 계산합니다.</summary>
        private static Vector2 IntersectSegmentWithLine(
            in Vector2 segmentStart,
            in Vector2 segmentEnd,
            in Vector2 lineStart,
            in Vector2 lineEnd)
        {
            Vector2 segment = segmentEnd - segmentStart;
            Vector2 line = lineEnd - lineStart;
            float denominator = CrossZ(segment, line);
            if (Mathf.Abs(denominator) <= BoundaryTolerance)
            {
                // inside 상태가 바뀌었는데 두 선이 수치적으로 평행한 경우입니다. 큰 좌표나 매우 짧은
                // Edge에서만 발생하며, NaN 대신 경계에 더 가까운 endpoint를 안정적인 fallback으로 씁니다.
                float startDistance = Mathf.Abs(CrossZ(line, segmentStart - lineStart));
                float endDistance = Mathf.Abs(CrossZ(line, segmentEnd - lineStart));
                return startDistance <= endDistance ? segmentStart : segmentEnd;
            }

            // p+t*r = q+u*s에서 양변과 s를 외적하면 u 항이 사라집니다.
            // 따라서 t = cross(q-p,s)/cross(r,s)이며 선분 교점이므로 수치 오차만 [0,1]로 clamp합니다.
            float t = CrossZ(lineStart - segmentStart, line) / denominator;
            return segmentStart + segment * Mathf.Clamp01(t);
        }

        /// <summary>2D Triangle 안의 점을 원본 corner A/B/C에 대한 barycentric 가중치로 변환합니다.</summary>
        private static Vector3 CalculateBarycentric(in SurfacePatchTriangle triangle, in Vector2 point)
        {
            float denominator = CrossZ(triangle.B - triangle.A, triangle.C - triangle.A);
            if (Mathf.Abs(denominator) <= BoundaryTolerance)
                throw new InvalidOperationException($"Patch triangle {triangle.TriangleIndex} is degenerate.");

            // 각 가중치는 점 반대편 부분 Triangle의 signed area를 전체 signed area로 나눈 값입니다.
            // 세 부분 면적의 합은 전체 면적이므로 u+v+w=1이 유지되고 winding 부호도 서로 상쇄됩니다.
            float u = CrossZ(triangle.B - point, triangle.C - point) / denominator;
            float v = CrossZ(triangle.C - point, triangle.A - point) / denominator;
            float w = 1f - u - v;

            // 경계 clipping에서 생기는 -0에 가까운 값만 제거한 뒤 다시 정규화합니다.
            // 큰 음수는 알고리즘 결함을 숨기지 않도록 clamp하지 않습니다.
            if (u < 0f && u >= -BoundaryTolerance) u = 0f;
            if (v < 0f && v >= -BoundaryTolerance) v = 0f;
            if (w < 0f && w >= -BoundaryTolerance) w = 0f;
            float sum = u + v + w;
            return new Vector3(u / sum, v / sum, w / sum);
        }

        /// <summary>두 2D 벡터 외적의 스칼라 z 성분을 반환합니다.</summary>
        private static float CrossZ(in Vector2 a, in Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>벡터의 두 성분이 NaN/Infinity가 아닌지 검사합니다.</summary>
        private static bool IsFinite(in Vector2 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y);
    }
}
