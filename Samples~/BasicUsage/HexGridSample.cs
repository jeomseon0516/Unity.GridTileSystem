using UnityEngine;
using UnityEngine.Serialization;
using Jeomseon.Unity.GridTileSystem;

namespace Jeomseon.Unity.GridTileSystem.Samples.BasicUsage
{
    public sealed class HexGridSample : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("gridManager"), FormerlySerializedAs("_gridManager")]
        private HexGridController gridController;

        private void Start()
        {
            if (gridController == null)
            {
                return;
            }

            Debug.Log($"Generated hex tile count: {gridController.Tiles.Count}");
            gridController.OnEnterTile += HandleEnterTile;
        }

        private void OnDestroy()
        {
            if (gridController != null)
            {
                gridController.OnEnterTile -= HandleEnterTile;
            }
        }

        private static void HandleEnterTile(IHexTile tile)
        {
            Debug.Log($"Entered tile: {tile.Coordinates}");
        }
    }
}
