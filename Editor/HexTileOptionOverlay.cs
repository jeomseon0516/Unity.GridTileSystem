#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jeomseon.Unity.GridTileSystem.Editor
{
    [Overlay(typeof(SceneView), OverlayId, "Hex Tile Option", false)]
    internal sealed class HexTileOptionOverlay : Overlay
    {
        private const string OverlayId = "jeomseon.gridtilesystem.hextileoption";

        private VisualElement _root;
        private ScrollView _propertyScrollView;
        private SerializedProperty _hexProperty;
        public Action VisualsChanged { private get; set; }

        public HexTileOptionOverlay() { }

        public override VisualElement CreatePanelContent()
        {
            _root = new VisualElement { style = { minWidth = 220, maxHeight = 340 } };
            Slider opacitySlider = new("Content Opacity", 0.35f, 1f)
            {
                value = 0.95f,
                showInputField = true
            };
            opacitySlider.RegisterValueChangedCallback(change =>
                _propertyScrollView.style.opacity = change.newValue);
            _root.Add(opacitySlider);

            _propertyScrollView = new ScrollView { style = { height = 300, opacity = opacitySlider.value } };
            _root.Add(_propertyScrollView);
            RefreshContent();
            return _root;
        }

        public void ShowProperty(SerializedProperty hexProperty)
        {
            _hexProperty = hexProperty;
            displayed = true;
            RefreshContent();
        }

        public void Hide()
        {
            _hexProperty = null;
            displayed = false;
            _propertyScrollView?.Clear();
        }

        private void RefreshContent()
        {
            if (_propertyScrollView is null)
            {
                return;
            }

            _propertyScrollView.Clear();

            if (_hexProperty is null) return;

            PropertyField propertyField = new(_hexProperty);
            propertyField.RegisterCallback<SerializedPropertyChangeEvent>(_ => VisualsChanged?.Invoke());
            _propertyScrollView.Add(propertyField);
            propertyField.Bind(_hexProperty.serializedObject);
        }
    }
}
#endif
