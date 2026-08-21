namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>GameObject에 대한 Adapter 선택 결과입니다.</summary>
    public enum SurfaceAdapterResolution
    {
        /// <summary>Adapter를 하나로 확정했습니다.</summary>
        Resolved,
        /// <summary>이 GameObject를 다룰 수 있는 factory가 없습니다.</summary>
        NoAdapterFound,
        /// <summary>
        /// 같은 우선순위의 factory가 둘 이상 매칭됐습니다. 임의로 고르면 Scene 구성에 따라 결과가
        /// 달라지므로 선택하지 않고 호출자에게 알립니다.
        /// </summary>
        AmbiguousCandidates
    }
}
