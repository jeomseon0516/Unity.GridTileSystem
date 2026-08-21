using Jeomseon.Unity.GridTileSystem.Surface.Core;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class SurfaceClosestPointTests
    {
        /// <summary>z=0 평면의 2×2 quad입니다.</summary>
        private static SurfaceTopology CreateQuad() => SurfaceTopologyBuilder.Build(
            new SurfaceHandle(61),
            new[]
            {
                new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f),
                new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f)
            },
            new[] { 0, 1, 2, 2, 1, 3 });

        [Test]
        public void TryFind_PointAboveSurface_ProjectsOntoNearestTriangle()
        {
            SurfaceTopology topology = CreateQuad();

            Assert.That(
                SurfaceClosestPoint.TryFind(topology, new Vector3(-0.5f, -0.5f, 3f), out SurfacePoint point, out float sqr),
                Is.True);

            Assert.That(point.IsValid, Is.True);
            Assert.That(point.Surface, Is.EqualTo(topology.Handle));
            // 표면이 z=0이므로 수직 거리는 정확히 3이어야 합니다.
            Assert.That(Mathf.Sqrt(sqr), Is.EqualTo(3f).Within(0.0001f));
            Vector3 evaluated = topology.Evaluate(point);
            Assert.That(evaluated.x, Is.EqualTo(-0.5f).Within(0.0001f));
            Assert.That(evaluated.y, Is.EqualTo(-0.5f).Within(0.0001f));
            Assert.That(evaluated.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void TryFind_PointOutsideSurface_ClampsToBoundary()
        {
            SurfaceTopology topology = CreateQuad();

            Assert.That(
                SurfaceClosestPoint.TryFind(topology, new Vector3(5f, 0f, 0f), out SurfacePoint point, out _),
                Is.True);

            Vector3 evaluated = topology.Evaluate(point);
            // 표면은 x=1에서 끝나므로 경계로 clamp돼야 합니다.
            Assert.That(evaluated.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(point.IsValid, Is.True);
        }

        [Test]
        public void TryFind_AlwaysProducesValidBarycentricInsideTriangle()
        {
            SurfaceTopology topology = CreateQuad();
            Vector3[] queries =
            {
                new(0f, 0f, 1f), new(-2f, -2f, 0f), new(0.9f, -0.9f, -1f),
                new(0f, 5f, 0f), new(1f, 1f, 0f)
            };

            foreach (Vector3 query in queries)
            {
                Assert.That(SurfaceClosestPoint.TryFind(topology, query, out SurfacePoint point, out _), Is.True);
                Assert.That(point.IsValid, Is.True, $"query {query}");
                Assert.That(point.Barycentric.x, Is.GreaterThanOrEqualTo(-0.0001f));
                Assert.That(point.Barycentric.y, Is.GreaterThanOrEqualTo(-0.0001f));
                Assert.That(point.Barycentric.z, Is.GreaterThanOrEqualTo(-0.0001f));
            }
        }

        [Test]
        public void TryFind_NonFiniteQuery_ReturnsFalse()
        {
            SurfaceTopology topology = CreateQuad();
            Assert.That(
                SurfaceClosestPoint.TryFind(topology, new Vector3(float.NaN, 0f, 0f), out _, out _),
                Is.False);
        }
    }
}
