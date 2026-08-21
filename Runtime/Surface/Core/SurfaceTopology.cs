using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// 하나의 Triangle Surface를 나타내는 불변 snapshot입니다. 원본 위치, 방향성 Face,
    /// Face 인접 정보, 연결 성분과 구축 진단을 포함합니다.
    /// </summary>
    public class SurfaceTopology
    {
        /// <summary><see cref="SurfaceTriangle"/>이 index로 참조하는 원본 local vertex 위치입니다.</summary>
        private readonly Vector3[] _positions;
        /// <summary>winding 방향을 보존한 원본 Triangle입니다.</summary>
        private readonly SurfaceTriangle[] _triangles;
        /// <summary>각 Triangle의 방향성 Edge 세 개에 대응하는 인접 Triangle입니다.</summary>
        private readonly SurfaceTriangleAdjacency[] _adjacency;
        /// <summary>이 snapshot을 구축하면서 발견한 topology 결함입니다.</summary>
        private readonly SurfaceTopologyDiagnostic[] _diagnostics;
        /// <summary>각 Triangle에 부여한 연결 성분 식별자입니다.</summary>
        private readonly int[] _componentIds;
        /// <summary>내부 배열을 외부 cast로 변경할 수 없게 봉인한 position view입니다.</summary>
        private readonly IReadOnlyList<Vector3> _positionsView;
        /// <summary>내부 배열을 외부 cast로 변경할 수 없게 봉인한 Triangle view입니다.</summary>
        private readonly IReadOnlyList<SurfaceTriangle> _trianglesView;
        /// <summary>내부 배열을 외부 cast로 변경할 수 없게 봉인한 adjacency view입니다.</summary>
        private readonly IReadOnlyList<SurfaceTriangleAdjacency> _adjacencyView;
        /// <summary>내부 배열을 외부 cast로 변경할 수 없게 봉인한 diagnostic view입니다.</summary>
        private readonly IReadOnlyList<SurfaceTopologyDiagnostic> _diagnosticsView;
        /// <summary>내부 배열을 외부 cast로 변경할 수 없게 봉인한 component view입니다.</summary>
        private readonly IReadOnlyList<int> _componentIdsView;

        /// <summary>이 topology의 모든 <see cref="SurfacePoint"/>가 공유하는 논리적 identity를 가져옵니다.</summary>
        public SurfaceHandle Handle { get; }
        /// <summary>원본 local position을 가져옵니다.</summary>
        public virtual IReadOnlyList<Vector3> Positions => _positionsView;
        /// <summary>방향성 Triangle을 가져옵니다.</summary>
        public virtual IReadOnlyList<SurfaceTriangle> Triangles => _trianglesView;
        /// <summary>Triangle 인접 정보 레코드를 가져옵니다.</summary>
        public virtual IReadOnlyList<SurfaceTriangleAdjacency> Adjacency => _adjacencyView;
        /// <summary>Traversal 전에 소비자가 검사해야 하는 결함을 가져옵니다.</summary>
        public virtual IReadOnlyList<SurfaceTopologyDiagnostic> Diagnostics => _diagnosticsView;
        /// <summary>모든 Triangle의 연결 성분 식별자를 가져옵니다.</summary>
        public virtual IReadOnlyList<int> ComponentIds => _componentIdsView;
        /// <summary>서로 분리된 Triangle 연결 성분의 개수를 가져옵니다.</summary>
        public virtual int ComponentCount { get; }

        /// <summary>계산형 topology가 배열 복사 없이 Surface identity만 초기화합니다.</summary>
        protected SurfaceTopology(SurfaceHandle handle)
        {
            if (!handle.IsValid) throw new ArgumentException("A valid surface handle is required.", nameof(handle));
            Handle = handle;
            _positions = Array.Empty<Vector3>();
            _triangles = Array.Empty<SurfaceTriangle>();
            _adjacency = Array.Empty<SurfaceTriangleAdjacency>();
            _diagnostics = Array.Empty<SurfaceTopologyDiagnostic>();
            _componentIds = Array.Empty<int>();
            _positionsView = Array.AsReadOnly(_positions);
            _trianglesView = Array.AsReadOnly(_triangles);
            _adjacencyView = Array.AsReadOnly(_adjacency);
            _diagnosticsView = Array.AsReadOnly(_diagnostics);
            _componentIdsView = Array.AsReadOnly(_componentIds);
            ComponentCount = 0;
        }

        /// <summary>Builder가 소유한 배열로 검증된 topology snapshot을 생성합니다.</summary>
        internal SurfaceTopology(
            SurfaceHandle handle,
            Vector3[] positions,
            SurfaceTriangle[] triangles,
            SurfaceTriangleAdjacency[] adjacency,
            SurfaceTopologyDiagnostic[] diagnostics,
            int[] componentIds,
            int componentCount)
        {
            Handle = handle;
            _positions = positions ?? throw new ArgumentNullException(nameof(positions));
            _triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
            _adjacency = adjacency ?? throw new ArgumentNullException(nameof(adjacency));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _componentIds = componentIds ?? throw new ArgumentNullException(nameof(componentIds));
            _positionsView = Array.AsReadOnly(_positions);
            _trianglesView = Array.AsReadOnly(_triangles);
            _adjacencyView = Array.AsReadOnly(_adjacency);
            _diagnosticsView = Array.AsReadOnly(_diagnostics);
            _componentIdsView = Array.AsReadOnly(_componentIds);
            ComponentCount = componentCount;
        }

        /// <summary>
        /// intrinsic point를 p = uA + vB + wC로 원본 local 3D 좌표에 평가합니다.
        /// 여기서 (u,v,w)는 각 Triangle vertex에 대응하는 barycentric 가중치입니다.
        /// </summary>
        public virtual Vector3 Evaluate(in SurfacePoint point)
        {
            if (!point.IsValid)
                throw new ArgumentException("Surface point has invalid barycentric coordinates.", nameof(point));
            if (!point.Surface.Equals(Handle))
                throw new ArgumentException("Surface point belongs to another surface.", nameof(point));
            if ((uint)point.TriangleIndex >= (uint)_triangles.Length)
                throw new ArgumentOutOfRangeException(nameof(point));

            SurfaceTriangle triangle = _triangles[point.TriangleIndex];
            return _positions[triangle.A] * point.Barycentric.x +
                   _positions[triangle.B] * point.Barycentric.y +
                   _positions[triangle.C] * point.Barycentric.z;
        }

        /// <summary>지정한 Triangle을 parameterization과 Region 생성에 사용할 수 있는지 반환합니다.</summary>
        public virtual bool IsTriangleTraversable(int triangleIndex) =>
            (uint)triangleIndex < (uint)Triangles.Count;

        /// <summary>Surface local 위치를 topology identity와 barycentric 좌표로 변환할 수 있으면 반환합니다.</summary>
        public virtual bool TryGetSurfacePoint(in Vector3 localPosition, out SurfacePoint point)
        {
            point = default;
            return false;
        }
    }
}
