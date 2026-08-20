using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexGridTilePicker : IHexGridTilePicker
    {
        private const float SquareRootThree = 1.732051f;
        private const float SquareRootThreeDivideThree = SquareRootThree / 3;
        private const float TwoFDivideThree = 2f / 3;
        private const float NegativeOneFDivideThree = -1f / 3;

        private readonly DecalProjector _decalProjector;
        private readonly IHexGridTileDataStore _tileData;

        public HexGridTilePicker(DecalProjector decalProjector, IHexGridTileDataStore tileData)
        {
            _decalProjector = decalProjector;
            _tileData = tileData;
        }

        public bool TryPick(in Ray ray, LayerMask layerMask, float hexagonRadius, out (bool, RaycastHit) hitTuple, out HexGrid hexGrid)
        {
            hexGrid = null;
            hitTuple.Item1 = false;

            if (Physics.Raycast(ray, out hitTuple.Item2, Mathf.Infinity, layerMask))
            {
                hitTuple.Item1 = true;

                Vector2 convertedPosition = new(
                    hitTuple.Item2.point.x - _decalProjector.transform.position.x,
                    hitTuple.Item2.point.z - _decalProjector.transform.position.z);

                convertedPosition /= _decalProjector.size.x * _decalProjector.transform.localScale.x;

                Vector2 axialCoordinates = new(
                    TwoFDivideThree * convertedPosition.x / hexagonRadius,
                    (NegativeOneFDivideThree * convertedPosition.x + SquareRootThreeDivideThree * convertedPosition.y) / hexagonRadius);

                HexCoordinates hexCoordinates = HexCoordinates.Round(axialCoordinates);

                if (_tileData.TryGetTile(hexCoordinates, out HexGrid hex))
                {
                    hexGrid = hex;
                    return hex.IsActive;
                }
            }

            return false;
        }
    }
}
