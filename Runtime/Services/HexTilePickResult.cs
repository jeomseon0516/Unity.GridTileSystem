using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    /// <summary>Physics 교차 정보와 그 위치의 활성 Hex Tile을 함께 반환하는 picking 결과입니다.</summary>
    public readonly struct HexTilePickResult
    {
        public RaycastHit Hit { get; }
        public HexTile Tile { get; }

        internal HexTilePickResult(in RaycastHit hit, HexTile tile)
        {
            Hit = hit;
            Tile = tile;
        }
    }
}
