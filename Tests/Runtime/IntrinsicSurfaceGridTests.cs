using System.Linq;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class IntrinsicSurfaceGridTests
    {
        [Test]
        public void Layout_CenterToCoordinates_RoundTripsAxialRange()
        {
            IntrinsicHexLayout layout = new(new Vector2(2f, -3f), 0.75f);

            for (int q = -3; q <= 3; q++)
            {
                for (int r = -3; r <= 3; r++)
                {
                    AxialCoordinates axial = new(q, r);
                    Assert.That(layout.GetCoordinates(layout.GetCenter(axial)), Is.EqualTo(new HexCoordinates(q, r)));
                }
            }
        }

        [Test]
        public void Layout_Corners_AreCounterClockwiseAndRadiusFromCenter()
        {
            IntrinsicHexLayout layout = new(Vector2.zero, 2f);
            Vector2[] corners = layout.GetCorners(new AxialCoordinates(0, 0));

            Assert.That(corners, Has.Length.EqualTo(6));
            for (int i = 0; i < corners.Length; i++)
            {
                Assert.That(corners[i].magnitude, Is.EqualTo(2f).Within(0.00001f));
                Vector2 next = corners[(i + 1) % corners.Length];
                Assert.That(CrossZ(corners[i], next), Is.GreaterThan(0f));
            }
        }

        [Test]
        public void Layout_RejectsNonFiniteOriginAndQueryPosition()
        {
            Assert.That(
                () => new IntrinsicHexLayout(new Vector2(float.NaN, 0f), 1f),
                Throws.ArgumentException);
            IntrinsicHexLayout layout = new(Vector2.zero, 1f);
            Assert.That(
                () => layout.GetCoordinates(new Vector2(float.PositiveInfinity, 0f)),
                Throws.ArgumentException);
        }

        [Test]
        public void Build_WithoutGridRadius_KeepsOnlyCompleteHexRegions()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);

            float expectedArea = 3f * Mathf.Sqrt(3f) * 0.5f * 0.5f * 0.5f;
            foreach (SurfaceGridTileRegion tile in grid.Tiles)
            {
                Assert.That(
                    tile.Region.IntrinsicArea,
                    Is.EqualTo(expectedArea).Within(expectedArea * 0.0001f),
                    $"Tile {tile.Coordinates} was clipped at the surface boundary.");
            }
        }

        [Test]
        public void Build_WithoutGridRadius_KeepsCoordinatesUniqueAndRegionsBound()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);

            Assert.That(grid.Tiles.Count, Is.GreaterThan(0));
            Assert.That(
                grid.Tiles.Select(tile => tile.Coordinates).Distinct().Count(),
                Is.EqualTo(grid.Tiles.Count));
            // 완전한 Hex만 남으므로 모든 Region은 유효한 Surface binding과 전체 면적을 가져야 합니다.
            Assert.That(grid.Tiles.All(tile => tile.Region.TriangleIndices.Count > 0), Is.True);
            Assert.That(grid.Tiles.SelectMany(tile => tile.Region.Vertices)
                .All(vertex => vertex.SurfacePoint.IsValid), Is.True);
        }

        [Test]
        public void Build_SmallerTileRadius_ProducesFinerResolution()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid coarse = SurfaceGridBuilder.Build(
                topology, seed, 1f, SurfacePatchBuildSettings.Unlimited);
            SurfaceGrid fine = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);

            // Hex 면적은 반지름의 제곱에 비례하므로 반지름을 절반으로 줄이면 Tile 수는 약 4배가 됩니다.
            // 외곽 여백 때문에 정확히 4배는 아니므로 넉넉한 하한만 검증합니다.
            Assert.That(fine.Tiles.Count, Is.GreaterThan(coarse.Tiles.Count * 3));
        }

        [Test]
        public void Build_PlacesGridOriginTileAtSeedPoint()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);

            SurfacePatchTriangle seedTriangle = grid.Patch.Triangles.Single(t => t.TriangleIndex == 0);
            Vector2 expected = seedTriangle.A * seed.Barycentric.x +
                               seedTriangle.B * seed.Barycentric.y +
                               seedTriangle.C * seed.Barycentric.z;
            Assert.That(grid.TryGetTile(new HexCoordinates(0, 0), out SurfaceGridTileRegion origin), Is.True);
            Assert.That(origin.IntrinsicCenter, Is.EqualTo(expected));
        }

        [Test]
        public void PatchMapper_SeedPoint_MapsToGridOriginAndTileLookup()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);

            bool mapped = SurfacePatchMapper.TryGetIntrinsicPosition(grid.Patch, seed, out Vector2 intrinsic);
            HexCoordinates coordinates = grid.Layout.GetCoordinates(intrinsic);

            Assert.That(mapped, Is.True);
            Assert.That(intrinsic, Is.EqualTo(grid.Layout.Origin));
            Assert.That(coordinates, Is.EqualTo(new HexCoordinates(0, 0)));
            Assert.That(grid.TryGetTile(coordinates, out SurfaceGridTileRegion tile), Is.True);
            Assert.That(tile.Coordinates, Is.EqualTo(coordinates));
        }

        [Test]
        public void PatchMapper_PointFromAnotherSurface_DoesNotAliasSameTriangleIndex()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);
            SurfacePoint foreignPoint = new(new SurfaceHandle(999), 0, seed.Barycentric);

            Assert.That(
                SurfacePatchMapper.TryGetIntrinsicPosition(grid.Patch, foreignPoint, out _),
                Is.False);
        }

        private static SurfaceTopology CreateLargePlane() => SurfaceTopologyBuilder.Build(
            new SurfaceHandle(31),
            new[]
            {
                new Vector3(-10f, -10f, 0f), new Vector3(10f, -10f, 0f),
                new Vector3(-10f, 10f, 0f), new Vector3(10f, 10f, 0f)
            },
            new[] { 0, 1, 2, 2, 1, 3 });

        private static float CrossZ(in Vector2 a, in Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
