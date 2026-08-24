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
        /// <summary>Sample 종료 시 복원할 사용자의 원래 output Material입니다.</summary>
        private Material _originalMaterial;
        /// <summary>vertex color를 눈으로 확인하기 위해 Sample이 생성하고 파괴하는 runtime Material입니다.</summary>
        private Material _sampleMaterial;

        /// <summary>Controller의 초기 Bake 결과를 기록하고 네 가지 Tile 상호작용 이벤트를 구독합니다.</summary>
        private void Start()
        {
            if (gridController == null)
            {
                return;
            }

            ConfigureSampleMaterial();
            ApplyCheckerColors();
            Debug.Log($"Generated hex tile count: {gridController.TileCount}");
            gridController.OnEnterTile += HandleEnterTile;
            gridController.OnExitTile += HandleExitTile;
            gridController.OnMouseDownTile += HandleMouseDownTile;
            gridController.OnMouseUpTile += HandleMouseUpTile;
        }

        /// <summary>Scene 종료 시 Sample이 추가한 Tile 상호작용 구독을 대칭 해제합니다.</summary>
        private void OnDestroy()
        {
            if (gridController != null)
            {
                gridController.OnEnterTile -= HandleEnterTile;
                gridController.OnExitTile -= HandleExitTile;
                gridController.OnMouseDownTile -= HandleMouseDownTile;
                gridController.OnMouseUpTile -= HandleMouseUpTile;
            }
            MeshRenderer outputRenderer = GetComponent<MeshRenderer>();
            if (outputRenderer != null && outputRenderer.sharedMaterial == _sampleMaterial)
                outputRenderer.sharedMaterial = _originalMaterial;
            if (_sampleMaterial != null) Destroy(_sampleMaterial);
        }

        /// <summary>
        /// 별도 Shader asset이나 Render Pipeline 패키지 없이 Unity 기본 vertex-color Shader를 Sample에
        /// 연결합니다. Shader가 제거된 Player 구성에서는 원래 Material을 유지하고 경고만 남깁니다.
        /// </summary>
        private void ConfigureSampleMaterial()
        {
            MeshRenderer outputRenderer = GetComponent<MeshRenderer>();
            Shader vertexColorShader = Shader.Find("Sprites/Default");
            if (outputRenderer == null || vertexColorShader == null)
            {
                Debug.LogWarning("The sample could not find MeshRenderer or the built-in Sprites/Default shader.", this);
                return;
            }

            _originalMaterial = outputRenderer.sharedMaterial;
            _sampleMaterial = new Material(vertexColorShader) { name = "Grid Tile Sample Vertex Color (Runtime)" };
            outputRenderer.sharedMaterial = _sampleMaterial;
        }

        /// <summary>
        /// Logical 좌표 parity로 두 색을 교차 적용합니다. 순수 데이터를 직접 일괄 수정한 뒤 Geometry를
        /// 다시 만들지 않고 visual snapshot만 한 번 갱신해 Backend 분리 계약을 보여줍니다.
        /// </summary>
        private void ApplyCheckerColors()
        {
            Color even = new(0.08f, 0.65f, 1f, 0.82f);
            Color odd = new(0.12f, 0.95f, 0.68f, 0.82f);
            foreach (HexTile tile in gridController.Tiles)
            {
                int parity = (tile.Coordinates.Q - tile.Coordinates.R) & 1;
                tile.Data.Color = parity == 0 ? even : odd;
            }
            gridController.RefreshRendering();
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
