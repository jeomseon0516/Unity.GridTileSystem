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
        public void Build_GridRadiusOneOnLargePlane_CreatesSevenLogicalTiles()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, 1, SurfacePatchBuildSettings.Unlimited);

            Assert.That(grid.Tiles.Count, Is.EqualTo(7));
            Assert.That(grid.Tiles.Select(tile => tile.Coordinates).Distinct().Count(), Is.EqualTo(7));
            Assert.That(grid.Tiles.All(tile => tile.Region.TriangleIndices.Count > 0), Is.True);
            Assert.That(grid.Tiles.SelectMany(tile => tile.Region.Vertices)
                .All(vertex => vertex.SurfacePoint.IsValid), Is.True);
        }

        [Test]
        public void Build_GridRadiusZero_CentersTileAtSeedPoint()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, 0, SurfacePatchBuildSettings.Unlimited);
            SurfaceGridTileRegion tile = grid.Tiles.Single();

            SurfacePatchTriangle seedTriangle = grid.Patch.Triangles.Single(t => t.TriangleIndex == 0);
            Vector2 expected = seedTriangle.A * seed.Barycentric.x +
                               seedTriangle.B * seed.Barycentric.y +
                               seedTriangle.C * seed.Barycentric.z;
            Assert.That(tile.IntrinsicCenter, Is.EqualTo(expected));
            Assert.That(tile.Coordinates, Is.EqualTo(new HexCoordinates(0, 0)));
        }

        [Test]
        public void PatchMapper_SeedPoint_MapsToGridOriginAndTileLookup()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, 1, SurfacePatchBuildSettings.Unlimited);

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
                topology, seed, 0.5f, 1, SurfacePatchBuildSettings.Unlimited);
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
