using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>공통 Unity Mesh API만 사용하여 Built-in/URP/HDRP에서 동작하는 기본 Render Backend입니다.</summary>
    public sealed class MeshSurfaceGridRenderBackend : ISurfaceGridRenderBackend
    {
        private const string DefaultMaterialResourceName = "SurfaceGridDepthBiased";
        /// <summary>생성 Mesh를 표시하는 대상 MeshFilter입니다.</summary>
        private readonly MeshFilter _meshFilter;
        /// <summary>표현 활성 상태를 제어하는 대상 MeshRenderer입니다.</summary>
        private readonly MeshRenderer _meshRenderer;
        /// <summary>Backend가 생성하고 파괴할 runtime Mesh입니다.</summary>
        private Mesh _mesh;
        /// <summary>마지막 Geometry의 vertex별 Logical Tile index입니다.</summary>
        private int[] _tileIndices = Array.Empty<int>();
        /// <summary>Fill submesh로 재적용할 수 있도록 보존한 마지막 Triangle index buffer입니다.</summary>
        private int[] _triangleIndices = Array.Empty<int>();
        /// <summary>Outline submesh로 재적용할 수 있도록 보존한 마지막 Line index buffer입니다.</summary>
        private int[] _outlineIndices = Array.Empty<int>();
        /// <summary>Renderer에 사용자 Material이 없을 때만 할당하는 패키지 내장 공유 Material입니다.</summary>
        private Material _defaultMaterial;

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
            _triangleIndices = Copy(geometry.TriangleIndices);
            _outlineIndices = Copy(geometry.OutlineIndices);
            _tileIndices = Copy(geometry.TileIndices);
            _mesh.vertices = positions;
            _mesh.normals = normals;
            _mesh.uv = intrinsic;
            // Tile별 Draw Mode를 아직 모르므로 Fill 전체로 시작합니다. 뒤이어 호출되는 ApplyVisuals가
            // 실제 Tile별 표현으로 즉시 덮어씁니다(Controller의 Bake 순서가 항상 그렇습니다).
            _mesh.subMeshCount = 1;
            _mesh.SetIndices(_triangleIndices, MeshTopology.Triangles, 0);
            SetSubMeshMaterialCount(1);
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
            ApplyDrawModePartition(visuals);
        }

        /// <summary>
        /// Tile마다 다른 Draw Mode를 한 Mesh 안에서 섞어 그리도록 Fill/Outline index를 다시 나눕니다.
        /// Triangle과 Outline Edge는 항상 하나의 Tile Region 안에서만 만들어지므로(clipping이 Tile마다
        /// 독립적인 vertex를 새로 발급), 각 Triangle/Edge의 첫 vertex가 가리키는 Tile index로 그 전체가
        /// 속한 Tile을 알 수 있습니다.
        /// </summary>
        private void ApplyDrawModePartition(IReadOnlyList<SurfaceTileVisual> visuals)
        {
            List<int> fillIndices = new(_triangleIndices.Length);
            for (int i = 0; i < _triangleIndices.Length; i += 3)
            {
                SurfaceGridDrawMode mode = visuals[_tileIndices[_triangleIndices[i]]].DrawMode;
                if (mode is SurfaceGridDrawMode.Outline or SurfaceGridDrawMode.None) continue;
                fillIndices.Add(_triangleIndices[i]);
                fillIndices.Add(_triangleIndices[i + 1]);
                fillIndices.Add(_triangleIndices[i + 2]);
            }

            List<int> outlineIndices = new(_outlineIndices.Length);
            for (int i = 0; i < _outlineIndices.Length; i += 2)
            {
                SurfaceGridDrawMode mode = visuals[_tileIndices[_outlineIndices[i]]].DrawMode;
                if (mode is SurfaceGridDrawMode.Fill or SurfaceGridDrawMode.None) continue;
                outlineIndices.Add(_outlineIndices[i]);
                outlineIndices.Add(_outlineIndices[i + 1]);
            }

            if (fillIndices.Count > 0 && outlineIndices.Count > 0)
            {
                _mesh.subMeshCount = 2;
                _mesh.SetIndices(fillIndices, MeshTopology.Triangles, 0);
                _mesh.SetIndices(outlineIndices, MeshTopology.Lines, 1);
                SetSubMeshMaterialCount(2);
            }
            else if (outlineIndices.Count > 0)
            {
                _mesh.subMeshCount = 1;
                _mesh.SetIndices(outlineIndices, MeshTopology.Lines, 0);
                SetSubMeshMaterialCount(1);
            }
            else
            {
                _mesh.subMeshCount = 1;
                _mesh.SetIndices(fillIndices, MeshTopology.Triangles, 0);
                SetSubMeshMaterialCount(1);
            }
        }

        /// <inheritdoc />
        public void ApplyDeformation(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector3> normals)
        {
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (normals == null) throw new ArgumentNullException(nameof(normals));
            if (_mesh == null) return;
            // index buffer와 Tile 구성은 그대로 두고 vertex stream만 교체하므로 Mesh.Clear()를 호출하지
            // 않습니다. 길이가 달라지면 기존 triangle index가 범위를 벗어나므로 즉시 거부합니다.
            if (positions.Count != _mesh.vertexCount || normals.Count != _mesh.vertexCount)
            {
                throw new ArgumentException(
                    $"Deformation must cover exactly {_mesh.vertexCount} vertices to keep the existing index buffer valid.",
                    nameof(positions));
            }

            _mesh.vertices = Copy(positions);
            _mesh.normals = Copy(normals);
            _mesh.RecalculateBounds();
        }

        /// <inheritdoc />
        public void SetRenderingEnabled(bool enabled) => _meshRenderer.enabled = enabled;

        /// <summary>
        /// 현재 할당된 Material을 유지한 채 submesh 개수에 맞춰 배열 길이만 맞춥니다. 별도
        /// Outline 전용 Material 필드를 두지 않고 같은 vertex color Material을 두 submesh가
        /// 공유합니다 — Fill/Outline 차이는 primitive topology(Triangles/Lines)만으로 표현됩니다.
        /// </summary>
        private void SetSubMeshMaterialCount(int count)
        {
            Material[] current = _meshRenderer.sharedMaterials;
            Material material = current.Length > 0 ? current[0] : null;
            if (material == null) material = EnsureDefaultMaterial();
            if (current.Length == count && current.Length > 0 && current[0] == material) return;
            Material[] updated = new Material[count];
            for (int i = 0; i < count; i++) updated[i] = material;
            _meshRenderer.sharedMaterials = updated;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_mesh != null)
            {
                if (_meshFilter != null && _meshFilter.sharedMesh == _mesh) _meshFilter.sharedMesh = null;
                DestroyOwnedObject(_mesh);
                _mesh = null;
            }
            DisposeDefaultMaterial();
            _tileIndices = Array.Empty<int>();
            _triangleIndices = Array.Empty<int>();
            _outlineIndices = Array.Empty<int>();
        }

        /// <summary>패키지 내장 depth-bias 기본 Material을 지연 로드합니다.</summary>
        private Material EnsureDefaultMaterial()
        {
            if (_defaultMaterial != null) return _defaultMaterial;
            Material template = Resources.Load<Material>(DefaultMaterialResourceName);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"Built-in surface grid material resource '{DefaultMaterialResourceName}' was not found.");
            }
            _defaultMaterial = template;
            return _defaultMaterial;
        }

        /// <summary>Backend가 할당한 슬롯만 비웁니다. Resources 공유 Material은 파괴하지 않습니다.</summary>
        private void DisposeDefaultMaterial()
        {
            if (_defaultMaterial == null) return;
            if (_meshRenderer != null)
            {
                Material[] materials = _meshRenderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != _defaultMaterial) continue;
                    materials[i] = null;
                    changed = true;
                }
                if (changed) _meshRenderer.sharedMaterials = materials;
            }
            _defaultMaterial = null;
        }

        private static void DestroyOwnedObject(UnityEngine.Object ownedObject)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(ownedObject);
            else UnityEngine.Object.DestroyImmediate(ownedObject);
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
