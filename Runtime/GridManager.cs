using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.Rendering.Universal;
using Jeomseon.Unity.Attributes;
using Jeomseon.Unity.GridTileSystem.Services;

namespace Jeomseon.Unity.GridTileSystem
{
    public sealed class GridManager : MonoBehaviour, ISerializationCallbackReceiver
    {
        public event UnityAction<IHexGrid> OnEnterTile
        {
            add => onEnterTile.AddListener(value);
            remove => onEnterTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnExitTile
        {
            add => onExitTile.AddListener(value);
            remove => onExitTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnMouseDownTile
        {
            add => onMouseDownTile.AddListener(value);
            remove => onMouseDownTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnMouseUpTile
        {
            add => onMouseUpTile.AddListener(value);
            remove => onMouseUpTile.RemoveListener(value);
        }

        private static readonly int _absoluteLimit = Shader.PropertyToID("_AbsoluteLimit");
        private static readonly int _radius = Shader.PropertyToID("_Radius");

        [FormerlySerializedAs("_onHighlightTile")]
        [Header("Events")]
        [SerializeField, FormerlySerializedAs("_onEnterTile")] private UnityEvent<IHexGrid> onEnterTile = new();
        [SerializeField, FormerlySerializedAs("_onExitTile")] private UnityEvent<IHexGrid> onExitTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseUpTile")] private UnityEvent<IHexGrid> onMouseUpTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseDownTile")] private UnityEvent<IHexGrid> onMouseDownTile = new();

        [Header("Settings")]
        [SerializeField] private HexGridSettings settings;

        /* TODO(P3-02, extensibility): 현재는 패키지 외부 의존성을 줄이기 위해 List를 직렬화합니다.
         * 자체 SerializedDictionary가 별도 패키지로 안정화되면 좌표 기반 직렬화 컬렉션으로 교체를 검토합니다.
         */
        [SerializeField, FormerlySerializedAs("_hexGrids")] private List<HexGrid> hexGrids = new();

        [SerializeField, GetOrAddComponent, FormerlySerializedAs("_decalProjector")] private DecalProjector decalProjector;

        [SerializeField, FormerlySerializedAs("_mainCamera")] private Camera mainCamera;

        [field: SerializeField] public GameObject RootObject { get; private set; }

        private bool _servicesInitialized;
        private IHexGridPointerInput _pointerInput;
        private IHexGridTilePicker _picker;
        private IHexGridTileDataStore _tileDataStore;
        private IHexGridSelectionState _selectionState;
        private IHexOptionBufferUploader _bufferUploader;

        public float HexagonRadius
        {
            get => settings.HexagonRadius;
            set => settings.HexagonRadius = value;
        }

        public int TileLimit
        {
            get => settings.TileLimit;
            set => settings.TileLimit = value;
        }

        public LayerMask LayerMask
        {
            get => settings.LayerMask;
            set => settings.LayerMask = value;
        }

        public float HexagonWidth => HexagonRadius * decalProjector.size.x * 2 * decalProjector.transform.localScale.x;

        public bool IsRender
        {
            get => decalProjector.enabled;
            set => decalProjector.enabled = value;
        }

        public int TileCount
        {
            get
            {
                int tileLimit = settings.TileLimit;
                int limitDouble = tileLimit * 2;
                int outerSum = limitDouble * (limitDouble + 1) / 2;
                int innerSum = tileLimit * (tileLimit + 1) / 2;
                return (outerSum - innerSum) * 2 + limitDouble + 1;
            }
        }

        private void Awake()
        {
            EnsureServices();
            _tileDataStore.RebuildLookup();
            SendToShaderHexOption();
        }

        private void OnEnable()
        {
            EnsureServices();
            settings.SettingsChanged += HandleSettingsChanged;
        }

        private void OnDisable()
        {
            settings.SettingsChanged -= HandleSettingsChanged;
        }

        private void Start()
        {
            if (!mainCamera)
            {
                Debug.LogWarning("카메라가 초기화되지 않았으므로 자동으로 메인카메라를 찾습니다");
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!mainCamera || !_pointerInput.TryGetPointer(out Vector2 screenPosition, out bool pressed, out bool released))
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            bool found = _picker.TryPick(ray, settings.LayerMask, settings.HexagonRadius, out _, out HexGrid hex);
            HexGrid pickedHex = found ? hex : null;

            _selectionState.UpdateHover(pickedHex);

            if (pressed)
            {
                _selectionState.NotifyMouseDown(pickedHex);
            }

            if (released)
            {
                _selectionState.NotifyMouseUp(pickedHex);
            }
        }

        private void OnApplicationQuit()
        {
            _bufferUploader?.Release();
        }

        private void OnDestroy()
        {
            _bufferUploader?.Release();
        }

        public void SetActiveAxialCoordinates(int q, int r, bool isActive)
        {
            EnsureServices();
            _tileDataStore.SetActive(new(q, r), isActive);
        }

        /// <summary>
        /// .. 레이캐스트를 한후 타일에 충돌했는지 검사합니다
        /// 타일에 충돌한 경우 true를 반환하고 hexGrid를 전달합니다
        /// 타일이 아니지만 레이캐스트 충돌이 일어난 경우 TUPLE의 첫번째 아이템이 true를 전달합니다
        /// </summary>
        /// <param name="ray"> .. 검사할 레이 </param>
        /// <param name="hitTuple"> .. 히트 정보를 담는 튜플 </param>
        /// <param name="hexGrid"> .. 충돌한 타일 </param>
        /// <returns></returns>
        public bool TryGetTileDataByRay(in Ray ray, out (bool, RaycastHit) hitTuple, out IHexGrid hexGrid)
        {
            EnsureServices();
            bool isSuccess = _picker.TryPick(ray, settings.LayerMask, settings.HexagonRadius, out hitTuple, out HexGrid hex);
            hexGrid = hex;
            return isSuccess;
        }

        public void CalculateTile()
        {
            /* TODO(P2-01, performance): 큰 그리드에서는 전체 재계산 대신 변경 영역만 갱신하고,
             * 좌표 계산을 Burst/Jobs로 옮길 수 있도록 순수 계산 계층을 분리합니다.
             */
            EnsureServices();
            _tileDataStore.Rebuild(decalProjector, settings.HexagonRadius, settings.TileLimit);
        }

        /* TODO(P3-01, extensibility): URP DecalProjector 전용 구현을 렌더링 백엔드로 추상화하여
         * 메시, Shader Graph, 다른 렌더 파이프라인 구현을 선택할 수 있게 합니다.
         */
        public void SendToShaderHexOption()
        {
            EnsureServices();
            _bufferUploader.Upload(_tileDataStore.Grids, decalProjector.material);
        }

        public List<IHexGrid> GetGrids()
        {
            EnsureServices();
            return _tileDataStore.Grids.OfType<IHexGrid>().ToList();
        }

        public void OnBeforeSerialize()
        {
            SendToShaderHexOption();
        }

        public void OnAfterDeserialize()
        {
            EnsureServices();
            _tileDataStore.RebuildLookup();
        }

        private void EnsureServices()
        {
            if (_servicesInitialized) return;
            _servicesInitialized = true;

            _tileDataStore = new HexGridTileDataStore(hexGrids);
            _picker = new HexGridTilePicker(decalProjector, _tileDataStore);
            _pointerInput = new HexGridPointerInput();
            _bufferUploader = new HexOptionBufferUploader();
            _selectionState = new HexGridSelectionState();

            _selectionState.Entered += onEnterTile.Invoke;
            _selectionState.Exited += onExitTile.Invoke;
            _selectionState.MouseDown += onMouseDownTile.Invoke;
            _selectionState.MouseUp += onMouseUpTile.Invoke;

            _tileDataStore.TileVisualsChanged += () => _bufferUploader.Upload(_tileDataStore.Grids, decalProjector.material);
        }

        private void HandleSettingsChanged()
        {
            decalProjector.material.SetFloat(_absoluteLimit, settings.TileLimit);
            decalProjector.material.SetFloat(_radius, settings.HexagonRadius);
        }
    }
}
