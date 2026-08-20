# 기본 사용 예제

이 샘플은 별도 Auto Fix 없이 열 수 있는 렌더 파이프라인 비종속 예제입니다.

`HexGridBasicUsage` Scene을 열고 Play Mode에 진입합니다. Scene에는 다음 항목이 미리
연결돼 있습니다.

- `HexGridSettings` asset (`Tile Radius: 0.038`, `Grid Radius: 6`, `Interaction Layer Mask: Layer 3`)
- 내부 Material을 소유하는 `MeshProjector`와 `HexGridProjectorEffect`
- Layer 3의 충돌 지면과 Main Camera
- `HexGridController`와 `HexGridSample`

127개 타일이 별도 Bake 조작 없이 표시되는지, 가장 바깥 타일의 육각형 외곽선이 잘리지 않는지,
Play Mode에서 예외가 없는지 확인합니다.

Edit Mode에서는 `HexGridController`를 선택하고 Scene의 타일을 클릭하면 상세 속성 Overlay가
표시됩니다. 모든 타일의 라벨을 상시 그리지 않으며 선택 좌표만 Scene에 표시됩니다. Overlay 상단의
`Content Opacity` 슬라이더로 상세 속성 영역의 투명도를 조절할 수 있습니다.
