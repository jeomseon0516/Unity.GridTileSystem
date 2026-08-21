using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>readable <see cref="MeshFilter"/> 표면을 Surface Space에 연결합니다.</summary>
    public sealed class MeshSurfaceAdapter : ISurfaceAdapter
    {
        /// <summary>topology와 barycentric binding의 원본 MeshFilter입니다.</summary>
        private readonly MeshFilter _meshFilter;
        /// <summary>마지막으로 구축한 topology이며 월드 좌표 평가에 재사용합니다.</summary>
        private SurfaceTopology _topology;

        /// <inheritdoc />
        public SurfaceHandle Handle { get; }
        /// <inheritdoc />
        public SurfaceCapabilities Capabilities => SurfaceCapabilities.Static | SurfaceCapabilities.CpuReadable;
        /// <inheritdoc />
        public Transform SurfaceTransform => _meshFilter != null ? _meshFilter.transform : null;
        /// <inheritdoc />
        public Collider PickingCollider { get; }
        /// <inheritdoc />
        public Bounds WorldBounds => _meshFilter != null && _meshFilter.sharedMesh != null
            ? TransformBounds(_meshFilter.sharedMesh.bounds, _meshFilter.transform)
            : default;

        /// <summary>MeshFilter와 선택적 Collider로 Adapter를 생성합니다.</summary>
        public MeshSurfaceAdapter(MeshFilter meshFilter, Collider pickingCollider, SurfaceHandle handle)
        {
            _meshFilter = meshFilter != null ? meshFilter : throw new ArgumentNullException(nameof(meshFilter));
            PickingCollider = pickingCollider;
            Handle = handle;
        }

        /// <inheritdoc />
        public SurfaceTopology BuildTopology()
        {
            if (_meshFilter == null || _meshFilter.sharedMesh == null)
                throw new InvalidOperationException("Mesh surface requires a MeshFilter with a shared Mesh.");
            _topology = MeshTopologyFactory.BuildTopology(_meshFilter.sharedMesh, Handle);
            return _topology;
        }

        /// <inheritdoc />
        public bool TryEvaluateWorldPosition(in SurfacePoint point, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (_topology == null || _meshFilter == null) return false;
            if (!point.IsValid || point.Surface != Handle) return false;
            worldPosition = _meshFilter.transform.TransformPoint(_topology.Evaluate(point));
            return true;
        }

        /// <inheritdoc />
        public void Dispose() => _topology = null;

        /// <summary>local 경계 상자의 여덟 꼭짓점을 변환해 회전에도 정확한 월드 AABB를 만듭니다.</summary>
        internal static Bounds TransformBounds(in Bounds localBounds, Transform transform)
        {
            Vector3 center = transform.TransformPoint(localBounds.center);
            Vector3 extents = localBounds.extents;
            Bounds result = new(center, Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = new(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);
                result.Encapsulate(transform.TransformPoint(localBounds.center + offset));
            }
            return result;
        }
    }
}
