namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Surface boundary Edge 너머에 이어지는 다른 Surface가 있는지 답합니다. Grid는 실제로 경계에
    /// 도달했을 때만 질의하므로 월드 전체를 미리 연결해 둘 필요가 없습니다.
    /// </summary>
    /// <remarks>
    /// 구현은 "가까우면 연결됨"으로 판정해서는 안 됩니다. 위치 일치와 법선 정합을 모두 요구해야
    /// 우연히 스쳐 지나가는 무관한 표면이 Grid에 딸려 들어오지 않습니다.
    /// </remarks>
    public interface ISurfaceConnectivity
    {
        /// <summary>지정한 boundary Edge 너머로 이어지는 Surface Edge를 찾습니다.</summary>
        bool TryGetLink(SurfaceHandle surface, int triangleIndex, int edge, out SurfaceLink link);
    }
}
