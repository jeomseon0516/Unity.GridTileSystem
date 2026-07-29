using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Rendering.Universal;
using Jeomseon.Attribute;
using Jeomseon.Extensions;
using Jeomseon.Helper;

namespace Jeomseon.HexGrid
{
    public sealed class GridManager : MonoBehaviour, ISerializationCallbackReceiver
    {
        // TODO(리팩토링): 입력 처리, 물리 레이캐스트, 타일 데이터, GPU 버퍼 갱신 책임을
        // 각각의 서비스로 분리하고 인터페이스로 주입하여 테스트 가능한 구조로 변경합니다.
        // TODO(확장): URP DecalProjector 전용 구현을 렌더링 백엔드로 추상화하여
        // 메시, Shader Graph, 다른 렌더 파이프라인 구현을 선택할 수 있게 합니다.
        private const float HEXAGON_RADIUS_MIN = 0.025f;
        private const float HEXAGON_RADIUS_MAX = 0.5f;

        private const float SQUARE_ROOT_THREE = 1.732051f;
        private const float SQUARE_ROOT_THREE_HALF = SQUARE_ROOT_THREE * 0.5f;
        private const float SQUARE_ROOT_THREE_DIVIDE_THREE = SQUARE_ROOT_THREE / 3;
        private const float TWO_F_DIVIDE_THREE = 2f / 3;
        private const float NEGATIVE_ONE_F_DIVIDE_THREE = -1f / 3;

