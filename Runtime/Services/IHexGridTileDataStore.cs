using System;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexGridTileDataStore
    {
        IReadOnlyList<HexGrid> Grids { get; }

        event Action TileVisualsChanged;

        bool TryGetTile(in HexCoordinates coordinates, out HexGrid hexGrid);
        void SetActive(in AxialCoordinates coordinates, bool isActive);
        void Rebuild(DecalProjector decalProjector, float hexagonRadius, int tileLimit);
        void RebuildLookup();
    }
}
