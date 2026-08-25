using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>
    /// Geometry와 Tile visual을 structured buffer에 보관하고 indexed indirect draw로 제출하는 선택적
    /// 대규모 Backend입니다. Shader는 <c>_SurfaceGridVertices</c>와 <c>_SurfaceGridVisuals</c>를 읽습니다.
    /// </summary>
    public sealed class StructuredBufferSurfaceGridRenderBackend : ISurfaceGridRenderBackend
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct GpuVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 IntrinsicPosition;
            public int TileIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GpuVisual
        {
            public Vector4 Color;
            public uint IsActive;
        }

        private static readonly int VerticesProperty = Shader.PropertyToID("_SurfaceGridVertices");
        private static readonly int VisualsProperty = Shader.PropertyToID("_SurfaceGridVisuals");
        private readonly MaterialPropertyBlock _properties = new();
        private GraphicsBuffer _vertices;
        private GraphicsBuffer _indices;
        private GraphicsBuffer _outlineIndices;
        private GraphicsBuffer _visuals;
        private GraphicsBuffer _arguments;
        private GraphicsBuffer _outlineArguments;
        private GpuVertex[] _vertexData;
        private int _vertexCount;
        private bool _renderingEnabled = true;
        private SurfaceGridDrawMode _drawMode = SurfaceGridDrawMode.Fill;

        /// <summary>현재 GPU에 올라간 vertex 개수를 가져옵니다.</summary>
        public int VertexCount => _vertexCount;
        /// <summary>현재 GPU에 올라간 Fill index 개수를 가져옵니다.</summary>
        public int IndexCount { get; private set; }
        /// <summary>현재 GPU에 올라간 Outline index 개수를 가져옵니다.</summary>
        public int OutlineIndexCount { get; private set; }

        /// <inheritdoc />
        public void ApplyGeometry(SurfaceGridGeometry geometry)
        {
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            ReleaseGeometry();
            _vertexCount = geometry.Positions.Count;
            IndexCount = geometry.TriangleIndices.Count;
            OutlineIndexCount = geometry.OutlineIndices.Count;
            if (_vertexCount == 0) return;

            _vertexData = new GpuVertex[_vertexCount];
            for (int i = 0; i < _vertexData.Length; i++)
            {
                _vertexData[i] = new GpuVertex
                {
                    Position = geometry.Positions[i],
                    Normal = geometry.Normals[i],
                    IntrinsicPosition = geometry.IntrinsicPositions[i],
                    TileIndex = geometry.TileIndices[i]
                };
            }
            _vertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _vertexData.Length, Marshal.SizeOf<GpuVertex>());
            _vertices.SetData(_vertexData);
            _properties.SetBuffer(VerticesProperty, _vertices);

            if (IndexCount > 0)
            {
                int[] indices = new int[IndexCount];
                for (int i = 0; i < indices.Length; i++) indices[i] = geometry.TriangleIndices[i];
                _indices = new GraphicsBuffer(GraphicsBuffer.Target.Index, indices.Length, sizeof(int));
                _arguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);
                _indices.SetData(indices);
                _arguments.SetData(new[] { (uint)IndexCount, 1u, 0u, 0u, 0u });
            }

            if (OutlineIndexCount > 0)
            {
                int[] outline = new int[OutlineIndexCount];
                for (int i = 0; i < outline.Length; i++) outline[i] = geometry.OutlineIndices[i];
                _outlineIndices = new GraphicsBuffer(GraphicsBuffer.Target.Index, outline.Length, sizeof(int));
                _outlineArguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);
                _outlineIndices.SetData(outline);
                _outlineArguments.SetData(new[] { (uint)OutlineIndexCount, 1u, 0u, 0u, 0u });
            }
        }

        /// <summary>
        /// Color/활성 상태를 GPU buffer에 올립니다. <see cref="SurfaceTileVisual.DrawMode"/>는 Tile 단위로
        /// 읽지 않습니다 — 이 Backend의 Draw Mode는 <see cref="SetDrawMode"/>로 지정하는 grid 전체
        /// 단일 값입니다.
        /// </summary>
        public void ApplyVisuals(IReadOnlyList<SurfaceTileVisual> visuals)
        {
            if (visuals == null) throw new ArgumentNullException(nameof(visuals));
            _visuals?.Dispose();
            _visuals = null;
            if (visuals.Count == 0) return;
            GpuVisual[] data = new GpuVisual[visuals.Count];
            for (int i = 0; i < data.Length; i++)
                data[i] = new GpuVisual { Color = visuals[i].Color, IsActive = visuals[i].IsActive ? 1u : 0u };
            _visuals = new GraphicsBuffer(GraphicsBuffer.Target.Structured, data.Length, Marshal.SizeOf<GpuVisual>());
            _visuals.SetData(data);
            _properties.SetBuffer(VisualsProperty, _visuals);
        }

        /// <inheritdoc />
        public void ApplyDeformation(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector3> normals)
        {
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (normals == null) throw new ArgumentNullException(nameof(normals));
            if (positions.Count != _vertexCount || normals.Count != _vertexCount)
                throw new ArgumentException("Deformation arrays must match the uploaded vertex count.");
            if (_vertices == null) return;

            for (int i = 0; i < _vertexData.Length; i++)
            {
                _vertexData[i].Position = positions[i];
                _vertexData[i].Normal = normals[i];
            }
            _vertices.SetData(_vertexData);
        }

        /// <summary>
        /// 현재 buffer를 지정 Material로 indexed indirect draw에 제출합니다. 현재 DrawMode에 따라
        /// Fill(Triangles)/Outline(Lines) buffer 중 하나 또는 둘 다 제출합니다. None은 draw call을
        /// 제출하지 않습니다. 둘 다 그리는 경우
        /// 같은 Material로 두 번(Triangles pass, Lines pass) 호출합니다 — 별도 Outline 전용
        /// Material 계약을 두지 않고 primitive topology 차이만으로 표현을 구분합니다.
        /// </summary>
        public void Draw(Material material, in Bounds bounds, Camera camera = null, int layer = 0)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            if (!_renderingEnabled) return;

            if (_drawMode is SurfaceGridDrawMode.Fill or SurfaceGridDrawMode.Both &&
                _vertices != null && _indices != null && _arguments != null)
            {
                Graphics.DrawProceduralIndirect(
                    material, bounds, MeshTopology.Triangles, _indices, _arguments,
                    0, camera, _properties, ShadowCastingMode.Off, false, layer);
            }

            if (_drawMode is SurfaceGridDrawMode.Outline or SurfaceGridDrawMode.Both &&
                _vertices != null && _outlineIndices != null && _outlineArguments != null)
            {
                Graphics.DrawProceduralIndirect(
                    material, bounds, MeshTopology.Lines, _outlineIndices, _outlineArguments,
                    0, camera, _properties, ShadowCastingMode.Off, false, layer);
            }
        }

        /// <inheritdoc />
        public void SetRenderingEnabled(bool enabled) => _renderingEnabled = enabled;

        /// <summary>
        /// Grid 전체에 적용할 단일 Draw Mode를 바꿉니다. Tile마다 다른 Mode를 섞으려면
        /// <see cref="MeshSurfaceGridRenderBackend"/>를 사용하세요 — 이 GPU Backend는
        /// <see cref="ApplyVisuals"/>가 받는 <see cref="SurfaceTileVisual.DrawMode"/>를 읽지 않고,
        /// 이 메서드로 지정한 grid 전체 단일 모드만 <see cref="Draw"/>에서 사용합니다.
        /// </summary>
        public void SetDrawMode(SurfaceGridDrawMode mode) => _drawMode = mode;

        /// <inheritdoc />
        public void Dispose()
        {
            ReleaseGeometry();
            _visuals?.Dispose();
            _visuals = null;
        }

        private void ReleaseGeometry()
        {
            _vertices?.Dispose();
            _indices?.Dispose();
            _arguments?.Dispose();
            _outlineIndices?.Dispose();
            _outlineArguments?.Dispose();
            _vertices = null;
            _indices = null;
            _arguments = null;
            _outlineIndices = null;
            _outlineArguments = null;
            _vertexData = null;
            _vertexCount = 0;
            IndexCount = 0;
            OutlineIndexCount = 0;
        }
    }
}
