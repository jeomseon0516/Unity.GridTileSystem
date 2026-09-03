# Grid Tile System 로드맵

## 현재 방향 — intrinsic Surface Grid

`GridTileSystem`은 Projector를 사용하지 않습니다. Triangle topology를 local 2D chart로 펼치고,
실제 길이로 정의한 Hex를 source Triangle과 교차한 뒤 barycentric binding으로 표면에 정합합니다.
Projector는 별도의 실험적 패키지이며 이 패키지와 Adapter도 공유하지 않습니다.

Grid 결과 지오메트리는 표준 `Mesh`라 렌더 파이프라인에 의존하지 않지만, 2026-09-02 워크스페이스
기준이 Unity `6000.6` + URP `17.6`으로 바뀌면서 **번들 fallback 셰이더/머티리얼은 URP를
대상으로 합니다**(`com.unity.render-pipelines.universal` 의존성 추가). `StructuredBuffer` 백엔드에
사용자가 넘기는 Material도 URP LightMode pass가 있어야 렌더됩니다.

설계와 수학의 기준 문서는 하네스의 다음 문서입니다.

- `architecture/intrinsic-surface-grid.md`
- `architecture/intrinsic-surface-grid-flow.md`
- `architecture/intrinsic-surface-grid-study-guide.md`
- `architecture/grid-tile-system-rendering-backends.md`

## Terrain 정합 렌더링 — Depth Bias 방식 폐기 결정 (2026-08-25)

굴곡진 Terrain 위에 Tile Mesh를 z-fighting 없이 밀착시키는 문제에서 두 접근을 시도했고 **둘 다
일반해로 채택하지 않기로 결정**했습니다. 다음 세션은 아래 조사 방향부터 이어갑니다.

- **쉐이더 Depth Bias(`Offset factor, units`) 방식은 폐기합니다.**
  `Runtime/Resources/SurfaceGridDepthBiased.shader`의 `Offset [_DepthBiasFactor], [_DepthBiasUnits]`는
  지오메트리를 옮기지 않고 래스터라이즈 시점 깊이값만 미는 GPU 트릭입니다. `Factor` 항은 카메라
  시점에서 폴리곤의 화면상 기울기(screen-space depth slope)에 비례해 커지므로 카메라 각도와 지형
  굴곡이 바뀔 때마다 필요한 크기가 달라지고, `Units` 항은 깊이버퍼 정밀도·near/far 평면·렌더
  파이프라인에 따라 실제 월드 단위 의미가 달라집니다(Unity 자체도 플랫폼별 Offset 해석 차이를
  문서화). 실측 결과 기본값 -1로는 타일이 지면 판정을 이기지 못했고, 사용자가 직접 -84까지 내려야
  안정적으로 보였습니다. 이는 일반해가 아니라 지금 씬·카메라 구성의 최악 케이스를 우연히 넘긴
  값이며, 외부 소비자가 있는 공개 패키지가 씬마다 재튜닝해야 하는 매직 넘버에 의존하는 것은 채택
  기준에 맞지 않습니다.
- **`surfaceOffset`(법선 방향 vertex lift) 단독 방식도 불충분함을 확인했습니다.** 굴곡이 강한 Terrain
  Sample에서 `0.03`만 적용해도 타일이 지면 위에 눈에 띄게 높이 떠 보이는 현상이 실측 확인됐습니다.
  `SurfaceGridGeometryBuilder`의 offset 적용 자체(`CalculateFaceNormal` 기반 평탄 face normal ×
  `surfaceOffset`)에는 결함이 없어 보이므로, 이는 "offset 크기가 작아서"가 아니라 offset 이전에
  이미 Tile Mesh 위치와 실제 화면에 보이는 Terrain 표면 사이에 더 큰 위치 불일치가 있다는 신호로
  해석합니다.
