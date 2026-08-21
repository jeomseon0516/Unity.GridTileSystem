using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexTilePicker : IHexTilePicker
    {
        /// <summary>Physics hit을 intrinsic Logical Tile로 변환하는 저수준 picker입니다.</summary>
        private readonly SurfaceGridPicker _surfacePicker;
        /// <summary>Logical 좌표에 대응하는 사용자 Tile 상태 저장소입니다.</summary>
        private readonly IHexTileStore _tileData;

        /// <summary>Surface identity picker와 사용자 Tile 저장소를 결합합니다.</summary>
        public HexTilePicker(
            Collider surfaceCollider,
            SurfaceTopology topology,
            SurfaceGrid grid,
            IHexTileStore tileData)
        {
            _surfacePicker = new SurfaceGridPicker(surfaceCollider, topology, grid);
            _tileData = tileData;
        }

        /// <summary>Ray가 활성 Logical Tile과 교차하면 사용자 Tile 상태를 반환합니다.</summary>
        public bool TryPick(in Ray ray, LayerMask layerMask, out (bool, RaycastHit) hitTuple, out HexTile tile)
        {
            tile = null;
            bool found = _surfacePicker.TryPick(ray, layerMask, out RaycastHit hit, out SurfaceGridTileRegion region);
            hitTuple = (hit.collider != null, hit);
            if (found && _tileData.TryGetTile(region.Coordinates, out HexTile foundTile))
            {
                tile = foundTile;
                return foundTile.IsActive;
            }
            return false;
        }
    }
}
