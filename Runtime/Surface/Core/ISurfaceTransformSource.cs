using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Surface local 좌표를 월드로 옮기는 변환을 handle로 조회합니다. chart가 여러 Surface에 걸치면
    /// Surface마다 변환이 다르므로, 하나의 출력 공간으로 모으려면 이 계약이 필요합니다.
    /// </summary>
    public interface ISurfaceTransformSource
    {
        /// <summary>지정한 Surface의 local-to-world 변환을 가져옵니다.</summary>
        bool TryGetSurfaceToWorld(SurfaceHandle surface, out Matrix4x4 surfaceToWorld);
    }
}
