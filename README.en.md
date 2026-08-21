# Jeomseon Unity Grid Tile System

> The upcoming intrinsic Surface Grid architecture is incompatible with scenes authored with the
> projector workflow. Configure and bake them again with the new `HexGridController`.

A Unity package that builds hex grids in intrinsic Surface Space derived from triangle topology. It
does not project a world-XZ or projector plane onto geometry, so folded meshes, walls, and curved
surfaces can be traversed through topology.

## Requirements

- Unity 6000.5.7f1 or newer
- A Read/Write Enabled static Mesh for runtime topology construction
- A MeshCollider using the same source Mesh when picking is required
- The `com.jeomseon.unity` OpenUPM scope

The package has no Projector, URP, HDRP, or external shader-package dependency.

## Setup

1. Create a `HexGridSettings` asset.
2. Prepare a source `MeshFilter` and a `MeshCollider` using the same Mesh.
3. If visualization is required, create a separate output object with `MeshFilter` and `MeshRenderer`.
4. Assign the source and settings; assign both output references only when visualization is needed.
5. Choose a seed triangle and barycentric point, then run `Rebuild Tiles`.

```text
Static Mesh Adapter → Triangle topology → local unfolding patch → intrinsic hexes
→ polygon clipping → barycentric regions → geometry snapshot → mesh render backend
```

Current runtime support starts with static readable meshes. Terrain virtual topology, skinned binding,
shared-boundary chunking, and Burst/Jobs optimization remain planned.

Leaving both output references empty enables a logical-only grid with tile state and picking but no generated
render mesh. `HexTileData` owns serializable state, while `HexTile` adds UnityEvent interaction. Call
`HexGridController.RefreshRendering()` after directly applying synchronized data.
