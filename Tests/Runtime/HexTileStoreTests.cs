using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Services;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexTileStoreTests
    {
        private GameObject _surfaceHost;
        private SurfaceTopology _topology;
        private SurfaceGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _surfaceHost = new GameObject(nameof(HexTileStoreTests));
            _topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(51),
                new[]
                {
                    new Vector3(-10f, -10f, 0f), new Vector3(10f, -10f, 0f),
                    new Vector3(-10f, 10f, 0f), new Vector3(10f, 10f, 0f)
                },
                new[] { 0, 1, 2, 2, 1, 3 });
            SurfacePoint seed = new(_topology.Handle, 0, new Vector3(0.2f, 0.4f, 0.4f));
            _grid = SurfaceGridBuilder.Build(_topology, seed, 0.5f, 1, SurfacePatchBuildSettings.Unlimited);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_surfaceHost);

        [Test]
        public void Bake_GridRadiusOne_CreatesSevenUniqueTilesAndPopulatesLookup()
        {
            List<HexTile> tiles = new();
            HexTileStore store = CreateStore(tiles);
            Assert.That(tiles, Has.Count.EqualTo(7));
            Assert.That(store.TryGetTile(new HexCoordinates(0, 0), out _), Is.True);
            Assert.That(store.TryGetTile(new HexCoordinates(5, 5), out _), Is.False);
        }

        [Test]
        public void Bake_CalledTwice_ReplacesTilesAndPreservesUserState()
        {
            List<HexTile> tiles = new();
            HexTileStore store = CreateStore(tiles);
            store.TryGetTile(new HexCoordinates(0, 0), out HexTile first);
            first.AddProperty("marker");
            store.Bake(_topology, _grid, _surfaceHost.transform, 1);

            store.TryGetTile(new HexCoordinates(0, 0), out HexTile second);
            Assert.That(tiles, Has.Count.EqualTo(7));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Properties, Does.Contain("marker"));
        }

        [Test]
        public void SetActive_ForKnownCoordinates_UpdatesTileIsActive()
        {
            List<HexTile> tiles = new();
            HexTileStore store = CreateStore(tiles);
            store.SetActive(new AxialCoordinates(0, 0), false);
            store.TryGetTile(new HexCoordinates(0, 0), out HexTile tile);
            Assert.That(tile.IsActive, Is.False);
        }

        [Test]
        public void SetActive_ForUnknownCoordinates_DoesNothing()
        {
            HexTileStore store = new(new List<HexTile>());
            Assert.DoesNotThrow(() => store.SetActive(new AxialCoordinates(99, 99), false));
        }

        [Test]
        public void TileVisualsChanged_FiresOnBakeAndVisualStateChanges()
        {
            List<HexTile> tiles = new();
            HexTileStore store = new(tiles);
            int fireCount = 0;
            store.TileVisualsChanged += () => fireCount++;
            store.Bake(_topology, _grid, _surfaceHost.transform, 1);
            Assert.That(fireCount, Is.EqualTo(1));
            tiles[0].IsActive = false;
            tiles[0].Color = Color.red;
            Assert.That(fireCount, Is.EqualTo(3));
        }

        [Test]
        public void RebuildLookup_ManualList_PopulatesCoordinates()
        {
            HexTileStore store = new(new List<HexTile> { new(0, 0), new(1, 0) });
            store.RebuildLookup();
            Assert.That(store.TryGetTile(new HexCoordinates(0, 0), out _), Is.True);
            Assert.That(store.TryGetTile(new HexCoordinates(1, 0), out _), Is.True);
        }

        [Test]
        public void Bake_SurfaceTransform_TransformsDisplayPositionToWorld()
        {
            List<HexTile> identityTiles = new();
            CreateStore(identityTiles);
            Vector3 identityPosition = identityTiles[0].TilePosition;
            _surfaceHost.transform.position = new Vector3(0f, 4f, 0f);
            List<HexTile> translatedTiles = new();
            CreateStore(translatedTiles);
            Assert.That(translatedTiles[0].TilePosition, Is.EqualTo(identityPosition + Vector3.up * 4f));
        }

        private HexTileStore CreateStore(List<HexTile> tiles)
        {
            HexTileStore store = new(tiles);
            store.Bake(_topology, _grid, _surfaceHost.transform, 1);
            return store;
        }
    }
}
