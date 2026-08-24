# 변경 기록

이 문서는 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 형식을 따릅니다.

## [Unreleased]

- Grid가 Surface 경계를 넘어 이어지도록 chart 확장을 추가했습니다. `ISurfaceConnectivity`와
  `SurfaceLink`가 boundary Edge 너머의 Surface Edge를 표현하고, `TriangleUnfoldingParameterizer`가
  `ISurfaceProvider`와 연결 계층을 받아 여러 Surface의 Face를 하나의 chart에 계속 펼칩니다. 연결을
  건너도 같은 코사인 법칙과 두 원 교차 연산을 그대로 쓰므로 tangent가 자동으로 이어집니다.
- `GeometrySurfaceConnectivity`를 추가했습니다. boundary Edge의 월드 끝점이 허용 오차 안에서
  일치하고 두 Face 법선이 정합할 때만 연결로 인정하므로, 가깝지만 무관한 표면이나 같은 경계를
  공유하되 반대를 향하는 표면(벽과 그 뒷면)은 연결되지 않습니다. Surface마다 boundary Edge 공간
  색인을 한 번만 만들고 Edge별 질의 결과는 "연결 없음"까지 캐시합니다.
- `SurfacePatch.Surface`의 의미를 **seed Surface**로 좁히고 `SpansMultipleSurfaces`를 추가했습니다.
  Face별 Surface identity는 `SurfacePatchTriangle.Surface`에 있습니다.
- `SurfaceRegionBuilder`가 topology 없이 Patch만으로 Region을 만드는 overload를 제공합니다. Region
  vertex는 Face가 실제로 속한 Surface를 가리키므로 여러 Surface에 걸친 Tile도 올바르게 복원됩니다.
- `SurfaceGridSystem` 기본 구성이 경계 연결을 포함합니다. 연결 계층을 `null`로 주입하면 chart가
  seed Surface 안에서만 확장되는 기존 동작이 유지됩니다.
- 여러 Surface에 걸친 Grid를 `SurfaceGridGeometryBuilder`에 넘기면 명시적으로 거부합니다. Surface마다
  local-to-world 변환이 다르므로 하나의 출력 Mesh로 합치려면 Surface별 변환이 필요하며, 조용히 잘못된
  Geometry를 만드는 대신 실패로 알립니다.

- Grid 격자에 회전을 도입했습니다. `IntrinsicHexLayout`이 chart 원점 기준 회전각을 받으며,
  `IntrinsicHexLayout.FromDirection`은 chart 방향 벡터를 첫 Hex 꼭짓점 방향으로 삼습니다. 회전각
  기본값은 0이고 그때의 중심·꼭짓점·좌표 변환 결과는 회전 도입 전과 동일합니다.
- 격자 열/행 구간 계산을 `SurfaceGridBuilder`에서 `IntrinsicHexLayout`으로 옮겼습니다. 회전이 있으면
  Patch 경계를 격자 좌표계로 역회전한 AABB에서 구간을 구하므로 회전한 격자에서도 경계 Tile을
  빠뜨리지 않습니다.
- `SurfaceChartDirection`을 추가했습니다. Surface local 3D 방향을 seed Face의 chart 2D 방향으로
  옮기며, 표면 법선과 나란해 격자 방향을 정의할 수 없는 입력은 조용히 무시하지 않고 실패로 알립니다.
- `SurfaceGridRequest`와 `SurfaceGridSystem`을 추가했습니다. 사용자는 **월드 seed 위치와 Tile 해상도만**
  지정하고 시스템이 주변 표면을 스스로 찾아 Grid를 만듭니다. Surface를 등록하거나 목록으로 전달하거나
  Triangle index를 입력하는 단계는 없습니다. 선택적으로 월드 초기 방향을 주면 seed 표면의 접평면에
  투영되어 격자 회전이 됩니다.
- `SurfaceGridBuildResult`/`SurfaceGridBuildStatus`로 실패 원인을 구분해 알립니다. 표면 미발견,
  초기 방향 불가, 구축 실패, 완전한 Tile 없음을 각각 진단 문자열과 함께 반환하며 예외를 던지지 않습니다.

