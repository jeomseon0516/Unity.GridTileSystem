using System;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>topology 위에서 임의의 점에 가장 가까운 <see cref="SurfacePoint"/>를 찾습니다.</summary>
    /// <remarks>
    /// Physics raycast가 아니라 실제 geometry로 계산하므로 Collider가 없어도 동작하고, Collider가 원본
    /// Mesh와 다른 경우에도 실제 표면 기준 결과를 냅니다.
    /// </remarks>
    public static class SurfaceClosestPoint
    {
        /// <summary>Surface local 공간의 점에 가장 가까운 표면 위 점을 찾습니다.</summary>
        /// <param name="topology">검색할 topology입니다.</param>
        /// <param name="localPosition">Surface local 공간의 질의 위치입니다.</param>
        /// <param name="point">가장 가까운 표면 위 점입니다.</param>
        /// <param name="squaredDistance">질의 위치와 결과 사이의 제곱 거리입니다.</param>
        public static bool TryFind(
            SurfaceTopology topology,
            in Vector3 localPosition,
            out SurfacePoint point,
            out float squaredDistance)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            point = default;
            squaredDistance = float.PositiveInfinity;
            if (!IsFinite(localPosition)) return false;

            bool found = false;
            for (int triangleIndex = 0; triangleIndex < topology.Triangles.Count; triangleIndex++)
            {
                if (!topology.IsTriangleTraversable(triangleIndex)) continue;

                SurfaceTriangle triangle = topology.Triangles[triangleIndex];
                Vector3 a = topology.Positions[triangle.A];
                Vector3 b = topology.Positions[triangle.B];
                Vector3 c = topology.Positions[triangle.C];
                Vector3 barycentric = ClosestBarycentric(localPosition, a, b, c);
                Vector3 candidate = a * barycentric.x + b * barycentric.y + c * barycentric.z;
                float distance = (candidate - localPosition).sqrMagnitude;
                if (distance >= squaredDistance) continue;

                squaredDistance = distance;
                point = new SurfacePoint(topology.Handle, triangleIndex, barycentric);
                found = true;
            }

            return found;
        }

        /// <summary>
        /// 삼각형 위에서 질의 점에 가장 가까운 지점의 barycentric 좌표를 구합니다.
        /// Ericson의 Real-Time Collision Detection에 실린 voronoi 영역 분류를 따르며, 결과는 항상
        /// 삼각형 내부 또는 경계이므로 <see cref="SurfacePoint"/> 불변식을 만족합니다.
        /// </summary>
        private static Vector3 ClosestBarycentric(in Vector3 p, in Vector3 a, in Vector3 b, in Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            // 정점 A의 바깥 voronoi 영역입니다.
            if (d1 <= 0f && d2 <= 0f) return new Vector3(1f, 0f, 0f);

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            // 정점 B의 바깥 voronoi 영역입니다.
            if (d3 >= 0f && d4 <= d3) return new Vector3(0f, 1f, 0f);

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                // Edge AB 위입니다.
                float v = SafeRatio(d1, d1 - d3);
                return new Vector3(1f - v, v, 0f);
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            // 정점 C의 바깥 voronoi 영역입니다.
            if (d6 >= 0f && d5 <= d6) return new Vector3(0f, 0f, 1f);

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                // Edge AC 위입니다.
                float w = SafeRatio(d2, d2 - d6);
                return new Vector3(1f - w, 0f, w);
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                // Edge BC 위입니다.
                float w = SafeRatio(d4 - d3, d4 - d3 + (d5 - d6));
                return new Vector3(0f, 1f - w, w);
            }

            // 삼각형 내부입니다.
            float denominator = va + vb + vc;
            if (Mathf.Abs(denominator) <= float.Epsilon) return new Vector3(1f, 0f, 0f);
            float vv = vb / denominator;
            float ww = vc / denominator;
            return new Vector3(1f - vv - ww, vv, ww);
        }

        /// <summary>0으로 나누는 것을 막고 결과를 [0,1]로 제한합니다.</summary>
        private static float SafeRatio(float numerator, float denominator) =>
            Mathf.Abs(denominator) <= float.Epsilon ? 0f : Mathf.Clamp01(numerator / denominator);

        /// <summary>좌표가 NaN/Infinity를 포함하지 않는지 검사합니다.</summary>
        private static bool IsFinite(in Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
