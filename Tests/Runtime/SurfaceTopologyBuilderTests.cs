using System.Linq;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class SurfaceTopologyBuilderTests
    {
        [Test]
        public void SurfaceHandle_PreservesSixtyFourBitIdentityAndReservesZero()
        {
            const ulong identity = (ulong)uint.MaxValue + 123UL;

            Assert.That(new SurfaceHandle(identity).Value, Is.EqualTo(identity));
            Assert.That(() => new SurfaceHandle(0UL), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(SurfaceHandle.Invalid.IsValid, Is.False);
        }

        private static readonly Vector3[] QuadPositions =
        {
            new(0f, 0f, 0f),
            new(1f, 0f, 0f),
            new(0f, 1f, 0f),
            new(1f, 1f, 0f)
        };

        [Test]
        public void Build_TwoTriangleQuad_ConnectsSharedEdgeSymmetrically()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1), QuadPositions, new[] { 0, 1, 2, 2, 1, 3 });

            Assert.That(topology.Triangles.Count, Is.EqualTo(2));
            Assert.That(topology.Adjacency[0].GetNeighbor(1), Is.EqualTo(1));
            Assert.That(topology.Adjacency[1].GetNeighbor(0), Is.EqualTo(0));
            Assert.That(topology.Diagnostics, Is.Empty);
        }

        [Test]
        public void Build_SameDirectedSharedEdge_ReportsInconsistentWinding()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1), QuadPositions, new[] { 0, 1, 2, 1, 2, 3 });

            Assert.That(
                topology.Diagnostics.Any(d => d.Kind == SurfaceTopologyDiagnosticKind.InconsistentWinding),
                Is.True);
        }

        [Test]
        public void Build_ThreeTrianglesSharingEdge_ReportsNonManifoldEdge()
        {
            Vector3[] positions =
            {
                new(0f, 0f, 0f), new(1f, 0f, 0f), new(0f, 1f, 0f),
                new(0f, -1f, 0f), new(0f, 0f, 1f)
            };

            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1), positions, new[] { 0, 1, 2, 1, 0, 3, 0, 1, 4 });

            Assert.That(
                topology.Diagnostics.Any(d => d.Kind == SurfaceTopologyDiagnosticKind.NonManifoldEdge),
                Is.True);
            Assert.That(topology.Adjacency.All(value =>
                value.Edge0 < 0 && value.Edge1 < 0 && value.Edge2 < 0), Is.True);
            Assert.That(topology.ComponentCount, Is.EqualTo(3));
        }

        [Test]
        public void Build_RepeatedIndexTriangle_DoesNotCreateSelfAdjacency()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1),
                new[] { Vector3.zero, Vector3.right },
                new[] { 0, 0, 1 });

            SurfaceTriangleAdjacency adjacency = topology.Adjacency[0];
            Assert.That(adjacency.Edge0, Is.EqualTo(-1));
            Assert.That(adjacency.Edge1, Is.EqualTo(-1));
            Assert.That(adjacency.Edge2, Is.EqualTo(-1));
        }

        [Test]
        public void Build_NonFinitePosition_ThrowsAtTopologyBoundary()
        {
            Assert.That(
                () => SurfaceTopologyBuilder.Build(
                    new SurfaceHandle(1),
                    new[] { Vector3.zero, Vector3.right, new Vector3(float.NaN, 1f, 0f) },
                    new[] { 0, 1, 2 }),
                Throws.ArgumentException);
        }

        [Test]
        public void Build_ZeroAreaTriangle_ReportsDegenerateTriangle()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1),
                new[] { Vector3.zero, Vector3.right, Vector3.right * 2f },
                new[] { 0, 1, 2 });

            Assert.That(topology.Diagnostics.Single().Kind, Is.EqualTo(SurfaceTopologyDiagnosticKind.DegenerateTriangle));
        }

        [Test]
        public void Build_DisconnectedTriangles_AssignsSeparateComponents()
        {
            Vector3[] positions =
            {
                Vector3.zero, Vector3.right, Vector3.up,
                Vector3.forward * 2f, Vector3.forward * 2f + Vector3.right,
                Vector3.forward * 2f + Vector3.up
            };

            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1), positions, new[] { 0, 1, 2, 3, 4, 5 });

            Assert.That(topology.ComponentCount, Is.EqualTo(2));
            Assert.That(topology.ComponentIds[0], Is.Not.EqualTo(topology.ComponentIds[1]));
        }

        [Test]
        public void Evaluate_UsesTriangleIdentityAndBarycentricCoordinates()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(7), QuadPositions, new[] { 0, 1, 2 });
            SurfacePoint point = new(new SurfaceHandle(7), 0, new Vector3(0.25f, 0.25f, 0.5f));

            Assert.That(topology.Evaluate(point), Is.EqualTo(new Vector3(0.25f, 0.5f, 0f)));
            Assert.That(point.IsValid, Is.True);
        }

        [Test]
        public void Unfold_FoldedQuad_PreservesEverySourceEdgeLength()
        {
            Vector3[] positions =
            {
                new(0f, 0f, 0f),
                new(1f, 0f, 0f),
                new(0f, 1f, 0f),
                new(1f, 0f, 1f)
            };
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1), positions, new[] { 0, 1, 2, 1, 0, 3 });

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0);

            Assert.That(patch.Triangles.Count, Is.EqualTo(2));
            foreach (SurfacePatchTriangle unfolded in patch.Triangles)
            {
                SurfaceTriangle source = topology.Triangles[unfolded.TriangleIndex];
                Assert.That(Vector2.Distance(unfolded.A, unfolded.B),
                    Is.EqualTo(Vector3.Distance(positions[source.A], positions[source.B])).Within(0.00001f));
                Assert.That(Vector2.Distance(unfolded.B, unfolded.C),
                    Is.EqualTo(Vector3.Distance(positions[source.B], positions[source.C])).Within(0.00001f));
                Assert.That(Vector2.Distance(unfolded.C, unfolded.A),
                    Is.EqualTo(Vector3.Distance(positions[source.C], positions[source.A])).Within(0.00001f));
            }
        }

        [Test]
        public void Unfold_OneHundredEightyDegreeFold_PlacesFacesOnOppositeSidesOfSharedEdge()
        {
            Vector3[] positions =
            {
                new(0f, 0f, 0f), new(1f, 0f, 0f),
                new(0f, 1f, 0f), new(0f, -1f, 0f)
            };
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1), positions, new[] { 0, 1, 2, 1, 0, 3 });

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0);
            SurfacePatchTriangle first = patch.Triangles.Single(t => t.TriangleIndex == 0);
            SurfacePatchTriangle second = patch.Triangles.Single(t => t.TriangleIndex == 1);

            float firstSide = Cross(first.B - first.A, first.C - first.A);
            float secondSide = Cross(first.B - first.A, second.C - first.A);
            Assert.That(firstSide * secondSide, Is.LessThan(0f));
        }

        [Test]
        public void Unfold_MaximumTriangleCount_TruncatesPatchExplicitly()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1), QuadPositions, new[] { 0, 1, 2, 2, 1, 3 });
            SurfacePatchBuildSettings settings = new(1, float.PositiveInfinity, float.PositiveInfinity);

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0, settings);

            Assert.That(patch.Triangles.Count, Is.EqualTo(1));
            Assert.That(patch.WasTruncated, Is.True);
            Assert.That(patch.ClosureToleranceExceeded, Is.False);
        }

        [Test]
        public void Unfold_SmallIntrinsicRadius_TruncatesDistantNeighbor()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(1), QuadPositions, new[] { 0, 1, 2, 2, 1, 3 });
            SurfacePatchBuildSettings settings = new(10, 0.25f, float.PositiveInfinity);

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, 0, settings);

            Assert.That(patch.Triangles.Count, Is.EqualTo(1));
            Assert.That(patch.WasTruncated, Is.True);
        }

        private static float Cross(in Vector2 a, in Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
