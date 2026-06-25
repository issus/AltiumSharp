# OriginalCircuit.Altium — Guides

Task-oriented guides for the library. **Every guide is rooted in a runnable example
project** under [`examples/`](../examples/): the prose lives in the example's own
`README.md`, right next to a `Program.cs` that compiles and runs. All example projects
are part of the solution, so they are built on every CI run — if a documented snippet
stops compiling, the build fails. The code in the guides is therefore always real,
working code, not pseudo-code that drifts out of date.

Run any example with:

```bash
dotnet run --project examples/<ExampleName>
```

Read-oriented examples load real sample files from [`TestData/`](../TestData/) by default,
and accept a path to your own file as an argument.

## Getting started

For installation, the quick-start, and the coordinate system, see the top-level
[README](../README.md) and the [API reference](../API-REFERENCE.md).

## Guides

### Reading & inspecting

| Guide | What you get | Example |
|-------|--------------|---------|
| [Loading & inspecting files](../examples/LoadFiles/) | Open all four file types and walk their contents | `LoadFiles` |
| [Inspecting a PCB](../examples/InspectBoard/) | Board size, layer stack, primitive/net counts, rule summary | `InspectBoard` |
| [Nets & connectivity](../examples/NetReport/) | Per-net copper membership, and what the model does/doesn't track | `NetReport` |
| [Inspecting a whole project](../examples/InspectProject/) | Documents, variants, configurations and the sheet hierarchy from a `.PrjPcb` | `InspectProject` |

### Extracting

| Guide | What you get | Example |
|-------|--------------|---------|
| [Extracting a bill of materials](../examples/ExtractBom/) | Grouped BOM (CSV + HTML) from a `.SchDoc` | `ExtractBom` |
| [Pick-and-place / centroid file](../examples/GeneratePickAndPlace/) | Assembly centroid CSV from a `.PcbDoc` | `GeneratePickAndPlace` |
| [Extracting embedded assets](../examples/ExtractEmbeddedAssets/) | STEP 3D models and bitmap images to disk | `ExtractEmbeddedAssets` |

### Creating & authoring

| Guide | What you get | Example |
|-------|--------------|---------|
| [Creating files from scratch](../examples/CreateFiles/) | Build PcbLib, SchLib, SchDoc, PcbDoc with the fluent builders | `CreateFiles` |
| [Modifying existing files](../examples/ModifyFiles/) | Load → change → save round-trips for all four types | `ModifyFiles` |
| [Programmatic footprint generation](../examples/BuildFootprintGenerator/) | Parametric QFN/DIP footprint families | `BuildFootprintGenerator` |
| [Multi-part components](../examples/BuildMultiPartComponent/) | Symbols with `PartCount > 1` and `OwnerPartId` | `BuildMultiPartComponent` |

### Rendering

| Guide | What you get | Example |
|-------|--------------|---------|
| [Rendering components & boards](../examples/RenderFiles/) | PNG/SVG output, board view sides, layer filtering | `RenderFiles` |
| [Component catalogs](../examples/LibraryCatalog/) | Render a whole library to a thumbnail gallery | `LibraryCatalog` |

### Integration

| Guide | What you get | Example |
|-------|--------------|---------|
| [Serving previews from a web app](../examples/WebPreviewService/) | Render components to an HTTP response (minimal API) | `WebPreviewService` |

### Navigating & tooling

| Guide | What you get | Example |
|-------|--------------|---------|
| [Hierarchical schematics](../examples/WalkHierarchy/) | Walk sheet symbols into child sheets | `WalkHierarchy` |
| [Diffing libraries](../examples/DiffLibraries/) | Added / removed / changed components between two libraries | `DiffLibraries` |
| [Validating & linting](../examples/ValidateLibrary/) | Reader diagnostics plus library-hygiene checks | `ValidateLibrary` |

## A note on scope

The library reads and writes the four document/library types; there is **no project-file
(`.PrjPcb`/`.PrjScr`) reader**, and the schematic model is geometric (no built-in
pin-to-net connectivity). Guides call out these boundaries where they matter rather than
implying capabilities that aren't there. The PCB side *does* model net membership (by
`NetIndex`), which is what the [nets guide](../examples/NetReport/) uses.
