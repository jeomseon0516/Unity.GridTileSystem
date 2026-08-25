using System;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>Tile 내부와 외곽선을 함께 그립니다.</summary>
    [Serializable]
    public sealed class BothDrawPolicy : IHexTileDrawPolicy
    {
        /// <inheritdoc />
        public SurfaceGridDrawMode DrawMode => SurfaceGridDrawMode.Both;
    }
}
