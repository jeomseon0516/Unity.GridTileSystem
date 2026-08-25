using System;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>Tile의 논리·입력 상태는 유지하면서 시각화만 그리지 않습니다.</summary>
    [Serializable]
    public sealed class NoneDrawPolicy : IHexTileDrawPolicy
    {
        /// <inheritdoc />
        public SurfaceGridDrawMode DrawMode => SurfaceGridDrawMode.None;
    }
}
