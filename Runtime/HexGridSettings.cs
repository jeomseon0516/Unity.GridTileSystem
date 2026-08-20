using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.GridTileSystem
{
    [CreateAssetMenu(fileName = "HexGridSettings", menuName = "Jeomseon/Grid Tile System/Hex Grid Settings")]
    public sealed class HexGridSettings : ScriptableObject
    {
        public const float TileRadiusMin = 0.025f;
        public const float TileRadiusMax = 0.5f;

        [SerializeField, Range(TileRadiusMin, TileRadiusMax), FormerlySerializedAs("hexagonRadius")]
        private float tileRadius = 0.025f;
        [SerializeField, Min(0), FormerlySerializedAs("tileLimit")] private int gridRadius = 3;
        [SerializeField, FormerlySerializedAs("layerMask")] private LayerMask interactionLayerMask;

        public event Action SettingsChanged;

        public float TileRadius
        {
            get => tileRadius;
            set
            {
                tileRadius = value;
                SettingsChanged?.Invoke();
            }
        }

        public int GridRadius
        {
            get => gridRadius;
            set
            {
                gridRadius = value;
                SettingsChanged?.Invoke();
            }
        }

        public LayerMask InteractionLayerMask
        {
            get => interactionLayerMask;
            set
            {
                interactionLayerMask = value;
                SettingsChanged?.Invoke();
            }
        }

        #if UNITY_EDITOR
        private void OnValidate() => SettingsChanged?.Invoke();
        #endif
    }
}
