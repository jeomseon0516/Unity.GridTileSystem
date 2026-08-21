using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>공통 Unity Mesh API만 사용하여 Built-in/URP/HDRP에서 동작하는 기본 Render Backend입니다.</summary>
    public sealed class MeshSurfaceGridRenderBackend : ISurfaceGridRenderBackend
    {
        /// <summary>생성 Mesh를 표시하는 대상 MeshFilter입니다.</summary>
        private readonly MeshFilter _meshFilter;
        /// <summary>표현 활성 상태를 제어하는 대상 MeshRenderer입니다.</summary>
        private readonly MeshRenderer _meshRenderer;
        /// <summary>Backend가 생성하고 파괴할 runtime Mesh입니다.</summary>
        private Mesh _mesh;
        /// <summary>마지막 Geometry의 vertex별 Logical Tile index입니다.</summary>
        private int[] _tileIndices = Array.Empty<int>();

        /// <summary>Backend가 Geometry를 적용할 Unity 공통 렌더 컴포넌트를 지정합니다.</summary>
        public MeshSurfaceGridRenderBackend(MeshFilter meshFilter, MeshRenderer meshRenderer)
        {
            _meshFilter = meshFilter != null ? meshFilter : throw new ArgumentNullException(nameof(meshFilter));
            _meshRenderer = meshRenderer != null ? meshRenderer : throw new ArgumentNullException(nameof(meshRenderer));
        }

        /// <inheritdoc />
        public void ApplyGeometry(SurfaceGridGeometry geometry)
        {
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            EnsureMesh();
            _mesh.Clear();
            _mesh.indexFormat = geometry.Positions.Count > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;

            Vector3[] positions = Copy(geometry.Positions);
            Vector3[] normals = Copy(geometry.Normals);
            Vector2[] intrinsic = Copy(geometry.IntrinsicPositions);
            int[] triangles = Copy(geometry.TriangleIndices);
            _tileIndices = Copy(geometry.TileIndices);
            _mesh.vertices = positions;
            _mesh.normals = normals;
            _mesh.uv = intrinsic;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
            _meshFilter.sharedMesh = _mesh;
        }

        /// <inheritdoc />
        public void ApplyVisuals(IReadOnlyList<SurfaceTileVisual> visuals)
        {
            if (visuals == null) throw new ArgumentNullException(nameof(visuals));
            if (_mesh == null || _tileIndices.Length == 0) return;

            Color[] colors = new Color[_tileIndices.Length];
            for (int vertex = 0; vertex < colors.Length; vertex++)
            {
                int tileIndex = _tileIndices[vertex];
                if ((uint)tileIndex >= (uint)visuals.Count)
                    throw new ArgumentException("Visual list does not cover every geometry tile index.", nameof(visuals));
                SurfaceTileVisual visual = visuals[tileIndex];
                Color color = visual.Color;
                // 기본 Backend는 active 상태를 alpha 0/원래 alpha로 인코딩합니다. Material이 vertex color를
                // 소비하는 방식은 사용자 선택이며 파이프라인별 Backend는 다른 표현 전략을 사용할 수 있습니다.
                if (!visual.IsActive) color.a = 0f;
                colors[vertex] = color;
            }
            _mesh.colors = colors;
        }

        /// <inheritdoc />
        public void SetRenderingEnabled(bool enabled) => _meshRenderer.enabled = enabled;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_mesh == null) return;
            if (_meshFilter != null && _meshFilter.sharedMesh == _mesh) _meshFilter.sharedMesh = null;
            if (Application.isPlaying) UnityEngine.Object.Destroy(_mesh);
            else UnityEngine.Object.DestroyImmediate(_mesh);
            _mesh = null;
            _tileIndices = Array.Empty<int>();
        }

        /// <summary>필요할 때 Backend 소유 runtime Mesh를 생성합니다.</summary>
        private void EnsureMesh()
        {
            if (_mesh != null) return;
            _mesh = new Mesh { name = "Surface Grid Runtime Mesh" };
            _mesh.MarkDynamic();
        }

        /// <summary>읽기 전용 목록을 Unity Mesh API가 요구하는 소유 배열로 복사합니다.</summary>
        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            T[] result = new T[source.Count];
            for (int i = 0; i < result.Length; i++) result[i] = source[i];
            return result;
        }
    }
}
