# Grid Tile System 로드맵

## 2026-08-20 Projector/Shader 분리

- [x] `GridTileSystem -> Projector`, `GridTileSystem -> Shaders` 단방향 패키지 경계 적용
- [x] URP Decal 직접 의존, Renderer Feature Auto Fix와 외부 Material 소유권 제거
- [x] 단일 육각형 도형 렌더링과 Grid 중심/인덱스 계산 분리
- [x] 외곽 타일의 중심 기준 범위 판정으로 잘린 외곽선 수정
- [x] MeshRenderer, SkinnedMeshRenderer, Terrain receiver 수집 테스트
- [x] Unity CLI 컴파일, GridTileSystem EditMode 76/76, Projector EditMode 5/5, Shaders EditMode
      11/11 통과 (2026-08-20 16:19, 컴파일 오류 0건 — `gridtile-stabilize-final-compile.log`,
      `gridtile-stabilize-tests.xml`, `projector-stabilize-final-tests.xml`,
      `shaders-stabilize-tests.xml`)
- [x] `MeshProjector` 코드 리뷰: Terrain 메시는 해상도·`terrainData` 교체·heightmap 페인트 시에만
      재생성되도록 정확히 캐시됨. SkinnedMesh는 매 프레임 `BakeMesh` 호출이 정확한 변형 반영을 위해
      구조적으로 불가피함 — 버그 아님, 최종 문서화 때 비용으로 명시 필요(TODO)
- [x] `HexagonShapeCore.hlsl`(단일 육각형 외곽선만) / `HexGridCore.hlsl`(중심·인덱스·격자소속만,
      shape core를 include) 경계 코드 리뷰로 재확인
- [x] `HexGridProjection.shader`의 외곽 타일 외곽선 유지 계약(`JeomseonTryGetVisibleHexCell`의 이웃
      셀 탐색, XY 박스 클립 없음) 코드 리뷰로 재확인 — 회귀 없음
- [x] README/README.en/CHANGELOG/package.json/asmdef 참조 정합성 확인 — `DecalProjector`/`URP`/
      구 클래스명 잔존 없음
- [ ] Sample Game View에서 정적 메시/Terrain 렌더링과 포인터 상호작용 사용자 육안 확인 (아래
      체크리스트)

### 사용자 육안 확인 체크리스트 (2026-08-20)

**`Jeomseon.Unity.Packages.TestProject`에서 확인** (URP 전용 프로젝트가 아니어도 됩니다 — 렌더
파이프라인 비종속 설계로 바뀌었습니다):

1. Package Manager에서 GridTileSystem의 `Basic Usage` Sample을 Import하고
   `HexGridBasicUsage.unity`를 Play Mode로 실행합니다.
   - Game View에 육각형 그리드가 지면(Plane) 위에 투영되어 보이는지
   - 가장 바깥쪽 타일들의 외곽선이 잘리지 않고 전체 육각형 모양으로 보이는지(각지거나 끊긴
     부분이 없는지)
   - Console에 `Generated hex tile count: 127`이 정확히 한 번만 출력되는지
   - 포인터를 움직여 타일에 진입/이탈할 때 좌표 로그가 각각 한 번씩만 출력되는지(중복 발화 없음)
   - Play Mode 진입·종료 시 예외나 ComputeBuffer 관련 경고가 콘솔에 없는지
2. Edit Mode에서 Inspector의 `Rebuild Tiles → Clear Baked Tiles → Rebuild Tiles`를 반복해도 예외가
   없는지, Scene View에서 타일을 클릭하면 Overlay가 열리고 `Content Opacity` 슬라이더가 동작하는지
3. Projector 패키지의 `Basic Usage` Sample을 Import하고 `ProjectorBasicUsage.unity`를 Play Mode로
   실행합니다.
   - 일반 Mesh(Plane 등) 표면에 투영 텍스처/색상이 정상적으로 나타나는지
   - Terrain 표면에도 투영이 끊기거나 왜곡 없이 나타나는지(Terrain Receiver 사용 시)
   - Scene에서 Terrain을 편집(높이 페인트)했을 때 투영이 새 지형에 맞춰 갱신되는지

