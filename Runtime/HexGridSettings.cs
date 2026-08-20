using System;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem
{
    [CreateAssetMenu(fileName = "HexGridSettings", menuName = "Jeomseon/Grid Tile System/Hex Grid Settings")]
    public sealed class HexGridSettings : ScriptableObject
    {
        public const float HexagonRadiusMin = 0.025f;
        public const float HexagonRadiusMax = 0.5f;

        [SerializeField, Range(HexagonRadiusMin, HexagonRadiusMax)] private float hexagonRadius = 0.025f;
        [SerializeField, Min(0)] private int tileLimit = 3;
        [SerializeField] private LayerMask layerMask;

        public event Action SettingsChanged;

        public float HexagonRadius
        {
            get => hexagonRadius;
            set
            {
                hexagonRadius = value;
                SettingsChanged?.Invoke();
            }
        }

        public int TileLimit
        {
            get => tileLimit;
            set
            {
                tileLimit = value;
                SettingsChanged?.Invoke();
            }
        }

        public LayerMask LayerMask
        {
            get => layerMask;
            set => layerMask = value;
        }

        #if UNITY_EDITOR
        private void OnValidate() => SettingsChanged?.Invoke();
        #endif
    }
}
