using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Jeomseon.Unity.Projector;
using Jeomseon.Unity.GridTileSystem;
using Jeomseon.Unity.GridTileSystem.Services;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexTileStoreTests
    {
        private GameObject _projectorHost;
        private MeshProjector _projector;

        [SetUp]
        public void SetUp()
        {
            _projectorHost = new GameObject(nameof(HexTileStoreTests));
            _projector = _projectorHost.AddComponent<MeshProjector>();
            _projector.Size = new Vector3(10f, 10f, 10f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_projectorHost);
        }

        [Test]
        public void Rebuild_ForLimitOne_CreatesSevenUniqueTilesAndPopulatesLookup()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexTile> tiles = new();
            HexTileStore store = new(tiles);

            store.Bake(_projector, 0.1f, 1, ~0);

            Assert.That(tiles, Has.Count.EqualTo(7));
            Assert.That(store.TryGetTile(new HexCoordinates(0, 0), out _), Is.True);
            Assert.That(store.TryGetTile(new HexCoordinates(5, 5), out _), Is.False);
        }

        [Test]
        public void Rebuild_CalledTwice_ReplacesTilesInPlaceRatherThanDuplicating()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexTile> tiles = new();
            HexTileStore store = new(tiles);

            store.Bake(_projector, 0.1f, 1, ~0);
            store.TryGetTile(new HexCoordinates(0, 0), out global::Jeomseon.Unity.GridTileSystem.HexTile first);
            first.AddProperty("marker");

            store.Bake(_projector, 0.1f, 1, ~0);

            Assert.That(tiles, Has.Count.EqualTo(7));
            store.TryGetTile(new HexCoordinates(0, 0), out global::Jeomseon.Unity.GridTileSystem.HexTile second);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Properties, Does.Contain("marker"));
        }

        [Test]
        public void SetActive_ForKnownCoordinates_UpdatesTileIsActive()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexTile> tiles = new();
            HexTileStore store = new(tiles);
            store.Bake(_projector, 0.1f, 1, ~0);

            store.SetActive(new AxialCoordinates(0, 0), false);

            store.TryGetTile(new HexCoordinates(0, 0), out global::Jeomseon.Unity.GridTileSystem.HexTile hex);
            Assert.That(hex.IsActive, Is.False);
        }

        [Test]
        public void SetActive_ForUnknownCoordinates_DoesNothing()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexTile> tiles = new();
            HexTileStore store = new(tiles);

            Assert.DoesNotThrow(() => store.SetActive(new AxialCoordinates(99, 99), false));
        }

        [Test]
        public void TileVisualsChanged_FiresOnRebuildAndOnSubsequentActiveOrColorChange()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexTile> tiles = new();
            HexTileStore store = new(tiles);
            int fireCount = 0;
            store.TileVisualsChanged += () => fireCount++;

            store.Bake(_projector, 0.1f, 1, ~0);
            Assert.That(fireCount, Is.EqualTo(1));

            store.TryGetTile(new HexCoordinates(0, 0), out global::Jeomseon.Unity.GridTileSystem.HexTile hex);
            hex.IsActive = false;
            Assert.That(fireCount, Is.EqualTo(2));

            hex.Color = Color.red;
            Assert.That(fireCount, Is.EqualTo(3));
        }

        [Test]
        public void RebuildLookup_AfterManualListConstruction_PopulatesLookupFromExistingTiles()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexTile> tiles = new()
            {
                new(0, 0),
                new(1, 0)
            };
            HexTileStore store = new(tiles);

            store.RebuildLookup();

            Assert.That(store.TryGetTile(new HexCoordinates(0, 0), out _), Is.True);
            Assert.That(store.TryGetTile(new HexCoordinates(1, 0), out _), Is.True);
        }

        [Test]
        public void Clear_AfterBake_ThenBakeAgain_RebuildsTilesAndLookup()
        {
            List<global::Jeomseon.Unity.GridTileSystem.HexTile> tiles = new();
            HexTileStore store = new(tiles);

            store.Bake(_projector, 0.1f, 1, ~0);
            store.Clear();
            store.Bake(_projector, 0.1f, 1, ~0);

            Assert.That(tiles, Has.Count.EqualTo(7));
            Assert.That(store.TryGetTile(new HexCoordinates(0, 0), out _), Is.True);
        }

        [Test]
        public void Bake_WithColliderSurface_SnapsTilePositionsToSurface()
        {
            _projectorHost.transform.SetPositionAndRotation(new Vector3(0f, 4f, 0f), Quaternion.Euler(90f, 0f, 0f));
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            surface.layer = 3;
            Physics.SyncTransforms();
            List<HexTile> tiles = new();
            HexTileStore store = new(tiles);

            store.Bake(_projector, 0.1f, 1, 1 << 3);

            Assert.That(tiles[0].TilePosition.y, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(surface);
        }
    }
}
