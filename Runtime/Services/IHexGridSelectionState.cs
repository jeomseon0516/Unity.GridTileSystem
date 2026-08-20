using System;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexGridSelectionState
    {
        event Action<IHexGrid> Entered;
        event Action<IHexGrid> Exited;
        event Action<IHexGrid> MouseDown;
        event Action<IHexGrid> MouseUp;

        void UpdateHover(HexGrid candidate);
        void NotifyMouseDown(HexGrid tile);
        void NotifyMouseUp(HexGrid tile);
    }
}
