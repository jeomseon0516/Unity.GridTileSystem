using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>
    /// 기본 제공 factory의 선택 우선순위입니다. 값이 클수록 더 구체적인 입력으로 간주합니다.
    /// 사용자 factory는 이 값 사이나 위를 골라 삽입할 수 있습니다.
    /// </summary>
    public static class SurfaceAdapterPriority
    {
        /// <summary>Terrain은 다른 Surface 입력과 같은 GameObject에 공존하지 않습니다.</summary>
        public const int Terrain = 300;
        /// <summary>SkinnedMeshRenderer는 MeshFilter보다 구체적인 입력입니다.</summary>
        public const int SkinnedMesh = 200;
        /// <summary>MeshFilter는 가장 일반적인 fallback입니다.</summary>
        public const int Mesh = 100;
    }
}
