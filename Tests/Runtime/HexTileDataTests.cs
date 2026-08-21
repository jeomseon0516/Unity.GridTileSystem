using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexTileDataTests
    {
        [Test]
        public void Data_DoesNotOwnUnityObjectOrUnityEventFields()
        {
            System.Type[] fieldTypes = typeof(HexTileData)
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Select(field => field.FieldType)
                .ToArray();

            Assert.That(fieldTypes.Any(type => typeof(Object).IsAssignableFrom(type)), Is.False);
            Assert.That(fieldTypes.Any(type => typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(type)), Is.False);
        }

        [Test]
        public void Facade_DelegatesMutableStateToPureDataAndRaisesEventsOnce()
        {
            HexTile tile = new(1, -1, new Vector3(2f, 3f, 4f), new Vector2(5f, 6f), 2);
            int activeChanges = 0;
            int colorChanges = 0;
            tile.OnChangedActive += (_, _) => activeChanges++;
            tile.OnChangedColor += (_, _) => colorChanges++;

            tile.AddProperty("path");
            tile.IsActive = false;
            tile.Color = Color.red;

            Assert.That(tile.Data.Coordinates, Is.EqualTo(new HexCoordinates(1, -1)));
            Assert.That(tile.Data.TilePosition, Is.EqualTo(new Vector3(2f, 3f, 4f)));
            Assert.That(tile.Data.IntrinsicPosition, Is.EqualTo(new Vector2(5f, 6f)));
            Assert.That(tile.Data.Properties, Does.Contain("path"));
            Assert.That(tile.Data.IsActive, Is.False);
            Assert.That(tile.Data.Color, Is.EqualTo(Color.red));
            Assert.That(activeChanges, Is.EqualTo(1));
            Assert.That(colorChanges, Is.EqualTo(1));
        }

        [Test]
        public void Index_IsAssignedFromConstructorAndPreservedAcrossUserStateCopy()
        {
            HexTile source = new(1, -2, Vector3.zero, Vector2.zero, 41);
            source.Data.IsActive = false;
            source.Data.Color = Color.magenta;
            HexTile rebaked = new(1, -2, Vector3.zero, Vector2.zero, 7);

            // 재Bake는 순서 인덱스를 새로 부여하고 사용자 상태만 물려받아야 합니다.
            typeof(HexTile)
                .GetMethod("CopyStateFrom", System.Reflection.BindingFlags.Instance |
                                            System.Reflection.BindingFlags.NonPublic)!
                .Invoke(rebaked, new object[] { source });

            Assert.That(rebaked.Index, Is.EqualTo(7));
            Assert.That(rebaked.Data.IsActive, Is.False);
            Assert.That(rebaked.Data.Color, Is.EqualTo(Color.magenta));
        }

        [Test]
        public void Facade_WithMissingMigratedData_RecoversWithoutThrowing()
        {
            HexTile tile = new(0, 0);
            System.Reflection.FieldInfo dataField = typeof(HexTile).GetField(
                "data",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            dataField.SetValue(tile, null);

            Assert.DoesNotThrow(() => _ = tile.Coordinates);
            Assert.That(tile.Data, Is.Not.Null);
            Assert.That(tile.IsActive, Is.True);
        }
    }
}
