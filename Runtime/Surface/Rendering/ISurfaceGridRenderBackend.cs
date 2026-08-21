using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>Geometry를 재생성하지 않고 변경할 수 있는 Logical Tile의 시각 상태입니다.</summary>
    public readonly struct SurfaceTileVisual
    {
        /// <summary>Tile vertex에 적용할 색상을 가져옵니다.</summary>
        public Color Color { get; }
        /// <summary>Tile을 렌더링 가능한 상태로 표시할지 가져옵니다.</summary>
        public bool IsActive { get; }

        /// <summary>Tile 색상과 활성 상태를 생성합니다.</summary>
        public SurfaceTileVisual(in Color color, bool isActive)
        {
            Color = color;
            IsActive = isActive;
        }
    }

    /// <summary>Surface Grid Core와 실제 Draw 구현 사이의 렌더 파이프라인 비종속 수명 계약입니다.</summary>
    public interface ISurfaceGridRenderBackend : IDisposable
    {
        /// <summary>Topology/Region 변경으로 새로 생성된 Geometry snapshot을 적용합니다.</summary>
        void ApplyGeometry(SurfaceGridGeometry geometry);
        /// <summary>Geometry를 유지하면서 Logical Tile의 색상과 활성 상태를 적용합니다.</summary>
        void ApplyVisuals(IReadOnlyList<SurfaceTileVisual> visuals);
        /// <summary>Backend가 소유한 표현의 활성 상태를 변경합니다.</summary>
        void SetRenderingEnabled(bool enabled);
    }
}
