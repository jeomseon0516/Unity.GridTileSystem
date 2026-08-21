using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>인덱스 Triangle Geometry에서 압축 인접 정보와 진단 결과를 구축합니다.</summary>
    public static class SurfaceTopologyBuilder
    {
        /// <summary>
        /// Triangle을 면적 0으로 간주하는 외적 길이 제곱의 임계값입니다.
        /// 외적의 길이는 Triangle 면적의 두 배이므로 여기에는 제곱된 단위가 사용됩니다.
        /// </summary>
        private const float DegenerateAreaEpsilon = 0.0000000001f;

        /// <summary>
        /// 원본 Geometry를 복사하고 무방향 Edge를 짝지은 뒤 winding/manifold 조건을 검사하고
        /// 연결 성분을 부여합니다. Triangle index 개수는 3의 배수여야 합니다.
        /// </summary>
        public static SurfaceTopology Build(
            SurfaceHandle handle,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<int> indices)
        {
            if (!handle.IsValid) throw new ArgumentException("A valid surface handle is required.", nameof(handle));
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (indices == null) throw new ArgumentNullException(nameof(indices));
            if (indices.Count % 3 != 0) throw new ArgumentException("Triangle index count must be divisible by three.", nameof(indices));

            Vector3[] positionArray = new Vector3[positions.Count];
            for (int i = 0; i < positionArray.Length; i++)
            {
                Vector3 position = positions[i];
                if (!IsFinite(position))
                    throw new ArgumentException($"Position {i} contains NaN or Infinity.", nameof(positions));
                positionArray[i] = position;
            }

            int triangleCount = indices.Count / 3;
            SurfaceTriangle[] triangles = new SurfaceTriangle[triangleCount];
            SurfaceTriangleAdjacency[] adjacency = new SurfaceTriangleAdjacency[triangleCount];
            Array.Fill(adjacency, new SurfaceTriangleAdjacency(-1, -1, -1));
            List<SurfaceTopologyDiagnostic> diagnostics = new();
            Dictionary<EdgeKey, List<EdgeReference>> edges = new();

            // 각 Triangle은 A→B, B→C, C→A 순서의 방향성 Edge 세 개를 제공합니다.
            // EdgeKey는 인접 Face를 찾기 위해 방향을 제거하지만 EdgeReference는 최초 방향을 보존합니다.
            // 따라서 공유 Edge를 같은 방향으로 순회하는 두 Face를 winding 불일치로 진단할 수 있습니다.
            for (int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
            {
                int a = indices[triangleIndex * 3];
                int b = indices[triangleIndex * 3 + 1];
                int c = indices[triangleIndex * 3 + 2];
                ValidateVertexIndex(a, positionArray.Length, indices);
                ValidateVertexIndex(b, positionArray.Length, indices);
                ValidateVertexIndex(c, positionArray.Length, indices);

                SurfaceTriangle triangle = new(a, b, c);
                triangles[triangleIndex] = triangle;
                // |(B-A)×(C-A)|는 Triangle 면적의 두 배입니다. 반복 index도 같은 결과를 만들지만,
                // 원본 Mesh 결함의 의미를 명확하게 유지하기 위해 별도 조건으로 검사합니다.
                bool isDegenerate = a == b || b == c || c == a ||
                    Vector3.Cross(positionArray[b] - positionArray[a], positionArray[c] - positionArray[a]).sqrMagnitude <=
                    DegenerateAreaEpsilon;
                if (isDegenerate)
                {
                    diagnostics.Add(new SurfaceTopologyDiagnostic(
                        SurfaceTopologyDiagnosticKind.DegenerateTriangle, triangleIndex));
                    // 반복 index Edge가 같은 Triangle 내부에서 서로 짝지어지거나 면적 0 Face를 통해
                    // 정상 Face가 연결되지 않도록 degenerate Triangle은 adjacency 후보에서 제외합니다.
                    continue;
                }

                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    triangle.GetEdge(edgeIndex, out int start, out int end);
                    EdgeKey key = new(start, end);
                    if (!edges.TryGetValue(key, out List<EdgeReference> references))
                    {
                        references = new List<EdgeReference>(2);
                        edges.Add(key, references);
                    }
                    references.Add(new EdgeReference(triangleIndex, edgeIndex, start, end));
                }
            }

            // 모든 Face를 수집한 뒤 정확히 두 Face가 공유하는 Edge만 연결합니다. 수집 중 즉시
            // 연결하면 세 번째 Face가 뒤늦게 나타났을 때 앞의 두 Face 연결을 되돌려야 하므로,
            // 2단계 pairing이 non-manifold Edge를 확실한 traversal 경계로 유지합니다.
            foreach (KeyValuePair<EdgeKey, List<EdgeReference>> pair in edges)
            {
                List<EdgeReference> references = pair.Value;
                if (references.Count > 2)
                {
                    for (int i = 2; i < references.Count; i++)
                    {
                        diagnostics.Add(new SurfaceTopologyDiagnostic(
                            SurfaceTopologyDiagnosticKind.NonManifoldEdge,
                            references[i].TriangleIndex,
                            references[0].TriangleIndex,
                            pair.Key.Min,
                            pair.Key.Max));
                    }
                    continue;
                }
                if (references.Count != 2) continue;

                EdgeReference first = references[0];
                EdgeReference second = references[1];
                adjacency[first.TriangleIndex] = adjacency[first.TriangleIndex]
                    .WithNeighbor(first.EdgeIndex, second.TriangleIndex);
                adjacency[second.TriangleIndex] = adjacency[second.TriangleIndex]
                    .WithNeighbor(second.EdgeIndex, first.TriangleIndex);

                // winding이 일관된 인접 Face는 공유 Edge를 서로 반대 방향으로 순회합니다.
                if (first.Start == second.Start && first.End == second.End)
                {
                    diagnostics.Add(new SurfaceTopologyDiagnostic(
                        SurfaceTopologyDiagnosticKind.InconsistentWinding,
                        second.TriangleIndex,
                        first.TriangleIndex,
                        pair.Key.Min,
                        pair.Key.Max));
                }
            }

            int[] componentIds = BuildComponentIds(adjacency, out int componentCount);
            return new SurfaceTopology(
                handle,
                positionArray,
                triangles,
                adjacency,
                diagnostics.ToArray(),
                componentIds,
                componentCount);
        }

        /// <summary>
        /// Triangle 인접 Graph를 너비 우선 탐색(BFS)하여 연결 성분을 부여합니다.
        /// 경계 Edge(-1)에서는 탐색을 중단하고 분리된 Surface island에는 서로 다른 ID를 부여합니다.
        /// </summary>
        private static int[] BuildComponentIds(
            IReadOnlyList<SurfaceTriangleAdjacency> adjacency,
            out int componentCount)
        {
            int[] componentIds = new int[adjacency.Count];
            Array.Fill(componentIds, -1);
            Queue<int> pending = new();
            componentCount = 0;

            for (int start = 0; start < adjacency.Count; start++)
            {
                if (componentIds[start] >= 0) continue;

                componentIds[start] = componentCount;
                pending.Enqueue(start);
                while (pending.Count > 0)
                {
                    int current = pending.Dequeue();
                    SurfaceTriangleAdjacency neighbors = adjacency[current];
                    for (int edge = 0; edge < 3; edge++)
                    {
                        int neighbor = neighbors.GetNeighbor(edge);
                        if (neighbor < 0 || componentIds[neighbor] >= 0) continue;

                        componentIds[neighbor] = componentCount;
                        pending.Enqueue(neighbor);
                    }
                }

                componentCount++;
            }

            return componentIds;
        }

        /// <summary>복사된 원본 position 배열의 범위를 벗어난 index를 거부합니다.</summary>
        private static void ValidateVertexIndex(int index, int vertexCount, IReadOnlyList<int> indices)
        {
            if ((uint)index >= (uint)vertexCount)
                throw new ArgumentException($"Vertex index {index} is outside the position array.", nameof(indices));
        }

        /// <summary>벡터의 모든 성분이 유한한 실수인지 검사합니다.</summary>
        private static bool IsFinite(in Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        /// <summary>(작은 vertex, 큰 vertex) 순서로 정규화한 무방향 Edge key입니다.</summary>
        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            /// <summary>작은 endpoint index를 가져옵니다.</summary>
            public int Min { get; }
            /// <summary>큰 endpoint index를 가져옵니다.</summary>
            public int Max { get; }

            /// <summary>방향과 무관한 Edge key를 생성합니다.</summary>
            public EdgeKey(int a, int b)
            {
                Min = Math.Min(a, b);
                Max = Math.Max(a, b);
            }

            /// <inheritdoc />
            public bool Equals(EdgeKey other) => Min == other.Min && Max == other.Max;
            /// <inheritdoc />
            public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);
            /// <inheritdoc />
            public override int GetHashCode() => HashCode.Combine(Min, Max);
        }

        /// <summary>무방향 Edge를 짝짓는 동안 최초 Face와 방향성 Edge 정보를 보존합니다.</summary>
        private readonly struct EdgeReference
        {
            /// <summary>이 Edge를 최초로 제공한 Triangle index를 가져옵니다.</summary>
            public int TriangleIndex { get; }
            /// <summary>최초 Triangle 내부의 local edge index를 가져옵니다.</summary>
            public int EdgeIndex { get; }
            /// <summary>winding 순서에서 Edge 시작 vertex를 가져옵니다.</summary>
            public int Start { get; }
            /// <summary>winding 순서에서 Edge 끝 vertex를 가져옵니다.</summary>
            public int End { get; }
            /// <summary>최초 Triangle의 Edge 참조를 생성합니다.</summary>
            public EdgeReference(int triangleIndex, int edgeIndex, int start, int end)
            {
                TriangleIndex = triangleIndex;
                EdgeIndex = edgeIndex;
                Start = start;
                End = end;
            }
        }
    }
}