- Skinned Mesh 변형 추종을 추가했습니다. `SurfaceReceiverKind.SkinnedMesh` Receiver는 bind pose Mesh로
  topology를 만들고, Grid vertex마다 소속 Triangle 세 정점의 bone 가중치를 barycentric으로 보간해
  같은 bone index끼리 합산·정규화한 `SurfaceSkinBinding`을 Bake 시점에 한 번 만듭니다. 이후에는
  bone Transform에서 유도한 skinning 행렬만 바뀌므로 **매 프레임 `BakeMesh`를 호출하지 않고**
  topology·Patch·clipping·Tile 구성을 그대로 유지한 채 vertex 위치와 법선만 갱신합니다.
  Animator가 bone 자세를 확정한 뒤 실행되도록 `LateUpdate`에서 갱신합니다.
- `ISurfaceGridRenderBackend.ApplyDeformation(positions, normals)`을 추가했습니다. Geometry와 index
  buffer를 유지한 채 vertex stream만 교체하며, 길이가 다르면 기존 index가 무효해지므로 거부합니다.
- `SurfaceGridGeometry.SurfacePoints`를 추가했습니다. vertex별 원본 barycentric binding을 보존하므로
  변형 추종 경로가 Geometry 순서와 어긋날 위험 없이 binding을 파생할 수 있습니다.
- Skinned Receiver의 Collider는 선택 사항입니다. 지정하지 않으면 picking 없이 논리 Grid와 표현만
  유지하며, 지정하면 bind pose 기준으로 picking합니다(변형 중에는 실제 표면과 어긋날 수 있음).
- Terrain heightfield의 position/triangle/adjacency를 계산형 read-only view로 제공하는 virtual topology
  Adapter를 추가했습니다. 전체 Terrain Mesh를 복제하지 않으며 hole Face는 펼침 traversal과 picking에서
  제외합니다. `HexGridReceiver.SurfaceKind`로 Static Mesh와 Terrain 입력을 선택할 수 있습니다.
- `Terrain Usage` Sample Scene을 추가하고 Unity 6000.5.7f1에서 실제 Bake 결과 12개 Tile/952개 vertex와
  전체 EditMode 테스트 121/121을 검증했습니다.
- Patch 펼침 방문 상태를 전체 topology 크기의 배열에서 수용된 Triangle만 보관하는 Dictionary/List로
  변경해 Terrain 크기와 무관하게 설정된 Patch 제한에 비례하도록 했습니다.
- 외곽 생성 정책을 Surface와 일부라도 겹치는 Tile 포함에서 **전체 Hex 면적이 Surface 안에 들어오는
  완전한 Tile만 유지**하는 방식으로 변경했습니다. `SurfaceRegion.IntrinsicArea`와 원본 Hex 면적을
  상대 오차 안에서 비교하므로 여러 Triangle과 접힌 경계를 가로지르는 완전한 Tile은 유지하고,
  Surface 외곽에서 잘린 Tile만 Logical Grid와 출력 Mesh에서 제외합니다.
- **(Breaking)** `HexGridController`의 단일 source/collider/output/seed/tiles 필드를 제거하고
  `Receivers` 목록으로 교체했습니다. 각 `HexGridReceiver`는 독립 topology, Patch, 좌표계, Tile 상태,
  picker와 선택적 Mesh Backend를 소유합니다. 여러 표면의 같은 `(q,r)` 좌표는 서로 다른 Tile이며,
  상태 변경에는 Receiver index를 함께 지정합니다.
- Receiver 하나의 구성 또는 Bake가 실패해도 나머지 Receiver는 계속 생성되는 부분 실패 계약을
  추가했습니다. Picking은 구성된 Receiver Collider들을 직접 검사해 Ray에서 가장 가까운 활성 Tile을
  반환합니다.
- **(Breaking)** `GridRadius` 설정을 제거했습니다. Grid 범위를 seed 중심 Hex ring 개수로 지정하던
  방식은 Projector/Shader 시절의 제약이었습니다. 이제 사용자는 Tile 해상도(`TileRadius`)만 정하고
  Grid는 **Patch가 펼친 Surface 전체를 덮습니다**. 덮는 최대 범위는 기존
  `maximumPatchTriangles`/`maximumPatchRadius`가 성능 안전장치로 계속 제어합니다.
  `SurfaceGridBuilder.Build`에서 `gridRadius` 매개변수가 사라졌고,
  `IHexTileStore.Bake`도 같은 매개변수를 더 이상 받지 않습니다.
