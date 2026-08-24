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
        [Tooltip("Seed 주변에서 표면을 찾을 반경입니다.")]
        [SerializeField, Min(0.001f)] private float seedSearchRadius = SurfaceQueryOptions.DefaultSearchRadius;
        [Tooltip("표면 후보로 삼을 layer입니다. 기본은 모든 layer입니다.")]
        [SerializeField] private LayerMask surfaceLayerMask = ~0;
        [Tooltip("같은 거리의 후보 중 선호할 방향입니다. 지면 위 Grid가 일반적이라 기본은 아래쪽입니다.")]
        [SerializeField] private Vector3 preferredSurfaceDirection = Vector3.down;

        [Header("Output")]
        [SerializeField] private MeshFilter outputMeshFilter;
        [SerializeField] private MeshRenderer outputMeshRenderer;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.002f;

        [Header("Patch limits")]
        [SerializeField, Min(1)] private int maximumPatchTriangles = 4096;
        [SerializeField, Min(0.001f)] private float maximumPatchRadius = 100f;
        [SerializeField, Min(0.000001f)] private float maximumClosureError = 0.01f;

        [SerializeField, HideInInspector] private List<HexTile> tiles = new();

        private HexGridSettings _subscribedSettings;
        private IHexGridPointerInput _pointerInput;
        private IHexTileSelectionState _selectionState;
        private HexTileStore _tileStore;
        private GeometrySurfaceQuery _query;
        private SurfaceGridSystem _system;
        private SurfaceGrid _grid;
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
        public event UnityAction<IHexTile> OnEnterTile { add => onEnterTile.AddListener(value); remove => onEnterTile.RemoveListener(value); }
        /// <summary>Pointer가 Tile에서 나갔을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnExitTile { add => onExitTile.AddListener(value); remove => onExitTile.RemoveListener(value); }
        /// <summary>Tile 위에서 pointer down이 발생했을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnMouseDownTile { add => onMouseDownTile.AddListener(value); remove => onMouseDownTile.RemoveListener(value); }
        /// <summary>Tile 위에서 pointer up이 발생했을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnMouseUpTile { add => onMouseUpTile.AddListener(value); remove => onMouseUpTile.RemoveListener(value); }

        /// <summary>현재 Grid의 Tile 개수를 가져옵니다.</summary>
        public int TileCount => tiles?.Count ?? 0;
        /// <summary>현재 Grid의 Tile 상태 목록을 가져옵니다.</summary>
        public IReadOnlyList<HexTile> Tiles => tiles;
        /// <summary>마지막으로 성공한 intrinsic Grid snapshot을 가져옵니다.</summary>
        public SurfaceGrid SurfaceGrid => _grid;
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

        private void OnEnable() { EnsureServices(); SubscribeToSettings(); if (settings != null) BakeTiles(); }
        private void OnDisable() { UnsubscribeFromSettings(); _selectionState?.Clear(); ReleaseGrid(); }
        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (Application.isPlaying && mainCamera == null) Debug.LogWarning($"{nameof(HexGridController)} could not find a Main Camera.", this);
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
            maximumPatchTriangles = Mathf.Max(1, maximumPatchTriangles);
            maximumPatchRadius = Mathf.Max(0.001f, maximumPatchRadius);
            maximumClosureError = Mathf.Max(0.000001f, maximumClosureError);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            seedSearchRadius = Mathf.Max(0.001f, seedSearchRadius);
            SubscribeToSettings();
        }

        private void OnDestroy() { UnsubscribeFromSettings(); ReleaseServices(); }

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
                // 이전 Bake가 캐시한 Adapter와 topology를 버려 Scene 변경이 반영되게 합니다.
                _query.Clear();

                SurfaceGridRequest request = new(
                    SeedPosition,
                    settings.TileRadius,
                    initialDirection,
                    new SurfacePatchBuildSettings(maximumPatchTriangles, maximumPatchRadius, maximumClosureError),
                    new SurfaceQueryOptions(seedSearchRadius, preferredSurfaceDirection, surfaceLayerMask));

                SurfaceGridBuildResult result = _system.Build(request);
                if (result.Grid == null)
                {
                    Debug.LogError($"{nameof(HexGridController)} could not build a grid ({result.Status}): {result.Diagnostic}", this);
                    _tileStore.Clear();
                    return;
                }

                _grid = result.Grid;
                Seed = result.Seed;
                if (!result.IsSuccess) Debug.LogWarning($"{nameof(HexGridController)} built an empty grid ({result.Status}): {result.Diagnostic}", this);
                if (_grid.Patch.WasTruncated) Debug.LogWarning($"{nameof(HexGridController)} Surface Patch reached its configured limit.", this);
                if (_grid.Patch.ClosureToleranceExceeded) Debug.LogWarning($"{nameof(HexGridController)} Surface Patch exceeded closure tolerance.", this);

                _tileStore.Bake(_query, _query, _grid);
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
            if (_isBaking || _renderBackend == null || _grid == null) return;
            SurfaceTileVisual[] visuals = new SurfaceTileVisual[tiles.Count];
            for (int i = 0; i < visuals.Length; i++) visuals[i] = new SurfaceTileVisual(tiles[i].Color, tiles[i].IsActive);
            _renderBackend.ApplyVisuals(visuals);
        }

        /// <summary>Axial 좌표 Tile의 활성 상태를 변경합니다.</summary>
        public void SetTileActive(int q, int r, bool isActive)
        {
            EnsureServices();
            _tileStore.SetActive(new AxialCoordinates(q, r), isActive);
        }

        /// <summary>Ray가 지나는 표면들 가운데 가장 가까운 활성 Tile을 반환합니다.</summary>
        public bool TryPickTile(in Ray ray, out RaycastHit hit, out IHexTile tile)
        {
            hit = default;
            tile = null;
            if (settings == null) return false;
            bool found = false;
            float closestDistance = float.PositiveInfinity;
            foreach (HexTilePicker picker in _pickers)
            {
                if (!picker.TryPick(ray, settings.InteractionLayerMask, out (bool, RaycastHit) candidate, out HexTile candidateTile)) continue;
                if (candidate.Item2.distance >= closestDistance) continue;
                found = true;
                closestDistance = candidate.Item2.distance;
                hit = candidate.Item2;
                tile = candidateTile;
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
            HashSet<SurfaceHandle> covered = new();
            foreach (SurfacePatchTriangle face in _grid.Patch.Triangles) covered.Add(face.Surface);

            foreach (SurfaceHandle surface in covered)
            {
                if (!_query.TryGetAdapter(surface, out ISurfaceAdapter adapter)) continue;
                // Collider는 선택 사항입니다. 없으면 그 Surface 위 Tile은 논리와 표현만 유지됩니다.
                if (adapter?.PickingCollider == null) continue;
                if (!_query.TryGetTopology(surface, out SurfaceTopology topology)) continue;
                _pickers.Add(new HexTilePicker(adapter.PickingCollider, topology, _grid, _tileStore));
            }
        }

        /// <summary>출력 쌍이 있으면 Geometry를 만들어 Backend에 적용하고 변형 binding을 준비합니다.</summary>
        private void BuildGeometry()
        {
            if (outputMeshFilter == null || outputMeshRenderer == null) return;
            _renderBackend ??= new MeshSurfaceGridRenderBackend(outputMeshFilter, outputMeshRenderer);
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(
                _query, _query, _grid, outputMeshFilter.transform.worldToLocalMatrix, surfaceOffset);
            _renderBackend.ApplyGeometry(geometry);
            _renderBackend.SetRenderingEnabled(_renderingEnabled);
            BuildSkinBinding(geometry);
        }

        /// <summary>
        /// Grid가 단일 Skinned Surface 위에만 있으면 bone binding을 만듭니다. 여러 Surface에 걸친
        /// Grid는 Surface마다 변형 규칙이 달라 하나의 binding으로 표현할 수 없으므로 변형을 따르지 않습니다.
        /// </summary>
        private void BuildSkinBinding(SurfaceGridGeometry geometry)
        {
            ReleaseSkinBinding();
            if (_grid.Patch.SpansMultipleSurfaces) return;
            if (!_query.TryGetAdapter(_grid.Patch.Surface, out ISurfaceAdapter adapter)) return;
            if (adapter is not SkinnedMeshSurfaceAdapter skinned || skinned.Renderer == null) return;
            if (!_query.TryGetTopology(_grid.Patch.Surface, out SurfaceTopology topology)) return;

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
            _grid = null;
            Seed = default;
            _pickers.Clear();
            ReleaseSkinBinding();
            _renderBackend?.Dispose();
            _renderBackend = null;
        }

        private void ReleaseGrid()
        {
            ReleaseGridState();
            _query?.Clear();
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

        private void HandleSettingsChanged() { if (isActiveAndEnabled && settings != null) BakeTiles(); }
        private void HandleTileVisualsChanged() => RefreshRendering();
        private void HandleTileEntered(IHexTile tile) => onEnterTile.Invoke(tile);
        private void HandleTileExited(IHexTile tile) => onExitTile.Invoke(tile);
        private void HandleTileMouseDown(IHexTile tile) => onMouseDownTile.Invoke(tile);
        private void HandleTileMouseUp(IHexTile tile) => onMouseUpTile.Invoke(tile);
    }
}
