using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>dirty Chunk에 속한 Tile만 독립 Geometry snapshot으로 다시 만드는 CPU 기준 구현입니다.</summary>
    public static class SurfaceGridChunkGeometryBuilder
    {
        /// <summary>단일 Surface Grid에서 지정 Chunk에 속한 Tile Geometry만 생성합니다.</summary>
        public static SurfaceGridGeometry Build(
            SurfaceTopology topology,
            SurfaceGrid grid,
            in SurfaceGridChunk chunk,
            int chunkSize,
            in Matrix4x4 surfaceToTarget,
            float surfaceOffset = 0f)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

            SurfaceGrid chunkGrid = SelectChunk(grid, chunk, chunkSize);
            return SurfaceGridGeometryBuilder.Build(topology, chunkGrid, surfaceToTarget, surfaceOffset);
        }

        /// <summary>여러 Surface에 걸친 Grid에서 지정 Chunk만 공통 target 공간 Geometry로 생성합니다.</summary>
        public static SurfaceGridGeometry Build(
            ISurfaceProvider surfaces,
            ISurfaceTransformSource transforms,
            SurfaceGrid grid,
            in SurfaceGridChunk chunk,
            int chunkSize,
            in Matrix4x4 worldToTarget,
            float surfaceOffset = 0f)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (transforms == null) throw new ArgumentNullException(nameof(transforms));
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            SurfaceGrid chunkGrid = SelectChunk(grid, chunk, chunkSize);
            return SurfaceGridGeometryBuilder.Build(
                surfaces, transforms, chunkGrid, worldToTarget, surfaceOffset);
        }

        private static SurfaceGrid SelectChunk(
            SurfaceGrid grid,
            in SurfaceGridChunk chunk,
            int chunkSize)
        {
            List<SurfaceGridTileRegion> selected = new();
            foreach (SurfaceGridTileRegion tile in grid.Tiles)
            {
                if (SurfaceGridChunk.FromTile(tile.Coordinates, chunkSize).Equals(chunk)) selected.Add(tile);
            }

            return new SurfaceGrid(grid, selected.ToArray(), selected.Count, selected.Count);
        }
    }
}
