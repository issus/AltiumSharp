# OriginalCircuit.Altium.Rendering.Svg

SVG rendering for OriginalCircuit.Altium components and documents. Produces standards-compliant SVG 1.1 output with no native dependencies.

## Installation

```
dotnet add package OriginalCircuit.Altium.Rendering.Svg
```

This package depends on `OriginalCircuit.Altium` and `OriginalCircuit.Altium.Rendering.Core`.

## Usage

**Render a schematic component to SVG:**

```csharp
using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Rendering.Svg;

await using var reader = new SchLibReader("MyLibrary.SchLib");
SchLib schLib = await reader.ReadAsync();

SchComponent component = schLib.Components.First();

var renderer = new SvgRenderer();
string svg = renderer.RenderComponentToString(component, new SvgRenderOptions
{
    Padding = 10,
    StrokeWidth = 1.0,
});

await File.WriteAllTextAsync("component.svg", svg);
```

**Render a PCB footprint:**

```csharp
await using var reader = new PcbLibReader("MyFootprints.PcbLib");
PcbLib pcbLib = await reader.ReadAsync();

PcbComponent component = pcbLib.Components.First();

var renderer = new SvgRenderer();
string svg = renderer.RenderComponentToString(component, new SvgRenderOptions
{
    Padding = 5,
    IncludeLayerColors = true,
});

await File.WriteAllTextAsync("footprint.svg", svg);
```

**Access the XDocument directly:**

```csharp
XDocument svgDocument = renderer.RenderComponent(component, options);
// Manipulate the SVG XML tree before saving
svgDocument.Save("footprint.svg");
```

## Output Format

The SVG output uses a `viewBox` sized to the component bounds plus padding. All coordinates are in mils. Text elements use `font-family: monospace` by default and can be overridden via `SvgRenderOptions`.

## License

MIT
