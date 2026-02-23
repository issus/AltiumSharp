# OriginalCircuit.Altium

Core library for reading and writing Altium Designer EDA files (.PcbLib, .SchLib, .PcbDoc, .SchDoc) using .NET 10. No Altium Designer installation is required.

## Key Features

- **Async API** — all read and write operations are fully async with `CancellationToken` support
- **Source-generated serialization** — parameter mapping between Altium's pipe-delimited format and C# record types is handled by a Roslyn source generator; no reflection at runtime
- **Four file formats** — read and write PcbLib, SchLib, PcbDoc, and SchDoc files
- **Structured diagnostics** — readers collect warnings and non-fatal errors as `AltiumDiagnostic` records rather than throwing, allowing partial reads of corrupt files
- **Cross-platform** — targets net10.0 with no Windows-specific dependencies in the core library

## Installation

```
dotnet add package OriginalCircuit.Altium
```

## Basic Usage

```csharp
using OriginalCircuit.Altium;

// Read a schematic library
await using var reader = new SchLibReader("MyLibrary.SchLib");
SchLib schLib = await reader.ReadAsync();

// Inspect diagnostics
foreach (var diagnostic in schLib.Diagnostics)
{
    Console.WriteLine($"[{diagnostic.Severity}] {diagnostic.Message}");
}

// Iterate components
foreach (SchComponent component in schLib.Components)
{
    Console.WriteLine($"{component.Name}: {component.Description}");
    Console.WriteLine($"  Pins: {component.GetPrimitivesOfType<SchPin>().Count()}");
}

// Read a PCB document
await using var pcbReader = new PcbDocReader("MyBoard.PcbDoc");
PcbDoc pcbDoc = await pcbReader.ReadAsync();

foreach (PcbNet net in pcbDoc.Nets)
{
    Console.WriteLine($"Net: {net.Name}");
}

// Write a modified library back
await using var writer = new SchLibWriter("Modified.SchLib");
await writer.WriteAsync(schLib);
```

## Namespaces

| Namespace | Contents |
|-----------|----------|
| `OriginalCircuit.Altium` | Readers, writers, and top-level data model classes |
| `OriginalCircuit.Altium.BasicTypes` | `Coord`, `CoordPoint`, `CoordRect`, `ParameterCollection`, `Layer` |
| `OriginalCircuit.Altium.Records` | All primitive types (`PcbPad`, `SchPin`, `SchWire`, etc.) |

## Error Handling

Readers throw `AltiumCorruptFileException` for unrecoverable file corruption. Non-fatal issues are collected as diagnostics on the returned model object. The `AltiumUnsupportedFeatureException` type is thrown when a record type is encountered that the library does not support.

## License

MIT
