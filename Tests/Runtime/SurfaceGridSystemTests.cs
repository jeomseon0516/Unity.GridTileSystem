using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using Jeomseon.Unity.GridTileSystem.Surface.Query;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class SurfaceGridSystemTests
    {
        [Test]
        public void Build_FindsSurfaceFromSeedPositionAlone()
        {
            using SurfaceGridSystem system = CreateSystem(CreateLargePlane(), out _);
            SurfaceGridRequest request = new(Vector3.zero, 0.5f);

            SurfaceGridBuildResult result = system.Build(request);

            // 사용자는 Surface를 등록하지도 지정하지도 않았습니다. seed 위치 하나로 Grid가 나와야 합니다.
            Assert.That(result.IsSuccess, Is.True, result.Diagnostic);
            Assert.That(result.Grid.Tiles.Count, Is.GreaterThan(0));
            Assert.That(result.Seed.IsValid, Is.True);
            Assert.That(result.Topology, Is.Not.Null);
            Assert.That(result.Diagnostic, Is.Null);
        }

        [Test]
        public void Build_WithoutAnySurface_ReportsDiagnosticInsteadOfFailingSilently()
        {
            using SurfaceGridSystem system = new(new EmptySurfaceQuery(), new EmptySurfaceQuery());
            SurfaceGridRequest request = new(new Vector3(0f, 100f, 0f), 0.5f);

            SurfaceGridBuildResult result = system.Build(request);

            Assert.That(result.Status, Is.EqualTo(SurfaceGridBuildStatus.SurfaceNotFound));
            Assert.That(result.Grid, Is.Null);
            Assert.That(result.Diagnostic, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Build_TileRadiusLargerThanSurface_ReportsNoCompleteTiles()
        {
            using SurfaceGridSystem system = CreateSystem(CreateSmallPlane(), out _);
            SurfaceGridRequest request = new(Vector3.zero, 10f);

            SurfaceGridBuildResult result = system.Build(request);

            // 완전한 Hex만 남기는 계약 때문에 Tile이 하나도 없을 수 있습니다. 이것도 조용히 넘기지 않습니다.
            Assert.That(result.Status, Is.EqualTo(SurfaceGridBuildStatus.NoCompleteTiles));
            Assert.That(result.Grid, Is.Not.Null);
            Assert.That(result.Grid.Tiles.Count, Is.EqualTo(0));
            Assert.That(result.Diagnostic, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Build_InitialDirection_OrientsGridWithoutChangingTileMetric()
        {
            SurfaceTopology topology = CreateLargePlane();
            using SurfaceGridSystem plainSystem = CreateSystem(topology, out _);
            using SurfaceGridSystem orientedSystem = CreateSystem(topology, out _);

            SurfaceGridBuildResult plain = plainSystem.Build(new SurfaceGridRequest(Vector3.zero, 0.5f));
            SurfaceGridBuildResult oriented = orientedSystem.Build(new SurfaceGridRequest(
                Vector3.zero,
                0.5f,
                new Vector3(0f, 1f, 0f),
                SurfacePatchBuildSettings.Unlimited,
                SurfaceQueryOptions.Default));

            Assert.That(plain.IsSuccess, Is.True, plain.Diagnostic);
            Assert.That(oriented.IsSuccess, Is.True, oriented.Diagnostic);
            // 평면 topology는 xy 평면에 있으므로 +y 초기 방향은 chart에서 90° 회전이 됩니다.
            Assert.That(plain.Grid.Layout.Rotation, Is.EqualTo(0f));
            Assert.That(oriented.Grid.Layout.Rotation, Is.EqualTo(Mathf.PI * 0.5f).Within(0.0001f));
            // 회전은 등거리 변환이므로 Tile 해상도는 그대로여야 합니다.
            Assert.That(oriented.Grid.Layout.Radius, Is.EqualTo(plain.Grid.Layout.Radius));
        }

        [Test]
        public void Build_InitialDirectionAlongSurfaceNormal_ReportsInvalidDirection()
        {
            using SurfaceGridSystem system = CreateSystem(CreateLargePlane(), out _);
            SurfaceGridRequest request = new(
                Vector3.zero,
                0.5f,
                new Vector3(0f, 0f, 1f),
                SurfacePatchBuildSettings.Unlimited,
                SurfaceQueryOptions.Default);

            SurfaceGridBuildResult result = system.Build(request);

            // 법선과 나란한 방향은 격자 방향을 정의할 수 없습니다. 임의로 0으로 되돌리지 않습니다.
            Assert.That(result.Status, Is.EqualTo(SurfaceGridBuildStatus.InvalidInitialDirection));
            Assert.That(result.Diagnostic, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Request_RejectsNonFiniteInputAndNonPositiveTileRadius()
        {
            Assert.That(
                () => new SurfaceGridRequest(new Vector3(float.NaN, 0f, 0f), 0.5f),
                Throws.ArgumentException);
            Assert.That(
                () => new SurfaceGridRequest(Vector3.zero, 0f),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new SurfaceGridRequest(
                    Vector3.zero,
                    0.5f,
                    new Vector3(0f, float.PositiveInfinity, 0f),
                    SurfacePatchBuildSettings.Unlimited,
                    SurfaceQueryOptions.Default),
                Throws.ArgumentException);
        }

        [Test]
        public void ChartDirection_MapsSurfaceDirectionOntoUnfoldedChart()
        {
            SurfaceTopology topology = CreateLargePlane();
            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(
                topology, new SurfacePoint(topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f)),
                SurfacePatchBuildSettings.Unlimited);
            SurfacePatchTriangle seedTriangle = FindSeedTriangle(patch, 0);

            bool tangentMapped = SurfaceChartDirection.TryGetChartDirection(
                topology, seedTriangle, new Vector3(1f, 0f, 0f), out Vector2 tangent);
            bool normalMapped = SurfaceChartDirection.TryGetChartDirection(
                topology, seedTriangle, new Vector3(0f, 0f, 1f), out _);

            Assert.That(tangentMapped, Is.True);
            // 평면 위에서 펼침은 등거리 변환이므로 단위 방향은 chart에서도 단위 길이를 유지해야 합니다.
            Assert.That(tangent.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(normalMapped, Is.False, "A direction along the surface normal cannot orient the grid.");
        }

        private static SurfacePatchTriangle FindSeedTriangle(SurfacePatch patch, int triangleIndex)
        {
            foreach (SurfacePatchTriangle triangle in patch.Triangles)
            {
                if (triangle.TriangleIndex == triangleIndex) return triangle;
            }
            throw new InvalidOperationException("Seed triangle is missing from its own patch.");
        }

        private static SurfaceGridSystem CreateSystem(SurfaceTopology topology, out StubSurfaceQuery query)
        {
            query = new StubSurfaceQuery(topology);
            return new SurfaceGridSystem(query, query);
        }

        private static SurfaceTopology CreateLargePlane() => SurfaceTopologyBuilder.Build(
            new SurfaceHandle(41),
            new[]
            {
                new Vector3(-10f, -10f, 0f), new Vector3(10f, -10f, 0f),
                new Vector3(-10f, 10f, 0f), new Vector3(10f, 10f, 0f)
            },
            new[] { 0, 1, 2, 2, 1, 3 });

        private static SurfaceTopology CreateSmallPlane() => SurfaceTopologyBuilder.Build(
            new SurfaceHandle(42),
            new[]
            {
                new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f),
                new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f)
            },
            new[] { 0, 1, 2, 2, 1, 3 });

        /// <summary>Physics 없이 지정한 topology 하나만 돌려주는 질의 대역입니다.</summary>
        private sealed class StubSurfaceQuery : ISurfaceQuery, ISurfaceProvider
        {
            private readonly SurfaceTopology _topology;

            public StubSurfaceQuery(SurfaceTopology topology) => _topology = topology;

            public bool TryFindSeed(in Vector3 worldPosition, in SurfaceQueryOptions options, out SurfaceQueryHit hit)
            {
                if (!SurfaceClosestPoint.TryFind(_topology, worldPosition, out SurfacePoint point, out float squaredDistance))
                {
                    hit = default;
                    return false;
                }
                // Adapter가 없으면 topology가 이미 월드 기준이라는 계약이므로 방향 변환이 항등이 됩니다.
                hit = new SurfaceQueryHit(point, null, _topology, Mathf.Sqrt(squaredDistance));
                return true;
            }

            public bool TryGetTopology(SurfaceHandle handle, out SurfaceTopology topology)
            {
                topology = handle == _topology.Handle ? _topology : null;
                return topology != null;
            }
        }

        /// <summary>표면이 하나도 없는 상황을 재현하는 질의 대역입니다.</summary>
        private sealed class EmptySurfaceQuery : ISurfaceQuery, ISurfaceProvider
        {
            public bool TryFindSeed(in Vector3 worldPosition, in SurfaceQueryOptions options, out SurfaceQueryHit hit)
            {
                hit = default;
                return false;
            }

            public bool TryGetTopology(SurfaceHandle handle, out SurfaceTopology topology)
            {
                topology = null;
                return false;
            }
        }
    }
}
