using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Seed 접평면에 3D offset을 투영하는 1차 exponential/log-map 비교 구현입니다. 곡률이 큰 영역의
    /// production 해법이 아니라 Triangle Unfolding의 누적 회전과 왜곡을 정량 비교하는 기준선입니다.
    /// </summary>
    public sealed class TangentExpMapSurfaceParameterizer : ISurfaceParameterizer
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
            SurfacePatchSet coverage = _coverage.Parameterize(surfaces, seed, settings, connectivity);
            List<SurfacePatch> projected = new(coverage.Patches.Count);
            foreach (SurfacePatch patch in coverage.Patches)
            {
                if (patch.SpansMultipleSurfaces)
                {
                    throw new NotSupportedException(
                        "Tangent ExpMap comparison requires one local coordinate space per patch.");
                }
                projected.Add(ProjectPatch(surfaces, seed, patch));
            }
            return new SurfacePatchSet(seed, projected.ToArray());
        }

        private static SurfacePatch ProjectPatch(
            ISurfaceProvider surfaces,
            in SurfacePoint requestSeed,
            SurfacePatch source)
        {
            if (!surfaces.TryGetTopology(source.Surface, out SurfaceTopology topology))
                throw new ArgumentException("Patch surface is not available from the provider.", nameof(surfaces));
            SurfaceTriangle seedTriangle = topology.Triangles[source.SeedTriangleIndex];
            Vector3 a = topology.Positions[seedTriangle.A];
            Vector3 b = topology.Positions[seedTriangle.B];
            Vector3 c = topology.Positions[seedTriangle.C];
            Vector3 origin = source.Surface == requestSeed.Surface &&
                             source.SeedTriangleIndex == requestSeed.TriangleIndex
                ? topology.Evaluate(requestSeed)
                : (a + b + c) / 3f;
            Vector3 tangent = (b - a).normalized;
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            Vector3 bitangent = Vector3.Cross(normal, tangent);

            SurfacePatchTriangle[] triangles = new SurfacePatchTriangle[source.Triangles.Count];
            float maximumDistortion = 0f;
            double distortionSum = 0d;
            int distortionCount = 0;
            for (int i = 0; i < triangles.Length; i++)
            {
                SurfacePatchTriangle sourceTriangle = source.Triangles[i];
                SurfaceTriangle triangle = topology.Triangles[sourceTriangle.TriangleIndex];
                Vector2 projectedA = Project(topology.Positions[triangle.A] - origin, tangent, bitangent);
                Vector2 projectedB = Project(topology.Positions[triangle.B] - origin, tangent, bitangent);
                Vector2 projectedC = Project(topology.Positions[triangle.C] - origin, tangent, bitangent);
                triangles[i] = new SurfacePatchTriangle(
                        sourceTriangle.Surface,
                        sourceTriangle.TriangleIndex,
                        projectedA,
                        projectedB,
                        projectedC)
                    .WithGraphGeodesicDistance(sourceTriangle.GraphGeodesicDistance);
                AccumulateDistortion(topology.Positions[triangle.A], topology.Positions[triangle.B],
                    projectedA, projectedB, ref maximumDistortion, ref distortionSum, ref distortionCount);
                AccumulateDistortion(topology.Positions[triangle.B], topology.Positions[triangle.C],
                    projectedB, projectedC, ref maximumDistortion, ref distortionSum, ref distortionCount);
                AccumulateDistortion(topology.Positions[triangle.C], topology.Positions[triangle.A],
                    projectedC, projectedA, ref maximumDistortion, ref distortionSum, ref distortionCount);
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

        private static Vector2 Project(in Vector3 offset, in Vector3 tangent, in Vector3 bitangent) =>
            new(Vector3.Dot(offset, tangent), Vector3.Dot(offset, bitangent));

        private static void AccumulateDistortion(
            in Vector3 a,
            in Vector3 b,
            in Vector2 projectedA,
            in Vector2 projectedB,
            ref float maximum,
            ref double sum,
            ref int count)
        {
            float sourceLength = Vector3.Distance(a, b);
            float relative = Mathf.Abs(Vector2.Distance(projectedA, projectedB) - sourceLength) /
                             Mathf.Max(sourceLength, 0.000001f);
            maximum = Mathf.Max(maximum, relative);
            sum += relative;
            count++;
        }
    }
}
