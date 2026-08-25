using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Services;
using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using Jeomseon.Unity.GridTileSystem.Surface.Query;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>
    /// 월드 seed 위치 하나에서 intrinsic Hex Grid를 만들고 상호작용을 조합합니다. Surface를 등록하거나
    /// 지정하는 단계는 없으며, seed 주변에서 시스템이 표면을 찾고 맞닿은 표면까지 Grid를 이어갑니다.
    /// </summary>
    [ExecuteAlways]
    public sealed class HexGridController : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField, FormerlySerializedAs("_onEnterTile"), FormerlySerializedAs("_onHighlightTile")] private UnityEvent<IHexTile> onEnterTile = new();
        [SerializeField, FormerlySerializedAs("_onExitTile")] private UnityEvent<IHexTile> onExitTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseUpTile")] private UnityEvent<IHexTile> onMouseUpTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseDownTile")] private UnityEvent<IHexTile> onMouseDownTile = new();

        [Header("Settings")]
        [SerializeField] private HexGridSettings settings;
        [SerializeField, FormerlySerializedAs("_mainCamera")] private Camera mainCamera;

        [Header("Seed")]
        [Tooltip("Grid를 시작할 기준입니다. 비워 두면 이 GameObject의 Transform을 사용합니다.")]
        [SerializeField] private Transform seedAnchor;
        [Tooltip("기준에서 더할 월드 오프셋입니다.")]
        [SerializeField] private Vector3 seedOffset;
        [Tooltip("격자를 정렬할 월드 방향입니다. 영벡터면 회전 없이 chart 기본 방향을 씁니다.")]
        [SerializeField] private Vector3 initialDirection;
        [Header("Output")]
        [SerializeField] private MeshFilter outputMeshFilter;
        [SerializeField] private MeshRenderer outputMeshRenderer;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.002f;

        // HideInInspector를 쓰지 않습니다. UI Toolkit의 PropertyField는 자식 element의 SerializedProperty
        // 경로에 HideInInspector 필드가 조상으로 있으면 예외 없이 빈 트리를 만듭니다(HexTileOptionOverlay가
        // Scene View에서 선택한 개별 Tile을 편집하려면 바로 이 배열의 자식 property를 바인딩해야 하므로
        // 충돌합니다). 기본 Inspector에서는 HexGridControllerInspector가 DrawPropertiesExcluding으로
        // 이 필드만 명시적으로 제외합니다.
        [SerializeField] private List<HexTile> tiles = new();

        private HexGridSettings _subscribedSettings;
        private IHexGridPointerInput _pointerInput;
        private IHexTileSelectionState _selectionState;
        private HexTileStore _tileStore;
        private GeometrySurfaceQuery _query;
        private SurfaceGridSystem _system;
        private ISurfaceGridRenderBackend _renderBackend;
        private readonly List<HexTilePicker> _pickers = new();
        private SurfaceSkinBinding _skinBinding;
        private SkinnedMeshRenderer _skinnedRenderer;
        private Matrix4x4[] _skinningMatrices;
        private Vector3[] _deformedPositions;
        private Vector3[] _deformedNormals;
        private bool _isBaking;
        private bool _renderingEnabled = true;

        /// <summary>Pointer가 Tile에 들어왔을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnEnterTile
        {
            add => (onEnterTile ??= new UnityEvent<IHexTile>()).AddListener(value);
            remove => onEnterTile?.RemoveListener(value);
        }
        /// <summary>Pointer가 Tile에서 나갔을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnExitTile
        {
            add => (onExitTile ??= new UnityEvent<IHexTile>()).AddListener(value);
            remove => onExitTile?.RemoveListener(value);
        }
        /// <summary>Tile 위에서 pointer down이 발생했을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnMouseDownTile
        {
            add => (onMouseDownTile ??= new UnityEvent<IHexTile>()).AddListener(value);
            remove => onMouseDownTile?.RemoveListener(value);
        }
        /// <summary>Tile 위에서 pointer up이 발생했을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnMouseUpTile
        {
            add => (onMouseUpTile ??= new UnityEvent<IHexTile>()).AddListener(value);
            remove => onMouseUpTile?.RemoveListener(value);
        }

        /// <summary>현재 Grid의 Tile 개수를 가져옵니다.</summary>
        public int TileCount => tiles?.Count ?? 0;
        /// <summary>현재 Grid의 Tile 상태 목록을 가져옵니다.</summary>
        public IReadOnlyList<HexTile> Tiles => tiles;
        /// <summary>마지막으로 성공한 intrinsic Grid snapshot을 가져옵니다.</summary>
        public SurfaceGrid SurfaceGrid { get; private set; }
        /// <summary>Editor의 전체 Tile 경계 Gizmo가 사용하는 마지막 Geometry snapshot입니다.</summary>
        internal SurfaceGridGeometry DebugGeometry { get; private set; }
        /// <summary><see cref="DebugGeometry"/> 좌표가 속한 local 공간의 Transform입니다.</summary>
        internal Transform DebugGeometrySpace => outputMeshFilter != null ? outputMeshFilter.transform : null;

        /// <summary>Grid가 이번에 사용한 seed 지점을 가져옵니다.</summary>
        public SurfacePoint Seed { get; private set; }
        /// <summary>intrinsic Tile 반지름을 가져오거나 설정합니다.</summary>
        public float TileRadius { get => settings.TileRadius; set => settings.TileRadius = value; }
        /// <summary>Surface picking에 사용하는 Physics layer mask를 가져오거나 설정합니다.</summary>
        public LayerMask InteractionLayerMask { get => settings.InteractionLayerMask; set => settings.InteractionLayerMask = value; }
        /// <summary>Scale 적용 전 intrinsic Hex의 꼭짓점 간 너비를 가져옵니다.</summary>
        public float TileWidth => TileRadius * 2f;
        /// <summary>Grid를 시작할 월드 위치를 가져옵니다.</summary>
        public Vector3 SeedPosition => (seedAnchor != null ? seedAnchor.position : transform.position) + seedOffset;
        /// <summary>Grid 표현의 활성 상태를 가져오거나 설정합니다.</summary>
        public bool IsRenderingEnabled
        {
            get => _renderingEnabled;
            set { _renderingEnabled = value; _renderBackend?.SetRenderingEnabled(value); }
        }
        private void OnEnable()
        {
            EnsureServices();
            SubscribeToSettings();
            if (settings == null) return;
            // Edit Mode의 빈 목록은 사용자가 Clear Baked Tiles로 확정한 상태입니다. 직렬화 Tile이
            // 있을 때만 domain reload 뒤 실제 Preview Geometry를 복구하고, Play Mode는 비어 있어도
            // 실행에 필요한 Grid를 자동 생성합니다.
            if (Application.isPlaying || TileCount > 0) BakeTiles();
        }
        private void OnDisable()
        {
            UnsubscribeFromSettings();
            _selectionState?.Clear();
            ReleaseGrid();
        }
        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (Application.isPlaying && mainCamera == null) Debug.LogWarning($"{nameof(HexGridController)} could not find a Main Camera.", this);
            // Enter Play Mode에서 domain/scene reload를 끄면 직렬화 Tile 목록은 남지만 Editor 전용
            // lifecycle 중 파괴된 runtime Mesh/Backend는 복원되지 않을 수 있습니다. 정상 OnEnable Bake
            // 결과가 있으면 건드리지 않고, logical snapshot 또는 출력 Mesh가 유실된 경우에만 재구축합니다.
            if (Application.isPlaying && settings != null &&
                (SurfaceGrid == null || outputMeshFilter != null && outputMeshFilter.sharedMesh == null))
            {
                BakeTiles();
            }
        }
        private void Update()
        {
            if (!Application.isPlaying || mainCamera == null || settings == null) return;
            if (!_pointerInput.TryGetPointer(out Vector2 screenPosition, out bool pressed, out bool released)) return;
            ProcessPointer(screenPosition, pressed, released);
        }

        /// <summary>
        /// 변형되는 Surface 위 Grid를 현재 프레임 자세로 갱신합니다. Animator가 bone Transform을
        /// 확정하는 시점이 Update 이후이므로 LateUpdate에서 실행해야 한 프레임 밀리지 않습니다.
        /// </summary>
        private void LateUpdate()
        {
            if (Application.isPlaying) UpdateDeformation();
        }

        private void OnValidate()
        {
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            SubscribeToSettings();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSettings();
            ReleaseServices();
        }

        /// <summary>Seed 위치에서 표면을 찾아 Grid를 다시 만듭니다.</summary>
        public void BakeTiles()
        {
            if (settings == null) { Debug.LogError($"{nameof(HexGridController)} requires a {nameof(HexGridSettings)} asset.", this); return; }
            if (!HasValidOutputPair) { Debug.LogError($"{nameof(HexGridController)} must assign both output components or neither.", this); return; }
            EnsureServices();
            _isBaking = true;
            try
            {
                _selectionState.Clear();
                ReleaseGridState();
                // 이전 Bake의 연결 결과와 Adapter/topology를 함께 버려 Scene 변경이 반영되게 합니다.
                _system.Clear();

                SurfaceGridRequest request = new(
                    SeedPosition,
                    settings.TileRadius,
                    initialDirection,
                    settings.PatchBuildSettings,
                    settings.QueryOptions);

                SurfaceGridBuildResult result = _system.Build(request);
                if (result.Grid == null)
                {
                    Debug.LogError($"{nameof(HexGridController)} could not build a grid ({result.Status}): {result.Diagnostic}", this);
                    _tileStore.Clear();
                    return;
                }

                SurfaceGrid = result.Grid;
                Seed = result.Seed;
                if (!result.IsSuccess) Debug.LogWarning($"{nameof(HexGridController)} built an empty grid ({result.Status}): {result.Diagnostic}", this);
                if (SurfaceGrid.Patch.WasTruncated) Debug.LogWarning($"{nameof(HexGridController)} Surface Patch reached its configured limit.", this);
                if (SurfaceGrid.Patch.ClosureToleranceExceeded) Debug.LogWarning($"{nameof(HexGridController)} Surface Patch exceeded closure tolerance.", this);

                _tileStore.Bake(_query, _query, SurfaceGrid);
                BuildPickers();
                BuildGeometry();
            }
            catch (Exception exception)
            {
                _tileStore.Clear();
                ReleaseGridState();
                Debug.LogException(exception, this);
            }
            finally { _isBaking = false; }
            RefreshRendering();
        }

        /// <summary>Logical Tile과 생성 Geometry를 지웁니다.</summary>
        public void ClearTiles()
        {
            EnsureServices();
            _isBaking = true;
            try { _selectionState.Clear(); _tileStore.Clear(); ReleaseGridState(); }
            finally { _isBaking = false; }
        }

        /// <summary>Geometry를 재생성하지 않고 현재 Tile 시각 상태를 적용합니다.</summary>
        public void RefreshRendering()
        {
            if (_isBaking || _renderBackend == null || SurfaceGrid == null) return;
            SurfaceTileVisual[] visuals = new SurfaceTileVisual[tiles.Count];
            for (int i = 0; i < visuals.Length; i++)
            {
                HexTile tile = tiles[i];
                IHexTileDrawPolicy policy = tile.Data.DrawPolicy ?? settings.DefaultDrawPolicy;
                SurfaceGridDrawMode mode = policy != null ? policy.DrawMode : SurfaceGridDrawMode.Fill;
                visuals[i] = new SurfaceTileVisual(tile.Color, tile.IsActive, mode);
            }
            _renderBackend.ApplyVisuals(visuals);
        }

        /// <summary>
        /// Editor가 실제 Mesh Preview를 요청할 때 유실된 비직렬화 Backend를 복구합니다. 정상 Preview가
        /// 있으면 Geometry를 다시 만들지 않고 현재 직렬화 시각 상태만 적용합니다.
        /// </summary>
        internal void EnsureRenderingPreview()
        {
            if (settings == null || !HasValidOutputPair || outputMeshFilter == null) return;
            if (!Application.isPlaying && TileCount == 0) return;
            if (SurfaceGrid == null || _renderBackend == null || outputMeshFilter.sharedMesh == null)
            {
                BakeTiles();
                return;
            }

            RefreshRendering();
        }

        /// <summary>Axial 좌표 Tile의 활성 상태를 변경합니다.</summary>
        public void SetTileActive(int q, int r, bool isActive)
        {
            EnsureServices();
            _tileStore.SetActive(new AxialCoordinates(q, r), isActive);
        }

        /// <summary>Ray가 지나는 표면들 가운데 가장 가까운 활성 Tile을 반환합니다.</summary>
        public bool TryPickTile(in Ray ray, out RaycastHit hit, out IHexTile tile)
            => TryPickTile(ray, false, out hit, out tile);

        /// <summary>Editor Scene View가 비활성 Tile도 검사·선택할 수 있게 반환합니다.</summary>
        internal bool TryPickTileIncludingInactive(in Ray ray, out RaycastHit hit, out IHexTile tile)
            => TryPickTile(ray, true, out hit, out tile);

        private bool TryPickTile(in Ray ray, bool includeInactive, out RaycastHit hit, out IHexTile tile)
        {
            hit = default;
            tile = null;
            if (settings == null) return false;
            bool found = false;
            float closestDistance = float.PositiveInfinity;
            foreach (HexTilePicker picker in _pickers)
            {
                if (!picker.TryPick(ray, settings.InteractionLayerMask, out HexTilePickResult candidate)) continue;
                if (!includeInactive && !candidate.Tile.IsActive) continue;
                if (candidate.Hit.distance >= closestDistance) continue;
                found = true;
                closestDistance = candidate.Hit.distance;
                hit = candidate.Hit;
                tile = candidate.Tile;
            }
            return found;
        }

        /// <summary>출력 컴포넌트 쌍이 둘 다 지정됐거나 둘 다 비어 있는지 가져옵니다.</summary>
        private bool HasValidOutputPair => (outputMeshFilter == null) == (outputMeshRenderer == null);

        private void ProcessPointer(Vector2 screenPosition, bool pressed, bool released)
        {
            bool found = TryPickTile(mainCamera.ScreenPointToRay(screenPosition), out _, out IHexTile tile);
            HexTile picked = found ? (HexTile)tile : null;
            _selectionState.UpdateHover(picked);
            if (pressed) _selectionState.NotifyMouseDown(picked);
            if (released) _selectionState.NotifyMouseUp(picked);
        }

        /// <summary>Grid가 덮는 각 Surface의 Collider마다 picker를 만듭니다.</summary>
        private void BuildPickers()
        {
            _pickers.Clear();
            foreach (SurfaceHandle surface in SurfaceGrid.Surfaces)
            {
                if (!_query.TryGetAdapter(surface, out ISurfaceAdapter adapter)) continue;
                // Collider는 선택 사항입니다. 없으면 그 Surface 위 Tile은 논리와 표현만 유지됩니다.
                if (adapter?.PickingCollider == null) continue;
                if (!_query.TryGetTopology(surface, out SurfaceTopology topology)) continue;
                _pickers.Add(new HexTilePicker(adapter.PickingCollider, topology, SurfaceGrid, _tileStore));
            }
        }

        /// <summary>출력 쌍이 있으면 Geometry를 만들어 Backend에 적용하고 변형 binding을 준비합니다.</summary>
        private void BuildGeometry()
        {
            if (outputMeshFilter == null || outputMeshRenderer == null) return;
            _renderBackend ??= new MeshSurfaceGridRenderBackend(outputMeshFilter, outputMeshRenderer);
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(
                _query, _query, SurfaceGrid, outputMeshFilter.transform.worldToLocalMatrix, surfaceOffset);
            _renderBackend.ApplyGeometry(geometry);
            _renderBackend.SetRenderingEnabled(_renderingEnabled);
            DebugGeometry = geometry;
            BuildSkinBinding(geometry);
        }

        /// <summary>
        /// Grid가 단일 Skinned Surface 위에만 있으면 bone binding을 만듭니다. 여러 Surface에 걸친
        /// Grid는 Surface마다 변형 규칙이 달라 하나의 binding으로 표현할 수 없으므로 변형을 따르지 않습니다.
        /// </summary>
        private void BuildSkinBinding(SurfaceGridGeometry geometry)
        {
            ReleaseSkinBinding();
            if (SurfaceGrid.SpansMultipleSurfaces) return;
            SurfaceHandle surface = SurfaceGrid.Surfaces[0];
            if (!_query.TryGetAdapter(surface, out ISurfaceAdapter adapter)) return;
            if (adapter is not SkinnedMeshSurfaceAdapter skinned || skinned.Renderer == null) return;
            if (!_query.TryGetTopology(surface, out SurfaceTopology topology)) return;

            int boneCount = SkinnedMeshTopologyFactory.GetBoneCount(skinned.Renderer);
            if (boneCount == 0)
            {
                Debug.LogWarning("Skinned surface has no bones; the grid stays in bind pose.", skinned.Renderer);
                return;
            }

            _skinnedRenderer = skinned.Renderer;
            _skinBinding = SurfaceSkinBindingBuilder.Build(
                topology,
                geometry.SurfacePoints,
                SkinnedMeshTopologyFactory.ReadInfluences(skinned.Renderer.sharedMesh),
                boneCount);
            _skinningMatrices = new Matrix4x4[boneCount];
            _deformedPositions = new Vector3[_skinBinding.VertexCount];
            _deformedNormals = new Vector3[_skinBinding.VertexCount];
        }

        /// <summary>bind pose binding과 현재 bone 자세로 vertex 위치·법선만 다시 계산합니다.</summary>
        private void UpdateDeformation()
        {
            if (_skinBinding == null || _renderBackend == null || outputMeshFilter == null) return;
            if (_skinnedRenderer == null) return;

            Matrix4x4 targetFromWorld = outputMeshFilter.transform.worldToLocalMatrix;
            SkinnedMeshTopologyFactory.GetSkinningMatrices(_skinnedRenderer, targetFromWorld, _skinningMatrices);
            _skinBinding.Evaluate(_skinningMatrices, _deformedPositions, _deformedNormals);
            if (surfaceOffset > 0f)
            {
                // Geometry 생성과 같은 규칙으로 z-fighting 회피 offset을 다시 적용합니다.
                for (int i = 0; i < _deformedPositions.Length; i++)
                    _deformedPositions[i] += _deformedNormals[i] * surfaceOffset;
            }

            _renderBackend.ApplyDeformation(_deformedPositions, _deformedNormals);
        }

        private void EnsureServices()
        {
            if (_selectionState == null)
            {
                _pointerInput = new HexGridPointerInput();
                _selectionState = new HexTileSelectionState();
                _selectionState.Entered += HandleTileEntered;
                _selectionState.Exited += HandleTileExited;
                _selectionState.MouseDown += HandleTileMouseDown;
                _selectionState.MouseUp += HandleTileMouseUp;
            }
            if (_tileStore == null)
            {
                tiles ??= new List<HexTile>();
                _tileStore = new HexTileStore(tiles);
                _tileStore.TileVisualsChanged += HandleTileVisualsChanged;
                _tileStore.RebuildLookup();
            }
            if (_query == null)
            {
                _query = new GeometrySurfaceQuery();
                _system = new SurfaceGridSystem(_query);
            }
        }

        /// <summary>Grid snapshot, picker, Backend와 변형 상태를 해제합니다.</summary>
        private void ReleaseGridState()
        {
            SurfaceGrid = null;
            Seed = default;
            _pickers.Clear();
            ReleaseSkinBinding();
            _renderBackend?.Dispose();
            _renderBackend = null;
            DebugGeometry = null;
        }

        private void ReleaseGrid()
        {
            ReleaseGridState();
            _system?.Clear();
        }

        private void ReleaseSkinBinding()
        {
            _skinBinding = null;
            _skinnedRenderer = null;
            _skinningMatrices = null;
            _deformedPositions = null;
            _deformedNormals = null;
        }

        private void ReleaseServices()
        {
            if (_selectionState != null)
            {
                _selectionState.Entered -= HandleTileEntered;
                _selectionState.Exited -= HandleTileExited;
                _selectionState.MouseDown -= HandleTileMouseDown;
                _selectionState.MouseUp -= HandleTileMouseUp;
                _selectionState.Clear();
            }
            if (_tileStore != null) _tileStore.TileVisualsChanged -= HandleTileVisualsChanged;
            ReleaseGrid();
            _system?.Dispose();
            _system = null;
            _query = null;
            _tileStore = null;
            _pointerInput = null;
            _selectionState = null;
        }

        private void SubscribeToSettings()
        {
            if (_subscribedSettings == settings) return;
            UnsubscribeFromSettings();
            _subscribedSettings = settings;
            if (_subscribedSettings != null) _subscribedSettings.SettingsChanged += HandleSettingsChanged;
        }

        private void UnsubscribeFromSettings()
        {
            if (_subscribedSettings == null) return;
            _subscribedSettings.SettingsChanged -= HandleSettingsChanged;
            _subscribedSettings = null;
        }

        /// <summary>
        /// 설정 변경 알림을 받아 Grid를 다시 Bake합니다. Edit Mode에서 OnValidate 호출 스택으로 인한
        /// 파괴 계열 API 제한은 <see cref="HexGridSettings"/>가 알림을 보내기 전에 이미 다음 Editor
        /// tick으로 미루므로, 여기서는 항상 즉시 호출합니다.
        /// </summary>
        private void HandleSettingsChanged()
        {
            if (!isActiveAndEnabled || settings == null) return;
            if (!Application.isPlaying && TileCount == 0) return;
            BakeTiles();
        }

        private void HandleTileVisualsChanged() => RefreshRendering();
        private void HandleTileEntered(IHexTile tile) => onEnterTile?.Invoke(tile);
        private void HandleTileExited(IHexTile tile) => onExitTile?.Invoke(tile);
        private void HandleTileMouseDown(IHexTile tile) => onMouseDownTile?.Invoke(tile);
        private void HandleTileMouseUp(IHexTile tile) => onMouseUpTile?.Invoke(tile);
    }
}
