# 변경 기록

이 문서는 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 형식을 따릅니다.

## [Unreleased]

## [0.3.0] - 2026-08-25

- **실제 Unity 사용자 검증에서 발견된 결함 수정** (`SurfaceRegionBuilder`/`HexGridSettings`):
  - Sutherland-Hodgman clip 단계가 clip 경계 위에 정확히 있는 subject vertex를 만나면 intersection
    계산 결과와 그 vertex 자체를 중복으로 추가하던 결함을 고쳤습니다(`AppendDistinct`). 중복 점은
    zero-length Edge로 남아 Tile 외곽선 Edge count가 뒤섞이고(공유 Edge가 1도 2도 아닌 3~4로
    집계), Scene View Gizmo에 실제 Hex 변이 아닌 대각선이 그려지는 원인이었습니다.
  - `SurfaceRegionCanonicalizer`/`SurfaceRegionBuilder`의 quantize 허용 오차를 `1e-6`에서 `1e-4`로
    올렸습니다. 좌표 크기 ~10~20 범위에서 서로 다른 clip 경로가 만드는 float32 반올림 오차가
    실측 ~1e-6이라 예전 값은 오차 자체와 크기가 같아 같은 물리적 교점을 다른 점으로 오인했습니다.
    Canonicalizer 조회도 자기 quantize 칸 하나가 아니라 인접 3x3 칸을 모두 확인하도록 넓혀 격자
    경계에서 놓치는 경우를 없앴습니다. 여러 반경(0.15~4)과 Surface 경계를 가로지르는 Tile
    시나리오에서 모든 Tile의 외곽선이 닫힌 단순 loop(꼭짓점 degree 전부 2)를 이루는지 헤드리스로
    재검증했습니다.
  - `HexGridSettings`의 모든 setter가 Edit Mode에서는 무조건 `SettingsChanged`를 다음 Editor tick으로
    미루던 결함을 고쳤습니다. 원래 의도는 "Unity의 OnValidate 호출 스택 안에서 DestroyImmediate 금지"
    제약을 피하는 것이었는데, 실제로는 `OnValidate`가 아닌 일반 코드(테스트 포함)에서 부른 setter까지
    전부 지연시켜 동기 알림을 기대하는 코드가 즉시 반영을 못 받았습니다. 지연은 `OnValidate` 콜백
    경로에만 적용하고, 그 밖의 모든 setter 호출은 항상 즉시 알립니다.
  - Terrain Usage Sample Scene의 출력 `MeshRenderer`에 Material이 전혀 할당돼 있지 않아
    (`m_Materials: [{fileID: 0}]`) Play Mode에서 Tile Mesh가 Unity 기본 missing-material 마젠타로
    렌더링되던 결함을 고쳤습니다. Basic Usage Sample과 동일한 built-in `Sprites/Default` Material
    참조로 채웠습니다.
- `TerrainSurfaceTopology`가 모든 heightmap cell을 항상 `v01`-`v10` 대각선으로만 삼각분할하던
  결함을 고쳤습니다. Unity Editor에서 합성 non-planar cell에 대해 `TerrainCollider.Raycast`를
  직접 실측한 결과 Unity는 항상 `v00`-`v11` 대각선을 쓴다는 것을 확인했고, `GetTriangle`/
  `TryGetSurfacePoint`/`GetAdjacency`를 그에 맞춰 교정했습니다. 이 불일치는 굴곡이 강한 heightfield
  영역에서 Tile Mesh가 실제 렌더/충돌 Terrain 표면과 다른 위치에 놓이게 해 depth-bias 값을 아무리
  올려도 해결되지 않던 z-fighting/floating의 원인 중 하나였습니다. 남은 원인(barycentric 계산이
  질의점 Y를 실제 표면 높이 대신 0으로 사용)은 `ROADMAP.md`에 다음 작업으로 남겨뒀습니다.
