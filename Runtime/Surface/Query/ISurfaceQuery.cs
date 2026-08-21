using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Query
{
    /// <summary>
    /// 월드 위치에서 Grid를 시작할 Surface 지점을 찾습니다. 사용자는 Surface를 등록하거나 지정하지
    /// 않으며, 이 계층이 seed 주변에서 지원 가능한 표면을 스스로 발견합니다.
    /// </summary>
    /// <remarks>
    /// 내부에서 무엇을 쓰는지는 구현 세부사항입니다. Physics 질의, 캐시된 Scene 인덱스, 공간 해시,
    /// BVH 중 무엇으로 바꾸어도 이 계약과 사용자 API는 바뀌지 않습니다.
    /// </remarks>
    public interface ISurfaceQuery
    {
        /// <summary>월드 위치 주변에서 Grid seed로 쓸 Surface 지점을 찾습니다.</summary>
        bool TryFindSeed(in Vector3 worldPosition, in SurfaceQueryOptions options, out SurfaceQueryHit hit);
    }
}