- **대각선 불일치 가설, 실측으로 확정하고 수정 완료 (2026-08-25).**
  `TerrainSurfaceTopology.GetTriangle`(`Runtime/Surface/Adapters/TerrainSurfaceTopology.cs`)이 모든
  heightmap cell을 항상 `v01`-`v10` 대각선으로만 분할하고 있었는데, Unity Editor에서 합성
  non-planar cell에 대해 `TerrainCollider.Raycast`를 직접 실측한 결과 Unity는 항상 `v00`-`v11`
  대각선(main diagonal)을 사용한다는 것을 확정했습니다(단일 cell 테스트, 서로 정점을 공유하지 않는
  14개 cell 교차 검증, main-diagonal 합/anti-diagonal 합의 대소를 일부러 뒤집은 판별 테스트까지
  세 종류로 재확인 — Unity는 높이 구성과 무관하게 **항상 고정된 main diagonal**을 씁니다). `GetTriangle`/
  `TryGetSurfacePoint`/`GetAdjacency`를 Unity 규약에 맞춰 `v00`-`v11` 분할로 교정했고, 인접 Triangle
  매핑(Edge0/1/2가 가리키는 이웃 cell·local triangle)도 새 대각선에 맞게 다시 유도해 반영했습니다.
  EditMode 전체 344/344 통과(회귀 없음).
- **하지만 이 수정만으로 z-fighting/floating이 완전히 사라지지는 않았습니다.** 실제 Terrain Sample
  heightmap(두 능선+골짜기, 33×33, size=(16,3,16))에서 `TerrainSurfaceTopology.Evaluate()`가 계산한
  Y와 같은 (x,z)에서의 실제 `TerrainCollider.Raycast` Y를 22,671개 지점에서 광범위 비교한 결과 대각선
  수정 이후에도 평균 오차 0.0197, 최대 오차 0.32(전체 높이 범위 3 대비 약 10%)가 남아 있었습니다.
  가장 오차가 큰 지점을 직접 파고든 결과 **두 번째 원인**을 발견했습니다:
  `TerrainSurfaceTopology.TryGetSurfacePoint`가 `CalculateBarycentric(a, b, c, localPosition)`을
  호출할 때 `localPosition`의 Y를 실제 표면 높이가 아니라 **0으로 넘깁니다**. 이 barycentric 계산은
  3D dot product 기반이라 질의점이 실제 삼각형 평면에서 수직으로 얼마나 떨어져 있는지에 따라
  결과가 왜곡되며, 경사가 급한 삼각형일수록(및 질의점 Y가 실제 표면 Y와 멀수록) 오차가 커집니다 —
  "굴곡이 강한 곳일수록 심함"이라는 지금까지의 모든 관찰과 다시 일치합니다. **이 부분은 아직
  수정하지 않았습니다.** 다음 세션(또는 아래 Z-Fighting 재설계 착수 시점)에서 `TryGetSurfacePoint`가
  질의점의 실제 표면 Y를 먼저 추정하거나, Y에 무관한 barycentric 산출 방식(예: XZ 평면 투영 기반
  2D barycentric)으로 바꾸는 수정이 필요합니다.
- 위 두 원인 모두 이제 `ROADMAP.md`에 기록돼 있으므로, 이 절의 원래 결론(쉐이더 Depth Bias와
  `surfaceOffset` 단독 모두 일반해로 채택하지 않음)은 유지하되, **z-fighting 자체를 근본적으로 줄이는
  것은 Tile Mesh를 실제 렌더 표면과 최대한 정확히 일치시키는 것**이며 위 두 기하학적 결함이 그
  일치를 방해하고 있었다는 것이 이번 조사의 핵심 결론입니다.

## Z-Fighting 해결 재설계 — 사용자 설계안 (추후 작업으로 보류, 2026-08-25)

사용자가 위 조사 결과를 바탕으로 Depth Conflict 해결 구조 전체의 재설계 방향을 제시했습니다.
**지금은 구현하지 않고 다음 GridTileSystem 세션을 위해 이 설계안만 기록**합니다(다른 패키지 작업이
우선). `Unity Asset Store`의 `Terrain Grid System 2(TGS2)`가 유사 문제를 Mesh Depth
Offset/Cell·Territory Surface Depth Offset/Normal Offset/Camera Offset/Elevation/Stencil Buffer 등
여러 보정 수단을 조합해 제공한다는 점을 참고하되, **TGS2 구현을 그대로 복제하지 않고** GridTileSystem
고유 아키텍처에 맞게 재설계하는 것이 목표입니다.

