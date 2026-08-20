# Jeomseon Unity Grid Tile System

A Unity package for hex coordinates, tile data and interaction, and surface-projected grid visualization.

## Requirements

- Unity 6000.5.7f1 or newer
- `com.jeomseon.unity.projector` and `com.jeomseon.unity.shaders` (installed as dependencies)
- The `com.jeomseon.unity` scope configured for the OpenUPM registry

## Install

Add `com.jeomseon.unity.grid-tile-system` by name in Unity Package Manager. Before the OpenUPM release is available, use:

```text
https://github.com/jeomseon0516/Unity.GridTileSystem.git#v0.1.0
```

## Basic Usage

Import the `Basic Usage` Sample and open `HexGridBasicUsage`. The Scene already connects the
`MeshProjector`, `HexGridProjectorEffect`, surface Material, and settings without an Auto Fix step.
For a custom Scene, add `MeshProjector` and `HexGridController` to the same GameObject and assign the
settings and effect. `MeshProjector` supports `MeshRenderer`, `SkinnedMeshRenderer`, and `Terrain`
receivers without exposing its internal Material. Receiver surfaces need Colliders for interaction.

Unity's built-in 2D Hexagonal Tilemap may be a better fit for conventional 2D tile games. This package targets projected 3D surfaces, physics raycasts, and per-tile runtime state.
