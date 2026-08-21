using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Query
{
    /// <summary>
    /// Unity Physics의 공간 인덱스로 후보를 모읍니다. 등록이 없는 모델에서 "게임에 실제로 존재하는
    /// 표면"을 나타내는 가장 자연스러운 신호가 Collider이므로 이를 기본 후보 기준으로 사용합니다.
    /// 장식용 Mesh가 Grid에 섞이는 것도 자연히 줄어듭니다.
    /// </summary>
    /// <remarks>
    /// Collider가 없는 표면은 이 구현으로 찾을 수 없습니다. 그런 표면이 필요하면 캐시된 geometry
    /// 인덱스 구현으로 교체하며, 사용자 API는 바뀌지 않습니다. 후보를 좁히는 데만 Collider를 쓰고
    /// 실제 seed 위치는 원본 geometry로 계산하므로 Collider 정확도가 결과를 좌우하지 않습니다.
    /// </remarks>
    public sealed class PhysicsSurfaceCandidateSource : ISurfaceCandidateSource
    {
        /// <summary>재사용하는 overlap 결과 버퍼입니다.</summary>
        private readonly Collider[] _buffer;

        /// <summary>한 번에 검사할 최대 Collider 수를 지정합니다.</summary>
        public PhysicsSurfaceCandidateSource(int maximumCandidates = 64) =>
            _buffer = new Collider[Mathf.Max(1, maximumCandidates)];

        /// <inheritdoc />
        public int Collect(in Vector3 center, float radius, LayerMask layerMask, List<GameObject> results)
        {
            if (results == null) return 0;
            results.Clear();
            int count = Physics.OverlapSphereNonAlloc(
                center, radius, _buffer, layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider collider = _buffer[i];
                if (collider == null) continue;
                GameObject candidate = collider.gameObject;
                if (!results.Contains(candidate)) results.Add(candidate);
            }
            return results.Count;
        }
    }
}
