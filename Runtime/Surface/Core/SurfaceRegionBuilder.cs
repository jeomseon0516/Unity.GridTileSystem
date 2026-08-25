using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>볼록 intrinsic polygon을 펼쳐진 Surface Triangle과 clipping하여 Surface Region을 생성합니다.</summary>
    public static class SurfaceRegionBuilder
    {
        // 경계 위의 점을 부동소수점 오차로 외부 판정하지 않기 위한 거리성 허용 오차입니다. 같은
        // 물리적 교점이라도 어느 clip 단계를 거쳤는지에 따라 float32 반올림 오차가 몇 ULP씩
        // 달라질 수 있고(좌표 크기 ~10~20 범위에서 실측 오차 ~1e-6), 예전 1e-6은 그 오차 자체와
        // 크기가 같아 AppendDistinct의 clip-내부 중복 제거가 실패했습니다(SurfaceRegionCanonicalizer의
        // Tolerance와 같은 이유로 함께 올림). 정상적인 최소 Tile 반지름(0.025)보다는 훨씬 작습니다.
        private const float BoundaryTolerance = 0.0001f;

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
            return Build(patch, convexPolygon);
        }

        /// <summary>
        /// Surface topology 없이 Patch가 보존한 Face별 Surface identity만으로 Region을 만듭니다.
        /// clipping과 barycentric 복원은 2D chart 좌표만 사용하므로 topology가 필요 없으며, 덕분에
        /// 여러 Surface에 걸친 chart도 Face마다 올바른 Surface를 가리키는 Region을 만들 수 있습니다.
        /// </summary>
        public static SurfaceRegion Build(
            SurfacePatch patch,
            IReadOnlyList<Vector2> convexPolygon) => Build(patch, convexPolygon, null);

        /// <summary>Grid build 범위의 canonical 좌표 캐시를 공유하며 Region을 만듭니다.</summary>
        internal static SurfaceRegion Build(
            SurfacePatch patch,
            IReadOnlyList<Vector2> convexPolygon,
            SurfaceRegionCanonicalizer canonicalizer)
        {
            if (patch == null) throw new ArgumentNullException(nameof(patch));
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
                    Vector2 canonicalPoint = canonicalizer?.Canonicalize(point) ?? point;
                    // canonicalPoint는 인접 fragment가 동일한 intrinsic 경계 좌표를 공유하기 위한 표시
                    // 좌표입니다. 다른 Face에서 먼저 등록된 값으로 최대 tolerance만큼 이동할 수 있으므로
                    // 현재 Face의 Surface binding까지 그 값으로 계산하면 가는 Triangle(Terrain heightmap
                    // 등)에서는 barycentric 오차가 증폭돼 Face 밖으로 벗어날 수 있습니다. binding은
                    // clipping이 보장한 현재 Face 내부의 원래 point에서 계산해야 합니다.
                    Vector3 barycentric = CalculateBarycentric(patchTriangle, point);
                    SurfacePoint surfacePoint = new(patchTriangle.Surface, patchTriangle.TriangleIndex, barycentric);
                    vertices.Add(new SurfaceRegionVertex(canonicalPoint, surfacePoint));
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
                    AppendDistinct(output, IntersectSegmentWithLine(previous, current, clipStart, clipEnd));
                }
                if (currentInside) AppendDistinct(output, current);

                previous = current;
                previousInside = currentInside;
            }

            // 닫힌 고리이므로 마지막과 첫 점이 겹칠 수도 있습니다(예: subject의 마지막 vertex가 정확히
            // clip 경계 위에 있어 intersection과 그 vertex 자체가 같은 위치인 경우).
            if (output.Count > 1 && (output[0] - output[^1]).sqrMagnitude <= BoundaryTolerance * BoundaryTolerance)
                output.RemoveAt(output.Count - 1);

            return output;
        }

        /// <summary>
        /// subject vertex가 clip 경계 위에 정확히(오차 이내) 있으면 Sutherland-Hodgman의 intersection
        /// 계산과 그 vertex 자체가 같은 점을 만듭니다. 중복 점을 그대로 두면 두 점 사이 zero-length
        /// Edge가 fan triangulation에 남아 반대편 fragment와 만나야 할 경계 Edge의 quantize 식별이
        /// 어긋납니다(공유 Edge count가 정확히 2가 아니라 3~4로 뒤섞임). 여러 clip 단계를 거치며 이미
        /// 들어간 점과(바로 앞이 아니어도) 겹칠 수 있으므로 전체 목록을 확인합니다. Region 하나의
        /// 점 개수가 많지 않아 비용은 무시할 만합니다.
        /// </summary>
        private static void AppendDistinct(List<Vector2> output, in Vector2 point)
        {
            for (int i = 0; i < output.Count; i++)
            {
                if ((output[i] - point).sqrMagnitude <= BoundaryTolerance * BoundaryTolerance) return;
            }
            output.Add(point);
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

    /// <summary>
    /// 서로 다른 Tile clipping이 같은 intrinsic 교점을 계산하면 최초 좌표 하나로 통일합니다.
    /// quantization은 lookup에만 사용하고 실제 좌표를 반올림하지 않으므로 Grid 전체가 이동하지 않습니다.
    /// </summary>
    internal sealed class SurfaceRegionCanonicalizer
    {
        // 같은 물리적 교점이라도 어느 Patch Triangle에서 계산됐는지에 따라 float32 반올림 오차가
        // 몇 ULP씩 어긋날 수 있습니다(예: 좌표 크기 ~10~20 범위에서 실측 오차 ~1e-6). 1e-6은 이
        // 오차 자체와 크기가 같아 실측 실패가 나서, 정상적인 최소 Tile 반지름(0.025)보다는 훨씬
        // 작으면서 float32 노이즈는 넉넉히 흡수하는 값으로 올렸습니다.
        private const float Tolerance = 0.0001f;
        private readonly Dictionary<(long X, long Y), Vector2> _vertices = new();

        /// <summary>
        /// 같은 물리적 교점도 계산 경로(어느 Patch Triangle에서 clipping됐는지)에 따라 부동소수점
        /// 결과가 미세하게 달라질 수 있고, 그 차이가 quantize 격자 칸 경계를 사이에 두면 자기 칸
        /// 하나만 조회해서는 이미 등록된 같은 점을 놓칩니다. 인접 3x3 칸을 모두 조회하고, 새로
        /// 등록할 때도 3x3 칸 전부에 등록해 어느 방향에서 반올림되어 오더라도 같은 항목을 찾도록
        /// 합니다.
        /// </summary>
        public Vector2 Canonicalize(in Vector2 point)
        {
            long centerX = (long)Math.Round(point.x / Tolerance);
            long centerY = (long)Math.Round(point.y / Tolerance);

            for (long dx = -1; dx <= 1; dx++)
            {
                for (long dy = -1; dy <= 1; dy++)
                {
                    if (_vertices.TryGetValue((centerX + dx, centerY + dy), out Vector2 candidate) &&
                        (candidate - point).sqrMagnitude <= Tolerance * Tolerance)
                    {
                        return candidate;
                    }
                }
            }

            for (long dx = -1; dx <= 1; dx++)
                for (long dy = -1; dy <= 1; dy++)
                    _vertices[(centerX + dx, centerY + dy)] = point;
            return point;
        }
    }
}
