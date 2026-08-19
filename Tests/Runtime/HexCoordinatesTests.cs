using NUnit.Framework;
using UnityEngine;
using Jeomseon.Unity.GridTileSystem;

namespace Jeomseon.HexGrid.Tests
{
    public sealed class HexCoordinatesTests
    {
        [Test]
        public void Constructor_CalculatesSCoordinate()
        {
            HexCoordinates coordinates = new(2, -5);

            Assert.That(coordinates.S, Is.EqualTo(3));
        }

        [Test]
        public void Vector3Conversion_UsesAllCubeCoordinates()
        {
            Vector3 converted = new HexCoordinates(2, -5);

            Assert.That(converted, Is.EqualTo(new Vector3(2, -5, 3)));
        }

        [Test]
        public void AxialCoordinates_WithSameValues_AreEqual()
        {
            Assert.That(new AxialCoordinates(4, -1), Is.EqualTo(new AxialCoordinates(4, -1)));
        }

        [Test]
        public void Round_WithNoAmbiguity_ReturnsExactCoordinates()
        {
            HexCoordinates result = HexCoordinates.Round(new Vector2(2f, -5f));

            Assert.That(result, Is.EqualTo(new HexCoordinates(2, -5)));
        }

        [Test]
        public void Round_WhenSHasTheLargestRoundingError_RecomputesSFromQAndR()
        {
            // q and r both round down to 0 with a 0.3 error each, while their naive s (-0.6 -> -1)
            // has the larger 0.4 error, so s must be rebuilt from the corrected q/r instead of kept.
            HexCoordinates result = HexCoordinates.Round(new Vector2(0.3f, 0.3f));

            Assert.That(result, Is.EqualTo(new HexCoordinates(0, 0)));
        }

        [Test]
        public void Round_AlwaysSatisfiesCubeCoordinateInvariant(
            [Values(-2.7f, -1.4f, -0.5f, 0f, 0.49f, 1.5f, 3.2f)] float q,
            [Values(-2.7f, -1.4f, -0.5f, 0f, 0.49f, 1.5f, 3.2f)] float r)
        {
            HexCoordinates result = HexCoordinates.Round(new Vector2(q, r));

            Assert.That(result.Q + result.R + result.S, Is.EqualTo(0));
        }
    }
}
