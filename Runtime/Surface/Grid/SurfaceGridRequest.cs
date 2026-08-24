using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Query;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>
    /// Grid 하나를 생성하기 위해 사용자가 지정하는 전부입니다. Surface를 등록하거나 지정하는 항목은
    /// 없으며, 시스템이 <see cref="SeedPosition"/> 주변에서 표면을 스스로 찾습니다.
    /// </summary>
    public readonly struct SurfaceGridRequest
    {
        /// <summary>Grid를 시작할 월드 위치입니다. 이 지점에 가장 알맞은 표면이 seed가 됩니다.</summary>
        public Vector3 SeedPosition { get; }
        /// <summary>
        /// 격자를 정렬할 월드 방향입니다. seed 표면의 접평면에 투영되어 chart 회전이 되며, 투영 결과가
        /// 첫 Hex 꼭짓점 방향이 됩니다. <see cref="Vector3.zero"/>이면 회전 없이 chart 기본 방향을 씁니다.
        /// </summary>
        public Vector3 InitialDirection { get; }
        /// <summary>Tile 해상도입니다. Hex 중심에서 꼭짓점까지의 실제 표면 길이입니다.</summary>
        public float TileRadius { get; }
        /// <summary>Grid가 덮을 최대 범위를 결정하는 Patch 성장 제한입니다.</summary>
        public SurfacePatchBuildSettings PatchSettings { get; }
        /// <summary>Seed 표면을 찾을 때 사용할 탐색 범위와 선택 기준입니다.</summary>
        public SurfaceQueryOptions QueryOptions { get; }

        /// <summary>기본 탐색 옵션과 무제한 Patch로 seed 위치와 Tile 해상도만 지정합니다.</summary>
        public SurfaceGridRequest(in Vector3 seedPosition, float tileRadius)
            : this(
                seedPosition,
                tileRadius,
                Vector3.zero,
                SurfacePatchBuildSettings.Unlimited,
                SurfaceQueryOptions.Default)
        {
        }

        /// <summary>seed 위치, Tile 해상도, 초기 방향, Patch 제한과 탐색 옵션을 모두 지정합니다.</summary>
        public SurfaceGridRequest(
            in Vector3 seedPosition,
            float tileRadius,
            in Vector3 initialDirection,
            in SurfacePatchBuildSettings patchSettings,
            in SurfaceQueryOptions queryOptions)
        {
            if (!IsFinite(seedPosition))
                throw new ArgumentException("Seed position must contain only finite values.", nameof(seedPosition));
            if (!IsFinite(initialDirection))
                throw new ArgumentException("Initial direction must contain only finite values.", nameof(initialDirection));
            if (tileRadius <= 0f || float.IsNaN(tileRadius) || float.IsInfinity(tileRadius))
                throw new ArgumentOutOfRangeException(nameof(tileRadius));

            SeedPosition = seedPosition;
            InitialDirection = initialDirection;
            TileRadius = tileRadius;
            PatchSettings = patchSettings;
            QueryOptions = queryOptions;
        }

        /// <summary>좌표가 NaN/Infinity를 포함하지 않는지 검사합니다.</summary>
        private static bool IsFinite(in Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
