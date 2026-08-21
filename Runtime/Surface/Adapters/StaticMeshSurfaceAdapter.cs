using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>읽기 가능한 Unity static <see cref="Mesh"/>를 파이프라인 비종속 Surface topology로 변환합니다.</summary>
    public static class StaticMeshSurfaceAdapter
    {
        /// <summary>
        /// 모든 Mesh submesh로부터 소유권이 독립된 topology snapshot을 구축합니다. Runtime Mesh는
        /// Read/Write Enabled여야 하며, 결과가 달라질 수 있는 암묵적 Physics fallback은 사용하지 않습니다.
        /// </summary>
        public static SurfaceTopology BuildTopology(Mesh mesh)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (!mesh.isReadable)
            {
                throw new InvalidOperationException(
                    $"Mesh '{mesh.name}' must have Read/Write Enabled to build runtime surface topology.");
            }

            // Unity 6000.5에서는 obsolete Instance ID 대신 64-bit EntityId raw data를 사용합니다.
            // GetHashCode로 32-bit 축약하면 서로 다른 Mesh가 같은 Surface로 alias될 수 있습니다.
            // Core에는 Unity 타입을 누출하지 않으며 identity 수명은 현재 Unity process에 한정됩니다.
            ulong handleValue = EntityId.ToULong(mesh.GetEntityId());
            if (handleValue == 0UL) handleValue = 1UL;
            return SurfaceTopologyBuilder.Build(new SurfaceHandle(handleValue), mesh.vertices, mesh.triangles);
        }
    }
}