### 1. 책임 분리 — 메시 정합 알고리즘 자체는 왜곡하지 않음

```text
Surface Sampling
    ↓
Surface-Conforming Geometry 생성
    ↓
Tile Rendering
    ↓
Depth Conflict Resolution
```

Z-Fighting 해결은 `Rendering / Depth Conflict Resolution` 계층만의 책임으로 두고, Tile 생성
알고리즘(Surface Sampling·Geometry 생성)은 Depth Conflict를 피하기 위해 왜곡하지 않습니다. 이번
세션에 고친 대각선 불일치·barycentric Y 오차 같은 **진짜 기하 결함은 이 계층 분리와 무관하게
먼저 바로잡아야** 하며, Depth Conflict Resolution 계층은 "완전히 정합된 Mesh에도 남는 부동소수점
수준의 coincident-surface z-fighting"을 다루는 최종 안전장치로 위치시킵니다.

### 2. Depth Bias를 완전히 폐기하지 않고 1차 해결책으로 재도입

이번 세션에서 폐기한 것은 "환경마다 재튜닝해야 하는 매직 넘버"였지, Depth Bias 기법 자체가 아닙니다.
Constant Depth Bias + Slope-Scaled Depth Bias 조합을 기본 1차 해결책으로 재도입하되, Built-in/URP/HDRP
전반에서 공통 구현 가능한 범위(ShaderLab `Offset`/Material·RenderState 수준)를 우선 검토하고, 특정
Render Pipeline의 Decal 기능에는 의존하지 않습니다.

### 3. Normal Offset을 Geometry Fallback으로 제공

Depth Bias만으로 부족한 환경을 위한 기하학적 대안입니다. World Y 방향이 아니라 **각 vertex가 대응하는
실제 Surface normal 방향**으로 `P' = P + N * offset`을 적용합니다. 이미 Surface Sampling 단계에서
계산·조회한 normal을 재사용하고, Normal Offset만을 위해 추가 Raycast를 발생시키지 않습니다(현재
`SurfaceGridGeometryBuilder`의 `CalculateFaceNormal` 기반 구현이 이 원칙과 이미 부합합니다).

### 4. Adaptive Normal Bias

고정 Normal Offset도 급경사에서는 여전히 뜬 것처럼 보일 수 있으므로, 평탄한 곳은 offset을 거의 0에
가깝게 유지하고 문제가 생길 가능성이 높은 영역에서만 키우는 적응형 방식을 검토합니다. 평가 기준 후보:

- Surface normal 변화량
- 인접 Triangle 사이의 dihedral angle
- Surface curvature approximation
- Triangle slope
- Tile triangle과 원본 surface triangle 간의 위치 오차
- Camera view direction과 surface normal의 관계(단, 이 항목은 카메라 종속이므로 5번처럼 기본
  정책이 아닌 opt-in으로 분리)

카메라에 독립적인 값을 우선 채택합니다.

### 5. Camera Offset은 opt-in 정책

`P' = P + directionToCamera * cameraOffset` 방식은 카메라 방향에 타일을 살짝 이동시켜 시인성을
높이지만, GridTileSystem은 카메라와 독립적인 Surface/Grid 라이브러리 원칙을 유지해야 하므로 기본
정책으로 사용하지 않고 `CameraBiasMode.None`/`CameraBiasMode.ViewDirection` 같은 명시적 opt-in
구조로만 제공합니다.

### 6. API 설계 — Rendering Bias / Geometry Bias / View-dependent Bias 분리

```text
Rendering Bias      - Depth Bias, Slope Depth Bias
Geometry Bias        - Normal Offset, Adaptive Normal Offset
View-dependent Bias   - Camera Offset
```

