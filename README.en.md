# Jeomseon Unity Grid Tile System

> The upcoming intrinsic Surface Grid architecture is incompatible with scenes authored with the
> projector workflow. Configure and bake them again with the new `HexGridController`.

A Unity package that builds hex grids in intrinsic Surface Space derived from triangle topology. It
does not project a world-XZ or projector plane onto geometry, so folded meshes, walls, and curved
surfaces can be traversed through topology.
Only hexes whose full intrinsic area fits inside the surface are retained, so boundary tiles are never clipped.

## Requirements

- Unity 6000.5.7f1 or newer
- A Read/Write Enabled static Mesh or `TerrainData` for runtime topology construction
- A MeshCollider using the same Mesh, or a TerrainCollider using the same TerrainData
- The `com.jeomseon.unity` OpenUPM scope

The package has no Projector, URP, HDRP, or external shader-package dependency.

## Setup

1. Create a `HexGridSettings` asset.
2. Add colliders for readable static meshes or terrains in the scene. No surface registration is required.
3. Set the world seed with the controller Transform or `Seed Anchor`/`Seed Offset`.
4. For visualization, assign a dedicated output `MeshFilter` and `MeshRenderer` pair.
5. Run `Rebuild Tiles`. The system finds the seed surface and expands across touching surfaces automatically.

```text
Static Mesh/Terrain Adapter → Triangle topology → local unfolding patch → intrinsic hexes
→ polygon clipping → barycentric regions → geometry snapshot → mesh render backend
```

Runtime supports readable static meshes and Terrain heightfields. Terrain positions, triangles, and adjacency
are computed without cloning the entire heightfield into a Mesh, and hole faces are traversal boundaries.
Skinned meshes follow bone deformation while the grid stays on one surface. A grid spanning multiple surfaces
remains in bind pose because those surfaces do not share one deformation binding. Shared-boundary chunking and
Burst/Jobs foundations are available through the advanced APIs below.

Leaving both output references empty enables a logical-only grid with tile state and picking but no generated
render mesh; assigning only one is rejected. `HexGridSettings` owns reusable seed-selection policy through
`Seed Search Radius`, `Surface Layer Mask`, and `Preferred Surface Direction`. The controller keeps the
scene-dependent `Initial Direction`, which aligns the first hex corner in world space. `HexTileData` owns
serializable state, while `HexTile` adds UnityEvent interaction. Call
`HexGridController.RefreshRendering()` after directly applying synchronized data.

![Cyan and blue hex checker grid conforming to the complete Basic Usage surface](Documentation~/Images/basic-usage-game.png)

This image was captured from the current `Basic Usage` sample in Play Mode with Unity 6000.5.7f1. At a tile
radius of 0.5 it generated 126 complete hexes and excluded tiles clipped by the surface boundary.

![Orange and yellow hex tiles conforming to curved terrain and hole boundaries](Documentation~/Images/terrain-usage-game.png)

`Terrain Usage` builds 149 tiles from virtual topology without cloning the terrain into a Mesh. This current
Play Mode capture uses zero geometry offset and the package-owned depth-biased fallback material to avoid
Terrain LOD depth conflicts without lifting the mesh from the heightfield.

![Inspector for the Terrain Hex Grid object with Transform, Surface Grid output, Mesh Renderer, and Hex Grid Controller](Documentation~/Images/terrain-controller-inspector.jpg)

The Inspector keeps the `Surface Grid` output mesh, renderer, controller bake result, and events on one object.
There is no receiver field or separate surface-registration workflow.

## Accuracy and large-grid APIs

Enable `SurfacePatchBuildSettings.SplitWhenLimitReached` to continue from frontier faces when a triangle-count
or graph-intrinsic-radius limit is reached. Frontier placements keep every patch in one intrinsic coordinate
system, each face is assigned once, and `SurfaceGridBuilder` combines partial regions from all patches into one
logical grid. Inspect `SurfacePatchSet` plus closure error, graph-geodesic upper bound, and maximum/average metric
distortion diagnostics on each patch.
`HexGridSettings` enables the same policy by default through `Split Patch When Limit Reached`.

`SurfaceGridSnapshot.Diff` reports added, removed, and rebound coordinates between versions.
`SurfaceGridChunk.CollectDirty` and `SurfaceGridChunkGeometryBuilder` rebuild only affected chunks.
`SurfaceBindingEvaluationJob` evaluates positions and normals from prepared contiguous topology/barycentric
arrays on Burst workers; the caller keeps Unity-object access and NativeArray lifetime outside the job.

`MeshSurfaceGridRenderBackend` remains the default. The optional
`StructuredBufferSurfaceGridRenderBackend` uploads vertex, index, and visual data and submits an indexed indirect
draw. Its material must implement `_SurfaceGridVertices` and `_SurfaceGridVisuals`; this Core package does not
include URP/HDRP-specific integration.

`TangentExpMapSurfaceParameterizer` is a first-order seed-tangent log-map comparison baseline, not a complete
heat-method geodesic solver. Use `SurfaceParameterizationComparison` to compare its patch count and maximum metric
distortion against Triangle Unfolding on the same input.

## Current limitations

- Automatic multi-patch coverage cannot remove the topological defects required to cover an entire sphere with
  regular hexes. The current graph geodesic is a face-centroid adjacency upper bound, not the heat method.
- The GPU backend provides a generic structured-buffer/indirect contract. Terrain-heightmap compute generation and
  render-pipeline-specific shaders/passes remain separate optional integrations driven by real project demand.
- Structured-buffer skinned deformation re-uploads the complete interleaved buffer. Combine Burst evaluation with
  chunking for large dynamic surfaces.
