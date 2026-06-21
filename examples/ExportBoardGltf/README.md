# ExportBoardGltf

Exports a whole PCB document (`.PcbDoc`) as a **3D glTF model** — the full layer
stack (copper, laminate at true thickness, solder mask, silkscreen, drills) plus
the placed component 3D bodies.

```
dotnet run --project examples/ExportBoardGltf                 # bundled sample board
dotnet run --project examples/ExportBoardGltf -- MyBoard.PcbDoc
```

It writes a binary `.glb` (full board + components), a bare-board `.gltf`, and a
copper-only `.glb` into a temp folder and prints the paths.

## What it shows

- Loading a board with `AltiumLibrary.OpenPcbDocAsync(path)`.
- `GltfRenderer.RenderAsync(document, path)` — the format is inferred from the
  extension (`.glb` ⇒ binary, `.gltf` ⇒ JSON with an embedded buffer).
- `GltfRenderSettings` for per-feature control: `IncludeComponents`,
  `IncludeSolderMask`, `IncludeSilkscreen`, `IncludeCopper`, `IncludeDrills`,
  `CopperFinish` (ENIG / HASL / bare), `CopperLayerFilter`, and the arc/component
  tessellation tolerances.

## Scene structure

Every board feature and every component is its own **named, toggleable node**, so
a glTF viewer (three.js, Babylon.js, Blender, the Windows 3D Viewer) can switch
them on and off:

- `Substrate` — the FR-4 laminate, extruded to the true stack-up thickness
- `Copper.Top Layer`, `Copper.Bottom Layer`, `Copper.<inner>` … — one per copper layer
- `SolderMask.Top` / `SolderMask.Bottom`
- `Silkscreen.Top` / `Silkscreen.Bottom`
- `Drills` — plated via and through-hole barrels
- `Components` — one child node per placed component body

Nodes carry an `extras` payload tagging the Altium layer/role, so a viewer can
also filter them programmatically. The model uses the glTF convention (right-handed,
+Y up, metres); board coordinates are converted from Altium `Coord` to millimetres
and centred at the origin.
