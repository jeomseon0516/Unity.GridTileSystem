using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Query;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class GeometrySurfaceQueryTests
    {
        /// <summary>Physics 없이 지정한 GameObject만 후보로 돌려주는 테스트용 수집기입니다.</summary>
        private sealed class FixedCandidateSource : ISurfaceCandidateSource
        {
            private readonly GameObject[] _candidates;
            public FixedCandidateSource(params GameObject[] candidates) => _candidates = candidates;

            public int Collect(in Vector3 center, float radius, LayerMask layerMask, List<GameObject> results)
            {
                results.Clear();
                foreach (GameObject candidate in _candidates)
                {
                    if (candidate != null) results.Add(candidate);
                }
                return results.Count;
            }
        }

        private static Mesh CreatePlaneMesh()
        {
            Mesh mesh = new() { name = "Query Plane" };
            mesh.vertices = new[]
            {
                new Vector3(-5f, 0f, -5f), new Vector3(5f, 0f, -5f),
                new Vector3(-5f, 0f, 5f), new Vector3(5f, 0f, 5f)
            };
            // y=0 평면이며 법선이 +y를 향하도록 winding을 잡습니다.
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static GameObject CreateMeshSurface(string name, Vector3 position, Mesh mesh)
        {
            GameObject host = new(name);
            host.transform.position = position;
            host.AddComponent<MeshFilter>().sharedMesh = mesh;
            return host;
        }

        [Test]
        public void TryFindSeed_AboveMeshSurface_ReturnsSurfacePointWithoutAnyRegistration()
        {
            Mesh mesh = CreatePlaneMesh();
            GameObject surface = CreateMeshSurface("Ground", Vector3.zero, mesh);
            GeometrySurfaceQuery query = new(
                new FixedCandidateSource(surface), SurfaceAdapterResolver.CreateDefault());

            try
            {
                // 사용자는 어떤 Surface도 등록하거나 지정하지 않았습니다.
                bool found = query.TryFindSeed(
                    new Vector3(1f, 3f, -2f), SurfaceQueryOptions.Default, out SurfaceQueryHit hit);

                Assert.That(found, Is.True);
                Assert.That(hit.Point.IsValid, Is.True);
                Assert.That(hit.Topology, Is.Not.Null);
                Assert.That(hit.Distance, Is.EqualTo(3f).Within(0.001f));
                Vector3 world = hit.Adapter.SurfaceTransform.TransformPoint(hit.Topology.Evaluate(hit.Point));
                Assert.That(world.x, Is.EqualTo(1f).Within(0.001f));
                Assert.That(world.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(world.z, Is.EqualTo(-2f).Within(0.001f));
            }
            finally
            {
                query.Clear();
                Object.DestroyImmediate(surface);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void TryFindSeed_ProvidesTopologyBackThroughSurfaceProvider()
        {
            Mesh mesh = CreatePlaneMesh();
            GameObject surface = CreateMeshSurface("Ground", Vector3.zero, mesh);
            GeometrySurfaceQuery query = new(
                new FixedCandidateSource(surface), SurfaceAdapterResolver.CreateDefault());

            try
            {
                query.TryFindSeed(new Vector3(0f, 2f, 0f), SurfaceQueryOptions.Default, out SurfaceQueryHit hit);

                // Grid는 목록이 아니라 handle 조회로 topology를 되찾습니다.
                Assert.That(query.TryGetTopology(hit.Point.Surface, out SurfaceTopology topology), Is.True);
                Assert.That(topology, Is.SameAs(hit.Topology));
            }
            finally
            {
                query.Clear();
                Object.DestroyImmediate(surface);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void TryFindSeed_TwoSurfacesSharingOneMesh_ReceiveDistinctHandles()
        {
            Mesh mesh = CreatePlaneMesh();
            GameObject lower = CreateMeshSurface("Lower", Vector3.zero, mesh);
            GameObject upper = CreateMeshSurface("Upper", new Vector3(0f, 10f, 0f), mesh);
            GeometrySurfaceQuery query = new(
                new FixedCandidateSource(lower, upper), SurfaceAdapterResolver.CreateDefault());

            try
            {
                query.TryFindSeed(new Vector3(0f, 1f, 0f), SurfaceQueryOptions.Default, out SurfaceQueryHit low);
                query.TryFindSeed(new Vector3(0f, 11f, 0f), SurfaceQueryOptions.Default, out SurfaceQueryHit high);

                // 같은 Mesh asset을 공유해도 서로 다른 Surface여야 합니다.
                Assert.That(low.Point.Surface, Is.Not.EqualTo(high.Point.Surface));
                Assert.That(low.Topology, Is.Not.SameAs(high.Topology));
            }
            finally
            {
                query.Clear();
                Object.DestroyImmediate(lower);
                Object.DestroyImmediate(upper);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void TryFindSeed_PrefersSurfaceInPreferredDirection()
        {
            Mesh mesh = CreatePlaneMesh();
            GameObject floor = CreateMeshSurface("Floor", Vector3.zero, mesh);
            GameObject ceiling = CreateMeshSurface("Ceiling", new Vector3(0f, 4f, 0f), mesh);
            GeometrySurfaceQuery query = new(
                new FixedCandidateSource(floor, ceiling), SurfaceAdapterResolver.CreateDefault());

            try
            {
                // 바닥과 천장 사이에서 아래쪽을 선호하면 바닥이 선택돼야 합니다.
                query.TryFindSeed(new Vector3(0f, 2.1f, 0f), SurfaceQueryOptions.Default, out SurfaceQueryHit hit);
                Vector3 world = hit.Adapter.SurfaceTransform.TransformPoint(hit.Topology.Evaluate(hit.Point));
                Assert.That(world.y, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                query.Clear();
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(ceiling);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void TryFindSeed_UnsupportedGameObject_ReturnsFalseWithoutThrowing()
        {
            GameObject empty = new("No Surface");
            GeometrySurfaceQuery query = new(
                new FixedCandidateSource(empty), SurfaceAdapterResolver.CreateDefault());

            try
            {
                Assert.That(
                    query.TryFindSeed(Vector3.zero, SurfaceQueryOptions.Default, out _),
                    Is.False);
            }
            finally
            {
                query.Clear();
                Object.DestroyImmediate(empty);
            }
        }
    }
}
