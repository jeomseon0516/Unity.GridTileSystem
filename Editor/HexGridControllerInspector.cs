#if UNITY_EDITOR
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jeomseon.Unity.GridTileSystem.Editor
{
    using Editor = UnityEditor.Editor;
    using Event = UnityEngine.Event;

    [CustomEditor(typeof(HexGridController))]
    internal sealed class HexGridControllerInspector : Editor
    {
        private HexGridController _controller;
        private SerializedProperty _settingsProperty;
        private SerializedProperty _tilesProperty;
        private HexTileOptionOverlay _tileOptionOverlay;
        private Editor _settingsEditor;
        private GUIStyle _labelStyle;
        private HexTile _selectedTile;
        private bool _showSettings = true;
        private ListView _tilesListView;
        private InfoField _tileCountField;
        private VisualElement _inlineSettingsHost;
        private HelpBox _missingSettingsHelpBox;
        private HelpBox _configuredHelpBox;
        private Button _rebuildButton;
        private Button _clearButton;

        private void OnEnable()
        {
            _controller = (HexGridController)target;
            _settingsProperty = serializedObject.FindProperty("settings");
            _tilesProperty = serializedObject.FindProperty("tiles");
            _tileOptionOverlay = new HexTileOptionOverlay
            {
                VisualsChanged = HandleSerializedTileVisualChanged
            };
            EditorApplication.delayCall += AddOverlay;
            EditorApplication.delayCall += EnsureRenderingPreview;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= AddOverlay;
            EditorApplication.delayCall -= EnsureRenderingPreview;
            _tileOptionOverlay?.Hide();
            SceneView.RemoveOverlayFromActiveView(_tileOptionOverlay);
            _tileOptionOverlay = null;
            _selectedTile = null;
            if (_settingsEditor != null) DestroyImmediate(_settingsEditor);
        }

        /// <summary>
        /// UI Toolkit 기반 Inspector 루트를 구성합니다. Bake된 Tile은 수백 개까지 늘어날 수 있고 각
        /// 원소마다 UnityEvent가 6개씩 있어, 기존 IMGUI 배열 그리기(DrawDefaultInspector)로는 화면에
        /// 보이지 않는 Tile까지 전부 컨트롤을 만들어 Inspector가 느려집니다. Tile 목록은
        /// virtualization을 지원하는 ListView로 그려 실제 보이는 행만 컨트롤을 만들도록 합니다.
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.RegisterCallback<SerializedPropertyChangeEvent>(HandleInspectorPropertyChanged);
            root.Add(CreateInspectorContent());
            root.Add(CreateTilesListView());
            return root;
        }

        /// <summary>
        /// IMGUI 호스트(UI Toolkit을 쓰지 않는 Inspector 경로)용 대체 경로입니다. UI Toolkit 경로와 같은
        /// 순서로 그리며, Tile 목록은 virtualization 없이 기본 배열 UI로 표시합니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawImguiInspector();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_tilesProperty, true);
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Tile 목록을 제외한 Inspector 본문을 UI Toolkit으로 구성합니다.</summary>
        private VisualElement CreateInspectorContent()
        {
            VisualElement content = new();
            _tileCountField = new InfoField("Baked Tile Count");
            content.Add(_tileCountField);

            // tiles는 아래 ListView가 따로 그리므로 여기서는 제외합니다.
            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (iterator.propertyPath == "tiles") continue;
                content.Add(new PropertyField(iterator.Copy()));
            }

            _inlineSettingsHost = new VisualElement();
            content.Add(_inlineSettingsHost);
            RebuildInlineSettings();
            content.TrackPropertyValue(_settingsProperty, _ => RebuildInlineSettings());

            _missingSettingsHelpBox = new HelpBox(
                "Assign Settings. The grid is discovered automatically from the seed position — no surface needs to be registered.",
                HelpBoxMessageType.Error);
            _configuredHelpBox = new HelpBox(
                "Tiles are created automatically from the seed position. Click a tile in the Scene view or in the list below to edit it.",
                HelpBoxMessageType.Info);
            content.Add(_missingSettingsHelpBox);
            content.Add(_configuredHelpBox);

            _rebuildButton = new Button(RebuildTiles) { text = "Rebuild Tiles" };
            _clearButton = new Button(() =>
            {
                if (EditorUtility.DisplayDialog(
                        "Clear Baked Tiles?",
                        "This removes serialized tile data. Tiles will be recreated automatically at runtime or when Rebuild Tiles is used.",
                        "Clear",
                        "Cancel"))
                {
                    ClearTiles();
                }
            }) { text = "Clear Baked Tiles" };
            content.Add(_rebuildButton);
            content.Add(_clearButton);

            RefreshInspectorState();
            content.TrackSerializedObjectValue(serializedObject, _ => RefreshInspectorState());
            return content;
        }

        /// <summary>Tile 수, 구성 안내와 버튼 활성 상태를 현재 Controller 상태에 맞춥니다.</summary>
        private void RefreshInspectorState()
        {
            if (_tileCountField == null || _controller == null) return;

            serializedObject.UpdateIfRequiredOrScript();
            _tileCountField.SetValueWithoutNotify(_controller.TileCount.ToString());
            bool isConfigured = _settingsProperty.objectReferenceValue != null;
            _missingSettingsHelpBox.style.display = isConfigured ? DisplayStyle.None : DisplayStyle.Flex;
            _configuredHelpBox.style.display = isConfigured ? DisplayStyle.Flex : DisplayStyle.None;
            _rebuildButton.SetEnabled(isConfigured);
            _clearButton.SetEnabled(_controller.TileCount != 0);
        }

        /// <summary>할당된 HexGridSettings의 Inspector를 접이식으로 함께 표시합니다.</summary>
        private void RebuildInlineSettings()
        {
            if (_inlineSettingsHost == null) return;

            _inlineSettingsHost.Clear();
            HexGridSettings gridSettings = _settingsProperty.objectReferenceValue as HexGridSettings;
            if (gridSettings == null) return;

            Foldout foldout = new() { text = "Hex Grid Settings", value = _showSettings };
            foldout.RegisterValueChangedCallback(change =>
            {
                if (change.target == foldout) _showSettings = change.newValue;
            });
            foldout.Add(new InspectorElement(gridSettings));
            // IMGUI 경로의 EndChangeCheck와 같이 Settings 편집 시 Controller와 Scene 뷰를 갱신합니다.
            foldout.TrackSerializedObjectValue(new SerializedObject(gridSettings), _ =>
            {
                if (_controller == null) return;
                EditorUtility.SetDirty(_controller);
                SceneView.RepaintAll();
            });
            _inlineSettingsHost.Add(foldout);
        }

        private void DrawImguiInspector()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Baked Tile Count", _controller.TileCount.ToString());
            // tiles는 아래 ListView가 따로 그리므로 여기서는 제외합니다.
            DrawPropertiesExcluding(serializedObject, "tiles");
            serializedObject.ApplyModifiedProperties();
            DrawInlineSettings();

            bool isConfigured = _settingsProperty.objectReferenceValue != null;
            if (!isConfigured)
            {
                EditorGUILayout.HelpBox(
                    "Assign Settings. The grid is discovered automatically from the seed position — no surface needs to be registered.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Tiles are created automatically from the seed position. Click a tile in the Scene view or in the list below to edit it.",
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

        /// <summary>Bake된 Tile 목록을 virtualization으로 그리는 접이식 ListView를 만듭니다.</summary>
        private ListView CreateTilesListView()
        {
            _tilesListView = new ListView
            {
                headerTitle = "Baked Tiles",
                showFoldoutHeader = true,
                showBorder = true,
                showAddRemoveFooter = false,
                reorderable = false,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = { maxHeight = 400 },
                makeItem = () => new PropertyField(),
                bindItem = (element, index) =>
                {
                    if ((uint)index >= (uint)_tilesProperty.arraySize)
                    {
                        ((PropertyField)element).Unbind();
                        element.style.display = DisplayStyle.None;
                        return;
                    }
                    element.style.display = DisplayStyle.Flex;
                    SerializedProperty tileElement = _tilesProperty.GetArrayElementAtIndex(index);
                    ((PropertyField)element).BindProperty(tileElement.FindPropertyRelative("data"));
                },
            };
            _tilesListView.BindProperty(_tilesProperty);
            _tilesListView.selectionChanged += selection =>
            {
                foreach (object item in selection)
                {
                    if (item is not int index) continue;
                    IReadOnlyList<HexTile> tiles = _controller.Tiles;
                    if ((uint)index >= (uint)(tiles?.Count ?? 0)) continue;
                    SelectTile(tiles[index]);
                    break;
                }
            };
            return _tilesListView;
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
            // Serialized array가 줄어든 직후 기존 binding은 이전 항목 수를 잠시 보존할 수 있습니다.
            // 이전 index로 Rebuild하지 않고 새 arraySize를 기준으로 binding 자체를 다시 구성합니다.
            if (_tilesListView != null)
            {
                _tilesListView.ClearSelection();
                _tilesListView.Unbind();
                _tilesListView.BindProperty(_tilesProperty);
            }
            RefreshInspectorState();
            Repaint();
        }

        private void AddOverlay()
        {
            if (_tileOptionOverlay == null) return;

            SceneView.AddOverlayToActiveView(_tileOptionOverlay);
            SceneView.RepaintAll();
        }

        private void EnsureRenderingPreview()
        {
            if (_controller == null || !_controller.isActiveAndEnabled) return;
            _controller.EnsureRenderingPreview();
            SceneView.RepaintAll();
        }

        private void HandleInspectorPropertyChanged(SerializedPropertyChangeEvent changeEvent)
        {
            string propertyPath = changeEvent.changedProperty?.propertyPath;
            if (string.IsNullOrEmpty(propertyPath) || !propertyPath.StartsWith("tiles", System.StringComparison.Ordinal))
                return;
            HandleSerializedTileVisualChanged();
        }

        /// <summary>
        /// SerializedProperty 편집은 HexTile의 public setter와 UnityEvent를 우회하므로 Editor에서 직접
        /// 현재 Color/Active/DrawPolicy를 Backend에 다시 적용합니다.
        /// </summary>
        private void HandleSerializedTileVisualChanged()
        {
            if (_controller == null) return;
            _controller.EnsureRenderingPreview();
            EditorUtility.SetDirty(_controller);
            SceneView.RepaintAll();
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 &&
                _controller.TryPickTileIncludingInactive(
                    HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition), out _, out IHexTile pickedTile) &&
                pickedTile is HexTile hexTile)
            {
                SelectTile(hexTile);
                currentEvent.Use();
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

            DrawTileOutlineGizmos();
        }

        /// <summary>실제 Draw Mode와 별개로 편집 중인 전체 Tile 경계를 표시합니다.</summary>
        private void DrawTileOutlineGizmos()
        {
            SurfaceGridGeometry geometry = _controller.DebugGeometry;
            Transform space = _controller.DebugGeometrySpace;
            if (geometry == null || space == null) return;

            IReadOnlyList<int> outline = geometry.OutlineIndices;
            if (outline.Count == 0) return;

            Vector3[] lines = new Vector3[outline.Count];
            Matrix4x4 localToWorld = space.localToWorldMatrix;
            for (int i = 0; i < outline.Count; i++)
                lines[i] = localToWorld.MultiplyPoint3x4(geometry.Positions[outline[i]]);

            Color previousColor = Handles.color;
            Handles.color = new Color(1f, 0.65f, 0f, 0.9f);
            Handles.DrawLines(lines);
            Handles.color = previousColor;
        }

        /// <summary>Scene View 클릭과 ListView 선택이 공유하는, 선택한 Tile을 Overlay에 표시하는 경로입니다.</summary>
        private void SelectTile(HexTile tile)
        {
            if (!TryFindTileProperty(tile, out SerializedProperty tileProperty)) return;
            // array element(HexTile) 전체가 아니라 실제 편집 대상 데이터(HexTileData)로 한 단계 더
            // 들어가 바인딩합니다. UnityEvent 필드가 나란히 있는 HexTile을 그대로 PropertyField에
            // 넘기면 Overlay에 불필요한 이벤트 리스너 UI까지 함께 뜹니다.
            SerializedProperty dataProperty = tileProperty.FindPropertyRelative("data");
            dataProperty.isExpanded = true;
            _selectedTile = tile;
            _tileOptionOverlay.ShowProperty(dataProperty);
            SceneView.RepaintAll();
        }

        /// <summary>IMGUI의 LabelField(label, value)와 같이 Inspector 라벨 폭에 맞춘 읽기 전용 값을 표시합니다.</summary>
        private sealed class InfoField : BaseField<string>
        {
            private readonly Label _valueLabel;

            public InfoField(string label) : this(label, new Label()) { }

            private InfoField(string label, Label valueLabel) : base(label, valueLabel)
            {
                _valueLabel = valueLabel;
                AddToClassList(alignedFieldUssClassName);
            }

            public override void SetValueWithoutNotify(string newValue)
            {
                base.SetValueWithoutNotify(newValue);
                _valueLabel.text = newValue;
            }
        }

        private bool TryFindTileProperty(IHexTile tile, out SerializedProperty tileProperty)
        {
            IReadOnlyList<HexTile> tiles = _controller.Tiles;
            for (int tileIndex = 0; tiles != null && tileIndex < tiles.Count; tileIndex++)
            {
                if (!ReferenceEquals(tiles[tileIndex], tile)) continue;
                tileProperty = _tilesProperty.GetArrayElementAtIndex(tileIndex);
                return true;
            }
            tileProperty = null;
            return false;
        }
    }
}
#endif