- `SurfacePatch.IntrinsicBounds`를 추가했습니다. Grid 생성이 이 경계에서 덮어야 할 Tile 좌표 구간을
  직접 산출합니다. flat-top layout의 중심 x가 q에만 의존하는 성질을 이용해 열을 먼저 확정하고
  각 열의 행 구간을 닫힌 형태로 계산한 뒤, 완전히 포함되지 않는 외곽 후보를 제거합니다.
- **(Breaking)** `HexTileData.CalculateIndex`와 `HexTile.CalculateIndex`를 제거했습니다. Hex ring
  순회를 전제한 인덱스는 표면 전체를 덮는 Grid에서 의미가 없습니다. `Index`는 이제 **Bake 순서로
  부여한 0 기반 값**이며, Controller의 Tile 목록 순서이자 생성 Geometry의 Tile index와 같으므로
  렌더 Backend에 넘기는 시각 상태 배열의 첨자로 그대로 쓸 수 있습니다. 영구 식별에는 계속
  `Coordinates`를 사용합니다.
- `Basic Usage` Sample이 `OnEnterTile`만 구독하던 것을 `OnExitTile`/`OnMouseDownTile`/
  `OnMouseUpTile`까지 확장해 네 가지 Tile 상호작용 계약을 Console에서 모두 확인할 수 있게
  했습니다. Sample README에 동작별 기대 로그 표와, `HexTileSelectionState`가 **새 Tile의 Enter를
  이전 Tile의 Exit보다 먼저** 발행한다는 순서 계약을 명시했습니다.
- `ProcessPointer` 통합 테스트의 카메라 배치 결함을 수정했습니다. 테스트 평면 Mesh의 winding 법선은
  `+z`인데 카메라가 `-z`쪽에서 진입해 `Physics.queriesHitBackfaces` 기본값(false) 아래에서
  `Collider.Raycast`가 항상 실패했고, 앞면에서 진입하더라도 화면 중앙 ray가 Grid seed가 아닌 원점을
  겨냥해 Grid 범위 밖 좌표를 반환했습니다. 카메라를 seed 앞면으로 옮겨 enter/down/up/exit 이벤트
  순서를 실제로 검증합니다. Unity 6000.5.7f1 EditMode CLI 결과는 119/119입니다.
- **(Breaking)** Projector/Shader 패키지 의존, Effect, Projection Shader와 Buffer Uploader를 완전히
  제거하고 Triangle topology 기반 intrinsic Surface Grid로 교체했습니다.
- compact adjacency, topology 진단, Triangle Unfolding local Patch, convex clipping, barycentric binding,
  실제 길이 기반 Hex layout과 Surface picking을 추가했습니다.
- Non-manifold Edge와 degenerate Triangle을 traversal 경계로 차단해 잘못된 Face 연결과 자기 인접을
  방지했습니다. NaN/Infinity position, concave polygon과 zero-length clip Edge는 입력 경계에서 거부합니다.
- Patch intrinsic radius를 임의의 seed Triangle A corner가 아니라 실제 Surface seed에서 측정합니다.
- 불변 snapshot의 `IReadOnlyList`가 내부 배열로 다시 cast될 수 있던 경로를 read-only view로
  봉인해 topology, Patch, Region, Grid와 Geometry를 외부에서 변경할 수 없게 했습니다.
- Hex origin/query 좌표의 NaN/Infinity와 Geometry 변환 행렬의 비유한 값·비가역 변환을 입력
  경계에서 거부합니다. Surface snapshot 혼용과 음수·비유한 offset도 즉시 오류로 처리합니다.
- Picking은 전역 Physics hit가 아니라 구성된 원본 Collider를 직접 raycast하므로 앞쪽의 무관한
  Collider가 Surface 선택을 가로막지 않습니다.
- `SurfaceHandle`을 64-bit identity로 확장하고 Static Mesh Adapter가 `EntityId.ToULong()`을
  사용하게 해 서로 다른 Mesh가 32-bit hash 충돌로 같은 Surface로 취급될 가능성을 제거했습니다.
- Unity 6000.5.7f1 EditMode Test Runner에서 구조 전환 이후 전체 117/117 테스트가 통과했습니다.
- Basic Usage의 seed를 Unity Plane 모서리 Triangle 0에서 중앙 Triangle 100으로 옮기고 Tile Radius를
  0.38로 조정해 전체 127개 Grid가 중앙에 표시되도록 수정했습니다.
