using System;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>Tile 외곽선만 그립니다.</summary>
    [Serializable]
    public sealed class OutlineDrawPolicy : IHexTileDrawPolicy
    {
        /// <inheritdoc />
        public SurfaceGridDrawMode DrawMode => SurfaceGridDrawMode.Outline;
    }
}
