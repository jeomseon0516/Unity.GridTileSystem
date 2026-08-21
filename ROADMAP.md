# Grid Tile System 로드맵

## 현재 방향 — intrinsic Surface Grid

`GridTileSystem`은 Projector나 특정 Render Pipeline을 사용하지 않습니다. Triangle topology를 local
2D chart로 펼치고, 실제 길이로 정의한 Hex를 source Triangle과 교차한 뒤 barycentric binding으로
표면에 정합합니다. Projector는 별도의 실험적 패키지이며 이 패키지와 Adapter도 공유하지 않습니다.

설계와 수학의 기준 문서는 하네스의 다음 문서입니다.

- `architecture/intrinsic-surface-grid.md`
- `architecture/intrinsic-surface-grid-flow.md`
- `architecture/intrinsic-surface-grid-study-guide.md`
- `architecture/grid-tile-system-rendering-backends.md`

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

## 안정화 게이트

- [x] 생성 csproj 기준 Runtime/Tests/Editor/Samples 컴파일 오류 0건
- [x] package.json과 asmdef JSON 검증
- [x] Projector/Decal/Render Pipeline 타입이 Runtime API에 남지 않았는지 정적 검색
- [x] Unity 6000.5.7f1 EditMode CLI 128/128 통과 (2026-08-21, macOS 임시 검증 프로젝트)
- [x] `Basic Usage` Sample의 Game View/Scene View 시각 확인
- [x] Play Mode 진입·종료 및 Rebuild/Clear/Rebuild 실측
- [x] `ProcessPointer` 경로의 enter/exit/down/up 이벤트 순서 자동 검증
- [ ] 실제 Input System 장치 입력에서 Game View 좌표가 전달되는지 실측
      (`HexGridPointerInput.TryGetPointer` 구간만 남음, 자동화 불가)
- [ ] 최종 Sample/Inspector 이미지와 검증 절차를 Documentation에 반영

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

## 다음 기능 후보

### P1 — 정확도와 입력 Surface 확장

- [ ] graph/heat-method 등 실제 geodesic bound와 metric distortion 기반 Patch 자동 분할
- [ ] shared boundary canonicalization으로 인접 Patch의 미세한 seam 제거
- [x] Skinned Mesh barycentric deformation binding (2026-08-21) — bone 가중치 barycentric 보간,
      `BakeMesh` 없이 `LateUpdate`에서 위치·법선만 갱신
- [ ] 대규모 Terrain용 선택적 GPU heightmap visualization Backend

### P2 — 규모 확장

- [ ] Patch intrinsic 경계의 회전 낭비 제거. Triangle Unfolding은 seed Face의 A를 원점, B를 +X축에
      두므로 chart가 원본과 다른 각도로 놓입니다. Unity 기본 Plane(10×10)의 경계가 14.14×14.14로
      나오는 이유이며, 축 정렬 경계를 순회하면 후보의 약 절반이 표면과 겹치지 않아 버려집니다.
      결과는 정확하지만, 주축 정렬 chart 또는 convex hull 기반 후보 산출로 Bake 비용을 줄일 수
      있습니다(현재 Plane 기준 Tile Radius 0.5에서 56ms).

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