위 항목은 자동화 테스트(76+5+11개 EditMode 테스트)로 검증할 수 없는 실제 렌더링·상호작용
계약이라 사용자 확인이 필요합니다. 확인 전에는 커밋·태그·push하지 않습니다.

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **완료(코드) — P0-01 — 좌표·인덱스 정확성** (2026-08-19, Unity 실측은 아직 안 됨)
   - **실제 결함 발견·수정**: `HexGridController.TryGetTileDataByRayInternal`의 레이캐스트 타일 피킹이
     쓰던 큐브 좌표 라운딩(cube coordinate rounding)에서, S 성분의 오차가 가장 클 때
     `hexCoordinates.z = -roundHex.x - roundHex.y`여야 할 보정식이 `-roundHex.z - roundHex.y`로
     **자기 자신(z)을 참조**하고 있었습니다. 예를 들어 축 좌표 (0.3, 0.3)을 넣으면 올바른 결과는
     (0,0,0)인데 이 버그로 (0,0,1)이 나와 `Q+R+S=0` 불변식 자체가 깨졌습니다 — 단순히 인접 타일이
     아니라 **존재할 수 없는 좌표**가 선택될 수 있었습니다(테스트로 재현·고정: `Round_Always
     SatisfiesCubeCoordinateInvariant`, `Round_WhenSHasTheLargestRoundingError_...`).
   - 이 라운딩 로직을 `HexGridController`에서 `HexCoordinates.Round(in Vector2)` 정적 메서드로
     추출했습니다(`HexGridStructs.cs`) — `Physics.Raycast` 없이 순수 단위 테스트로 검증 가능해졌고,
     `HexGridController` 쪽 코드도 20여 줄에서 3줄로 줄었습니다.
   - `HexGrid.GetHexIndex`(축 좌표 → 연속 인덱스 매핑)는 직접 유도해보니 수학적으로 이미
     올바른 구현이었습니다(경계 폭 공식·누적합 보정 모두 정확). 기존에 남아 있던 TODO는 "검증이
     안 됨"이었지 "버그가 있음"이 아니었던 것으로 확인 — `HexTileIndexTests`로 limit
     0/1/2/3/5에 대해 인덱스가 `0..TileCount-1` 범위에서 연속·중복 없음을 확인하는 회귀 테스트를
     추가하고 TODO 주석은 제거했습니다.
   - `Runtime`/`Tests` 모두 `dotnet build`로 컴파일 오류 0개 확인(신규 테스트 파일은
     `TestProject`의 stale csproj에 `<Compile Include>`를 수동 추가해 검증). 실제 Unity Test
     Runner 실행과 Scene에서의 레이캐스트 피킹 육안 확인은 아직 안 함.
2. **완료(코드) — P1-01 — HexGridController 책임 분리** (2026-08-20, Unity 실측은 아직 안 됨)
   - `HexGridController`(418줄)를 조합 루트로 축소하고, 입력·Raycast·타일 데이터·선택 상태·GPU 갱신을
     `Runtime/Services/`의 순수 C# 서비스 5개로 분리했습니다: `HexGridPointerInput`(입력),
     `HexTilePicker`(레이캐스트/피킹), `HexTileStore`(타일 데이터), `HexTileSelectionState`
     (호버·클릭 디스패치), `HexTileBufferUploader`(GPU ComputeBuffer 업로드). 각각 인터페이스를
     가지며 `HexGridController.EnsureServices()`에서 조립합니다(TODO가 요구하던 "인터페이스로 주입"
     충족). 직렬화 필드는 역할이 분명한 `tiles`로 변경하고 이전 이름은
     `FormerlySerializedAs`로 마이그레이션합니다.
   - **설정값 ScriptableObject 분리(사용자 요청으로 범위 확장)**: `TileRadius`/`GridRadius`/
     `InteractionLayerMask`를 새 `HexGridSettings : ScriptableObject`(`Runtime/HexGridSettings.cs`)로
     옮겼습니다. `projector`/`mainCamera`/`tiles`는 Scene-로컬 데이터라 Controller에 둡니다.
     렌더링에 필요 없던 `RootObject`는 제거했습니다. 기존 `[InvokeOnInspectorChange]` 메커니즘은
     필드가 별도 asset으로 옮겨가며 더 이상 발화하지 않게 되어 제거했고, 대신
     `HexGridSettings.OnValidate()` + `SettingsChanged` C# 이벤트로 대체했습니다
     (`HexGridController.OnEnable`/`OnDisable`에서 대칭 구독). 부수 효과로 Runtime 스크립트에서
     `TileRadius`를 바꿔도 Projector 프로퍼티와 직렬화 타일이 자동 갱신됩니다.
   - **버그 수정**: `OnEnterTile`/`OnExitTile`/`OnMouseDownTile`/`OnMouseUpTile` 4개 이벤트가 전부
     리스너를 2회씩 호출하던 결함을 고쳤습니다. 원인은 `CalculateTile()`의 `CreateTile`이 타일별
     이벤트를 매니저 `UnityEvent`에 미리 구독(`createdHex.OnEnterTile += onEnterTile.Invoke`)해두는
     동시에, 호출부(`OnHoverMouse` 등)에서도 `hex.InvokeOnEnterTile()` 직후
     `onEnterTile.Invoke(hex)`를 또 직접 호출해 매니저 이벤트가 이중 발화된 것이었습니다.
     `HexTileSelectionState`가 각 경로(Enter/Exit/MouseDown/MouseUp)를 정확히 1회씩만 발화하도록
     재설계해 해결했으며, `HexTileSelectionStateTests`에 회귀 테스트를 추가했습니다.
   - `Update()`가 프레임당 동일한 레이로 피킹을 3회(호버/mousedown/mouseup) 중복 호출하던 것도
     1회로 합쳤습니다(부수효과 없는 순수 조회라 결과가 항상 같음 — P2-01 성능 재설계가 아니라
     리팩터 과정에서 자연히 없어진 중복 제거).
   - 새 테스트: `HexTileStoreTests`(Rebuild/SetActive/TryGetTile/RebuildLookup/
     `TileVisualsChanged` 발화), `HexTileSelectionStateTests`(이중 발화 버그 회귀),
     `HexGridControllerConfigurationTests`(Projector 구성·Bake/Clear/Bake 수명 회귀)를 추가했습니다.
   - Unity 6000.5.7f1 EditMode 테스트 76개를 통과했습니다. Inspector/렌더링은 사용자 육안 확인이
     남아 있습니다.
