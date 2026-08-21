using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Services;
using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>Static Mesh의 intrinsic Surface Space에 Hex Grid를 생성하고 상호작용을 조합합니다.</summary>
    [ExecuteAlways]
    public sealed class HexGridController : MonoBehaviour
    {
        /// <summary>Pointer가 새 Tile에 들어왔을 때 호출하는 직렬화 이벤트입니다.</summary>
        [Header("Events")]
        [SerializeField, FormerlySerializedAs("_onEnterTile"), FormerlySerializedAs("_onHighlightTile")]
        private UnityEvent<IHexTile> onEnterTile = new();
        /// <summary>Pointer가 현재 Tile에서 나갔을 때 호출하는 직렬화 이벤트입니다.</summary>
        [SerializeField, FormerlySerializedAs("_onExitTile")] private UnityEvent<IHexTile> onExitTile = new();
        /// <summary>Tile 위에서 pointer up이 발생했을 때 호출하는 직렬화 이벤트입니다.</summary>
        [SerializeField, FormerlySerializedAs("_onMouseUpTile")] private UnityEvent<IHexTile> onMouseUpTile = new();
        /// <summary>Tile 위에서 pointer down이 발생했을 때 호출하는 직렬화 이벤트입니다.</summary>
        [SerializeField, FormerlySerializedAs("_onMouseDownTile")] private UnityEvent<IHexTile> onMouseDownTile = new();

        /// <summary>Tile 크기, Grid 반경과 interaction layer 설정 asset입니다.</summary>
        [Header("Settings")]
        [SerializeField] private HexGridSettings settings;
        /// <summary>Topology와 barycentric binding의 원본 readable Static Mesh입니다.</summary>
        [SerializeField] private MeshFilter sourceMeshFilter;
        /// <summary>원본 Surface hit의 Triangle identity를 제공하는 MeshCollider입니다.</summary>
        [SerializeField] private MeshCollider surfaceCollider;
        /// <summary>생성된 Grid Geometry를 받을 원본과 다른 MeshFilter입니다.</summary>
        [SerializeField] private MeshFilter outputMeshFilter;
        /// <summary>Grid Geometry를 표시할 공통 MeshRenderer입니다.</summary>
        [SerializeField] private MeshRenderer outputMeshRenderer;
        /// <summary>Runtime pointer ray를 생성할 Camera입니다.</summary>
        [SerializeField, FormerlySerializedAs("_mainCamera")] private Camera mainCamera;
        /// <summary>Grid local chart를 시작할 원본 Triangle index입니다.</summary>
        [SerializeField, Min(0)] private int seedTriangleIndex;
        /// <summary>Seed Triangle 내부 위치를 나타내는 barycentric (u,v,w)입니다.</summary>
        [SerializeField] private Vector3 seedBarycentric = new(1f / 3f, 1f / 3f, 1f / 3f);
        /// <summary>Patch에 포함할 최대 Triangle 개수입니다.</summary>
        [SerializeField, Min(1)] private int maximumPatchTriangles = 4096;
        /// <summary>Patch Triangle 중심에 허용할 최대 intrinsic 반경입니다.</summary>
        [SerializeField, Min(0.001f)] private float maximumPatchRadius = 100f;
        /// <summary>곡률 cycle에서 허용할 최대 closure error입니다.</summary>
        [SerializeField, Min(0.000001f)] private float maximumClosureError = 0.01f;
        /// <summary>z-fighting을 피하기 위해 Grid vertex를 Surface 법선 방향으로 이동할 거리입니다.</summary>
        [SerializeField, Min(0f)] private float surfaceOffset = 0.002f;
        /// <summary>직렬화되어 사용자 상태와 이벤트를 보존하는 Logical Tile 목록입니다.</summary>
        [SerializeField, HideInInspector, FormerlySerializedAs("hexGrids"), FormerlySerializedAs("_hexGrids")]
        private List<HexTile> tiles = new();

        /// <summary>현재 구독 중인 Settings asset입니다.</summary>
        private HexGridSettings _subscribedSettings;
        /// <summary>Pointer 장치 입력 adapter입니다.</summary>
        private IHexGridPointerInput _pointerInput;
        /// <summary>Surface hit을 사용자 Tile 상태로 변환하는 picker입니다.</summary>
        private IHexTilePicker _tilePicker;
        /// <summary>직렬화 Tile 목록과 좌표 lookup을 관리합니다.</summary>
        private IHexTileStore _tileStore;
        /// <summary>hover/down/up 상태 전환과 이벤트 순서를 관리합니다.</summary>
        private IHexTileSelectionState _selectionState;
        /// <summary>파이프라인 비종속 Geometry/Visual 수명 Backend입니다.</summary>
        private ISurfaceGridRenderBackend _renderBackend;
        /// <summary>마지막 Bake의 원본 topology snapshot입니다.</summary>
        private SurfaceTopology _topology;
        /// <summary>마지막 Bake의 intrinsic Logical Grid snapshot입니다.</summary>
        private SurfaceGrid _surfaceGrid;
        /// <summary>Store 재구축 중 발생하는 visual event의 조기 업로드를 막습니다.</summary>
        private bool _isBaking;
        /// <summary>Backend 재생성 뒤 복원할 표현 활성 상태입니다.</summary>
        private bool _renderingEnabled = true;

        /// <summary>Pointer가 Tile에 들어왔을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnEnterTile { add => onEnterTile.AddListener(value); remove => onEnterTile.RemoveListener(value); }
        /// <summary>Pointer가 Tile에서 나갔을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnExitTile { add => onExitTile.AddListener(value); remove => onExitTile.RemoveListener(value); }
        /// <summary>Tile 위에서 pointer down이 발생했을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnMouseDownTile { add => onMouseDownTile.AddListener(value); remove => onMouseDownTile.RemoveListener(value); }
        /// <summary>Tile 위에서 pointer up이 발생했을 때 발생합니다.</summary>
        public event UnityAction<IHexTile> OnMouseUpTile { add => onMouseUpTile.AddListener(value); remove => onMouseUpTile.RemoveListener(value); }
        /// <summary>현재 직렬화된 Logical Tile 상태를 가져옵니다.</summary>
        public IReadOnlyList<HexTile> Tiles => tiles;
        /// <summary>현재 Logical Tile 개수를 가져옵니다.</summary>
        public int TileCount => tiles.Count;
        /// <summary>마지막으로 성공한 intrinsic Surface Grid snapshot을 가져옵니다.</summary>
        public SurfaceGrid SurfaceGrid => _surfaceGrid;
        /// <summary>Hex 중심에서 꼭짓점까지의 실제 Surface 길이를 가져오거나 설정합니다.</summary>
        public float TileRadius { get => settings.TileRadius; set => settings.TileRadius = value; }
        /// <summary>Seed에서 생성할 Hex ring 개수를 가져오거나 설정합니다.</summary>
        public int GridRadius { get => settings.GridRadius; set => settings.GridRadius = value; }
        /// <summary>Surface picking에 사용하는 Physics layer mask를 가져오거나 설정합니다.</summary>
        public LayerMask InteractionLayerMask { get => settings.InteractionLayerMask; set => settings.InteractionLayerMask = value; }
        /// <summary>Scale 적용 전 intrinsic Hex의 꼭짓점 간 너비를 가져옵니다.</summary>
        public float TileWidth => TileRadius * 2f;
        /// <summary>Grid 표현 활성 상태를 가져오거나 설정합니다.</summary>
        public bool IsRenderingEnabled
        {
            get => _renderingEnabled;
            set { _renderingEnabled = value; EnsureBackend(); _renderBackend?.SetRenderingEnabled(value); }
        }

        /// <summary>활성화 시 서비스와 Settings 구독을 복원하고 구성이 완전하면 Grid를 생성합니다.</summary>
        private void OnEnable() { EnsureServices(); SubscribeToSettings(); if (IsConfigured()) BakeTiles(); }
        /// <summary>비활성화 시 구독, selection과 runtime Mesh를 해제합니다.</summary>
        private void OnDisable() { UnsubscribeFromSettings(); _selectionState?.Clear(); ReleaseBackend(); }
        /// <summary>Runtime 시작 시 지정되지 않은 Camera를 Main Camera에서 찾습니다.</summary>
        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (Application.isPlaying && mainCamera == null)
                Debug.LogWarning($"{nameof(HexGridController)} could not find a Main Camera.", this);
        }

        /// <summary>Runtime pointer를 Surface Grid Tile로 변환하고 selection 상태를 갱신합니다.</summary>
        private void Update()
        {
            if (!Application.isPlaying || mainCamera == null || settings == null || _tilePicker == null) return;
            if (!_pointerInput.TryGetPointer(out Vector2 screenPosition, out bool pressed, out bool released)) return;
            ProcessPointer(screenPosition, pressed, released);
        }

        /// <summary>화면 좌표 입력 하나를 Camera Ray, Surface picking과 selection 이벤트로 변환합니다.</summary>
        private void ProcessPointer(Vector2 screenPosition, bool pressed, bool released)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            bool found = _tilePicker.TryPick(ray, settings.InteractionLayerMask, out _, out HexTile tile);
            HexTile picked = found ? tile : null;
            _selectionState.UpdateHover(picked);
            if (pressed) _selectionState.NotifyMouseDown(picked);
            if (released) _selectionState.NotifyMouseUp(picked);
        }

        /// <summary>직렬화 값의 최소 범위를 보정하고 Settings 구독을 갱신합니다.</summary>
        private void OnValidate()
        {
            maximumPatchTriangles = Mathf.Max(1, maximumPatchTriangles);
            maximumPatchRadius = Mathf.Max(0.001f, maximumPatchRadius);
            maximumClosureError = Mathf.Max(0.000001f, maximumClosureError);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            SubscribeToSettings();
        }
        /// <summary>GameObject 파괴 시 모든 서비스 이벤트와 Backend 자원을 해제합니다.</summary>
        private void OnDestroy() { UnsubscribeFromSettings(); ReleaseServices(); }

        /// <summary>현재 Static Mesh Surface와 Settings로 topology, Grid, Tile, Geometry를 다시 생성합니다.</summary>
        public void BakeTiles()
        {
            if (!ValidateConfiguration()) return;
            EnsureServices();
            _isBaking = true;
            try
            {
                _selectionState.Clear();
                _topology = StaticMeshSurfaceAdapter.BuildTopology(sourceMeshFilter.sharedMesh);
                SurfacePoint seed = new(_topology.Handle, seedTriangleIndex, seedBarycentric);
                if (!seed.IsValid) { Debug.LogError("Seed barycentric coordinates must be non-negative and sum to one.", this); return; }
                SurfacePatchBuildSettings patchSettings = new(maximumPatchTriangles, maximumPatchRadius, maximumClosureError);
                _surfaceGrid = SurfaceGridBuilder.Build(_topology, seed, settings.TileRadius, settings.GridRadius, patchSettings);
                _tileStore.Bake(_topology, _surfaceGrid, sourceMeshFilter.transform, settings.GridRadius);
                _tilePicker = new HexTilePicker(surfaceCollider, _topology, _surfaceGrid, _tileStore);
                EnsureBackend();
                if (_renderBackend != null)
                {
                    Matrix4x4 surfaceToOutput = outputMeshFilter.transform.worldToLocalMatrix * sourceMeshFilter.transform.localToWorldMatrix;
                    _renderBackend.ApplyGeometry(
                        SurfaceGridGeometryBuilder.Build(_topology, _surfaceGrid, surfaceToOutput, surfaceOffset));
                    _renderBackend.SetRenderingEnabled(_renderingEnabled);
                }
            }
            finally { _isBaking = false; }
            RefreshRendering();
            if (_surfaceGrid.Patch.WasTruncated) Debug.LogWarning("Surface Patch reached its triangle-count or intrinsic-radius limit.", this);
            if (_surfaceGrid.Patch.ClosureToleranceExceeded) Debug.LogWarning("Surface Patch exceeded closure tolerance; reduce radius or split the chart.", this);
        }

        /// <summary>Logical Tile과 생성 Geometry를 모두 지우고 사용자 lookup을 초기화합니다.</summary>
        public void ClearTiles()
        {
            EnsureServices();
            _isBaking = true;
            try
            {
                _selectionState.Clear();
                // Store.Clear는 visual 변경 이벤트를 동기 발행한다. 기존 Geometry가 살아 있는 동안
                // 빈 visual 배열을 Backend에 적용하면 tile index 계약이 깨지므로 정리 구간에서는
                // HandleTileVisualsChanged를 억제하고 snapshot과 Backend를 함께 폐기한다.
                _tileStore.Clear();
                _topology = null;
                _surfaceGrid = null;
                _tilePicker = null;
                ReleaseBackend();
            }
            finally { _isBaking = false; }
        }

        /// <summary>Geometry를 재생성하지 않고 현재 Tile 색상·활성 상태를 Backend에 적용합니다.</summary>
        public void RefreshRendering()
        {
            if (_isBaking || _surfaceGrid == null) return;
            EnsureBackend();
            if (_renderBackend == null) return;
            SurfaceTileVisual[] visuals = new SurfaceTileVisual[tiles.Count];
            for (int i = 0; i < visuals.Length; i++) visuals[i] = new SurfaceTileVisual(tiles[i].Color, tiles[i].IsActive);
            _renderBackend.ApplyVisuals(visuals);
        }

        /// <summary>지정한 Axial 좌표 Tile의 활성 상태를 변경합니다.</summary>
        public void SetTileActive(int q, int r, bool isActive) { EnsureServices(); _tileStore.SetActive(new AxialCoordinates(q, r), isActive); }

        /// <summary>Ray가 활성 Surface Grid Tile과 교차하면 Physics hit과 Tile API를 반환합니다.</summary>
        public bool TryPickTile(in Ray ray, out RaycastHit hit, out IHexTile tile)
        {
            hit = default; tile = null;
            if (settings == null || _tilePicker == null) return false;
            bool picked = _tilePicker.TryPick(ray, settings.InteractionLayerMask, out (bool, RaycastHit) result, out HexTile pickedTile);
            hit = result.Item2; tile = pickedTile; return picked;
        }

        /// <summary>순수 사용자 상태·입력·selection 서비스를 필요할 때 생성하고 이벤트를 연결합니다.</summary>
        private void EnsureServices()
        {
            if (_tileStore != null) return;
            _tileStore = new HexTileStore(tiles); _pointerInput = new HexGridPointerInput(); _selectionState = new HexTileSelectionState();
            _selectionState.Entered += HandleTileEntered; _selectionState.Exited += HandleTileExited;
            _selectionState.MouseDown += HandleTileMouseDown; _selectionState.MouseUp += HandleTileMouseUp;
            _tileStore.TileVisualsChanged += HandleTileVisualsChanged; _tileStore.RebuildLookup();
        }
        /// <summary>기본 공통 Mesh Backend를 필요할 때 생성합니다.</summary>
        private void EnsureBackend()
        {
            if (_renderBackend != null || outputMeshFilter == null || outputMeshRenderer == null) return;
            _renderBackend = new MeshSurfaceGridRenderBackend(outputMeshFilter, outputMeshRenderer);
        }
        /// <summary>Backend가 소유한 runtime Mesh를 해제합니다.</summary>
        private void ReleaseBackend() { _renderBackend?.Dispose(); _renderBackend = null; }
        /// <summary>서비스 이벤트를 해제하고 runtime 참조를 초기화합니다.</summary>
        private void ReleaseServices()
        {
            if (_selectionState != null)
            {
                _selectionState.Entered -= HandleTileEntered; _selectionState.Exited -= HandleTileExited;
                _selectionState.MouseDown -= HandleTileMouseDown; _selectionState.MouseUp -= HandleTileMouseUp; _selectionState.Clear();
            }
            if (_tileStore != null) _tileStore.TileVisualsChanged -= HandleTileVisualsChanged;
            ReleaseBackend(); _pointerInput = null; _tilePicker = null; _tileStore = null; _selectionState = null;
            _topology = null; _surfaceGrid = null;
        }

        /// <summary>Settings asset 교체를 감지하여 변경 이벤트를 중복 없이 구독합니다.</summary>
        private void SubscribeToSettings()
        {
            if (_subscribedSettings == settings) return; UnsubscribeFromSettings(); _subscribedSettings = settings;
            if (_subscribedSettings != null) _subscribedSettings.SettingsChanged += HandleSettingsChanged;
        }
        /// <summary>현재 Settings asset의 변경 이벤트 구독을 해제합니다.</summary>
        private void UnsubscribeFromSettings()
        {
            if (_subscribedSettings == null) return; _subscribedSettings.SettingsChanged -= HandleSettingsChanged; _subscribedSettings = null;
        }
        /// <summary>설정 변경 시 구성이 완전한 활성 Controller만 다시 Bake합니다.</summary>
        private void HandleSettingsChanged() { if (isActiveAndEnabled && IsConfigured()) BakeTiles(); }
        /// <summary>Tile visual 상태 변경을 Backend에 전달합니다.</summary>
        private void HandleTileVisualsChanged() => RefreshRendering();
        /// <summary>Selection enter를 공개 UnityEvent에 전달합니다.</summary>
        private void HandleTileEntered(IHexTile tile) => onEnterTile.Invoke(tile);
        /// <summary>Selection exit를 공개 UnityEvent에 전달합니다.</summary>
        private void HandleTileExited(IHexTile tile) => onExitTile.Invoke(tile);
        /// <summary>Selection down을 공개 UnityEvent에 전달합니다.</summary>
        private void HandleTileMouseDown(IHexTile tile) => onMouseDownTile.Invoke(tile);
        /// <summary>Selection up을 공개 UnityEvent에 전달합니다.</summary>
        private void HandleTileMouseUp(IHexTile tile) => onMouseUpTile.Invoke(tile);

        /// <summary>오류 로그 없이 자동 Bake 가능한 최소 구성인지 검사합니다.</summary>
        private bool IsConfigured() => settings != null && sourceMeshFilter != null && sourceMeshFilter.sharedMesh != null &&
            surfaceCollider != null && surfaceCollider.sharedMesh == sourceMeshFilter.sharedMesh &&
            IsRenderingConfigurationValid();

        /// <summary>표현을 생략하거나 완전한 출력 쌍을 지정했는지 확인합니다.</summary>
        private bool IsRenderingConfigurationValid()
        {
            bool hasFilter = outputMeshFilter != null;
            bool hasRenderer = outputMeshRenderer != null;
            return hasFilter == hasRenderer && (!hasFilter || !ReferenceEquals(sourceMeshFilter, outputMeshFilter));
        }

        /// <summary>필수 참조와 MeshCollider identity 계약을 검사하고 구체적인 오류를 기록합니다.</summary>
        private bool ValidateConfiguration()
        {
            if (settings == null) { Debug.LogError($"{nameof(HexGridController)} requires a {nameof(HexGridSettings)} asset.", this); return false; }
            if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null) { Debug.LogError($"{nameof(HexGridController)} requires a source MeshFilter with a Mesh.", this); return false; }
            if (!sourceMeshFilter.sharedMesh.isReadable) { Debug.LogError("Source Mesh must have Read/Write Enabled.", sourceMeshFilter); return false; }
            if (surfaceCollider == null || surfaceCollider.sharedMesh != sourceMeshFilter.sharedMesh) { Debug.LogError("Surface MeshCollider must reference the same Mesh as the source MeshFilter.", this); return false; }
            int triangleCount = sourceMeshFilter.sharedMesh.triangles.Length / 3;
            if (seedTriangleIndex < 0 || seedTriangleIndex >= triangleCount) { Debug.LogError($"Seed triangle index must be in [0, {triangleCount - 1}].", this); return false; }
            if ((outputMeshFilter == null) != (outputMeshRenderer == null)) { Debug.LogError("Assign both output MeshFilter and MeshRenderer, or leave both empty for a logical-only Grid.", this); return false; }
            if (outputMeshFilter != null && ReferenceEquals(sourceMeshFilter, outputMeshFilter)) { Debug.LogError("Source and output MeshFilter must be different.", this); return false; }
            return true;
        }
    }
}
