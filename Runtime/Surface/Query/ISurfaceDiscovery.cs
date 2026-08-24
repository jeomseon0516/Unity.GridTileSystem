using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Query
{
    /// <summary>
    /// 월드의 지정한 위치 주변에서 Surface를 발견해 Adapter와 topology를 준비합니다. Seed 탐색과
    /// 경계 연결 탐색이 같은 발견 계층을 공유하므로 등록 없는 모델이 두 곳에서 일관되게 유지됩니다.
    /// </summary>
    public interface ISurfaceDiscovery
    {
        /// <summary>지정한 위치 주변의 Surface Adapter를 준비해 채우고 개수를 반환합니다.</summary>
        int Discover(in Vector3 worldPosition, float radius, LayerMask layerMask, List<ISurfaceAdapter> results);

        /// <summary>이미 발견된 Surface의 Adapter를 찾습니다.</summary>
        bool TryGetAdapter(SurfaceHandle surface, out ISurfaceAdapter adapter);
    }
}
