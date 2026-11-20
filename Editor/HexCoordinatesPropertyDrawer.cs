#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Jeomseon.Unity.EditorToolkit.Editor;

namespace Jeomseon.Unity.GridTileSystem.Editor
{
    [CustomPropertyDrawer(typeof(HexCoordinates))]
    internal sealed class HexCoordinatesPropertyDrawer : PropertyDrawer
    {
        public readonly struct HexCoordinatesProp
        {
            public SerializedProperty QProp { get; }
            public SerializedProperty RProp { get; }
            public SerializedProperty SProp { get; }

            public HexCoordinatesProp(SerializedProperty qProp, SerializedProperty rProp, SerializedProperty sProp)
            {
                QProp = qProp;
                RProp = rProp;
                SProp = sProp;
            }
        }

        private readonly Dictionary<int, HexCoordinatesProp> _cachedHexCoordinatesProps = new();
        private GUIStyle _labelStyle = null;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new CoordinatesField(property.displayName);
            field.AddAxis("Q", property.FindPropertyRelative(SerializedPropertyReflection.GetBackingFieldName("Q")));
            field.AddAxis("R", property.FindPropertyRelative(SerializedPropertyReflection.GetBackingFieldName("R")));
            field.AddAxis("S", property.FindPropertyRelative(SerializedPropertyReflection.GetBackingFieldName("S")));
            return field;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int hash = property.GetHashCode();
            
            if (!_cachedHexCoordinatesProps.TryGetValue(hash, out HexCoordinatesProp props))
            {
                props = new(
                    property.FindPropertyRelative(SerializedPropertyReflection.GetBackingFieldName("Q")),
                    property.FindPropertyRelative(SerializedPropertyReflection.GetBackingFieldName("R")),
                    property.FindPropertyRelative(SerializedPropertyReflection.GetBackingFieldName("S")));

                _cachedHexCoordinatesProps.Add(hash, props);
            }
            
            _labelStyle ??= new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            
            using EditorGUI.PropertyScope propScope = new(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);
            float labelWidth = EditorGUIUtility.singleLineHeight;  // 각 라벨의 너비
            float fieldWidth = (position.width - labelWidth * 3) / 3f;

            // Q 필드
            Rect qRect = new(position.x, position.y, labelWidth, position.height);
            EditorGUI.LabelField(qRect, "Q", _labelStyle);
            Rect qFieldRect = new(position.x + labelWidth, position.y, fieldWidth, position.height);
            props.QProp.intValue = EditorGUI.IntField(qFieldRect, props.QProp.intValue);

            // R 필드
            Rect rRect = new(position.x + labelWidth + fieldWidth, position.y, labelWidth, position.height);
            EditorGUI.LabelField(rRect, "R", _labelStyle);
            Rect rFieldRect = new(position.x + (labelWidth + fieldWidth) + labelWidth, position.y, fieldWidth, position.height);
            props.RProp.intValue = EditorGUI.IntField(rFieldRect, props.RProp.intValue);

            // S 필드
            Rect sRect = new(position.x + (labelWidth + fieldWidth) * 2, position.y, labelWidth, position.height);
            EditorGUI.LabelField(sRect, "S", _labelStyle);
            Rect sFieldRect = new(position.x + (labelWidth + fieldWidth) * 2 + labelWidth, position.y, fieldWidth, position.height);
            props.SProp.intValue = EditorGUI.IntField(sFieldRect, props.SProp.intValue);
        }

        /// <summary>
        /// IMGUI의 PrefixLabel + 축 라벨/정수 필드 배치를 UI Toolkit에서 재현합니다. Inspector 라벨 폭
        /// 정렬을 다른 필드와 맞추기 위해 BaseField의 라벨·정렬 처리를 사용하며, 값 자체는 축별
        /// IntegerField가 각 SerializedProperty에 직접 바인딩해 보관합니다.
        /// </summary>
        private sealed class CoordinatesField : BaseField<int>
        {
            private readonly VisualElement _axes;

            public CoordinatesField(string label) : this(label, new VisualElement()) { }

            private CoordinatesField(string label, VisualElement axes) : base(label, axes)
            {
                _axes = axes;
                _axes.style.flexDirection = FlexDirection.Row;
                _axes.style.flexGrow = 1f;
                AddToClassList(alignedFieldUssClassName);
            }

            public void AddAxis(string axisName, SerializedProperty axisProperty)
            {
                _axes.Add(new Label(axisName)
                {
                    style =
                    {
                        width = EditorGUIUtility.singleLineHeight,
                        flexShrink = 0f,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                });

                var axisField = new IntegerField
                {
                    style =
                    {
                        flexGrow = 1f,
                        flexBasis = 0f,
                        minWidth = 0f
                    }
                };
                axisField.BindProperty(axisProperty);
                _axes.Add(axisField);
            }
        }
    }
}
#endif
