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

            settings.TileRadius = 2f;
            // 음수는 최소값으로 보정되며, 기본값이 이미 최소값이므로 위에서 한 번 올려 두어야
            // 보정 자체가 실제 변경으로 관측됩니다.
            settings.TileRadius = -10f;
            // 이미 최소값이므로 같은 방향의 추가 보정은 중복 알림을 만들지 않아야 합니다.
            settings.TileRadius = -20f;
            settings.InteractionLayerMask = settings.InteractionLayerMask;

            Assert.That(settings.TileRadius, Is.EqualTo(HexGridSettings.TileRadiusMin));
            Assert.That(changes, Is.EqualTo(2));
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
