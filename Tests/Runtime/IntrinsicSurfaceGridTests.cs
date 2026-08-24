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
        public void Layout_RotationZero_MatchesUnrotatedLayout()
        {
            IntrinsicHexLayout plain = new(new Vector2(2f, -3f), 0.75f);
            IntrinsicHexLayout rotated = new(new Vector2(2f, -3f), 0.75f, 0f);

            Assert.That(rotated.Rotation, Is.EqualTo(0f));
            for (int q = -3; q <= 3; q++)
            {
                for (int r = -3; r <= 3; r++)
                {
                    AxialCoordinates axial = new(q, r);
                    // 회전 0에서는 cos=1, sin=0이므로 회전 도입 전과 완전히 같은 값이어야 합니다.
                    Assert.That(rotated.GetCenter(axial), Is.EqualTo(plain.GetCenter(axial)));
                    Assert.That(rotated.GetCorners(axial), Is.EqualTo(plain.GetCorners(axial)));
                }
            }
            Vector2 probe = new(2.4f, -2.1f);
            Assert.That(rotated.GetCoordinates(probe), Is.EqualTo(plain.GetCoordinates(probe)));
        }

        [Test]
        public void Layout_Rotation_RotatesLatticeAboutOrigin()
        {
            Vector2 origin = new(2f, -3f);
            float rotation = Mathf.PI / 7f;
            IntrinsicHexLayout plain = new(origin, 0.75f);
            IntrinsicHexLayout rotated = new(origin, 0.75f, rotation);

            for (int q = -3; q <= 3; q++)
            {
                for (int r = -3; r <= 3; r++)
                {
                    AxialCoordinates axial = new(q, r);
                    Vector2 offset = plain.GetCenter(axial) - origin;
                    Vector2 expected = origin + new Vector2(
                        offset.x * Mathf.Cos(rotation) - offset.y * Mathf.Sin(rotation),
                        offset.x * Mathf.Sin(rotation) + offset.y * Mathf.Cos(rotation));

                    Assert.That(rotated.GetCenter(axial).x, Is.EqualTo(expected.x).Within(0.00001f));
                    Assert.That(rotated.GetCenter(axial).y, Is.EqualTo(expected.y).Within(0.00001f));
                }
            }
        }

        [Test]
        public void Layout_Rotation_RoundTripsAxialRangeAndKeepsHexMetric()
        {
            IntrinsicHexLayout layout = new(new Vector2(-1.5f, 4f), 0.75f, 2.4f);

            for (int q = -3; q <= 3; q++)
            {
                for (int r = -3; r <= 3; r++)
                {
                    AxialCoordinates axial = new(q, r);
                    Assert.That(layout.GetCoordinates(layout.GetCenter(axial)), Is.EqualTo(new HexCoordinates(q, r)));
                }
            }

            // 회전은 등거리 변환이므로 인접 Hex 중심 간격은 sqrt(3)R 그대로여야 합니다.
            Vector2 center = layout.GetCenter(new AxialCoordinates(0, 0));
            Vector2 neighbor = layout.GetCenter(new AxialCoordinates(1, 0));
            Assert.That(
                Vector2.Distance(center, neighbor),
                Is.EqualTo(Mathf.Sqrt(3f) * 0.75f).Within(0.00001f));
            foreach (Vector2 corner in layout.GetCorners(new AxialCoordinates(0, 0)))
            {
                Assert.That(Vector2.Distance(center, corner), Is.EqualTo(0.75f).Within(0.00001f));
            }
        }

        [Test]
        public void Layout_FromDirection_PlacesFirstCornerAlongDirection()
        {
            Vector2 origin = new(1f, 1f);
            Vector2 direction = new(0f, 2f);
            IntrinsicHexLayout layout = IntrinsicHexLayout.FromDirection(origin, 0.5f, direction);

            Vector2 firstCorner = layout.GetCorners(new AxialCoordinates(0, 0))[0];
            Vector2 expected = origin + direction.normalized * 0.5f;

            Assert.That(layout.Rotation, Is.EqualTo(Mathf.PI * 0.5f).Within(0.00001f));
            Assert.That(firstCorner.x, Is.EqualTo(expected.x).Within(0.00001f));
            Assert.That(firstCorner.y, Is.EqualTo(expected.y).Within(0.00001f));
        }

        [Test]
        public void Layout_RejectsNonFiniteRotationAndDegenerateDirection()
        {
            Assert.That(
                () => new IntrinsicHexLayout(Vector2.zero, 1f, float.NaN),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(
                () => new IntrinsicHexLayout(Vector2.zero, 1f, float.PositiveInfinity),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(
                () => IntrinsicHexLayout.FromDirection(Vector2.zero, 1f, Vector2.zero),
                Throws.ArgumentException);
            Assert.That(
                () => IntrinsicHexLayout.FromDirection(Vector2.zero, 1f, new Vector2(float.NaN, 1f)),
                Throws.ArgumentException);
        }

        [Test]
        public void Build_RotationZero_MatchesUnrotatedGrid()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid plain = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);
            SurfaceGrid rotated = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited, 0f);

            Assert.That(rotated.Tiles.Count, Is.EqualTo(plain.Tiles.Count));
            Assert.That(
                rotated.Tiles.Select(tile => tile.Coordinates),
                Is.EqualTo(plain.Tiles.Select(tile => tile.Coordinates)));
            Assert.That(
                rotated.Tiles.Select(tile => tile.IntrinsicCenter),
                Is.EqualTo(plain.Tiles.Select(tile => tile.IntrinsicCenter)));
        }

        [Test]
        public void Build_WithRotation_KeepsOnlyCompleteHexRegions()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited, 0.7f);

            float expectedArea = 3f * Mathf.Sqrt(3f) * 0.5f * 0.5f * 0.5f;
            Assert.That(grid.Tiles.Count, Is.GreaterThan(0));
            foreach (SurfaceGridTileRegion tile in grid.Tiles)
            {
                Assert.That(
                    tile.Region.IntrinsicArea,
                    Is.EqualTo(expectedArea).Within(expectedArea * 0.0001f),
                    $"Tile {tile.Coordinates} was clipped at the surface boundary.");
                // 회전한 격자에서도 Tile 중심이 자기 좌표로 되돌아와야 picking이 성립합니다.
                Assert.That(grid.Layout.GetCoordinates(tile.IntrinsicCenter), Is.EqualTo(tile.Coordinates));
            }
        }

        [Test]
        public void Build_RotatedBySixtyDegrees_ReproducesSameTileCenters()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));

            SurfaceGrid plain = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited);
            SurfaceGrid rotated = SurfaceGridBuilder.Build(
                topology, seed, 0.5f, SurfacePatchBuildSettings.Unlimited, Mathf.PI / 3f);

            // Hex 격자는 원점 기준 60° 회전에 대해 자기 자신으로 사상되므로 좌표 이름만 바뀌고
            // Tile 중심 집합과 개수는 같아야 합니다. 회전 처리에 스케일 오류가 있으면 여기서 깨집니다.
            Assert.That(rotated.Tiles.Count, Is.EqualTo(plain.Tiles.Count));
            foreach (SurfaceGridTileRegion tile in rotated.Tiles)
            {
                Assert.That(
                    plain.Tiles.Any(other => Vector2.Distance(other.IntrinsicCenter, tile.IntrinsicCenter) < 0.0001f),
                    Is.True,
                    $"Rotated tile {tile.Coordinates} has no counterpart in the unrotated grid.");
            }
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
