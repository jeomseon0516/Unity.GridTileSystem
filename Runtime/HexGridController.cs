using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Services;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>여러 Static Mesh Surface의 독립 intrinsic Hex Grid와 상호작용을 조합합니다.</summary>
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
        [SerializeField] private List<HexGridReceiver> receivers = new();
        [SerializeField, FormerlySerializedAs("_mainCamera")] private Camera mainCamera;
        [SerializeField, Min(1)] private int maximumPatchTriangles = 4096;
        [SerializeField, Min(0.001f)] private float maximumPatchRadius = 100f;
        [SerializeField, Min(0.000001f)] private float maximumClosureError = 0.01f;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.002f;

        private HexGridSettings _subscribedSettings;
        private IHexGridPointerInput _pointerInput;
        private IHexTileSelectionState _selectionState;
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
        /// <summary>구성된 Surface Receiver 목록을 가져옵니다.</summary>
        public IReadOnlyList<HexGridReceiver> Receivers => receivers;
        /// <summary>모든 Receiver의 Tile 개수 합을 가져옵니다.</summary>
        public int TileCount { get { int count = 0; foreach (HexGridReceiver receiver in receivers) count += receiver?.TileCount ?? 0; return count; } }
        /// <summary>모든 Receiver가 공유하는 intrinsic Tile 반지름을 가져오거나 설정합니다.</summary>
        public float TileRadius { get => settings.TileRadius; set => settings.TileRadius = value; }
        /// <summary>Surface picking에 사용하는 Physics layer mask를 가져오거나 설정합니다.</summary>
        public LayerMask InteractionLayerMask { get => settings.InteractionLayerMask; set => settings.InteractionLayerMask = value; }
        /// <summary>Scale 적용 전 intrinsic Hex의 꼭짓점 간 너비를 가져옵니다.</summary>
        public float TileWidth => TileRadius * 2f;
        /// <summary>모든 Receiver의 표현 활성 상태를 가져오거나 설정합니다.</summary>
        public bool IsRenderingEnabled
        {
            get => _renderingEnabled;
            set { _renderingEnabled = value; foreach (HexGridReceiver receiver in receivers) receiver?.SetRenderingEnabled(value); }
        }

        private void OnEnable() { EnsureServices(); SubscribeToSettings(); if (IsConfigured()) BakeTiles(); }
        private void OnDisable() { UnsubscribeFromSettings(); _selectionState?.Clear(); ReleaseReceivers(); }
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
        /// 변형되는 Receiver의 Grid 위치를 현재 프레임 자세로 갱신합니다. Animator가 bone Transform을
        /// 확정하는 시점이 Update 이후이므로 LateUpdate에서 실행해야 한 프레임 밀리지 않습니다.
        /// </summary>
        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            foreach (HexGridReceiver receiver in receivers)
            {
                if (receiver?.IsDeformable == true) receiver.UpdateDeformation();
            }
        }
        private void ProcessPointer(Vector2 screenPosition, bool pressed, bool released)
        {
            bool found = TryPickTile(mainCamera.ScreenPointToRay(screenPosition), out _, out IHexTile tile);
            HexTile picked = found ? (HexTile)tile : null;
            _selectionState.UpdateHover(picked);
            if (pressed) _selectionState.NotifyMouseDown(picked);
            if (released) _selectionState.NotifyMouseUp(picked);
        }
        private void OnValidate()
        {
            maximumPatchTriangles = Mathf.Max(1, maximumPatchTriangles);
            maximumPatchRadius = Mathf.Max(0.001f, maximumPatchRadius);
            maximumClosureError = Mathf.Max(0.000001f, maximumClosureError);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            SubscribeToSettings();
        }
        private void OnDestroy() { UnsubscribeFromSettings(); ReleaseServices(); }

        /// <summary>유효한 각 Receiver를 독립적으로 Bake하며 실패한 Receiver만 건너뜁니다.</summary>
        public void BakeTiles()
        {
            if (settings == null) { Debug.LogError($"{nameof(HexGridController)} requires a {nameof(HexGridSettings)} asset.", this); return; }
            if (receivers == null || receivers.Count == 0) { Debug.LogError($"{nameof(HexGridController)} requires at least one Receiver.", this); return; }
            EnsureServices();
            _isBaking = true;
            try
            {
                _selectionState.Clear();
                SurfacePatchBuildSettings patchSettings = new(maximumPatchTriangles, maximumPatchRadius, maximumClosureError);
                for (int i = 0; i < receivers.Count; i++)
                {
                    HexGridReceiver receiver = receivers[i];
                    if (receiver == null) { Debug.LogError($"Receiver {i} is null.", this); continue; }
                    receiver.VisualsChanged -= HandleTileVisualsChanged;
                    receiver.VisualsChanged += HandleTileVisualsChanged;
                    if (!receiver.Validate(i, this)) { receiver.Clear(); continue; }
                    try
                    {
                        receiver.Bake(settings.TileRadius, patchSettings, surfaceOffset);
                        receiver.SetRenderingEnabled(_renderingEnabled);
                        if (receiver.SurfaceGrid.Patch.WasTruncated) Debug.LogWarning($"Receiver {i} Surface Patch reached its configured limit.", this);
                        if (receiver.SurfaceGrid.Patch.ClosureToleranceExceeded) Debug.LogWarning($"Receiver {i} Surface Patch exceeded closure tolerance.", this);
                    }
                    catch (Exception exception)
                    {
                        receiver.Clear();
                        Debug.LogException(exception, this);
                    }
                }
            }
            finally { _isBaking = false; }
            RefreshRendering();
        }

        /// <summary>모든 Receiver의 Logical Tile과 생성 Geometry를 지웁니다.</summary>
        public void ClearTiles()
        {
            EnsureServices();
            _isBaking = true;
            try { _selectionState.Clear(); foreach (HexGridReceiver receiver in receivers) receiver?.Clear(); }
            finally { _isBaking = false; }
        }
        /// <summary>Geometry를 재생성하지 않고 모든 Receiver의 현재 Tile 시각 상태를 적용합니다.</summary>
        public void RefreshRendering()
        {
            if (_isBaking) return;
            foreach (HexGridReceiver receiver in receivers) receiver?.ApplyVisuals();
        }
        /// <summary>지정한 Receiver의 Axial 좌표 Tile 활성 상태를 변경합니다.</summary>
        public void SetTileActive(int receiverIndex, int q, int r, bool isActive)
        {
            if (receiverIndex < 0 || receiverIndex >= receivers.Count) throw new ArgumentOutOfRangeException(nameof(receiverIndex));
            receivers[receiverIndex]?.SetTileActive(new AxialCoordinates(q, r), isActive);
        }
        /// <summary>Ray와 교차하는 Receiver 가운데 가장 가까운 활성 Tile을 반환합니다.</summary>
        public bool TryPickTile(in Ray ray, out RaycastHit hit, out IHexTile tile)
        {
            hit = default;
            tile = null;
            if (settings == null) return false;
            bool found = false;
            float closestDistance = float.PositiveInfinity;
            foreach (HexGridReceiver receiver in receivers)
            {
                if (receiver == null || !receiver.TryPick(ray, settings.InteractionLayerMask, out RaycastHit candidateHit, out HexTile candidateTile)) continue;
                if (candidateHit.distance >= closestDistance) continue;
                found = true;
                closestDistance = candidateHit.distance;
                hit = candidateHit;
                tile = candidateTile;
            }
            return found;
        }

        private void EnsureServices()
        {
            if (_selectionState != null) return;
            _pointerInput = new HexGridPointerInput();
            _selectionState = new HexTileSelectionState();
            _selectionState.Entered += HandleTileEntered;
            _selectionState.Exited += HandleTileExited;
            _selectionState.MouseDown += HandleTileMouseDown;
            _selectionState.MouseUp += HandleTileMouseUp;
        }
        private void ReleaseReceivers()
        {
            if (receivers == null) return;
            foreach (HexGridReceiver receiver in receivers)
            {
                if (receiver == null) continue;
                receiver.VisualsChanged -= HandleTileVisualsChanged;
                receiver.Release();
            }
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
            ReleaseReceivers();
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
        private void HandleSettingsChanged() { if (isActiveAndEnabled && IsConfigured()) BakeTiles(); }
        private void HandleTileVisualsChanged() => RefreshRendering();
        private void HandleTileEntered(IHexTile tile) => onEnterTile.Invoke(tile);
        private void HandleTileExited(IHexTile tile) => onExitTile.Invoke(tile);
        private void HandleTileMouseDown(IHexTile tile) => onMouseDownTile.Invoke(tile);
        private void HandleTileMouseUp(IHexTile tile) => onMouseUpTile.Invoke(tile);
        private bool IsConfigured()
        {
            if (settings == null || receivers == null || receivers.Count == 0) return false;
            foreach (HexGridReceiver receiver in receivers) if (receiver?.IsConfigured == true) return true;
            return false;
        }
    }
}
