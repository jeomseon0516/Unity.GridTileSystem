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
        public void CalculateIndex_ForRadiusThree_IsUniqueAndContiguous()
        {
            int radius = 3;
            int[] indices = (
                from q in Enumerable.Range(-radius, radius * 2 + 1)
                from r in Enumerable.Range(-radius, radius * 2 + 1)
                let coordinates = new HexCoordinates(q, r)
                where Mathf.Abs(coordinates.S) <= radius
                select HexTileData.CalculateIndex(coordinates, radius)).OrderBy(value => value).ToArray();

            Assert.That(indices, Is.EqualTo(Enumerable.Range(0, 37).ToArray()));
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
