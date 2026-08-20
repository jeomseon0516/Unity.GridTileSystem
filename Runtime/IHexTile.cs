using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jeomseon.Unity.GridTileSystem
{
    public interface IHexTile
    {
        IReadOnlyList<string> Properties { get; }
        bool IsActive { get; set; }
        Color Color { get; set; }
        
        HexCoordinates Coordinates { get; }
        Vector3 TilePosition { get; }
        Vector2 NormalizedPosition { get; }
        
        event UnityAction<IHexTile> OnEnterTile;
        event UnityAction<IHexTile> OnExitTile;
        event UnityAction<IHexTile> OnMouseDownTile;
        event UnityAction<IHexTile> OnMouseUpTile;

        event UnityAction<IHexTile, bool> OnChangedActive;
        event UnityAction<IHexTile, Color> OnChangedColor;
        
        void AddProperty(string property);
        bool RemoveProperty(string property);
    }

}
