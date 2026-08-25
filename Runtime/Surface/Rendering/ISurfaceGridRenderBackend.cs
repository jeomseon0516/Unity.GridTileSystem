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
        /// <summary>이 Tile을 그리지 않거나 Fill/Outline/Both로 표현할 방식을 가져옵니다.</summary>
        public SurfaceGridDrawMode DrawMode { get; }

        /// <summary>Tile 색상, 활성 상태와 Draw Mode를 생성합니다.</summary>
        public SurfaceTileVisual(in Color color, bool isActive, SurfaceGridDrawMode drawMode = SurfaceGridDrawMode.Fill)
        {
            Color = color;
            IsActive = isActive;
            DrawMode = drawMode;
        }
    }

    /// <summary>
    /// Backend가 Geometry를 그리는 방식입니다. 색상·활성 상태와 달리 Geometry를 다시 만들지 않고
    /// 전환할 수 있는 순수 표현 정책이며, 게임 상태(예: 유닛 배치 모드)에 따라 런타임에 자유롭게
    /// 바꿀 수 있도록 설계했습니다.
    /// </summary>
    public enum SurfaceGridDrawMode
    {
        /// <summary>Tile 내부를 채운 삼각형으로 그립니다. 기본값입니다.</summary>
        Fill,
        /// <summary>Tile 외곽 Hex 윤곽선만 그립니다. 내부는 그리지 않아 아래 표면이 그대로 드러납니다.</summary>
        Outline,
        /// <summary>채운 삼각형과 외곽선을 함께 그립니다.</summary>
        Both,
        /// <summary>Tile의 Fill과 Outline을 모두 그리지 않습니다.</summary>
        None
    }

    /// <summary>Surface Grid Core와 실제 Draw 구현 사이의 렌더 파이프라인 비종속 수명 계약입니다.</summary>
    public interface ISurfaceGridRenderBackend : IDisposable
    {
        /// <summary>Topology/Region 변경으로 새로 생성된 Geometry snapshot을 적용합니다.</summary>
        void ApplyGeometry(SurfaceGridGeometry geometry);
        /// <summary>
        /// Geometry를 유지하면서 Logical Tile의 색상·활성 상태·Draw Mode를 적용합니다. 목록은 Tile
        /// index 순서와 일치해야 하며, 이미 Fill/Outline index를 모두 보유한 마지막 Geometry를 그대로
        /// 재사용하므로 Geometry를 다시 만들지 않고 즉시 반영됩니다. Draw Mode를 Tile 단위로 섞어
        /// 그리는지는 Backend마다 다릅니다(<see cref="MeshSurfaceGridRenderBackend"/>는 지원, GPU
        /// structured buffer Backend는 grid 전체 단일 모드만 지원).
        /// </summary>
        void ApplyVisuals(IReadOnlyList<SurfaceTileVisual> visuals);
        /// <summary>
        /// Tile 구성과 index buffer를 유지한 채 변형된 vertex 위치와 법선만 적용합니다.
        /// Skinned Surface처럼 topology는 그대로이고 정점만 움직이는 입력에서 매 프레임 호출하는
        /// 경로이며, 두 목록의 길이는 마지막으로 적용한 Geometry의 vertex 수와 같아야 합니다.
        /// </summary>
        void ApplyDeformation(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector3> normals);
        /// <summary>Backend가 소유한 표현의 활성 상태를 변경합니다.</summary>
        void SetRenderingEnabled(bool enabled);
    }
}
