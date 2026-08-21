#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Editor
{
    using Editor = UnityEditor.Editor;
    using Event = UnityEngine.Event;

    [CustomEditor(typeof(HexGridController))]
    internal sealed class HexGridControllerInspector : Editor
    {
        private HexGridController _controller;
        private SerializedProperty _tilesProperty;
        private SerializedProperty _settingsProperty;
        private HexTileOptionOverlay _tileOptionOverlay;
        private Editor _settingsEditor;
        private GUIStyle _labelStyle;
        private HexTile _selectedTile;
        private bool _showSettings = true;

        private void OnEnable()
        {
            _controller = (HexGridController)target;
            _tilesProperty = serializedObject.FindProperty("tiles");
            _settingsProperty = serializedObject.FindProperty("settings");
            _tileOptionOverlay = new HexTileOptionOverlay();
            EditorApplication.delayCall += AddOverlay;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= AddOverlay;
            _tileOptionOverlay?.Hide();
            SceneView.RemoveOverlayFromActiveView(_tileOptionOverlay);
            _tileOptionOverlay = null;
            _selectedTile = null;
            if (_settingsEditor != null) DestroyImmediate(_settingsEditor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Baked Tile Count", _tilesProperty.arraySize.ToString());
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
            DrawInlineSettings();

            bool isConfigured = IsConfigured();
            if (!isConfigured)
            {
                EditorGUILayout.HelpBox(
                    "Assign Settings and a readable source Mesh/MeshCollider. Output Mesh components are optional, but must be assigned as a pair.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Tiles are created automatically. Click a tile in the Scene view to edit it.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!isConfigured))
            {
                if (GUILayout.Button("Rebuild Tiles")) RebuildTiles();
            }

            using (new EditorGUI.DisabledScope(_tilesProperty.arraySize == 0))
            {
                if (GUILayout.Button("Clear Baked Tiles") &&
                    EditorUtility.DisplayDialog(
                        "Clear Baked Tiles?",
                        "This removes serialized tile data. Tiles will be recreated automatically at runtime or when Rebuild Tiles is used.",
                        "Clear",
                        "Cancel"))
                {
                    ClearTiles();
                }
            }
        }

        private void DrawInlineSettings()
        {
            HexGridSettings gridSettings = _settingsProperty.objectReferenceValue as HexGridSettings;
            if (gridSettings == null)
            {
                if (_settingsEditor != null) DestroyImmediate(_settingsEditor);
                return;
            }

            _showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showSettings, "Hex Grid Settings");
            if (_showSettings)
            {
                Editor.CreateCachedEditor(gridSettings, null, ref _settingsEditor);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _settingsEditor.OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(_controller);
                        SceneView.RepaintAll();
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void RebuildTiles()
        {
            Undo.RecordObject(_controller, "Rebuild Hex Tiles");
            _selectedTile = null;
            _tileOptionOverlay?.Hide();
            _controller.BakeTiles();
            FinishTileMutation();
        }

        private void ClearTiles()
        {
            Undo.RecordObject(_controller, "Clear Hex Tiles");
            _selectedTile = null;
            _controller.ClearTiles();
            _tileOptionOverlay?.Hide();
            FinishTileMutation();
        }

        private void FinishTileMutation()
        {
            serializedObject.Update();
            EditorUtility.SetDirty(_controller);
            SceneView.RepaintAll();
            Repaint();
        }

        private bool IsConfigured()
        {
            SerializedProperty sourceProperty = serializedObject.FindProperty("sourceMeshFilter");
            SerializedProperty colliderProperty = serializedObject.FindProperty("surfaceCollider");
            SerializedProperty outputFilterProperty = serializedObject.FindProperty("outputMeshFilter");
            SerializedProperty outputRendererProperty = serializedObject.FindProperty("outputMeshRenderer");
            MeshFilter source = sourceProperty.objectReferenceValue as MeshFilter;
            MeshCollider surface = colliderProperty.objectReferenceValue as MeshCollider;
            MeshFilter output = outputFilterProperty.objectReferenceValue as MeshFilter;
            bool hasOutputFilter = output != null;
            bool hasOutputRenderer = outputRendererProperty.objectReferenceValue != null;
            bool validOutput = hasOutputFilter == hasOutputRenderer && (!hasOutputFilter || !ReferenceEquals(source, output));
            return _settingsProperty.objectReferenceValue != null && source != null && source.sharedMesh != null &&
                   surface != null && surface.sharedMesh == source.sharedMesh && validOutput;
        }

        private void AddOverlay()
        {
            if (_tileOptionOverlay == null) return;

            SceneView.AddOverlayToActiveView(_tileOptionOverlay);
            SceneView.RepaintAll();
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 &&
                _controller.TryPickTile(
                    HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition), out _, out IHexTile pickedTile))
            {
                int index = FindTileIndex(pickedTile);
                if (index >= 0 && index < _tilesProperty.arraySize)
                {
                    SerializedProperty tileProperty = _tilesProperty.GetArrayElementAtIndex(index);
                    tileProperty.isExpanded = true;
                    _selectedTile = (HexTile)pickedTile;
                    _tileOptionOverlay.ShowProperty(tileProperty);
                    SceneView.RepaintAll();
                    currentEvent.Use();
                }
            }

            _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (_selectedTile != null)
            {
                HexCoordinates coordinates = _selectedTile.Coordinates;
                Handles.Label(
                    _selectedTile.TilePosition,
                    $"Tile ({coordinates.Q}, {coordinates.R}, {coordinates.S})",
                    _labelStyle);
            }
        }

        private int FindTileIndex(IHexTile tile)
        {
            for (int i = 0; i < _controller.Tiles.Count; i++)
            {
                if (ReferenceEquals(_controller.Tiles[i], tile)) return i;
            }

            return -1;
        }
    }
}
#endif
