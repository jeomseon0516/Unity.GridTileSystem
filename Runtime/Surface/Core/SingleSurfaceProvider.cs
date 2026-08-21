using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// topology 하나만 제공하는 <see cref="ISurfaceProvider"/>입니다. cross-surface 연결이 없는
    /// 구성에서 기존 단일 Surface 동작을 그대로 유지하며, 테스트에서도 최소 provider로 사용합니다.
    /// </summary>
    public sealed class SingleSurfaceProvider : ISurfaceProvider
    {
        /// <summary>이 provider가 제공하는 유일한 topology입니다.</summary>
        private readonly SurfaceTopology _topology;

        /// <summary>제공할 topology를 지정합니다.</summary>
        public SingleSurfaceProvider(SurfaceTopology topology) =>
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));

        /// <inheritdoc />
        public bool TryGetTopology(SurfaceHandle handle, out SurfaceTopology topology)
        {
            if (handle == _topology.Handle)
            {
                topology = _topology;
                return true;
            }

            topology = null;
            return false;
        }
    }
}
