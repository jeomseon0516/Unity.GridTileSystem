using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class TerrainTopologyFactoryTests
    {
        [Test]
        public void BuildTopology_UsesVirtualHeightfieldViewsAndEvaluatesTerrainHeight()
        {
            TerrainData terrainData = CreateTerrainData();
            float[,] heights = new float[33, 33];
            heights[16, 16] = 0.5f;
            terrainData.SetHeights(0, 0, heights);

            TerrainSurfaceTopology topology = TerrainTopologyFactory.BuildTopology(terrainData);

            Assert.That(topology.Positions, Is.Not.InstanceOf<Vector3[]>());
            Assert.That(topology.Triangles, Is.Not.InstanceOf<SurfaceTriangle[]>());
            Assert.That(topology.Positions.Count, Is.EqualTo(33 * 33));
            Assert.That(topology.Triangles.Count, Is.EqualTo(32 * 32 * 2));
            Vector3 center = topology.Positions[16 * 33 + 16];
            Assert.That(center, Is.EqualTo(new Vector3(16f, 4f, 16f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(topology.TryGetSurfacePoint(new Vector3(8.25f, 0f, 7.25f), out SurfacePoint point), Is.True);
            Assert.That(topology.Evaluate(point).x, Is.EqualTo(8.25f).Within(0.0001f));
            Assert.That(topology.Evaluate(point).z, Is.EqualTo(7.25f).Within(0.0001f));

            Object.DestroyImmediate(terrainData);
        }

        [Test]
        public void BuildTopology_HoleCellIsTraversalBoundary()
        {
            TerrainData terrainData = CreateTerrainData();
            bool[,] holes = new bool[terrainData.holesResolution, terrainData.holesResolution];
            for (int z = 0; z < terrainData.holesResolution; z++)
                for (int x = 0; x < terrainData.holesResolution; x++) holes[z, x] = true;
            holes[4, 3] = false;
            terrainData.SetHoles(0, 0, holes);
            TerrainSurfaceTopology topology = TerrainTopologyFactory.BuildTopology(terrainData);
            int holeTriangle = (4 * 32 + 3) * 2;

            Assert.That(topology.IsTriangleTraversable(holeTriangle), Is.False);
            Assert.That(topology.Adjacency[holeTriangle], Is.EqualTo(new SurfaceTriangleAdjacency(-1, -1, -1)));
            Assert.That(
                () => TriangleUnfoldingParameterizer.Build(topology, holeTriangle),
                Throws.ArgumentException);

            Object.DestroyImmediate(terrainData);
        }

        private static TerrainData CreateTerrainData()
        {
            TerrainData terrainData = new() { heightmapResolution = 33, size = new Vector3(32f, 8f, 32f) };
            return terrainData;
        }
    }
}
