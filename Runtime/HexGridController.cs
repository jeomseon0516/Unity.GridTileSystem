using System.Collections.Generic;
using Jeomseon.Unity.Attributes;
using Jeomseon.Unity.GridTileSystem.Services;
using Jeomseon.Unity.Projector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.GridTileSystem
{
    [RequireComponent(typeof(MeshProjector))]
    [ExecuteAlways]
    public sealed class HexGridController : MonoBehaviour
    {
        private static readonly int GridRadiusId = Shader.PropertyToID("_HexGridRadius");
        private static readonly int TileRadiusId = Shader.PropertyToID("_HexGridTileRadius");

        [Header("Events")]
        [SerializeField, FormerlySerializedAs("_onEnterTile"), FormerlySerializedAs("_onHighlightTile")]
        private UnityEvent<IHexTile> onEnterTile = new();
        [SerializeField, FormerlySerializedAs("_onExitTile")] private UnityEvent<IHexTile> onExitTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseUpTile")] private UnityEvent<IHexTile> onMouseUpTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseDownTile")] private UnityEvent<IHexTile> onMouseDownTile = new();

        [Header("Settings")]
        [SerializeField] private HexGridSettings settings;
        [SerializeField, HideInInspector, FormerlySerializedAs("hexGrids"), FormerlySerializedAs("_hexGrids")]
        private List<HexTile> tiles = new();
        [SerializeField, GetOrAddComponent, FormerlySerializedAs("decalProjector"), FormerlySerializedAs("_decalProjector")]
        private MeshProjector projector;
        [SerializeField, FormerlySerializedAs("_mainCamera")] private Camera mainCamera;

        private HexGridSettings _subscribedSettings;
        private IHexGridPointerInput _pointerInput;
        private IHexTilePicker _tilePicker;
        private IHexTileStore _tileStore;
        private IHexTileSelectionState _selectionState;
        private IHexTileBufferUploader _bufferUploader;
        private MeshProjector _serviceProjector;
        private List<HexTile> _serviceTiles;

        public event UnityAction<IHexTile> OnEnterTile
        {
            add => onEnterTile.AddListener(value);
            remove => onEnterTile.RemoveListener(value);
        }

        public event UnityAction<IHexTile> OnExitTile
        {
            add => onExitTile.AddListener(value);
            remove => onExitTile.RemoveListener(value);
        }

        public event UnityAction<IHexTile> OnMouseDownTile
        {
            add => onMouseDownTile.AddListener(value);
            remove => onMouseDownTile.RemoveListener(value);
        }

        public event UnityAction<IHexTile> OnMouseUpTile
        {
            add => onMouseUpTile.AddListener(value);
            remove => onMouseUpTile.RemoveListener(value);
        }

        public IReadOnlyList<HexTile> Tiles => tiles;
        public int TileCount => tiles.Count;

        public float TileRadius
        {
            get => settings.TileRadius;
            set => settings.TileRadius = value;
        }

        public int GridRadius
        {
            get => settings.GridRadius;
            set => settings.GridRadius = value;
        }

        public LayerMask InteractionLayerMask
        {
            get => settings.InteractionLayerMask;
            set => settings.InteractionLayerMask = value;
        }

        public float TileWidth => TileRadius * projector.Size.x * 2f * projector.transform.lossyScale.x;

        public bool IsRenderingEnabled
        {
            get => projector.RenderingEnabled;
            set => projector.RenderingEnabled = value;
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (!ValidateConfiguration()) return;

            EnsureServices();
            _tileStore.RebuildLookup();
            if (tiles.Count == 0) BakeTiles();
            else RefreshRendering();
        }

        private void OnEnable()
        {
            EnsureServices();
            SubscribeToSettings();
            if (IsConfigured())
            {
                _tileStore.RebuildLookup();
                RefreshRendering();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromSettings();
            _selectionState?.Clear();
            _bufferUploader?.Release();
        }

        private void Start()
        {
            if (mainCamera != null) return;

            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning($"{nameof(HexGridController)} could not find a Main Camera.", this);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (mainCamera == null || settings == null || projector == null ||
                !_pointerInput.TryGetPointer(out Vector2 screenPosition, out bool pressed, out bool released))
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            bool found = _tilePicker.TryPick(
                ray, settings.InteractionLayerMask, settings.TileRadius, out _, out HexTile tile);
            HexTile pickedTile = found ? tile : null;
            _selectionState.UpdateHover(pickedTile);

            if (pressed) _selectionState.NotifyMouseDown(pickedTile);
            if (released) _selectionState.NotifyMouseUp(pickedTile);
        }

        private void OnDestroy()
        {
            UnsubscribeFromSettings();
            ReleaseServices();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SubscribeToSettings();
        }
#endif

        public void BakeTiles()
        {
            if (!ValidateConfiguration()) return;

            EnsureServices();
            _selectionState.Clear();
            _tileStore.Bake(
                projector, settings.TileRadius, settings.GridRadius, settings.InteractionLayerMask);
        }

        public void ClearTiles()
        {
            EnsureServices();
            _selectionState.Clear();
            _tileStore.Clear();
        }

        public void RefreshRendering()
        {
            if (!ValidateConfiguration()) return;

            EnsureServices();
            projector.SetFloat(GridRadiusId, settings.GridRadius);
            projector.SetFloat(TileRadiusId, settings.TileRadius);
            _bufferUploader.Upload(_tileStore.Tiles, projector);
        }

        public void SetTileActive(int q, int r, bool isActive)
        {
            EnsureServices();
            _tileStore.SetActive(new AxialCoordinates(q, r), isActive);
        }

        public bool TryPickTile(in Ray ray, out RaycastHit hit, out IHexTile tile)
        {
            hit = default;
            tile = null;
            if (settings == null || projector == null) return false;

            EnsureServices();
            bool picked = _tilePicker.TryPick(
                ray, settings.InteractionLayerMask, settings.TileRadius, out (bool hitAnything, RaycastHit hitInfo) result,
                out HexTile pickedTile);
            hit = result.hitInfo;
            tile = pickedTile;
            return picked;
        }

        private void EnsureServices()
        {
            if (_tileStore != null && !ReferenceEquals(_serviceTiles, tiles)) ReleaseServices();

            if (_tileStore == null)
            {
                _serviceTiles = tiles;
                _tileStore = new HexTileStore(tiles);
                _pointerInput = new HexGridPointerInput();
                _bufferUploader = new HexTileBufferUploader();
                _selectionState = new HexTileSelectionState();

                _selectionState.Entered += HandleTileEntered;
                _selectionState.Exited += HandleTileExited;
                _selectionState.MouseDown += HandleTileMouseDown;
                _selectionState.MouseUp += HandleTileMouseUp;
                _tileStore.TileVisualsChanged += HandleTileVisualsChanged;
            }

            if (_tilePicker == null || _serviceProjector != projector)
            {
                _serviceProjector = projector;
                _tilePicker = new HexTilePicker(projector, _tileStore);
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

            if (_tileStore != null) _tileStore.TileVisualsChanged -= HandleTileVisualsChanged;
            _bufferUploader?.Release();
            _pointerInput = null;
            _tilePicker = null;
            _tileStore = null;
            _selectionState = null;
            _bufferUploader = null;
            _serviceProjector = null;
            _serviceTiles = null;
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

        private void HandleSettingsChanged()
        {
            if (ValidateConfiguration()) BakeTiles();
        }

        private void HandleTileVisualsChanged() => RefreshRendering();
        private void HandleTileEntered(IHexTile tile) => onEnterTile.Invoke(tile);
        private void HandleTileExited(IHexTile tile) => onExitTile.Invoke(tile);
        private void HandleTileMouseDown(IHexTile tile) => onMouseDownTile.Invoke(tile);
        private void HandleTileMouseUp(IHexTile tile) => onMouseUpTile.Invoke(tile);

        private bool ValidateConfiguration()
        {
            if (settings == null)
            {
                Debug.LogError($"{nameof(HexGridController)} requires a {nameof(HexGridSettings)} asset.", this);
                return false;
            }

            if (projector == null)
            {
                Debug.LogError($"{nameof(HexGridController)} requires a {nameof(MeshProjector)} reference.", this);
                return false;
            }

            if (projector.Effect == null)
            {
                Debug.LogError(
                    $"{nameof(MeshProjector)} requires a compatible hex grid {nameof(ProjectorEffect)}.",
                    projector);
                return false;
            }

            return true;
        }

        private bool IsConfigured()
            => settings != null && projector != null && projector.Effect != null;
    }
}
