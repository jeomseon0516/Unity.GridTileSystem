using System;
using Jeomseon.Unity.Attributes;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Query;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.GridTileSystem
{
    [CreateAssetMenu(fileName = "HexGridSettings", menuName = "Jeomseon/Grid Tile System/Hex Grid Settings")]
    public sealed class HexGridSettings : ScriptableObject
    {
        /// <summary>Inspector에서 허용하는 최소 intrinsic Tile 반지름입니다.</summary>
        public const float TileRadiusMin = 0.025f;
        /// <summary>
        /// Hex 중심에서 꼭짓점까지의 intrinsic Surface 길이입니다. Grid는 표면 전체를 덮으므로 이 값이
        /// Tile 해상도를 정하는 유일한 설정이며, 작을수록 더 많은 Tile이 생성됩니다.
        /// </summary>
        [SerializeField, Min(TileRadiusMin), FormerlySerializedAs("hexagonRadius")]
        private float tileRadius = 0.025f;
        /// <summary>Surface picking Physics Raycast에 사용할 layer mask입니다.</summary>
        [SerializeField, FormerlySerializedAs("layerMask")] private LayerMask interactionLayerMask;

        [Header("Surface discovery")]
        [SerializeField, Min(0.001f)] private float seedSearchRadius = SurfaceQueryOptions.DefaultSearchRadius;
        [SerializeField] private LayerMask surfaceLayerMask = ~0;
        [SerializeField] private Vector3 preferredSurfaceDirection = Vector3.down;

        [Header("Patch limits")]
        [SerializeField] private bool splitPatchWhenLimitReached = true;
        [SerializeField, Min(1)] private int maximumPatchTriangles = 4096;
        [SerializeField, Min(0.001f)] private float maximumPatchRadius = 100f;
        [SerializeField, Min(0.000001f)] private float maximumClosureError = 0.01f;

        [Header("Rendering")]
        [SerializeField, SerializeReference, SerializeReferenceSelector]
        private IHexTileDrawPolicy defaultDrawPolicy;

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
                RaiseSettingsChanged();
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
                RaiseSettingsChanged();
            }
        }

        public float SeedSearchRadius
        {
            get => seedSearchRadius;
            set => SetFloat(ref seedSearchRadius, Mathf.Max(0.001f, value));
        }

        public LayerMask SurfaceLayerMask
        {
            get => surfaceLayerMask;
            set
            {
                if (surfaceLayerMask == value) return;
                surfaceLayerMask = value;
                RaiseSettingsChanged();
            }
        }

        public Vector3 PreferredSurfaceDirection
        {
            get => preferredSurfaceDirection;
            set
            {
                if (preferredSurfaceDirection == value) return;
                preferredSurfaceDirection = value;
                RaiseSettingsChanged();
            }
        }

        public bool SplitPatchWhenLimitReached
        {
            get => splitPatchWhenLimitReached;
            set
            {
                if (splitPatchWhenLimitReached == value) return;
                splitPatchWhenLimitReached = value;
                RaiseSettingsChanged();
            }
        }

        public int MaximumPatchTriangles
        {
            get => maximumPatchTriangles;
            set
            {
                int clamped = Mathf.Max(1, value);
                if (maximumPatchTriangles == clamped) return;
                maximumPatchTriangles = clamped;
                RaiseSettingsChanged();
            }
        }

        public float MaximumPatchRadius
        {
            get => maximumPatchRadius;
            set => SetFloat(ref maximumPatchRadius, Mathf.Max(0.001f, value));
        }

        public float MaximumClosureError
        {
            get => maximumClosureError;
            set => SetFloat(ref maximumClosureError, Mathf.Max(0.000001f, value));
        }

        /// <summary>
        /// 개별 Tile이 override하지 않는 한 적용할 기본 Draw Policy를 가져오거나 설정합니다.
        /// <c>null</c>이면 Fill로 그립니다.
        /// </summary>
        public IHexTileDrawPolicy DefaultDrawPolicy
        {
            get => defaultDrawPolicy;
            set
            {
                if (ReferenceEquals(defaultDrawPolicy, value)) return;
                defaultDrawPolicy = value;
                RaiseSettingsChanged();
            }
        }

        public SurfaceQueryOptions QueryOptions =>
            new(seedSearchRadius, preferredSurfaceDirection, surfaceLayerMask);

        public SurfacePatchBuildSettings PatchBuildSettings =>
            new(maximumPatchTriangles, maximumPatchRadius, maximumClosureError, splitPatchWhenLimitReached);

        private void SetFloat(ref float field, float value)
        {
            if (Mathf.Approximately(field, value)) return;
            field = value;
            RaiseSettingsChanged();
        }

        /// <summary>
        /// 설정 변경을 구독자에게 알립니다. 일반 runtime setter는 즉시 알리지만, Editor에서
        /// <see cref="OnValidate"/> 지연 알림이 이미 예약됐다면 같은 tick의 setter 변경도 그 알림에
        /// 합쳐 Mesh 파괴·재생성이 OnValidate 스택에서 실행되지 않게 합니다.
        /// </summary>
        private void RaiseSettingsChanged()
        {
#if UNITY_EDITOR
            // OnValidate가 이미 안전한 다음 Editor tick 알림을 예약했다면, 같은 tick의 setter 변경도
            // 그 알림에 합칩니다. 여기서 즉시 알리면 Controller가 OnValidate 처리 도중 runtime Mesh를
            // Dispose/Rebake할 수 있고, 예약된 알림으로 같은 Bake가 한 번 더 실행됩니다.
            if (_settingsChangedQueued) return;
#endif
            SettingsChanged?.Invoke();
        }

#if UNITY_EDITOR
        private bool _settingsChangedQueued;

        private void QueueSettingsChanged()
        {
            if (_settingsChangedQueued) return;
            _settingsChangedQueued = true;
            UnityEditor.EditorApplication.delayCall += RaiseQueuedSettingsChanged;
        }

        private void RaiseQueuedSettingsChanged()
        {
            UnityEditor.EditorApplication.delayCall -= RaiseQueuedSettingsChanged;
            _settingsChangedQueued = false;
            if (this == null) return;
            SettingsChanged?.Invoke();
        }

        private void OnDisable()
        {
            if (!_settingsChangedQueued) return;
            UnityEditor.EditorApplication.delayCall -= RaiseQueuedSettingsChanged;
            _settingsChangedQueued = false;
        }

        /// <summary>
        /// Editor Inspector 변경을 설정 변경 이벤트로 전달합니다. Unity는 OnValidate 호출 스택 안에서
        /// 파괴 계열 API(DestroyImmediate 등) 호출을 허용하지 않으므로, 이 콜백에서 발생한 알림만
        /// 다음 Editor tick으로 미뤄 구독자(Controller의 Bake)가 그 스택 밖에서 안전하게 실행되게
        /// 합니다. 이 콜백이 아닌 경로로 온 setter 호출은 <see cref="RaiseSettingsChanged"/>가 항상
        /// 즉시 전달합니다.
        /// </summary>
        private void OnValidate()
        {
            tileRadius = Mathf.Max(TileRadiusMin, tileRadius);
            seedSearchRadius = Mathf.Max(0.001f, seedSearchRadius);
            maximumPatchTriangles = Mathf.Max(1, maximumPatchTriangles);
            maximumPatchRadius = Mathf.Max(0.001f, maximumPatchRadius);
            maximumClosureError = Mathf.Max(0.000001f, maximumClosureError);
            QueueSettingsChanged();
        }
#endif
    }
}
