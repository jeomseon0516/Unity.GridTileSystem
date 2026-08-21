using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>같은 GameObject의 <see cref="Terrain"/>을 Surface로 연결합니다.</summary>
    public sealed class TerrainSurfaceAdapterFactory : ISurfaceAdapterFactory
    {
        /// <inheritdoc />
        public int Priority => SurfaceAdapterPriority.Terrain;

        /// <inheritdoc />
        public bool CanCreate(GameObject target) =>
            target != null &&
            target.TryGetComponent(out Terrain terrain) &&
            terrain.terrainData != null;

        /// <inheritdoc />
        public ISurfaceAdapter Create(GameObject target, SurfaceHandle handle)
        {
            Terrain terrain = target.GetComponent<Terrain>();
            target.TryGetComponent(out TerrainCollider collider);
            return new TerrainSurfaceAdapter(terrain, collider, handle);
        }
    }
}
