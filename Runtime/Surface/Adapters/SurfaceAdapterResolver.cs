using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>
    /// GameObject가 어떤 종류의 Surface 입력을 제공하는지 factory 우선순위로 판별합니다.
    /// 사용자는 Surface 종류를 고르지 않으며 Grid Core는 이 판별 결과를 알지 못합니다.
    /// </summary>
    public sealed class SurfaceAdapterResolver
    {
        /// <summary>우선순위 내림차순으로 정렬해 보관하는 factory 목록입니다.</summary>
        private readonly List<ISurfaceAdapterFactory> _factories = new();

        /// <summary>기본 제공 factory로 resolver를 만듭니다.</summary>
        public static SurfaceAdapterResolver CreateDefault()
        {
            SurfaceAdapterResolver resolver = new();
            resolver.Register(new TerrainSurfaceAdapterFactory());
            resolver.Register(new SkinnedMeshSurfaceAdapterFactory());
            resolver.Register(new MeshSurfaceAdapterFactory());
            return resolver;
        }

        /// <summary>factory를 추가하고 우선순위 순서를 유지합니다.</summary>
        public void Register(ISurfaceAdapterFactory factory)
        {
            if (factory == null) return;
            _factories.Add(factory);
            _factories.Sort(static (left, right) => right.Priority.CompareTo(left.Priority));
        }

        /// <summary>대상 GameObject에 맞는 Adapter를 확정합니다.</summary>
        public SurfaceAdapterResolution Resolve(
            GameObject target,
            SurfaceHandle handle,
            out ISurfaceAdapter adapter)
        {
            adapter = null;
            if (target == null) return SurfaceAdapterResolution.NoAdapterFound;

            ISurfaceAdapterFactory best = null;
            bool ambiguous = false;
            foreach (ISurfaceAdapterFactory factory in _factories)
            {
                if (!factory.CanCreate(target)) continue;
                if (best == null) { best = factory; continue; }
                // 목록이 우선순위 내림차순이므로 첫 매칭이 최고 우선순위입니다.
                // 같은 우선순위가 또 매칭되면 결정적으로 고를 수 없습니다.
                if (factory.Priority == best.Priority) { ambiguous = true; }
                break;
            }

            if (best == null) return SurfaceAdapterResolution.NoAdapterFound;
            if (ambiguous) return SurfaceAdapterResolution.AmbiguousCandidates;

            adapter = best.Create(target, handle);
            return SurfaceAdapterResolution.Resolved;
        }
    }
}
