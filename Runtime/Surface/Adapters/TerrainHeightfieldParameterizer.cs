using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>Terrain local XZ를 접히지 않는 intrinsic heightfield chart로 사용합니다.</summary>
    public sealed class TerrainHeightfieldParameterizer : ISurfaceParameterizer
    {
        private readonly TriangleUnfoldingSurfaceParameterizer _coverage = new();

        /// <inheritdoc />
        public SurfacePatchSet Parameterize(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            in SurfacePatchBuildSettings settings,
            ISurfaceConnectivity connectivity)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (!surfaces.TryGetTopology(seed.Surface, out SurfaceTopology seedTopology) ||
                seedTopology is not TerrainSurfaceTopology)
                throw new ArgumentException("Terrain heightfield parameterization requires Terrain topology.", nameof(seed));

            SurfacePatchSet coverage = _coverage.Parameterize(surfaces, seed, settings, connectivity);
            Vector3 seedPosition = seedTopology.Evaluate(seed);
            Vector2 origin = new(seedPosition.x, seedPosition.z);
            List<SurfacePatch> projected = new(coverage.Patches.Count);
            foreach (SurfacePatch patch in coverage.Patches) projected.Add(ProjectPatch(surfaces, patch, origin));
            return new SurfacePatchSet(seed, projected.ToArray());
        }

        private static SurfacePatch ProjectPatch(
            ISurfaceProvider surfaces,
            SurfacePatch source,
            in Vector2 origin)
        {
            if (source.SpansMultipleSurfaces)
                throw new NotSupportedException("A Terrain heightfield patch must belong to one Terrain.");
            if (!surfaces.TryGetTopology(source.Surface, out SurfaceTopology topology) ||
                topology is not TerrainSurfaceTopology)
                throw new ArgumentException("Patch surface is not Terrain topology.", nameof(surfaces));

            SurfacePatchTriangle[] triangles = new SurfacePatchTriangle[source.Triangles.Count];
            float maximumDistortion = 0f;
            double distortionSum = 0d;
            int distortionCount = 0;
            for (int i = 0; i < triangles.Length; i++)
            {
                SurfacePatchTriangle sourceTriangle = source.Triangles[i];
                SurfaceTriangle triangle = topology.Triangles[sourceTriangle.TriangleIndex];
                Vector3 positionA = topology.Positions[triangle.A];
                Vector3 positionB = topology.Positions[triangle.B];
                Vector3 positionC = topology.Positions[triangle.C];
                Vector2 a = Project(positionA, origin);
                Vector2 b = Project(positionB, origin);
                Vector2 c = Project(positionC, origin);
                triangles[i] = new SurfacePatchTriangle(
                        sourceTriangle.Surface, sourceTriangle.TriangleIndex, a, b, c)
                    .WithGraphGeodesicDistance(sourceTriangle.GraphGeodesicDistance);
                AccumulateDistortion(positionA, positionB, a, b,
                    ref maximumDistortion, ref distortionSum, ref distortionCount);
                AccumulateDistortion(positionB, positionC, b, c,
                    ref maximumDistortion, ref distortionSum, ref distortionCount);
                AccumulateDistortion(positionC, positionA, c, a,
                    ref maximumDistortion, ref distortionSum, ref distortionCount);
            }

            return new SurfacePatch(
                source.Surface,
                source.SeedTriangleIndex,
                triangles,
                new SurfacePatchDiagnostics(
                    0f,
                    source.WasTruncated,
                    false,
                    source.MaximumGraphGeodesicDistance,
                    maximumDistortion,
                    distortionCount == 0 ? 0f : (float)(distortionSum / distortionCount)));
        }

        private static Vector2 Project(in Vector3 position, in Vector2 origin) =>
            new(position.x - origin.x, position.z - origin.y);

        private static void AccumulateDistortion(
            in Vector3 sourceA,
            in Vector3 sourceB,
            in Vector2 projectedA,
            in Vector2 projectedB,
            ref float maximum,
            ref double sum,
            ref int count)
        {
            float sourceLength = Vector3.Distance(sourceA, sourceB);
            float relative = Mathf.Abs(Vector2.Distance(projectedA, projectedB) - sourceLength) /
                             Mathf.Max(sourceLength, 0.000001f);
            maximum = Mathf.Max(maximum, relative);
            sum += relative;
            count++;
        }
    }
}
