# Grid Tile System 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **완료(코드) — P0-01 — 좌표·인덱스 정확성** (2026-08-19, Unity 실측은 아직 안 됨)
   - **실제 결함 발견·수정**: `GridManager.TryGetTileDataByRayInternal`의 레이캐스트 타일 피킹이
     쓰던 큐브 좌표 라운딩(cube coordinate rounding)에서, S 성분의 오차가 가장 클 때
     `hexCoordinates.z = -roundHex.x - roundHex.y`여야 할 보정식이 `-roundHex.z - roundHex.y`로
     **자기 자신(z)을 참조**하고 있었습니다. 예를 들어 축 좌표 (0.3, 0.3)을 넣으면 올바른 결과는
     (0,0,0)인데 이 버그로 (0,0,1)이 나와 `Q+R+S=0` 불변식 자체가 깨졌습니다 — 단순히 인접 타일이
     아니라 **존재할 수 없는 좌표**가 선택될 수 있었습니다(테스트로 재현·고정: `Round_Always
     SatisfiesCubeCoordinateInvariant`, `Round_WhenSHasTheLargestRoundingError_...`).
   - 이 라운딩 로직을 `GridManager`에서 `HexCoordinates.Round(in Vector2)` 정적 메서드로
     추출했습니다(`HexGridStructs.cs`) — `Physics.Raycast` 없이 순수 단위 테스트로 검증 가능해졌고,
     `GridManager` 쪽 코드도 20여 줄에서 3줄로 줄었습니다.
   - `HexGrid.GetHexIndex`(축 좌표 → 연속 인덱스 매핑)는 직접 유도해보니 수학적으로 이미
     올바른 구현이었습니다(경계 폭 공식·누적합 보정 모두 정확). 기존에 남아 있던 TODO는 "검증이
     안 됨"이었지 "버그가 있음"이 아니었던 것으로 확인 — `HexGridIndexTests`로 limit
     0/1/2/3/5에 대해 인덱스가 `0..TileCount-1` 범위에서 연속·중복 없음을 확인하는 회귀 테스트를
     추가하고 TODO 주석은 제거했습니다.
   - `Runtime`/`Tests` 모두 `dotnet build`로 컴파일 오류 0개 확인(신규 테스트 파일은
     `TestProject`의 stale csproj에 `<Compile Include>`를 수동 추가해 검증). 실제 Unity Test
     Runner 실행과 Scene에서의 레이캐스트 피킹 육안 확인은 아직 안 함.
