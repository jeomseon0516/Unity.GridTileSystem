using System;
using System.Collections.Generic;
using System.Linq;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class SurfaceSkinBindingTests
    {
        /// <summary>xy 평면의 두 삼각형 quad입니다. 정점 0·2는 bone 0, 정점 1·3은 bone 1에 묶습니다.</summary>
        private static SurfaceTopology CreateQuad() => SurfaceTopologyBuilder.Build(
            new SurfaceHandle(77),
            new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f)
            },
            new[] { 0, 1, 2, 2, 1, 3 });

        private static IReadOnlyList<IReadOnlyList<SurfaceBoneInfluence>> CreateTwoBoneInfluences() => new[]
        {
            new[] { new SurfaceBoneInfluence(0, 1f) },
            new[] { new SurfaceBoneInfluence(1, 1f) },
            new[] { new SurfaceBoneInfluence(0, 1f) },
            new[] { new SurfaceBoneInfluence(1, 1f) }
        };

        [Test]
        public void Build_InterpolatesBoneWeightsAcrossTriangleByBarycentric()
        {
            SurfaceTopology topology = CreateQuad();
            // Triangle 0 = (v0, v1, v2). v0/v2는 bone 0, v1은 bone 1이므로 barycentric (0.25, 0.5, 0.25)는
            // bone 0에 0.5, bone 1에 0.5가 되어야 합니다.
            SurfacePoint point = new(topology.Handle, 0, new Vector3(0.25f, 0.5f, 0.25f));

            SurfaceSkinBinding binding = SurfaceSkinBindingBuilder.Build(
                topology, new[] { point }, CreateTwoBoneInfluences(), 2);

            var influences = binding.GetInfluences(0).OrderBy(i => i.BoneIndex).ToArray();
            Assert.That(influences, Has.Length.EqualTo(2));
            Assert.That(influences[0].BoneIndex, Is.EqualTo(0));
            Assert.That(influences[0].Weight, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(influences[1].BoneIndex, Is.EqualTo(1));
            Assert.That(influences[1].Weight, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Build_NormalizesAccumulatedWeightsToOne()
        {
            SurfaceTopology topology = CreateQuad();
            SurfacePoint[] points =
            {
                new(topology.Handle, 0, new Vector3(1f, 0f, 0f)),
                new(topology.Handle, 0, new Vector3(0.2f, 0.3f, 0.5f)),
                new(topology.Handle, 1, new Vector3(1f / 3f, 1f / 3f, 1f / 3f))
            };

            SurfaceSkinBinding binding = SurfaceSkinBindingBuilder.Build(
                topology, points, CreateTwoBoneInfluences(), 2);

            for (int vertex = 0; vertex < points.Length; vertex++)
            {
                float total = binding.GetInfluences(vertex).Sum(i => i.Weight);
                Assert.That(total, Is.EqualTo(1f).Within(0.0001f), $"vertex {vertex}");
            }
        }

        [Test]
        public void Evaluate_WithIdentityMatrices_ReproducesBindPosePositions()
        {
            SurfaceTopology topology = CreateQuad();
            SurfacePoint point = new(topology.Handle, 0, new Vector3(0.25f, 0.5f, 0.25f));
            SurfaceSkinBinding binding = SurfaceSkinBindingBuilder.Build(
                topology, new[] { point }, CreateTwoBoneInfluences(), 2);

            Vector3[] positions = new Vector3[1];
            Vector3[] normals = new Vector3[1];
            binding.Evaluate(new[] { Matrix4x4.identity, Matrix4x4.identity }, positions, normals);

            Assert.That(positions[0], Is.EqualTo(topology.Evaluate(point)));
            Assert.That(normals[0], Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void Evaluate_WithTranslatedBone_MovesPointByWeightedAmount()
        {
            SurfaceTopology topology = CreateQuad();
            SurfacePoint point = new(topology.Handle, 0, new Vector3(0.25f, 0.5f, 0.25f));
            SurfaceSkinBinding binding = SurfaceSkinBindingBuilder.Build(
                topology, new[] { point }, CreateTwoBoneInfluences(), 2);
            Vector3 bindPosition = topology.Evaluate(point);

            // bone 1만 +z로 2만큼 이동하면 가중치 0.5이므로 결과는 정확히 1만큼 이동해야 합니다.
            Matrix4x4[] matrices =
            {
                Matrix4x4.identity,
                Matrix4x4.Translate(new Vector3(0f, 0f, 2f))
            };
            Vector3[] positions = new Vector3[1];
            Vector3[] normals = new Vector3[1];
            binding.Evaluate(matrices, positions, normals);

            Assert.That(positions[0].x, Is.EqualTo(bindPosition.x).Within(0.0001f));
            Assert.That(positions[0].y, Is.EqualTo(bindPosition.y).Within(0.0001f));
            Assert.That(positions[0].z, Is.EqualTo(bindPosition.z + 1f).Within(0.0001f));
        }

        [Test]
        public void Evaluate_RejectsMismatchedBufferAndMatrixCounts()
        {
            SurfaceTopology topology = CreateQuad();
            SurfacePoint point = new(topology.Handle, 0, new Vector3(1f / 3f, 1f / 3f, 1f / 3f));
            SurfaceSkinBinding binding = SurfaceSkinBindingBuilder.Build(
                topology, new[] { point }, CreateTwoBoneInfluences(), 2);

            Assert.That(
                () => binding.Evaluate(new[] { Matrix4x4.identity, Matrix4x4.identity }, new Vector3[2], new Vector3[2]),
                Throws.ArgumentException);
            Assert.That(
                () => binding.Evaluate(new[] { Matrix4x4.identity }, new Vector3[1], new Vector3[1]),
                Throws.ArgumentException);
        }

        [Test]
        public void Build_RejectsForeignSurfacePointAndOutOfRangeBoneIndex()
        {
            SurfaceTopology topology = CreateQuad();
            SurfacePoint foreign = new(new SurfaceHandle(999), 0, new Vector3(1f / 3f, 1f / 3f, 1f / 3f));
            Assert.That(
                () => SurfaceSkinBindingBuilder.Build(topology, new[] { foreign }, CreateTwoBoneInfluences(), 2),
                Throws.ArgumentException);

            SurfacePoint valid = new(topology.Handle, 0, new Vector3(1f, 0f, 0f));
            var badInfluences = new[]
            {
                new[] { new SurfaceBoneInfluence(5, 1f) },
                new[] { new SurfaceBoneInfluence(0, 1f) },
                new[] { new SurfaceBoneInfluence(0, 1f) },
                new[] { new SurfaceBoneInfluence(0, 1f) }
            };
            Assert.That(
                () => SurfaceSkinBindingBuilder.Build(topology, new[] { valid }, badInfluences, 2),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Build_RejectsPointWithoutAnyBoneInfluence()
        {
            SurfaceTopology topology = CreateQuad();
            SurfacePoint point = new(topology.Handle, 0, new Vector3(1f, 0f, 0f));
            var emptyInfluences = new IReadOnlyList<SurfaceBoneInfluence>[]
            {
                Array.Empty<SurfaceBoneInfluence>(),
                Array.Empty<SurfaceBoneInfluence>(),
                Array.Empty<SurfaceBoneInfluence>(),
                Array.Empty<SurfaceBoneInfluence>()
            };

            Assert.That(
                () => SurfaceSkinBindingBuilder.Build(topology, new[] { point }, emptyInfluences, 2),
                Throws.InvalidOperationException);
        }
    }
}
