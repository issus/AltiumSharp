# Examples

This directory contains runnable example projects demonstrating common tasks with the OriginalCircuit.Altium library.

## Running Examples

Each example is a standalone application (mostly console apps; **BoardViewer** is a web
app). Run an example with:

```
dotnet run --project examples/<ExampleName>
```

For example:

```
dotnet run --project examples/CreateFiles
```

Each example has its own README with a full walkthrough; the
[guides index](../guides/README.md) lists them all by topic. Read-oriented examples load
a sample from [`TestData/`](../TestData/) by default and accept a path to your own file
as an argument.

## Example Projects

### CreateFiles

Demonstrates creating SchLib and PcbLib files from scratch using the fluent API.

- Creates a schematic library with a two-pin resistor component
- Creates a PCB library with an 0402 SMD footprint
- Writes both files to disk and verifies they can be read back

```
dotnet run --project examples/CreateFiles
```

### LoadFiles

Demonstrates reading existing Altium files and inspecting their contents.

- Opens a SchLib and prints each component name, description, and pin count
- Opens a PcbLib and prints each footprint name and primitive count
- Opens a SchDoc and lists all wires and component instances
- Shows how to inspect the `Diagnostics` collection for non-fatal read issues

```
dotnet run --project examples/LoadFiles
```

### ModifyFiles

Demonstrates reading a file, making changes, and writing it back.

- Reads a SchLib, updates component descriptions, and saves the result
- Adds a new footprint to an existing PcbLib
- Shows round-trip fidelity: the output file can be opened in Altium Designer

```
dotnet run --project examples/ModifyFiles
```

### RenderFiles

Demonstrates rendering components to raster images and SVG.

- Renders each footprint in a PcbLib to a PNG file using `OriginalCircuit.Altium.Rendering.Raster`
- Renders each component in a SchLib to an SVG file using `OriginalCircuit.Altium.Rendering.Svg`
- Shows how to configure rendering options (size, padding, colors)

```
dotnet run --project examples/RenderFiles
```

### ExtractBom

Reads a schematic document and produces a grouped bill of materials.

- Groups identical parts into one line with a quantity and designator list
- Reads designators from component parameters, values from `Comment`, footprints from
  implementation links
- Writes the BOM as CSV and HTML

```
dotnet run --project examples/ExtractBom
```

See [ExtractBom/README.md](ExtractBom/README.md) for the walkthrough.

### GeneratePickAndPlace

Reads a PCB document and writes an assembly pick-and-place (centroid) CSV.

- One row per placed component: designator, X/Y centroid (mm), rotation, board side
- Reads placement from `PcbComponent.X/Y/Rotation/Layer/SourceDesignator`

```
dotnet run --project examples/GeneratePickAndPlace
```

See [GeneratePickAndPlace/README.md](GeneratePickAndPlace/README.md) for the walkthrough.

### ExtractEmbeddedAssets

Extracts embedded binary assets to disk.

- Pulls 3D STEP models out of a PcbLib (`PcbLibrary.Models`)
- Pulls bitmap images out of a SchDoc/SchLib (`SchImage.ImageData`), sniffing the format

```
dotnet run --project examples/ExtractEmbeddedAssets
```

See [ExtractEmbeddedAssets/README.md](ExtractEmbeddedAssets/README.md) for the walkthrough.

### InspectBoard

Reports a PCB's outline size, layer stack, primitive/net counts, and design rules.
See [InspectBoard/README.md](InspectBoard/README.md).

```
dotnet run --project examples/InspectBoard
```

### NetReport

Counts the copper objects on each net (resolving `NetIndex` into the net list).
See [NetReport/README.md](NetReport/README.md).

```
dotnet run --project examples/NetReport
```

### BuildFootprintGenerator

Generates parametric QFN/DIP footprints into a PcbLib using the fluent builders.
See [BuildFootprintGenerator/README.md](BuildFootprintGenerator/README.md).

```
dotnet run --project examples/BuildFootprintGenerator
```

### BuildMultiPartComponent

Builds a multi-part symbol with `PartCount` and per-primitive `OwnerPartId`.
See [BuildMultiPartComponent/README.md](BuildMultiPartComponent/README.md).

```
dotnet run --project examples/BuildMultiPartComponent
```

### LibraryCatalog

Renders every component in a library to a PNG/SVG thumbnail gallery (`index.html`).
See [LibraryCatalog/README.md](LibraryCatalog/README.md).

```
dotnet run --project examples/LibraryCatalog
```

### WalkHierarchy

Walks a hierarchical schematic from its top sheet through sheet symbols into child sheets.
See [WalkHierarchy/README.md](WalkHierarchy/README.md).

```
dotnet run --project examples/WalkHierarchy
```

### DiffLibraries

Reports added/removed/changed components between two libraries of the same type.
See [DiffLibraries/README.md](DiffLibraries/README.md).

```
dotnet run --project examples/DiffLibraries
```

### ValidateLibrary

Surfaces reader diagnostics plus library-hygiene lint over a PcbLib/SchLib.
See [ValidateLibrary/README.md](ValidateLibrary/README.md).

```
dotnet run --project examples/ValidateLibrary
```

### RenderBoard

Renders a whole PCB document (`.PcbDoc`) as a **photorealistic 2D board** (a fab-house /
gerber-viewer look) to PNG and SVG, in a few colour/finish/side variations.
See [RenderBoard/README.md](RenderBoard/README.md).

```
dotnet run --project examples/RenderBoard                 # bundled sample board
dotnet run --project examples/RenderBoard -- MyBoard.PcbDoc
```

### BoardViewer

An interactive web app: **drag-and-drop a `.PcbDoc`** and view a photorealistic render in
the browser, with live colour controls, layer toggles, and SVG/PNG export.
See [BoardViewer/README.md](BoardViewer/README.md).

```
dotnet run --project examples/BoardViewer   # then open the printed URL
```

### ExportBoardGltf

Exports a whole PCB document (`.PcbDoc`) as a **3D glTF model** — the full layer stack
(copper, laminate at true thickness, solder mask, silkscreen, drills) plus placed component
3D bodies, as named, toggleable nodes for any glTF viewer.
See [ExportBoardGltf/README.md](ExportBoardGltf/README.md).

```
dotnet run --project examples/ExportBoardGltf                 # bundled sample board
dotnet run --project examples/ExportBoardGltf -- MyBoard.PcbDoc
```

## Test Data

The examples expect Altium files in a `TestData/` directory at the repository root. A set of sample files is provided there. You can also point the examples at your own files by editing the file paths at the top of `Program.cs` in each project.
