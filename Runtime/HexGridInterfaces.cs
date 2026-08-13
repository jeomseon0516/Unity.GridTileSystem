using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jeomseon.Unity.GridTileSystem
{
    public interface IHexGrid
    {
        IReadOnlyList<string> Properties { get; }
        bool IsActive { get; set; }
        Color Color { get; set; }
        
        HexCoordinates HexPoint { get; }
        Vector3 TilePosition { get; }
        Vector2 NormalizedPosition { get; }
        
        event UnityAction<IHexGrid> OnEnterTile;
        event UnityAction<IHexGrid> OnExitTile;
        event UnityAction<IHexGrid> OnMouseDownTile;
        event UnityAction<IHexGrid> OnMouseUpTile;

        event UnityAction<IHexGrid, bool> OnChangedActive;
        event UnityAction<IHexGrid, Color> OnChangedColor;
        
        void AddProperty(string property);
        bool RemoveProperty(string property);
    }

}

