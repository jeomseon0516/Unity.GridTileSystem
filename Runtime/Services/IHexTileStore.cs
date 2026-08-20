using System;
using System.Collections.Generic;
using Jeomseon.Unity.Projector;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexTileStore
    {
        IReadOnlyList<HexTile> Tiles { get; }

        event Action TileVisualsChanged;

        bool TryGetTile(in HexCoordinates coordinates, out HexTile tile);
        void SetActive(in AxialCoordinates coordinates, bool isActive);
        void Bake(MeshProjector projector, float tileRadius, int gridRadius, LayerMask surfaceLayerMask);
        void Clear();
        void RebuildLookup();
    }
}
