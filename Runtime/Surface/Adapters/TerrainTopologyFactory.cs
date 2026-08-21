using System;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>Unity TerrainData를 배열 복제 없는 virtual Surface topology로 변환합니다.</summary>
    public static class TerrainTopologyFactory
    {
        /// <summary>지정한 Surface identity로 heightfield 계산 view를 만듭니다.</summary>
        public static TerrainSurfaceTopology BuildTopology(TerrainData terrainData, Surface.Core.SurfaceHandle handle)
        {
            if (terrainData == null) throw new ArgumentNullException(nameof(terrainData));
            return new TerrainSurfaceTopology(handle, terrainData);
        }

        /// <summary>
        /// TerrainData asset identity에서 handle을 유도합니다. 같은 TerrainData를 공유하는 여러
        /// 인스턴스가 같은 handle을 받게 되므로, Registry 등록 경로에서는 handle을 받는 overload를
        /// 사용합니다.
        /// </summary>
        public static TerrainSurfaceTopology BuildTopology(TerrainData terrainData)
        {
            if (terrainData == null) throw new ArgumentNullException(nameof(terrainData));
            ulong handleValue = EntityId.ToULong(terrainData.GetEntityId());
            if (handleValue == 0UL) handleValue = 1UL;
            return BuildTopology(terrainData, new Surface.Core.SurfaceHandle(handleValue));
        }
    }
}
