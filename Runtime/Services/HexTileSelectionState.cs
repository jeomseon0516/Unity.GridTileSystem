using System;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexTileSelectionState : IHexTileSelectionState
    {
        private HexTile _currentTile;

        public event Action<IHexTile> Entered;
        public event Action<IHexTile> Exited;
        public event Action<IHexTile> MouseDown;
        public event Action<IHexTile> MouseUp;

        public void UpdateHover(HexTile candidate)
        {
            if (candidate == _currentTile) return;

            HexTile previous = _currentTile;
            _currentTile = candidate;

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

        public void Clear() => UpdateHover(null);

        public void NotifyMouseDown(HexTile tile)
        {
            if (tile is null) return;

            tile.InvokeOnMouseDownTile();
            MouseDown?.Invoke(tile);
        }

        public void NotifyMouseUp(HexTile tile)
        {
            if (tile is null) return;

            tile.InvokeOnMouseUpTile();
            MouseUp?.Invoke(tile);
        }
    }
}
