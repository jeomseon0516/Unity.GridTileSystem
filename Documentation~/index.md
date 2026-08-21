# Jeomseon Unity Grid Tile System

설치와 기본 사용 방법은 패키지 루트의 `README.md`를 참고하세요.

## 주요 API

- `HexGridController`: 타일 생성, 조회, 상호작용 이벤트와 렌더링 데이터를 관리합니다.
- `HexGridSettings`: 타일 반지름, Grid 반지름과 상호작용 Layer를 직렬화합니다.
- `IHexTile`: 타일의 좌표, 상태, 색상과 이벤트를 노출합니다.
- `HexTileData`: UnityEvent/Renderer 없이 저장·복제·동기화할 수 있는 타일 상태입니다.
- `AxialCoordinates`: Q, R 축 좌표입니다.
- `HexCoordinates`: Q, R, S 큐브 좌표입니다.

## 렌더링 경계

`HexGridController`는 intrinsic Surface Grid Geometry snapshot을 `ISurfaceGridRenderBackend`에 전달합니다.
기본 Backend는 공통 Mesh API만 사용하며 Projector 또는 특정 Render Pipeline을 참조하지 않습니다.
파이프라인별 최적화가 필요하면 같은 snapshot 계약을 소비하는 별도 Backend를 구현합니다.
Output Mesh 참조를 둘 다 비우면 Backend 없이 논리 타일과 피킹만 Bake할 수 있습니다.

현재 Runtime Surface Adapter는 readable Static Mesh를 지원합니다. 포인터 상호작용에는 원본 Mesh와
같은 Mesh를 사용하는 MeshCollider와 `HexGridSettings.InteractionLayerMask` 설정이 필요합니다.

## Basic Usage 검증

`HexGridBasicUsage`는 Unity Plane 중앙 Triangle 100에서 Tile Radius 0.38, Grid Radius 6으로
시작합니다. Play Mode에서 checker 색상의 127개 Tile과 `Generated hex tile count: 127` 로그를
확인합니다. Edit Mode에서는 Controller Inspector의 `Rebuild Tiles`, `Clear Baked Tiles`,
`Rebuild Tiles`를 순서대로 실행해 runtime Mesh와 127개 Tile이 예외 없이 복원되는지 확인합니다.
