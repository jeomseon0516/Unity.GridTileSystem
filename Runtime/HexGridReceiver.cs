using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Services;
using Jeomseon.Unity.GridTileSystem.Surface.Adapters;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using Jeomseon.Unity.GridTileSystem.Surface.Rendering;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>하나의 Static Mesh 또는 Terrain Surface와 그 위의 독립 Grid 상태를 정의합니다.</summary>
    [Serializable]
    public sealed class HexGridReceiver
    {
        /// <summary>Topology를 구축할 Unity Surface 입력 종류입니다.</summary>
        [SerializeField] private SurfaceReceiverKind surfaceKind;
        /// <summary>Topology와 barycentric binding의 원본 readable Static Mesh입니다.</summary>
        [SerializeField] private MeshFilter sourceMeshFilter;
        /// <summary>원본 Surface hit의 Triangle identity를 제공하는 MeshCollider입니다.</summary>
        [SerializeField] private MeshCollider surfaceCollider;
        /// <summary>Terrain 입력과 local transform을 제공하는 컴포넌트입니다.</summary>
        [SerializeField] private Terrain sourceTerrain;
        /// <summary>Terrain picking을 제공하는 Collider입니다.</summary>
        [SerializeField] private TerrainCollider terrainCollider;
        /// <summary>bind pose topology와 bone 변형을 제공하는 SkinnedMeshRenderer입니다.</summary>
        [SerializeField] private SkinnedMeshRenderer sourceSkinnedRenderer;
        /// <summary>생성된 Grid Geometry를 받을 원본과 다른 MeshFilter입니다.</summary>
        [SerializeField] private MeshFilter outputMeshFilter;
        /// <summary>Grid Geometry를 표시할 공통 MeshRenderer입니다.</summary>
        [SerializeField] private MeshRenderer outputMeshRenderer;
        /// <summary>Grid local chart를 시작할 원본 Triangle index입니다.</summary>
        [SerializeField, Min(0)] private int seedTriangleIndex;
        /// <summary>Seed Triangle 내부 위치를 나타내는 barycentric (u,v,w)입니다.</summary>
        [SerializeField] private Vector3 seedBarycentric = new(1f / 3f, 1f / 3f, 1f / 3f);
        /// <summary>직렬화되어 사용자 상태와 이벤트를 보존하는 Logical Tile 목록입니다.</summary>
        [SerializeField, HideInInspector] private List<HexTile> tiles = new();

        [NonSerialized] private HexTileStore _tileStore;
        [NonSerialized] private HexTilePicker _tilePicker;
        [NonSerialized] private ISurfaceGridRenderBackend _renderBackend;
        [NonSerialized] private SurfaceTopology _topology;
        [NonSerialized] private SurfaceGrid _surfaceGrid;
        [NonSerialized] private SurfaceSkinBinding _skinBinding;
        [NonSerialized] private Matrix4x4[] _skinningMatrices;
        [NonSerialized] private Vector3[] _deformedPositions;
        [NonSerialized] private Vector3[] _deformedNormals;
        [NonSerialized] private float _surfaceOffset;

        /// <summary>Receiver의 Surface 입력 종류를 가져옵니다.</summary>
        public SurfaceReceiverKind SurfaceKind => surfaceKind;
        /// <summary>원본 Static Mesh 컴포넌트를 가져옵니다.</summary>
        public MeshFilter SourceMeshFilter => sourceMeshFilter;
        /// <summary>원본 Terrain 컴포넌트를 가져옵니다.</summary>
        public Terrain SourceTerrain => sourceTerrain;
        /// <summary>원본 SkinnedMeshRenderer를 가져옵니다.</summary>
        public SkinnedMeshRenderer SourceSkinnedRenderer => sourceSkinnedRenderer;
        /// <summary>이 Receiver가 매 프레임 변형을 따라가야 하는지 가져옵니다.</summary>
        internal bool IsDeformable => surfaceKind == SurfaceReceiverKind.SkinnedMesh && _skinBinding != null;
        /// <summary>원본 Mesh와 같은 Triangle identity를 제공하는 Collider를 가져옵니다.</summary>
        public Collider SurfaceCollider => GetSurfaceCollider();
        /// <summary>현재 Receiver의 직렬화된 Tile 상태를 가져옵니다.</summary>
        public IReadOnlyList<HexTile> Tiles => tiles;
        /// <summary>현재 Receiver의 Tile 개수를 가져옵니다.</summary>
        public int TileCount => tiles.Count;
        /// <summary>마지막으로 성공한 intrinsic Grid snapshot을 가져옵니다.</summary>
        public SurfaceGrid SurfaceGrid => _surfaceGrid;

        /// <summary>Tile 시각 상태가 바뀌어 Controller가 Backend 갱신을 요청해야 할 때 발생합니다.</summary>
        internal event Action VisualsChanged;

        /// <summary>Unity 직렬화와 Inspector 목록 추가에 사용하는 빈 Receiver를 생성합니다.</summary>
        public HexGridReceiver()
        {
        }

        /// <summary>코드에서 Receiver 구성을 생성합니다.</summary>
        public HexGridReceiver(
            MeshFilter sourceMeshFilter,
            MeshCollider surfaceCollider,
            MeshFilter outputMeshFilter = null,
            MeshRenderer outputMeshRenderer = null)
        {
            surfaceKind = SurfaceReceiverKind.StaticMesh;
            this.sourceMeshFilter = sourceMeshFilter;
            this.surfaceCollider = surfaceCollider;
            this.outputMeshFilter = outputMeshFilter;
            this.outputMeshRenderer = outputMeshRenderer;
        }

        /// <summary>코드에서 Terrain Receiver 구성을 생성합니다.</summary>
        public HexGridReceiver(
            Terrain sourceTerrain,
            TerrainCollider terrainCollider,
            MeshFilter outputMeshFilter = null,
            MeshRenderer outputMeshRenderer = null)
        {
            surfaceKind = SurfaceReceiverKind.Terrain;
            this.sourceTerrain = sourceTerrain;
            this.terrainCollider = terrainCollider;
            this.outputMeshFilter = outputMeshFilter;
            this.outputMeshRenderer = outputMeshRenderer;
        }

        internal bool IsConfigured => surfaceKind switch
        {
            SurfaceReceiverKind.StaticMesh => sourceMeshFilter != null && sourceMeshFilter.sharedMesh != null &&
                                              surfaceCollider != null && surfaceCollider.sharedMesh == sourceMeshFilter.sharedMesh &&
                                              HasValidOutputPair,
            SurfaceReceiverKind.Terrain => sourceTerrain != null && sourceTerrain.terrainData != null &&
                                           terrainCollider != null && terrainCollider.terrainData == sourceTerrain.terrainData &&
                                           HasValidOutputPair,
            // Skinned Surface는 변형 중 Collider가 bind pose와 어긋나므로 picking을 필수로 두지 않습니다.
            SurfaceReceiverKind.SkinnedMesh => sourceSkinnedRenderer != null &&
                                               sourceSkinnedRenderer.sharedMesh != null &&
                                               HasValidOutputPair,
            _ => false
        };

        internal bool HasValidOutputPair => (outputMeshFilter == null) == (outputMeshRenderer == null) &&
                                            (outputMeshFilter == null || sourceMeshFilter == null || !ReferenceEquals(sourceMeshFilter, outputMeshFilter));

        internal void EnsureStore()
        {
            if (_tileStore != null) return;
            tiles ??= new List<HexTile>();
            _tileStore = new HexTileStore(tiles);
            _tileStore.TileVisualsChanged += HandleVisualsChanged;
            _tileStore.RebuildLookup();
        }

        internal void Bake(float tileRadius, in SurfacePatchBuildSettings patchSettings, float surfaceOffset)
        {
            EnsureStore();
            _topology = BuildTopology();
            SurfacePoint seed = new(_topology.Handle, seedTriangleIndex, seedBarycentric);
            if (!seed.IsValid) throw new InvalidOperationException("Seed barycentric coordinates must be non-negative and sum to one.");
            _surfaceGrid = SurfaceGridBuilder.Build(_topology, seed, tileRadius, patchSettings);
            Transform surfaceTransform = GetSurfaceTransform();
            _tileStore.Bake(_topology, _surfaceGrid, surfaceTransform);
            Collider surfaceCollider = GetSurfaceCollider();
            // Skinned Receiver는 Collider가 선택 사항이므로 없으면 picking 없이 논리 Grid만 유지합니다.
            _tilePicker = surfaceCollider != null
                ? new HexTilePicker(surfaceCollider, _topology, _surfaceGrid, _tileStore)
                : null;
            _surfaceOffset = surfaceOffset;
            _skinBinding = null;
            EnsureBackend();
            if (_renderBackend == null) return;
            Matrix4x4 surfaceToOutput = outputMeshFilter.transform.worldToLocalMatrix * surfaceTransform.localToWorldMatrix;
            SurfaceGridGeometry geometry = SurfaceGridGeometryBuilder.Build(_topology, _surfaceGrid, surfaceToOutput, surfaceOffset);
            _renderBackend.ApplyGeometry(geometry);
            if (surfaceKind == SurfaceReceiverKind.SkinnedMesh) BuildSkinBinding(geometry);
        }

        internal bool TryPick(in Ray ray, LayerMask layerMask, out RaycastHit hit, out HexTile tile)
        {
            hit = default;
            tile = null;
            if (_tilePicker == null) return false;
            bool picked = _tilePicker.TryPick(ray, layerMask, out (bool, RaycastHit) result, out tile);
            hit = result.Item2;
            return picked;
        }

        /// <summary>
        /// bind pose binding과 현재 bone 자세로 vertex 위치·법선만 다시 계산해 Backend에 적용합니다.
        /// topology, Patch, Tile 구성과 index buffer는 그대로 유지하므로 Geometry를 재생성하지 않습니다.
        /// </summary>
        internal void UpdateDeformation()
        {
            if (!IsDeformable || _renderBackend == null || outputMeshFilter == null) return;
            if (sourceSkinnedRenderer == null) return;

            Matrix4x4 targetFromWorld = outputMeshFilter.transform.worldToLocalMatrix;
            SkinnedMeshTopologyFactory.GetSkinningMatrices(sourceSkinnedRenderer, targetFromWorld, _skinningMatrices);
            _skinBinding.Evaluate(_skinningMatrices, _deformedPositions, _deformedNormals);
            if (_surfaceOffset > 0f)
            {
                // Geometry 생성과 같은 규칙으로 z-fighting 회피 offset을 다시 적용합니다.
                for (int i = 0; i < _deformedPositions.Length; i++)
                    _deformedPositions[i] += _deformedNormals[i] * _surfaceOffset;
            }

            _renderBackend.ApplyDeformation(_deformedPositions, _deformedNormals);
        }

        internal void ApplyVisuals()
        {
            if (_renderBackend == null || _surfaceGrid == null) return;
            SurfaceTileVisual[] visuals = new SurfaceTileVisual[tiles.Count];
            for (int i = 0; i < visuals.Length; i++) visuals[i] = new SurfaceTileVisual(tiles[i].Color, tiles[i].IsActive);
            _renderBackend.ApplyVisuals(visuals);
        }

        internal void SetRenderingEnabled(bool enabled)
        {
            EnsureBackend();
            _renderBackend?.SetRenderingEnabled(enabled);
        }

        internal void SetTileActive(in AxialCoordinates coordinates, bool isActive)
        {
            EnsureStore();
            _tileStore.SetActive(coordinates, isActive);
        }

        internal void Clear()
        {
            EnsureStore();
            _tileStore.Clear();
            _topology = null;
            _surfaceGrid = null;
            _tilePicker = null;
            ReleaseSkinBinding();
            ReleaseBackend();
        }

        internal void Release()
        {
            if (_tileStore != null) _tileStore.TileVisualsChanged -= HandleVisualsChanged;
            ReleaseBackend();
            _tileStore = null;
            _tilePicker = null;
            _topology = null;
            _surfaceGrid = null;
            ReleaseSkinBinding();
        }

        internal bool Validate(int receiverIndex, UnityEngine.Object context)
        {
            if (surfaceKind == SurfaceReceiverKind.Terrain) return ValidateTerrain(receiverIndex, context);
            if (surfaceKind == SurfaceReceiverKind.SkinnedMesh) return ValidateSkinned(receiverIndex, context);
            if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
            {
                Debug.LogError($"Receiver {receiverIndex} requires a source MeshFilter with a Mesh.", context);
                return false;
            }
            if (!sourceMeshFilter.sharedMesh.isReadable)
            {
                Debug.LogError($"Receiver {receiverIndex} source Mesh must have Read/Write Enabled.", sourceMeshFilter);
                return false;
            }
            if (surfaceCollider == null || surfaceCollider.sharedMesh != sourceMeshFilter.sharedMesh)
            {
                Debug.LogError($"Receiver {receiverIndex} MeshCollider must reference the same Mesh as its source MeshFilter.", context);
                return false;
            }
            int triangleCount = sourceMeshFilter.sharedMesh.triangles.Length / 3;
            if (seedTriangleIndex < 0 || seedTriangleIndex >= triangleCount)
            {
                Debug.LogError($"Receiver {receiverIndex} seed triangle index must be in [0, {triangleCount - 1}].", context);
                return false;
            }
            if (!HasValidOutputPair)
            {
                Debug.LogError($"Receiver {receiverIndex} must assign both output components or neither, and source/output MeshFilters must differ.", context);
                return false;
            }
            return true;
        }

        /// <summary>Skinned Receiver 참조, readable Mesh, seed와 output 계약을 검사합니다.</summary>
        private bool ValidateSkinned(int receiverIndex, UnityEngine.Object context)
        {
            if (sourceSkinnedRenderer == null || sourceSkinnedRenderer.sharedMesh == null)
            {
                Debug.LogError($"Receiver {receiverIndex} requires a SkinnedMeshRenderer with a shared Mesh.", context);
                return false;
            }
            if (!sourceSkinnedRenderer.sharedMesh.isReadable)
            {
                Debug.LogError(
                    $"Receiver {receiverIndex} skinned Mesh must have Read/Write Enabled.", sourceSkinnedRenderer);
                return false;
            }
            int triangleCount = sourceSkinnedRenderer.sharedMesh.triangles.Length / 3;
            if (seedTriangleIndex < 0 || seedTriangleIndex >= triangleCount)
            {
                Debug.LogError($"Receiver {receiverIndex} seed triangle index must be in [0, {triangleCount - 1}].", context);
                return false;
            }
            if (!HasValidOutputPair)
            {
                Debug.LogError($"Receiver {receiverIndex} must assign both output components or neither.", context);
                return false;
            }
            return true;
        }

        /// <summary>현재 입력 종류에 맞는 topology를 구축합니다.</summary>
        private SurfaceTopology BuildTopology() => surfaceKind switch
        {
            SurfaceReceiverKind.StaticMesh => MeshTopologyFactory.BuildTopology(sourceMeshFilter.sharedMesh),
            SurfaceReceiverKind.Terrain => TerrainTopologyFactory.BuildTopology(sourceTerrain.terrainData),
            SurfaceReceiverKind.SkinnedMesh => SkinnedMeshTopologyFactory.BuildTopology(sourceSkinnedRenderer.sharedMesh),
            _ => throw new ArgumentOutOfRangeException(nameof(surfaceKind))
        };

        /// <summary>현재 Surface의 local-to-world 기준 Transform을 반환합니다.</summary>
        private Transform GetSurfaceTransform() => surfaceKind switch
        {
            SurfaceReceiverKind.Terrain => sourceTerrain.transform,
            SurfaceReceiverKind.SkinnedMesh => sourceSkinnedRenderer.transform,
            _ => sourceMeshFilter.transform
        };

        /// <summary>현재 Surface를 직접 raycast할 Collider를 반환합니다.</summary>
        private Collider GetSurfaceCollider() => surfaceKind switch
        {
            SurfaceReceiverKind.Terrain => terrainCollider,
            // Skinned Receiver의 Collider는 선택 사항이며, 지정하면 bind pose 기준으로 picking합니다.
            SurfaceReceiverKind.SkinnedMesh => surfaceCollider,
            _ => surfaceCollider
        };

        /// <summary>Terrain Receiver 참조, seed와 output 계약을 검사합니다.</summary>
        private bool ValidateTerrain(int receiverIndex, UnityEngine.Object context)
        {
            if (sourceTerrain == null || sourceTerrain.terrainData == null)
            {
                Debug.LogError($"Receiver {receiverIndex} requires a Terrain with TerrainData.", context);
                return false;
            }
            if (terrainCollider == null || terrainCollider.terrainData != sourceTerrain.terrainData)
            {
                Debug.LogError($"Receiver {receiverIndex} TerrainCollider must reference the same TerrainData.", context);
                return false;
            }
            int triangleCount = (sourceTerrain.terrainData.heightmapResolution - 1) *
                                (sourceTerrain.terrainData.heightmapResolution - 1) * 2;
            if (seedTriangleIndex < 0 || seedTriangleIndex >= triangleCount)
            {
                Debug.LogError($"Receiver {receiverIndex} seed triangle index must be in [0, {triangleCount - 1}].", context);
                return false;
            }
            if (!HasValidOutputPair)
            {
                Debug.LogError($"Receiver {receiverIndex} must assign both output components or neither.", context);
                return false;
            }
            return true;
        }

        /// <summary>Geometry vertex 순서 그대로 bone binding을 만들고 변형 버퍼를 준비합니다.</summary>
        private void BuildSkinBinding(SurfaceGridGeometry geometry)
        {
            int boneCount = SkinnedMeshTopologyFactory.GetBoneCount(sourceSkinnedRenderer);
            if (boneCount == 0)
            {
                Debug.LogWarning(
                    "Skinned receiver has no bones; the grid stays in bind pose.", sourceSkinnedRenderer);
                return;
            }

            _skinBinding = SurfaceSkinBindingBuilder.Build(
                _topology,
                geometry.SurfacePoints,
                SkinnedMeshTopologyFactory.ReadInfluences(sourceSkinnedRenderer.sharedMesh),
                boneCount);
            _skinningMatrices = new Matrix4x4[boneCount];
            _deformedPositions = new Vector3[_skinBinding.VertexCount];
            _deformedNormals = new Vector3[_skinBinding.VertexCount];
        }

        /// <summary>출력 쌍이 구성된 경우 기본 Mesh Backend를 생성합니다.</summary>
        private void EnsureBackend()
        {
            if (_renderBackend != null || outputMeshFilter == null || outputMeshRenderer == null) return;
            _renderBackend = new MeshSurfaceGridRenderBackend(outputMeshFilter, outputMeshRenderer);
        }

        /// <summary>변형 추종 상태와 버퍼를 해제합니다.</summary>
        private void ReleaseSkinBinding()
        {
            _skinBinding = null;
            _skinningMatrices = null;
            _deformedPositions = null;
            _deformedNormals = null;
        }

        /// <summary>이 Receiver가 소유한 Backend와 runtime Mesh를 해제합니다.</summary>
        private void ReleaseBackend()
        {
            _renderBackend?.Dispose();
            _renderBackend = null;
        }

        /// <summary>Store의 시각 상태 변경을 Controller에 전달합니다.</summary>
        private void HandleVisualsChanged() => VisualsChanged?.Invoke();
    }
}