        public event UnityAction<IHexGrid> OnEnterTile
        {
            add => _onEnterTile.AddListener(value);
            remove => _onEnterTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnExitTile
        {
            add => _onExitTile.AddListener(value);
            remove => _onExitTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnMouseDownTile
        {
            add => _onMouseDownTile.AddListener(value);
            remove => _onMouseDownTile.RemoveListener(value);
        }

        public event UnityAction<IHexGrid> OnMouseUpTile
        {
            add => _onMouseUpTile.AddListener(value);
            remove => _onMouseUpTile.RemoveListener(value);
        }
        
        private static readonly int _absoluteLimit = Shader.PropertyToID("_AbsoluteLimit");
        private static readonly int _bufferOn = Shader.PropertyToID("_BufferOn");
        private static readonly int _hexOptions = Shader.PropertyToID("_HexOptions");

        [FormerlySerializedAs("_onHighlightTile")]
        [Header("Events")]
        [SerializeField] private UnityEvent<IHexGrid> _onEnterTile = new();
        [SerializeField] private UnityEvent<IHexGrid> _onExitTile = new();
        [SerializeField] private UnityEvent<IHexGrid> _onMouseUpTile = new();
        [SerializeField] private UnityEvent<IHexGrid> _onMouseDownTile = new();

        // TODO(확장): 현재는 패키지 외부 의존성을 줄이기 위해 List를 직렬화합니다.
        // 자체 SerializedDictionary가 별도 패키지로 안정화되면 좌표 기반 직렬화 컬렉션으로 교체를 검토합니다.
        [SerializeField] private List<HexGrid> _hexGrids = new();
        private readonly Dictionary<AxialCoordinates, HexGrid> _hexGridLookup = new();

        [SerializeField, InitializeRequireComponent] private DecalProjector _decalProjector;

        [FormerlySerializedAs("_qLimit")]
        [SerializeField, Min(0)] private int _tileLimit = 3;

        [SerializeField] private Camera _mainCamera;

        private ComputeBuffer _hexOptionBuffer;
        private HexGrid _currentHex = null;
        private static readonly int _radius = Shader.PropertyToID("_Radius");

        [field: SerializeField, Range(HEXAGON_RADIUS_MIN, HEXAGON_RADIUS_MAX)] public float HexagonRadius { get; set; } = 0.025f;
        [field: SerializeField] public GameObject RootObject { get; private set; }

        public float HexagonWidth => HexagonRadius * _decalProjector.size.x * 2 * _decalProjector.transform.localScale.x;

        public bool IsRender
        {
            get => _decalProjector.enabled;
            set => _decalProjector.enabled = value;
        }

        public int TileCount
        {
            get
            {
                int limitDouble = _tileLimit * 2;
                return (JeomseonMath.GetArithmeticSeriesSum(limitDouble) - JeomseonMath.GetArithmeticSeriesSum(_tileLimit)) * 2 + limitDouble + 1;
            }
        }

        public int TileLimit
        {
            get => _tileLimit;
            set
            {
                _tileLimit = value;
                _decalProjector.material.SetFloat(_absoluteLimit, _tileLimit);
            }
        }

        [field: SerializeField] public LayerMask LayerMask { get; set; }

        private void Awake()
        {
            RebuildLookup();
            SendToShaderHexOption();
        }

        private void Start()
        {
            if (!_mainCamera)
            {
                Debug.LogWarning("카메라가 초기화되지 않았으므로 자동으로 메인카메라를 찾습니다");
                _mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || _mainCamera == null)
            {
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            OnHoverMouse(position);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                OnMouseDownEvent(position);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                OnMouseUpEvent(position);
            }
        }

        private void OnApplicationQuit()
        {
            _hexOptionBuffer?.Release();
            _hexOptionBuffer = null;
        }

        private void OnDestroy()
        {
            _hexOptionBuffer?.Release();
            _hexOptionBuffer = null;
        }

        private void OnMouseDownEvent(Vector2 mousePosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

            if (TryGetTileDataByRayInternal(ray, out (bool, RaycastHit) _, out HexGrid hex))
            {
                hex.InvokeOnMouseDownTile();
                _onMouseDownTile.Invoke(hex);
            }
        }

        private void OnMouseUpEvent(Vector2 mousePosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

            if (TryGetTileDataByRayInternal(ray, out (bool, RaycastHit) _, out HexGrid hex))
            {
                hex.InvokeOnMouseUpTile();
                _onMouseUpTile.Invoke(hex);
            }
        }

        private void OnHoverMouse(Vector2 mousePosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

            if (TryGetTileDataByRayInternal(ray, out (bool, RaycastHit) _, out HexGrid hex))
            {
                if (_currentHex != hex)
                {
                    HexGrid prevHex = _currentHex;
                    _currentHex = hex;
                    _currentHex.InvokeOnEnterTile();
                    _onEnterTile.Invoke(_currentHex);

                    if (prevHex is not null)
                    {
                        prevHex.InvokeOnExitTile();
                        _onExitTile.Invoke(prevHex);
                    }
                }
            }
            else
            {
                if (_currentHex is not null)
                {
                    _currentHex.InvokeOnExitTile();
                    _onExitTile.Invoke(_currentHex);
                    _currentHex = null;
                }
            }
        }

        public void SetActiveAxialCoordinates(int q, int r, bool isActive)
        {
            if (!_hexGridLookup.TryGetValue(new(q, r), out HexGrid hex)) return;

            hex.IsActive = isActive;
        }

        private bool TryGetTileDataByRayInternal(in Ray ray, out (bool, RaycastHit) hitTuple, out HexGrid hexGrid)
        {
            hexGrid = null;
            hitTuple.Item1 = false;

            if (Physics.Raycast(ray, out hitTuple.Item2, Mathf.Infinity, LayerMask))
            {
                hitTuple.Item1 = true;
                
                Vector2 convertedPosition = new(
                    hitTuple.Item2.point.x - _decalProjector.transform.position.x, 
                    hitTuple.Item2.point.z - _decalProjector.transform.position.z);
                
                convertedPosition /= _decalProjector.size.x * _decalProjector.GetLocalScaleX();

                Vector2 axialCoordinates = new(
                    TWO_F_DIVIDE_THREE * convertedPosition.x / HexagonRadius,
                    (NEGATIVE_ONE_F_DIVIDE_THREE * convertedPosition.x + SQUARE_ROOT_THREE_DIVIDE_THREE * convertedPosition.y) / HexagonRadius);

                float sFloat = -axialCoordinates.x - axialCoordinates.y;

                Vector3Int roundHex = new(
                    Mathf.RoundToInt(axialCoordinates.x),
                    Mathf.RoundToInt(axialCoordinates.y),
                    Mathf.RoundToInt(sFloat));

                Vector3 hexDifferent = new(
                    Mathf.Abs(roundHex.x - axialCoordinates.x),
                    Mathf.Abs(roundHex.y - axialCoordinates.y),
                    Mathf.Abs(roundHex.z - sFloat));

                Vector3Int hexCoordinates = roundHex;

                switch (hexDifferent)
                {
                    case var _ when hexDifferent.x > hexDifferent.y && hexDifferent.x > hexDifferent.z:
                        hexCoordinates.x = -roundHex.y - roundHex.z;
                        break;
                    case var _ when hexDifferent.y > hexDifferent.z:
                        hexCoordinates.y = -roundHex.x - roundHex.z;
                        break;
                    default:
                        hexCoordinates.z = -roundHex.z - roundHex.y;
                        break;
                }

                if (_hexGridLookup.TryGetValue(new(hexCoordinates.x, hexCoordinates.y), out HexGrid hex))
                {
                    hexGrid = hex;
                    return hex.IsActive;
                }
            }

            return false;
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
            bool isSuccesses = TryGetTileDataByRayInternal(ray, out hitTuple, out HexGrid hex);
            hexGrid = hex;
            return isSuccesses;
        }

        public void CalculateTile()
        {
            // TODO(성능): 큰 그리드에서는 전체 재계산 대신 변경 영역만 갱신하고,
            // 좌표 계산을 Burst/Jobs로 옮길 수 있도록 순수 계산 계층을 분리합니다.
            Vector3 projectorSize = _decalProjector.size * _decalProjector.GetLocalScaleX();

            for (int q = -_tileLimit; q <= _tileLimit; q++)
            {
                for (int r = -_tileLimit; r <= _tileLimit; r++)
                {
                    int s = -(q + r);
                    if (s >= -_tileLimit && s <= _tileLimit)
                    {
                        Vector2 hexNormalizedPosition = new(
                            HexagonRadius * (1.5f * q),
                            HexagonRadius * (SQUARE_ROOT_THREE_HALF * q + SQUARE_ROOT_THREE * r));

                        Vector3 hexPosition = projectorSize.x * hexNormalizedPosition;

                        hexPosition = new(
                            hexPosition.x + _decalProjector.transform.position.x,
                            0.0f,
                            hexPosition.y + _decalProjector.transform.position.z);

                        Ray ray = new(
                            new(
                                hexPosition.x,
                                _decalProjector.transform.position.y + projectorSize.y * 0.5f,
                                hexPosition.z),
                            Vector3.down);

                        if (Physics.Raycast(ray, out RaycastHit hit, projectorSize.y, 1 << 3))
                        {
                            hexPosition = hit.point;
                        }

                        AxialCoordinates key = new(q, r);
                        HexGrid newHex = CreateTile();

                        if (_hexGridLookup.TryGetValue(key, out HexGrid hex))
                        {
                            hex.Properties.ForEach(newHex.AddProperty);
                            newHex.IsActive = hex.IsActive;
                            newHex.Color = hex.Color;
                            int index = _hexGrids.IndexOf(hex);
                            _hexGrids[index] = newHex;
                            _hexGridLookup[key] = newHex;
                        }
                        else
                        {
                            _hexGrids.Add(newHex);
                            _hexGridLookup.Add(key, newHex);
                        }

                        HexGrid CreateTile()
                        {
                            HexGrid createdHex = new(q, r, hexPosition, hexNormalizedPosition + new Vector2(0.5f, 0.5f), _tileLimit);
                            createdHex.OnChangedActive += OnChangedActiveTile;
                            createdHex.OnChangedColor += OnChangedColorTile;
                            createdHex.OnEnterTile += _onEnterTile.Invoke;
                            createdHex.OnMouseDownTile += _onMouseDownTile.Invoke;
                            createdHex.OnMouseUpTile += _onMouseUpTile.Invoke;
                            createdHex.OnExitTile += _onExitTile.Invoke;
                            return createdHex;
                        }
                    }
                }
            }

            SendToShaderHexOption();
        }

        private void OnChangedActiveTile(IHexGrid iHex, bool isActive)
        {
            SendToShaderHexOption();
        }

        private void OnChangedColorTile(IHexGrid iHex, Color color)
        {
            SendToShaderHexOption();
        }

        public void SendToShaderHexOption()
        {
            // TODO(성능): 값 하나가 변경될 때마다 전체 ComputeBuffer를 재생성하지 않고
            // dirty index만 갱신하며 GraphicsBuffer 사용 가능 여부도 검토합니다.
            HexOption[] hexOptions = _hexGrids
                .Select(hex => hex.GetShaderOption())
                .ToArray();
            
            int stride = Marshal.SizeOf<HexOption>();
            bool isZero = hexOptions.Length > 0;

            _decalProjector.material.SetInt(_bufferOn, isZero ? 1 : 0);

            if (isZero)
            {
                _hexOptionBuffer?.Release();
                _hexOptionBuffer = new(hexOptions.Length, stride);
                _hexOptionBuffer.SetData(hexOptions);
                _decalProjector.material.SetBuffer(_hexOptions, _hexOptionBuffer);
            }
        }

        public List<IHexGrid> GetGrids() => _hexGrids.OfType<IHexGrid>().ToList();

        public void OnBeforeSerialize()
        {
            SendToShaderHexOption();
        }

        public void OnAfterDeserialize()
        {
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _hexGridLookup.Clear();

            foreach (HexGrid hexGrid in _hexGrids)
            {
                if (hexGrid is not null)
                {
                    _hexGridLookup[hexGrid.HexPoint] = hexGrid;
                }
            }
        }

        #if UNITY_EDITOR
        [OnChangedValueForMethod(nameof(_tileLimit))]
        private void OnChangedLimit()
        {
            _decalProjector.material.SetFloat(_absoluteLimit, _tileLimit);
        }

        [OnChangedValueForMethod(nameof(HexagonRadius))]
        private void OnChangedRadius()
        {
            _decalProjector.material.SetFloat(_radius, HexagonRadius);
        }
        #endif
    }
}
