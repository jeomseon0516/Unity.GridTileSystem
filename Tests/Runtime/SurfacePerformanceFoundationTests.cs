using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class SurfacePerformanceFoundationTests
    {
        [Test]
        public void BindingEvaluationJob_EvaluatesPositionAndNormal()
        {
            using NativeArray<float3> positions = new(new[]
            {
                new float3(0f, 0f, 0f), new float3(2f, 0f, 0f), new float3(0f, 2f, 0f)
            }, Allocator.TempJob);
            using NativeArray<int3> triangles = new(new[] { new int3(0, 1, 2) }, Allocator.TempJob);
            using NativeArray<SurfaceBindingJobData> bindings = new(new[]
            {
                new SurfaceBindingJobData(0, new float3(0.25f, 0.25f, 0.5f))
            }, Allocator.TempJob);
            using NativeArray<float3> outputPositions = new(1, Allocator.TempJob);
            using NativeArray<float3> outputNormals = new(1, Allocator.TempJob);
            SurfaceBindingEvaluationJob job = new()
            {
                Positions = positions,
                Triangles = triangles,
                Bindings = bindings,
                OutputPositions = outputPositions,
                OutputNormals = outputNormals
            };

            job.Schedule(1).Complete();

            Assert.That((Vector3)outputPositions[0], Is.EqualTo(new Vector3(0.5f, 1f, 0f)));
            Assert.That((Vector3)outputNormals[0], Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void ParameterizationComparison_RunsBothImplementationsOnSameCoverage()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(71),
                new[]
                {
                    new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0.5f)
                },
                new[] { 0, 1, 2, 2, 1, 3 });
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(1f / 3f, 1f / 3f, 1f / 3f));

            SurfaceParameterizationComparison comparison = SurfaceParameterizationComparison.Compare(
                new TriangleUnfoldingSurfaceParameterizer(),
                new TangentExpMapSurfaceParameterizer(),
                new SingleSurfaceProvider(topology),
                seed,
                SurfacePatchBuildSettings.Unlimited);

            Assert.That(comparison.BaselinePatchCount, Is.EqualTo(1));
            Assert.That(comparison.CandidatePatchCount, Is.EqualTo(1));
            Assert.That(comparison.BaselineMaximumMetricDistortion, Is.LessThan(0.00001f));
            Assert.That(comparison.CandidateMaximumMetricDistortion, Is.GreaterThan(0f));
        }

        [Test]
        public void DistortionAdaptiveParameterizer_SplitsUntilMetricThresholdIsMet()
        {
            SurfaceTopology topology = SurfaceTopologyBuilder.Build(
                new SurfaceHandle(72),
                new[]
                {
                    new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0.5f)
                },
                new[] { 0, 1, 2, 2, 1, 3 });
            SurfacePoint seed = new(topology.Handle, 0, new Vector3(1f / 3f, 1f / 3f, 1f / 3f));
            ISurfaceParameterizer parameterizer = new DistortionAdaptiveSurfaceParameterizer(
                new TangentExpMapSurfaceParameterizer(), 0.00001f);

            SurfacePatchSet result = parameterizer.Parameterize(
                new SingleSurfaceProvider(topology), seed, SurfacePatchBuildSettings.Unlimited, null);

            Assert.That(result.Patches, Has.Count.EqualTo(2));
            Assert.That(result.MaximumMetricDistortion, Is.LessThanOrEqualTo(0.00001f));
        }
    }
}
