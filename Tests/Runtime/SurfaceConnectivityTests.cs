using System.Collections.Generic;
using System.Linq;
using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using Jeomseon.Unity.GridTileSystem.Surface.Query;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class SurfaceConnectivityTests
    {
        /// <summary>xy 평면에 놓인 두 Quad가 x = 1에서 정확히 맞닿는 구성입니다.</summary>
        private const float SharedEdgeX = 1f;

        [Test]
        public void Connectivity_TouchingSurfaces_FindsLinkAcrossSharedEdge()
        {
            StubWorld world = StubWorld.CreateAdjacentPlanes();
            GeometrySurfaceConnectivity connectivity = new(world, world);

            bool found = TryFindAnyLink(world, world.Left, connectivity, out SurfaceLink link);

            Assert.That(found, Is.True, "The shared edge between two touching planes must be linked.");
            Assert.That(link.FromSurface, Is.EqualTo(world.Left));
            Assert.That(link.ToSurface, Is.EqualTo(world.Right));
            Assert.That(link.IsValid, Is.True);
        }

        [Test]
        public void Connectivity_SeparatedSurfaces_DoesNotLink()
        {
            // 가까움만으로 연결을 인정하면 무관한 표면이 Grid에 딸려 들어옵니다.
            StubWorld world = StubWorld.CreateSeparatedPlanes(0.5f);
            GeometrySurfaceConnectivity connectivity = new(world, world);

            Assert.That(TryFindAnyLink(world, world.Left, connectivity, out _), Is.False);
        }

        [Test]
        public void Connectivity_OppositeFacingSurfaces_DoesNotLink()
        {
            // 경계는 정확히 공유하지만 서로 반대를 향하는 표면입니다(벽과 그 뒷면).
            StubWorld world = StubWorld.CreateOppositeFacingPlanes();
            GeometrySurfaceConnectivity connectivity = new(world, world);

            Assert.That(TryFindAnyLink(world, world.Left, connectivity, out _), Is.False);
        }

        [Test]
        public void Connectivity_RepeatedQuery_ReusesCachedAnswer()
        {
            StubWorld world = StubWorld.CreateAdjacentPlanes();
            GeometrySurfaceConnectivity connectivity = new(world, world);

            TryFindAnyLink(world, world.Left, connectivity, out _);
            int afterFirst = connectivity.CachedQueryCount;
            int discoveriesAfterFirst = world.DiscoverCallCount;
            TryFindAnyLink(world, world.Left, connectivity, out _);

            Assert.That(connectivity.CachedQueryCount, Is.EqualTo(afterFirst));
            Assert.That(world.DiscoverCallCount, Is.EqualTo(discoveriesAfterFirst), "A cached edge must not be re-resolved.");
        }

        [Test]
        public void Patch_WithConnectivity_ExtendsChartAcrossSurfaces()
        {
            StubWorld world = StubWorld.CreateAdjacentPlanes();
            GeometrySurfaceConnectivity connectivity = new(world, world);
            SurfacePoint seed = new(world.Left, 0, new Vector3(0.5f, 0.25f, 0.25f));

            SurfacePatch withoutLinks = TriangleUnfoldingParameterizer.Build(
                world, seed, SurfacePatchBuildSettings.Unlimited, null);
            SurfacePatch withLinks = TriangleUnfoldingParameterizer.Build(
                world, seed, SurfacePatchBuildSettings.Unlimited, connectivity);

            Assert.That(withoutLinks.SpansMultipleSurfaces, Is.False);
            Assert.That(withoutLinks.Triangles.Count, Is.EqualTo(2));
            Assert.That(withLinks.SpansMultipleSurfaces, Is.True);
            Assert.That(withLinks.Triangles.Count, Is.EqualTo(4));
            Assert.That(withLinks.Surface, Is.EqualTo(world.Left), "Patch.Surface stays the seed surface.");
            Assert.That(withLinks.Triangles.Any(triangle => triangle.Surface == world.Right), Is.True);
            // 연결을 건너도 같은 펼침 연산을 쓰므로 chart는 접히지 않고 이어져야 합니다.
            Assert.That(withLinks.IntrinsicBounds.width, Is.GreaterThan(withoutLinks.IntrinsicBounds.width));
        }

        [Test]
        public void Patch_AcrossLink_PreservesEdgeLengthsOfTheLinkedSurface()
        {
            StubWorld world = StubWorld.CreateAdjacentPlanes();
            GeometrySurfaceConnectivity connectivity = new(world, world);
            SurfacePoint seed = new(world.Left, 0, new Vector3(0.5f, 0.25f, 0.25f));

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(
                world, seed, SurfacePatchBuildSettings.Unlimited, connectivity);
            world.TryGetTopology(world.Right, out SurfaceTopology right);

            foreach (SurfacePatchTriangle face in patch.Triangles.Where(t => t.Surface == world.Right))
            {
                SurfaceTriangle triangle = right.Triangles[face.TriangleIndex];
                AssertEdgeLength(right, triangle.A, triangle.B, face.A, face.B);
                AssertEdgeLength(right, triangle.B, triangle.C, face.B, face.C);
                AssertEdgeLength(right, triangle.C, triangle.A, face.C, face.A);
            }
        }

        [Test]
        public void Grid_AcrossLink_ProducesTilesBoundToBothSurfaces()
        {
            StubWorld world = StubWorld.CreateAdjacentPlanes();
            GeometrySurfaceConnectivity connectivity = new(world, world);
            SurfacePoint seed = new(world.Left, 0, new Vector3(0.5f, 0.25f, 0.25f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                world, seed, 0.25f, SurfacePatchBuildSettings.Unlimited, Vector3.zero, connectivity);

            HashSet<SurfaceHandle> surfaces = new();
            foreach (SurfaceGridTileRegion tile in grid.Tiles)
            {
                foreach (SurfaceRegionVertex vertex in tile.Region.Vertices) surfaces.Add(vertex.SurfacePoint.Surface);
            }

            Assert.That(grid.Tiles.Count, Is.GreaterThan(0));
            // Region vertex가 Face별 Surface를 그대로 가리켜야 3D 복원이 올바른 표면에서 일어납니다.
            Assert.That(surfaces, Does.Contain(world.Left));
            Assert.That(surfaces, Does.Contain(world.Right));
        }

        [Test]
        public void Grid_WithoutConnectivity_StaysOnTheSeedSurface()
        {
            StubWorld world = StubWorld.CreateAdjacentPlanes();
            SurfacePoint seed = new(world.Left, 0, new Vector3(0.5f, 0.25f, 0.25f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                world, seed, 0.25f, SurfacePatchBuildSettings.Unlimited, Vector3.zero, null);

            Assert.That(grid.Patch.SpansMultipleSurfaces, Is.False);
            foreach (SurfaceGridTileRegion tile in grid.Tiles)
            {
                foreach (SurfaceRegionVertex vertex in tile.Region.Vertices)
                    Assert.That(vertex.SurfacePoint.Surface, Is.EqualTo(world.Left));
            }
        }

        [Test]
        public void Patch_ChainedSurfaces_ReachesEveryLinkedSurfaceWithoutBeingListed()
        {
            // S-F: 세 Surface 중 어느 것도 요청에 없습니다. 경계에 도달할 때마다 다음 것을 발견합니다.
            StubWorld world = StubWorld.CreateChain(3);
            GeometrySurfaceConnectivity connectivity = new(world, world);
            SurfacePoint seed = new(world.Chain[0], 0, new Vector3(0.5f, 0.25f, 0.25f));

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(
                world, seed, SurfacePatchBuildSettings.Unlimited, connectivity);

            HashSet<SurfaceHandle> reached = new(patch.Triangles.Select(triangle => triangle.Surface));
            Assert.That(patch.Triangles.Count, Is.EqualTo(6));
            Assert.That(reached, Is.EquivalentTo(world.Chain));
            Assert.That(patch.IntrinsicBounds.width, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void Patch_LimitedGrowth_NeverTouchesSurfacesItDoesNotReach()
        {
            // S-I의 lazy 절반: 성장이 멈춘 뒤의 Surface는 topology조차 만들지 않아야 합니다.
            StubWorld world = StubWorld.CreateChain(3);
            GeometrySurfaceConnectivity connectivity = new(world, world);
            SurfacePoint seed = new(world.Chain[0], 0, new Vector3(0.5f, 0.25f, 0.25f));
            SurfacePatchBuildSettings limited = new(2, 100f, 1f);

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(world, seed, limited, connectivity);

            Assert.That(patch.WasTruncated, Is.True);
            Assert.That(patch.SpansMultipleSurfaces, Is.False);
            Assert.That(world.TopologyRequests(world.Chain[2]), Is.EqualTo(0),
                "The far surface must never be built when the chart never reaches its boundary.");
        }

        [Test]
        public void Grid_ChainedSurfaces_BindsTilesToEverySurfaceInTheChain()
        {
            StubWorld world = StubWorld.CreateChain(3);
            GeometrySurfaceConnectivity connectivity = new(world, world);
            SurfacePoint seed = new(world.Chain[0], 0, new Vector3(0.5f, 0.25f, 0.25f));

            SurfaceGrid grid = SurfaceGridBuilder.Build(
                world, seed, 0.25f, SurfacePatchBuildSettings.Unlimited, Vector3.zero, connectivity);

            HashSet<SurfaceHandle> bound = new();
            foreach (SurfaceGridTileRegion tile in grid.Tiles)
            {
                foreach (SurfaceRegionVertex vertex in tile.Region.Vertices) bound.Add(vertex.SurfacePoint.Surface);
            }

            Assert.That(bound, Is.EquivalentTo(world.Chain));
        }

        private static void AssertEdgeLength(
            SurfaceTopology topology,
            int firstVertex,
            int secondVertex,
            in Vector2 firstChart,
            in Vector2 secondChart)
        {
            float expected = Vector3.Distance(topology.Positions[firstVertex], topology.Positions[secondVertex]);
            Assert.That(Vector2.Distance(firstChart, secondChart), Is.EqualTo(expected).Within(0.0001f));
        }

        private static bool TryFindAnyLink(
            ISurfaceProvider surfaces,
            SurfaceHandle surface,
            ISurfaceConnectivity connectivity,
            out SurfaceLink link)
        {
            link = default;
            if (!surfaces.TryGetTopology(surface, out SurfaceTopology topology)) return false;
            for (int triangleIndex = 0; triangleIndex < topology.Triangles.Count; triangleIndex++)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    if (topology.Adjacency[triangleIndex].GetNeighbor(edge) >= 0) continue;
                    if (connectivity.TryGetLink(surface, triangleIndex, edge, out link)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Physics 없이 topology 몇 개만 들고 있는 월드 대역입니다. Adapter의 Transform이 없으면
        /// topology가 이미 월드 기준이라는 계약이므로 좌표 변환이 항등이 됩니다.
        /// </summary>
        private sealed class StubWorld : ISurfaceProvider, ISurfaceDiscovery
        {
            private readonly Dictionary<SurfaceHandle, SurfaceTopology> _topologies = new();
            private readonly Dictionary<SurfaceHandle, ISurfaceAdapter> _adapters = new();

            private readonly Dictionary<SurfaceHandle, Bounds> _bounds = new();
            private readonly Dictionary<SurfaceHandle, int> _topologyRequests = new();

            public SurfaceHandle Left { get; private set; }
            public SurfaceHandle Right { get; private set; }
            public IReadOnlyList<SurfaceHandle> Chain { get; private set; } = System.Array.Empty<SurfaceHandle>();
            public int DiscoverCallCount { get; private set; }

            /// <summary>지정한 Surface의 topology가 요청된 횟수입니다. lazy 확장 관찰에 씁니다.</summary>
            public int TopologyRequests(SurfaceHandle surface) =>
                _topologyRequests.TryGetValue(surface, out int count) ? count : 0;

            /// <summary>x = 1에서 정확히 맞닿고 같은 방향을 향하는 두 평면입니다.</summary>
            public static StubWorld CreateAdjacentPlanes() => Create(
                Quad(new SurfaceHandle(51), -1f, SharedEdgeX, false),
                Quad(new SurfaceHandle(52), SharedEdgeX, 3f, false));

            /// <summary>경계가 떨어져 있어 연결로 볼 수 없는 두 평면입니다.</summary>
            public static StubWorld CreateSeparatedPlanes(float gap) => Create(
                Quad(new SurfaceHandle(53), -1f, SharedEdgeX, false),
                Quad(new SurfaceHandle(54), SharedEdgeX + gap, 3f, false));

            /// <summary>경계는 공유하지만 winding이 반대라 서로 등지고 있는 두 평면입니다.</summary>
            public static StubWorld CreateOppositeFacingPlanes() => Create(
                Quad(new SurfaceHandle(55), -1f, SharedEdgeX, false),
                Quad(new SurfaceHandle(56), SharedEdgeX, 3f, true));

            public bool TryGetTopology(SurfaceHandle handle, out SurfaceTopology topology)
            {
                _topologyRequests[handle] = TopologyRequests(handle) + 1;
                return _topologies.TryGetValue(handle, out topology);
            }

            public bool TryGetAdapter(SurfaceHandle surface, out ISurfaceAdapter adapter) =>
                _adapters.TryGetValue(surface, out adapter);

            /// <summary>Physics 질의처럼 반경과 겹치는 Surface만 돌려줍니다. lazy 확장의 전제입니다.</summary>
            public int Discover(in Vector3 worldPosition, float radius, LayerMask layerMask, List<ISurfaceAdapter> results)
            {
                DiscoverCallCount++;
                results.Clear();
                foreach (KeyValuePair<SurfaceHandle, ISurfaceAdapter> entry in _adapters)
                {
                    if (_bounds[entry.Key].SqrDistance(worldPosition) <= radius * radius) results.Add(entry.Value);
                }
                return results.Count;
            }

            /// <summary>x축을 따라 정확히 맞닿는 Quad를 <paramref name="count"/>개 잇습니다.</summary>
            public static StubWorld CreateChain(int count)
            {
                StubWorld world = new();
                List<SurfaceHandle> chain = new();
                for (int i = 0; i < count; i++)
                {
                    SurfaceTopology topology = Quad(new SurfaceHandle(61 + i), -1f + i * 2f, 1f + i * 2f, false);
                    world.Add(topology);
                    chain.Add(topology.Handle);
                }
                world.Chain = chain;
                world.Left = chain[0];
                world.Right = chain[count - 1];
                return world;
            }

            private static StubWorld Create(SurfaceTopology left, SurfaceTopology right)
            {
                StubWorld world = new();
                world.Left = left.Handle;
                world.Right = right.Handle;
                world.Add(left);
                world.Add(right);
                return world;
            }

            private void Add(SurfaceTopology topology)
            {
                _topologies[topology.Handle] = topology;
                _adapters[topology.Handle] = new StubAdapter(topology.Handle);
                Bounds bounds = new(topology.Positions[0], Vector3.zero);
                foreach (Vector3 position in topology.Positions) bounds.Encapsulate(position);
                _bounds[topology.Handle] = bounds;
            }

            /// <summary>xy 평면 위 [minimumX, maximumX] × [-1, 1] Quad를 만듭니다.</summary>
            private static SurfaceTopology Quad(SurfaceHandle handle, float minimumX, float maximumX, bool flipped)
            {
                Vector3[] positions =
                {
                    new(minimumX, -1f, 0f), new(maximumX, -1f, 0f),
                    new(minimumX, 1f, 0f), new(maximumX, 1f, 0f)
                };
                int[] indices = flipped
                    ? new[] { 2, 1, 0, 3, 1, 2 }
                    : new[] { 0, 1, 2, 2, 1, 3 };
                return SurfaceTopologyBuilder.Build(handle, positions, indices);
            }
        }

        /// <summary>Transform과 Collider가 없는 최소 Adapter 대역입니다.</summary>
        private sealed class StubAdapter : ISurfaceAdapter
        {
            public StubAdapter(SurfaceHandle handle) => Handle = handle;

            public SurfaceHandle Handle { get; }
            public SurfaceCapabilities Capabilities => SurfaceCapabilities.Static | SurfaceCapabilities.CpuReadable;
            public Transform SurfaceTransform => null;
            public Collider PickingCollider => null;
            public Bounds WorldBounds => default;

            public SurfaceTopology BuildTopology() => null;
            public bool TryEvaluateWorldPosition(in SurfacePoint point, out Vector3 worldPosition)
            {
                worldPosition = default;
                return false;
            }

            public void Dispose()
            {
            }
        }
    }
}
