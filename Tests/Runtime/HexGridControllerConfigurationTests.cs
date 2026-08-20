using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Jeomseon.Unity.Projector;
using Jeomseon.Unity.GridTileSystem;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexGridControllerConfigurationTests
    {
        [Test]
        public void BakeTiles_WithoutSettings_ReportsConfigurationErrorInsteadOfThrowing()
        {
            GameObject gameObject = new(nameof(HexGridControllerConfigurationTests));
            gameObject.AddComponent<MeshProjector>();
            HexGridController manager = gameObject.AddComponent<HexGridController>();

            LogAssert.Expect(LogType.Error, "HexGridController requires a HexGridSettings asset.");
            Assert.DoesNotThrow(manager.BakeTiles);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void MeshProjector_DoesNotExposeMutableMaterial()
        {
            Assert.That(typeof(MeshProjector).GetProperty("Material"), Is.Null);
            Assert.That(typeof(MeshProjector).GetField("material"), Is.Null);
        }
    }
}
