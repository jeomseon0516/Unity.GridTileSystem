# 기본 사용 예제

이 샘플은 Projector 없이 Static Mesh topology에서 intrinsic Grid를 생성하는 예제입니다.

Edit Mode에서 `Rebuild Tiles`를 누르면 실제 Tile Mesh를 미리 확인할 수 있습니다.
`Clear Baked Tiles`는 명시적으로 0개 상태를 유지하며, Play Mode는 Bake된 Tile이 없으면
실행에 필요한 Grid를 자동 Bake합니다.

`HexGridBasicUsage` Scene을 열고 Play Mode에 진입합니다. Scene에는 다음 항목이 미리
연결돼 있습니다.

- `HexGridSettings` asset (`Tile Radius: 0.5`, `Interaction Layer Mask: Layer 3`)
- 같은 Mesh를 사용하는 source `MeshFilter`와 `MeshCollider`
- Controller 전용 별도 output `MeshFilter`/`MeshRenderer`
- Layer 3의 Surface와 Main Camera
- `HexGridController`와 `HexGridSample`

육각형 전체가 Surface 안에 들어오는 완전한 타일만 생성되고 Grid Geometry가 Plane 표면에 정합되는지
확인합니다. Surface 경계에서 일부가 잘리는 타일은 생성하지 않으므로 가장자리에는 Tile Radius에 따른
여백이 생깁니다. output MeshRenderer에는 Unity built-in `Sprites/Default` Material이 Scene에 미리
직렬화돼 있으며, Sample 스크립트가 Material이나 Tile 색상을 lifecycle 중 변경하지 않습니다.
Edit Mode와 Play Mode 모두 Controller의 동일한 실제 Mesh Backend 결과를 표시합니다.

기존 중앙 Triangle index `100`의 중심을 월드 `Seed Offset`으로 마이그레이션했습니다. 사용자가
Triangle 번호를 입력하는 단계는 없습니다. 정상 실행 시 Console에
`Generated hex tile count: 126`이 출력되고, 기본 cyan Grid의 외곽에 잘린 타일이 없어야 합니다.
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

Grid가 덮는 최대 범위는 `HexGridSettings`의 `Maximum Patch Triangles`와 `Maximum Patch Radius`가
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

이벤트가 전혀 발생하지 않고 `Mouse.current.position`이 `(0,0)`으로 유지되면 Grid나 Collider보다 먼저
`Edit > Project Settings > Input System Package > Play Mode Input Behavior`를 확인합니다. 기본값
`Pointers And Keyboards Respect Game View Focus`에서는 Game View가 포커스를 가져야 pointer 입력이
Player Loop로 전달됩니다. 포커스와 무관한 반복 검증 프로젝트는 별도 Input Settings asset을 만들고
`All Device Input Always Goes To Game View`를 명시할 수 있습니다. 이 설정은 프로젝트 전체 Editor 입력
정책이므로 패키지 Runtime에서 변경하지 않습니다.

`HexTileSelectionState`는 **새 타일의 Enter를 이전 타일의 Exit보다 먼저** 발행합니다. 타일 사이를
직접 이동할 때 `Entered → Exited` 순서로 보이는 것이 정상이며, Exit에서 상태를 되돌리는 코드는 이
순서를 전제해야 합니다. 네 이벤트 모두 유효한 타일에서만 발생하므로 Grid 밖 클릭은 아무 이벤트도
만들지 않습니다.

Edit Mode에서는 `HexGridController`를 선택하고 Scene의 타일을 클릭하면 상세 속성 Overlay가
표시됩니다. 모든 타일의 라벨을 상시 그리지 않으며 선택 좌표만 Scene에 표시됩니다. Overlay 상단의
`Content Opacity` 슬라이더로 상세 속성 영역의 투명도를 조절할 수 있습니다.
