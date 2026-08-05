# Jeomseon Unity Grid Tile System

육각형 좌표와 타일 데이터, 마우스 상호작용, URP 데칼 기반 그리드 표시 기능을 제공하는 Unity 패키지입니다.

## 요구 사항

- Unity 6000.5.7f1 이상
- Universal Render Pipeline 14.0.11 이상
- OpenUPM Scoped Registry에 `com.jeomseon.unity` 스코프 등록

## 설치

Unity Package Manager에서 이름으로 다음 패키지를 추가합니다.

```text
com.jeomseon.unity.grid-tile-system
```

OpenUPM 게시 전에는 Git URL로 설치할 수 있습니다.

```text
https://github.com/jeomseon0516/Unity.GridTileSystem.git#v0.1.0
```

## 기본 사용

1. URP 프로젝트의 GameObject에 `GridManager`와 `DecalProjector`를 구성합니다.
2. 데칼 머티리얼과 카메라, 레이어 마스크를 연결합니다.
3. Inspector의 `Calculate Grids`로 타일 데이터를 생성합니다.
4. `GetGrids`, `TryGetTileDataByRay` 및 타일 이벤트로 게임 로직을 연결합니다.

## Unity 기본 기능과의 비교

Unity의 2D Tilemap 및 Hexagonal Tilemap이 요구 사항을 충족한다면 기본 기능을 우선 검토할 수 있습니다. 이 패키지는 월드 공간의 표면을 레이캐스트하고 URP 데칼로 육각 그리드를 표시하며 타일별 런타임 상태를 다루는 3D 사용 사례를 목표로 합니다.

## 현재 제한 사항

- 렌더링 구현은 URP `DecalProjector`에 종속됩니다.
- 입력은 Unity Input System의 마우스 장치를 사용합니다.
- 큰 그리드의 부분 갱신 및 Burst/Jobs 최적화는 아직 제공하지 않습니다.

영문 문서는 [README.en.md](README.en.md)를 참고하세요.
