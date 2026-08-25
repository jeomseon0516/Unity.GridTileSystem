using Jeomseon.Unity.GridTileSystem.Surface.Rendering;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>개별 Tile 또는 Grid 기본값이 사용할 표현 방식을 정의합니다.</summary>
    public interface IHexTileDrawPolicy
    {
        /// <summary>이 Policy가 적용할 Fill/Outline/Both 표현 방식을 가져옵니다.</summary>
        SurfaceGridDrawMode DrawMode { get; }
    }
}
