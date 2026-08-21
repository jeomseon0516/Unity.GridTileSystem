#if UNITY_EDITOR
using System.Collections.Generic;
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
        private SerializedProperty _settingsProperty;
        private SerializedProperty _receiversProperty;
        private HexTileOptionOverlay _tileOptionOverlay;
        private Editor _settingsEditor;
        private GUIStyle _labelStyle;
        private HexTile _selectedTile;
        private bool _showSettings = true;

        private void OnEnable()
        {
            _controller = (HexGridController)target;
            _settingsProperty = serializedObject.FindProperty("settings");
            _receiversProperty = serializedObject.FindProperty("receivers");
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
            EditorGUILayout.LabelField("Baked Tile Count", _controller.TileCount.ToString());
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
            DrawInlineSettings();

            bool isConfigured = IsConfigured();
            if (!isConfigured)
            {
                EditorGUILayout.HelpBox(
                    "Assign Settings and at least one Receiver with a readable source Mesh/MeshCollider. Each Receiver's output components are optional, but must be assigned as a pair.",
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

            using (new EditorGUI.DisabledScope(_controller.TileCount == 0))
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
            if (_settingsProperty.objectReferenceValue == null || _controller.Receivers.Count == 0) return false;
            foreach (HexGridReceiver receiver in _controller.Receivers)
            {
                if (receiver == null || receiver.SurfaceCollider == null) continue;
                if (receiver.SurfaceKind == SurfaceReceiverKind.StaticMesh &&
                    receiver.SourceMeshFilter != null && receiver.SourceMeshFilter.sharedMesh != null) return true;
                if (receiver.SurfaceKind == SurfaceReceiverKind.Terrain &&
                    receiver.SourceTerrain != null && receiver.SourceTerrain.terrainData != null) return true;
            }
            return false;
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
                if (TryFindTileProperty(pickedTile, out SerializedProperty tileProperty))
                {
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

        private bool TryFindTileProperty(IHexTile tile, out SerializedProperty tileProperty)
        {
            for (int receiverIndex = 0; receiverIndex < _controller.Receivers.Count; receiverIndex++)
            {
                IReadOnlyList<HexTile> tiles = _controller.Receivers[receiverIndex]?.Tiles;
                if (tiles == null) continue;
                for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
                {
                    if (!ReferenceEquals(tiles[tileIndex], tile)) continue;
                    SerializedProperty receiverProperty = _receiversProperty.GetArrayElementAtIndex(receiverIndex);
                    tileProperty = receiverProperty.FindPropertyRelative("tiles").GetArrayElementAtIndex(tileIndex);
                    return true;
                }
            }
            tileProperty = null;
            return false;
        }
    }
}
#endif
