using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Rendering.Universal;
using Jeomseon.Unity.Attributes;
using Jeomseon.Collections;

namespace Jeomseon.Unity.GridTileSystem
{
    public sealed class GridManager : MonoBehaviour, ISerializationCallbackReceiver
    {
        private const float HexagonRadiusMin = 0.025f;
        private const float HexagonRadiusMax = 0.5f;

        private const float SquareRootThree = 1.732051f;
        private const float SquareRootThreeHalf = SquareRootThree * 0.5f;
        private const float SquareRootThreeDivideThree = SquareRootThree / 3;
        private const float TwoFDivideThree = 2f / 3;
        private const float NegativeOneFDivideThree = -1f / 3;

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
        private static readonly int _bufferOn = Shader.PropertyToID("_BufferOn");
        private static readonly int _hexOptions = Shader.PropertyToID("_HexOptions");

        [FormerlySerializedAs("_onHighlightTile")]
        [Header("Events")]
        [SerializeField, FormerlySerializedAs("_onEnterTile")] private UnityEvent<IHexGrid> onEnterTile = new();
        [SerializeField, FormerlySerializedAs("_onExitTile")] private UnityEvent<IHexGrid> onExitTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseUpTile")] private UnityEvent<IHexGrid> onMouseUpTile = new();
        [SerializeField, FormerlySerializedAs("_onMouseDownTile")] private UnityEvent<IHexGrid> onMouseDownTile = new();

        /* TODO(P3-02, extensibility): 현재는 패키지 외부 의존성을 줄이기 위해 List를 직렬화합니다.
         * 자체 SerializedDictionary가 별도 패키지로 안정화되면 좌표 기반 직렬화 컬렉션으로 교체를 검토합니다.
         */
        [SerializeField, FormerlySerializedAs("_hexGrids")] private List<HexGrid> hexGrids = new();
        private readonly Dictionary<AxialCoordinates, HexGrid> _hexGridLookup = new();

        [SerializeField, GetOrAddComponent, FormerlySerializedAs("_decalProjector")] private DecalProjector decalProjector;

        [FormerlySerializedAs("_qLimit")]
        [SerializeField, Min(0), FormerlySerializedAs("_tileLimit")] private int tileLimit = 3;

        [SerializeField, FormerlySerializedAs("_mainCamera")] private Camera mainCamera;

        private ComputeBuffer _hexOptionBuffer;
        private HexGrid _currentHex = null;
        private static readonly int _radius = Shader.PropertyToID("_Radius");

