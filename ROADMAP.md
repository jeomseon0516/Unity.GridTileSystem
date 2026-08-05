# Grid Tile System 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **P0-01 — 좌표·인덱스 정확성**
   - 경계값, 중복, 음수 좌표 및 좌표 변환 왕복 테스트를 보강합니다.
2. **P1-01 — GridManager 책임 분리**
   - 입력, Raycast, 타일 데이터, 선택 상태, GPU 갱신을 독립 서비스로 분리합니다.
3. **P1-02 — 순수 타일 데이터와 Unity 이벤트 분리**
   - 직렬화 가능한 데이터 모델과 런타임 상호작용 상태의 경계를 정의합니다.
4. **P1-03 — Inspector Reflection 제거**
   - GridManager가 명시적인 Editor 조회 API 또는 SerializedProperty 경로를 제공합니다.
5. **P2-01~03 — 부분 갱신과 Scene View 최적화**
   - 변경 영역만 계산하고 ComputeBuffer를 재사용하며 라벨을 가시 범위로 제한합니다.
6. **P3-01 — 렌더링 백엔드 추상화**
   - URP Decal 구현을 인터페이스 뒤로 격리해 다른 파이프라인 확장을 허용합니다.
7. **P3-02 — SerializedDictionary 연동**
   - 별도 패키지가 안정화되면 좌표 조회 성능과 Inspector 편의성을 비교합니다.
