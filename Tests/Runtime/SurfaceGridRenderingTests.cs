using System.Collections.Generic;
using System.Linq;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

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
            // Outline은 항상 Edge(Line) 쌍이고, 참조하는 index는 실제 vertex 범위 안에 있어야 합니다.
            Assert.That(geometry.OutlineIndices.Count % 2, Is.Zero);
            Assert.That(
                geometry.OutlineIndices.All(index => index >= 0 && index < geometry.Positions.Count),
                Is.True);
        }

        [Test]
        public void GeometryBuilder_TileSpanningBothPlaneTriangles_OutlineFormsClosedLoopsPerTile()
        {
            // CreatePlane()은 대각선 하나로 나뉜 두 Triangle입니다. 반경 4는 20 유닛 평면 기준으로
            // 커서 일부 Tile은 그 대각선(=두 Patch Triangle의 공유 Edge)을 가로지릅니다. 이런 Tile의
            // Region은 Patch Triangle마다 별도 fragment로 clipping되므로, fragment 경계에서 새는
            // 내부 seam이 있으면(과거 버그) 아래 "닫힌 루프" 불변식이 깨집니다.
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(topology, grid);

            for (int tileIndex = 0; tileIndex < grid.Tiles.Count; tileIndex++)
            {
                List<int> tileOutlineIndices = new();
                for (int i = 0; i < geometry.OutlineIndices.Count; i += 2)
                {
                    int a = geometry.OutlineIndices[i];
                    if (geometry.TileIndices[a] != tileIndex) continue;
                    tileOutlineIndices.Add(a);
                    tileOutlineIndices.Add(geometry.OutlineIndices[i + 1]);
                }

                // 단순 닫힌 다각형(육각형)의 경계라면, quantize한 각 꼭짓점 위치는 정확히 두 Edge의
                // 끝점으로만 등장해야 합니다. fragment 사이 내부 seam이 새면 그 seam의 두 끝점이
                // 추가로 한 번씩 더 나타나(degree 3+) 이 불변식이 깨집니다.
                Dictionary<(long, long), int> degree = new();
                for (int i = 0; i < tileOutlineIndices.Count; i++)
                {
                    Vector2 position = geometry.IntrinsicPositions[tileOutlineIndices[i]];
                    (long, long) key = ((long)Mathf.Round(position.x / 0.0001f), (long)Mathf.Round(position.y / 0.0001f));
                    degree.TryGetValue(key, out int count);
                    degree[key] = count + 1;
                }

                Assert.That(degree.Values, Has.All.EqualTo(2),
                    $"Tile {tileIndex} outline is not a simple closed loop (a fragment seam likely leaked in).");
            }
        }

        [Test]
        public void GeometryBuilder_HexesFullyInsideOneTriangle_OutlineKeepsOnlyTheSixHexBoundaryEdgesEach()
        {
            // 하나의 큰 Triangle 안에는 완전히 들어가는 Tile이 여러 개 생길 수 있습니다(이 반경/seed
            // 조합은 실제로 3개입니다 — Grid는 Patch가 덮는 영역 안에서 맞는 만큼 Hex를 채우므로, 이
            // Triangle 하나짜리 topology에서는 Patch 크기 제한이 Tile 개수를 좌우하지 않습니다).
            // 각 Tile은 다른 Patch Triangle과 clipping이 겹치지 않아 fragment 하나(fan 4개=12 index)만
            // 생기므로, Tile마다 fan의 중심 대각선 3개(내부, 공유 Edge)는 제외되고 육각형 자체의 변
            // 6개만 남아야 합니다.
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(42),
                new[] { Vector3.zero, new Vector3(4f, 0f, 0f), new Vector3(0f, 4f, 0f) },
                new[] { 0, 1, 2 });
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.4f, 0.3f, 0.3f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);
            Assert.That(grid.Tiles.Count, Is.GreaterThan(0));

            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(topology, grid);

            for (int tileIndex = 0; tileIndex < grid.Tiles.Count; tileIndex++)
            {
                SurfaceRegion region = grid.Tiles[tileIndex].Region;
                Assert.That(region.Vertices.Count, Is.EqualTo(6), $"tile {tileIndex} should be a single unfragmented hex fan.");
                Assert.That(region.TriangleIndices.Count, Is.EqualTo(12), $"tile {tileIndex} should be a single unfragmented hex fan.");

                HashSet<(int, int)> outlineEdges = new();
                for (int i = 0; i < geometry.OutlineIndices.Count; i += 2)
                {
                    int a = geometry.OutlineIndices[i];
                    int b = geometry.OutlineIndices[i + 1];
                    if (geometry.TileIndices[a] != tileIndex) continue;
                    outlineEdges.Add(a < b ? (a, b) : (b, a));
                }
                // fan triangulation은 pivot 정점에서 나가는 내부 대각선 3개를 만듭니다(4개 Triangle,
                // 9개 서로 다른 Edge 중 3개가 두 Triangle에 공유됨). 정확히 6개만 남아야 그 내부
                // 대각선이 전부 제외되고 육각형 자체의 변만 Outline에 남았다는 뜻입니다.
                Assert.That(outlineEdges.Count, Is.EqualTo(6),
                    $"tile {tileIndex} outline must contain six distinct hex boundary edges.");
            }
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
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Hidden/Jeomseon/Surface Grid Depth Biased"));
            backend.Dispose();
            Assert.That(renderer.sharedMaterial, Is.Null);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void MeshBackend_PreservesUserAssignedMaterial()
        {
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);
            GameObject host = new(nameof(MeshBackend_PreservesUserAssignedMaterial));
            MeshFilter filter = host.AddComponent<MeshFilter>();
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            Material userMaterial = new(Shader.Find("Sprites/Default"));
            renderer.sharedMaterial = userMaterial;
            MeshSurfaceGridRenderBackend backend = new(filter, renderer);

            try
            {
                backend.ApplyGeometry(SurfaceGridGeometryBuilder.Build(topology, grid));
                Assert.That(renderer.sharedMaterial, Is.SameAs(userMaterial));
            }
            finally
            {
                backend.Dispose();
                Object.DestroyImmediate(userMaterial);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MeshBackend_MixedDrawModePerTile_PartitionsFillAndOutlineIntoSeparateSubMeshes()
        {
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);
            Assert.That(grid.Tiles.Count, Is.GreaterThan(1), "This test needs at least two tiles to mix draw modes.");
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(topology, grid);
            GameObject host = new(nameof(MeshBackend_MixedDrawModePerTile_PartitionsFillAndOutlineIntoSeparateSubMeshes));
            MeshFilter filter = host.AddComponent<MeshFilter>();
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            MeshSurfaceGridRenderBackend backend = new(filter, renderer);

            try
            {
                backend.ApplyGeometry(geometry);
                // 타일 0만 Outline, 나머지는 전부 Fill로 요청합니다.
                SurfaceTileVisual[] visuals = new SurfaceTileVisual[grid.Tiles.Count];
                visuals[0] = new SurfaceTileVisual(Color.white, true, SurfaceGridDrawMode.Outline);
                for (int i = 1; i < visuals.Length; i++)
                    visuals[i] = new SurfaceTileVisual(Color.white, true, SurfaceGridDrawMode.Fill);
                backend.ApplyVisuals(visuals);

                int expectedOutlineIndices = 0;
                for (int i = 0; i < geometry.OutlineIndices.Count; i += 2)
                {
                    if (geometry.TileIndices[geometry.OutlineIndices[i]] == 0) expectedOutlineIndices += 2;
                }
                int expectedFillIndices = 0;
                for (int i = 0; i < geometry.TriangleIndices.Count; i += 3)
                {
                    if (geometry.TileIndices[geometry.TriangleIndices[i]] != 0) expectedFillIndices += 3;
                }

                Mesh mesh = filter.sharedMesh;
                Assert.That(mesh.subMeshCount, Is.EqualTo(2));
                Assert.That(mesh.GetIndices(0).Length, Is.EqualTo(expectedFillIndices));
                Assert.That(mesh.GetTopology(0), Is.EqualTo(MeshTopology.Triangles));
                Assert.That(mesh.GetIndices(1).Length, Is.EqualTo(expectedOutlineIndices));
                Assert.That(mesh.GetTopology(1), Is.EqualTo(MeshTopology.Lines));
            }
            finally
            {
                backend.Dispose();
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MeshBackend_NoneDrawMode_ExcludesAllFillAndOutlineIndices()
        {
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(topology, grid);
            GameObject host = new(nameof(MeshBackend_NoneDrawMode_ExcludesAllFillAndOutlineIndices));
            MeshFilter filter = host.AddComponent<MeshFilter>();
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            MeshSurfaceGridRenderBackend backend = new(filter, renderer);

            try
            {
                backend.ApplyGeometry(geometry);
                SurfaceTileVisual[] visuals = Enumerable
                    .Repeat(new SurfaceTileVisual(Color.white, true, SurfaceGridDrawMode.None), grid.Tiles.Count)
                    .ToArray();
                backend.ApplyVisuals(visuals);

                Assert.That(filter.sharedMesh.GetIndices(0), Is.Empty);
            }
            finally
            {
                backend.Dispose();
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void StructuredBufferBackend_OnGraphicsDevice_UploadsGeometryVisualsAndDeformation()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("A real graphics device is required to validate GraphicsBuffer allocation.");
            SurfaceTopology topology = CreatePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 4f, SurfacePatchBuildSettings.Unlimited);
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(topology, grid);
            StructuredBufferSurfaceGridRenderBackend backend = new();

            try
            {
                backend.ApplyGeometry(geometry);
                backend.ApplyVisuals(Enumerable
                    .Repeat(new SurfaceTileVisual(Color.cyan, true), grid.Tiles.Count)
                    .ToArray());
                Assert.DoesNotThrow(() => backend.ApplyDeformation(geometry.Positions, geometry.Normals));
                Assert.That(backend.VertexCount, Is.EqualTo(geometry.Positions.Count));
                Assert.That(backend.IndexCount, Is.EqualTo(geometry.TriangleIndices.Count));
            }
            finally
            {
                backend.Dispose();
            }
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
        public void GeometryBuilder_SurfaceOffset_KeepsSharedFragmentVerticesWatertight()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(43),
                new[]
                {
                    new Vector3(-2f, -2f, 0f), new Vector3(2f, -2f, 0f),
                    new Vector3(-2f, 2f, 0f), new Vector3(2f, 2f, 1.5f)
                },
                new[] { 0, 1, 2, 2, 1, 3 });
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.75f, SurfacePatchBuildSettings.Unlimited);

            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(
                topology, grid, Matrix4x4.identity, 0.3f);

            var sharedVertices = geometry.IntrinsicPositions
                .Select((position, index) => (Position: position, Index: index))
                .GroupBy(item => item.Position)
                .Where(group => group.Count() > 1)
                .ToArray();
            Assert.That(sharedVertices, Is.Not.Empty, "The grid must cross a topology edge for this regression test.");
            foreach (var group in sharedVertices)
            {
                Vector3 sharedPosition = geometry.Positions[group.First().Index];
                Assert.That(group.All(item => geometry.Positions[item.Index] == sharedPosition), Is.True,
                    $"Intrinsic vertex {group.Key} split after applying the normal offset.");
            }
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
