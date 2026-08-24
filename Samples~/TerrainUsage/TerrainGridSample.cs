using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Samples.TerrainUsage
{
    /// <summary>Terrain virtual topology Grid에 checker 색상을 적용하고 생성 결과를 기록합니다.</summary>
    public sealed class TerrainGridSample : MonoBehaviour
    {
        [SerializeField] private HexGridController gridController;
        private Material _originalMaterial;
        private Material _sampleMaterial;

        /// <summary>Terrain Grid의 시각 상태를 설정하고 생성 개수를 기록합니다.</summary>
        private void Start()
        {
            if (gridController == null) return;
            MeshRenderer outputRenderer = GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (outputRenderer != null && shader != null)
            {
                _originalMaterial = outputRenderer.sharedMaterial;
                _sampleMaterial = new Material(shader) { name = "Terrain Grid Sample Vertex Color (Runtime)" };
                outputRenderer.sharedMaterial = _sampleMaterial;
            }
            Color even = new(0.95f, 0.55f, 0.12f, 0.85f);
            Color odd = new(0.95f, 0.85f, 0.2f, 0.85f);
            foreach (HexTile tile in gridController.Tiles)
            {
                int parity = (tile.Coordinates.Q - tile.Coordinates.R) & 1;
                tile.Data.Color = parity == 0 ? even : odd;
            }
            gridController.RefreshRendering();
            Debug.Log($"Generated Terrain hex tile count: {gridController.TileCount}");
        }

        /// <summary>Sample이 생성한 runtime Material을 해제합니다.</summary>
        private void OnDestroy()
        {
            MeshRenderer outputRenderer = GetComponent<MeshRenderer>();
            if (outputRenderer != null && outputRenderer.sharedMaterial == _sampleMaterial)
                outputRenderer.sharedMaterial = _originalMaterial;
            if (_sampleMaterial != null) Destroy(_sampleMaterial);
        }
    }
}
