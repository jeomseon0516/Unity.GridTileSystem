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
2. **P1-01 — GridManager 책임 분리**
   - 입력, Raycast, 타일 데이터, 선택 상태, GPU 갱신을 독립 서비스로 분리합니다.
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
