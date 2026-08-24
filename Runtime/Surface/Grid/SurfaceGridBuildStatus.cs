namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>
    /// Grid 생성 시도의 결과 구분입니다. 실패를 조용히 넘기지 않고 어느 단계에서 멈췄는지 알립니다.
    /// </summary>
    public enum SurfaceGridBuildStatus
    {
        /// <summary>완전한 Tile을 하나 이상 가진 Grid를 만들었습니다.</summary>
        Success = 0,
        /// <summary>Seed 주변에서 지원 가능한 표면을 찾지 못했습니다.</summary>
        SurfaceNotFound,
        /// <summary>초기 방향이 seed 표면의 법선과 나란해 격자 방향을 정의할 수 없습니다.</summary>
        InvalidInitialDirection,
        /// <summary>표면은 찾았지만 chart 또는 Region 구축이 실패했습니다.</summary>
        BuildFailed,
        /// <summary>Grid는 만들었지만 표면에 완전히 들어가는 Tile이 하나도 없습니다.</summary>
        NoCompleteTiles,
    }
}
