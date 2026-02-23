# OriginalCircuit.Altium.Rendering.Raster

Raster image rendering (PNG, JPG) for OriginalCircuit.Altium components and documents, powered by [SkiaSharp](https://github.com/mono/SkiaSharp).

## Installation

```
dotnet add package OriginalCircuit.Altium.Rendering.Raster
```

This package depends on `OriginalCircuit.Altium` and `OriginalCircuit.Altium.Rendering.Core`.

## Usage

**Render a schematic component to PNG:**

```csharp
using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Rendering.Raster;

await using var reader = new SchLibReader("MyLibrary.SchLib");
SchLib schLib = await reader.ReadAsync();

SchComponent component = schLib.Components.First();

var renderer = new RasterRenderer();
using SKData imageData = renderer.RenderComponent(component, new RasterRenderOptions
{
    Width = 800,
    Height = 600,
    Background = SKColors.White,
    Format = SKEncodedImageFormat.Png,
    Quality = 100,
});

await File.WriteAllBytesAsync("component.png", imageData.ToArray());
```

**Render a PCB footprint:**

```csharp
await using var reader = new PcbLibReader("MyFootprints.PcbLib");
PcbLib pcbLib = await reader.ReadAsync();

PcbComponent component = pcbLib.Components.First();

var renderer = new RasterRenderer();
using SKData imageData = renderer.RenderComponent(component, new RasterRenderOptions
{
    Width = 400,
    Height = 400,
    Background = SKColors.Black,
    Format = SKEncodedImageFormat.Png,
});

await File.WriteAllBytesAsync("footprint.png", imageData.ToArray());
```

## Platform Support

SkiaSharp provides native binaries for Windows, macOS, Linux (x64 and ARM64), Android, and iOS. The appropriate native runtime package is selected automatically based on the target platform.

## License

MIT
