using UnityEngine;

namespace Jeomseon.HexGrid.Samples
{
    public sealed class GridTileSystemSample : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;

        private void Start()
        {
            if (_gridManager == null)
            {
                return;
            }

            Debug.Log($"생성된 육각 타일 수: {_gridManager.GetGrids().Count}");
            _gridManager.OnEnterTile += HandleEnterTile;
        }

        private static void HandleEnterTile(IHexGrid tile)
        {
            Debug.Log($"진입한 타일: {tile.HexPoint}");
        }
    }
}
