using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Jeomseon.Unity.Attributes;
using Jeomseon.Unity.Core.Events;
using Jeomseon.Unity.Core.Mathematics;

namespace Jeomseon.Unity.GridTileSystem
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HexTileRenderData
    {
        public Vector3 Color;
        public int IsActive;
    }

    [System.Serializable]
    public class HexTile : IHexTile
    {
        [field: SerializeField, ReadOnly] public Vector3 TilePosition { get; private set; } = Vector3.zero;
        [field: SerializeField, ReadOnly] public Vector2 NormalizedPosition { get; private set; } = Vector2.zero;
        [field: SerializeField, ReadOnly] public int Index { get; private set; } = 0;

        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                onChangedActive.Invoke(this, isActive);
            }
        }
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                onChangedColor.Invoke(this, color);
            }
        }

        public IReadOnlyList<string> Properties => properties;

        public HexCoordinates Coordinates => coordinates;

        [SerializeField, FormerlySerializedAs("_properties")] private List<string> properties = new();
        [SerializeField, ReadOnly, FormerlySerializedAs("hexPoint"), FormerlySerializedAs("_hexPoint")]
        private HexCoordinates coordinates = new();

        [SerializeField, FormerlySerializedAs("_isActive")] private bool isActive = true;
        [SerializeField, FormerlySerializedAs("_color")] private Color color = Color.cyan;

        public event UnityAction<IHexTile> OnEnterTile
        {
            add => onEnterTile.AddListener(value);
            remove => onEnterTile.RemoveListener(value);
        }

        public event UnityAction<IHexTile> OnExitTile
        {
            add => onExitTile.AddListener(value);
            remove => onExitTile.RemoveListener(value);
        }

        public event UnityAction<IHexTile> OnMouseDownTile
        {
            add => onMouseDownTile.AddListener(value);
            remove => onMouseDownTile.RemoveListener(value);
        }

        public event UnityAction<IHexTile> OnMouseUpTile
        {
            add => onMouseUpTile.AddListener(value);
            remove => onMouseUpTile.RemoveListener(value);
        }

        public event UnityAction<IHexTile, bool> OnChangedActive
        {
            add => onChangedActive.AddListener(value);
            remove => onChangedActive.RemoveListener(value);
        }

        public event UnityAction<IHexTile, Color> OnChangedColor
        {
            add => onChangedColor.AddListener(value);
            remove => onChangedColor.RemoveListener(value);
        }

        [FormerlySerializedAs("_onHighlightTile")]
        [Header("Interactable Events")]
        [SerializeField, FormerlySerializedAs("_onEnterTile")] private UnityEvent<IHexTile> onEnterTile = new();
        [SerializeField, FormerlySerializedAs("_onExitTile")] private UnityEvent<IHexTile> onExitTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseUpTile")] private UnityEvent<IHexTile> onMouseUpTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseDownTile")] private UnityEvent<IHexTile> onMouseDownTile = new();

        [Header("Value Changed Events")]
        [SerializeField, FormerlySerializedAs("_onChangedActive")] private UnityEvent<IHexTile, bool> onChangedActive = new();
        [SerializeField, FormerlySerializedAs("_onChangedColor")] private UnityEvent<IHexTile, Color> onChangedColor = new();

        public void AddProperty(string property)
        {
            properties.Add(property);
        }

        public bool RemoveProperty(string property)
        {
            return properties.RemoveAll(p => property == p) > 0;
        }

        public void InvokeOnEnterTile()
        {
            onEnterTile.Invoke(this);
        }

        public void InvokeOnExitTile()
        {
            onExitTile.Invoke(this);
        }

        public void InvokeOnMouseUpTile()
        {
            onMouseUpTile.Invoke(this);
        }

        public void InvokeOnMouseDownTile()
        {
            onMouseDownTile.Invoke(this);
        }

        internal HexTileRenderData GetRenderData() => new()
        {
            Color = color.ToRgbVector(),
            IsActive = isActive ? 1 : 0
        };

        public void SetTilePosition(in Vector3 tilePosition)
        {
            TilePosition = tilePosition;
        }

        public HexTile(int q, int r, in Vector3 tilePosition, in Vector2 normalizedPosition, int gridRadius)
            : this(q, r, tilePosition, normalizedPosition)
        {
            Index = CalculateIndex(gridRadius);
        }

        public HexTile(int q, int r, in Vector3 tilePosition, in Vector2 normalizedPosition) : this(q, r, tilePosition)
        {
            NormalizedPosition = normalizedPosition;
        }

        public HexTile(int q, int r, in Vector3 tilePosition) : this(q, r)
        {
            TilePosition = tilePosition;
        }

        public HexTile(in AxialCoordinates axialCoordinates)
        {
            coordinates = axialCoordinates;
        }

        /* TODO(P1-02, architecture): UnityEvent를 포함한 상호작용 상태와 순수 타일 데이터를 분리하여
         * 런타임 저장, 네트워크 동기화 및 단위 테스트에 사용할 수 있게 합니다.
         */
        public HexTile(int q, int r)
        {
            coordinates = new HexCoordinates(q, r);
            onChangedActive.SetPersistentListenerState(UnityEventCallState.EditorAndRuntime);
            onChangedColor.SetPersistentListenerState(UnityEventCallState.EditorAndRuntime);
        }

        public int CalculateIndex(int gridRadius)
        {
            int q = coordinates.Q;
            int r = coordinates.R;

            int rMin = Mathf.Max(-gridRadius, -q - gridRadius);
            int index = (2 * gridRadius + 1) * (q + gridRadius);

            for (int i = -gridRadius; i < q; i++)
            {
                index -= Mathf.Abs(i);
            }

            index += r - rMin;

            return index;
        }

        internal void CopyStateFrom(HexTile source)
        {
            properties.Clear();
            properties.AddRange(source.properties);
            isActive = source.isActive;
            color = source.color;
        }

        #if UNITY_EDITOR
        private void HandleActiveChangedInEditor()
        {
            onChangedActive.Invoke(this, isActive);
        }

        private void HandleColorChangedInEditor()
        {
            onChangedColor.Invoke(this, color);
        }
        #endif
    }
}
