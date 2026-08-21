# 기본 사용 예제

이 샘플은 Projector 없이 Static Mesh topology에서 intrinsic Grid를 생성하는 예제입니다.

`HexGridBasicUsage` Scene을 열고 Play Mode에 진입합니다. Scene에는 다음 항목이 미리
연결돼 있습니다.

- `HexGridSettings` asset (`Tile Radius: 0.5`, `Interaction Layer Mask: Layer 3`)
- `Receivers` 목록의 평면 Receiver 하나
- Receiver에서 같은 Mesh를 사용하는 source `MeshFilter`와 `MeshCollider`
- Receiver 전용 별도 output `MeshFilter`/`MeshRenderer`
- Layer 3의 Surface와 Main Camera
- `HexGridController`와 `HexGridSample`

육각형 전체가 Surface 안에 들어오는 완전한 타일만 생성되고 Grid Geometry가 Plane 표면에 정합되는지
확인합니다. Surface 경계에서 일부가 잘리는 타일은 생성하지 않으므로 가장자리에는 Tile Radius에 따른
여백이 생깁니다. 기본
Sample 스크립트는 별도 Shader asset이나 Render Pipeline 의존 없이 Unity 기본
`Sprites/Default` Shader로 runtime Material을 만들고 Logical 좌표 parity에 따라 두 vertex color를
교차 적용합니다. Player에서 해당 기본 Shader를 strip했다면 경고가 출력되며, 이 경우 vertex color를
소비하는 프로젝트 Material을 output MeshRenderer에 지정합니다.

Unity 기본 Plane의 중앙 Triangle인 index `100`을 seed로 사용합니다. 정상 실행 시 Console에
`Generated hex tile count: 126`이 출력되고, 청록/파랑 checker Grid의 외곽에 잘린 타일이 없어야 합니다.
Inspector의 `Rebuild Tiles → Clear Baked Tiles → Rebuild Tiles` 순서에서도 예외 없이 같은 개수가
복원돼야 합니다.

## Tile 해상도

Grid 범위를 지정하는 설정은 없습니다. Grid는 항상 Surface 전체를 덮으며, 사용자가 정하는 값은
`Tile Radius` 하나입니다. Hex 면적은 반지름의 제곱에 비례하므로 반지름을 절반으로 줄이면 Tile 수는
약 네 배가 됩니다. Unity 기본 Plane 기준 실측값은 다음과 같습니다.

| Tile Radius | Tile 수 |
| --- | --- |
| 0.38 | 229 |
| 0.5 (기본) | 126 |
| 0.75 | 50 |
| 1.0 | 26 |

`Tile Radius`는 원본 Mesh의 **local 공간 길이**입니다. Sample의 Plane은 scale 1.4를 쓰므로 화면에
보이는 Tile은 이 값의 1.4배 크기입니다.

Grid가 덮는 최대 범위는 `HexGridController`의 `Maximum Patch Triangles`와 `Maximum Patch Radius`가
성능 안전장치로 제어합니다. 한계에 도달하면 Console에 경고가 출력되고 Grid가 표면 일부만 덮습니다.

## Pointer 상호작용 확인

Sample 스크립트는 네 가지 Tile 이벤트를 모두 구독해 Console에 좌표를 남깁니다. Play Mode에서 Game
View 위로 마우스를 움직이며 다음을 확인합니다.

| 동작 | 기대 로그 |
| --- | --- |
| Grid 타일 위로 진입 | `Entered tile: (q, r)` 1회 |
| 같은 타일 안에서 이동 | 추가 로그 없음 |
| 옆 타일로 이동 | 새 타일 `Entered` 먼저, 이전 타일 `Exited` 나중 |
| Grid 밖으로 이동 | `Exited tile: (q, r)` 1회 |
| 타일 위에서 클릭 | `Pointer down on tile:` → `Pointer up on tile:` |
| Grid 밖에서 클릭 | down/up 로그 없음 |

`HexTileSelectionState`는 **새 타일의 Enter를 이전 타일의 Exit보다 먼저** 발행합니다. 타일 사이를
직접 이동할 때 `Entered → Exited` 순서로 보이는 것이 정상이며, Exit에서 상태를 되돌리는 코드는 이
순서를 전제해야 합니다. 네 이벤트 모두 유효한 타일에서만 발생하므로 Grid 밖 클릭은 아무 이벤트도
만들지 않습니다.

Edit Mode에서는 `HexGridController`를 선택하고 Scene의 타일을 클릭하면 상세 속성 Overlay가
표시됩니다. 모든 타일의 라벨을 상시 그리지 않으며 선택 좌표만 Scene에 표시됩니다. Overlay 상단의
`Content Opacity` 슬라이더로 상세 속성 영역의 투명도를 조절할 수 있습니다.
