using UnityEngine;
using UnityEngine.Serialization;
using Jeomseon.Unity.GridTileSystem;

namespace Jeomseon.HexGrid.Samples
{
    public sealed class GridTileSystemSample : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_gridManager")] private GridManager gridManager;

        private void Start()
        {
            if (gridManager == null)
            {
                return;
            }

            Debug.Log($"생성된 육각 타일 수: {gridManager.GetGrids().Count}");
            gridManager.OnEnterTile += HandleEnterTile;
        }

        private static void HandleEnterTile(IHexGrid tile)
        {
            Debug.Log($"진입한 타일: {tile.HexPoint}");
        }
    }
}
