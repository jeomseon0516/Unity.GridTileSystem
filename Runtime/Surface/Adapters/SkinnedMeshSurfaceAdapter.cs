using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>
    /// <see cref="SkinnedMeshRenderer"/>의 bind pose 표면을 Surface Space에 연결합니다. topology는 bind
    /// pose 기준으로 고정되고 변형은 bone 가중치 binding이 따라갑니다.
    /// </summary>
    public sealed class SkinnedMeshSurfaceAdapter : ISurfaceAdapter
    {
        /// <summary>bind pose Mesh와 bone 자세를 제공하는 Renderer입니다.</summary>
        private readonly SkinnedMeshRenderer _renderer;
        /// <summary>마지막으로 구축한 bind pose topology입니다.</summary>
        private SurfaceTopology _topology;

        /// <inheritdoc />
        public SurfaceHandle Handle { get; }
        /// <inheritdoc />
        public SurfaceCapabilities Capabilities =>
            SurfaceCapabilities.Deformable | SurfaceCapabilities.HasSkinning | SurfaceCapabilities.CpuReadable;
        /// <inheritdoc />
        public Transform SurfaceTransform => _renderer != null ? _renderer.transform : null;
        /// <inheritdoc />
        public Collider PickingCollider { get; }
        /// <inheritdoc />
        public Bounds WorldBounds => _renderer != null ? _renderer.bounds : default;

        /// <summary>Renderer와 선택적 Collider로 Adapter를 생성합니다.</summary>
        public SkinnedMeshSurfaceAdapter(SkinnedMeshRenderer renderer, Collider pickingCollider, SurfaceHandle handle)
        {
            _renderer = renderer != null ? renderer : throw new ArgumentNullException(nameof(renderer));
            PickingCollider = pickingCollider;
            Handle = handle;
        }

        /// <summary>bone binding 구축에 필요한 원본 Renderer를 가져옵니다.</summary>
        public SkinnedMeshRenderer Renderer => _renderer;

        /// <inheritdoc />
        public SurfaceTopology BuildTopology()
        {
            if (_renderer == null || _renderer.sharedMesh == null)
                throw new InvalidOperationException("Skinned surface requires a SkinnedMeshRenderer with a shared Mesh.");
            _topology = SkinnedMeshTopologyFactory.BuildTopology(_renderer.sharedMesh, Handle);
            return _topology;
        }

        /// <summary>
        /// bind pose 기준 위치를 반환합니다. 변형된 현재 자세가 아니라는 점에 주의합니다. 변형 추종은
        /// <see cref="SurfaceSkinBinding"/>이 담당하며, 이 메서드는 Grid seed 질의처럼 bind pose 기준
        /// 위치가 필요한 경로에서 사용합니다.
        /// </summary>
        public bool TryEvaluateWorldPosition(in SurfacePoint point, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (_topology == null || _renderer == null) return false;
            if (!point.IsValid || point.Surface != Handle) return false;
            worldPosition = _renderer.transform.TransformPoint(_topology.Evaluate(point));
            return true;
        }

        /// <inheritdoc />
        public void Dispose() => _topology = null;
    }
}