3. **P1-02 — 순수 타일 데이터와 Unity 이벤트 분리**
   - 직렬화 가능한 데이터 모델과 런타임 상호작용 상태의 경계를 정의합니다.
4. **완료 — P1-03 — Inspector Reflection 제거** (2026-08-20)
   - `HexGridController.Tiles` 읽기 API와 `tiles` SerializedProperty를 사용합니다. Inspector가 private
     List를 직접 비우던 코드는 Store의 `Clear()`로 교체해 Bake/Clear/Bake 회귀를 막았습니다.
5. **P2-01~03 — 부분 갱신과 Scene View 최적화**
   - ComputeBuffer 재사용은 완료했습니다. 부분 갱신은 보류합니다.
   - 모든 타일의 상시 라벨을 제거하고 선택 타일 좌표만 Scene View에 표시합니다. 상세 정보는
     투명도 조절이 가능한 Overlay에만 둡니다(완료).
   - Scene View 타일 옵션 UI는 EditorToolkit의 범용 IMGUI 창 대신 Unity `Overlay` API로
     이전했습니다(완료).
6. **완료 — P3-01 — 렌더링 백엔드 분리**
   - URP Decal 직접 의존을 제거하고 독립 `Jeomseon.Unity.Projector` 패키지의 `MeshProjector`와
     검증된 Effect 계약으로 교체했습니다. Grid 수학은 `Jeomseon.Unity.Shaders`에 분리했습니다.
7. **P3-02 — SerializedDictionary 연동**
   - 별도 패키지가 안정화되면 좌표 조회 성능과 Inspector 편의성을 비교합니다.

## Scene Sample (2026-08-20)

- `HexGridSettings`를 Grid 크기의 단일 기준으로 사용하며 Controller Inspector에서 참조 Settings를
  인라인 편집할 수 있습니다.
- `Samples~/BasicUsage/HexGridBasicUsage.unity`에는 Main Camera, Directional Light, Layer 3 충돌
  지면, `MeshProjector`, `HexGridProjectorEffect`, `HexGridController`, `HexGridSample`이 미리
  연결돼 있습니다. Render Pipeline Asset, Decal Renderer Feature, Auto Fix가 필요 없습니다.
- `HexGridSettings.asset`은 `Tile Radius 0.038`, `Grid Radius 6`, `Interaction Layer Mask 8`로 설정하고
  `HexGridController.settings`에 연결했습니다. Play Mode 시작 시 127개 타일을 생성합니다.
- 샘플 스크립트는 타일 생성 수와 포인터 진입 좌표를 Console에 기록하고 `OnDestroy`에서 이벤트를
  해제합니다. 타일 진입 이벤트 이중 발화 수정도 눈으로 확인할 수 있습니다.
- Unity 6000.5.7f1 CLI로 Runtime/Editor/Sample 컴파일과 Scene의 127개 직렬화 타일을 확인했습니다.
  남은 사용자 실측은 일반 Mesh/Terrain 표시, 외곽 타일 전체 외곽선, 포인터 이벤트 1회 발화,
  Overlay 투명도 및 Play Mode 진입·종료 경고 부재입니다.
- 최종 문서화 단계에서 Scene Game View와 `HexGridController`/`HexGridSettings` Inspector 화면을
  `Documentation~/Images`에 캡처하고 한·영 README에 같은 검증 절차를 반영해야 합니다.