`TileDepthConflictMode`(`None`/`DepthBias`/`DepthBiasWithNormalFallback` 등) 같은 단순 enum이나,
확장 가능성이 실제로 있을 때만 `ITileDepthConflictResolver` 같은 Strategy 인터페이스를 검토합니다.
불필요하게 추상화 계층을 늘리지 않는다는 AGENTS.md 원칙을 그대로 적용합니다.

**착수 전 필수 선행 작업**: 위 barycentric Y 오차 수정과, 대각선 수정 반영 후 Terrain/Basic 두
Sample의 실제 Unity 육안 재확인이 이 재설계보다 먼저입니다 — 근본 기하 결함이 남아 있는 상태에서
Depth Conflict Resolution 계층부터 설계하면 어느 정도가 "진짜 coincident-surface z-fighting"이고
어느 정도가 "기하 결함이 만든 위치 오차"인지 구분할 수 없습니다.

## 완료된 기반

- [x] `SurfaceHandle`/`SurfacePoint`로 Surface identity와 barycentric 위치 표현
- [x] compact Triangle adjacency, component 및 degenerate/winding/non-manifold 진단
- [x] readable Static Mesh topology Adapter
- [x] 전체 Mesh 복제 없는 Terrain virtual topology Adapter와 hole traversal 경계
- [x] Triangle unfolding과 Triangle 수/intrinsic 반경/closure tolerance 기반 Patch 성장
- [x] convex polygon clipping과 barycentric Surface Region binding
- [x] 실제 길이 기반 flat-top Hex layout, Logical Tile 생성과 intrinsic picking
- [x] Grid 후보 범위를 사용자 지정 반경이 아니라 Patch intrinsic 경계에서 자동 산출
- [x] Surface 외곽에서 잘린 Region을 제외하고 완전한 Hex Tile만 유지하는 면적 계약
- [x] Receiver 목록과 표면별 독립 topology·Patch·Grid·Tile·Backend 수명
- [x] Receiver Collider별 picking과 단일 Receiver 실패를 격리하는 부분 실패 계약
- [x] 파이프라인 비종속 `SurfaceGridGeometry` 및 `ISurfaceGridRenderBackend` 계약
- [x] 공통 Mesh API 기반 `MeshSurfaceGridRenderBackend`
- [x] `HexGridController`/Store/Selection 서비스를 intrinsic Grid 경로로 마이그레이션
- [x] `HexTileData` 순수 상태와 `HexTile` UnityEvent façade 분리
- [x] Output Backend 없는 logical-only Bake와 picking 지원
- [x] Projector/Shader/ComputeBuffer 의존 및 관련 Package asset 제거
- [x] Basic Usage Sample을 Source MeshCollider + Output MeshRenderer 구성으로 교체
- [x] TerrainData 기반 Terrain Usage Sample Scene과 Bake 검증
- [x] non-manifold traversal boundary, Surface snapshot 혼용 및 비유한 입력 방어 테스트
- [x] chart 원점 기준 격자 회전(`IntrinsicHexLayout.Rotation`)과 회전한 격자의 경계 구간 산출
- [x] Surface local 방향을 chart 방향으로 옮기는 `SurfaceChartDirection`
- [x] 월드 seed 위치 하나로 Grid를 만드는 `SurfaceGridRequest`/`SurfaceGridSystem`과 상태 기반 실패 진단
- [x] boundary Edge 위치·법선 정합으로 이어지는 Surface를 찾는 `ISurfaceConnectivity`와
      `GeometrySurfaceConnectivity`, 여러 Surface에 걸치는 chart 확장
- [x] Terrain → Bridge → Terrain 형태의 3-Surface chain과 제한된 Patch 성장 회귀 검증(N7),
      Bake 전 연결/boundary/Adapter/topology 캐시를 함께 무효화하는 수명 정책
