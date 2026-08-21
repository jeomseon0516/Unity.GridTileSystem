using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Jeomseon.Unity.Core.Events;
using Jeomseon.Unity.Core.Mathematics;

namespace Jeomseon.Unity.GridTileSystem
{
    [StructLayout(LayoutKind.Sequential)]
    /// <summary>GPU 또는 Mesh Backend에 전달할 타일별 색상·활성 상태의 연속 메모리 표현입니다.</summary>
    public struct HexTileRenderData
    {
        /// <summary>알파를 제외한 선형 RGB 값입니다.</summary>
        public Vector3 Color;
        /// <summary>GPU 구조체 정렬이 명확하도록 bool 대신 사용하는 0 또는 1 값입니다.</summary>
        public int IsActive;
    }

    [System.Serializable]
    /// <summary>육각 좌표, 사용자 속성, 시각 상태와 pointer 이벤트를 보존하는 직렬화 타일입니다.</summary>
    public class HexTile : IHexTile
    {
        /// <summary>Surface Region 중심의 표시용 월드 위치를 가져옵니다.</summary>
        public Vector3 TilePosition => SafeData.TilePosition;
        /// <summary>호환성을 위해 보존한 intrinsic chart 중심 좌표를 가져옵니다.</summary>
        public Vector2 NormalizedPosition => SafeData.IntrinsicPosition;
        /// <summary>Bake 순서로 부여한 0 기반 직렬 인덱스를 가져옵니다.</summary>
        public int Index => SafeData.Index;
        /// <summary>Unity 이벤트와 분리된 순수 직렬화 데이터를 가져옵니다.</summary>
        public HexTileData Data => SafeData;

        /// <summary>선택·표현에 사용할 활성 상태를 가져오거나 설정합니다.</summary>
        public bool IsActive
        {
            get => SafeData.IsActive;
            set
            {
                SafeData.IsActive = value;
                onChangedActive.Invoke(this, value);
            }
        }
        /// <summary>타일 시각화 색상을 가져오거나 설정합니다.</summary>
        public Color Color
        {
            get => SafeData.Color;
            set
            {
                SafeData.Color = value;
                onChangedColor.Invoke(this, value);
            }
        }

        /// <summary>사용자가 추가한 문자열 속성 목록을 읽기 전용으로 노출합니다.</summary>
        public IReadOnlyList<string> Properties => SafeData.Properties;

        /// <summary>타일의 불변 논리 Cube 좌표를 가져옵니다.</summary>
        public HexCoordinates Coordinates => SafeData.Coordinates;

        /// <summary>Renderer와 UnityEvent를 참조하지 않는 저장·동기화 가능한 타일 상태입니다.</summary>
        [SerializeField] private HexTileData data = new(new AxialCoordinates(0, 0));
        /// <summary>이전 직렬화 형식에 data 필드가 없을 때 기본 데이터를 지연 복구합니다.</summary>
        private HexTileData SafeData => data ??= new HexTileData(new AxialCoordinates(0, 0));

        /// <summary>Pointer가 타일에 진입할 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnEnterTile
        {
            add => onEnterTile.AddListener(value);
            remove => onEnterTile.RemoveListener(value);
        }

        /// <summary>Pointer가 타일에서 이탈할 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnExitTile
        {
            add => onExitTile.AddListener(value);
            remove => onExitTile.RemoveListener(value);
        }

        /// <summary>타일 위에서 pointer down이 발생할 때 호출됩니다.</summary>
        public event UnityAction<IHexTile> OnMouseDownTile
        {
            add => onMouseDownTile.AddListener(value);
            remove => onMouseDownTile.RemoveListener(value);
        }

        /// <summary>타일 위에서 pointer up이 발생할 때 호출됩니다.</summary>
        public event UnityAction<IHexTile> OnMouseUpTile
        {
            add => onMouseUpTile.AddListener(value);
            remove => onMouseUpTile.RemoveListener(value);
        }

        /// <summary>활성 상태가 변경될 때 발생합니다.</summary>
        public event UnityAction<IHexTile, bool> OnChangedActive
        {
            add => onChangedActive.AddListener(value);
            remove => onChangedActive.RemoveListener(value);
        }

        /// <summary>색상이 변경될 때 발생합니다.</summary>
        public event UnityAction<IHexTile, Color> OnChangedColor
        {
            add => onChangedColor.AddListener(value);
            remove => onChangedColor.RemoveListener(value);
        }

        [FormerlySerializedAs("_onHighlightTile")]
        [Header("Interactable Events")]
        /// <summary>타일 진입 영구·런타임 리스너를 보존합니다.</summary>
        [SerializeField, FormerlySerializedAs("_onEnterTile")] private UnityEvent<IHexTile> onEnterTile = new();
        /// <summary>타일 이탈 영구·런타임 리스너를 보존합니다.</summary>
        [SerializeField, FormerlySerializedAs("_onExitTile")] private UnityEvent<IHexTile> onExitTile = new();
        /// <summary>Pointer up 영구·런타임 리스너를 보존합니다.</summary>
        [SerializeField, FormerlySerializedAs("_onMouseUpTile")] private UnityEvent<IHexTile> onMouseUpTile = new();
        /// <summary>Pointer down 영구·런타임 리스너를 보존합니다.</summary>
        [SerializeField, FormerlySerializedAs("_onMouseDownTile")] private UnityEvent<IHexTile> onMouseDownTile = new();

