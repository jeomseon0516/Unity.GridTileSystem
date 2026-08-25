using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>Surface Grid Region을 파이프라인 비종속 연속 vertex/index 배열로 변환합니다.</summary>
        public static class SurfaceGridGeometryBuilder
    {
        /// <summary>역행렬 계산에서 0으로 취급할 determinant 절댓값 한계입니다.</summary>
        private const float SingularDeterminantTolerance = 0.000000000001f;
        /// <summary>모든 Tile Region을 하나의 CPU Geometry snapshot으로 병합합니다.</summary>
        public static SurfaceGridGeometry Build(SurfaceTopology topology, SurfaceGrid grid)
            => Build(topology, grid, Matrix4x4.identity);

        /// <summary>Surface local Geometry를 지정한 대상 local 공간으로 변환하여 snapshot을 만듭니다.</summary>
        public static SurfaceGridGeometry Build(
            SurfaceTopology topology,
            SurfaceGrid grid,
            in Matrix4x4 surfaceToTarget)
            => Build(topology, grid, surfaceToTarget, 0f);

        /// <summary>대상 공간 변환과 법선 방향 offset을 적용하여 Geometry snapshot을 만듭니다.</summary>
        public static SurfaceGridGeometry Build(
            SurfaceTopology topology,
            SurfaceGrid grid,
            in Matrix4x4 surfaceToTarget,
            float surfaceOffset)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (grid.Patch.Surface != topology.Handle)
                throw new ArgumentException("Grid belongs to another surface topology.", nameof(grid));
            if (grid.SpansMultipleSurfaces)
            {
                // 여러 Surface에 걸친 chart는 Surface마다 다른 변환을 가지므로 provider와 변환 조회를
                // 함께 받는 overload를 써야 합니다. 조용히 잘못된 Geometry를 만들지 않습니다.
                throw new ArgumentException(
                    "Grid spans multiple surfaces; use the overload that takes a provider and a transform source.",
                    nameof(grid));
            }
            if (surfaceOffset < 0f || float.IsNaN(surfaceOffset) || float.IsInfinity(surfaceOffset))
                throw new ArgumentOutOfRangeException(nameof(surfaceOffset));
            if (!IsFinite(surfaceToTarget))
                throw new ArgumentException("Surface transform must contain only finite values.", nameof(surfaceToTarget));
            if (Mathf.Abs(surfaceToTarget.determinant) <= SingularDeterminantTolerance)
            {
                throw new ArgumentException(
                    "Surface transform must be invertible so normals can use its inverse transpose.",
                    nameof(surfaceToTarget));
            }

            List<Vector3> positions = new();
            List<Vector3> normals = new();
            List<Vector2> intrinsicPositions = new();
            List<int> tileIndices = new();
            List<int> triangleIndices = new();
            List<int> outlineIndices = new();
            List<SurfacePoint> surfacePoints = new();
            Dictionary<Vector2, Vector3> sharedPositions = new();
            // 행렬 역산은 vertex마다 반복하기 비싸므로 snapshot 구축당 한 번만 계산합니다.
            Matrix4x4 normalMatrix = surfaceToTarget.inverse.transpose;

            for (int tileIndex = 0; tileIndex < grid.Tiles.Count; tileIndex++)
            {
                SurfaceRegion region = grid.Tiles[tileIndex].Region;
                int vertexOffset = positions.Count;
                foreach (SurfaceRegionVertex vertex in region.Vertices)
                {
                    Vector3 sourceNormal = CalculateFaceNormal(topology, vertex.SurfacePoint.TriangleIndex);
                    // 비균일 Scale에서도 법선이 접평면에 수직으로 남으려면 위치 행렬이 아니라
                    // inverse-transpose 행렬로 변환해야 합니다. 방향이므로 translation은 적용하지 않습니다.
                    Vector3 targetNormal = normalMatrix.MultiplyVector(sourceNormal);
                    targetNormal = targetNormal.sqrMagnitude > 0f ? targetNormal.normalized : Vector3.zero;
                    Vector3 targetPosition = surfaceToTarget.MultiplyPoint3x4(topology.Evaluate(vertex.SurfacePoint)) +
                                             targetNormal * surfaceOffset;
                    positions.Add(ResolveSharedPosition(sharedPositions, vertex.IntrinsicPosition, targetPosition));
                    normals.Add(targetNormal);
                    intrinsicPositions.Add(vertex.IntrinsicPosition);
                    tileIndices.Add(tileIndex);
                    // 변형 추종 경로가 Geometry를 다시 만들지 않고 위치만 재평가할 수 있도록
                    // 원본 Surface binding을 vertex 순서 그대로 보존합니다.
                    surfacePoints.Add(vertex.SurfacePoint);
                }

                foreach (int sourceIndex in region.TriangleIndices)
                {
                    triangleIndices.Add(vertexOffset + sourceIndex);
                }
                AppendOutlineEdges(region.Vertices, region.TriangleIndices, vertexOffset, outlineIndices);
            }

            return new SurfaceGridGeometry(
                positions.ToArray(),
                normals.ToArray(),
                intrinsicPositions.ToArray(),
                tileIndices.ToArray(),
                triangleIndices.ToArray(),
                outlineIndices.ToArray(),
                surfacePoints.ToArray());
        }

        /// <summary>
        /// 여러 Surface에 걸친 Grid의 Geometry를 만듭니다. vertex마다 자기 Surface의 topology로 위치를
        /// 평가하고 그 Surface의 local-to-world 변환을 거쳐 공통 월드 공간으로 모은 뒤, 마지막에
        /// <paramref name="worldToTarget"/>으로 출력 공간으로 옮깁니다.
        /// </summary>
        public static SurfaceGridGeometry Build(
            ISurfaceProvider surfaces,
            ISurfaceTransformSource transforms,
            SurfaceGrid grid,
            in Matrix4x4 worldToTarget,
            float surfaceOffset)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (transforms == null) throw new ArgumentNullException(nameof(transforms));
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (surfaceOffset < 0f || float.IsNaN(surfaceOffset) || float.IsInfinity(surfaceOffset))
                throw new ArgumentOutOfRangeException(nameof(surfaceOffset));
            if (!IsFinite(worldToTarget))
                throw new ArgumentException("Target transform must contain only finite values.", nameof(worldToTarget));
            if (Mathf.Abs(worldToTarget.determinant) <= SingularDeterminantTolerance)
            {
                throw new ArgumentException(
                    "Target transform must be invertible so normals can use its inverse transpose.",
                    nameof(worldToTarget));
            }

            List<Vector3> positions = new();
            List<Vector3> normals = new();
            List<Vector2> intrinsicPositions = new();
            List<int> tileIndices = new();
            List<int> triangleIndices = new();
            List<int> outlineIndices = new();
            List<SurfacePoint> surfacePoints = new();
            Dictionary<Vector2, Vector3> sharedPositions = new();
            // Surface별 위치·법선 변환은 vertex마다 역산하기 비싸므로 처음 만났을 때 한 번만 계산합니다.
            Dictionary<SurfaceHandle, (SurfaceTopology Topology, Matrix4x4 ToTarget, Matrix4x4 Normal)> resolved = new();

            for (int tileIndex = 0; tileIndex < grid.Tiles.Count; tileIndex++)
            {
                SurfaceRegion region = grid.Tiles[tileIndex].Region;
                int vertexOffset = positions.Count;
                foreach (SurfaceRegionVertex vertex in region.Vertices)
                {
                    SurfaceHandle surface = vertex.SurfacePoint.Surface;
                    if (!resolved.TryGetValue(surface, out var entry))
                    {
                        if (!surfaces.TryGetTopology(surface, out SurfaceTopology surfaceTopology))
                            throw new ArgumentException($"Surface {surface} is not available from the provider.", nameof(surfaces));
                        if (!transforms.TryGetSurfaceToWorld(surface, out Matrix4x4 surfaceToWorld))
                            throw new ArgumentException($"Surface {surface} has no world transform.", nameof(transforms));
                        Matrix4x4 toTarget = worldToTarget * surfaceToWorld;
                        entry = (surfaceTopology, toTarget, toTarget.inverse.transpose);
                        resolved.Add(surface, entry);
                    }

                    Vector3 sourceNormal = CalculateFaceNormal(entry.Topology, vertex.SurfacePoint.TriangleIndex);
                    Vector3 targetNormal = entry.Normal.MultiplyVector(sourceNormal);
                    targetNormal = targetNormal.sqrMagnitude > 0f ? targetNormal.normalized : Vector3.zero;
                    Vector3 targetPosition = entry.ToTarget.MultiplyPoint3x4(entry.Topology.Evaluate(vertex.SurfacePoint)) +
                                             targetNormal * surfaceOffset;
                    positions.Add(ResolveSharedPosition(sharedPositions, vertex.IntrinsicPosition, targetPosition));
                    normals.Add(targetNormal);
                    intrinsicPositions.Add(vertex.IntrinsicPosition);
                    tileIndices.Add(tileIndex);
                    surfacePoints.Add(vertex.SurfacePoint);
                }

                foreach (int sourceIndex in region.TriangleIndices) triangleIndices.Add(vertexOffset + sourceIndex);
                AppendOutlineEdges(region.Vertices, region.TriangleIndices, vertexOffset, outlineIndices);
            }

            return new SurfaceGridGeometry(
                positions.ToArray(),
                normals.ToArray(),
                intrinsicPositions.ToArray(),
                tileIndices.ToArray(),
                triangleIndices.ToArray(),
                outlineIndices.ToArray(),
                surfacePoints.ToArray());
        }

        /// <summary>Edge 등장 횟수 quantize에 쓰는 허용 오차입니다. Region canonicalizer와 동일합니다.</summary>
        private const float OutlineEdgeQuantizeTolerance = 0.0001f;

        /// <summary>
        /// 한 Tile의 삼각화 결과에서 정확히 한 Triangle만 참조하는 Edge(=Tile 바깥 경계)만 골라 Line
        /// index로 추가합니다. clipping은 겹치는 Patch Triangle마다 별도 fragment를 만들고 각
        /// fragment가 자기 vertex를 새로 발급하므로(index를 공유하지 않음), fragment 사이 공유 Edge를
        /// index로 비교하면 서로 다른 Edge로 오인해 그대로 남습니다. 그래서 vertex index가 아니라
        /// intrinsic 2D 위치를 quantize한 값으로 Edge를 식별해야 fragment 경계도 올바르게 걸러집니다.
        /// </summary>
        private static void AppendOutlineEdges(
            IReadOnlyList<SurfaceRegionVertex> regionVertices,
            IReadOnlyList<int> regionTriangleIndices,
            int vertexOffset,
            List<int> outlineIndices)
        {
            Dictionary<((long, long) Low, (long, long) High), (int A, int B, int Count)> edges = new();
            for (int i = 0; i < regionTriangleIndices.Count; i += 3)
            {
                CountEdge(edges, regionVertices, regionTriangleIndices[i], regionTriangleIndices[i + 1]);
                CountEdge(edges, regionVertices, regionTriangleIndices[i + 1], regionTriangleIndices[i + 2]);
                CountEdge(edges, regionVertices, regionTriangleIndices[i + 2], regionTriangleIndices[i]);
            }

            foreach ((int A, int B, int Count) edge in edges.Values)
            {
                if (edge.Count != 1) continue;
                outlineIndices.Add(vertexOffset + edge.A);
                outlineIndices.Add(vertexOffset + edge.B);
            }
        }

        /// <summary>두 local vertex의 quantize된 위치로 방향 없는 Edge key를 만들어 등장 횟수를 누적합니다.</summary>
        private static void CountEdge(
            Dictionary<((long, long) Low, (long, long) High), (int A, int B, int Count)> edges,
            IReadOnlyList<SurfaceRegionVertex> regionVertices,
            int a,
            int b)
        {
            (long, long) positionA = Quantize(regionVertices[a].IntrinsicPosition);
            (long, long) positionB = Quantize(regionVertices[b].IntrinsicPosition);
            bool aIsLow = IsLessOrEqual(positionA, positionB);
            ((long, long) Low, (long, long) High) key = aIsLow ? (positionA, positionB) : (positionB, positionA);

            if (edges.TryGetValue(key, out (int A, int B, int Count) existing))
            {
                edges[key] = (existing.A, existing.B, existing.Count + 1);
                return;
            }
            edges[key] = aIsLow ? (a, b, 1) : (b, a, 1);
        }

        /// <summary>intrinsic 2D 위치를 정수 격자로 반올림해 부동소수점 오차 안에서 같은 점을 같은 key로 묶습니다.</summary>
        private static (long, long) Quantize(in Vector2 point) => (
            (long)Math.Round(point.x / OutlineEdgeQuantizeTolerance),
            (long)Math.Round(point.y / OutlineEdgeQuantizeTolerance));

        /// <summary>두 quantize된 좌표 쌍에 임의의 전순서를 부여해 Edge key의 방향을 정규화합니다.</summary>
        private static bool IsLessOrEqual((long, long) a, (long, long) b) =>
            a.Item1 != b.Item1 ? a.Item1 < b.Item1 : a.Item2 <= b.Item2;

        /// <summary>
        /// clipping fragment가 같은 intrinsic 경계점을 서로 다른 Face binding으로 보존해도 최종 위치는
        /// 최초 평가값 하나로 통일합니다. 특히 법선 offset은 Face마다 방향이 달라 공유 경계를 벌릴 수
        /// 있으므로 offset 적용 뒤의 위치를 공유해야 watertight surface가 유지됩니다.
        /// </summary>
        private static Vector3 ResolveSharedPosition(
            Dictionary<Vector2, Vector3> sharedPositions,
            in Vector2 intrinsicPosition,
            in Vector3 evaluatedPosition)
        {
            if (sharedPositions.TryGetValue(intrinsicPosition, out Vector3 sharedPosition)) return sharedPosition;
            sharedPositions.Add(intrinsicPosition, evaluatedPosition);
            return evaluatedPosition;
        }

        /// <summary>원본 Triangle winding의 외적을 정규화해 Face 법선을 계산합니다.</summary>
        private static Vector3 CalculateFaceNormal(SurfaceTopology topology, int triangleIndex)
        {
            SurfaceTriangle triangle = topology.Triangles[triangleIndex];
            Vector3 a = topology.Positions[triangle.A];
            Vector3 b = topology.Positions[triangle.B];
            Vector3 c = topology.Positions[triangle.C];
            // (B-A)×(C-A)는 winding의 오른손 법칙 방향이며 길이는 면적의 두 배입니다.
            // Topology 진단을 통과한 Face라면 normalize할 수 있고, 결함 입력은 zero vector로 남깁니다.
            Vector3 cross = Vector3.Cross(b - a, c - a);
            return cross.sqrMagnitude > 0f ? cross.normalized : Vector3.zero;
        }

        /// <summary>행렬의 모든 원소가 NaN/Infinity 없이 역행렬 계산에 사용할 수 있는지 검사합니다.</summary>
        private static bool IsFinite(in Matrix4x4 matrix)
        {
            for (int element = 0; element < 16; element++)
            {
                float value = matrix[element];
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            }
            return true;
        }
    }
}
