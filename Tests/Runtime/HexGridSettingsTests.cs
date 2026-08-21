using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexGridSettingsTests
    {
        [Test]
        public void RuntimeSetters_ClampMathematicalDomainsAndAvoidDuplicateNotifications()
        {
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            int changes = 0;
            settings.SettingsChanged += () => changes++;

            settings.TileRadius = -10f;
            settings.GridRadius = -3;
            settings.InteractionLayerMask = settings.InteractionLayerMask;

            Assert.That(settings.TileRadius, Is.EqualTo(HexGridSettings.TileRadiusMin));
            Assert.That(settings.GridRadius, Is.Zero);
            Assert.That(changes, Is.EqualTo(1));
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void TileRadius_AllowsRealSurfaceLengthsAboveFormerInspectorMaximum()
        {
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 12f;
            Assert.That(settings.TileRadius, Is.EqualTo(12f));
            Object.DestroyImmediate(settings);
        }
    }
}
