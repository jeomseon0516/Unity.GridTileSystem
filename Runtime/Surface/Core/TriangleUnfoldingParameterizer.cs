using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 공유 Edge를 축으로 인접 Triangle을 강체처럼 펼쳐 local intrinsic chart를 구축합니다.
    /// 각각의 Triangle은 원본 Edge 길이 세 개를 모두 보존합니다.
    /// </summary>
    public static class TriangleUnfoldingParameterizer
    {
        /// <summary>수치적으로 0인 기준선으로 나누는 것을 막는 최소 유효 Edge 길이입니다.</summary>
        private const float EdgeLengthEpsilon = 0.000001f;

        /// <summary>
        /// Seed Triangle의 연결 성분을 하나의 2D chart로 펼칩니다. 면적이 없는 degenerate Triangle은
        /// Edge의 어느 쪽인지 안정적으로 정의할 수 없으므로 예외를 발생시킵니다.
        /// </summary>
        public static SurfacePatch Build(SurfaceTopology topology, int seedTriangleIndex)
            => Build(topology, seedTriangleIndex, SurfacePatchBuildSettings.Unlimited);

        /// <summary>
        /// 정확한 Surface seed를 중심으로 intrinsic radius를 측정하여 local chart를 만듭니다.
        /// </summary>
        public static SurfacePatch Build(
            SurfaceTopology topology,
            in SurfacePoint seed,
            in SurfacePatchBuildSettings settings)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (!seed.IsValid || seed.Surface != topology.Handle)
                throw new ArgumentException("Seed must be a valid point on the supplied topology.", nameof(seed));
            return BuildInternal(topology, seed.TriangleIndex, settings, seed.Barycentric);
        }

        /// <summary>
        /// 지정한 Triangle 개수, intrinsic radius와 closure tolerance를 적용해 local chart를 만듭니다.
        /// 제한에 도달한 결과는 실패가 아니라 <see cref="SurfacePatch.WasTruncated"/>로 명시됩니다.
        /// </summary>
        public static SurfacePatch Build(
            SurfaceTopology topology,
            int seedTriangleIndex,
            in SurfacePatchBuildSettings settings)
            => BuildInternal(topology, seedTriangleIndex, settings, null);

        /// <summary>공개 overload의 검증과 radius 원점 정책을 공유하는 실제 펼침 구현입니다.</summary>
        private static SurfacePatch BuildInternal(
            SurfaceTopology topology,
            int seedTriangleIndex,
            in SurfacePatchBuildSettings settings,
            Vector3? seedBarycentric)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if ((uint)seedTriangleIndex >= (uint)topology.Triangles.Count)
                throw new ArgumentOutOfRangeException(nameof(seedTriangleIndex));

            var unfolded = new SurfacePatchTriangle?[topology.Triangles.Count];
            Queue<int> pending = new();
            SurfacePatchTriangle seedTriangle = UnfoldSeed(topology, seedTriangleIndex);
            unfolded[seedTriangleIndex] = seedTriangle;
            // SurfacePoint overload는 실제 seed를, Triangle index overload는 Face 무게중심을 radius
            // 원점으로 씁니다. chart의 임의 좌표 원점(A corner)에 의존하면 같은 Patch라도 seed 위치에
            // 따라 성장 범위가 비대칭이 되는 잘못된 extrinsic 정책이 됩니다.
            Vector2 radiusOrigin = seedBarycentric.HasValue
                ? seedTriangle.A * seedBarycentric.Value.x +
                  seedTriangle.B * seedBarycentric.Value.y +
                  seedTriangle.C * seedBarycentric.Value.z
                : (seedTriangle.A + seedTriangle.B + seedTriangle.C) / 3f;
            pending.Enqueue(seedTriangleIndex);
            float maximumClosureError = 0f;
            bool wasTruncated = false;
            bool closureToleranceExceeded = false;
            int acceptedTriangleCount = 1;

            while (pending.Count > 0)
            {
                int currentIndex = pending.Dequeue();
                SurfacePatchTriangle current = unfolded[currentIndex].Value;
                SurfaceTriangleAdjacency adjacency = topology.Adjacency[currentIndex];

                for (int edge = 0; edge < 3; edge++)
                {
                    int neighborIndex = adjacency.GetNeighbor(edge);
                    if (neighborIndex < 0) continue;

                    SurfacePatchTriangle candidate = UnfoldNeighbor(topology, current, edge, neighborIndex);
                    if (!unfolded[neighborIndex].HasValue)
                    {
                        Vector2 centroid = (candidate.A + candidate.B + candidate.C) / 3f;
                        if (acceptedTriangleCount >= settings.MaximumTriangleCount ||
                            Vector2.Distance(centroid, radiusOrigin) > settings.MaximumIntrinsicRadius)
                        {
                            // Radius는 정점이 아니라 Triangle 중심으로 판정합니다. 경계 Triangle 전체를
                            // 포함할지는 다음 Region clipping 단계가 결정하므로 Patch 성장 정책은 단순합니다.
                            wasTruncated = true;
                            continue;
                        }

                        unfolded[neighborIndex] = candidate;
                        acceptedTriangleCount++;
                        pending.Enqueue(neighborIndex);
                        continue;
                    }

                    // Cycle이 있으면 하나의 Face에 여러 펼침 경로로 도달할 수 있습니다. Gaussian 곡률이
                    // 있는 표면에서는 경로별 결과가 일치하지 않을 수 있는데 이를 holonomy라 합니다.
                    // Edge 길이를 바꾸는 평균화를 하지 않고 최초 배치를 유지하며 차이를 오차로 노출합니다.
                    maximumClosureError = Mathf.Max(
                        maximumClosureError,
                        MaximumCornerDistance(unfolded[neighborIndex].Value, candidate));
                    if (maximumClosureError > settings.MaximumClosureError)
                        closureToleranceExceeded = true;
                }
            }

            List<SurfacePatchTriangle> result = new();
            for (int i = 0; i < unfolded.Length; i++)
            {
                if (unfolded[i].HasValue) result.Add(unfolded[i].Value);
            }

            return new SurfacePatch(
                topology.Handle,
                seedTriangleIndex,
                result.ToArray(),
                maximumClosureError,
                wasTruncated,
                closureToleranceExceeded);
        }

        /// <summary>코사인 법칙으로 Edge 길이 세 개를 보존하며 Seed Face를 2D에 배치합니다.</summary>
        private static SurfacePatchTriangle UnfoldSeed(SurfaceTopology topology, int triangleIndex)
        {
            SurfaceTriangle triangle = topology.Triangles[triangleIndex];
            Vector3 a3 = topology.Positions[triangle.A];
            Vector3 b3 = topology.Positions[triangle.B];
            Vector3 c3 = topology.Positions[triangle.C];
            float ab = Vector3.Distance(a3, b3);
            float ac = Vector3.Distance(a3, c3);
            float bc = Vector3.Distance(b3, c3);
            if (ab <= EdgeLengthEpsilon) throw DegenerateTriangle(triangleIndex);

            // A를 원점, B를 +X축에 둡니다. |C-A|=ac와 |C-B|=bc인 두 원의 방정식을 빼면
            // C.x를 얻을 수 있고, C.y는 피타고라스 정리로 계산됩니다.
            float cx = (ac * ac + ab * ab - bc * bc) / (2f * ab);
            float cySquared = ac * ac - cx * cx;
            if (cySquared <= EdgeLengthEpsilon * EdgeLengthEpsilon) throw DegenerateTriangle(triangleIndex);

            return new SurfacePatchTriangle(
                triangleIndex,
                Vector2.zero,
                new Vector2(ab, 0f),
                new Vector2(cx, Mathf.Sqrt(Mathf.Max(0f, cySquared))));
        }

        /// <summary>
        /// 이미 펼쳐진 공유 Edge 양 끝을 중심으로 하는 두 원의 교점으로 인접 Face를 배치합니다.
        /// 두 교점 중 현재 Face의 반대편에 있는 해를 선택합니다.
        /// </summary>
        private static SurfacePatchTriangle UnfoldNeighbor(
            SurfaceTopology topology,
            in SurfacePatchTriangle current,
            int currentEdge,
            int neighborIndex)
        {
            SurfaceTriangle currentTriangle = topology.Triangles[current.TriangleIndex];
            currentTriangle.GetEdge(currentEdge, out int sharedStartVertex, out int sharedEndVertex);
            int currentStartCorner = FindCorner(currentTriangle, sharedStartVertex);
            int currentEndCorner = FindCorner(currentTriangle, sharedEndVertex);
            int currentOppositeCorner = 3 - currentStartCorner - currentEndCorner;
            Vector2 u2 = current.GetCorner(currentStartCorner);
            Vector2 v2 = current.GetCorner(currentEndCorner);
            Vector2 currentOpposite = current.GetCorner(currentOppositeCorner);

            SurfaceTriangle neighbor = topology.Triangles[neighborIndex];
            int neighborStartCorner = FindCorner(neighbor, sharedStartVertex);
            int neighborEndCorner = FindCorner(neighbor, sharedEndVertex);
            int neighborOppositeCorner = 3 - neighborStartCorner - neighborEndCorner;
            int oppositeVertex = neighbor.GetVertex(neighborOppositeCorner);
            float radiusU = Vector3.Distance(
                topology.Positions[sharedStartVertex], topology.Positions[oppositeVertex]);
            float radiusV = Vector3.Distance(
                topology.Positions[sharedEndVertex], topology.Positions[oppositeVertex]);

            Vector2 edgeVector = v2 - u2;
            float edgeLength = edgeVector.magnitude;
            if (edgeLength <= EdgeLengthEpsilon) throw DegenerateTriangle(neighborIndex);

            // |W-U|=radiusU, |W-V|=radiusV인 두 원을 교차시킵니다. 두 식을 빼서 공유 Edge
            // 방향 거리 x를 구하고, 수직 거리 h는 피타고라스 정리로 구합니다.
            float x = (radiusU * radiusU - radiusV * radiusV + edgeLength * edgeLength) /
                      (2f * edgeLength);
            float hSquared = radiusU * radiusU - x * x;
            if (hSquared <= EdgeLengthEpsilon * EdgeLengthEpsilon) throw DegenerateTriangle(neighborIndex);
            Vector2 tangent = edgeVector / edgeLength;
            Vector2 perpendicular = new(-tangent.y, tangent.x);
            Vector2 basePoint = u2 + tangent * x;
            float height = Mathf.Sqrt(Mathf.Max(0f, hSquared));
            Vector2 positiveCandidate = basePoint + perpendicular * height;
            Vector2 negativeCandidate = basePoint - perpendicular * height;

            // 부호 있는 2D 외적은 점이 방향성 Edge U→V의 어느 쪽에 있는지 나타냅니다.
            // 펼친 인접 Face는 반대쪽에 있어야 하며, 같은 쪽을 택하면 반사되어 서로 겹칩니다.
            float currentSide = Cross(edgeVector, currentOpposite - u2);
            Vector2 opposite2 = Cross(edgeVector, positiveCandidate - u2) * currentSide < 0f
                ? positiveCandidate
                : negativeCandidate;

            Vector2[] corners = new Vector2[3];
            corners[neighborStartCorner] = u2;
            corners[neighborEndCorner] = v2;
            corners[neighborOppositeCorner] = opposite2;
            return new SurfacePatchTriangle(neighborIndex, corners[0], corners[1], corners[2]);
        }

        /// <summary>원본 vertex index를 포함하는 Triangle local corner를 찾습니다.</summary>
        private static int FindCorner(in SurfaceTriangle triangle, int vertex)
        {
            for (int corner = 0; corner < 3; corner++)
            {
                if (triangle.GetVertex(corner) == vertex) return corner;
            }

            throw new InvalidOperationException("Adjacent triangles do not contain their recorded shared edge.");
        }

        /// <summary>2D 외적의 스칼라 z 성분을 반환합니다.</summary>
        private static float Cross(in Vector2 a, in Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>동일 Face의 두 배치에서 대응 corner 사이의 최대 거리를 측정합니다.</summary>
        private static float MaximumCornerDistance(in SurfacePatchTriangle a, in SurfacePatchTriangle b) =>
            Mathf.Max(Vector2.Distance(a.A, b.A), Vector2.Distance(a.B, b.B), Vector2.Distance(a.C, b.C));

        /// <summary>Face가 유효한 2D Triangle을 정의할 수 없을 때 사용할 예외를 생성합니다.</summary>
        private static InvalidOperationException DegenerateTriangle(int triangleIndex) =>
            new($"Triangle {triangleIndex} is degenerate and cannot be unfolded.");
    }
}
