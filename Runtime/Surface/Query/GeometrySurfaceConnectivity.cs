using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Query
{
    /// <summary>
    /// boundary Edge의 월드 위치와 법선을 실제 geometry로 대조해 이어지는 Surface를 찾습니다.
    /// Grid가 경계에 도달했을 때만 질의되므로 월드 전체의 연결을 미리 계산하지 않습니다.
    /// </summary>
    /// <remarks>
    /// "가까우면 연결됨"으로 판정하지 않습니다. 두 Edge 끝점이 허용 오차 안에서 일치하고 두 Face의
    /// 법선이 정합해야만 연결로 인정하므로, 스쳐 지나가는 무관한 표면이 Grid에 딸려 들어오지 않습니다.
    /// </remarks>
    public sealed class GeometrySurfaceConnectivity : ISurfaceConnectivity
    {
        /// <summary>handle로 topology를 조회하는 계층입니다.</summary>
        private readonly ISurfaceProvider _surfaces;
        /// <summary>경계 주변에서 새 Surface를 발견하는 계층입니다.</summary>
        private readonly ISurfaceDiscovery _discovery;
        /// <summary>연결을 인정하는 기준입니다.</summary>
        private readonly SurfaceConnectivityOptions _options;
        /// <summary>Surface마다 한 번만 만드는 boundary Edge 색인입니다.</summary>
        private readonly Dictionary<SurfaceHandle, BoundaryEdgeIndex> _boundaryIndices = new();
        /// <summary>이미 답한 Edge 질의 결과입니다. 연결이 없다는 사실도 함께 기억합니다.</summary>
        private readonly Dictionary<(SurfaceHandle Surface, int TriangleIndex, int Edge), SurfaceLink?> _links = new();
        /// <summary>발견 결과를 재사용하는 버퍼입니다.</summary>
        private readonly List<ISurfaceAdapter> _discoveryBuffer = new();
        /// <summary>색인 조회 결과를 재사용하는 버퍼입니다.</summary>
        private readonly List<int> _candidateEdges = new();

        /// <summary>지금까지 확정한 연결의 개수를 가져옵니다.</summary>
        public int CachedQueryCount => _links.Count;

        /// <summary>기본 기준으로 연결 계층을 만듭니다.</summary>
        public GeometrySurfaceConnectivity(ISurfaceProvider surfaces, ISurfaceDiscovery discovery)
            : this(surfaces, discovery, SurfaceConnectivityOptions.Default)
        {
        }

        /// <summary>연결 판정 기준을 지정해 연결 계층을 만듭니다.</summary>
        public GeometrySurfaceConnectivity(
            ISurfaceProvider surfaces,
            ISurfaceDiscovery discovery,
            in SurfaceConnectivityOptions options)
        {
            _surfaces = surfaces ?? throw new ArgumentNullException(nameof(surfaces));
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _options = options;
        }

        /// <inheritdoc />
        public bool TryGetLink(SurfaceHandle surface, int triangleIndex, int edge, out SurfaceLink link)
        {
            link = default;
            if ((uint)edge >= 3u) return false;

            (SurfaceHandle, int, int) key = (surface, triangleIndex, edge);
            if (_links.TryGetValue(key, out SurfaceLink? cached))
            {
                if (!cached.HasValue) return false;
                link = cached.Value;
                return true;
            }

            bool found = Resolve(surface, triangleIndex, edge, out link);
            // 연결이 없다는 결과도 기억해야 같은 경계를 다시 질의할 때 재계산하지 않습니다.
            _links[key] = found ? link : null;
            return found;
        }

        /// <summary>캐시한 색인과 연결 결과를 모두 비웁니다.</summary>
        public void Clear()
        {
            _boundaryIndices.Clear();
            _links.Clear();
        }

        /// <summary>실제 geometry를 대조해 연결을 확정합니다.</summary>
        private bool Resolve(SurfaceHandle surface, int triangleIndex, int edge, out SurfaceLink link)
        {
            link = default;
            if (!_surfaces.TryGetTopology(surface, out SurfaceTopology topology)) return false;
            if ((uint)triangleIndex >= (uint)topology.Triangles.Count) return false;
            if (!TryGetTransform(surface, out Matrix4x4 surfaceToWorld)) return false;
            if (!TryGetWorldEdge(topology, surfaceToWorld, triangleIndex, edge, out BoundaryEdge source)) return false;

            Vector3 midpoint = (source.Start + source.End) * 0.5f;
            float edgeLength = Vector3.Distance(source.Start, source.End);
            // 이어지는 표면의 Collider가 경계에 맞닿아 있으므로 Edge 길이면 충분합니다.
            float searchRadius = edgeLength + _options.PositionTolerance;
            _discovery.Discover(midpoint, searchRadius, _options.LayerMask, _discoveryBuffer);

            float bestError = float.PositiveInfinity;
            bool found = false;
            foreach (ISurfaceAdapter adapter in _discoveryBuffer)
            {
                if (adapter == null || adapter.Handle == surface) continue;
                if (!_surfaces.TryGetTopology(adapter.Handle, out SurfaceTopology candidateTopology)) continue;
                if (!TryGetTransform(adapter.Handle, out Matrix4x4 candidateToWorld)) continue;

                BoundaryEdgeIndex index = GetBoundaryIndex(adapter.Handle, candidateTopology, candidateToWorld);
                index.Collect(midpoint, _candidateEdges);
                foreach (int entry in _candidateEdges)
                {
                    BoundaryEdge target = index.Edges[entry];
                    if (!TryMatch(source, target, out bool reverseParameter, out float error)) continue;
                    if (error >= bestError) continue;

                    bestError = error;
                    link = new SurfaceLink(
                        surface, triangleIndex, edge,
                        adapter.Handle, target.TriangleIndex, target.Edge,
                        reverseParameter);
                    found = true;
                }
            }

            return found;
        }

        /// <summary>두 boundary Edge가 같은 경계를 공유하는지 위치와 법선으로 판정합니다.</summary>
        private bool TryMatch(
            in BoundaryEdge source,
            in BoundaryEdge target,
            out bool reverseParameter,
            out float error)
        {
            reverseParameter = false;
            error = float.PositiveInfinity;

            float forward = Vector3.Distance(source.Start, target.Start) +
                            Vector3.Distance(source.End, target.End);
            float reversed = Vector3.Distance(source.Start, target.End) +
                             Vector3.Distance(source.End, target.Start);
            float tolerance = _options.PositionTolerance * 2f;
            if (forward > tolerance && reversed > tolerance) return false;

            reverseParameter = reversed < forward;
            error = Mathf.Min(forward, reversed);
            // 위치가 맞아도 법선이 어긋나면 연결이 아닙니다. 같은 경계를 공유하지만 서로 반대를
            // 향하는 표면(벽과 그 뒷면)이 Grid에 이어 붙는 것을 막습니다.
            return _options.IsNormalCompatible(source.Normal, target.Normal);
        }

        /// <summary>Surface local 좌표를 월드로 옮기는 행렬을 구합니다.</summary>
        private bool TryGetTransform(SurfaceHandle surface, out Matrix4x4 surfaceToWorld)
        {
            surfaceToWorld = Matrix4x4.identity;
            if (!_discovery.TryGetAdapter(surface, out ISurfaceAdapter adapter)) return false;
            Transform surfaceTransform = adapter != null ? adapter.SurfaceTransform : null;
            // Transform이 없으면 topology가 이미 월드 기준이라는 계약이므로 항등 변환을 씁니다.
            if (surfaceTransform != null) surfaceToWorld = surfaceTransform.localToWorldMatrix;
            return true;
        }

        /// <summary>Surface의 boundary Edge 색인을 만들거나 캐시에서 가져옵니다.</summary>
        private BoundaryEdgeIndex GetBoundaryIndex(
            SurfaceHandle surface,
            SurfaceTopology topology,
            in Matrix4x4 surfaceToWorld)
        {
            if (_boundaryIndices.TryGetValue(surface, out BoundaryEdgeIndex index)) return index;
            index = BoundaryEdgeIndex.Build(topology, surfaceToWorld, _options.PositionTolerance);
            _boundaryIndices[surface] = index;
            return index;
        }

        /// <summary>지정한 Edge가 boundary이면 월드 좌표와 법선을 계산합니다.</summary>
        private static bool TryGetWorldEdge(
            SurfaceTopology topology,
            in Matrix4x4 surfaceToWorld,
            int triangleIndex,
            int edge,
            out BoundaryEdge result)
        {
            result = default;
            if (!topology.IsTriangleTraversable(triangleIndex)) return false;
            if (topology.Adjacency[triangleIndex].GetNeighbor(edge) >= 0) return false;
            return TryBuildEdge(topology, surfaceToWorld, triangleIndex, edge, out result);
        }

        /// <summary>Face의 월드 법선과 지정한 Edge의 월드 끝점을 계산합니다.</summary>
        private static bool TryBuildEdge(
            SurfaceTopology topology,
            in Matrix4x4 surfaceToWorld,
            int triangleIndex,
            int edge,
            out BoundaryEdge result)
        {
            result = default;
            SurfaceTriangle triangle = topology.Triangles[triangleIndex];
            Vector3 a = surfaceToWorld.MultiplyPoint3x4(topology.Positions[triangle.A]);
            Vector3 b = surfaceToWorld.MultiplyPoint3x4(topology.Positions[triangle.B]);
            Vector3 c = surfaceToWorld.MultiplyPoint3x4(topology.Positions[triangle.C]);
            // 법선은 방향 변환이 아니라 월드 정점에서 직접 계산해 비균일 스케일에서도 정확합니다.
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude <= 0f) return false;

            // Edge e는 corner e에서 시작해 corner (e+1)%3에서 끝납니다.
            Vector3 start = edge switch { 0 => a, 1 => b, _ => c };
            Vector3 end = edge switch { 0 => b, 1 => c, _ => a };
            result = new BoundaryEdge(triangleIndex, edge, start, end, normal.normalized);
            return true;
        }

        /// <summary>Surface 하나의 boundary Edge와 그 월드 위치입니다.</summary>
        private readonly struct BoundaryEdge
        {
            /// <summary>이 Edge가 속한 Triangle index입니다.</summary>
            public int TriangleIndex { get; }
            /// <summary>Triangle 안에서의 Edge 번호입니다.</summary>
            public int Edge { get; }
            /// <summary>Edge 시작점의 월드 위치입니다.</summary>
            public Vector3 Start { get; }
            /// <summary>Edge 끝점의 월드 위치입니다.</summary>
            public Vector3 End { get; }
            /// <summary>이 Edge가 속한 Face의 월드 법선입니다.</summary>
            public Vector3 Normal { get; }

            /// <summary>월드 좌표로 표현한 boundary Edge를 만듭니다.</summary>
            public BoundaryEdge(int triangleIndex, int edge, in Vector3 start, in Vector3 end, in Vector3 normal)
            {
                TriangleIndex = triangleIndex;
                Edge = edge;
                Start = start;
                End = end;
                Normal = normal;
            }
        }

        /// <summary>
        /// Surface의 boundary Edge를 한 번만 모아 중점 기준 공간 해시로 보관합니다. Terrain처럼 Face가
        /// 많은 Surface에서도 경계 질의마다 전체를 훑지 않도록 합니다.
        /// </summary>
        private sealed class BoundaryEdgeIndex
        {
            /// <summary>공간 해시 한 칸의 크기입니다.</summary>
            private readonly float _cellSize;
            /// <summary>격자 칸에서 Edge 목록으로 가는 조회입니다.</summary>
            private readonly Dictionary<Vector3Int, List<int>> _cells = new();

            /// <summary>모아 둔 boundary Edge 배열입니다.</summary>
            public BoundaryEdge[] Edges { get; }

            /// <summary>Edge 배열과 칸 크기로 색인을 만듭니다.</summary>
            private BoundaryEdgeIndex(BoundaryEdge[] edges, float cellSize)
            {
                Edges = edges;
                _cellSize = cellSize;
                for (int i = 0; i < edges.Length; i++)
                {
                    Vector3Int cell = ToCell((edges[i].Start + edges[i].End) * 0.5f);
                    if (!_cells.TryGetValue(cell, out List<int> bucket)) _cells[cell] = bucket = new List<int>();
                    bucket.Add(i);
                }
            }

            /// <summary>topology 전체를 한 번 훑어 boundary Edge 색인을 만듭니다.</summary>
            public static BoundaryEdgeIndex Build(
                SurfaceTopology topology,
                in Matrix4x4 surfaceToWorld,
                float positionTolerance)
            {
                List<BoundaryEdge> edges = new();
                float longestEdge = 0f;
                for (int triangleIndex = 0; triangleIndex < topology.Triangles.Count; triangleIndex++)
                {
                    if (!topology.IsTriangleTraversable(triangleIndex)) continue;
                    SurfaceTriangleAdjacency adjacency = topology.Adjacency[triangleIndex];
                    for (int edge = 0; edge < 3; edge++)
                    {
                        if (adjacency.GetNeighbor(edge) >= 0) continue;
                        if (!TryBuildEdge(topology, surfaceToWorld, triangleIndex, edge, out BoundaryEdge boundary)) continue;
                        edges.Add(boundary);
                        longestEdge = Mathf.Max(longestEdge, Vector3.Distance(boundary.Start, boundary.End));
                    }
                }

                // 칸이 가장 긴 Edge보다 작으면 중점이 서로 다른 칸으로 흩어져 3×3×3 이웃 조회로도
                // 대응 Edge를 놓칠 수 있습니다.
                float cellSize = Mathf.Max(longestEdge, positionTolerance * 4f, 0.0001f);
                return new BoundaryEdgeIndex(edges.ToArray(), cellSize);
            }

            /// <summary>
            /// 지정한 위치 주변 칸에 있는 Edge index를 모읍니다. 칸 크기가 가장 긴 Edge 이상이므로
            /// 3×3×3 이웃만 보면 허용 오차 안에서 대응할 수 있는 Edge를 모두 포함합니다.
            /// </summary>
            public void Collect(in Vector3 position, List<int> results)
            {
                results.Clear();
                if (Edges.Length == 0) return;

                Vector3Int center = ToCell(position);
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            Vector3Int cell = new(center.x + x, center.y + y, center.z + z);
                            if (_cells.TryGetValue(cell, out List<int> bucket)) results.AddRange(bucket);
                        }
                    }
                }
            }

            /// <summary>월드 위치를 격자 칸 좌표로 옮깁니다.</summary>
            private Vector3Int ToCell(in Vector3 position) => new(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize),
                Mathf.FloorToInt(position.z / _cellSize));
        }
    }
}
