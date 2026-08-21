using System;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    /// <summary>현재 hover 타일과 enter/exit/down/up 이벤트 순서를 관리하는 순수 상태 서비스입니다.</summary>
    public sealed class HexTileSelectionState : IHexTileSelectionState
    {
        /// <summary>현재 pointer가 머무는 타일이며 없으면 null입니다.</summary>
        private HexTile _currentTile;

        /// <summary>새 타일 진입 후 발생합니다.</summary>
        public event Action<IHexTile> Entered;
        /// <summary>이전 타일 이탈 후 발생합니다.</summary>
        public event Action<IHexTile> Exited;
        /// <summary>유효 타일의 pointer down 후 발생합니다.</summary>
        public event Action<IHexTile> MouseDown;
        /// <summary>유효 타일의 pointer up 후 발생합니다.</summary>
        public event Action<IHexTile> MouseUp;

        /// <summary>hover 후보를 교체하고 새 타일 enter 뒤 이전 타일 exit 순서로 알립니다.</summary>
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

        /// <summary>현재 hover를 해제하고 필요한 exit 이벤트를 발생시킵니다.</summary>
        public void Clear() => UpdateHover(null);

        /// <summary>null이 아닌 타일에 pointer down 이벤트를 전달합니다.</summary>
        public void NotifyMouseDown(HexTile tile)
        {
            if (tile is null) return;

            tile.InvokeOnMouseDownTile();
            MouseDown?.Invoke(tile);
        }

        /// <summary>null이 아닌 타일에 pointer up 이벤트를 전달합니다.</summary>
        public void NotifyMouseUp(HexTile tile)
        {
            if (tile is null) return;

            tile.InvokeOnMouseUpTile();
            MouseUp?.Invoke(tile);
        }
    }
}
