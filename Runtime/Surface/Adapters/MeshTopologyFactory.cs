using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>읽기 가능한 Unity static <see cref="Mesh"/>를 파이프라인 비종속 Surface topology로 변환합니다.</summary>
    public static class MeshTopologyFactory
    {
        /// <summary>
        /// 모든 Mesh submesh로부터 소유권이 독립된 topology snapshot을 구축합니다. Runtime Mesh는
        /// Read/Write Enabled여야 하며, 결과가 달라질 수 있는 암묵적 Physics fallback은 사용하지 않습니다.
        /// </summary>
        /// <summary>지정한 Surface identity로 topology snapshot을 구축합니다.</summary>
        public static SurfaceTopology BuildTopology(Mesh mesh, SurfaceHandle handle)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (!mesh.isReadable)
            {
                throw new InvalidOperationException(
                    $"Mesh '{mesh.name}' must have Read/Write Enabled to build runtime surface topology.");
            }

            return SurfaceTopologyBuilder.Build(handle, mesh.vertices, mesh.triangles);
        }

        /// <summary>
        /// Mesh asset identity에서 handle을 유도합니다. 같은 Mesh를 공유하는 여러 인스턴스가 같은
        /// handle을 받게 되므로, Registry 등록 경로에서는 handle을 받는 overload를 사용합니다.
        /// </summary>
        public static SurfaceTopology BuildTopology(Mesh mesh)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            // Unity 6000.5에서는 obsolete Instance ID 대신 64-bit EntityId raw data를 사용합니다.
            ulong handleValue = EntityId.ToULong(mesh.GetEntityId());
            if (handleValue == 0UL) handleValue = 1UL;
            return BuildTopology(mesh, new SurfaceHandle(handleValue));
        }
    }
}