- [x] `HexGridReceiver`/`SurfaceReceiverKind` 삭제. `HexGridController`가 seed 위치 기반
      `SurfaceGridRequest`로 직접 Grid를 만들고, Surface Collider별 picker와 여러 Surface에 걸친
      Geometry/Tile 중심 계산을 갖춤(N8)
- [x] Basic/Terrain Sample Scene을 새 seed/output 직렬화 필드로 마이그레이션하고 Unity CLI 실제 Bake 검증

## 안정화 게이트

- [x] Controller/SO 직렬화 책임 재검수: Scene 참조·배치는 Controller, Surface 탐색과 Patch 생성 정책은
      `HexGridSettings`가 소유하며 `SurfaceQueryOptions`/`SurfacePatchBuildSettings`로 계층 전달
- [x] 생성 csproj 기준 Runtime/Tests/Editor/Samples 컴파일 오류 0건
- [x] package.json과 asmdef JSON 검증
- [x] Projector/Decal/Render Pipeline 타입이 Runtime API에 남지 않았는지 정적 검색
- [x] Unity 6000.5.7f1 EditMode CLI 128/128 통과 (2026-08-21, macOS 임시 검증 프로젝트)
- [x] `Basic Usage` Sample의 Game View/Scene View 시각 확인
- [x] Play Mode 진입·종료 및 Rebuild/Clear/Rebuild 실측
- [x] `ProcessPointer` 경로의 enter/exit/down/up 이벤트 순서 자동 검증
- [x] Unity 6000.5.7f1 EditMode CLI 164/164 통과 (2026-08-24, N7/N8 및 Sample 마이그레이션 이후)
- [x] Unity 6000.5.7f1 EditMode CLI 177/177 통과 (2026-08-24, 정확도·성능 기반,
      Controller 자동 분할 및 Apple M2 Pro Metal GPU buffer 실측 포함)
- [x] `HexGridSettings.OnValidate` 설정 변경을 다음 Editor tick으로 병합해 runtime Mesh
      `DestroyImmediate` 금지 콜백 예외 제거, 회귀 포함 EditMode CLI 178/178 통과 (2026-08-24)
- [x] 실제 Input System 장치 입력에서 Game View 좌표가 전달되는지 실측 (2026-08-24, 물리 Mouse로
      Basic Usage Sample의 Enter/Exit/Down/Up Console 로그 확인, 최종 통과)
- [x] Input System 가상 Mouse의 실제 PlayMode frame 경로에서 Enter/Down/Up 1/1 통과.
      버튼 전이는 이전·현재 상태 비교로 update 순서와 무관하게 보존하고 Sample은 `OnEnable`에서 구독
- [x] Basic Sample 이벤트 미발생의 근본 원인을 프로젝트 Input System Editor 입력 라우팅으로 확인.
      활성 Input Settings asset 없이 기본 `PointersAndKeyboardsRespectGameViewFocus`를 사용해
      `Mouse.current.position == (0,0)`이 유지됐으며, IMGUI 우회는 폐기하고 검증 프로젝트에
      `AllDeviceInputAlwaysGoesToGameView` 설정을 명시
- [x] **진짜 근본 원인 정정 (2026-08-24)**: 위 Input System 라우팅 수정 후에도 물리 Mouse에서 계속
      실패해 Controller `Update()`에 임시 계측(`Mouse.current.position`, `Camera.ScreenPointToRay`,
      `Physics.Raycast` 직접 확인)을 넣어 재확인한 결과 Mouse 좌표 자체는 정상이었고, 실제 원인은
      `HexGridSettings.InteractionLayerMask`였습니다. Basic/Terrain 두 Sample 모두
      `interactionLayerMask`가 Unity 6000.5의 현재 `LayerMask` 직렬화 형식(`m_Bits` 블록)이 아니라
      구형 맨 정수(bare int) 형식으로 저장돼 있어 실행 중 값이 0(Nothing)으로 읽혔습니다. 게다가
      Sample이 특정 Layer 번호(3)에 의존했는데, 그 번호는 신규 프로젝트에서 이름이 없을 수 있어
      Inspector 드롭다운에 아예 나타나지 않는 문제도 함께 발견했습니다. 두 Sample 모두
      `InteractionLayerMask`를 `Everything`으로 바꿔 프로젝트별 Layer 설정에 전혀 의존하지 않도록
      수정하고, 블록 직렬화 형식으로 정정했습니다. TestProject에 import된 Sample 사본도 동기화했고,
      Terrain 쪽은 이 참에 누락돼 있던 최신 필드(`seedSearchRadius` 등)도 함께 동기화했습니다.