- Tile마다 None(숨김)/Fill(채움)/Outline(윤곽선)/Both 표현을 독립적으로 지정하는 기반을 추가했습니다.
  `MeshSurfaceGridRenderBackend`는 Tile별 Fill/Outline index를 별도 submesh로 분리해 한 Mesh 안에서
  서로 다른 Tile을 섞어 그립니다(`ApplyDrawModePartition`). GPU structured-buffer Backend는 Tile별
  혼합을 지원하지 않고 `SetDrawMode`로 지정하는 grid 전체 단일 모드만 지원합니다(대규모 Terrain
  대상이라 Tile당 GPU 분기 비용이 부담스러워 범위를 좁혔습니다). override 유무를 신뢰할 수 없던
  일반 `[Serializable]` class는 `IHexTileDrawPolicy`와 `[SerializeReference]` 기반
  `NoneDrawPolicy`/`FillDrawPolicy`/`OutlineDrawPolicy`/`BothDrawPolicy`로 교체했습니다. Settings 기본값과 Tile별
  override 모두 `SerializeReferenceSelector`에서 타입을 선택하거나 실제 `null`로 지울 수 있습니다.
- Scene View 편집 선택은 `IsActive == false`인 Tile도 포함하도록 런타임 입력 picking과
  분리했습니다. 비활성 Tile은 게임 입력은 계속 무시하지만 Editor에서 선택·재활성화할 수 있습니다.
- Backend에 전달되지 않아 Edit/Play 어느 쪽에서도 효과가 없던
  `OutlineDrawPolicy.Thickness`/`Padding` placeholder를 제거했습니다.
- `HexGridSettings.OnValidate` 지연 알림이 이미 예약된 tick에 public setter가 추가로
  변경되어도 즉시 Mesh를 교체하지 않고 하나의 delay call로 합치도록 고쳤습니다.
- 위 회귀 테스트가 Settings가 아닌 Controller의 `OnValidate`만 호출해 실제
  ScriptableObject Inspector 경로를 재현하지 못하던 구성을 바로잡았습니다.
- Player Mouse 통합 테스트가 native Mouse와 가상 Mouse를 동시에 등록해 `Mouse.current`가
  바뀐 장치의 상태를 읽을 수 있던 비결정적 구성을 고쳤습니다. 테스트 동안 기존 Mouse를
  격리하고 각 state update 후 가상 Mouse를 current로 확정합니다.
- Terrain heightfield의 비영 곡률 loop를 shared-edge unfolding 또는 seed 접평면에 투영해 chart가
  접히고 긴 Triangle·구멍이 생기던 문제를 고쳤습니다. Terrain은 local XZ heightfield chart를
  사용하고 실제 높이는 barycentric binding으로 복원합니다.
- 같은 intrinsic 경계점의 offset 적용 위치를 공유해 Face 법선 차이로 fragment 사이가 벌어지지
  않게 했고, Outline 내부 경계 제거 허용오차를 canonicalization 기준과 일치시켰습니다.
- Renderer에 사용자 Material이 없을 때 패키지 내장 vertex-color/depth-bias Material을 자동으로
  생성하는 기본 경로를 추가했습니다. Geometry는 `Surface Offset = 0`으로 표면에 정합한 채 Terrain
  LOD와의 깊이 충돌만 렌더 단계에서 보정하며, 사용자 Material과 Backend 비소유 Material은 유지합니다.
- Region canonicalization은 공유 intrinsic 표시 좌표에만 적용하고, Face별 SurfacePoint의
  barycentric binding은 clipping 원본 점으로 계산하도록 분리했습니다.
- `Clear Baked Tiles`가 직렬화 배열을 비운 즉시 ListView가 이전 index로 다시 bind해
  범위 초과 예외를 내던 문제를 unbind/rebind과 bind index 방어로 고쳤습니다.
- Edit Mode의 빈 Tile 목록은 명시적 Clear 상태로 유지합니다. 직렬화 Tile이
  있을 때만 Preview Geometry를 복구하고, Play Mode는 Tile이 비었으면 자동 Bake합니다.
- Terrain Sample heightmap을 두 개의 가파른 능선, 중앙 골짜기와 보조 ridge가 있는
  굴곡으로 갱신했습니다. Seed 높이를 새 지형에 맞춰 조정하고 149개 Tile Bake 상태를
  Scene에 직렬화해 Sample을 열자마자 Edit Mode 정합 결과를 확인할 수 있게 했습니다.
