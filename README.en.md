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
2. Add one entry per surface to the controller's `Receivers` list.
3. Choose `Surface Kind`. Assign a source `MeshFilter` and matching `MeshCollider` for Static Mesh,
   or a `Terrain` and matching `TerrainCollider` for Terrain.
4. For visualization, assign that receiver a separate output `MeshFilter` and `MeshRenderer` pair.
5. Choose each receiver's seed triangle and barycentric point, then run `Rebuild Tiles`.

```text
Static Mesh/Terrain Adapter → Triangle topology → local unfolding patch → intrinsic hexes
→ polygon clipping → barycentric regions → geometry snapshot → mesh render backend
```

Runtime supports readable static meshes and Terrain heightfields. Terrain positions, triangles, and adjacency
are computed without cloning the entire heightfield into a Mesh, and hole faces are traversal boundaries.
Skinned binding, shared-boundary chunking, and Burst/Jobs optimization remain planned.

Each receiver owns an independent topology, coordinate space, tile state, picker, and optional render backend.
The same `(q,r)` on two receivers identifies different tiles, and one invalid receiver does not prevent valid
receivers from baking. Leaving both output references empty enables a logical-only grid with tile state and picking but no generated
render mesh. `HexTileData` owns serializable state, while `HexTile` adds UnityEvent interaction. Call
`HexGridController.RefreshRendering()` after directly applying synchronized data.