        [Header("Value Changed Events")]
        /// <summary>활성 상태 변경 리스너를 보존합니다.</summary>
        [SerializeField, FormerlySerializedAs("_onChangedActive")] private UnityEvent<IHexTile, bool> onChangedActive = new();
        /// <summary>색상 변경 리스너를 보존합니다.</summary>
        [SerializeField, FormerlySerializedAs("_onChangedColor")] private UnityEvent<IHexTile, Color> onChangedColor = new();

        /// <summary>사용자 문자열 속성을 추가합니다. 중복 값도 보존합니다.</summary>
        public void AddProperty(string property)
        {
            SafeData.AddProperty(property);
        }

        /// <summary>같은 값을 가진 문자열 속성을 모두 제거하고 하나 이상 제거했는지 반환합니다.</summary>
        public bool RemoveProperty(string property)
        {
            return SafeData.RemoveProperty(property);
        }

        /// <summary>타일 진입 이벤트를 Controller selection 순서에서 발생시킵니다.</summary>
        public void InvokeOnEnterTile()
        {
            onEnterTile.Invoke(this);
        }

        /// <summary>타일 이탈 이벤트를 Controller selection 순서에서 발생시킵니다.</summary>
        public void InvokeOnExitTile()
        {
            onExitTile.Invoke(this);
        }

        /// <summary>타일 pointer up 이벤트를 발생시킵니다.</summary>
        public void InvokeOnMouseUpTile()
        {
            onMouseUpTile.Invoke(this);
        }

        /// <summary>타일 pointer down 이벤트를 발생시킵니다.</summary>
        public void InvokeOnMouseDownTile()
        {
            onMouseDownTile.Invoke(this);
        }

        /// <summary>현재 색상과 활성 상태를 Backend 전송 구조체로 변환합니다.</summary>
        internal HexTileRenderData GetRenderData() => new()
        {
            Color = SafeData.Color.ToRgbVector(),
            IsActive = SafeData.IsActive ? 1 : 0
        };

        /// <summary>표시용 월드 위치를 갱신합니다. 타일 identity와 picking에는 영향을 주지 않습니다.</summary>
        public void SetTilePosition(in Vector3 tilePosition)
        {
            SafeData.SetTilePosition(tilePosition);
        }

        /// <summary>모든 표시 좌표와 Bake 순서 인덱스를 포함한 타일을 생성합니다.</summary>
        public HexTile(int q, int r, in Vector3 tilePosition, in Vector2 normalizedPosition, int index)
            : this(q, r)
        {
            data = new HexTileData(q, r, tilePosition, normalizedPosition, index);
        }

        /// <summary>논리 좌표, 월드 위치와 intrinsic chart 위치를 포함한 타일을 생성합니다.</summary>
        public HexTile(int q, int r, in Vector3 tilePosition, in Vector2 normalizedPosition) : this(q, r, tilePosition)
        {
            data = new HexTileData(q, r, tilePosition, normalizedPosition, 0);
        }

        /// <summary>논리 좌표와 표시용 월드 위치를 포함한 타일을 생성합니다.</summary>
        public HexTile(int q, int r, in Vector3 tilePosition) : this(q, r)
        {
            data = new HexTileData(q, r, tilePosition, Vector2.zero, 0);
        }

        /// <summary>Axial 좌표만으로 타일을 생성합니다.</summary>
        public HexTile(in AxialCoordinates axialCoordinates)
            : this(axialCoordinates.Q, axialCoordinates.R) { }

        /// <summary>q, r 논리 좌표로 기본 상태의 타일을 생성합니다.</summary>
        public HexTile(int q, int r)
        {
            data = new HexTileData(new AxialCoordinates(q, r));
            onChangedActive.SetPersistentListenerState(UnityEventCallState.EditorAndRuntime);
            onChangedColor.SetPersistentListenerState(UnityEventCallState.EditorAndRuntime);
        }

        /// <summary>재Bake된 같은 좌표 타일에 사용자 속성·활성·색상 상태를 복사합니다.</summary>
        internal void CopyStateFrom(HexTile source)
        {
            SafeData.CopyUserStateFrom(source.SafeData);
        }

        #if UNITY_EDITOR
        private void HandleActiveChangedInEditor()
        {
            onChangedActive.Invoke(this, SafeData.IsActive);
        }

        private void HandleColorChangedInEditor()
        {
            onChangedColor.Invoke(this, SafeData.Color);
        }
        #endif
    }
}