- Editor Inspector에 Bake된 Tile 목록을 보여주는 virtualization 지원 `ListView`를 추가했습니다.
  기존 IMGUI 배열 그리기는 보이지 않는 Tile까지 컨트롤을 만들어 Tile이 많아지면 Inspector가
  느려졌습니다. Scene View 클릭과 목록 선택이 같은 Tile 편집 Overlay 경로를 공유합니다. Edit Mode에서
  비직렬화 Preview Mesh/Backend가 유실되면 Inspector가 복구하고, List/Overlay의 SerializedProperty
  편집이 public setter를 우회해도 Color·Active·DrawPolicy를 즉시 실제 Mesh에 다시 적용합니다.
- Scene View에 Edit Mode에서도 항상 모든 Tile의 외곽 Hex 윤곽을 그리는 Gizmo를 추가했습니다.
  마지막 Bake Geometry를 재사용하므로 별도 계산이 없고, 실제 출력 Mesh가 Fill로 그리고 있어도
  경계를 확인할 수 있습니다.
- `HexGridController`에서 재사용 가능한 Surface 탐색·Patch 성장 정책을 제거하고
  `HexGridSettings`로 이동했습니다. Controller는 Camera, seed Transform/offset/direction, 출력
  Renderer와 surface offset처럼 Scene/Prefab 배치에 속하는 값만 직렬화합니다.
- Editor에서 `HexGridSettings.OnValidate`가 발생한 같은 콜백 안에서 runtime Mesh를
  `DestroyImmediate`하던 예외를 수정했습니다. 편집 모드 설정 변경은 다음 Editor tick의 Bake 하나로
  병합되고, Controller가 비활성화·파괴되면 예약도 취소됩니다.
- Parameterizer가 함께 산출하는 폐합 오차·성장 제한·graph 거리·metric distortion을
  `SurfacePatchDiagnostics`로 묶었습니다. 실행 의존성까지 DTO로 감싸지 않고 반복 전달되는 진단값만
  명확한 결과 컨테이너로 정리했습니다.
- `IHexTilePicker.TryPick`의 중복 성공 상태와 무명 `(bool, RaycastHit)` 튜플을 제거했습니다.
  성공 시 `RaycastHit`과 활성 `HexTile`을 함께 제공하는 `HexTilePickResult`를 반환합니다.
- `HexGridPointerInput`이 Input System의 순간 `wasPressedThisFrame`/`wasReleasedThisFrame` 상태에
  의존하지 않고 이전·현재 버튼 상태를 직접 비교합니다. Input update 순서에 따라 Down/Up이 사라지는
  문제를 막았으며 실제 PlayMode Mouse 상태 주입으로 Enter/Down/Up을 검증합니다.
- Basic Sample의 이벤트 구독을 `Start`에서 `OnEnable`로 앞당겨 Controller의 첫 Update가 발생시키는
  Enter 이벤트도 놓치지 않습니다. 런타임 생성·도메인 전환에서도 UnityEvent 필드가 null이면 안전하게
  초기화합니다. Basic Sample 컴포넌트는 입력 이벤트 로그만 담당하며 Material과 Tile 상태를 변경하지
  않습니다. Terrain Sample의 연출 전용 컴포넌트는 제거했습니다. 두 Scene은 built-in
  `Sprites/Default` Material을 직렬화하고 Edit Mode와 Play Mode에서 동일한 실제
  `MeshSurfaceGridRenderBackend` 결과를 표시합니다.
- Basic Sample의 이벤트 미발생 원인은 Grid picking이 아니라 프로젝트 Input System의 Editor 입력
  라우팅이었습니다. 활성 Input Settings asset 없이 기본 `PointersAndKeyboardsRespectGameViewFocus`를
  사용하면 Game View가 입력 포커스를 받지 못한 동안 `Mouse.current.position`이 `(0,0)`으로 유지됩니다.
  패키지는 IMGUI 우회 없이 Input System 경로만 유지하며, 검증 프로젝트가 명시적 Input Settings asset과
  `AllDeviceInputAlwaysGoesToGameView`를 사용하도록 구성했습니다.
- Patch corner convex hull과 겹치지 않는 Hex 후보를 Region clipping 전에 제거하고
  `CandidateTileCount`/`RegionBuildCount`로 비용을 계측합니다.