- [x] `HexGridController`의 Editor 전용 코드 분리 검토 (2026-08-24, 사용자 요청). `OnValidate` 호출
      스택 안에서 `DestroyImmediate` 금지라는 Unity 제약을 우회하던 "다음 Editor tick으로 병합" 로직이
      Controller와 `HexGridSettings` 두 곳에 나뉘어 있었습니다. Controller와 Settings는 어차피 같은
      Runtime 어셈블리라 물리적으로 다른 Editor 어셈블리로 분리할 수는 없지만, 책임을 재검토해 전부
      `HexGridSettings`(원래 `OnValidate`가 있던 곳)로 옮겼습니다. `HexGridController.cs`는 이제
      `#if UNITY_EDITOR`가 0건입니다. 사용하지 않던 `HexTile.cs`의 죽은 Editor 전용 메서드 2개도
      함께 제거했습니다. 관련 회귀 테스트(`SettingsChanged_InEditMode_DefersAndCoalescesMeshRebake`)를
      새 위치를 보도록 갱신, Runtime/Tests/PlayModeTests/Editor/Samples 전체 `dotnet build` 오류 0건.
- [x] Basic/Terrain Play Mode와 Terrain Controller Inspector 현재 이미지를 `Documentation~/Images`에
      캡처하고 한·영 README 및 Sample 검증 절차에 반영

열린 TestProject Editor에서 Asset Database를 명시적으로 새로고침한 뒤 최신 118개 테스트를
인식·통과했습니다. 생성 csproj 빌드는 계속 보조 증거로만 취급합니다.

2026-08-21 macOS에서 GUI Editor와 충돌하지 않는 임시 CLI 검증 프로젝트로 전체 119개를 재실행해
`ProcessPointer` 통합 테스트의 카메라 배치 결함을 찾아 수정했습니다. 원인은 두 가지가 겹친
것이었습니다. 테스트 평면 Mesh의 winding 법선이 `+z`인데 카메라가 `-z`쪽에서 진입해
`Physics.queriesHitBackfaces` 기본값 아래에서 `Collider.Raycast`가 항상 실패했고, 앞면으로 고쳐도
화면 중앙 ray가 Grid seed `(-2,-2,0)`이 아니라 원점을 겨냥해 Grid 범위 밖 좌표 `(3,1)`을
반환했습니다. 카메라를 seed 앞면으로 옮긴 뒤 최종 결과는 119/119입니다.

`Basic Usage` 원본은 stale 직렬화 Tile 127개를 제거하고 `HexGridSample`을 Scene에 실제 연결했으며,
runtime `Sprites/Default` Material과 vertex color checker 표현을 추가했습니다. Sample assembly 보조
컴파일은 통과했습니다. 기존 import Sample은 TestProject의 `SampleBackups` 밖에 백업한 뒤 Package
Manager로 최신 작업 사본을 갱신했습니다. 중앙 seed Triangle 100과 Tile Radius 0.38에서 Play Mode checker Grid 127개와
생성 로그를 확인했습니다. 실측 중 발견한 Clear 순서 예외를 수정하고 Rebuild/Clear/Rebuild를 다시
실행해 오류 없이 복원되는 것도 확인했습니다. OS UI 자동화 click은 Input System Game View pointer
좌표로 전달되지 않아 pointer 이벤트만 별도 실제 입력 확인 항목으로 남깁니다.

## 일정 목표

