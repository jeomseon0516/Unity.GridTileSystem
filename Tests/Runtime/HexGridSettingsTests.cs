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

        [Test]
        public void GenerationPolicies_AreOwnedBySettingsAndCreateNamedOptionValues()
        {
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.SeedSearchRadius = 12f;
            settings.SurfaceLayerMask = 1 << 7;
            settings.PreferredSurfaceDirection = Vector3.forward;
            settings.MaximumPatchTriangles = 77;
            settings.MaximumPatchRadius = 18f;
            settings.MaximumClosureError = 0.002f;
            settings.SplitPatchWhenLimitReached = false;

            Assert.That(settings.QueryOptions.SearchRadius, Is.EqualTo(12f));
            Assert.That(settings.QueryOptions.LayerMask.value, Is.EqualTo(1 << 7));
            Assert.That(settings.QueryOptions.PreferredDirection, Is.EqualTo(Vector3.forward));
            Assert.That(settings.PatchBuildSettings.MaximumTriangleCount, Is.EqualTo(77));
            Assert.That(settings.PatchBuildSettings.MaximumIntrinsicRadius, Is.EqualTo(18f));
            Assert.That(settings.PatchBuildSettings.MaximumClosureError, Is.EqualTo(0.002f));
            Assert.That(settings.PatchBuildSettings.SplitWhenLimitReached, Is.False);

            Object.DestroyImmediate(settings);
        }

        [Test]
        public void DefaultDrawPolicy_ChangesReferenceAndNotifiesOnlyForActualChanges()
        {
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            IHexTileDrawPolicy policy = new OutlineDrawPolicy();
            int changes = 0;
            settings.SettingsChanged += () => changes++;

            settings.DefaultDrawPolicy = policy;
            settings.DefaultDrawPolicy = policy;
            settings.DefaultDrawPolicy = null;

            Assert.That(settings.DefaultDrawPolicy, Is.Null);
            Assert.That(changes, Is.EqualTo(2));
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void DrawPolicies_ExposeTheirFixedRenderingModes()
        {
            Assert.That(new FillDrawPolicy().DrawMode,
                Is.EqualTo(Surface.Rendering.SurfaceGridDrawMode.Fill));
            Assert.That(new OutlineDrawPolicy().DrawMode,
                Is.EqualTo(Surface.Rendering.SurfaceGridDrawMode.Outline));
            Assert.That(new BothDrawPolicy().DrawMode,
                Is.EqualTo(Surface.Rendering.SurfaceGridDrawMode.Both));
            Assert.That(new NoneDrawPolicy().DrawMode,
                Is.EqualTo(Surface.Rendering.SurfaceGridDrawMode.None));
        }
    }
}
