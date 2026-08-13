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
    }
}
