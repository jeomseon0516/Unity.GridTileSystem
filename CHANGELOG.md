# 변경 기록

이 문서는 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 형식을 따릅니다.

## [Unreleased]

- **(Breaking)** URP `DecalProjector`와 사용자 변경 가능한 Grid Material을 제거하고, 내부 Material을
  소유하는 `Jeomseon.Unity.Projector.MeshProjector` + 검증된 `ProjectorEffect` 구성으로 교체했습니다.
- Grid 렌더링 수학을 `Jeomseon.Unity.Shaders`의 `HexagonShapeCore.hlsl`과 `HexGridCore.hlsl`로
  분리했습니다. GridTileSystem은 타일 상태 버퍼와 Projector 통합만 담당합니다.
- 가장 바깥쪽 타일은 중심 좌표가 Grid 안에 있으면 경계 밖 픽셀도 인접 타일의 단일 도형 외곽선으로
  복원해 선 굵기와 안티앨리어싱을 포함한 전체 외곽선을 렌더링합니다.
- Basic Usage Scene은 특정 Render Pipeline, URP Asset, Decal Feature나 Auto Fix 없이 가져오는 즉시
  구성되며 파이프라인 비종속 표면 Shader를 사용합니다.
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
  `HexTileSelectionState`/`HexTileBufferUploader`)로 분리했습니다(P1-01).
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
