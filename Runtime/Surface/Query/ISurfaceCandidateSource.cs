using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Query
{
    /// <summary>
    /// 주어진 반경 안에서 Surface가 될 수 있는 GameObject 후보를 모읍니다. 사용자가 Surface를
    /// 등록하지 않으므로 이 계층이 월드에서 직접 찾아냅니다.
    /// </summary>
    /// <remarks>
    /// 매 호출마다 Scene 전체를 훑는 구현은 사용하지 않습니다. 기본 구현은 Unity가 이미 유지하는
    /// Physics 공간 인덱스를 재사용하며, 필요하면 캐시된 geometry 인덱스로 교체할 수 있습니다.
    /// </remarks>
    public interface ISurfaceCandidateSource
    {
        /// <summary>후보를 <paramref name="results"/>에 채우고 개수를 반환합니다.</summary>
        int Collect(in Vector3 center, float radius, LayerMask layerMask, List<GameObject> results);
    }
}
