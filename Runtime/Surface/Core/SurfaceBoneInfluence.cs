namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>Surface 위 한 지점이 특정 bone에게 받는 가중치입니다.</summary>
    public readonly struct SurfaceBoneInfluence
    {
        /// <summary>영향을 주는 bone의 index를 가져옵니다.</summary>
        public int BoneIndex { get; }
        /// <summary>해당 bone의 정규화된 가중치를 가져옵니다.</summary>
        public float Weight { get; }

        /// <summary>bone index와 가중치로 influence를 생성합니다.</summary>
        public SurfaceBoneInfluence(int boneIndex, float weight)
        {
            BoneIndex = boneIndex;
            Weight = weight;
        }
    }
}
