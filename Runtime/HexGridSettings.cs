using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.GridTileSystem
{
    [CreateAssetMenu(fileName = "HexGridSettings", menuName = "Jeomseon/Grid Tile System/Hex Grid Settings")]
    public sealed class HexGridSettings : ScriptableObject
    {
        /// <summary>Inspector에서 허용하는 최소 intrinsic Tile 반지름입니다.</summary>
        public const float TileRadiusMin = 0.025f;
        /// <summary>Hex 중심에서 꼭짓점까지의 intrinsic Surface 길이입니다.</summary>
        [SerializeField, Min(TileRadiusMin), FormerlySerializedAs("hexagonRadius")]
        private float tileRadius = 0.025f;
        /// <summary>Seed Tile 주변에 생성할 Hex ring 개수입니다.</summary>
        [SerializeField, Min(0), FormerlySerializedAs("tileLimit")] private int gridRadius = 3;
        /// <summary>Surface picking Physics Raycast에 사용할 layer mask입니다.</summary>
        [SerializeField, FormerlySerializedAs("layerMask")] private LayerMask interactionLayerMask;

        /// <summary>직렬화 설정 중 하나가 변경됐을 때 발생합니다.</summary>
        public event Action SettingsChanged;

        /// <summary>intrinsic Tile 반지름을 가져오거나 설정하고 변경 이벤트를 발생시킵니다.</summary>
        public float TileRadius
        {
            get => tileRadius;
            set
            {
                float clamped = Mathf.Max(TileRadiusMin, value);
                if (Mathf.Approximately(tileRadius, clamped)) return;
                tileRadius = clamped;
                SettingsChanged?.Invoke();
            }
        }

        /// <summary>Hex Grid ring 반경을 가져오거나 설정하고 변경 이벤트를 발생시킵니다.</summary>
        public int GridRadius
        {
            get => gridRadius;
            set
            {
                int clamped = Mathf.Max(0, value);
                if (gridRadius == clamped) return;
                gridRadius = clamped;
                SettingsChanged?.Invoke();
            }
        }

        /// <summary>Surface interaction layer mask를 가져오거나 설정하고 변경 이벤트를 발생시킵니다.</summary>
        public LayerMask InteractionLayerMask
        {
            get => interactionLayerMask;
            set
            {
                if (interactionLayerMask == value) return;
                interactionLayerMask = value;
                SettingsChanged?.Invoke();
            }
        }

        #if UNITY_EDITOR
        /// <summary>Editor Inspector 변경을 runtime과 같은 설정 변경 이벤트로 전달합니다.</summary>
        private void OnValidate()
        {
            tileRadius = Mathf.Max(TileRadiusMin, tileRadius);
            gridRadius = Mathf.Max(0, gridRadius);
            SettingsChanged?.Invoke();
        }
        #endif
    }
}
