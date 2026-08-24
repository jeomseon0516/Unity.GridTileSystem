using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Query
{
    /// <summary>
    /// Seed 주변 후보 geometry를 모아 Adapter로 topology를 만들고, 실제 표면 기하로 가장 알맞은
    /// <see cref="SurfacePoint"/>를 찾습니다. Grid는 이 결과만 받으며 어떤 Unity 타입이 쓰였는지
    /// 알지 못합니다.
    /// </summary>
    public sealed class GeometrySurfaceQuery : ISurfaceQuery, ISurfaceProvider, ISurfaceDiscovery, ISurfaceTransformSource
    {
        /// <summary>월드에서 후보 GameObject를 모으는 계층입니다.</summary>
        private readonly ISurfaceCandidateSource _candidates;
        /// <summary>후보에 맞는 Adapter를 판별하는 계층입니다.</summary>
        private readonly SurfaceAdapterResolver _resolver;
        /// <summary>이번 세션에서 구축한 topology를 handle로 되찾기 위한 캐시입니다.</summary>
        private readonly Dictionary<SurfaceHandle, SurfaceTopology> _topologies = new();
        /// <summary>같은 GameObject를 다시 만나면 Adapter와 handle을 재사용하기 위한 캐시입니다.</summary>
        private readonly Dictionary<GameObject, ISurfaceAdapter> _adapters = new();
        /// <summary>handle에서 Adapter를 되찾기 위한 역방향 조회입니다.</summary>
        private readonly Dictionary<SurfaceHandle, ISurfaceAdapter> _adaptersByHandle = new();
        /// <summary>후보 수집 결과를 재사용하는 버퍼입니다.</summary>
        private readonly List<GameObject> _candidateBuffer = new();
        /// <summary>다음에 발급할 Surface 식별자입니다.</summary>
        private ulong _nextHandle = 1UL;

        /// <summary>진단이나 재조회에 사용할 수 있도록 발견된 Adapter를 노출합니다.</summary>
        public IReadOnlyCollection<ISurfaceAdapter> DiscoveredAdapters => _adapters.Values;

        /// <summary>기본 Physics 후보 수집과 기본 Adapter 목록으로 query를 만듭니다.</summary>
        public GeometrySurfaceQuery()
            : this(new PhysicsSurfaceCandidateSource(), SurfaceAdapterResolver.CreateDefault())
        {
        }

        /// <summary>후보 수집과 Adapter 판별 계층을 주입합니다.</summary>
        public GeometrySurfaceQuery(ISurfaceCandidateSource candidates, SurfaceAdapterResolver resolver)
        {
            _candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <inheritdoc />
        public bool TryFindSeed(in Vector3 worldPosition, in SurfaceQueryOptions options, out SurfaceQueryHit hit)
        {
            hit = default;
            _candidates.Collect(worldPosition, options.SearchRadius, options.LayerMask, _candidateBuffer);
            if (_candidateBuffer.Count == 0) return false;

            float bestScore = float.PositiveInfinity;
            bool found = false;
            foreach (GameObject candidate in _candidateBuffer)
            {
                if (!TryPrepare(candidate, out ISurfaceAdapter adapter, out SurfaceTopology topology)) continue;

                Transform surfaceTransform = adapter.SurfaceTransform;
                if (surfaceTransform == null) continue;
                Vector3 localQuery = surfaceTransform.InverseTransformPoint(worldPosition);
                if (!SurfaceClosestPoint.TryFind(topology, localQuery, out SurfacePoint point, out _)) continue;

                Vector3 world = surfaceTransform.TransformPoint(topology.Evaluate(point));
                float score = Score(worldPosition, world, options.PreferredDirection);
                if (score >= bestScore) continue;

                bestScore = score;
                hit = new SurfaceQueryHit(point, adapter, topology, Vector3.Distance(worldPosition, world));
                found = true;
            }

            return found;
        }

        /// <inheritdoc />
        public bool TryGetTopology(SurfaceHandle handle, out SurfaceTopology topology) =>
            _topologies.TryGetValue(handle, out topology);

        /// <inheritdoc />
        public bool TryGetAdapter(SurfaceHandle surface, out ISurfaceAdapter adapter) =>
            _adaptersByHandle.TryGetValue(surface, out adapter);

        /// <inheritdoc />
        public bool TryGetSurfaceToWorld(SurfaceHandle surface, out Matrix4x4 surfaceToWorld)
        {
            surfaceToWorld = Matrix4x4.identity;
            if (!_adaptersByHandle.TryGetValue(surface, out ISurfaceAdapter adapter)) return false;
            Transform surfaceTransform = adapter != null ? adapter.SurfaceTransform : null;
            // Transform이 없으면 topology가 이미 월드 기준이라는 계약이므로 항등 변환을 씁니다.
            if (surfaceTransform != null) surfaceToWorld = surfaceTransform.localToWorldMatrix;
            return true;
        }

        /// <inheritdoc />
        public int Discover(in Vector3 worldPosition, float radius, LayerMask layerMask, List<ISurfaceAdapter> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            _candidates.Collect(worldPosition, radius, layerMask, _candidateBuffer);
            foreach (GameObject candidate in _candidateBuffer)
            {
                if (TryPrepare(candidate, out ISurfaceAdapter adapter, out _)) results.Add(adapter);
            }
            return results.Count;
        }

        /// <summary>이번 질의 세션에서 캐시한 Adapter와 topology를 모두 해제합니다.</summary>
        public void Clear()
        {
            foreach (ISurfaceAdapter adapter in _adapters.Values) adapter.Dispose();
            _adapters.Clear();
            _adaptersByHandle.Clear();
            _topologies.Clear();
        }

        /// <summary>후보 GameObject의 Adapter와 topology를 준비하고 캐시합니다.</summary>
        private bool TryPrepare(GameObject candidate, out ISurfaceAdapter adapter, out SurfaceTopology topology)
        {
            topology = null;
            if (_adapters.TryGetValue(candidate, out adapter))
                return _topologies.TryGetValue(adapter.Handle, out topology);

            SurfaceHandle handle = new(_nextHandle);
            SurfaceAdapterResolution resolution = _resolver.Resolve(candidate, handle, out adapter);
            switch (resolution)
            {
                case SurfaceAdapterResolution.NoAdapterFound:
                    // Surface가 아닌 GameObject가 후보에 섞이는 것은 정상이므로 조용히 건너뜁니다.
                    return false;
                case SurfaceAdapterResolution.AmbiguousCandidates:
                    Debug.LogWarning(
                        $"'{candidate.name}' matches multiple surface adapters with the same priority and was skipped.",
                        candidate);
                    return false;
            }

            try
            {
                topology = adapter.BuildTopology();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to build surface topology for '{candidate.name}': {exception.Message}", candidate);
                adapter.Dispose();
                adapter = null;
                return false;
            }

            // handle을 실제로 소비했을 때만 증가시켜 실패한 후보가 식별자를 낭비하지 않게 합니다.
            _nextHandle++;
            _adapters[candidate] = adapter;
            _adaptersByHandle[handle] = adapter;
            _topologies[handle] = topology;
            return true;
        }

        /// <summary>
        /// 거리에 선호 방향 보정을 더한 선택 점수입니다. 값이 작을수록 좋은 후보이며, 선호 방향
        /// 반대편(예: 머리 위 천장)에 있는 표면은 같은 거리라도 뒤로 밀립니다.
        /// </summary>
        private static float Score(in Vector3 query, in Vector3 surface, in Vector3 preferredDirection)
        {
            Vector3 delta = surface - query;
            float distance = delta.magnitude;
            if (distance <= 0.0001f) return 0f;
            float alignment = Vector3.Dot(delta / distance, preferredDirection);
            // alignment가 1이면 선호 방향과 일치, -1이면 정반대입니다. 최대 거리만큼 가산합니다.
            return distance * (2f - alignment) * 0.5f + distance;
        }
    }
}
