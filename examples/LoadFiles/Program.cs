// ============================================================================
// Example: Loading and Inspecting Altium Files
// ============================================================================
//
// This example demonstrates reading all four Altium file types and inspecting
// their contents programmatically.
//
// LOADING APPROACHES
// ──────────────────
// There are two ways to load files:
//
//   1. High-level facade (AltiumLibrary):
//      var lib = await AltiumLibrary.OpenPcbLibAsync("file.PcbLib");
//      Simple, async, auto-detects format. Returns interface types
//      (IPcbLibrary, ISchLibrary, ISchDocument, IPcbDocument).
//
//   2. Low-level readers:
//      var lib = new PcbLibReader().Read(stream);
//      Synchronous, takes a Stream. Useful when you already have the stream
//      or want more control. Returns concrete model types.
//
// Both approaches produce the same result. The high-level API is recommended
// for most use cases.
//
// TYPE HIERARCHY
// ──────────────
// The returned objects use interfaces for the common properties:
//   IPcbLibrary / ISchLibrary : ILibrary  (Count, Components, Contains, Add, Remove)
//   ISchDocument               (Components, Wires, NetLabels, Junctions, etc.)
//   IPcbDocument               (Components, Pads, Tracks, Vias, etc.)
//
// Cast to the concrete type (PcbLibrary, PcbDocument, etc.) to access
// additional features like Diagnostics, Nets, Rules, Polygons, SaveAsync.
//
// This example is self-contained: it creates test files first, then loads
// and inspects them, so you can run it without any existing Altium files.
//
// ============================================================================

using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Enums;
using PinElectricalType = OriginalCircuit.Altium.Models.Sch.PinElectricalType;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;

// Create test files so this example is self-contained (see bottom of file)
var tempDir = Path.Combine(Path.GetTempPath(), "AltiumLoadExample");
Directory.CreateDirectory(tempDir);
CreateTestFiles(tempDir);

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  1. Load a PCB Library (.PcbLib) using the high-level API               ║
// ║                                                                         ║
// ║  AltiumLibrary.OpenPcbLibAsync() accepts a file path or Stream.         ║
// ║  Returns IPcbLibrary with a .Components collection of IComponent.       ║
// ║  Each component exposes typed collections: Pads, Tracks, Arcs, etc.     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("=== Loading PcbLib (high-level API) ===");

var pcbLib = await AltiumLibrary.OpenPcbLibAsync(Path.Combine(tempDir, "Test.PcbLib"));
Console.WriteLine($"  Library has {pcbLib.Count} component(s)");

foreach (var component in pcbLib.Components)
{
    Console.WriteLine($"\n  Component: {component.Name}");
    Console.WriteLine($"    Description: {component.Description}");
    Console.WriteLine($"    Pads: {component.Pads.Count}");
    Console.WriteLine($"    Tracks: {component.Tracks.Count}");
    Console.WriteLine($"    Arcs: {component.Arcs.Count}");

    // Bounds returns a CoordRect (bounding box). Use .ToMm() to convert
    // Coord values to millimeters for display.
    Console.WriteLine($"    Bounds: {component.Bounds.Width.ToMm():F2} x {component.Bounds.Height.ToMm():F2} mm");

    // The Pads collection contains IComponent-level pad references.
    // Cast to PcbPad to access PCB-specific properties like SizeTop, HoleSize.
    foreach (var pad in component.Pads)
    {
        var p = (PcbPad)pad;
        Console.WriteLine($"    Pad {p.Designator}: at ({p.Location.X.ToMm():F2}, {p.Location.Y.ToMm():F2}) mm " +
                          $"size {p.SizeTop.X.ToMm():F2}x{p.SizeTop.Y.ToMm():F2} mm " +
                          $"hole={p.HoleSize.ToMm():F2} mm");
    }
}

