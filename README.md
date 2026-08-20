# Jeomseon Unity Grid Tile System

육각형 좌표, 타일 데이터와 상호작용, 표면 투영 기반 Grid 시각화를 제공하는 Unity 패키지입니다.

## 요구 사항

- Unity 6000.5.7f1 이상
- `com.jeomseon.unity.projector`와 `com.jeomseon.unity.shaders`(자동 설치)
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

1. Package Manager에서 `Basic Usage` Sample을 Import합니다.
2. `HexGridBasicUsage` Scene을 엽니다. 별도 Auto Fix 없이 Projector, Effect와 표면 Material이 연결됩니다.
3. 직접 구성할 때는 같은 GameObject에 `MeshProjector`와 `HexGridController`를 추가하고
   `HexGridSettings`와 `HexGridProjectorEffect`를 연결합니다.

`MeshProjector`는 내부 Material을 외부에 노출하지 않으며 `MeshRenderer`, `SkinnedMeshRenderer`,
`Terrain` 표면을 receiver로 처리합니다. 상호작용 대상에는 Collider가 필요합니다.

## Unity 기본 기능과의 비교

Unity의 2D Hexagonal Tilemap이 요구 사항을 충족한다면 기본 기능을 우선 검토할 수 있습니다. 이 패키지는 3D 표면 투영, 물리 레이캐스트와 타일별 런타임 상태를 함께 다루는 용도입니다.

## 현재 제한 사항

- Terrain은 기본 129 해상도의 생성 메시를 사용하며 Projector에서 조절할 수 있습니다.
- 입력은 Unity Input System의 마우스 장치를 사용합니다.
- 큰 그리드의 부분 갱신 및 Burst/Jobs 최적화는 아직 제공하지 않습니다.

영문 문서는 [README.en.md](README.en.md)를 참고하세요.
