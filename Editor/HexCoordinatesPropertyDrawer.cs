#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Jeomseon.Editor;

namespace Jeomseon.HexGrid.Editor
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
    }
}
#endif
