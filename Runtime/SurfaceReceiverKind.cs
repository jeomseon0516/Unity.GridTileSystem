namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>Receiver가 topology를 구축할 Unity Surface 입력 종류입니다.</summary>
    public enum SurfaceReceiverKind
    {
        /// <summary>readable MeshFilter와 같은 Mesh의 MeshCollider를 사용합니다.</summary>
        StaticMesh,
        /// <summary>TerrainData 계산형 topology와 TerrainCollider를 사용합니다.</summary>
        Terrain,
        /// <summary>
        /// SkinnedMeshRenderer의 bind pose Mesh로 topology를 만들고 bone 가중치로 변형을 따라갑니다.
        /// topology와 Tile 구성은 bind pose 기준으로 고정되며 매 프레임 vertex 위치만 갱신됩니다.
        /// </summary>
        SkinnedMesh
    }
}
