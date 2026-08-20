using System;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexTileSelectionState
    {
        event Action<IHexTile> Entered;
        event Action<IHexTile> Exited;
        event Action<IHexTile> MouseDown;
        event Action<IHexTile> MouseUp;

        void UpdateHover(HexTile candidate);
        void Clear();
        void NotifyMouseDown(HexTile tile);
        void NotifyMouseUp(HexTile tile);
    }
}
