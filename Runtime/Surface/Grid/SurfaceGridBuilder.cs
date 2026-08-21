using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>Surface seed 주변에 Logical Hex를 만들고 각 Hex를 Surface Region으로 정합합니다.</summary>
    public static class SurfaceGridBuilder
    {
        /// <summary>
        /// Seed가 속한 Triangle에서 local chart를 만들고 지정한 Grid 반경의 Hex Region을 생성합니다.
        /// Surface와 면적으로 겹치지 않는 Hex는 결과에 포함하지 않습니다.
        /// </summary>
        public static SurfaceGrid Build(
            SurfaceTopology topology,
            in SurfacePoint seed,
            float tileRadius,
            int gridRadius,
            in SurfacePatchBuildSettings patchSettings)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (!seed.IsValid || seed.Surface != topology.Handle)
                throw new ArgumentException("Seed must be a valid point on the supplied topology.", nameof(seed));
            if (tileRadius <= 0f || float.IsNaN(tileRadius) || float.IsInfinity(tileRadius))
                throw new ArgumentOutOfRangeException(nameof(tileRadius));
            if (gridRadius < 0) throw new ArgumentOutOfRangeException(nameof(gridRadius));

            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(topology, seed, patchSettings);
            SurfacePatchTriangle seedTriangle = FindPatchTriangle(patch, seed.TriangleIndex);
            Vector2 seedIntrinsic = seedTriangle.A * seed.Barycentric.x +
                                    seedTriangle.B * seed.Barycentric.y +
                                    seedTriangle.C * seed.Barycentric.z;
            IntrinsicHexLayout layout = new(seedIntrinsic, tileRadius);
            List<SurfaceGridTileRegion> tiles = new();

            for (int q = -gridRadius; q <= gridRadius; q++)
            {
                int minimumR = Mathf.Max(-gridRadius, -q - gridRadius);
                int maximumR = Mathf.Min(gridRadius, -q + gridRadius);
                for (int r = minimumR; r <= maximumR; r++)
                {
                    AxialCoordinates axial = new(q, r);
                    Vector2 center = layout.GetCenter(axial);
                    SurfaceRegion region = SurfaceRegionBuilder.Build(topology, patch, layout.GetCorners(axial));
                    if (region.TriangleIndices.Count == 0) continue;

                    tiles.Add(new SurfaceGridTileRegion(new HexCoordinates(q, r), center, region));
                }
            }

            return new SurfaceGrid(patch, layout, tiles.ToArray());
        }

        /// <summary>Patch 배열에서 원본 Triangle index가 일치하는 펼쳐진 Face를 찾습니다.</summary>
        private static SurfacePatchTriangle FindPatchTriangle(SurfacePatch patch, int triangleIndex)
        {
            foreach (SurfacePatchTriangle triangle in patch.Triangles)
            {
                if (triangle.TriangleIndex == triangleIndex) return triangle;
            }
            throw new InvalidOperationException("Seed triangle is missing from its own Surface Patch.");
        }
    }
}
