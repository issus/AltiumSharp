// ============================================================================
// Example: Rendering Altium Components to PNG and SVG
// ============================================================================
//
// This example demonstrates the rendering system, which can produce visual
// previews of PCB footprints and schematic symbols.
//
// RENDERING ARCHITECTURE
// ──────────────────────
// The rendering system is split into three NuGet packages:
//
//   OriginalCircuit.Altium.Rendering.Core   - Shared abstractions
//     CoordTransform  : Converts between world coordinates and screen pixels
//     RenderOptions   : Width, Height, BackgroundColor, AutoZoom, Scale
//     LayerColors     : Maps PCB layer IDs to display colors and draw order
//     IRenderContext  : Abstraction implemented by each output format
//
//   OriginalCircuit.Altium.Rendering.Raster - PNG output (requires SkiaSharp)
//     RasterRenderer  : Renders to PNG via SkiaSharp
//
//   OriginalCircuit.Altium.Rendering.Svg    - SVG output (no external deps)
//     SvgRenderer     : Renders to SVG using System.Xml.Linq
//
// WHAT CAN BE RENDERED
// ────────────────────
// Both renderers accept any IComponent (PcbComponent or SchComponent).
// They draw all contained primitives: pads, tracks, arcs, pins,
// rectangles, polylines, labels, etc.
//
// The renderers handle coordinate transformation automatically:
// with AutoZoom=true (default), the component is scaled to fit the
// output dimensions while maintaining aspect ratio.
//
// ============================================================================

using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Rendering;
using OriginalCircuit.Altium.Rendering.Raster;
using OriginalCircuit.Altium.Rendering.Svg;

