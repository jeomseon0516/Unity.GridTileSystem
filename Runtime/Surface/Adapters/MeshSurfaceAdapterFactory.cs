using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>같은 GameObject의 <see cref="MeshFilter"/>를 Surface로 연결합니다.</summary>
    public sealed class MeshSurfaceAdapterFactory : ISurfaceAdapterFactory
    {
        /// <inheritdoc />
        public int Priority => SurfaceAdapterPriority.Mesh;

        /// <inheritdoc />
        public bool CanCreate(GameObject target) =>
            target != null &&
            target.TryGetComponent(out MeshFilter filter) &&
            filter.sharedMesh != null;

        /// <inheritdoc />
        public ISurfaceAdapter Create(GameObject target, SurfaceHandle handle)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            target.TryGetComponent(out Collider collider);
            return new MeshSurfaceAdapter(filter, collider, handle);
        }
    }
}
