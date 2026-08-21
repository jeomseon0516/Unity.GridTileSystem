using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>
    /// Terrain heightfield의 정점·Triangle·인접 정보를 index 계산으로 제공하는 virtual topology입니다.
    /// 전체 Terrain Mesh나 position/index 배열을 복제하지 않습니다.
    /// </summary>
    public sealed class TerrainSurfaceTopology : SurfaceTopology
    {
        private static readonly IReadOnlyList<SurfaceTopologyDiagnostic> EmptyDiagnostics =
            Array.AsReadOnly(Array.Empty<SurfaceTopologyDiagnostic>());
        private readonly TerrainData _terrainData;
        private readonly int _resolution;
        private readonly int _cellResolution;
        private readonly IReadOnlyList<Vector3> _positions;
        private readonly IReadOnlyList<SurfaceTriangle> _triangles;
        private readonly IReadOnlyList<SurfaceTriangleAdjacency> _adjacency;
        private readonly IReadOnlyList<int> _componentIds;

        /// <inheritdoc />
        public override IReadOnlyList<Vector3> Positions => _positions;
        /// <inheritdoc />
        public override IReadOnlyList<SurfaceTriangle> Triangles => _triangles;
        /// <inheritdoc />
        public override IReadOnlyList<SurfaceTriangleAdjacency> Adjacency => _adjacency;
        /// <inheritdoc />
        public override IReadOnlyList<SurfaceTopologyDiagnostic> Diagnostics => EmptyDiagnostics;
        /// <inheritdoc />
        public override IReadOnlyList<int> ComponentIds => _componentIds;
        /// <inheritdoc />
        public override int ComponentCount => Triangles.Count == 0 ? 0 : 1;
        /// <summary>원본 Terrain heightmap 한 변의 vertex 개수를 가져옵니다.</summary>
        public int HeightmapResolution => _resolution;

        /// <summary>TerrainData를 복제하지 않는 계산형 topology를 생성합니다.</summary>
        internal TerrainSurfaceTopology(SurfaceHandle handle, TerrainData terrainData) : base(handle)
        {
            _terrainData = terrainData != null ? terrainData : throw new ArgumentNullException(nameof(terrainData));
            _resolution = terrainData.heightmapResolution;
            _cellResolution = _resolution - 1;
            if (_cellResolution <= 0) throw new ArgumentException("Terrain heightmap must contain at least one cell.", nameof(terrainData));
            _positions = new ComputedReadOnlyList<Vector3>(_resolution * _resolution, GetPosition);
            _triangles = new ComputedReadOnlyList<SurfaceTriangle>(_cellResolution * _cellResolution * 2, GetTriangle);
            _adjacency = new ComputedReadOnlyList<SurfaceTriangleAdjacency>(_triangles.Count, GetAdjacency);
            _componentIds = new ComputedReadOnlyList<int>(_triangles.Count, _ => 0);
        }

        /// <inheritdoc />
        public override bool IsTriangleTraversable(int triangleIndex)
        {
            if ((uint)triangleIndex >= (uint)Triangles.Count) return false;
            int cellIndex = triangleIndex / 2;
            int cellX = cellIndex % _cellResolution;
            int cellZ = cellIndex / _cellResolution;
            return !IsHole(cellX, cellZ);
        }

        /// <inheritdoc />
        public override Vector3 Evaluate(in SurfacePoint point)
        {
            if (!point.IsValid) throw new ArgumentException("Surface point has invalid barycentric coordinates.", nameof(point));
            if (point.Surface != Handle) throw new ArgumentException("Surface point belongs to another surface.", nameof(point));
            if (!IsTriangleTraversable(point.TriangleIndex)) throw new ArgumentOutOfRangeException(nameof(point));
            SurfaceTriangle triangle = GetTriangle(point.TriangleIndex);
            return GetPosition(triangle.A) * point.Barycentric.x +
                   GetPosition(triangle.B) * point.Barycentric.y +
                   GetPosition(triangle.C) * point.Barycentric.z;
        }

        /// <inheritdoc />
        public override bool TryGetSurfacePoint(in Vector3 localPosition, out SurfacePoint point)
        {
            point = default;
            Vector3 size = _terrainData.size;
            if (localPosition.x < 0f || localPosition.z < 0f || localPosition.x > size.x || localPosition.z > size.z) return false;
            float gridX = localPosition.x / size.x * _cellResolution;
            float gridZ = localPosition.z / size.z * _cellResolution;
            int cellX = Mathf.Min(_cellResolution - 1, Mathf.FloorToInt(gridX));
            int cellZ = Mathf.Min(_cellResolution - 1, Mathf.FloorToInt(gridZ));
            float fractionX = gridX - cellX;
            float fractionZ = gridZ - cellZ;
            int triangleIndex = (cellZ * _cellResolution + cellX) * 2 + (fractionX + fractionZ <= 1f ? 0 : 1);
            if (!IsTriangleTraversable(triangleIndex)) return false;
            SurfaceTriangle triangle = GetTriangle(triangleIndex);
            Vector3 a = GetPosition(triangle.A);
            Vector3 b = GetPosition(triangle.B);
            Vector3 c = GetPosition(triangle.C);
            Vector3 barycentric = CalculateBarycentric(a, b, c, localPosition);
            point = new SurfacePoint(Handle, triangleIndex, barycentric);
            return point.IsValid;
        }

        /// <summary>heightmap index를 Terrain local position으로 계산합니다.</summary>
        private Vector3 GetPosition(int vertexIndex)
        {
            int x = vertexIndex % _resolution;
            int z = vertexIndex / _resolution;
            Vector3 size = _terrainData.size;
            return new Vector3(
                size.x * x / _cellResolution,
                _terrainData.GetHeight(x, z),
                size.z * z / _cellResolution);
        }

        /// <summary>cell의 두 Triangle을 위쪽 winding으로 계산합니다.</summary>
        private SurfaceTriangle GetTriangle(int triangleIndex)
        {
            int cellIndex = triangleIndex / 2;
            int cellX = cellIndex % _cellResolution;
            int cellZ = cellIndex / _cellResolution;
            int v00 = cellZ * _resolution + cellX;
            int v10 = v00 + 1;
            int v01 = v00 + _resolution;
            int v11 = v01 + 1;
            return (triangleIndex & 1) == 0
                ? new SurfaceTriangle(v00, v01, v10)
                : new SurfaceTriangle(v10, v01, v11);
        }

        /// <summary>규칙적 heightfield의 이웃 Triangle을 배열 없이 계산합니다.</summary>
        private SurfaceTriangleAdjacency GetAdjacency(int triangleIndex)
        {
            if (!IsTriangleTraversable(triangleIndex)) return new SurfaceTriangleAdjacency(-1, -1, -1);
            int cellIndex = triangleIndex / 2;
            int x = cellIndex % _cellResolution;
            int z = cellIndex / _cellResolution;
            int localTriangle = triangleIndex & 1;
            if (localTriangle == 0)
            {
                return new SurfaceTriangleAdjacency(
                    GetNeighbor(x - 1, z, 1),
                    triangleIndex + 1,
                    GetNeighbor(x, z - 1, 1));
            }
            return new SurfaceTriangleAdjacency(
                triangleIndex - 1,
                GetNeighbor(x, z + 1, 0),
                GetNeighbor(x + 1, z, 0));
        }

        /// <summary>cell 범위와 hole을 검사해 이웃 Triangle index 또는 경계 -1을 반환합니다.</summary>
        private int GetNeighbor(int cellX, int cellZ, int localTriangle)
        {
            if ((uint)cellX >= (uint)_cellResolution || (uint)cellZ >= (uint)_cellResolution || IsHole(cellX, cellZ)) return -1;
            return (cellZ * _cellResolution + cellX) * 2 + localTriangle;
        }

        /// <summary>heightfield cell을 대응하는 Terrain hole texel에 매핑합니다.</summary>
        private bool IsHole(int cellX, int cellZ)
        {
            int holesResolution = _terrainData.holesResolution;
            if (holesResolution <= 0) return false;
            int holeX = Mathf.Min(holesResolution - 1, cellX * holesResolution / _cellResolution);
            int holeZ = Mathf.Min(holesResolution - 1, cellZ * holesResolution / _cellResolution);
            return _terrainData.IsHole(holeX, holeZ);
        }

        /// <summary>3D Triangle 평면 위 위치의 barycentric 좌표를 계산합니다.</summary>
        private static Vector3 CalculateBarycentric(in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 position)
        {
            Vector3 v0 = b - a;
            Vector3 v1 = c - a;
            Vector3 v2 = position - a;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denominator = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denominator) <= Mathf.Epsilon) return new Vector3(-1f, -1f, -1f);
            float v = (d11 * d20 - d01 * d21) / denominator;
            float w = (d00 * d21 - d01 * d20) / denominator;
            float u = 1f - v - w;
            const float tolerance = 0.0001f;
            if (u < 0f && u >= -tolerance) u = 0f;
            if (v < 0f && v >= -tolerance) v = 0f;
            if (w < 0f && w >= -tolerance) w = 0f;
            float sum = u + v + w;
            return new Vector3(u / sum, v / sum, w / sum);
        }
    }
}
