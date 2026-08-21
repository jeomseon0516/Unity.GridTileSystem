# Terrain 사용 예제

`TerrainBasicUsage` Scene은 TerrainData를 Mesh로 복제하지 않고 계산형 virtual topology로 Hex Grid를
생성합니다. Scene을 열고 Play Mode에 진입하면 주황/노랑 checker Tile이 완만한 Terrain 굴곡에
정합되어야 합니다.

- Receiver의 `Surface Kind`는 `Terrain`입니다.
- `Source Terrain`과 `Terrain Collider`는 같은 TerrainData를 사용합니다.
- Heightmap 정점·Triangle·인접 관계는 index 접근 시 계산됩니다.
- 완전한 Hex만 유지하므로 Terrain 외곽에는 잘린 Tile 대신 여백이 생깁니다.
- TerrainCollider hit는 local heightfield cell과 barycentric 좌표로 변환되어 picking에 사용됩니다.

Console의 `Generated Terrain hex tile count:`가 0보다 크고 Play Mode 진입·종료 중 예외가 없는지
확인합니다. Terrain hole은 traversal 경계이므로 hole을 가로질러 Grid가 연결되지 않아야 합니다.
