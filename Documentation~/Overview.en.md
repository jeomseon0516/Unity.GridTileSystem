# Jeomseon Unity Grid Tile System

[한국어](Overview.md) · [Installation](../README.en.md)

## Purpose

Builds intrinsic Hex Grids on triangle topology, separating logical state, surface bindings, and rendering.

## Usage flow

Configure settings/seed, discover readable Mesh/Terrain, unfold patches, clip Hexes with barycentric binding, create geometry snapshots, and render with Mesh/structured-buffer backends. RefreshRendering after direct data edits.

## Contracts and limitations

It does not remove global regular-Hex defects on closed surfaces. Graph geodesic is a face-center path upper bound, not heat-method distance. See README for multi-surface skinning and GPU backend limits.

## Package requirements

- Package ID: `com.jeomseon.unity.grid-tile-system`
- Version: `0.4.2`
- Unity: `6000.6` / `0f1`
- `com.jeomseon.unity.attributes`: `0.4.2`
- `com.jeomseon.unity.core`: `0.3.2`
- `com.jeomseon.unity.editor-toolkit`: `0.7.2`
- `com.unity.burst`: `1.8.30`
- `com.unity.collections`: `2.6.8`
- `com.unity.inputsystem`: `1.7.0`
- `com.unity.render-pipelines.universal`: `17.6.0`
- `com.unity.modules.terrain`: `1.0.0`
- `com.unity.modules.terrainphysics`: `1.0.0`

## Samples and verification

Import the following from Package Manager > Samples. Open the included Scene or Editor tool; follow any Setup step in its sample README.

- [Basic Usage](../Samples~/BasicUsage/README.md)
  - [HexGridBasicUsage](../Samples~/BasicUsage/HexGridBasicUsage.unity)
- [Terrain Usage](../Samples~/TerrainUsage/README.md)
  - [TerrainBasicUsage](../Samples~/TerrainUsage/TerrainBasicUsage.unity)

[Test sources](../Tests)

Add the package ID to manifest `testables`, then run the relevant Unity Test Runner assemblies. These links describe available tests, not a new successful test run.

## Source map

