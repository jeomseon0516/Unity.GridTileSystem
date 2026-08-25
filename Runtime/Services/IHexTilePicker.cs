using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexTilePicker
    {
        bool TryPick(in Ray ray, in LayerMask layerMask, out HexTilePickResult result);
    }
}
