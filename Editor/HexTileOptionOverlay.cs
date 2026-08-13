#if UNITY_EDITOR
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

        private ScrollView _scrollView;
        private SerializedProperty _hexProperty;

        public override VisualElement CreatePanelContent()
        {
            _scrollView = new ScrollView { style = { minWidth = 220, maxHeight = 300 } };
            RefreshContent();
            return _scrollView;
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
            _scrollView?.Clear();
        }

        private void RefreshContent()
        {
            if (_scrollView is null)
            {
                return;
            }

            _scrollView.Clear();

            if (_hexProperty is null)
            {
                return;
            }

            var propertyField = new PropertyField(_hexProperty);
            propertyField.Bind(_hexProperty.serializedObject);
            _scrollView.Add(propertyField);
        }
    }
}
#endif