- [BothDrawPolicy](../Runtime/BothDrawPolicy.cs)
- [FillDrawPolicy](../Runtime/FillDrawPolicy.cs)
- [HexGridController](../Runtime/HexGridController.cs)
- [HexGridSettings](../Runtime/HexGridSettings.cs)
- [AxialCoordinates, HexCoordinates](../Runtime/HexGridStructs.cs)
- [HexTileRenderData, HexTile](../Runtime/HexTile.cs)
- [HexTileData](../Runtime/HexTileData.cs)
- [IHexTile](../Runtime/IHexTile.cs)
- [IHexTileDrawPolicy](../Runtime/IHexTileDrawPolicy.cs)
- [NoneDrawPolicy](../Runtime/NoneDrawPolicy.cs)
- [OutlineDrawPolicy](../Runtime/OutlineDrawPolicy.cs)
- [HexGridPointerInput](../Runtime/Services/HexGridPointerInput.cs)
- [HexTilePicker](../Runtime/Services/HexTilePicker.cs)
- [HexTilePickResult](../Runtime/Services/HexTilePickResult.cs)
- [HexTileSelectionState](../Runtime/Services/HexTileSelectionState.cs)
- [HexTileStore](../Runtime/Services/HexTileStore.cs)
- [IHexGridPointerInput](../Runtime/Services/IHexGridPointerInput.cs)
- [IHexTilePicker](../Runtime/Services/IHexTilePicker.cs)
- [IHexTileSelectionState](../Runtime/Services/IHexTileSelectionState.cs)
- [IHexTileStore](../Runtime/Services/IHexTileStore.cs)
- [ISurfaceAdapter](../Runtime/Surface/Adapters/ISurfaceAdapter.cs)
- [ISurfaceAdapterFactory](../Runtime/Surface/Adapters/ISurfaceAdapterFactory.cs)
- [MeshSurfaceAdapter](../Runtime/Surface/Adapters/MeshSurfaceAdapter.cs)
- [MeshSurfaceAdapterFactory](../Runtime/Surface/Adapters/MeshSurfaceAdapterFactory.cs)
- [MeshTopologyFactory](../Runtime/Surface/Adapters/MeshTopologyFactory.cs)
- [SkinnedMeshSurfaceAdapter](../Runtime/Surface/Adapters/SkinnedMeshSurfaceAdapter.cs)
- [SkinnedMeshSurfaceAdapterFactory](../Runtime/Surface/Adapters/SkinnedMeshSurfaceAdapterFactory.cs)
- [SkinnedMeshTopologyFactory](../Runtime/Surface/Adapters/SkinnedMeshTopologyFactory.cs)
- [SurfaceAdapterPriority](../Runtime/Surface/Adapters/SurfaceAdapterPriority.cs)
- [SurfaceAdapterResolution](../Runtime/Surface/Adapters/SurfaceAdapterResolution.cs)
- [SurfaceAdapterResolver](../Runtime/Surface/Adapters/SurfaceAdapterResolver.cs)
- [TerrainHeightfieldParameterizer](../Runtime/Surface/Adapters/TerrainHeightfieldParameterizer.cs)
- [TerrainSurfaceAdapter](../Runtime/Surface/Adapters/TerrainSurfaceAdapter.cs)
- [TerrainSurfaceAdapterFactory](../Runtime/Surface/Adapters/TerrainSurfaceAdapterFactory.cs)
- [TerrainSurfaceTopology](../Runtime/Surface/Adapters/TerrainSurfaceTopology.cs)
- [TerrainTopologyFactory](../Runtime/Surface/Adapters/TerrainTopologyFactory.cs)
- [DistortionAdaptiveSurfaceParameterizer](../Runtime/Surface/Core/DistortionAdaptiveSurfaceParameterizer.cs)
- [ISurfaceConnectivity](../Runtime/Surface/Core/ISurfaceConnectivity.cs)
- [ISurfaceParameterizer](../Runtime/Surface/Core/ISurfaceParameterizer.cs)
- [ISurfaceProvider](../Runtime/Surface/Core/ISurfaceProvider.cs)
- [ISurfaceTransformSource](../Runtime/Surface/Core/ISurfaceTransformSource.cs)
- [SingleSurfaceProvider](../Runtime/Surface/Core/SingleSurfaceProvider.cs)
- [SurfaceBoneInfluence](../Runtime/Surface/Core/SurfaceBoneInfluence.cs)
- [SurfaceCapabilities](../Runtime/Surface/Core/SurfaceCapabilities.cs)
- [SurfaceChartDirection](../Runtime/Surface/Core/SurfaceChartDirection.cs)
- [SurfaceClosestPoint](../Runtime/Surface/Core/SurfaceClosestPoint.cs)
- [SurfaceConnectivityOptions](../Runtime/Surface/Core/SurfaceConnectivityOptions.cs)
- [SurfaceHandle](../Runtime/Surface/Core/SurfaceHandle.cs)
- [SurfaceLink](../Runtime/Surface/Core/SurfaceLink.cs)
- [SurfaceParameterizationComparison](../Runtime/Surface/Core/SurfaceParameterizationComparison.cs)
- [SurfacePatch](../Runtime/Surface/Core/SurfacePatch.cs)
- [SurfacePatchBuildSettings](../Runtime/Surface/Core/SurfacePatchBuildSettings.cs)
- [SurfacePatchDiagnostics](../Runtime/Surface/Core/SurfacePatchDiagnostics.cs)
- [SurfacePatchMapper](../Runtime/Surface/Core/SurfacePatchMapper.cs)
- [SurfacePatchSet](../Runtime/Surface/Core/SurfacePatchSet.cs)
- [SurfacePatchTriangle](../Runtime/Surface/Core/SurfacePatchTriangle.cs)
- [SurfacePoint](../Runtime/Surface/Core/SurfacePoint.cs)
- [SurfaceRegion](../Runtime/Surface/Core/SurfaceRegion.cs)
- [SurfaceRegionBuilder](../Runtime/Surface/Core/SurfaceRegionBuilder.cs)
- [SurfaceRegionVertex](../Runtime/Surface/Core/SurfaceRegionVertex.cs)
- [SurfaceSkinBinding](../Runtime/Surface/Core/SurfaceSkinBinding.cs)
- [SurfaceSkinBindingBuilder](../Runtime/Surface/Core/SurfaceSkinBindingBuilder.cs)
- [SurfaceTopology](../Runtime/Surface/Core/SurfaceTopology.cs)
- [SurfaceTopologyBuilder](../Runtime/Surface/Core/SurfaceTopologyBuilder.cs)
- [SurfaceTopologyDiagnosticKind, SurfaceTopologyDiagnostic](../Runtime/Surface/Core/SurfaceTopologyDiagnostic.cs)
- [SurfaceTriangle](../Runtime/Surface/Core/SurfaceTriangle.cs)
- [SurfaceTriangleAdjacency](../Runtime/Surface/Core/SurfaceTriangleAdjacency.cs)
- [TangentExpMapSurfaceParameterizer](../Runtime/Surface/Core/TangentExpMapSurfaceParameterizer.cs)
- [TriangleUnfoldingParameterizer](../Runtime/Surface/Core/TriangleUnfoldingParameterizer.cs)
- [TriangleUnfoldingSurfaceParameterizer](../Runtime/Surface/Core/TriangleUnfoldingSurfaceParameterizer.cs)
- [IntrinsicHexLayout](../Runtime/Surface/Grid/IntrinsicHexLayout.cs)
- [SurfaceGrid](../Runtime/Surface/Grid/SurfaceGrid.cs)
- [SurfaceGridBuilder](../Runtime/Surface/Grid/SurfaceGridBuilder.cs)
- [SurfaceGridBuildResult](../Runtime/Surface/Grid/SurfaceGridBuildResult.cs)
- [SurfaceGridBuildStatus](../Runtime/Surface/Grid/SurfaceGridBuildStatus.cs)
- [SurfaceGridChunk](../Runtime/Surface/Grid/SurfaceGridChunk.cs)
- [SurfaceGridDelta](../Runtime/Surface/Grid/SurfaceGridDelta.cs)
- [SurfaceGridPicker](../Runtime/Surface/Grid/SurfaceGridPicker.cs)
- [SurfaceGridRequest](../Runtime/Surface/Grid/SurfaceGridRequest.cs)
- [SurfaceGridSnapshot](../Runtime/Surface/Grid/SurfaceGridSnapshot.cs)
- [SurfaceGridSystem](../Runtime/Surface/Grid/SurfaceGridSystem.cs)
- [SurfaceGridTileRegion](../Runtime/Surface/Grid/SurfaceGridTileRegion.cs)
- [GeometrySurfaceConnectivity](../Runtime/Surface/Query/GeometrySurfaceConnectivity.cs)
- [GeometrySurfaceQuery](../Runtime/Surface/Query/GeometrySurfaceQuery.cs)
- [ISurfaceCandidateSource](../Runtime/Surface/Query/ISurfaceCandidateSource.cs)
- [ISurfaceDiscovery](../Runtime/Surface/Query/ISurfaceDiscovery.cs)
- [ISurfaceQuery](../Runtime/Surface/Query/ISurfaceQuery.cs)
- [PhysicsSurfaceCandidateSource](../Runtime/Surface/Query/PhysicsSurfaceCandidateSource.cs)
- [SurfaceQueryHit](../Runtime/Surface/Query/SurfaceQueryHit.cs)
- [SurfaceQueryOptions](../Runtime/Surface/Query/SurfaceQueryOptions.cs)
- [SurfaceTileVisual, SurfaceGridDrawMode, ISurfaceGridRenderBackend](../Runtime/Surface/Rendering/ISurfaceGridRenderBackend.cs)
- [MeshSurfaceGridRenderBackend](../Runtime/Surface/Rendering/MeshSurfaceGridRenderBackend.cs)
- [StructuredBufferSurfaceGridRenderBackend](../Runtime/Surface/Rendering/StructuredBufferSurfaceGridRenderBackend.cs)
- [SurfaceBindingJobData, SurfaceBindingEvaluationJob](../Runtime/Surface/Rendering/SurfaceBindingEvaluationJob.cs)
- [SurfaceGridChunkGeometryBuilder](../Runtime/Surface/Rendering/SurfaceGridChunkGeometryBuilder.cs)
- [SurfaceGridGeometry](../Runtime/Surface/Rendering/SurfaceGridGeometry.cs)
- [SurfaceGridGeometryBuilder](../Runtime/Surface/Rendering/SurfaceGridGeometryBuilder.cs)
- [AxialCoordinatesProp](../Editor/AxialCoordinatesPropertyDrawer.cs)
- [HexCoordinatesProp](../Editor/HexCoordinatesPropertyDrawer.cs)
