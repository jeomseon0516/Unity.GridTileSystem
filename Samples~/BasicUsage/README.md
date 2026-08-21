# 기본 사용 예제

이 샘플은 Projector 없이 Static Mesh topology에서 intrinsic Grid를 생성하는 예제입니다.

`HexGridBasicUsage` Scene을 열고 Play Mode에 진입합니다. Scene에는 다음 항목이 미리
연결돼 있습니다.

- `HexGridSettings` asset (`Tile Radius: 0.38`, `Grid Radius: 6`, `Interaction Layer Mask: Layer 3`)
- 같은 Mesh를 사용하는 source `MeshFilter`와 `MeshCollider`
- 별도 output `MeshFilter`/`MeshRenderer`
- Layer 3의 Surface와 Main Camera
- `HexGridController`와 `HexGridSample`

Surface 경계와 겹치는 타일만 생성되고 Grid Geometry가 Plane 표면에 정합되는지 확인합니다. 기본
Sample 스크립트는 별도 Shader asset이나 Render Pipeline 의존 없이 Unity 기본
`Sprites/Default` Shader로 runtime Material을 만들고 Logical 좌표 parity에 따라 두 vertex color를
교차 적용합니다. Player에서 해당 기본 Shader를 strip했다면 경고가 출력되며, 이 경우 vertex color를
소비하는 프로젝트 Material을 output MeshRenderer에 지정합니다.

Unity 기본 Plane의 중앙 Triangle인 index `100`을 seed로 사용합니다. 정상 실행 시 Console에
`Generated hex tile count: 127`이 출력되고, 청록/파랑 checker Grid가 Plane 중앙에 표시됩니다.
Inspector의 `Rebuild Tiles → Clear Baked Tiles → Rebuild Tiles` 순서에서도 예외 없이 127개가
복원돼야 합니다.

Edit Mode에서는 `HexGridController`를 선택하고 Scene의 타일을 클릭하면 상세 속성 Overlay가
표시됩니다. 모든 타일의 라벨을 상시 그리지 않으며 선택 좌표만 Scene에 표시됩니다. Overlay 상단의
`Content Opacity` 슬라이더로 상세 속성 영역의 투명도를 조절할 수 있습니다.
