using System;
using System.Collections.Generic;
using Jeomseon.Unity.Projector;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexTileStore : IHexTileStore
    {
        private const float SquareRootThree = 1.732051f;
        private const float SquareRootThreeHalf = SquareRootThree * 0.5f;

        private readonly List<HexTile> _tiles;
        private readonly Dictionary<AxialCoordinates, HexTile> _lookup = new();

        public event Action TileVisualsChanged;

        public HexTileStore(List<HexTile> tiles)
        {
            _tiles = tiles;
        }

        public IReadOnlyList<HexTile> Tiles => _tiles;

        public bool TryGetTile(in HexCoordinates coordinates, out HexTile tile)
            => _lookup.TryGetValue(coordinates, out tile);

        public void SetActive(in AxialCoordinates coordinates, bool isActive)
        {
            if (!_lookup.TryGetValue(coordinates, out HexTile hex)) return;

            hex.IsActive = isActive;
        }

        public void Bake(MeshProjector projector, float tileRadius, int gridRadius, LayerMask surfaceLayerMask)
        {
            RebuildLookup();
            Dictionary<AxialCoordinates, HexTile> previousTiles = new(_lookup);
            UnsubscribeFromTiles();
            _tiles.Clear();
            _lookup.Clear();

            Vector3 projectorSize = projector.Size;

            for (int q = -gridRadius; q <= gridRadius; q++)
            {
                for (int r = -gridRadius; r <= gridRadius; r++)
                {
                    int s = -(q + r);
                    if (s >= -gridRadius && s <= gridRadius)
                    {
                        Vector2 hexNormalizedPosition = new(
                            tileRadius * (1.5f * q),
                            tileRadius * (SquareRootThreeHalf * q + SquareRootThree * r));

                        Vector3 hexPosition = projector.transform.TransformPoint(new Vector3(
                            projectorSize.x * hexNormalizedPosition.x,
                            projectorSize.y * hexNormalizedPosition.y,
                            0f));
                        Vector3 rayOrigin = projector.transform.TransformPoint(new Vector3(
                            projectorSize.x * hexNormalizedPosition.x,
                            projectorSize.y * hexNormalizedPosition.y,
                            -projectorSize.z * 0.5f));
                        Vector3 rayDirection = projector.transform.forward;

                        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, projectorSize.z, surfaceLayerMask))
                        {
                            hexPosition = hit.point;
                        }

                        AxialCoordinates key = new(q, r);
                        HexTile tile = new(
                            q,
                            r,
                            hexPosition,
                            hexNormalizedPosition + new Vector2(0.5f, 0.5f),
                            gridRadius);

                        if (previousTiles.TryGetValue(key, out HexTile previousTile))
                        {
                            tile.CopyStateFrom(previousTile);
                        }

                        SubscribeToTile(tile);
                        _tiles.Add(tile);
                        _lookup.Add(key, tile);
                    }
                }
            }

            TileVisualsChanged?.Invoke();
        }

        public void Clear()
        {
            UnsubscribeFromTiles();
            _tiles.Clear();
            _lookup.Clear();
            TileVisualsChanged?.Invoke();
        }

        public void RebuildLookup()
        {
            _lookup.Clear();

            foreach (HexTile tile in _tiles)
            {
                if (tile is not null)
                {
                    _lookup[tile.Coordinates] = tile;
                    SubscribeToTile(tile);
                }
            }
        }

        private void SubscribeToTile(HexTile tile)
        {
            tile.OnChangedActive -= HandleTileActiveChanged;
            tile.OnChangedColor -= HandleTileColorChanged;
            tile.OnChangedActive += HandleTileActiveChanged;
            tile.OnChangedColor += HandleTileColorChanged;
        }

        private void UnsubscribeFromTiles()
        {
            foreach (HexTile tile in _tiles)
            {
                if (tile is null) continue;

                tile.OnChangedActive -= HandleTileActiveChanged;
                tile.OnChangedColor -= HandleTileColorChanged;
            }
        }

        private void HandleTileActiveChanged(IHexTile _, bool __) => TileVisualsChanged?.Invoke();

        private void HandleTileColorChanged(IHexTile _, Color __) => TileVisualsChanged?.Invoke();
    }
}
