#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Jeomseon.Editor.GUI;
using Jeomseon.Extensions;

namespace Jeomseon.HexGrid.Editor
{
    using Editor = UnityEditor.Editor;
    using Event = UnityEngine.Event;

    [CustomEditor(typeof(GridManager))]
    internal sealed class GridManagerInspector : Editor
    {
        // TODO(리팩토링): private 필드 리플렉션 접근을 제거하고 GridManager가 제공하는
        // 에디터 전용 읽기 API 또는 SerializedProperty 기반 구현으로 변경합니다.
        // TODO(확장): 다량의 타일 라벨은 가시 범위와 확대 수준에 따라 선별해 Scene View 부하를 줄입니다.
        public readonly struct HexGUIData
        {
            public Vector3 TilePosition { get; }
            public string Description { get; }

            public HexGUIData(in Vector3 tilePosition, string description)
            {
                TilePosition = tilePosition;
                Description = description;
            }
        }

        private List<HexGrid> _hexGrids = null;
        private readonly List<HexGUIData> _hexGridDescriptions = new();

        private GridManager _gridManager = null;
        private GUIStyle _style = null;
        private SerializedProperty _currentHexProperty = null;
        private SceneViewInnerWindow<GridManagerInspector> _hexOptionWindow = new();
        private Vector2 _scrollPosition = Vector2.zero;
        private int _tileCount = 0;

        private void OnEnable()
        {
            _gridManager = (target as GridManager)!;
            InitializeTile();
            _hexOptionWindow.OnEnable();
        }

        private void OnDisable()
        {
            _hexOptionWindow.OnDisable();
            _currentHexProperty = null;
        }

        private void InitializeTile()
        {
            _tileCount = _gridManager.TileCount;
            _hexGrids = (_gridManager
                .GetType()
                .GetField("_hexGrids", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_gridManager) as List<HexGrid>)!;

            _hexGridDescriptions.Clear();
            _hexGridDescriptions.Capacity = _hexGrids.Count;
            _hexGridDescriptions.AddRange(_hexGrids
                .Select(hex => new HexGUIData(
                    hex.TilePosition,
                    $"Q : {hex.HexPoint.Q} R : {hex.HexPoint.R} S : {hex.HexPoint.S} \n [{string.Join(", \n   ", hex.Properties)}]")));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"TileCount : {_tileCount}");

            DrawDefaultInspector();

            if (GUILayout.Button("Calculate Grids"))
            {
                _gridManager.CalculateTile();
                InitializeTile();
                EditorUtility.SetDirty(_gridManager);
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("Update Option"))
            {
                _gridManager.SendToShaderHexOption();
                serializedObject.ApplyModifiedProperties();
            }
            
            if (GUILayout.Button("Clear Tiles"))
            {
                _hexGrids.Clear();
                InitializeTile();
                EditorUtility.SetDirty(_gridManager);
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("Create Object") && _gridManager.RootObject)
            {
                if (!_gridManager.RootObject)
                {
                    Debug.LogWarning("Grid Manager의 Root Object가 설정되어 있지 않습니다 ");
                }
                else
                {
                    Undo.RecordObject(_gridManager.RootObject, "GridManager.RootObject");
                    GameObject[] children = new GameObject[_gridManager.RootObject.transform.childCount];
                    
                    for (int i = 0; i < _gridManager.RootObject.transform.childCount; i++)
                    {
                        children[i] = _gridManager.RootObject.transform.GetChild(i).gameObject;
                    }
                    
                    children.ForEach(DestroyImmediate);
                    
                    _hexGrids
                        .Select(grid => new GameObject($"Index_{grid.Index}"))
                        .ForEach(player => player.SetParent(_gridManager.RootObject, false));
                }
            }
        }

        private void OnSceneGUI()
        {
            _style ??= new()
            {
                normal =
                {
                    textColor = Color.black
                },
                alignment = TextAnchor.MiddleCenter
            };

            Event currentEvent = Event.current;
            if (_currentHexProperty is not null)
            {
                _hexOptionWindow.OnSceneGUI(id =>
                {
                    using EditorGUILayout.ScrollViewScope scrollScope = new(_scrollPosition);
                    _scrollPosition = scrollScope.scrollPosition;

                    using EditorGUI.ChangeCheckScope changeCheckScope = new();
                    EditorGUILayout.PropertyField(_currentHexProperty, true);
                    
                    if (changeCheckScope.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                });
            }
            
            if (!_hexOptionWindow.IsUse && 
                currentEvent.type == EventType.MouseUp && 
                currentEvent.button == 0 && 
                _gridManager.TryGetTileDataByRay(HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition), out (bool, RaycastHit) _, out IHexGrid hexGrid))
            {
                int index = _hexGrids.IndexOf((HexGrid)hexGrid);

                _currentHexProperty = serializedObject
                    .FindProperty("_hexGrids")
                    .GetArrayElementAtIndex(index);

                _currentHexProperty.isExpanded = true;
                currentEvent.Use();
            }
            
            Handles.BeginGUI();
            foreach (HexGUIData hexMeta in _hexGridDescriptions)
            {
                Vector3 guiPosition = HandleUtility.WorldToGUIPoint(hexMeta.TilePosition);
                GUI.Label(new(guiPosition.x - 25, guiPosition.y - 15, 50, 30), hexMeta.Description, _style);
                // GUI 그리기 종료
            }
            Handles.EndGUI();
        }
    }
}
#endif
