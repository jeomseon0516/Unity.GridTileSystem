# Terrain 사용 예제

`TerrainBasicUsage` Scene은 TerrainData를 Mesh로 복제하지 않고 계산형 virtual topology로 Hex Grid를
생성합니다. Scene에 149개 Tile이 Bake돼 있어 열자마자 기본 cyan Tile Mesh가 두 개의
가파른 능선·중앙 골짜기 Terrain에 정합됩니다. Surface나 seed를 바꾼 뒤에는 Edit Mode의
`Rebuild Tiles`로 갱신합니다. Play Mode는 Bake된 Tile이 없으면 자동 Bake합니다.

- `Terrain`과 `Terrain Collider`는 같은 TerrainData를 사용하며 별도 Surface 등록은 없습니다.
- Controller의 월드 `Seed Offset`에서 Terrain을 자동으로 찾습니다.
- Heightmap 정점·Triangle·인접 관계는 index 접근 시 계산됩니다.
- 비평면 heightfield를 shared-edge unfolding이나 seed 접평면에 투영하면 굴곡이 큰 곳에서 chart가
  접히거나 겹칠 수 있으므로, Terrain은 높이와 무관한 local XZ heightfield chart를 사용합니다.
  렌더 Geometry는 barycentric binding으로 원래 heightfield 곡면에 정확히 복원됩니다.
- 완전한 Hex만 유지하므로 Terrain 외곽에는 잘린 Tile 대신 여백이 생깁니다.
- TerrainCollider hit는 local heightfield cell과 barycentric 좌표로 변환되어 picking에 사용됩니다.
- Sample 출력 `Surface Offset`은 0이며 실제 Geometry는 원본 heightfield에 그대로 정합됩니다.
- output MeshRenderer에 사용자 Material이 없으면 Backend가 패키지 내장 depth-bias Material을
  자동으로 사용해 Terrain LOD와의 깊이 충돌만 보정합니다. 사용자 Material을 지정하면 교체하지 않습니다.

Controller Inspector의 `Baked Tile Count`가 0보다 크고 Play Mode 진입·종료 중 예외가 없는지
확인합니다. Terrain hole은 traversal 경계이므로 hole을 가로질러 Grid가 연결되지 않아야 합니다.
`Clear Baked Tiles`는 Edit Mode의 직렬화 Tile과 Preview Mesh를 제거하고 0개 상태를
유지합니다. Surface 형태나 seed를 수정한 뒤 `Rebuild Tiles`로 다시 생성합니다.
