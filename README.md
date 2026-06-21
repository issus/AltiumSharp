# OriginalCircuit.Altium

[![CI](https://github.com/issus/AltiumSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/issus/AltiumSharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/OriginalCircuit.Altium)](https://www.nuget.org/packages/OriginalCircuit.Altium)
[![License](https://img.shields.io/github/license/issus/AltiumSharp)](LICENSE)

A high-performance .NET library for reading and writing Altium Designer EDA files without requiring Altium Designer to be installed. It supports schematic libraries, PCB libraries, schematic documents, and PCB documents, and provides cross-platform rendering to raster images and SVG.

> **This is version 2.0** — a complete rewrite of the original AltiumSharp library with a new async API, source-generated serialization, and cross-platform rendering.

## Supported File Types

| File Type | Extension | Read | Write | Render |
|-----------|-----------|------|-------|--------|
| PCB Library | `.PcbLib` | Yes | Yes | Yes |
| Schematic Library | `.SchLib` | Yes | Yes | Yes |
| Schematic Document | `.SchDoc` | Yes | Yes | Yes |
| PCB Document | `.PcbDoc` | Yes | Yes | Yes |

## Installation

Install the core library:

```
dotnet add package OriginalCircuit.Altium
```

Optional rendering packages:

```
dotnet add package OriginalCircuit.Altium.Rendering.Raster   # PNG/JPG via SkiaSharp
dotnet add package OriginalCircuit.Altium.Rendering.Svg      # Vector SVG output
dotnet add package OriginalCircuit.Altium.Rendering.Gltf     # 3D glTF board models
```

## Quick Start

**Reading a schematic library:**

```csharp
using OriginalCircuit.Altium;

await using var reader = new SchLibReader("MyLibrary.SchLib");
SchLib schLib = await reader.ReadAsync();

foreach (SchComponent component in schLib.Components)
{
    Console.WriteLine($"{component.Name}: {component.Description}");
}
```

**Reading a PCB library:**

```csharp
await using var reader = new PcbLibReader("MyFootprints.PcbLib");
PcbLib pcbLib = await reader.ReadAsync();

foreach (PcbComponent component in pcbLib.Components)
{
    Console.WriteLine($"{component.Name} — {component.Primitives.Count} primitives");
}
```

**Creating a new PCB library:**

```csharp
using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Records;
using OriginalCircuit.Altium.BasicTypes;

var pcbLib = new PcbLib();
var component = new PcbComponent { Name = "R0402" };

component.Primitives.Add(new PcbPad
{
    Location = new CoordPoint(Coord.FromMils(-25), Coord.Zero),
    SizeTop = new CoordPoint(Coord.FromMMs(0.56), Coord.FromMMs(0.62)),
});

pcbLib.Components.Add(component);

await using var writer = new PcbLibWriter("Output.PcbLib");
await writer.WriteAsync(pcbLib);
```

## Rendering

Three rendering packages are available for 2D output, all built on the abstractions in `OriginalCircuit.Altium.Rendering.Core`:

- **OriginalCircuit.Altium.Rendering.Raster** — renders to PNG or JPG using SkiaSharp (cross-platform)
- **OriginalCircuit.Altium.Rendering.Svg** — renders to SVG using .NET XML APIs (no native dependencies)

For 3D, **OriginalCircuit.Altium.Rendering.Gltf** exports a `PcbDocument` to a glTF 2.0 model — the full
layer stack (copper, laminate at true thickness, solder mask, silkscreen, drills) plus the placed
component 3D bodies, as named, individually toggleable nodes. See the **ExportBoardGltf** example.

Both renderers draw individual components (`PcbComponent`, `SchComponent`) and whole documents (`PcbDocument` boards and `SchDocument` sheets):

```csharp
var board = await new PcbDocReader().ReadAsync("board.PcbDoc");

await new RasterRenderer().RenderAsync(board, "top.png",
    new RenderOptions { Width = 2000, Height = 2000 });
```

### PCB board views and layers

Board renders fill the physical board outline as a black substrate — so copper, silk and solder mask read like a real board and the outline stands out — and accept an optional `PcbRenderSettings` for the view side and layer visibility:

```csharp
// Flipped bottom view (mirrored so it reads as if the board were turned over),
// with mechanical and internal-copper layers hidden for a clean read of the routing.
var settings = new PcbRenderSettings
{
    ViewSide = PcbViewSide.Bottom,   // Top | Bottom | Both
    ShowMechanical = false,          // hide mechanical layers (57-72)
    ShowInternalCopper = false,      // hide mid-signal layers (2-31) and planes (39-54)
    // Or take full control instead of the toggles:
    // LayerFilter = layer => PcbLayerGroups.IsSignalOrSilk(layer),
};

await new RasterRenderer().RenderAsync(board, "bottom.png", options, settings);
```

`PcbLayerGroups` classifies layer IDs (`IsCopper`, `IsOverlay`, `IsMechanical`, `IsInternalPlane`, `IsMultiLayer`, `IsSignalOrSilk`) for building custom `LayerFilter` predicates. Component designator/comment text honours each component's visibility flags, and inverted (knockout) silk labels render as filled rectangles.

See the [examples/](examples/) directory for complete rendering examples.

## Examples

The [examples/](examples/) directory contains runnable examples, each with its own
walkthrough README. The [guides index](guides/README.md) lists them by topic.

- `CreateFiles` — create SchLib and PcbLib files from scratch
- `LoadFiles` — read files and inspect their contents
- `ModifyFiles` — read a file, modify components, and write it back
- `RenderFiles` — render components and boards to PNG and SVG (with board view-side and layer options)
- `ExtractBom` — read a schematic and produce a grouped bill of materials (CSV + HTML)
- `GeneratePickAndPlace` — read a board and write an assembly pick-and-place (centroid) CSV
- `ExtractEmbeddedAssets` — extract embedded 3D STEP models and images to disk
- `InspectBoard` — report a board's outline size, layer stack, counts, and design rules
- `NetReport` — count the copper objects on each net
- `BuildFootprintGenerator` — generate parametric QFN/DIP footprints into a PcbLib
- `BuildMultiPartComponent` — build a multi-part symbol with `PartCount` / `OwnerPartId`
- `LibraryCatalog` — render a whole library to a PNG/SVG thumbnail gallery
- `WalkHierarchy` — walk a hierarchical schematic through its sheet symbols
- `DiffLibraries` — report added/removed/changed components between two libraries
- `ValidateLibrary` — surface reader diagnostics plus library-hygiene lint
- `WebPreviewService` — render component previews to an HTTP response (minimal API)

Run any example with:

```
dotnet run --project examples/CreateFiles
```

Read-oriented examples load a file from `TestData/` by default, or take a path to your own file:

```
dotnet run --project examples/ExtractBom -- "C:\path\to\MyBoard.SchDoc"
```

## Credits

This library is a rewrite of the original [AltiumSharp](https://github.com/issus/AltiumSharp) project. Original implementation by Tiago Trinidad ([@Kronal](https://github.com/Kronal)).

## License

MIT — see [LICENSE](LICENSE) for details.
