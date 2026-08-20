using Jeomseon.Unity.Projector;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexTilePicker : IHexTilePicker
    {
        private const float SquareRootThree = 1.732051f;
        private const float SquareRootThreeDivideThree = SquareRootThree / 3;
        private const float TwoFDivideThree = 2f / 3;
        private const float NegativeOneFDivideThree = -1f / 3;

        private readonly MeshProjector _projector;
        private readonly IHexTileStore _tileData;

        public HexTilePicker(MeshProjector projector, IHexTileStore tileData)
        {
            _projector = projector;
            _tileData = tileData;
        }

        public bool TryPick(in Ray ray, LayerMask layerMask, float tileRadius, out (bool, RaycastHit) hitTuple, out HexTile tile)
        {
            tile = null;
            hitTuple.Item1 = false;

            if (Physics.Raycast(ray, out hitTuple.Item2, Mathf.Infinity, layerMask))
            {
                hitTuple.Item1 = true;

                Vector3 localPosition = _projector.transform.InverseTransformPoint(hitTuple.Item2.point);
                Vector2 convertedPosition = new(
                    localPosition.x / _projector.Size.x,
                    localPosition.y / _projector.Size.y);

                Vector2 axialCoordinates = new(
                    TwoFDivideThree * convertedPosition.x / tileRadius,
                    (NegativeOneFDivideThree * convertedPosition.x + SquareRootThreeDivideThree * convertedPosition.y) / tileRadius);

                HexCoordinates hexCoordinates = HexCoordinates.Round(axialCoordinates);

                if (_tileData.TryGetTile(hexCoordinates, out HexTile hex))
                {
                    tile = hex;
                    return hex.IsActive;
                }
            }

            return false;
        }
    }
}
