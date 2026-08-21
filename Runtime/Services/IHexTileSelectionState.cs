using System;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    /// <summary>hover와 pointer 이벤트 전이를 렌더링·Physics 구현과 분리하는 상태 계약입니다.</summary>
    public interface IHexTileSelectionState
    {
        /// <summary>새 타일에 진입했을 때 발생합니다.</summary>
        event Action<IHexTile> Entered;
        /// <summary>이전 타일에서 이탈했을 때 발생합니다.</summary>
        event Action<IHexTile> Exited;
        /// <summary>타일 위 pointer down을 알립니다.</summary>
        event Action<IHexTile> MouseDown;
        /// <summary>타일 위 pointer up을 알립니다.</summary>
        event Action<IHexTile> MouseUp;

        /// <summary>현재 hover 후보를 갱신합니다.</summary>
        void UpdateHover(HexTile candidate);
        /// <summary>현재 hover를 해제합니다.</summary>
        void Clear();
        /// <summary>유효 타일에 pointer down을 전달합니다.</summary>
        void NotifyMouseDown(HexTile tile);
        /// <summary>유효 타일에 pointer up을 전달합니다.</summary>
        void NotifyMouseUp(HexTile tile);
    }
}
