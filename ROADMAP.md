# Grid Tile System 로드맵

## 현재 방향 — intrinsic Surface Grid

`GridTileSystem`은 Projector나 특정 Render Pipeline을 사용하지 않습니다. Triangle topology를 local
2D chart로 펼치고, 실제 길이로 정의한 Hex를 source Triangle과 교차한 뒤 barycentric binding으로
표면에 정합합니다. Projector는 별도의 실험적 패키지이며 이 패키지와 Adapter도 공유하지 않습니다.

설계와 수학의 기준 문서는 하네스의 다음 문서입니다.

- `architecture/intrinsic-surface-grid.md`
- `architecture/intrinsic-surface-grid-study-guide.md`
- `architecture/grid-tile-system-rendering-backends.md`

## 완료된 기반

- [x] `SurfaceHandle`/`SurfacePoint`로 Surface identity와 barycentric 위치 표현
- [x] compact Triangle adjacency, component 및 degenerate/winding/non-manifold 진단
- [x] readable Static Mesh topology Adapter
- [x] Triangle unfolding과 Triangle 수/intrinsic 반경/closure tolerance 기반 Patch 성장
- [x] convex polygon clipping과 barycentric Surface Region binding
- [x] 실제 길이 기반 flat-top Hex layout, Logical Tile 생성과 intrinsic picking
- [x] 파이프라인 비종속 `SurfaceGridGeometry` 및 `ISurfaceGridRenderBackend` 계약
- [x] 공통 Mesh API 기반 `MeshSurfaceGridRenderBackend`
- [x] `HexGridController`/Store/Selection 서비스를 intrinsic Grid 경로로 마이그레이션
- [x] `HexTileData` 순수 상태와 `HexTile` UnityEvent façade 분리
- [x] Output Backend 없는 logical-only Bake와 picking 지원
- [x] Projector/Shader/ComputeBuffer 의존 및 관련 Package asset 제거
- [x] Basic Usage Sample을 Source MeshCollider + Output MeshRenderer 구성으로 교체
- [x] non-manifold traversal boundary, Surface snapshot 혼용 및 비유한 입력 방어 테스트

## 안정화 게이트

- [x] 생성 csproj 기준 Runtime/Tests/Editor/Samples 컴파일 오류 0건
- [x] package.json과 asmdef JSON 검증
- [x] Projector/Decal/Render Pipeline 타입이 Runtime API에 남지 않았는지 정적 검색
- [x] Unity 6000.5.7f1 EditMode Test Runner 118/118 통과 (2026-08-21)
- [x] `Basic Usage` Sample의 Game View/Scene View 시각 확인
- [x] Play Mode 진입·종료 및 Rebuild/Clear/Rebuild 실측
- [ ] 실제 사용자 pointer 이동에 의한 enter/exit/down/up 이벤트 실측
- [ ] 최종 Sample/Inspector 이미지와 검증 절차를 Documentation에 반영

열린 TestProject Editor에서 Asset Database를 명시적으로 새로고침한 뒤 최신 118개 테스트를
인식·통과했습니다. 생성 csproj 빌드는 계속 보조 증거로만 취급합니다.

`Basic Usage` 원본은 stale 직렬화 Tile 127개를 제거하고 `HexGridSample`을 Scene에 실제 연결했으며,
runtime `Sprites/Default` Material과 vertex color checker 표현을 추가했습니다. Sample assembly 보조
컴파일은 통과했습니다. 기존 import Sample은 TestProject의 `SampleBackups` 밖에 백업한 뒤 Package
Manager로 최신 작업 사본을 갱신했습니다. 중앙 seed Triangle 100과 Tile Radius 0.38에서 Play Mode checker Grid 127개와
생성 로그를 확인했습니다. 실측 중 발견한 Clear 순서 예외를 수정하고 Rebuild/Clear/Rebuild를 다시
실행해 오류 없이 복원되는 것도 확인했습니다. OS UI 자동화 click은 Input System Game View pointer
좌표로 전달되지 않아 pointer 이벤트만 별도 실제 입력 확인 항목으로 남깁니다.

## 다음 기능 후보

### P1 — 정확도와 입력 Surface 확장

- [ ] graph/heat-method 등 실제 geodesic bound와 metric distortion 기반 Patch 자동 분할
- [ ] shared boundary canonicalization으로 인접 Patch의 미세한 seam 제거
- [ ] Terrain virtual topology Adapter와 GPU heightmap visualization
- [ ] Skinned Mesh barycentric deformation binding

### P2 — 규모 확장

- [ ] immutable snapshot/version 및 변경 집합(delta) 계약
- [ ] Chunk 단위 Geometry 재생성과 dirty Tile 부분 갱신
- [ ] Burst/Jobs 기반 topology·clipping·geometry 생성
- [ ] GPU structured buffer/indirect draw Backend
- [ ] ExpMap 기반 곡률이 큰 영역의 parameterization 비교 구현

### P3 — 선택적 통합

- [ ] 실제 프로젝트 수요가 확인된 경우 별도 URP/HDRP Backend 패키지
- [ ] 별도 패키지로 pathfinding, Tile grouping 및 고수준 시각 효과
- [ ] `SerializedDictionary` 패키지 안정화 후 좌표 lookup/Inspector 연동 비교

## 제품화 판단

현재 목표는 직접 사용하는 작고 조합 가능한 기반입니다. 향후 제품화한다면 무료 Core는 Surface
identity, topology, logical Grid, 기본 Mesh Backend까지 제공하고 유료 확장은 pathfinding, grouping,
영역 편집, Terrain/Skinned Adapter, 대규모 GPU Backend와 고수준 시각 효과를 묶는 구성이 적합합니다.
단순 메시 타일 정합만으로는 경쟁력이 약하므로, 논리 Grid와 Surface binding을 렌더링에서 분리한
확장성이 제품 차별점이어야 합니다.
