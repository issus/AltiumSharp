# Examples

This directory contains runnable example projects demonstrating common tasks with the OriginalCircuit.Altium library.

## Running Examples

Each example is a standalone console application. Run an example with:

```
dotnet run --project examples/<ExampleName>
```

For example:

```
dotnet run --project examples/CreateFiles
```

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

## Test Data

The examples expect Altium files in a `TestData/` directory at the repository root. A set of sample files is provided there. You can also point the examples at your own files by editing the file paths at the top of `Program.cs` in each project.
