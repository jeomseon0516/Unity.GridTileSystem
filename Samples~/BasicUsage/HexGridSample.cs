using UnityEngine;
using UnityEngine.Serialization;
using Jeomseon.Unity.GridTileSystem;

namespace Jeomseon.Unity.GridTileSystem.Samples.BasicUsage
{
    /// <summary>생성된 intrinsic Grid 개수와 pointer 진입 좌표를 Console에서 확인하는 예제입니다.</summary>
    public sealed class HexGridSample : MonoBehaviour
    {
        /// <summary>Sample Scene에서 topology, Logical Tile 및 기본 Mesh Backend를 조립하는 Controller입니다.</summary>
        [SerializeField, FormerlySerializedAs("gridManager"), FormerlySerializedAs("_gridManager")]
        private HexGridController gridController;
        /// <summary>첫 Controller Update보다 먼저 네 가지 Tile 상호작용 이벤트를 구독합니다.</summary>
        private void OnEnable()
        {
            if (gridController == null) return;
            gridController.OnEnterTile += HandleEnterTile;
            gridController.OnExitTile += HandleExitTile;
            gridController.OnMouseDownTile += HandleMouseDownTile;
            gridController.OnMouseUpTile += HandleMouseUpTile;
        }

        /// <summary>Controller의 초기 Bake 결과를 기록합니다.</summary>
        private void Start()
        {
            if (gridController == null) return;
            if (Application.isPlaying) Debug.Log($"Generated hex tile count: {gridController.TileCount}");
        }

        /// <summary>비활성화 시 Sample이 추가한 Tile 상호작용 구독을 대칭 해제합니다.</summary>
        private void OnDisable()
        {
            if (gridController != null)
            {
                gridController.OnEnterTile -= HandleEnterTile;
                gridController.OnExitTile -= HandleExitTile;
                gridController.OnMouseDownTile -= HandleMouseDownTile;
                gridController.OnMouseUpTile -= HandleMouseUpTile;
            }
        }

        /// <summary>Pointer가 새 Logical Tile에 진입했을 때 Surface와 독립적인 Cube 좌표를 기록합니다.</summary>
        private static void HandleEnterTile(IHexTile tile)
        {
            Debug.Log($"Entered tile: {tile.Coordinates}");
        }

        /// <summary>
        /// Pointer가 Logical Tile에서 벗어났을 때 좌표를 기록합니다. 타일 사이를 곧바로 이동하면
        /// 새 타일의 Enter가 먼저, 이전 타일의 Exit가 그다음 순서로 출력됩니다.
        /// </summary>
        private static void HandleExitTile(IHexTile tile)
        {
            Debug.Log($"Exited tile: {tile.Coordinates}");
        }

        /// <summary>활성 Tile 위에서 pointer down이 발생했을 때 좌표를 기록합니다.</summary>
        private static void HandleMouseDownTile(IHexTile tile)
        {
            Debug.Log($"Pointer down on tile: {tile.Coordinates}");
        }

        /// <summary>활성 Tile 위에서 pointer up이 발생했을 때 좌표를 기록합니다.</summary>
        private static void HandleMouseUpTile(IHexTile tile)
        {
            Debug.Log($"Pointer up on tile: {tile.Coordinates}");
        }
    }
}