        [field: SerializeField, Range(HexagonRadiusMin, HexagonRadiusMax)] public float HexagonRadius { get; set; } = 0.025f;
        [field: SerializeField] public GameObject RootObject { get; private set; }

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
                int limitDouble = tileLimit * 2;
                int outerSum = limitDouble * (limitDouble + 1) / 2;
                int innerSum = tileLimit * (tileLimit + 1) / 2;
                return (outerSum - innerSum) * 2 + limitDouble + 1;
            }
        }

        public int TileLimit
        {
            get => tileLimit;
            set
            {
                tileLimit = value;
                decalProjector.material.SetFloat(_absoluteLimit, tileLimit);
            }
        }

        [field: SerializeField] public LayerMask LayerMask { get; set; }

        /* TODO(P1-01, architecture): 입력 처리, 물리 레이캐스트, 타일 데이터, GPU 버퍼 갱신 책임을
         * 각각의 서비스로 분리하고 인터페이스로 주입하여 테스트 가능한 구조로 변경합니다.
         */
        private void Awake()
        {
            RebuildLookup();
            SendToShaderHexOption();
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
            Mouse mouse = Mouse.current;
            if (mouse == null || mainCamera == null)
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
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            if (TryGetTileDataByRayInternal(ray, out (bool, RaycastHit) _, out HexGrid hex))
            {
                hex.InvokeOnMouseDownTile();
                onMouseDownTile.Invoke(hex);
            }
        }

        private void OnMouseUpEvent(Vector2 mousePosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            if (TryGetTileDataByRayInternal(ray, out (bool, RaycastHit) _, out HexGrid hex))
            {
                hex.InvokeOnMouseUpTile();
                onMouseUpTile.Invoke(hex);
            }
        }

        private void OnHoverMouse(Vector2 mousePosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            if (TryGetTileDataByRayInternal(ray, out (bool, RaycastHit) _, out HexGrid hex))
            {
                if (_currentHex != hex)
                {
                    HexGrid prevHex = _currentHex;
                    _currentHex = hex;
                    _currentHex.InvokeOnEnterTile();
                    onEnterTile.Invoke(_currentHex);

                    if (prevHex is not null)
                    {
                        prevHex.InvokeOnExitTile();
                        onExitTile.Invoke(prevHex);
                    }
                }
            }
            else
            {
                if (_currentHex is not null)
                {
                    _currentHex.InvokeOnExitTile();
                    onExitTile.Invoke(_currentHex);
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
                    hitTuple.Item2.point.x - decalProjector.transform.position.x, 
                    hitTuple.Item2.point.z - decalProjector.transform.position.z);
                
                convertedPosition /= decalProjector.size.x * decalProjector.transform.localScale.x;

                Vector2 axialCoordinates = new(
                    TwoFDivideThree * convertedPosition.x / HexagonRadius,
                    (NegativeOneFDivideThree * convertedPosition.x + SquareRootThreeDivideThree * convertedPosition.y) / HexagonRadius);

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
            /* TODO(P2-01, performance): 큰 그리드에서는 전체 재계산 대신 변경 영역만 갱신하고,
             * 좌표 계산을 Burst/Jobs로 옮길 수 있도록 순수 계산 계층을 분리합니다.
             */
            Vector3 projectorSize = decalProjector.size * decalProjector.transform.localScale.x;

            for (int q = -tileLimit; q <= tileLimit; q++)
            {
                for (int r = -tileLimit; r <= tileLimit; r++)
                {
                    int s = -(q + r);
                    if (s >= -tileLimit && s <= tileLimit)
                    {
                        Vector2 hexNormalizedPosition = new(
                            HexagonRadius * (1.5f * q),
                            HexagonRadius * (SquareRootThreeHalf * q + SquareRootThree * r));

                        Vector3 hexPosition = projectorSize.x * hexNormalizedPosition;

                        hexPosition = new(
                            hexPosition.x + decalProjector.transform.position.x,
                            0.0f,
                            hexPosition.y + decalProjector.transform.position.z);

                        Ray ray = new(
                            new(
                                hexPosition.x,
                                decalProjector.transform.position.y + projectorSize.y * 0.5f,
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
                            int index = hexGrids.IndexOf(hex);
                            hexGrids[index] = newHex;
                            _hexGridLookup[key] = newHex;
                        }
                        else
                        {
                            hexGrids.Add(newHex);
                            _hexGridLookup.Add(key, newHex);
                        }

                        HexGrid CreateTile()
                        {
                            HexGrid createdHex = new(q, r, hexPosition, hexNormalizedPosition + new Vector2(0.5f, 0.5f), tileLimit);
                            createdHex.OnChangedActive += OnChangedActiveTile;
                            createdHex.OnChangedColor += OnChangedColorTile;
                            createdHex.OnEnterTile += onEnterTile.Invoke;
                            createdHex.OnMouseDownTile += onMouseDownTile.Invoke;
                            createdHex.OnMouseUpTile += onMouseUpTile.Invoke;
                            createdHex.OnExitTile += onExitTile.Invoke;
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

        /* TODO(P3-01, extensibility): URP DecalProjector 전용 구현을 렌더링 백엔드로 추상화하여
         * 메시, Shader Graph, 다른 렌더 파이프라인 구현을 선택할 수 있게 합니다.
         */
        public void SendToShaderHexOption()
        {
            /* TODO(P2-02, performance): 값 하나가 변경될 때마다 전체 ComputeBuffer를 재생성하지 않고
             * dirty index만 갱신하며 GraphicsBuffer 사용 가능 여부도 검토합니다.
             */
            HexOption[] hexOptions = hexGrids
                .Select(hex => hex.GetShaderOption())
                .ToArray();
            
            int stride = Marshal.SizeOf<HexOption>();
            bool isZero = hexOptions.Length > 0;

            decalProjector.material.SetInt(_bufferOn, isZero ? 1 : 0);

            if (isZero)
            {
                _hexOptionBuffer?.Release();
                _hexOptionBuffer = new(hexOptions.Length, stride);
                _hexOptionBuffer.SetData(hexOptions);
                decalProjector.material.SetBuffer(_hexOptions, _hexOptionBuffer);
            }
        }

        public List<IHexGrid> GetGrids() => hexGrids.OfType<IHexGrid>().ToList();

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

            foreach (HexGrid hexGrid in hexGrids)
            {
                if (hexGrid is not null)
                {
                    _hexGridLookup[hexGrid.HexPoint] = hexGrid;
                }
            }
        }

        #if UNITY_EDITOR
        [InvokeOnInspectorChange(nameof(tileLimit))]
        private void OnChangedLimit()
        {
            decalProjector.material.SetFloat(_absoluteLimit, tileLimit);
        }

        [InvokeOnInspectorChange(nameof(HexagonRadius))]
        private void OnChangedRadius()
        {
            decalProjector.material.SetFloat(_radius, HexagonRadius);
        }
        #endif
    }
}