var outputDir = Path.Combine(Path.GetTempPath(), "AltiumRenderExample");
Directory.CreateDirectory(outputDir);
Console.WriteLine($"Output directory: {outputDir}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  1. Create a PCB component to render                                    ║
// ║                                                                         ║
// ║  We build a simplified QFP footprint with pads on one side, a           ║
// ║  silkscreen outline (4 tracks), a pin-1 marker (arc), and a             ║
// ║  designator text. This gives us multiple primitive types to render.     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

var pcbComponent = PcbComponent.Create("QFP48")
    .WithDescription("48-pin QFP footprint")

    // Five SMD pads along the left side, spaced at 2.54mm pitch.
    // Layer 1 = Top Copper. Each pad is 1.5 x 0.6 mm (horizontal orientation).
    .AddPad(p => p.At(Coord.FromMm(-6.35), Coord.FromMm(-5.08))
        .Size(Coord.FromMm(1.5), Coord.FromMm(0.6)).HoleSize(Coord.FromMm(0))
        .WithDesignator("1").Layer(1))
    .AddPad(p => p.At(Coord.FromMm(-6.35), Coord.FromMm(-2.54))
        .Size(Coord.FromMm(1.5), Coord.FromMm(0.6)).HoleSize(Coord.FromMm(0))
        .WithDesignator("2").Layer(1))
    .AddPad(p => p.At(Coord.FromMm(-6.35), Coord.FromMm(0))
        .Size(Coord.FromMm(1.5), Coord.FromMm(0.6)).HoleSize(Coord.FromMm(0))
        .WithDesignator("3").Layer(1))
    .AddPad(p => p.At(Coord.FromMm(-6.35), Coord.FromMm(2.54))
        .Size(Coord.FromMm(1.5), Coord.FromMm(0.6)).HoleSize(Coord.FromMm(0))
        .WithDesignator("4").Layer(1))
    .AddPad(p => p.At(Coord.FromMm(-6.35), Coord.FromMm(5.08))
        .Size(Coord.FromMm(1.5), Coord.FromMm(0.6)).HoleSize(Coord.FromMm(0))
        .WithDesignator("5").Layer(1))

    // Silkscreen outline: four tracks forming a rectangle.
    // Layer 21 = Top Overlay (silkscreen). Rendered in overlay color.
    .AddTrack(t => t.From(Coord.FromMm(-5.08), Coord.FromMm(-6.35))
        .To(Coord.FromMm(5.08), Coord.FromMm(-6.35)).Width(Coord.FromMm(0.254)).Layer(21))
    .AddTrack(t => t.From(Coord.FromMm(5.08), Coord.FromMm(-6.35))
        .To(Coord.FromMm(5.08), Coord.FromMm(6.35)).Width(Coord.FromMm(0.254)).Layer(21))
    .AddTrack(t => t.From(Coord.FromMm(5.08), Coord.FromMm(6.35))
        .To(Coord.FromMm(-5.08), Coord.FromMm(6.35)).Width(Coord.FromMm(0.254)).Layer(21))
    .AddTrack(t => t.From(Coord.FromMm(-5.08), Coord.FromMm(6.35))
        .To(Coord.FromMm(-5.08), Coord.FromMm(-6.35)).Width(Coord.FromMm(0.254)).Layer(21))

    // Pin 1 marker: a small filled circle at the corner (full arc, 0-360 degrees)
    .AddArc(a => a.At(Coord.FromMm(-5.08), Coord.FromMm(-6.35))
        .Radius(Coord.FromMm(0.5)).Angles(0, 360).Width(Coord.FromMm(0.12)).Layer(21))

    // ".Designator" is a special token replaced by the component's ref des
    .AddText(".Designator", t => t
        .At(Coord.FromMm(0), Coord.FromMm(7.62)).Height(Coord.FromMm(1.0)).Layer(21))
    .Build();

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  2. Render to PNG (raster)                                              ║
// ║                                                                         ║
// ║  RasterRenderer uses SkiaSharp to produce PNG images.                   ║
// ║  RenderAsync takes: (component, outputStream, options)                  ║
// ║  With AutoZoom=true (default), the component is automatically           ║
// ║  centered and scaled to fill the output dimensions.                     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Rendering PCB Component to PNG ===");

var rasterRenderer = new RasterRenderer();
var pngPath = Path.Combine(outputDir, "pcb_component.png");
using (var fs = File.Create(pngPath))
    await rasterRenderer.RenderAsync(pcbComponent, fs, new RenderOptions { Width = 512, Height = 512 });

Console.WriteLine($"  PNG saved: {pngPath} ({new FileInfo(pngPath).Length} bytes)");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  3. Render to SVG (vector)                                              ║
// ║                                                                         ║
// ║  SvgRenderer produces SVG XML. No external dependencies needed.         ║
// ║  The same RenderOptions and component interface is used.                ║
// ║  SVG output is ideal for web display or further processing.             ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Rendering PCB Component to SVG ===");

var svgRenderer = new SvgRenderer();
var svgPath = Path.Combine(outputDir, "pcb_component.svg");
using (var fs = File.Create(svgPath))
    await svgRenderer.RenderAsync(pcbComponent, fs, new RenderOptions { Width = 512, Height = 512 });

Console.WriteLine($"  SVG saved: {svgPath} ({new FileInfo(svgPath).Length} bytes)");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  4. Render a schematic component                                        ║
// ║                                                                         ║
// ║  The same renderers work for SchComponent too. Schematic rendering      ║
// ║  draws pins (with designator/name text), polylines, rectangles,         ║
// ║  labels, arcs, etc.                                                     ║
// ║                                                                         ║
// ║  Here we create an op-amp symbol using two construction styles:         ║
// ║    - Polyline body via the fluent builder (SchPolyline.Create())        ║
// ║    - Pins via direct construction (new SchPin { ... })                  ║
// ║  Both approaches are valid and interchangeable.                         ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Rendering Schematic Component to PNG ===");

var schComponent = new SchComponent { Name = "OPAMP" };

// Op-amp triangle body: polyline closing back to the first point
schComponent.AddPolyline(SchPolyline.Create()
    .LineWidth(1).Color(128)
    .From(Coord.FromMm(-2.54), Coord.FromMm(-3.81))    // Bottom-left
    .To(Coord.FromMm(-2.54), Coord.FromMm(3.81))        // Top-left
    .To(Coord.FromMm(5.08), Coord.FromMm(0))             // Right tip
    .To(Coord.FromMm(-2.54), Coord.FromMm(-3.81))        // Close
    .Build());

// Pins using direct construction (alternative to SchPin.Create() builder).
// ShowName/ShowDesignator control whether the pin's name and number are
// rendered as text next to the pin line.
schComponent.AddPin(new SchPin
{
    Location = new CoordPoint(Coord.FromMm(-7.62), Coord.FromMm(1.27)),
    Length = Coord.FromMm(5.08),
    Orientation = PinOrientation.Right,  // Pin line extends to the right
    Designator = "3",
    Name = "+",
    ShowName = true,
    ShowDesignator = true
});

schComponent.AddPin(new SchPin
{
    Location = new CoordPoint(Coord.FromMm(-7.62), Coord.FromMm(-1.27)),
    Length = Coord.FromMm(5.08),
    Orientation = PinOrientation.Right,
    Designator = "2",
    Name = "-",
    ShowName = true,
    ShowDesignator = true
});

schComponent.AddPin(new SchPin
{
    Location = new CoordPoint(Coord.FromMm(10.16), Coord.FromMm(0)),
    Length = Coord.FromMm(5.08),
    Orientation = PinOrientation.Left,   // Pin line extends to the left
    Designator = "1",
    Name = "OUT",
    ShowName = true,
    ShowDesignator = true
});

// SchLabel adds a text annotation at the specified location
schComponent.AddLabel(new SchLabel
{
    Text = "OPAMP",
    Location = new CoordPoint(Coord.FromMm(-1.27), Coord.FromMm(5.08)),
    FontId = 1,                          // Font index (1 = default)
    Color = 128
});

var schPngPath = Path.Combine(outputDir, "sch_component.png");
using (var fs = File.Create(schPngPath))
    await rasterRenderer.RenderAsync(schComponent, fs, new RenderOptions { Width = 512, Height = 512 });

Console.WriteLine($"  PNG saved: {schPngPath} ({new FileInfo(schPngPath).Length} bytes)");

// SVG of the same schematic component
Console.WriteLine("\n=== Rendering Schematic Component to SVG ===");

var schSvgPath = Path.Combine(outputDir, "sch_component.svg");
using (var fs = File.Create(schSvgPath))
    await svgRenderer.RenderAsync(schComponent, fs, new RenderOptions { Width = 512, Height = 512 });

Console.WriteLine($"  SVG saved: {schSvgPath} ({new FileInfo(schSvgPath).Length} bytes)");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  5. CoordTransform: world-to-screen coordinate mapping                  ║
// ║                                                                         ║
// ║  CoordTransform handles the mapping between Altium's internal           ║
// ║  coordinate space (Coord values) and screen pixel positions.            ║
// ║  This is what the renderers use internally, but you can also use it     ║
// ║  directly for custom rendering or hit-testing.                          ║
// ║                                                                         ║
// ║  AutoZoom() calculates Scale and Center to fit a CoordRect into the     ║
// ║  screen dimensions. WorldToScreen() then converts any Coord pair to     ║
// ║  pixel coordinates.                                                     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== CoordTransform Example ===");

var transform = new CoordTransform
{
    ScreenWidth = 1024,
    ScreenHeight = 768
};

// Bounds returns the bounding box (CoordRect) of all primitives
var bounds = pcbComponent.Bounds;
transform.AutoZoom(bounds);

Console.WriteLine($"  Component bounds: ({bounds.Min.X.ToMm():F2}, {bounds.Min.Y.ToMm():F2}) to " +
                  $"({bounds.Max.X.ToMm():F2}, {bounds.Max.Y.ToMm():F2}) mm");
Console.WriteLine($"  Scale: {transform.Scale:F6}");
Console.WriteLine($"  Center: ({transform.CenterX:F0}, {transform.CenterY:F0})");

// Convert the world origin (0,0) to screen pixel coordinates
var (sx, sy) = transform.WorldToScreen(Coord.FromMm(0), Coord.FromMm(0));
Console.WriteLine($"  World (0, 0) -> Screen ({sx:F1}, {sy:F1})");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  6. Layer Colors: predefined color scheme for PCB layers                ║
// ║                                                                         ║
// ║  LayerColors provides Altium's default display colors for each layer.   ║
// ║  GetColor() returns an ARGB uint (0xAARRGGBB).                          ║
// ║  GetDrawPriority() returns the draw order (higher = drawn later/on top).║
// ║  These are used by the renderers but available for custom rendering.    ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Layer Colors ===");

var layers = new (int Id, string Name)[]
{
    (1, "Top Layer"),              // Top copper
    (32, "Bottom Layer"),          // Bottom copper
    (21, "Top Overlay"),           // Top silkscreen
    (22, "Bottom Overlay"),        // Bottom silkscreen
    (57, "Multi Layer"),           // Multi-layer (all copper)
    (74, "Multi Layer (pads)")     // Multi-layer (pad-specific)
};

foreach (var (id, name) in layers)
{
    var color = LayerColors.GetColor(id);
    var priority = LayerColors.GetDrawPriority(id);
    Console.WriteLine($"  Layer {id} ({name}): color=0x{color:X8}, priority={priority}");
}

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  7. Custom RenderOptions                                                ║
// ║                                                                         ║
// ║  RenderOptions controls the output:                                     ║
// ║    Width/Height     - Output image dimensions in pixels                 ║
// ║    BackgroundColor  - ARGB background (0xFF000020 = dark blue)          ║
// ║    AutoZoom         - Auto-fit component to viewport (default: true)    ║
// ║    Scale            - Additional scale factor (default: 1.0)            ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Custom Render Options ===");

var options = new RenderOptions
{
    Width = 2048,
    Height = 2048,
    BackgroundColor = 0xFF000020,        // ARGB: dark blue background
    AutoZoom = true,
    Scale = 1.0
};

var hiResPath = Path.Combine(outputDir, "pcb_hires.png");
using (var fs = File.Create(hiResPath))
    await rasterRenderer.RenderAsync(pcbComponent, fs, options);

Console.WriteLine($"  Hi-res PNG: {hiResPath} ({new FileInfo(hiResPath).Length} bytes)");

Console.WriteLine($"\nAll rendered files are in: {outputDir}");
