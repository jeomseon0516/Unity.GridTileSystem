using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Jeomseon.Unity.GridTileSystem;
using Jeomseon.Unity.GridTileSystem.Services;

namespace Jeomseon.HexGrid.Tests
{
    public sealed class HexGridTileDataStoreTests
    {
        private GameObject _decalHost;
        private DecalProjector _decalProjector;

        [SetUp]
        public void SetUp()
        {
            _decalHost = new GameObject(nameof(HexGridTileDataStoreTests));
            _decalProjector = _decalHost.AddComponent<DecalProjector>();
            _decalProjector.size = new Vector3(10f, 10f, 10f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_decalHost);
        }

        [Test]
        public void Rebuild_ForLimitOne_CreatesSevenUniqueTilesAndPopulatesLookup()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexGrid> hexGrids = new();
            HexGridTileDataStore store = new(hexGrids);

            store.Rebuild(_decalProjector, 0.1f, 1);

            Assert.That(hexGrids, Has.Count.EqualTo(7));
            Assert.That(store.TryGetTile(new HexCoordinates(0, 0), out _), Is.True);
            Assert.That(store.TryGetTile(new HexCoordinates(5, 5), out _), Is.False);
        }

        [Test]
        public void Rebuild_CalledTwice_ReplacesTilesInPlaceRatherThanDuplicating()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexGrid> hexGrids = new();
            HexGridTileDataStore store = new(hexGrids);

            store.Rebuild(_decalProjector, 0.1f, 1);
            store.TryGetTile(new HexCoordinates(0, 0), out global::Jeomseon.Unity.GridTileSystem.HexGrid first);
            first.AddProperty("marker");

            store.Rebuild(_decalProjector, 0.1f, 1);

            Assert.That(hexGrids, Has.Count.EqualTo(7));
            store.TryGetTile(new HexCoordinates(0, 0), out global::Jeomseon.Unity.GridTileSystem.HexGrid second);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Properties, Does.Contain("marker"));
        }

        [Test]
        public void SetActive_ForKnownCoordinates_UpdatesTileIsActive()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexGrid> hexGrids = new();
            HexGridTileDataStore store = new(hexGrids);
            store.Rebuild(_decalProjector, 0.1f, 1);

            store.SetActive(new AxialCoordinates(0, 0), false);

            store.TryGetTile(new HexCoordinates(0, 0), out global::Jeomseon.Unity.GridTileSystem.HexGrid hex);
            Assert.That(hex.IsActive, Is.False);
        }

        [Test]
        public void SetActive_ForUnknownCoordinates_DoesNothing()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexGrid> hexGrids = new();
            HexGridTileDataStore store = new(hexGrids);

            Assert.DoesNotThrow(() => store.SetActive(new AxialCoordinates(99, 99), false));
        }

        [Test]
        public void TileVisualsChanged_FiresOnRebuildAndOnSubsequentActiveOrColorChange()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexGrid> hexGrids = new();
            HexGridTileDataStore store = new(hexGrids);
            int fireCount = 0;
            store.TileVisualsChanged += () => fireCount++;

            store.Rebuild(_decalProjector, 0.1f, 1);
            Assert.That(fireCount, Is.EqualTo(1));

            store.TryGetTile(new HexCoordinates(0, 0), out global::Jeomseon.Unity.GridTileSystem.HexGrid hex);
            hex.IsActive = false;
            Assert.That(fireCount, Is.EqualTo(2));

            hex.Color = Color.red;
            Assert.That(fireCount, Is.EqualTo(3));
        }

        [Test]
        public void RebuildLookup_AfterManualListConstruction_PopulatesLookupFromExistingTiles()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexGrid> hexGrids = new()
            {
                new(0, 0),
                new(1, 0)
            };
            HexGridTileDataStore store = new(hexGrids);

            store.RebuildLookup();

            Assert.That(store.TryGetTile(new HexCoordinates(0, 0), out _), Is.True);
            Assert.That(store.TryGetTile(new HexCoordinates(1, 0), out _), Is.True);
        }
    }
}
