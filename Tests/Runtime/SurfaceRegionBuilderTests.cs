using System.Linq;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class SurfaceRegionBuilderTests
    {
        [Test]
        public void Build_HexInsidePlaneTriangle_PreservesHexAndBarycentricBinding()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(11),
                new[] { Vector3.zero, new Vector3(4f, 0f, 0f), new Vector3(0f, 4f, 0f) },
                new[] { 0, 1, 2 });
            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0);
            Vector2[] hex = CreateHex(new Vector2(1f, 1f), 0.5f);

            SurfaceRegion region = SurfaceRegionBuilder.Build(topology, patch, hex);

            Assert.That(region.Vertices.Count, Is.EqualTo(6));
            Assert.That(region.TriangleIndices.Count, Is.EqualTo(12));
            Assert.That(region.IntrinsicArea, Is.EqualTo(3f * Mathf.Sqrt(3f) * 0.125f).Within(0.00001f));
            foreach (SurfaceRegionVertex vertex in region.Vertices)
            {
                Assert.That(vertex.SurfacePoint.IsValid, Is.True);
                Vector3 evaluated = topology.Evaluate(vertex.SurfacePoint);
                Assert.That(evaluated.x, Is.EqualTo(vertex.IntrinsicPosition.x).Within(0.00001f));
                Assert.That(evaluated.y, Is.EqualTo(vertex.IntrinsicPosition.y).Within(0.00001f));
            }
        }

        [Test]
        public void Build_ClockwisePolygon_NormalizesWinding()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(12),
                new[] { Vector3.zero, new Vector3(3f, 0f, 0f), new Vector3(0f, 3f, 0f) },
                new[] { 0, 1, 2 });
            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0);
            Vector2[] clockwiseSquare =
            {
                new(0.25f, 0.25f), new(0.25f, 0.75f),
                new(0.75f, 0.75f), new(0.75f, 0.25f)
            };

            SurfaceRegion region = SurfaceRegionBuilder.Build(topology, patch, clockwiseSquare);

            Assert.That(region.Vertices.Count, Is.EqualTo(4));
            Assert.That(region.Vertices.All(vertex => vertex.SurfacePoint.IsValid), Is.True);
        }

        [Test]
        public void Build_PolygonCrossingFold_ProducesBindingsOnBothSourceTriangles()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(13),
                new[]
                {
                    new Vector3(0f, 0f, 0f), new Vector3(2f, 0f, 0f),
                    new Vector3(0f, 2f, 0f), new Vector3(0f, 0f, 2f)
                },
                new[] { 0, 1, 2, 1, 0, 3 });
            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0);
            Vector2[] crossingRectangle =
            {
                new(0.25f, -0.5f), new(1.25f, -0.5f),
                new(1.25f, 0.5f), new(0.25f, 0.5f)
            };

            SurfaceRegion region = SurfaceRegionBuilder.Build(topology, patch, crossingRectangle);

            Assert.That(region.Vertices.Any(vertex => vertex.SurfacePoint.TriangleIndex == 0), Is.True);
            Assert.That(region.Vertices.Any(vertex => vertex.SurfacePoint.TriangleIndex == 1), Is.True);
            Assert.That(region.Vertices.All(vertex => vertex.SurfacePoint.IsValid), Is.True);
        }

        [Test]
        public void Build_ConcavePolygon_RejectsUnsupportedClipInput()
        {
            SurfaceTopology topology = CreateSingleTriangleTopology();
            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0);
            Vector2[] concave =
            {
                new(0f, 0f), new(2f, 0f), new(1f, 0.5f), new(2f, 2f), new(0f, 2f)
            };

            Assert.That(() => SurfaceRegionBuilder.Build(topology, patch, concave), Throws.ArgumentException);
        }

        [Test]
        public void Build_ZeroLengthPolygonEdge_RejectsAmbiguousHalfPlane()
        {
            SurfaceTopology topology = CreateSingleTriangleTopology();
            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0);
            Vector2[] repeated = { Vector2.zero, Vector2.right, Vector2.right, Vector2.up };

            Assert.That(() => SurfaceRegionBuilder.Build(topology, patch, repeated), Throws.ArgumentException);
        }

        private static SurfaceTopology CreateSingleTriangleTopology() => SurfaceTopologyBuilder.Build(
            new SurfaceHandle(14),
            new[] { Vector3.zero, new Vector3(4f, 0f, 0f), new Vector3(0f, 4f, 0f) },
            new[] { 0, 1, 2 });

        private static Vector2[] CreateHex(in Vector2 center, float radius)
        {
            Vector2[] result = new Vector2[6];
            for (int corner = 0; corner < result.Length; corner++)
            {
                float angle = Mathf.Deg2Rad * (corner * 60f);
                result[corner] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return result;
        }
    }
}
