# Jeomseon Unity Grid Tile System

설치와 기본 사용 방법은 패키지 루트의 `README.md`를 참고하세요.

## 주요 API

- `GridManager`: 타일 생성, 조회, 상호작용 이벤트와 렌더링 데이터를 관리합니다.
- `IHexGrid`: 타일의 좌표, 상태, 색상과 이벤트를 노출합니다.
- `AxialCoordinates`: Q, R 축 좌표입니다.
- `HexCoordinates`: Q, R, S 큐브 좌표입니다.

## 확장 방향

현재 구현은 URP DecalProjector를 사용합니다. 다른 렌더 파이프라인을 지원하려면 좌표 및 타일 데이터 계층과 렌더링 계층을 분리하는 작업이 우선 필요합니다.
