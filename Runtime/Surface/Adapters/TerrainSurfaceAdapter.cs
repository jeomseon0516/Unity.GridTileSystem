using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>Unity <see cref="Terrain"/> heightfield를 Mesh 복제 없이 Surface Space에 연결합니다.</summary>
    public sealed class TerrainSurfaceAdapter : ISurfaceAdapter
    {
        /// <summary>heightfield와 local transform을 제공하는 Terrain입니다.</summary>
        private readonly Terrain _terrain;
        /// <summary>마지막으로 구축한 계산형 topology입니다.</summary>
        private TerrainSurfaceTopology _topology;

        /// <inheritdoc />
        public SurfaceHandle Handle { get; }
        /// <inheritdoc />
        public SurfaceCapabilities Capabilities =>
            SurfaceCapabilities.Static | SurfaceCapabilities.HeightField | SurfaceCapabilities.CpuReadable;
        /// <inheritdoc />
        public Transform SurfaceTransform => _terrain != null ? _terrain.transform : null;
        /// <inheritdoc />
        public Collider PickingCollider { get; }
        /// <inheritdoc />
        public Bounds WorldBounds
        {
            get
            {
                if (_terrain == null || _terrain.terrainData == null) return default;
                // Terrain local 범위는 중심 기준이 아니라 0..size이므로 중심을 절반만큼 옮깁니다.
                Vector3 size = _terrain.terrainData.size;
                Bounds local = new(size * 0.5f, size);
                return MeshSurfaceAdapter.TransformBounds(local, _terrain.transform);
            }
        }

        /// <summary>Terrain과 선택적 Collider로 Adapter를 생성합니다.</summary>
        public TerrainSurfaceAdapter(Terrain terrain, Collider pickingCollider, SurfaceHandle handle)
        {
            _terrain = terrain != null ? terrain : throw new ArgumentNullException(nameof(terrain));
            PickingCollider = pickingCollider;
            Handle = handle;
        }

        /// <inheritdoc />
        public SurfaceTopology BuildTopology()
        {
            if (_terrain == null || _terrain.terrainData == null)
                throw new InvalidOperationException("Terrain surface requires a Terrain with TerrainData.");
            _topology = TerrainTopologyFactory.BuildTopology(_terrain.terrainData, Handle);
            return _topology;
        }

        /// <inheritdoc />
        public bool TryEvaluateWorldPosition(in SurfacePoint point, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (_topology == null || _terrain == null) return false;
            if (!point.IsValid || point.Surface != Handle) return false;
            worldPosition = _terrain.transform.TransformPoint(_topology.Evaluate(point));
            return true;
        }

        /// <inheritdoc />
        public void Dispose() => _topology = null;
    }
}
