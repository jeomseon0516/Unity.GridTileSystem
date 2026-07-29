using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Jeomseon.Attribute;
using Jeomseon.Extensions;

namespace Jeomseon.HexGrid
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HexOption
    {
        public Vector3 Color;
        public int IsActive;
    }

    [System.Serializable]
    public class HexGrid : IHexGrid
    {
        // TODO(리팩토링): UnityEvent를 포함한 상호작용 상태와 순수 타일 데이터를 분리하여
        // 런타임 저장, 네트워크 동기화 및 단위 테스트에 사용할 수 있게 합니다.
        [field: SerializeField, ReadOnly] public Vector3 TilePosition { get; private set; } = Vector3.zero;
        [field: SerializeField, ReadOnly] public Vector2 NormalizedPosition { get; private set; } = Vector2.zero;
        [field: SerializeField, ReadOnly] public int Index { get; private set; } = 0;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                _onChangedActive.Invoke(this, _isActive);
            }
        }
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                _onChangedColor.Invoke(this, _color);
            }
        }

        public IReadOnlyList<string> Properties => _properties;

        public HexCoordinates HexPoint => _hexPoint;

        [SerializeField] private List<string> _properties = new();
        [SerializeField, ReadOnly] private HexCoordinates _hexPoint = new();

        [SerializeField] private bool _isActive = true;
        [SerializeField] private Color _color = Color.cyan;

        public event UnityAction<IHexGrid> OnEnterTile
        {
            add => _onEnterTile.AddListener(value);
            remove => _onEnterTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnExitTile
        {
            add => _onExitTile.AddListener(value);
            remove => _onExitTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnMouseDownTile
        {
            add => _onMouseDownTile.AddListener(value);
            remove => _onMouseDownTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnMouseUpTile
        {
            add => _onMouseUpTile.AddListener(value);
            remove => _onMouseUpTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid, bool> OnChangedActive
        {
            add => _onChangedActive.AddListener(value);
            remove => _onChangedActive.RemoveListener(value);
        }

        public event UnityAction<IHexGrid, Color> OnChangedColor
        {
            add => _onChangedColor.AddListener(value);
            remove => _onChangedColor.RemoveListener(value);
        }

        [FormerlySerializedAs("_onHighlightTile")]
        [Header("Interactable Events")]
        [SerializeField] private UnityEvent<IHexGrid> _onEnterTile = new();
        [SerializeField] private UnityEvent<IHexGrid> _onExitTile = new();
        [SerializeField] private UnityEvent<IHexGrid> _onMouseUpTile = new();
        [SerializeField] private UnityEvent<IHexGrid> _onMouseDownTile = new();

        [Header("Value Changed Events")]
        [SerializeField] private UnityEvent<IHexGrid, bool> _onChangedActive = new();
        [SerializeField] private UnityEvent<IHexGrid, Color> _onChangedColor = new();

        public void AddProperty(string property)
        {
            _properties.Add(property);
        }

        public bool RemoveProperty(string property)
        {
            return _properties.RemoveByConditionAll(p => property == p);
        }

        public void InvokeOnEnterTile()
        {
            _onEnterTile.Invoke(this);
        }

        public void InvokeOnExitTile()
        {
            _onExitTile.Invoke(this);
        }

        public void InvokeOnMouseUpTile()
        {
            _onMouseUpTile.Invoke(this);
        }

        public void InvokeOnMouseDownTile()
        {
            _onMouseDownTile.Invoke(this);
        }

        public HexOption GetShaderOption() => new()
        {
            Color = _color.ToVec3(),
            IsActive = _isActive ? 1 : 0
        };

        public void SetTilePosition(in Vector3 tilePosition)
        {
            TilePosition = tilePosition;
        }

        public HexGrid(int q, int r, in Vector3 tilePosition, in Vector2 normalizedPosition, int limit) : this(q, r, tilePosition, normalizedPosition)
        {
            Index = GetHexIndex(limit);
        }

        public HexGrid(int q, int r, in Vector3 tilePosition, in Vector2 normalizedPosition) : this(q, r, tilePosition)
        {
            NormalizedPosition = normalizedPosition;
        }

        public HexGrid(int q, int r, in Vector3 tilePosition) : this(q, r)
        {
            TilePosition = tilePosition;
        }

        public HexGrid(in AxialCoordinates axialCoordinates)
        {
            _hexPoint = axialCoordinates;
        }

        public HexGrid(int q, int r)
        {
            _hexPoint = new(q, r);
            _onChangedActive.SetPersistentListenerState(UnityEventCallState.EditorAndRuntime);
            _onChangedColor.SetPersistentListenerState(UnityEventCallState.EditorAndRuntime);
        }

        public int GetHexIndex(int limit)
        {
            int q = _hexPoint.Q;
            int r = _hexPoint.R;

            int rMin = Mathf.Max(-limit, -q - limit);
            int index = (2 * limit + 1) * (q + limit);

            // TODO(검증): 좌표별 인덱스가 연속적이고 중복되지 않는지 경계값 단위 테스트를 보강합니다.
            for (int i = -limit; i < q; i++)
            {
                index -= Mathf.Abs(i);
            }

            index += r - rMin;

            return index;
        }

        #if UNITY_EDITOR
        private void HandleActiveChangedInEditor()
        {
            _onChangedActive.Invoke(this, _isActive);
        }

        private void HandleColorChangedInEditor()
        {
            _onChangedColor.Invoke(this, _color);
        }
        #endif
    }
}