아래 P1~P3와 안정화 게이트의 남은 항목은 **2026-08-31 완료를 1차 목표**로 하고, 늦어도
**2026-09-15**까지 마무리합니다. 기능 구현과 Unity 자동 검증을 먼저 끝낸 뒤 실제 입력/시각 확인과
최종 한·영 문서·이미지 검수를 닫습니다. 외부 프로젝트 수요가 선행 조건인 선택적 통합은 수요가
확정되지 않으면 구현 완료가 아니라 명시적 보류 결정과 근거를 남깁니다.

## 다음 기능 후보

### P1 — 정확도와 입력 Surface 확장

- [x] Face 중심 adjacency graph geodesic 상한과 metric distortion 기반 Patch 자동 분할
      (`SurfacePatchSet`, 공통 frontier 좌표계, Face 중복 없는 전체 Grid 병합,
      `DistortionAdaptiveSurfaceParameterizer`; heat method는 비교 범위 밖)
- [x] shared boundary canonicalization으로 인접 Tile clipping 교점을 bit-identical intrinsic 좌표로 통일
- [x] Terrain heightfield는 비평면 loop unfolding/seed 접평면의 fold 대신 local XZ chart를 사용하고,
      Face barycentric binding은 clipping 원본 점에서 복원. offset 적용 공유 경계도 watertight 유지
- [x] Parameterizer 진단 6종을 `SurfacePatchDiagnostics` 결과 컨테이너로 통합. 이미 존재하는
      `SurfaceGridRequest`/`SurfacePatchBuildSettings`/`SurfaceQueryOptions` 외에는 실행 의존성을
      불필요하게 DTO로 감싸지 않음
- [x] Picker 계층의 중복 `bool` + 무명 tuple 반환을 `HexTilePickResult`로 교체하고 전체 Runtime API의
      외부 경계 tuple 반환을 재검수
- [x] Skinned Mesh barycentric deformation binding (2026-08-21) — bone 가중치 barycentric 보간,
      `BakeMesh` 없이 `LateUpdate`에서 위치·법선만 갱신
- [ ] Tile별 None/Fill/Outline/Both Draw Mode와 Editor Tile ListView·Scene View 외곽 Gizmo. Backend
      partition 렌더링(`MeshSurfaceGridRenderBackend.ApplyDrawModePartition`)과 Outline Geometry에 이어,
      override 여부를 신뢰할 수 없던 일반 `[Serializable]` class를 `IHexTileDrawPolicy` +
      `[SerializeReference, SerializeReferenceSelector]` 다형 정책으로 교체했습니다(2026-08-25).
      `NoneDrawPolicy`/`FillDrawPolicy`/`OutlineDrawPolicy`/`BothDrawPolicy`를 Settings 기본값과 Tile별 override 양쪽에서
      선택하며, managed reference clear가 실제 `null`을 보존해 Settings fallback이 동작합니다.
      None은 Tile의 논리 상태와 입력 계약은 유지하고 Fill/Outline draw만 제외합니다.
      `MeshTopology.Lines`에 전달되지도 않았던 `OutlineDrawPolicy.Thickness/Padding` placeholder는
      실제로는 Edit/Play 모두에서 영원히 효과가 없어 제거했습니다. 두께·여백은 단위와
      triangle band geometry 계약을 정의한 별도 기능으로만 다시 추가해야 합니다.
      `IsActive == false`는 런타임 입력에서는 계속 무시하지만 Scene View 편집 선택에서는
      타일 정보를 열어 다시 활성화할 수 있게 picker filtering을 분리했습니다.
      Edit Mode에서도 실제 Mesh Preview를 복구·유지하고 Tile List/Overlay의 SerializedProperty 변경을
      즉시 Backend에 다시 적용하도록 연결했습니다. Basic/Terrain Sample도 Play Mode `Start`에서만
      Material/checker 색상을 구성하던 잘못된 정책을 제거하고 `[ExecuteAlways] OnEnable`에서 같은 실제
      Backend 입력을 Edit/Play 양쪽에 적용합니다. 구현·생성 csproj 6개 컴파일은 완료했고, 열린 Unity
      Editor에서 Edit Mode 실제 Mesh와 Play Mode 결과의 동일성/Selector 목록/clear null fallback을
      확인한 뒤 완료로 표시합니다. 대규모 Terrain용 GPU heightmap visualization Backend는 아래 P3로
      옮겨 명시적으로 보류합니다

