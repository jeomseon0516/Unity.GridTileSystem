using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 공유 Edge를 축으로 인접 Triangle을 강체처럼 펼쳐 local intrinsic chart를 구축합니다.
    /// 각각의 Triangle은 원본 Edge 길이 세 개를 모두 보존합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="ISurfaceConnectivity"/>를 함께 주면 Surface boundary에서 멈추지 않고 이어지는 다른
    /// Surface의 Face를 같은 chart에 계속 펼칩니다. 연결을 건너도 같은 코사인 법칙과 두 원 교차
    /// 연산을 그대로 적용하므로 tangent가 자동으로 이어지며 별도의 transport 수학이 필요 없습니다.
    /// </remarks>
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
            return BuildInternal(
                new SingleSurfaceProvider(topology), seed.Surface, seed.TriangleIndex, settings, seed.Barycentric, null);
        }

        /// <summary>
        /// 지정한 Triangle 개수, intrinsic radius와 closure tolerance를 적용해 local chart를 만듭니다.
        /// 제한에 도달한 결과는 실패가 아니라 <see cref="SurfacePatch.WasTruncated"/>로 명시됩니다.
        /// </summary>
        public static SurfacePatch Build(
            SurfaceTopology topology,
            int seedTriangleIndex,
            in SurfacePatchBuildSettings settings)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            return BuildInternal(
                new SingleSurfaceProvider(topology), topology.Handle, seedTriangleIndex, settings, null, null);
        }

        /// <summary>
        /// Surface를 handle로 조회하는 provider와 선택적 연결 계층으로 chart를 만듭니다. 연결 계층이
        /// 있으면 chart가 seed Surface 경계를 넘어 이어지는 Surface까지 확장됩니다.
        /// </summary>
        public static SurfacePatch Build(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            in SurfacePatchBuildSettings settings,
            ISurfaceConnectivity connectivity)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (!seed.IsValid) throw new ArgumentException("Seed must be a valid surface point.", nameof(seed));
            return BuildInternal(surfaces, seed.Surface, seed.TriangleIndex, settings, seed.Barycentric, connectivity);
        }

        /// <summary>
        /// 이미 다른 Patch에 배정된 Face를 제외하고 하나의 Patch를 펼치며, 성장 제한에서 만난 Face를
        /// 다음 Patch seed 후보로 반환합니다. 자동 분할 계층만 사용하는 내부 계약입니다.
        /// </summary>
        internal static SurfacePatch BuildPartition(
            ISurfaceProvider surfaces,
            in SurfacePatchTriangle seedPlacement,
            in Vector3 seedBarycentric,
            in SurfacePatchBuildSettings settings,
            ISurfaceConnectivity connectivity,
            ISet<(SurfaceHandle Surface, int TriangleIndex)> excluded,
            out List<SurfacePatchTriangle> frontier) =>
            BuildInternal(
                surfaces,
                seedPlacement.Surface,
                seedPlacement.TriangleIndex,
                settings,
                seedBarycentric,
                connectivity,
                excluded,
                seedPlacement,
                out frontier);

        /// <summary>공개 overload의 검증과 radius 원점 정책을 공유하는 실제 펼침 구현입니다.</summary>
        private static SurfacePatch BuildInternal(
            ISurfaceProvider surfaces,
            SurfaceHandle seedSurface,
            int seedTriangleIndex,
            in SurfacePatchBuildSettings settings,
            Vector3? seedBarycentric,
            ISurfaceConnectivity connectivity)
        {
            return BuildInternal(
                surfaces,
                seedSurface,
                seedTriangleIndex,
                settings,
                seedBarycentric,
                connectivity,
                null,
                null,
                out _);
        }

        private static SurfacePatch BuildInternal(
            ISurfaceProvider surfaces,
            SurfaceHandle seedSurface,
            int seedTriangleIndex,
            in SurfacePatchBuildSettings settings,
            Vector3? seedBarycentric,
            ISurfaceConnectivity connectivity,
            ISet<(SurfaceHandle Surface, int TriangleIndex)> excluded,
            SurfacePatchTriangle? seedPlacement,
            out List<SurfacePatchTriangle> frontier)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (!surfaces.TryGetTopology(seedSurface, out SurfaceTopology seedTopology))
                throw new ArgumentException("Seed surface is not available from the provider.", nameof(seedSurface));
            if ((uint)seedTriangleIndex >= (uint)seedTopology.Triangles.Count)
                throw new ArgumentOutOfRangeException(nameof(seedTriangleIndex));
            if (!seedTopology.IsTriangleTraversable(seedTriangleIndex))
                throw new ArgumentException("Seed triangle is not traversable.", nameof(seedTriangleIndex));
            if (excluded != null && excluded.Contains((seedSurface, seedTriangleIndex)))
                throw new ArgumentException("Seed triangle is already assigned to another patch.", nameof(seedTriangleIndex));

            frontier = new List<SurfacePatchTriangle>();
            HashSet<(SurfaceHandle Surface, int TriangleIndex)> frontierKeys = new();
            Dictionary<(SurfaceHandle Surface, int TriangleIndex), SurfacePatchTriangle> unfolded = new();
            List<SurfacePatchTriangle> acceptedTriangles = new();
            Queue<(SurfaceHandle Surface, int TriangleIndex)> pending = new();
            SurfacePatchTriangle seedTriangle = seedPlacement ?? UnfoldSeed(seedTopology, seedTriangleIndex);
            // SurfacePoint overload는 실제 seed를, Triangle index overload는 Face 무게중심을 radius
            // 원점으로 씁니다. chart의 임의 좌표 원점(A corner)에 의존하면 같은 Patch라도 seed 위치에
            // 따라 성장 범위가 비대칭이 되는 잘못된 extrinsic 정책이 됩니다.
            Vector2 radiusOrigin = seedBarycentric.HasValue
                ? seedTriangle.A * seedBarycentric.Value.x +
                  seedTriangle.B * seedBarycentric.Value.y +
                  seedTriangle.C * seedBarycentric.Value.z
                : (seedTriangle.A + seedTriangle.B + seedTriangle.C) / 3f;
            Vector2 seedCentroid = (seedTriangle.A + seedTriangle.B + seedTriangle.C) / 3f;
            seedTriangle = seedTriangle.WithGraphGeodesicDistance(Vector2.Distance(radiusOrigin, seedCentroid));
            unfolded.Add((seedSurface, seedTriangleIndex), seedTriangle);
            acceptedTriangles.Add(seedTriangle);
            pending.Enqueue((seedSurface, seedTriangleIndex));
            float maximumClosureError = 0f;
            bool wasTruncated = false;
            bool closureToleranceExceeded = false;
            int acceptedTriangleCount = 1;

            while (pending.Count > 0)
            {
                (SurfaceHandle currentSurface, int currentIndex) = pending.Dequeue();
                SurfacePatchTriangle current = unfolded[(currentSurface, currentIndex)];
                if (!surfaces.TryGetTopology(currentSurface, out SurfaceTopology currentTopology)) continue;
                SurfaceTriangleAdjacency adjacency = currentTopology.Adjacency[currentIndex];

                for (int edge = 0; edge < 3; edge++)
                {
                    if (!TryUnfoldAcrossEdge(
                            surfaces,
                            connectivity,
                            currentTopology,
                            currentSurface,
                            current,
                            adjacency.GetNeighbor(edge),
                            edge,
                            out SurfaceHandle neighborSurface,
                            out int neighborIndex,
                            out SurfacePatchTriangle candidate))
                    {
                        continue;
                    }

                    (SurfaceHandle, int) neighborKey = (neighborSurface, neighborIndex);
                    if (excluded != null && excluded.Contains(neighborKey)) continue;
                    if (!unfolded.TryGetValue(neighborKey, out SurfacePatchTriangle existing))
                    {
                        Vector2 centroid = (candidate.A + candidate.B + candidate.C) / 3f;
                        Vector2 currentCentroid = (current.A + current.B + current.C) / 3f;
                        float graphDistance = current.GraphGeodesicDistance +
                                              Vector2.Distance(currentCentroid, centroid);
                        if (acceptedTriangleCount >= settings.MaximumTriangleCount ||
                            graphDistance > settings.MaximumIntrinsicRadius)
                        {
                            // Face 중심 graph 거리는 Surface를 가로지르는 순회 경로의 상한입니다. chart
                            // 직선거리만 쓰면 곡률이나 장애물을 가로질러 실제보다 짧게 판정할 수 있습니다.
                            wasTruncated = true;
                            if (frontierKeys.Add(neighborKey))
                            {
                                frontier.Add(candidate);
                            }
                            continue;
                        }

                        candidate = candidate.WithGraphGeodesicDistance(graphDistance);
                        unfolded.Add(neighborKey, candidate);
                        acceptedTriangles.Add(candidate);
                        acceptedTriangleCount++;
                        pending.Enqueue((neighborSurface, neighborIndex));
                        continue;
                    }

                    // Cycle이 있으면 하나의 Face에 여러 펼침 경로로 도달할 수 있습니다. Gaussian 곡률이
                    // 있는 표면에서는 경로별 결과가 일치하지 않을 수 있는데 이를 holonomy라 합니다.
                    // Edge 길이를 바꾸는 평균화를 하지 않고 최초 배치를 유지하며 차이를 오차로 노출합니다.
                    maximumClosureError = Mathf.Max(
                        maximumClosureError,
                        MaximumCornerDistance(existing, candidate));
                    if (maximumClosureError > settings.MaximumClosureError)
                        closureToleranceExceeded = true;
                }
            }

            CalculateMetricDistortion(
                surfaces,
                acceptedTriangles,
                out float maximumMetricDistortion,
                out float averageMetricDistortion);
            float maximumGraphDistance = 0f;
            foreach (SurfacePatchTriangle triangle in acceptedTriangles)
                maximumGraphDistance = Mathf.Max(maximumGraphDistance, triangle.GraphGeodesicDistance);

            return new SurfacePatch(
                seedSurface,
                seedTriangleIndex,
                acceptedTriangles.ToArray(),
                new SurfacePatchDiagnostics(
                    maximumClosureError,
                    wasTruncated,
                    closureToleranceExceeded,
                    maximumGraphDistance,
                    maximumMetricDistortion,
                    averageMetricDistortion));
        }

        /// <summary>Face별 세 Edge의 3D/2D 상대 길이 오차를 집계합니다.</summary>
        private static void CalculateMetricDistortion(
            ISurfaceProvider surfaces,
            IReadOnlyList<SurfacePatchTriangle> triangles,
            out float maximum,
            out float average)
        {
            maximum = 0f;
            double sum = 0d;
            int count = 0;
            foreach (SurfacePatchTriangle patchTriangle in triangles)
            {
                if (!surfaces.TryGetTopology(patchTriangle.Surface, out SurfaceTopology topology)) continue;
                SurfaceTriangle triangle = topology.Triangles[patchTriangle.TriangleIndex];
                for (int edge = 0; edge < 3; edge++)
                {
                    int next = (edge + 1) % 3;
                    float sourceLength = Vector3.Distance(
                        topology.Positions[triangle.GetVertex(edge)],
                        topology.Positions[triangle.GetVertex(next)]);
                    float chartLength = Vector2.Distance(
                        patchTriangle.GetCorner(edge),
                        patchTriangle.GetCorner(next));
                    float relativeError = Mathf.Abs(chartLength - sourceLength) /
                                          Mathf.Max(sourceLength, EdgeLengthEpsilon);
                    maximum = Mathf.Max(maximum, relativeError);
                    sum += relativeError;
                    count++;
                }
            }
            average = count > 0 ? (float)(sum / count) : 0f;
        }

        /// <summary>
        /// 한 Edge 너머의 Face를 chart 좌표로 배치합니다. 같은 Surface의 인접 Face면 adjacency를,
        /// boundary Edge(<paramref name="neighborIndex"/>가 음수)면 연결 계층을 사용합니다.
        /// </summary>
        private static bool TryUnfoldAcrossEdge(
            ISurfaceProvider surfaces,
            ISurfaceConnectivity connectivity,
            SurfaceTopology currentTopology,
            SurfaceHandle currentSurface,
            in SurfacePatchTriangle current,
            int neighborIndex,
            int edge,
            out SurfaceHandle neighborSurface,
            out int resolvedNeighborIndex,
            out SurfacePatchTriangle candidate)
        {
            neighborSurface = default;
            resolvedNeighborIndex = -1;
            candidate = default;

            if (neighborIndex >= 0)
            {
                if (!currentTopology.IsTriangleTraversable(neighborIndex)) return false;
                neighborSurface = currentSurface;
                resolvedNeighborIndex = neighborIndex;
                candidate = UnfoldNeighbor(currentTopology, current, edge, neighborIndex);
                return true;
            }

            // neighborIndex가 음수라는 것이 곧 boundary Edge입니다. 별도 감지 로직이 필요 없습니다.
            if (connectivity == null) return false;
            if (!connectivity.TryGetLink(currentSurface, current.TriangleIndex, edge, out SurfaceLink link)) return false;
            if (!link.IsValid || link.ToSurface == currentSurface && link.ToTriangleIndex == current.TriangleIndex) return false;
            if (!surfaces.TryGetTopology(link.ToSurface, out SurfaceTopology neighborTopology)) return false;
            if ((uint)link.ToTriangleIndex >= (uint)neighborTopology.Triangles.Count) return false;
            if (!neighborTopology.IsTriangleTraversable(link.ToTriangleIndex)) return false;

            neighborSurface = link.ToSurface;
            resolvedNeighborIndex = link.ToTriangleIndex;
            candidate = UnfoldAcrossLink(neighborTopology, current, edge, link);
            return true;
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
                topology.Handle,
                triangleIndex,
                Vector2.zero,
                new Vector2(ab, 0f),
                new Vector2(cx, Mathf.Sqrt(Mathf.Max(0f, cySquared))));
        }

        /// <summary>같은 Surface의 인접 Face를 공유 vertex 대응으로 배치합니다.</summary>
        private static SurfacePatchTriangle UnfoldNeighbor(
            SurfaceTopology topology,
            in SurfacePatchTriangle current,
            int currentEdge,
            int neighborIndex)
        {
            SurfaceTriangle currentTriangle = topology.Triangles[current.TriangleIndex];
            currentTriangle.GetEdge(currentEdge, out int sharedStartVertex, out int sharedEndVertex);
            SurfaceTriangle neighbor = topology.Triangles[neighborIndex];
            return PlaceAcrossEdge(
                topology,
                topology.Handle,
                neighborIndex,
                current,
                currentEdge,
                FindCorner(neighbor, sharedStartVertex),
                FindCorner(neighbor, sharedEndVertex));
        }

        /// <summary>연결 너머 다른 Surface의 Face를 Edge 파라미터 대응으로 배치합니다.</summary>
        private static SurfacePatchTriangle UnfoldAcrossLink(
            SurfaceTopology neighborTopology,
            in SurfacePatchTriangle current,
            int currentEdge,
            in SurfaceLink link)
        {
            // Edge e는 corner e에서 시작해 corner (e+1)%3에서 끝납니다.
            int toStartCorner = link.ToEdge;
            int toEndCorner = (link.ToEdge + 1) % 3;
            // ReverseParameter면 출발 Edge의 시작점이 도착 Edge의 끝점과 같은 위치입니다.
            return PlaceAcrossEdge(
                neighborTopology,
                link.ToSurface,
                link.ToTriangleIndex,
                current,
                currentEdge,
                link.ReverseParameter ? toEndCorner : toStartCorner,
                link.ReverseParameter ? toStartCorner : toEndCorner);
        }

        /// <summary>
        /// 이미 펼쳐진 공유 Edge 양 끝을 중심으로 하는 두 원의 교점으로 다음 Face를 배치합니다.
        /// 두 교점 중 현재 Face의 반대편에 있는 해를 선택합니다. 같은 Surface든 연결 너머든 연산은
        /// 동일하며, 어느 corner가 Edge 양 끝에 대응하는지만 호출자가 정합니다.
        /// </summary>
        private static SurfacePatchTriangle PlaceAcrossEdge(
            SurfaceTopology neighborTopology,
            SurfaceHandle neighborSurface,
            int neighborIndex,
            in SurfacePatchTriangle current,
            int currentEdge,
            int neighborStartCorner,
            int neighborEndCorner)
        {
            if (neighborStartCorner == neighborEndCorner) throw DegenerateTriangle(neighborIndex);

            // Edge e의 양 끝은 corner e와 (e+1)%3이므로 현재 Face의 corner는 조회 없이 결정됩니다.
            int currentStartCorner = currentEdge;
            int currentEndCorner = (currentEdge + 1) % 3;
            int currentOppositeCorner = 3 - currentStartCorner - currentEndCorner;
            Vector2 u2 = current.GetCorner(currentStartCorner);
            Vector2 v2 = current.GetCorner(currentEndCorner);
            Vector2 currentOpposite = current.GetCorner(currentOppositeCorner);

            int neighborOppositeCorner = 3 - neighborStartCorner - neighborEndCorner;
            SurfaceTriangle neighbor = neighborTopology.Triangles[neighborIndex];
            Vector3 startPosition = neighborTopology.Positions[neighbor.GetVertex(neighborStartCorner)];
            Vector3 endPosition = neighborTopology.Positions[neighbor.GetVertex(neighborEndCorner)];
            Vector3 oppositePosition = neighborTopology.Positions[neighbor.GetVertex(neighborOppositeCorner)];
            float radiusU = Vector3.Distance(startPosition, oppositePosition);
            float radiusV = Vector3.Distance(endPosition, oppositePosition);

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
            return new SurfacePatchTriangle(neighborSurface, neighborIndex, corners[0], corners[1], corners[2]);
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
