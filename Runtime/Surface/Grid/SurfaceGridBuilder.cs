using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>Surface seed 주변에 Logical Hex를 만들고 각 Hex를 Surface Region으로 정합합니다.</summary>
    public static class SurfaceGridBuilder
    {
        /// <summary>완전 포함 면적 비교에서 누적 clipping 오차를 허용하는 상대 오차입니다.</summary>
        private const float FullTileAreaRelativeTolerance = 0.0001f;

        /// <summary>
        /// Seed가 속한 Triangle에서 local chart를 만들고, 그 chart 전체를 덮는 Hex Region을 생성합니다.
        /// 사용자는 Tile 해상도만 지정하며 Grid 범위는 Patch가 실제로 펼친 Surface 크기가 결정합니다.
        /// Hex 전체 면적이 Surface Region으로 복원되는 완전한 Tile만 결과에 포함합니다. 따라서 Surface
        /// 경계에서 잘린 Tile은 Logical Grid와 Geometry 양쪽에서 제외됩니다.
        /// Grid가 덮는 최대 범위는 <paramref name="patchSettings"/>의 Triangle 개수·intrinsic 반경
        /// 한계가 제어합니다.
        /// </summary>
        public static SurfaceGrid Build(
            SurfaceTopology topology,
            in SurfacePoint seed,
            float tileRadius,
            in SurfacePatchBuildSettings patchSettings) =>
            Build(topology, seed, tileRadius, patchSettings, 0f);

        /// <summary>
        /// 격자 회전각을 지정해 <see cref="Build(SurfaceTopology, in SurfacePoint, float, in SurfacePatchBuildSettings)"/>
        /// 와 같은 Grid를 생성합니다. 회전은 chart 상에서만 적용되므로 Tile 크기와 정합 규칙은 그대로입니다.
        /// </summary>
        public static SurfaceGrid Build(
            SurfaceTopology topology,
            in SurfacePoint seed,
            float tileRadius,
            in SurfacePatchBuildSettings patchSettings,
            float rotation)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            return Build(new SingleSurfaceProvider(topology), seed, tileRadius, patchSettings, rotation);
        }

        /// <summary>
        /// Surface local 방향을 격자 초기 방향으로 삼아 Grid를 생성합니다. 방향은 seed Face의 평면에
        /// 투영되어 chart 회전으로 변환되며, <see cref="Vector3.zero"/>는 회전 없음을 뜻합니다.
        /// </summary>
        public static SurfaceGrid Build(
            SurfaceTopology topology,
            in SurfacePoint seed,
            float tileRadius,
            in SurfacePatchBuildSettings patchSettings,
            in Vector3 initialSurfaceDirection)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            return Build(new SingleSurfaceProvider(topology), seed, tileRadius, patchSettings, initialSurfaceDirection);
        }

        /// <summary>
        /// Surface를 handle로 조회하는 provider를 받아 Grid를 생성합니다. Grid는 Surface 목록을 알지
        /// 못하며 seed가 가리키는 Surface에서 시작해 필요한 것만 조회합니다. 향후 chart가 여러 Surface에
        /// 걸치게 되면 이 provider가 경계 너머 topology를 공급합니다.
        /// </summary>
        public static SurfaceGrid Build(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            float tileRadius,
            in SurfacePatchBuildSettings patchSettings) =>
            Build(surfaces, seed, tileRadius, patchSettings, 0f);

        /// <summary>
        /// 격자 회전각을 지정해 provider 기반 Grid를 생성합니다. 회전각은 chart 2D 좌표계 기준
        /// 반시계 라디안이며, 사용자가 지정하는 초기 방향은 상위 계층이 이 각도로 변환해 전달합니다.
        /// </summary>
        public static SurfaceGrid Build(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            float tileRadius,
            in SurfacePatchBuildSettings patchSettings,
            float rotation) =>
            BuildCore(surfaces, seed, tileRadius, patchSettings, rotation, Vector3.zero);

        /// <summary>
        /// Surface local 방향을 격자 초기 방향으로 삼아 provider 기반 Grid를 생성합니다. seed Face의
        /// 평면에 수직인 방향은 격자 방향을 정의하지 못하므로 예외로 거부하며 조용히 무시하지 않습니다.
        /// </summary>
        public static SurfaceGrid Build(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            float tileRadius,
            in SurfacePatchBuildSettings patchSettings,
            in Vector3 initialSurfaceDirection) =>
            BuildCore(surfaces, seed, tileRadius, patchSettings, 0f, initialSurfaceDirection);

        /// <summary>
        /// 회전각 또는 Surface local 초기 방향 중 하나로 격자 방향을 결정하는 실제 구현입니다.
        /// <paramref name="initialSurfaceDirection"/>이 영벡터가 아니면 그쪽이 회전각을 대체합니다.
        /// </summary>
        private static SurfaceGrid BuildCore(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            float tileRadius,
            in SurfacePatchBuildSettings patchSettings,
            float rotation,
            in Vector3 initialSurfaceDirection)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (!seed.IsValid) throw new ArgumentException("Seed must be a valid surface point.", nameof(seed));
            if (!surfaces.TryGetTopology(seed.Surface, out SurfaceTopology topology))
                throw new ArgumentException("Seed surface is not available from the provider.", nameof(seed));
            if (tileRadius <= 0f || float.IsNaN(tileRadius) || float.IsInfinity(tileRadius))
                throw new ArgumentOutOfRangeException(nameof(tileRadius));

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, seed, patchSettings);
            SurfacePatchTriangle seedTriangle = FindPatchTriangle(patch, seed);
            Vector2 seedIntrinsic = seedTriangle.A * seed.Barycentric.x +
                                    seedTriangle.B * seed.Barycentric.y +
                                    seedTriangle.C * seed.Barycentric.z;
            IntrinsicHexLayout layout = new(seedIntrinsic, tileRadius, ResolveRotation(topology, seedTriangle, rotation, initialSurfaceDirection));
            List<SurfaceGridTileRegion> tiles = new();
            Rect bounds = patch.IntrinsicBounds;

            // 격자 좌표계에서 중심 x는 q에만 의존하므로 q 구간을 먼저 확정하고, 각 열의 r 구간을 구합니다.
            // 회전이 있으면 layout이 경계를 격자 좌표계로 역회전해 보수적인 구간을 돌려주므로 여기서는
            // 회전을 알 필요가 없습니다. 구간 밖으로 나간 Hex는 아래 면적 계약에서 걸러집니다.
            layout.GetColumnRange(bounds, out int minimumQ, out int maximumQ);
            for (int q = minimumQ; q <= maximumQ; q++)
            {
                layout.GetRowRange(bounds, q, out int minimumR, out int maximumR);
                for (int r = minimumR; r <= maximumR; r++)
                {
                    AxialCoordinates axial = new(q, r);
                    Vector2 center = layout.GetCenter(axial);
                    Vector2[] corners = layout.GetCorners(axial);
                    SurfaceRegion region = SurfaceRegionBuilder.Build(topology, patch, corners);
                    if (!CoversEntirePolygon(region, corners)) continue;

                    tiles.Add(new SurfaceGridTileRegion(new HexCoordinates(q, r), center, region));
                }
            }

            return new SurfaceGrid(patch, layout, tiles.ToArray());
        }

        /// <summary>초기 방향이 주어졌으면 chart 회전각으로 변환하고, 없으면 지정된 회전각을 씁니다.</summary>
        private static float ResolveRotation(
            SurfaceTopology topology,
            in SurfacePatchTriangle seedTriangle,
            float rotation,
            in Vector3 initialSurfaceDirection)
        {
            if (initialSurfaceDirection.sqrMagnitude <= 0f) return rotation;
            if (!SurfaceChartDirection.TryGetChartDirection(
                    topology, seedTriangle, initialSurfaceDirection, out Vector2 chartDirection))
            {
                throw new ArgumentException(
                    "Initial direction is parallel to the seed surface normal and cannot orient the grid.",
                    nameof(initialSurfaceDirection));
            }
            return Mathf.Atan2(chartDirection.y, chartDirection.x);
        }

        /// <summary>Region 면적이 원본 polygon 전체를 허용오차 안에서 덮는지 검사합니다.</summary>
        private static bool CoversEntirePolygon(SurfaceRegion region, IReadOnlyList<Vector2> polygon)
        {
            if (region.TriangleIndices.Count == 0) return false;
            float polygonAreaTwice = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 current = polygon[i];
                Vector2 next = polygon[(i + 1) % polygon.Count];
                polygonAreaTwice += current.x * next.y - current.y * next.x;
            }
            float polygonArea = Mathf.Abs(polygonAreaTwice) * 0.5f;
            float tolerance = Mathf.Max(0.000001f, polygonArea * FullTileAreaRelativeTolerance);
            return Mathf.Abs(region.IntrinsicArea - polygonArea) <= tolerance;
        }

        /// <summary>Patch 배열에서 원본 Triangle index가 일치하는 펼쳐진 Face를 찾습니다.</summary>
        private static SurfacePatchTriangle FindPatchTriangle(SurfacePatch patch, in SurfacePoint seed)
        {
            foreach (SurfacePatchTriangle triangle in patch.Triangles)
            {
                if (triangle.Matches(seed.Surface, seed.TriangleIndex)) return triangle;
            }
            throw new InvalidOperationException("Seed triangle is missing from its own Surface Patch.");
        }
    }
}
