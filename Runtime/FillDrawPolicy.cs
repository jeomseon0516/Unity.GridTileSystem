using System;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>Tile 내부를 채워 그립니다.</summary>
    [Serializable]
    public sealed class FillDrawPolicy : IHexTileDrawPolicy
    {
        /// <inheritdoc />
        public SurfaceGridDrawMode DrawMode => SurfaceGridDrawMode.Fill;
    }
}
