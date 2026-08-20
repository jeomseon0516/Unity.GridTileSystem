using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Jeomseon.Collections;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexGridTileDataStore : IHexGridTileDataStore
    {
        private const float SquareRootThree = 1.732051f;
        private const float SquareRootThreeHalf = SquareRootThree * 0.5f;

        private readonly List<HexGrid> _hexGrids;
        private readonly Dictionary<AxialCoordinates, HexGrid> _lookup = new();

        public event Action TileVisualsChanged;

        public HexGridTileDataStore(List<HexGrid> hexGrids)
        {
            _hexGrids = hexGrids;
        }

        public IReadOnlyList<HexGrid> Grids => _hexGrids;

        public bool TryGetTile(in HexCoordinates coordinates, out HexGrid hexGrid)
            => _lookup.TryGetValue(coordinates, out hexGrid);

        public void SetActive(in AxialCoordinates coordinates, bool isActive)
        {
            if (!_lookup.TryGetValue(coordinates, out HexGrid hex)) return;

            hex.IsActive = isActive;
        }

        public void Rebuild(DecalProjector decalProjector, float hexagonRadius, int tileLimit)
        {
            Vector3 projectorSize = decalProjector.size * decalProjector.transform.localScale.x;

            for (int q = -tileLimit; q <= tileLimit; q++)
            {
                for (int r = -tileLimit; r <= tileLimit; r++)
                {
                    int s = -(q + r);
                    if (s >= -tileLimit && s <= tileLimit)
                    {
                        Vector2 hexNormalizedPosition = new(
                            hexagonRadius * (1.5f * q),
                            hexagonRadius * (SquareRootThreeHalf * q + SquareRootThree * r));

                        Vector3 hexPosition = projectorSize.x * hexNormalizedPosition;

                        hexPosition = new(
                            hexPosition.x + decalProjector.transform.position.x,
                            0.0f,
                            hexPosition.y + decalProjector.transform.position.z);

                        Ray ray = new(
                            new(
                                hexPosition.x,
                                decalProjector.transform.position.y + projectorSize.y * 0.5f,
                                hexPosition.z),
                            Vector3.down);

                        if (Physics.Raycast(ray, out RaycastHit hit, projectorSize.y, 1 << 3))
                        {
                            hexPosition = hit.point;
                        }

                        AxialCoordinates key = new(q, r);
                        HexGrid newHex = CreateTile(q, r, hexPosition, hexNormalizedPosition, tileLimit);

                        if (_lookup.TryGetValue(key, out HexGrid hex))
                        {
                            hex.Properties.ForEach(newHex.AddProperty);
                            newHex.IsActive = hex.IsActive;
                            newHex.Color = hex.Color;
                            int index = _hexGrids.IndexOf(hex);
                            _hexGrids[index] = newHex;
                            _lookup[key] = newHex;
                        }
                        else
                        {
                            _hexGrids.Add(newHex);
                            _lookup.Add(key, newHex);
                        }
                    }
                }
            }

            TileVisualsChanged?.Invoke();
        }

        public void RebuildLookup()
        {
            _lookup.Clear();

            foreach (HexGrid hexGrid in _hexGrids)
            {
                if (hexGrid is not null)
                {
                    _lookup[hexGrid.HexPoint] = hexGrid;
                }
            }
        }

        private HexGrid CreateTile(int q, int r, in Vector3 hexPosition, in Vector2 hexNormalizedPosition, int tileLimit)
        {
            HexGrid createdHex = new(q, r, hexPosition, hexNormalizedPosition + new Vector2(0.5f, 0.5f), tileLimit);
            createdHex.OnChangedActive += (_, _) => TileVisualsChanged?.Invoke();
            createdHex.OnChangedColor += (_, _) => TileVisualsChanged?.Invoke();
            return createdHex;
        }
    }
}
