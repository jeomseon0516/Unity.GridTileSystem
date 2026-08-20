using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexTilePicker
    {
        bool TryPick(in Ray ray, LayerMask layerMask, float tileRadius, out (bool, RaycastHit) hitTuple, out HexTile tile);
    }
}