- `ClearTiles()`가 Store의 빈 visual 이벤트를 기존 Geometry Backend에 먼저 적용해
  `Visual list does not cover every geometry tile index` 예외를 내던 순서 결함을 수정했습니다.
  Unity 6000.5.7f1 EditMode Test Runner 최종 결과는 회귀 테스트를 포함해 118/118입니다.
- `ISurfaceGridRenderBackend`와 공통 Mesh API 기반 `MeshSurfaceGridRenderBackend`를 추가했습니다.
- **(Breaking)** `HexTileData`를 도입해 좌표·속성·색상·활성 상태를 UnityEvent 상호작용 façade인
  `HexTile`에서 분리했습니다. 기존 직렬화 타일은 다시 Bake해야 합니다.
- Output MeshFilter/MeshRenderer를 둘 다 비우면 렌더링 Backend 없이 논리 타일·피킹·상태만
  Bake할 수 있습니다. 하나만 지정한 구성은 오류로 처리합니다.
- `HexGridController`는 readable source MeshFilter, 동일 Mesh의 MeshCollider와 별도 output
  MeshFilter/MeshRenderer를 사용합니다.
- Unity 직렬화가 `tiles` List 인스턴스를 교체해도 Store가 이전 List를 계속 사용하지 않도록 서비스
  참조 변경 감지와 대칭 해제를 추가했습니다.

- **(Breaking)** 공개 명칭을 역할에 맞게 통일했습니다. `HexGridController` → `HexGridController`,
  `HexGrid`/`IHexGrid` → `HexTile`/`IHexTile`, `CalculateTile` → `BakeTiles`,
  `ClearTile` → `ClearTiles`, `HexagonRadius` → `TileRadius`, `TileLimit` → `GridRadius`,
  `LayerMask` → `InteractionLayerMask`입니다. 0.x 호환 별칭은 제공하지 않습니다.
- `Bake → Clear → Bake`에서 Inspector가 서비스의 조회·선택·GPU 상태를 우회해 직렬화 List만
  비우던 예외를 수정했습니다. 서비스는 Controller 수명 동안 유지하고 Store의 `Clear()`가 모든
  관련 상태를 일관되게 비웁니다.
- 타일은 비어 있으면 런타임에서 자동 생성되고 설정 변경 시 자동 재계산됩니다. Inspector 작업은
  `Rebuild Tiles`와 확인 대화상자가 있는 보조 `Clear Baked Tiles`로 단순화했으며, 렌더링과 무관한
  `Create Object`/`RootObject` 흐름을 제거했습니다.
- Inspector의 private List reflection 접근과 직렬화 콜백의 GPU 버퍼 갱신을 제거했습니다.
  ComputeBuffer를 재사용하고 Enable/Disable 시 명시적으로 복구·해제해 Inspector 상호작용 뒤
  데칼 밝기가 불안정해질 수 있던 수명 경로를 정리했습니다.
- Scene View에서 모든 타일의 긴 Gizmo 라벨을 상시 표시하지 않습니다. 선택한 타일의 좌표만
  표시하고 상세 속성은 Overlay에 표시하며, Overlay 콘텐츠 투명도 슬라이더를 추가했습니다.
- **(Breaking)** `HexGridController`의 `TileRadius`/`GridRadius`/`InteractionLayerMask`가 자체 직렬화 필드에서
  새 `HexGridSettings : ScriptableObject`(`Runtime/HexGridSettings.cs`) 참조로 이동했습니다.
  마이그레이션: `HexGridSettings` asset을 생성해 기존 세 값을 옮기고 `HexGridController.settings`에
  할당하세요.
- `HexGridController`의 입력·Raycast·타일 데이터·선택 상태·GPU 갱신 책임을 `Runtime/Services/`의 5개
  독립 서비스(`HexGridPointerInput`/`HexTilePicker`/`HexTileStore`/
  `HexTileSelectionState`)로 분리했습니다(P1-01). Projector 전용 `HexTileBufferUploader`는 이후
  intrinsic Backend 전환에서 제거했습니다.
- `OnEnterTile`/`OnExitTile`/`OnMouseDownTile`/`OnMouseUpTile`이 매 이벤트마다 리스너를 2회씩
  호출하던 버그를 수정했습니다.
- Runtime 스크립트로 `TileRadius`를 바꿔도 투영 값이 갱신되도록 설정 asset의 `SettingsChanged`
  이벤트를 Editor/Runtime 양쪽에서 처리합니다.
