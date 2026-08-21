using System.Linq;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class SurfaceGridRenderingTests
    {
        [Test]
        public void GeometryBuilder_EvaluatesSurfacePointsAndAssignsTileIndices()
        {
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);

            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(topology, grid);

            Assert.That(geometry.Positions.Count, Is.GreaterThan(0));
            Assert.That(geometry.Positions.Count, Is.EqualTo(geometry.Normals.Count));
            Assert.That(geometry.Positions.Count, Is.EqualTo(geometry.TileIndices.Count));
            Assert.That(geometry.TriangleIndices.Count % 3, Is.Zero);
            Assert.That(geometry.TileIndices.All(index => index >= 0 && index < grid.Tiles.Count), Is.True);
            Assert.That(geometry.Normals.All(normal => normal == Vector3.forward), Is.True);
        }

        [Test]
        public void MeshBackend_AppliesGeometryAndVisualAlphaWithoutPipelineTypes()
        {
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(topology, grid);
            GameObject host = new(nameof(MeshBackend_AppliesGeometryAndVisualAlphaWithoutPipelineTypes));
            MeshFilter filter = host.AddComponent<MeshFilter>();
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            MeshSurfaceGridRenderBackend backend = new(filter, renderer);

            backend.ApplyGeometry(geometry);
            // 시각 상태 배열은 항상 현재 Geometry의 Tile 개수를 모두 덮어야 합니다.
            SurfaceTileVisual[] visuals = Enumerable
                .Repeat(new SurfaceTileVisual(Color.red, false), grid.Tiles.Count)
                .ToArray();
            backend.ApplyVisuals(visuals);

            Assert.That(filter.sharedMesh, Is.Not.Null);
            Assert.That(filter.sharedMesh.vertexCount, Is.EqualTo(geometry.Positions.Count));
            Assert.That(filter.sharedMesh.colors.All(color => color.a == 0f), Is.True);
            backend.Dispose();
            Object.DestroyImmediate(host);
        }

        [Test]
        public void GeometryBuilder_SurfaceOffset_MovesVerticesAlongTransformedNormal()
        {
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);

            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(
                topology, grid, Matrix4x4.identity, 0.25f);

            Assert.That(geometry.Positions.All(position => position.z == 0.25f), Is.True);
        }

        [Test]
        public void GeometryBuilder_RejectsGridFromAnotherSurfaceAndInvalidOffset()
        {
            SurfaceTopology topology = CreatePlane();
            SurfaceTopology anotherTopology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(42),
                topology.Positions.ToArray(),
                new[] { 0, 1, 2, 2, 1, 3 });
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);

            Assert.That(
                () => SurfaceGridGeometryBuilder.Build(anotherTopology, grid),
                Throws.ArgumentException);
            Assert.That(
                () => SurfaceGridGeometryBuilder.Build(topology, grid, Matrix4x4.identity, -0.01f),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(
                () => SurfaceGridGeometryBuilder.Build(topology, grid, Matrix4x4.identity, float.NaN),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());

            Matrix4x4 nonFinite = Matrix4x4.identity;
            nonFinite.m00 = float.NaN;
            Assert.That(
                () => SurfaceGridGeometryBuilder.Build(topology, grid, nonFinite),
                Throws.ArgumentException);
            Assert.That(
                () => SurfaceGridGeometryBuilder.Build(topology, grid, Matrix4x4.Scale(new Vector3(1f, 0f, 1f))),
                Throws.ArgumentException);
        }

        [Test]
        public void Picker_RaycastsOnlyTheConfiguredSurfaceCollider()
        {
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);
            GameObject surface = new(nameof(Picker_RaycastsOnlyTheConfiguredSurfaceCollider));
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            MeshCollider surfaceCollider = surface.AddComponent<MeshCollider>();
            Mesh mesh = new()
            {
                vertices = topology.Positions.ToArray(),
                triangles = new[] { 0, 1, 2, 2, 1, 3 }
            };
            surfaceCollider.sharedMesh = mesh;
            blocker.transform.position = new Vector3(0f, 0f, 1f);
            Physics.SyncTransforms();

            try
            {
                SurfaceGridPicker picker = new(surfaceCollider, topology, grid);
                Ray ray = new(new Vector3(0f, 0f, 2f), Vector3.back);

                Assert.That(picker.TryPick(ray, ~0, out RaycastHit hit, out SurfaceGridTileRegion tile), Is.True);
                Assert.That(hit.collider, Is.SameAs(surfaceCollider));
                Assert.That(tile, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(blocker);
                Object.DestroyImmediate(surface);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Snapshots_DoNotExposeMutableBackingArrays()
        {
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(topology, grid);

            Assert.That(topology.Positions, Is.Not.InstanceOf<Vector3[]>());
            Assert.That(topology.Triangles, Is.Not.InstanceOf<SurfaceTriangle[]>());
            Assert.That(grid.Patch.Triangles, Is.Not.InstanceOf<SurfacePatchTriangle[]>());
            Assert.That(grid.Tiles, Is.Not.InstanceOf<SurfaceGridTileRegion[]>());
            Assert.That(grid.Tiles[0].Region.Vertices, Is.Not.InstanceOf<SurfaceRegionVertex[]>());
            Assert.That(geometry.Positions, Is.Not.InstanceOf<Vector3[]>());
            Assert.That(geometry.TriangleIndices, Is.Not.InstanceOf<int[]>());

            // IReadOnlyList의 런타임 객체를 배열로 다시 cast해 snapshot을 변경하는 우회로가 없어야 합니다.
            Assert.That(
                () => ((System.Collections.Generic.IList<Vector3>)topology.Positions)[0] = Vector3.zero,
                Throws.TypeOf<System.NotSupportedException>());
        }

        private static SurfaceTopology CreatePlane() => SurfaceTopologyBuilder.Build(
            new SurfaceHandle(41),
            new[]
            {
                new Vector3(-10f, -10f, 0f), new Vector3(10f, -10f, 0f),
                new Vector3(-10f, 10f, 0f), new Vector3(10f, 10f, 0f)
            },
            new[] { 0, 1, 2, 2, 1, 3 });
    }
}
