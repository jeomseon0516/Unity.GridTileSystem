using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    /// <summary>논리 타일 목록, 좌표 검색, 재Bake 상태 보존을 제공하는 저장소 계약입니다.</summary>
    public interface IHexTileStore
    {
        /// <summary>현재 타일 목록을 가져옵니다.</summary>
        IReadOnlyList<HexTile> Tiles { get; }

        /// <summary>Backend에 적용할 타일별 시각 상태가 변경될 때 발생합니다.</summary>
        event Action TileVisualsChanged;

        /// <summary>논리 좌표에 대응하는 타일을 찾습니다.</summary>
        bool TryGetTile(in HexCoordinates coordinates, out HexTile tile);
        /// <summary>좌표가 존재하면 활성 상태를 변경합니다.</summary>
        void SetActive(in AxialCoordinates coordinates, bool isActive);
        /// <summary>Surface Grid에서 목록을 재구성하고 같은 좌표의 사용자 상태를 보존합니다.</summary>
        void Bake(SurfaceTopology topology, SurfaceGrid grid, Transform surfaceTransform);
        /// <summary>타일과 lookup을 모두 비웁니다.</summary>
        void Clear();
        /// <summary>역직렬화된 목록에서 lookup과 이벤트 연결을 복원합니다.</summary>
        void RebuildLookup();
    }
}