- 레이캐스트 타일 피킹의 큐브 좌표 라운딩에서 S 성분 보정이 자기 자신(`roundHex.z`)을 참조해
  Q+R+S=0 불변식이 깨지고 엉뚱한(때로는 존재하지 않는) 타일이 선택되던 결함을 수정했습니다.
  라운딩 로직을 `HexGridController`에서 `HexCoordinates.Round(Vector2)` 정적 메서드로 추출해 순수
  단위 테스트로 검증 가능하게 했습니다.
- `HexTile.CalculateIndex`(축 좌표별 인덱스 매핑)가 경계값·음수 좌표를 포함해 연속적이고 중복 없는
  인덱스를 만드는지 확인하는 회귀 테스트를 추가했습니다(수학적으로는 기존부터 올바른 구현이었고,
  테스트 공백만 채웠습니다).
- `HexGridController`의 필수 Settings/Projector 참조 누락을 검사해 NRE 대신 명확한 오류를 표시하고,
  Inspector에서 잘못된 상태의 타일 계산을 비활성화합니다.
- 구형 URP Decal Shader Graph, Auto Fix 및 `TM`/오타 자산을 제거하고 Lit 확장 Shader Graph는
  `Jeomseon.Unity.Shaders`로 이동했습니다.

## [0.2.0] - 2026-08-13

- **(Breaking)** 개명 전 `Jeomseon.HexGrid` Runtime/Editor 네임스페이스를 패키지 규칙에 맞춰
  `Jeomseon.Unity.GridTileSystem`과 `Jeomseon.Unity.GridTileSystem.Editor`로 변경했습니다. 이전
  네임스페이스 호환 별칭은 제공하지 않습니다.

## [0.1.4] - 2026-08-11

- 워크스페이스 명명 규칙에 맞춰 `HexGridController`·`HexGrid`의 `[SerializeField] private` 필드를
  `_camelCase`에서 `camelCase`로 정리하고(`[FormerlySerializedAs]`로 기존 이름 보존),
  `HexGridController`의 `SCREAMING_SNAKE_CASE` 상수를 `PascalCase`로 정리했습니다.
  `GridManagerInspector`의 reflection·`FindProperty` 문자열도 함께 갱신했습니다. 공개 API 변경은
  없으며 기존 Scene·Prefab의 직렬화된 값은 그대로 유지됩니다. (Scene Sample 자산 추가는 이
  패키지의 안정화 착수 시점으로 보류합니다 — `handoffs/current.md` 참고.)

## [0.1.3] - 2026-08-10

- Runtime, Editor, Tests, Samples 어셈블리의 `rootNamespace`를 실제 공개 namespace와 일치시켰습니다.
- `Hex` 중간 폴더를 제거하여 파일 위치와 `Jeomseon.HexGrid` namespace가 일치하도록 정리했습니다.
- 샘플 코드를 독립적인 Sample asmdef로 분리했습니다.
- **(Breaking)** Scene View 타일 옵션을 EditorToolkit의 범용 IMGUI 창 `SceneViewInnerWindow`
  (`0.5.0`에서 제거됨)에서 Unity `Overlay` API 기반 `HexTileOptionOverlay`로 이전했습니다.

## [0.1.2] - 2026-08-05

- Unity 6000.5.7f1을 최소 지원 버전으로 상향했습니다.

## [0.1.1] - 2026-07-29

- Runtime·Editor·Tests·Samples 어셈블리의 `rootNamespace`와 소스 파일 위치를 namespace에 맞게 정리했습니다.

## [0.1.0] - 2026-07-29

### 추가

- 육각형 축 좌표 및 큐브 좌표 자료형
- 타일 상태와 상호작용 이벤트
- URP 데칼 기반 그리드 표시
- Scene View 타일 편집 도구
- 기본 사용 예제와 런타임 테스트

### 변경

- 패키지 이름을 `com.jeomseon.unity.grid-tile-system`으로 통일
- 표준 UPM 폴더 구조와 이름 기반 어셈블리 참조 적용
- 레거시 통합 저장소 의존성을 분리된 Jeomseon 패키지 의존성으로 교체
- 외부 SerializedDictionary 의존성을 직렬화 목록과 조회 캐시로 교체
- 레거시 마우스 컴포넌트 의존성을 Unity Input System 입력으로 교체
- 비공개 메서드 이름을 C# 명명 규칙에 맞게 정리

### 수정

- `HexCoordinates`에서 `Vector3` 변환 시 S 좌표 대신 R 좌표가 중복되던 문제
