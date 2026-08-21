using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>같은 GameObject의 <see cref="SkinnedMeshRenderer"/>를 Surface로 연결합니다.</summary>
    public sealed class SkinnedMeshSurfaceAdapterFactory : ISurfaceAdapterFactory
    {
        /// <inheritdoc />
        public int Priority => SurfaceAdapterPriority.SkinnedMesh;

        /// <inheritdoc />
        public bool CanCreate(GameObject target) =>
            target != null &&
            target.TryGetComponent(out SkinnedMeshRenderer renderer) &&
            renderer.sharedMesh != null;

        /// <inheritdoc />
        public ISurfaceAdapter Create(GameObject target, SurfaceHandle handle)
        {
            SkinnedMeshRenderer renderer = target.GetComponent<SkinnedMeshRenderer>();
            target.TryGetComponent(out Collider collider);
            return new SkinnedMeshSurfaceAdapter(renderer, collider, handle);
        }
    }
}
