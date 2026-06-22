# OriginalCircuit.Altium.Rendering.Gltf

glTF 2.0 3D rendering for OriginalCircuit.Altium PCB documents. Produces a complete, explorable
3D model of a board — the full layer stack (copper, laminate, solder mask, silkscreen, drills) at
true thicknesses, plus the embedded component 3D bodies placed on the board — as named, individually
toggleable nodes in a `.gltf` or `.glb` scene.

The 3D engine (native STEP reader/tessellator and glTF writer) is provided by the
`OriginalCircuit.Mech.STEP` and `OriginalCircuit.Mech.GLTF` libraries; this package contains the
Altium-domain glue that turns a `PcbDocument` into geometry. No native dependencies.

## Installation

```
dotnet add package OriginalCircuit.Altium.Rendering.Gltf
```

This package depends on `OriginalCircuit.Altium` and `OriginalCircuit.Altium.Rendering.Core`.

## Usage

**Render a PCB document to a binary glTF (`.glb`):**

```csharp
using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Rendering.Gltf;

await using var reader = new PcbDocReader("MyBoard.PcbDoc");
PcbDoc board = await reader.ReadAsync();

var renderer = new GltfRenderer();
await renderer.RenderAsync(board.Document, "MyBoard.glb");
```

**Bare board only (no components), as a text `.gltf`:**

```csharp
await renderer.RenderAsync(board.Document, "MyBoard.gltf", settings: new GltfRenderSettings
{
    IncludeComponents = false,
});
```

## Scene structure

Each board feature is emitted as a separate, named glTF node so a viewer (three.js, Babylon.js,
Blender, …) can toggle it independently:

- `Substrate` — the FR4 laminate, extruded to the true stack thickness, with unplated mounting
  holes and board cut-outs subtracted as see-through openings
- `Copper.Top Layer`, `Copper.Bottom Layer`, `Copper.<inner>` … — one node per copper layer; tracks,
  arcs, fills, regions, pads (with through-hole annuli) and via rings
- `SolderMask.Top` / `SolderMask.Bottom` — a translucent **inverse** layer: the board outline minus the
  pad/via openings (grown by the solder-mask expansion) and any features drawn on the mask layer, so
  the copper finish shows through the openings bright and reads tinted under the mask
- `Silkscreen.Top` / `Silkscreen.Bottom` — overlay tracks, arcs, fills and **text** (TrueType glyph
  outlines, or the stroke font, plus inverted/negative text and 2-D barcodes)
- `Drills` — plated hole and via barrels
- `Components` — one child node per placed 3D body, in its STEP per-face colours; bottom-side bodies
  are mirrored under the board
- `EmbeddedBoard.<name>` — for a **panel**, the referenced sub-board composited and tiled across the
  array; each grid cell instances the sub-board's shared feature meshes, so a 3×3 panel costs roughly
  one board's worth of geometry. Sub-boards are resolved from the panel file's directory, or via
  `GltfRenderSettings.EmbeddedBoardResolver`

Nodes carry an `extras` payload tagging the Altium layer/role for programmatic filtering. Pad shapes
(round, oval, rectangular, octagonal, rounded-rectangle) are rendered to shape.

## Coordinates and units

Output follows the glTF convention: right-handed, +Y up, metres. Altium `Coord` values are converted
to millimetres and then scaled into the glTF scene.

## License

MIT