- Grid build 범위의 canonical vertex cache로 인접 Tile이 공유하는 intrinsic 교점을 동일 좌표로 통일합니다.
- `SurfacePatchTriangle.GraphGeodesicDistance`와 Patch 최대 graph 거리·최대/평균 edge metric distortion
  진단을 추가했습니다. Patch 반경은 chart 직선거리 대신 adjacency 경로의 누적 intrinsic 거리 상한을
  사용합니다.
- `ISurfaceParameterizer`와 `SurfacePatchSet`을 추가하고 `SurfaceGridSystem`에 Parameterizer 주입 경계를
  연결했습니다. Triangle Unfolding과 향후 ExpMap/자동 분할이 같은 출력 계약을 사용합니다.
- 성장 frontier의 실제 펼침 좌표를 공유하는 Face 중복 없는 자동 다중 Patch와 전체 Patch Region 병합을
  추가했습니다. `DistortionAdaptiveSurfaceParameterizer`는 metric 임계값을 넘으면 Patch 크기를 줄입니다.
- `SurfaceGrid.Surfaces`/`SpansMultipleSurfaces`를 전체 Patch 기준으로 계산하고 Controller picker,
  Skinned binding, 단일/multi-surface Geometry 및 Chunk 소비자가 같은 범위를 사용합니다.
- `SurfaceGridSnapshot`/`SurfaceGridDelta`, dirty `SurfaceGridChunk`, Chunk Geometry 재생성 계약을 추가했습니다.
- Burst/Jobs barycentric 평가 커널과 GPU structured-buffer indexed-indirect Backend를 추가했습니다.
- seed tangent 1차 ExpMap 비교 기준과 `SurfaceParameterizationComparison`을 추가했습니다.
- Basic/Terrain Play Mode와 Controller Inspector 캡처를 한·영 README에 추가했습니다. Terrain Sample은
  렌더 LOD와의 z-fighting을 피하도록 출력 offset을 0.03으로 조정했습니다.

- **(Breaking)** `HexGridReceiver`/`SurfaceReceiverKind`를 삭제했습니다. `HexGridController`가
  seed 위치·초기 방향·탐색 옵션을 직접 직렬화하고 `SurfaceGridSystem`으로 Grid를 만듭니다. 사용자는
  더 이상 Surface 컴포넌트를 등록하거나 seed Triangle 번호를 입력하지 않습니다.
- `HexGridController`가 단일 `SurfaceGrid`(경계 연결로 여러 Surface에 걸칠 수 있음)를 소유합니다.
  Grid가 덮는 각 Surface의 Collider마다 picker를 만들어 Ray에서 가장 가까운 활성 Tile을 고릅니다.
- Skinned Surface 변형 추종은 Grid가 **단일** Skinned Surface 위에만 있을 때 적용됩니다. 여러
  Surface에 걸친 Grid는 Surface마다 변형 규칙이 달라 하나의 binding으로 표현할 수 없으므로 아직
  변형을 따르지 않습니다(bind pose로 남음).
- `SurfaceGridGeometryBuilder.Build(ISurfaceProvider, ISurfaceTransformSource, SurfaceGrid, ...)`와
  `HexTileStore.Bake(ISurfaceProvider, ISurfaceTransformSource, SurfaceGrid)`를 추가해 여러 Surface에
  걸친 Grid의 Geometry와 Tile 중심을 Surface별 변환으로 올바르게 만듭니다.
- `ISurfaceTransformSource`를 추가했습니다. `GeometrySurfaceQuery`가 이를 구현해 handle에서
  local-to-world 변환을 조회합니다.
- Basic/Terrain Sample Scene을 새 seed/output 필드로 마이그레이션했습니다. Unity 6000.5.7f1에서
  각각 126 Tile/2842 vertex, 12 Tile/952 vertex를 실제 Bake했습니다.
- `SurfaceGridSystem.Clear()`가 연결 결과와 boundary 색인을 Adapter/topology와 함께 비워 Scene 변경 뒤
  이전 Surface 연결이 재사용되지 않게 했습니다. N7의 3-Surface chain 및 제한 성장 회귀를 포함한
  EditMode 테스트 164/164가 통과했습니다.

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
