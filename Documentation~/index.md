# Jeomseon Unity Grid Tile System

설치와 기본 사용 방법은 패키지 루트의 `README.md`를 참고하세요.

## 주요 API

- `HexGridController`: 타일 생성, 조회, 상호작용 이벤트와 렌더링 데이터를 관리합니다.
- `HexGridSettings`: 타일 반지름, Grid 반지름과 상호작용 Layer를 직렬화합니다.
- `IHexTile`: 타일의 좌표, 상태, 색상과 이벤트를 노출합니다.
- `AxialCoordinates`: Q, R 축 좌표입니다.
- `HexCoordinates`: Q, R, S 큐브 좌표입니다.

## 렌더링 경계

`HexGridController`는 `Jeomseon.Unity.Projector.MeshProjector`에 타일 상태 버퍼를 전달합니다.
Projector의 Material은 외부에 노출되지 않으며 `HexGridProjectorEffect`가 GridTileSystem과 Projector를
연결합니다. 단일 육각형 외곽선과 Grid 중심·인덱스 수학은 `Jeomseon.Unity.Shaders`에 있어 독립적으로
재사용할 수 있습니다.

표시는 `MeshRenderer`, `SkinnedMeshRenderer`, `Terrain` Receiver를 지원합니다. 포인터 상호작용에는
Receiver 표면의 Collider와 `HexGridSettings.InteractionLayerMask` 설정이 필요합니다.
