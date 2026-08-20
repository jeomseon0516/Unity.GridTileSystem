using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexGridTilePicker
    {
        bool TryPick(in Ray ray, LayerMask layerMask, float hexagonRadius, out (bool, RaycastHit) hitTuple, out HexGrid hexGrid);
    }
}
