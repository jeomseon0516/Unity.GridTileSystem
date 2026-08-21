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
            in SurfacePatchBuildSettings patchSettings)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            return Build(new SingleSurfaceProvider(topology), seed, tileRadius, patchSettings);
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
            in SurfacePatchBuildSettings patchSettings)
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
            IntrinsicHexLayout layout = new(seedIntrinsic, tileRadius);
            List<SurfaceGridTileRegion> tiles = new();
            Rect bounds = patch.IntrinsicBounds;

            // flat-top layout에서 중심 x는 q에만 의존하므로(x = 1.5R·q) q 구간을 먼저 확정할 수 있습니다.
            // 각 q 열의 r 구간은 y = sqrt(3)R·(r + q/2)를 r에 대해 풀어 구합니다. Hex는 중심에서 R만큼
            // 뻗으므로 양쪽에 한 칸씩 여유를 두어 경계에 걸친 Tile을 빠뜨리지 않습니다.
            GetColumnRange(layout, bounds, out int minimumQ, out int maximumQ);
            for (int q = minimumQ; q <= maximumQ; q++)
            {
                GetRowRange(layout, bounds, q, out int minimumR, out int maximumR);
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

        /// <summary>intrinsic 경계를 덮는 데 필요한 flat-top Hex 열(q) 구간을 계산합니다.</summary>
        private static void GetColumnRange(
            in IntrinsicHexLayout layout,
            in Rect bounds,
            out int minimumQ,
            out int maximumQ)
        {
            // x = Origin.x + 1.5R·q 를 q에 대해 푼 값입니다.
            float columnSpacing = layout.Radius * 1.5f;
            minimumQ = Mathf.FloorToInt((bounds.xMin - layout.Origin.x) / columnSpacing) - 1;
            maximumQ = Mathf.CeilToInt((bounds.xMax - layout.Origin.x) / columnSpacing) + 1;
        }

        /// <summary>지정한 q 열에서 intrinsic 경계를 덮는 데 필요한 행(r) 구간을 계산합니다.</summary>
        private static void GetRowRange(
            in IntrinsicHexLayout layout,
            in Rect bounds,
            int q,
            out int minimumR,
            out int maximumR)
        {
            // y = Origin.y + sqrt(3)R·(r + q/2) 를 r에 대해 푼 값입니다.
            float rowSpacing = layout.Radius * Mathf.Sqrt(3f);
            float columnOffset = q * 0.5f;
            minimumR = Mathf.FloorToInt((bounds.yMin - layout.Origin.y) / rowSpacing - columnOffset) - 1;
            maximumR = Mathf.CeilToInt((bounds.yMax - layout.Origin.y) / rowSpacing - columnOffset) + 1;
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
