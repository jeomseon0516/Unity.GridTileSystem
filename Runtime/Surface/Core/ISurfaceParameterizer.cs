namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Surface seed와 성장 설정을 하나 이상의 local chart로 변환합니다. Grid와 렌더링은 Triangle
    /// Unfolding 또는 ExpMap의 구체 수학 대신 이 출력 계약만 소비합니다.
    /// </summary>
    public interface ISurfaceParameterizer
    {
        /// <summary>Seed 주변 Surface를 parameterize하여 Patch 집합을 만듭니다.</summary>
        SurfacePatchSet Parameterize(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            in SurfacePatchBuildSettings settings,
            ISurfaceConnectivity connectivity);
    }
}
