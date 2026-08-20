using System;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexGridSelectionState : IHexGridSelectionState
    {
        private HexGrid _currentHex;

        public event Action<IHexGrid> Entered;
        public event Action<IHexGrid> Exited;
        public event Action<IHexGrid> MouseDown;
        public event Action<IHexGrid> MouseUp;

        public void UpdateHover(HexGrid candidate)
        {
            if (candidate == _currentHex) return;

            HexGrid previous = _currentHex;
            _currentHex = candidate;

            if (candidate is not null)
            {
                candidate.InvokeOnEnterTile();
                Entered?.Invoke(candidate);
            }

            if (previous is not null)
            {
                previous.InvokeOnExitTile();
                Exited?.Invoke(previous);
            }
        }

        public void NotifyMouseDown(HexGrid tile)
        {
            if (tile is null) return;

            tile.InvokeOnMouseDownTile();
            MouseDown?.Invoke(tile);
        }

        public void NotifyMouseUp(HexGrid tile)
        {
            if (tile is null) return;

            tile.InvokeOnMouseUpTile();
            MouseUp?.Invoke(tile);
        }
    }
}
