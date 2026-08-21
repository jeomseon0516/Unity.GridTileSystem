namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Surface identity로 topology를 되찾는 경계입니다. Grid 생성기는 Surface **목록**을 받지 않고,
    /// chart를 펼치는 도중 필요한 Surface만 handle로 조회합니다. 이 간접 참조가 하나의 chart가 여러
    /// Surface에 걸치는 것을 가능하게 하며, 동시에 Grid가 "어떤 Surface들이 존재하는지" 모르게 합니다.
    /// </summary>
    public interface ISurfaceProvider
    {
        /// <summary>해당 handle의 topology를 찾으면 true를 반환합니다.</summary>
        bool TryGetTopology(SurfaceHandle handle, out SurfaceTopology topology);
    }
}
