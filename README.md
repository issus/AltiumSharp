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

Three rendering packages are available, all built on the abstractions in `OriginalCircuit.Altium.Rendering.Core`:

- **OriginalCircuit.Altium.Rendering.Raster** — renders to PNG or JPG using SkiaSharp (cross-platform)
- **OriginalCircuit.Altium.Rendering.Svg** — renders to SVG using .NET XML APIs (no native dependencies)

See the [examples/](examples/) directory for complete rendering examples.

## Examples

The [examples/](examples/) directory contains runnable examples:

- `CreateFiles` — create SchLib and PcbLib files from scratch
- `LoadFiles` — read files and inspect their contents
- `ModifyFiles` — read a file, modify components, and write it back
- `RenderFiles` — render components to PNG and SVG

Run any example with:

```
dotnet run --project examples/CreateFiles
```

## Credits

This library is a rewrite of the original [AltiumSharp](https://github.com/issus/AltiumSharp) project. Original implementation by Tiago Trinidad ([@Kronal](https://github.com/Kronal)).

## License

MIT — see [LICENSE](LICENSE) for details.