2. **완료(코드) — P1-01 — GridManager 책임 분리** (2026-08-20, Unity 실측은 아직 안 됨)
   - `GridManager`(418줄)를 조합 루트로 축소하고, 입력·Raycast·타일 데이터·선택 상태·GPU 갱신을
     `Runtime/Services/`의 순수 C# 서비스 5개로 분리했습니다: `HexGridPointerInput`(입력),
     `HexGridTilePicker`(레이캐스트/피킹), `HexGridTileDataStore`(타일 데이터), `HexGridSelectionState`
     (호버·클릭 디스패치), `HexOptionBufferUploader`(GPU ComputeBuffer 업로드). 각각 인터페이스를
     가지며 `GridManager.EnsureServices()`에서 조립합니다(TODO가 요구하던 "인터페이스로 주입"
     충족). `hexGrids` 필드는 이름·직렬화 위치 그대로 `GridManager`에 남겨 P1-03의 reflection/
     `FindProperty("hexGrids")` 접근이 그대로 유효합니다.
   - **설정값 ScriptableObject 분리(사용자 요청으로 범위 확장)**: `HexagonRadius`/`TileLimit`/
     `LayerMask`를 새 `HexGridSettings : ScriptableObject`(`Runtime/HexGridSettings.cs`)로
     옮겼습니다. `decalProjector`/`mainCamera`/`RootObject`/`hexGrids`는 Scene-로컬 데이터라 그대로
     `GridManager`에 둡니다. 기존 `[InvokeOnInspectorChange]`(`Jeomseon.Unity.Attributes`) 메커니즘은
     필드가 별도 asset으로 옮겨가며 더 이상 발화하지 않게 되어 제거했고, 대신
     `HexGridSettings.OnValidate()` + `SettingsChanged` C# 이벤트로 대체했습니다
     (`GridManager.OnEnable`/`OnDisable`에서 대칭 구독). 부수 효과로 Runtime 스크립트에서
     `HexagonRadius`를 바꿔도 이제 DecalProjector 머티리얼이 갱신됩니다(기존엔 에디터 전용
     콜백만 있어 Runtime 변경이 반영되지 않았음 — 개선). **(Breaking)** `HexagonRadius`/`TileLimit`/
     `LayerMask`의 직렬화 위치가 `GridManager` 자신에서 `HexGridSettings` asset 참조로 바뀌었습니다
     — Scene Sample이 아직 없어 마이그레이션할 기존 `.unity` 자산은 없습니다. 다음에 Scene Sample을
     만들 때 `HexGridSettings` asset도 함께 생성해 `GridManager.settings`에 연결해야 합니다.
   - **버그 수정**: `OnEnterTile`/`OnExitTile`/`OnMouseDownTile`/`OnMouseUpTile` 4개 이벤트가 전부
     리스너를 2회씩 호출하던 결함을 고쳤습니다. 원인은 `CalculateTile()`의 `CreateTile`이 타일별
     이벤트를 매니저 `UnityEvent`에 미리 구독(`createdHex.OnEnterTile += onEnterTile.Invoke`)해두는
     동시에, 호출부(`OnHoverMouse` 등)에서도 `hex.InvokeOnEnterTile()` 직후
     `onEnterTile.Invoke(hex)`를 또 직접 호출해 매니저 이벤트가 이중 발화된 것이었습니다.
     `HexGridSelectionState`가 각 경로(Enter/Exit/MouseDown/MouseUp)를 정확히 1회씩만 발화하도록
     재설계해 해결했으며, `HexGridSelectionStateTests`에 회귀 테스트를 추가했습니다.
   - `Update()`가 프레임당 동일한 레이로 피킹을 3회(호버/mousedown/mouseup) 중복 호출하던 것도
     1회로 합쳤습니다(부수효과 없는 순수 조회라 결과가 항상 같음 — P2-01 성능 재설계가 아니라
     리팩터 과정에서 자연히 없어진 중복 제거).
   - 새 테스트: `HexGridTileDataStoreTests`(Rebuild/SetActive/TryGetTile/RebuildLookup/
     `TileVisualsChanged` 발화), `HexGridSelectionStateTests`(이중 발화 버그 회귀). `Tests`
     asmdef에 `Unity.RenderPipelines.Universal.Runtime` 참조를 추가해 `DecalProjector`를 직접
     생성하는 EditMode 테스트가 가능해졌습니다(Physics 의존 지면 스냅 분기는 Edit Mode에서 항상
     no-hit이라 예외 없이 통과, 정확도 검증은 아님).
   - `dotnet build`로 Runtime/Editor/Tests 전부 컴파일 오류 0개 확인. **Unity Test Runner 실행과
     Inspector에서 `HexGridSettings` asset 생성/할당 동작 확인은 아직 안 함.**
3. **P1-02 — 순수 타일 데이터와 Unity 이벤트 분리**
   - 직렬화 가능한 데이터 모델과 런타임 상호작용 상태의 경계를 정의합니다.
4. **P1-03 — Inspector Reflection 제거**
   - GridManager가 명시적인 Editor 조회 API 또는 SerializedProperty 경로를 제공합니다.
5. **P2-01~03 — 부분 갱신과 Scene View 최적화**
   - 변경 영역만 계산하고 ComputeBuffer를 재사용하며 라벨을 가시 범위로 제한합니다.
   - Scene View 타일 옵션 UI는 EditorToolkit의 범용 IMGUI 창 대신 Unity `Overlay` API로
     이전했습니다(완료).
6. **P3-01 — 렌더링 백엔드 추상화**
   - URP Decal 구현을 인터페이스 뒤로 격리해 다른 파이프라인 확장을 허용합니다.
7. **P3-02 — SerializedDictionary 연동**
   - 별도 패키지가 안정화되면 좌표 조회 성능과 Inspector 편의성을 비교합니다.