### P2 — 규모 확장

- [x] Patch intrinsic 경계의 회전 낭비 제거. Triangle Unfolding은 seed Face의 A를 원점, B를 +X축에
      두므로 chart가 원본과 다른 각도로 놓입니다. Unity 기본 Plane(10×10)의 경계가 14.14×14.14로
      나오는 이유이며, 축 정렬 경계를 순회하면 후보의 약 절반이 표면과 겹치지 않아 버려집니다.
      결과는 정확하지만, 주축 정렬 chart 또는 convex hull 기반 후보 산출로 Bake 비용을 줄일 수
      있습니다(현재 Plane 기준 Tile Radius 0.5에서 56ms).
      (convex hull SAT 사전 필터와 후보/Region build 계측, 반복 Unity 실행 기준 약 8.5~9.7ms 및 clipping
      677/1533 baseline 기록; 할당 계측 API는 해당 Mono 실행에서 0B를 반환해 진단 한계로 기록)

- [x] immutable `SurfaceGridSnapshot` version 및 추가/제거/binding 변경 `SurfaceGridDelta` 계약
- [x] `SurfaceGridChunk` dirty 집합과 Chunk 단위 `SurfaceGridChunkGeometryBuilder`
- [x] Burst/Jobs 기반 barycentric 위치·법선 평가 커널과 NativeArray 수명 계약
- [x] GPU structured vertex/index/visual buffer 및 indexed indirect draw Backend
- [x] seed tangent 1차 ExpMap 기준 구현과 동일 입력 distortion 비교·adaptive 분할 정책

### P3 — 선택적 통합

현재 Core 정확도·성능 기반을 먼저 닫는 결정에 따라 아래 항목은 **확장안으로 보류**합니다. 실제 제품
요구, 대상 Render Pipeline과 데이터 수명 계약이 확정되기 전에는 Core에 선택적 의존성을 추가하지 않습니다.

- [ ] 실제 프로젝트 수요가 확인된 경우 별도 URP/HDRP Backend 패키지
- [ ] 별도 패키지로 pathfinding, Tile grouping 및 고수준 시각 효과
- [ ] 좌표 lookup을 Inspector에서 직접 편집해야 하는 실제 요구가 확인되면 Unity 6000.6 네이티브
      `[SerializeField] Dictionary<TKey,TValue>` 적용과 패키지 최소 버전 상향의 비용을 비교합니다.
      런타임 조회 캐시만 필요하면 현재 직렬화 목록+Dictionary 캐시 구조를 유지하며, 별도
      `SerializedDictionary` 패키지나 커스텀 직렬화 구현은 도입하지 않습니다.
- [ ] 대규모 Terrain용 GPU heightmap visualization Backend (2026-08-25 보류 결정) — 기존
      `StructuredBufferSurfaceGridRenderBackend`가 Grid Geometry/Visual 업로드와 grid 전체 단일
      Draw Mode까지는 이미 지원하지만, heightmap 샘플링 기반 대규모 시각화 자체는 실제 수요가
      확인된 적이 없어 구현하지 않습니다

## 제품화 판단

현재 목표는 직접 사용하는 작고 조합 가능한 기반입니다. 향후 제품화한다면 무료 Core는 Surface
identity, topology, logical Grid, 기본 Mesh Backend까지 제공하고 유료 확장은 pathfinding, grouping,
영역 편집, Terrain/Skinned Adapter, 대규모 GPU Backend와 고수준 시각 효과를 묶는 구성이 적합합니다.
단순 메시 타일 정합만으로는 경쟁력이 약하므로, 논리 Grid와 Surface binding을 렌더링에서 분리한
확장성이 제품 차별점이어야 합니다.
