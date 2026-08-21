# Jeomseon Unity Grid Tile System

> 다음 릴리스의 intrinsic Surface Grid 구조는 기존 Projector 기반 Scene과 호환되지 않습니다. 새
> `HexGridController` 구성으로 다시 Bake해야 합니다.

Triangle topology와 intrinsic Surface Space 위에 육각 Grid를 생성하는 Unity 패키지입니다. Grid를
월드 XZ 또는 Projector 평면에서 투영하지 않으므로 벽, 접힌 Mesh와 곡면을 topology를 따라 다룹니다.

## 요구 사항

- Unity 6000.5.7f1 이상
- Runtime topology 생성에 Read/Write Enabled인 Static Mesh
- 원본 Mesh와 같은 Mesh를 사용하는 MeshCollider(picking을 사용할 경우)
- OpenUPM Scoped Registry의 `com.jeomseon.unity` 스코프

Projector, URP, HDRP 또는 별도 Shader 패키지에 의존하지 않습니다.

## 설치

Unity Package Manager에서 `com.jeomseon.unity.grid-tile-system`을 추가합니다.

## 기본 구성

1. `HexGridSettings` asset을 생성합니다.
2. Surface용 `MeshFilter`와 동일 Mesh의 `MeshCollider`를 준비합니다.
3. 시각화가 필요하면 Grid 출력 전용 GameObject에 별도 `MeshFilter`와 `MeshRenderer`를 둡니다.
4. Controller에 source 참조와 Settings를 연결하고, 시각화할 때만 output 참조 두 개를 함께 연결합니다.
5. Seed Triangle index와 barycentric 좌표를 지정하고 `Rebuild Tiles`를 실행합니다.

Output 두 참조를 모두 비우면 논리 타일·피킹·상태만 사용하는 logical-only Grid로 동작합니다. 출력할
때는 Source와 output MeshFilter가 달라야 하며 Controller가 원본 Geometry 덮어쓰기를 검증합니다.

`HexTileData`는 좌표·속성·색상·활성 상태만 담고, `HexTile`은 이 데이터에 UnityEvent 상호작용을
결합합니다. 저장이나 네트워크 변환은 `HexTile.Data`를 기준으로 하고, 데이터를 직접 수정한 뒤에는
필요한 시점에 `HexGridController.RefreshRendering()`을 호출합니다.

```text
Static Mesh Adapter → Triangle topology → Triangle Unfolding local Patch
→ intrinsic Hex → Triangle/Hex clipping → barycentric Surface Region
→ pipeline-independent Geometry snapshot → Mesh Render Backend
```

색상과 활성 상태는 vertex color/alpha로 전달됩니다. Material이 vertex color를 어떻게 표현할지는
Material 또는 향후 파이프라인별 Backend가 결정합니다.

## 현재 제한 사항

- Runtime Adapter는 readable Static Mesh부터 지원합니다.
- Terrain virtual topology와 Skinned Mesh binding은 후속 단계입니다.
- 곡률이 있는 큰 영역은 여러 local Patch와 seam이 필요합니다.
- shared-boundary 통합, Chunk, Burst/Jobs 최적화는 아직 제공하지 않습니다.
- 입력은 Unity Input System과 MeshCollider raycast를 사용합니다.

수학 학습 순서는 Harness의 `architecture/intrinsic-surface-grid-study-guide.md`에 정리돼 있습니다.

영문 문서는 [README.en.md](README.en.md)를 참고하세요.