// Check if a named component exists (case-insensitive lookup)
Console.WriteLine($"\n  Contains 'R0402': {pcbLib.Contains("R0402")}");
Console.WriteLine($"  Contains 'UNKNOWN': {pcbLib.Contains("UNKNOWN")}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  2. Load a Schematic Library (.SchLib) using the low-level reader       ║
// ║                                                                         ║
// ║  SchLibReader.Read(stream) is synchronous and takes any readable Stream.║
// ║  Returns SchLibrary directly (concrete type, not interface).            ║
// ║  This approach is useful when you already have a stream from a          ║
// ║  database, HTTP response, or zip archive.                               ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Loading SchLib (low-level reader) ===");

using var schLibStream = File.OpenRead(Path.Combine(tempDir, "Test.SchLib"));
var schLib = new SchLibReader().Read(schLibStream);

Console.WriteLine($"  Library has {schLib.Count} component(s)");

foreach (var component in schLib.Components)
{
    Console.WriteLine($"\n  Component: {component.Name}");
    Console.WriteLine($"    Description: {component.Description}");

    // Schematic components have typed collections for each primitive kind
    Console.WriteLine($"    Pins: {component.Pins.Count}");
    Console.WriteLine($"    Rectangles: {component.Rectangles.Count}");
    Console.WriteLine($"    Lines: {component.Lines.Count}");
    Console.WriteLine($"    Polylines: {component.Polylines.Count}");

    // Implementations link the schematic symbol to PCB footprints.
    // Cast to SchComponent to access this (it's not on the interface).
    Console.WriteLine($"    Implementations: {((SchComponent)component).Implementations.Count}");

    // Inspect individual pins. Cast to SchPin for full access to
    // ElectricalType, Orientation, Length, etc.
    foreach (var pin in component.Pins)
    {
        var p = (SchPin)pin;
        Console.WriteLine($"    Pin {p.Designator} ({p.Name}): " +
                          $"at ({p.Location.X.ToMm():F2}, {p.Location.Y.ToMm():F2}) mm " +
                          $"type={p.ElectricalType}");
    }
}

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  3. Load a Schematic Document (.SchDoc)                                 ║
// ║                                                                         ║
// ║  SchDoc files contain placed component instances plus connectivity      ║
// ║  primitives (wires, net labels, junctions, power objects, ports, etc.). ║
// ║  The returned ISchDocument exposes typed collections for each kind.     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Loading SchDoc ===");

var schDoc = await AltiumLibrary.OpenSchDocAsync(Path.Combine(tempDir, "Test.SchDoc"));

Console.WriteLine($"  Components: {schDoc.Components.Count}");
Console.WriteLine($"  Wires: {schDoc.Wires.Count}");
Console.WriteLine($"  Net labels: {schDoc.NetLabels.Count}");
Console.WriteLine($"  Junctions: {schDoc.Junctions.Count}");
Console.WriteLine($"  Power objects: {schDoc.PowerObjects.Count}");

foreach (var nl in schDoc.NetLabels)
    Console.WriteLine($"  Net label: '{nl.Text}' at ({nl.Location.X.ToMm():F2}, {nl.Location.Y.ToMm():F2}) mm");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  4. Load a PCB Document (.PcbDoc)                                       ║
// ║                                                                         ║
// ║  PcbDoc files contain the physical board layout. The base IPcbDocument  ║
// ║  interface gives access to Components, Pads, Tracks, Vias.              ║
// ║  Cast to PcbDocument for additional features: Nets, Polygons, Rules,    ║
// ║  Diagnostics, BoardParameters, SaveAsync.                               ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Loading PcbDoc ===");

var pcbDoc = await AltiumLibrary.OpenPcbDocAsync(Path.Combine(tempDir, "Test.PcbDoc"));

Console.WriteLine($"  Components: {pcbDoc.Components.Count}");
Console.WriteLine($"  Pads: {pcbDoc.Pads.Count}");
Console.WriteLine($"  Tracks: {pcbDoc.Tracks.Count}");
Console.WriteLine($"  Vias: {pcbDoc.Vias.Count}");

// Cast to PcbDocument for Nets, Polygons, Rules, and Diagnostics
var pcbDocument = (PcbDocument)pcbDoc;
Console.WriteLine($"  Nets: {pcbDocument.Nets.Count}");
Console.WriteLine($"  Polygons: {pcbDocument.Polygons.Count}");

foreach (var net in pcbDocument.Nets)
    Console.WriteLine($"  Net: {net.Name}");

// Diagnostics contain non-fatal warnings/errors encountered during loading.
// For example, an unrecognized record type would generate a Warning diagnostic
// rather than throwing an exception, allowing the rest of the file to load.
if (pcbDocument.Diagnostics.Count > 0)
{
    Console.WriteLine("  Diagnostics:");
    foreach (var diag in pcbDocument.Diagnostics)
        Console.WriteLine($"    [{diag.Severity}] {diag.Message}");
}
else
{
    Console.WriteLine("  No diagnostics (clean load)");
}

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  5. Load from a MemoryStream (in-memory)                                ║
// ║                                                                         ║
// ║  All Open*Async methods accept a Stream, so you can load from any       ║
// ║  source: byte arrays, HTTP responses, embedded resources, etc.          ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Loading from MemoryStream ===");

var fileBytes = await File.ReadAllBytesAsync(Path.Combine(tempDir, "Test.PcbLib"));
using var memStream = new MemoryStream(fileBytes);
var memLib = await AltiumLibrary.OpenPcbLibAsync(memStream);
Console.WriteLine($"  Loaded {memLib.Count} component(s) from memory");

// Clean up
Directory.Delete(tempDir, recursive: true);
Console.WriteLine("\nDone!");

// ── Helper: Create test files ───────────────────────────────────────────────
// This creates minimal test files so the loading example is self-contained.
// In a real application you'd load files from disk or a database.

static void CreateTestFiles(string dir)
{
    // PcbLib with one 0402 resistor footprint
    var pcbLib = new PcbLibrary();
    var comp = PcbComponent.Create("R0402")
        .WithDescription("0402 Resistor")
        .AddPad(p => p.At(Coord.FromMm(-0.5), Coord.FromMm(0))
            .Size(Coord.FromMm(0.6), Coord.FromMm(0.8))
            .HoleSize(Coord.FromMm(0)).WithDesignator("1").Layer(1))
        .AddPad(p => p.At(Coord.FromMm(0.5), Coord.FromMm(0))
            .Size(Coord.FromMm(0.6), Coord.FromMm(0.8))
            .HoleSize(Coord.FromMm(0)).WithDesignator("2").Layer(1))
        .AddTrack(t => t.From(Coord.FromMm(-0.75), Coord.FromMm(-0.5))
            .To(Coord.FromMm(0.75), Coord.FromMm(-0.5)).Width(Coord.FromMm(0.12)).Layer(21))
        .AddArc(a => a.At(Coord.FromMm(-0.75), Coord.FromMm(0))
            .Radius(Coord.FromMm(0.12)).Angles(0, 360).Width(Coord.FromMm(0.08)).Layer(21))
        .Build();
    pcbLib.Add(comp);
    using (var fs = File.Create(Path.Combine(dir, "Test.PcbLib")))
        new PcbLibWriter().Write(pcbLib, fs);

    // SchLib with one resistor symbol
    var schLib = new SchLibrary();
    var schComp = new SchComponent { Name = "RES", Description = "Resistor", PartCount = 1 };
    schComp.AddRectangle(new SchRectangle
    {
        Corner1 = new CoordPoint(Coord.FromMm(-1.0), Coord.FromMm(-2.54)),
        Corner2 = new CoordPoint(Coord.FromMm(1.0), Coord.FromMm(2.54)),
        Color = 128
    });
    schComp.AddPin(SchPin.Create("1").WithName("A")
        .At(Coord.FromMm(0), Coord.FromMm(5.08)).Length(Coord.FromMm(2.54))
        .Orient(PinOrientation.Down).Electrical(PinElectricalType.Passive).Build());
    schComp.AddPin(SchPin.Create("2").WithName("B")
        .At(Coord.FromMm(0), Coord.FromMm(-5.08)).Length(Coord.FromMm(2.54))
        .Orient(PinOrientation.Up).Electrical(PinElectricalType.Passive).Build());
    schLib.Add(schComp);
    using (var fs = File.Create(Path.Combine(dir, "Test.SchLib")))
        new SchLibWriter().Write(schLib, fs);

    // SchDoc with a component, wire, net label, junction, and power object
    var schDoc = new SchDocument();
    var r1 = new SchComponent { Name = "R1", PartCount = 1, Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(50.8)) };
    r1.AddPin(SchPin.Create("1").WithName("A").At(Coord.FromMm(0), Coord.FromMm(5.08))
        .Length(Coord.FromMm(2.54)).Orient(PinOrientation.Down).Build());
    schDoc.AddComponent(r1);
    schDoc.AddPrimitive(SchWire.Create()
        .From(Coord.FromMm(50.8), Coord.FromMm(55.88))
        .To(Coord.FromMm(50.8), Coord.FromMm(63.5)).Color(128).Build());
    schDoc.AddPrimitive(new SchNetLabel { Text = "VCC", Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(63.5)), Color = 128 });
    schDoc.AddPrimitive(new SchJunction { Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(63.5)), Color = 128 });
    schDoc.AddPrimitive(new SchPowerObject { Text = "GND", Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(38.1)), Style = PowerPortStyle.Bar, Color = 128 });
    using (var fs = File.Create(Path.Combine(dir, "Test.SchDoc")))
        new SchDocWriter().Write(schDoc, fs);

    // PcbDoc with nets, a component, track, pad, via, and polygon pour
    var pcbDoc = new PcbDocument();
    pcbDoc.AddNet(new PcbNet { Name = "VCC" });
    pcbDoc.AddNet(new PcbNet { Name = "GND" });
    pcbDoc.AddComponent(new PcbComponent { Name = "R1", Description = "Resistor" });
    pcbDoc.AddTrack(new PcbTrack { Start = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)), End = new CoordPoint(Coord.FromMm(2.54), Coord.FromMm(0)), Width = Coord.FromMm(0.254), Layer = 1 });
    pcbDoc.AddPad(new PcbPad { Location = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)), SizeTop = new CoordPoint(Coord.FromMm(1.27), Coord.FromMm(1.27)), HoleSize = Coord.FromMm(0.64), Layer = 74, Designator = "1" });
    pcbDoc.AddVia(new PcbVia { Location = new CoordPoint(Coord.FromMm(2.54), Coord.FromMm(0)), Diameter = Coord.FromMm(1.0), HoleSize = Coord.FromMm(0.5) });
    var poly = new PcbPolygon { Layer = 1, Net = "GND", Name = "Pour", PolygonType = 1 };
    poly.AddVertex(new CoordPoint(Coord.FromMm(-5.08), Coord.FromMm(-5.08)));
    poly.AddVertex(new CoordPoint(Coord.FromMm(7.62), Coord.FromMm(-5.08)));
    poly.AddVertex(new CoordPoint(Coord.FromMm(7.62), Coord.FromMm(5.08)));
    poly.AddVertex(new CoordPoint(Coord.FromMm(-5.08), Coord.FromMm(5.08)));
    pcbDoc.AddPolygon(poly);
    using (var fs = File.Create(Path.Combine(dir, "Test.PcbDoc")))
        new PcbDocWriter().Write(pcbDoc, fs);
}
