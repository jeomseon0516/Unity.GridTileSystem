using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class MeshTopologyFactoryTests
    {
        [Test]
        public void BuildTopology_ReadableMesh_CreatesSurfaceIdentityAndTopology()
        {
            Mesh mesh = new()
            {
                name = nameof(BuildTopology_ReadableMesh_CreatesSurfaceIdentityAndTopology),
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.up },
                triangles = new[] { 0, 1, 2 }
            };

            SurfaceTopology topology = MeshTopologyFactory.BuildTopology(mesh);

            Assert.That(topology.Handle.IsValid, Is.True);
            Assert.That(topology.Positions.Count, Is.EqualTo(3));
            Assert.That(topology.Triangles.Count, Is.EqualTo(1));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BuildTopology_DifferentMeshes_CreateDifferentSurfaceIdentities()
        {
            Mesh first = CreateTriangleMesh();
            Mesh second = CreateTriangleMesh();
            try
            {
                SurfaceTopology firstTopology = MeshTopologyFactory.BuildTopology(first);
                SurfaceTopology secondTopology = MeshTopologyFactory.BuildTopology(second);

                Assert.That(firstTopology.Handle, Is.Not.EqualTo(secondTopology.Handle));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        /// <summary>Surface identity 비교에 사용할 최소 readable Triangle Mesh를 생성합니다.</summary>
        private static Mesh CreateTriangleMesh() => new()
        {
            vertices = new[] { Vector3.zero, Vector3.right, Vector3.up },
            triangles = new[] { 0, 1, 2 }
        };
    }
}
