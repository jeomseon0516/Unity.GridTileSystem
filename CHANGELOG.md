# 변경 기록

이 문서는 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 형식을 따릅니다.

## [Unreleased]

- **(Breaking)** `GridManager`의 `HexagonRadius`/`TileLimit`/`LayerMask`가 자체 직렬화 필드에서
  새 `HexGridSettings : ScriptableObject`(`Runtime/HexGridSettings.cs`) 참조로 이동했습니다.
  마이그레이션: `HexGridSettings` asset을 생성해 기존 세 값을 옮기고 `GridManager`의 새
  `settings` 필드에 할당하세요. `decalProjector`/`mainCamera`/`RootObject`/`hexGrids`는 그대로
  `GridManager`에 남아 있습니다.
- `GridManager`의 입력·Raycast·타일 데이터·선택 상태·GPU 갱신 책임을 `Runtime/Services/`의 5개
  독립 서비스(`HexGridPointerInput`/`HexGridTilePicker`/`HexGridTileDataStore`/
  `HexGridSelectionState`/`HexOptionBufferUploader`)로 분리했습니다(P1-01). `hexGrids` 필드는
  이름·직렬화 위치를 유지해 P1-03 대상 reflection/`FindProperty` 접근에 영향이 없습니다.
- `OnEnterTile`/`OnExitTile`/`OnMouseDownTile`/`OnMouseUpTile`이 매 이벤트마다 리스너를 2회씩
  호출하던 버그를 수정했습니다.
- Runtime 스크립트로 `HexagonRadius`를 바꿔도 DecalProjector 머티리얼이 갱신되지 않던 문제를
  수정했습니다(설정 asset의 `SettingsChanged` 이벤트로 Editor/Runtime 양쪽에서 항상 갱신).
- 레이캐스트 타일 피킹의 큐브 좌표 라운딩에서 S 성분 보정이 자기 자신(`roundHex.z`)을 참조해
  Q+R+S=0 불변식이 깨지고 엉뚱한(때로는 존재하지 않는) 타일이 선택되던 결함을 수정했습니다.
  라운딩 로직을 `GridManager`에서 `HexCoordinates.Round(Vector2)` 정적 메서드로 추출해 순수
  단위 테스트로 검증 가능하게 했습니다.
- `HexGrid.GetHexIndex`(축 좌표별 인덱스 매핑)가 경계값·음수 좌표를 포함해 연속적이고 중복 없는
  인덱스를 만드는지 확인하는 회귀 테스트를 추가했습니다(수학적으로는 기존부터 올바른 구현이었고,
  테스트 공백만 채웠습니다).

## [0.2.0] - 2026-08-13

- **(Breaking)** 개명 전 `Jeomseon.HexGrid` Runtime/Editor 네임스페이스를 패키지 규칙에 맞춰
  `Jeomseon.Unity.GridTileSystem`과 `Jeomseon.Unity.GridTileSystem.Editor`로 변경했습니다. 이전
  네임스페이스 호환 별칭은 제공하지 않습니다.

## [0.1.4] - 2026-08-11

- 워크스페이스 명명 규칙에 맞춰 `GridManager`·`HexGrid`의 `[SerializeField] private` 필드를
  `_camelCase`에서 `camelCase`로 정리하고(`[FormerlySerializedAs]`로 기존 이름 보존),
  `GridManager`의 `SCREAMING_SNAKE_CASE` 상수를 `PascalCase`로 정리했습니다.
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
