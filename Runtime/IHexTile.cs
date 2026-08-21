using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>게임 로직이 렌더링 구현을 몰라도 사용할 수 있는 Hex Tile 상태·상호작용 계약입니다.</summary>
    public interface IHexTile
    {
        /// <summary>사용자가 붙인 문자열 속성의 읽기 전용 목록입니다.</summary>
        IReadOnlyList<string> Properties { get; }
        /// <summary>선택과 시각화에 사용할 활성 상태입니다.</summary>
        bool IsActive { get; set; }
        /// <summary>타일 시각화 색상입니다.</summary>
        Color Color { get; set; }
        /// <summary>논리 Grid의 Cube 좌표입니다.</summary>
        HexCoordinates Coordinates { get; }
        /// <summary>타일 Surface 영역 중심의 월드 좌표 근삿값입니다.</summary>
        Vector3 TilePosition { get; }
        /// <summary>호환성을 위해 보존된 정규화 평면 좌표입니다.</summary>
        Vector2 NormalizedPosition { get; }
        /// <summary>Pointer가 타일에 진입할 때 발생합니다.</summary>
        event UnityAction<IHexTile> OnEnterTile;
        /// <summary>Pointer가 타일에서 이탈할 때 발생합니다.</summary>
        event UnityAction<IHexTile> OnExitTile;
        /// <summary>타일 위에서 Pointer down이 발생할 때 호출됩니다.</summary>
        event UnityAction<IHexTile> OnMouseDownTile;
        /// <summary>타일 위에서 Pointer up이 발생할 때 호출됩니다.</summary>
        event UnityAction<IHexTile> OnMouseUpTile;

        /// <summary>활성 상태가 변경될 때 발생합니다.</summary>
        event UnityAction<IHexTile, bool> OnChangedActive;
        /// <summary>색상이 변경될 때 발생합니다.</summary>
        event UnityAction<IHexTile, Color> OnChangedColor;
        /// <summary>사용자 문자열 속성을 추가합니다.</summary>
        void AddProperty(string property);
        /// <summary>값이 같은 사용자 문자열 속성을 모두 제거하고 제거 여부를 반환합니다.</summary>
        bool RemoveProperty(string property);
    }

}
