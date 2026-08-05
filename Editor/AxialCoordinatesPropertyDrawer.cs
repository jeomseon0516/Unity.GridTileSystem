#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Jeomseon.Editor;

namespace Jeomseon.HexGrid.Editor
{
    [CustomPropertyDrawer(typeof(AxialCoordinates))]
    internal sealed class AxialCoordinatesPropertyDrawer : PropertyDrawer
    {
        public readonly struct AxialCoordinatesProp
        {
            public SerializedProperty QProp { get; }
            public SerializedProperty RProp { get; }

            public AxialCoordinatesProp(SerializedProperty qProp, SerializedProperty rProp)
            {
                QProp = qProp;
                RProp = rProp;
            }
        }
        
        private GUIStyle _labelStyle = null;
        private readonly Dictionary<int, AxialCoordinatesProp> _cachedAxialCoordinatesProps = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _labelStyle ??= new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };

            int hash = property.GetHashCode();
            if (!_cachedAxialCoordinatesProps.TryGetValue(hash, out AxialCoordinatesProp props))
            {
                props = new(
                    property.FindPropertyRelative(SerializedPropertyReflection.GetBackingFieldName("Q")),
                    property.FindPropertyRelative(SerializedPropertyReflection.GetBackingFieldName("R")));

                _cachedAxialCoordinatesProps.Add(hash, props);
            }
            
            using EditorGUI.PropertyScope propScope = new(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            float labelWidth = EditorGUIUtility.singleLineHeight; // 각 라벨의 너비
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
        }
    }
}
#endif
