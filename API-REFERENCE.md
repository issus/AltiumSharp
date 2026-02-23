# OriginalCircuit.Altium V2 - API Reference

> Comprehensive API documentation for the OriginalCircuit.Altium library.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Coordinate System](#coordinate-system)
- [Creating Files](#creating-files)
  - [PCB Footprint Library (PcbLib)](#pcb-footprint-library-pcblib)
  - [Schematic Symbol Library (SchLib)](#schematic-symbol-library-schlib)
  - [Schematic Document (SchDoc)](#schematic-document-schdoc)
  - [PCB Document (PcbDoc)](#pcb-document-pcbdoc)
- [Loading Files](#loading-files)
  - [High-Level API (AltiumLibrary)](#high-level-api-altiumlibrary)
  - [Low-Level Readers](#low-level-readers)
  - [Loading from Streams](#loading-from-streams)
- [Modifying Files](#modifying-files)
- [Rendering](#rendering)
  - [Raster (PNG)](#raster-png)
  - [SVG](#svg)
  - [CoordTransform](#coordtransform)
  - [Layer Colors](#layer-colors)
- [PCB Fluent Builders](#pcb-fluent-builders)
- [Schematic Fluent Builders](#schematic-fluent-builders)
- [Error Handling](#error-handling)
- [Complete Type Reference](#complete-type-reference)
- [Layer ID Reference](#layer-id-reference)

---

## Overview

OriginalCircuit.Altium reads and writes Altium Designer files:

| File Type | Extension | Model Class | Purpose |
|-----------|-----------|-------------|---------|
| PCB Footprint Library | `.PcbLib` | `PcbLibrary` | Collection of PCB footprints |
| Schematic Symbol Library | `.SchLib` | `SchLibrary` | Collection of schematic symbols |
| Schematic Document | `.SchDoc` | `SchDocument` | Schematic sheet with placed components |
| PCB Document | `.PcbDoc` | `PcbDocument` | PCB layout with routed board |

**Target Framework:** .NET 10.0

**NuGet Packages:**

| Package | Description |
|---------|-------------|
| `OriginalCircuit.Altium` | Core library (models, readers, writers) |
| `OriginalCircuit.Altium.Rendering.Core` | Rendering abstractions |
| `OriginalCircuit.Altium.Rendering.Raster` | PNG output via SkiaSharp |
| `OriginalCircuit.Altium.Rendering.Svg` | SVG output (no external deps) |

---

## Installation

### Project Reference (local)

```xml
<ItemGroup>
  <ProjectReference Include="path/to/src/OriginalCircuit.Altium/OriginalCircuit.Altium.csproj" />
  <!-- Optional: for rendering -->
  <ProjectReference Include="path/to/src/OriginalCircuit.Altium.Rendering.Core/OriginalCircuit.Altium.Rendering.Core.csproj" />
  <ProjectReference Include="path/to/src/OriginalCircuit.Altium.Rendering.Raster/OriginalCircuit.Altium.Rendering.Raster.csproj" />
  <ProjectReference Include="path/to/src/OriginalCircuit.Altium.Rendering.Svg/OriginalCircuit.Altium.Rendering.Svg.csproj" />
</ItemGroup>
```

### Required Namespaces

```csharp
using OriginalCircuit.Altium;                       // AltiumLibrary facade
using OriginalCircuit.Altium.Models.Pcb;             // PcbLibrary, PcbDocument, PcbComponent, etc.
using OriginalCircuit.Altium.Models.Sch;             // SchLibrary, SchDocument, SchComponent, etc.
using OriginalCircuit.Altium.Primitives;             // Coord, CoordPoint, CoordRect, enums
using OriginalCircuit.Altium.Extensions;             // .Mils(), .Mm() extension methods
using OriginalCircuit.Altium.Serialization.Writers;  // PcbLibWriter, SchLibWriter, etc.
using OriginalCircuit.Altium.Serialization.Readers;  // PcbLibReader, SchLibReader, etc.
using OriginalCircuit.Altium.Rendering;              // CoordTransform, RenderOptions, LayerColors
using OriginalCircuit.Altium.Rendering.Raster;       // RasterRenderer
using OriginalCircuit.Altium.Rendering.Svg;          // SvgRenderer
using OriginalCircuit.Altium.Diagnostics;            // AltiumDiagnostic, DiagnosticSeverity
```

---

## Quick Start

```csharp
using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Primitives;

// Create a library with one footprint
var lib = AltiumLibrary.CreatePcbLib();
var comp = PcbComponent.Create("R0402")
    .WithDescription("0402 Resistor")
    .AddPad(p => p.At(Coord.FromMm(-0.5), Coord.FromMm(0))
        .Size(Coord.FromMm(0.6), Coord.FromMm(0.8)).WithDesignator("1").Layer(1))
    .AddPad(p => p.At(Coord.FromMm(0.5), Coord.FromMm(0))
        .Size(Coord.FromMm(0.6), Coord.FromMm(0.8)).WithDesignator("2").Layer(1))
    .Build();
lib.Add(comp);
await lib.SaveAsync("output.PcbLib");

// Read it back
var loaded = await AltiumLibrary.OpenPcbLibAsync("output.PcbLib");
Console.WriteLine($"{loaded.Count} components");
```

---

## Coordinate System

The library uses a fixed-point integer coordinate system. The `Coord` struct handles all conversions.

### Coord (readonly struct)

**Namespace:** `OriginalCircuit.Altium.Primitives`

**Internal unit:** 10,000 units = 1 mil (thousandth of an inch), ~393,701 units = 1 mm

```csharp
// Creation
var c1 = Coord.FromMm(2.54);          // 2.54 mm
var c2 = Coord.FromMils(100);         // 100 mils (= 2.54 mm)
var c3 = Coord.FromInches(0.1);       // 0.1 inches (= 2.54 mm)
var c4 = Coord.FromRaw(2540000);      // Raw internal value

// Conversion
double mm = c1.ToMm();                // 2.54
double mils = c1.ToMils();            // 100.0
double inches = c1.ToInches();        // 0.1
int raw = c1.ToRaw();                 // 2540000

// Parsing (supports "mil", "mm", "in" suffixes; defaults to mils)
var c5 = Coord.Parse("2.54mm");
var c6 = Coord.Parse("100mil");
Coord.TryParse("1.27mm", out var c7);

// Constants
Coord.Zero      // 0
Coord.OneMil    // 1 mil
Coord.OneMm     // 1 mm
Coord.OneInch   // 1 inch

// Arithmetic
var sum = c1 + c2;
var diff = c1 - c2;
var scaled = c1 * 2;
var half = c1 / 2;
var neg = -c1;

// Comparison (implements IComparable<Coord>)
bool eq = c1 == c2;
bool lt = c1 < c2;
bool gt = c1 > c2;

// Helpers
Coord.Min(c1, c2);
Coord.Max(c1, c2);
Coord.Abs(c1);

// Ratio (Coord / Coord returns double)
double ratio = c1 / c2;

// Formatting (supports "mil", "mm", "in", "raw" format specifiers)
string s = c1.ToString("mm");    // e.g. "2.54mm"
```

### Extension Methods

**Namespace:** `OriginalCircuit.Altium.Extensions`

```csharp
using OriginalCircuit.Altium.Extensions;

0.5.Mm()         // double -> Coord (0.5 mm)
2.54.Mm()        // double -> Coord (2.54 mm)
100.Mils()       // int -> Coord (100 mils)
1.0.Inches()     // double -> Coord (1 inch)
```

### CoordPoint (readonly struct)

```csharp
var pt = new CoordPoint(Coord.FromMm(2.54), Coord.FromMm(5.08));
Coord x = pt.X;                        // X coordinate
Coord y = pt.Y;                        // Y coordinate
var (x2, y2) = pt;                     // Deconstruct
var moved = pt.Offset(0.5.Mm(), 0.Mm());
double dist = pt.DistanceTo(CoordPoint.Zero);   // Distance in mils
var rotated = pt.Rotate(90);           // Rotate 90 degrees around origin
var rotated2 = pt.RotateAround(center, 45);     // Rotate around a point

// Arithmetic
var sum = pt + otherPt;
var diff = pt - otherPt;
var scaled = pt * 2.0;
var neg = -pt;

// Constants and equality
CoordPoint.Zero                        // (0, 0)
bool eq = pt == otherPt;
```

### CoordRect (readonly struct)

```csharp
// Construction
var rect = new CoordRect(
    Coord.FromMm(0), Coord.FromMm(0),         // min X, min Y
    Coord.FromMm(2.54), Coord.FromMm(5.08));  // max X, max Y
var rect2 = new CoordRect(pointA, pointB);     // From two CoordPoints
var rect3 = CoordRect.FromCenterAndSize(center, width, height);

// Properties
var center = rect.Center;              // CoordPoint
var width = rect.Width;                // Coord
var height = rect.Height;              // Coord
CoordPoint min = rect.Min;             // Bottom-left corner
CoordPoint max = rect.Max;             // Top-right corner
bool empty = rect.IsEmpty;

// Operations
bool hit = rect.Contains(new CoordPoint(1.27.Mm(), 1.27.Mm()));
bool overlaps = rect.Intersects(otherRect);
var intersection = rect.Intersect(otherRect);
var bigger = rect.Inflate(0.25.Mm());
var combined = rect.Union(otherRect);

// Constants
CoordRect.Empty                        // Empty rectangle
```

---

## Creating Files

### PCB Footprint Library (PcbLib)

```csharp
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Writers;

var pcbLib = new PcbLibrary();

// Fluent builder API for creating components
var resistor = PcbComponent.Create("R0402")
    .WithDescription("0402 Resistor Footprint")
    .WithHeight(Coord.FromMm(0.4))
    .AddPad(pad => pad
        .At(Coord.FromMm(-0.5), Coord.FromMm(0))
        .Size(Coord.FromMm(0.6), Coord.FromMm(0.8))
        .HoleSize(Coord.FromMm(0))
        .WithDesignator("1")
        .Layer(1))                      // Top Layer
    .AddPad(pad => pad
        .At(Coord.FromMm(0.5), Coord.FromMm(0))
        .Size(Coord.FromMm(0.6), Coord.FromMm(0.8))
        .HoleSize(Coord.FromMm(0))
        .WithDesignator("2")
        .Layer(1))
    .AddTrack(track => track            // Silkscreen courtyard
        .From(Coord.FromMm(-0.75), Coord.FromMm(-0.5))
        .To(Coord.FromMm(0.75), Coord.FromMm(-0.5))
        .Width(Coord.FromMm(0.12))
        .Layer(21))                     // Top Overlay
    .AddArc(arc => arc                  // Pin 1 marker
        .At(Coord.FromMm(-0.75), Coord.FromMm(0))
        .Radius(Coord.FromMm(0.12))
        .Angles(0, 360)
        .Width(Coord.FromMm(0.08))
        .Layer(21))
    .AddText(".Designator", text => text
        .At(Coord.FromMm(0), Coord.FromMm(1.0))
        .Height(Coord.FromMm(0.8))
        .Layer(21))
    .Build();

pcbLib.Add(resistor);

// Alternative: build component then add primitives imperatively
var dip8 = PcbComponent.Create("DIP8")
    .WithDescription("8-pin DIP Package")
    .WithHeight(Coord.FromMm(3.0))
    .Build();

for (var i = 0; i < 4; i++)
{
    dip8.AddPad(new PcbPad
    {
        Location = new CoordPoint(Coord.FromMm(-3.81 + i * 2.54), Coord.FromMm(-3.81)),
        SizeTop = new CoordPoint(Coord.FromMm(1.5), Coord.FromMm(1.5)),
        HoleSize = Coord.FromMm(0.8),
        Layer = 74,     // Multi-layer (through-hole)
        Designator = (i + 1).ToString()
    });
}
pcbLib.Add(dip8);

// Save
using var fs = File.Create("MyComponents.PcbLib");
new PcbLibWriter().Write(pcbLib, fs);
```

### Schematic Symbol Library (SchLib)

```csharp
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Writers;

var schLib = new SchLibrary();

// Create component with direct object construction + pin builders
var resistor = new SchComponent
{
    Name = "RES",
    Description = "Generic Resistor",
    PartCount = 1
};

// Rectangular body
resistor.AddRectangle(new SchRectangle
{
    Corner1 = new CoordPoint(Coord.FromMm(-1.0), Coord.FromMm(-2.54)),
    Corner2 = new CoordPoint(Coord.FromMm(1.0), Coord.FromMm(2.54)),
    Color = 128,
    IsFilled = true,
    FillColor = 0xFFFFFF
});

// Pins using fluent builder
resistor.AddPin(SchPin.Create("1")
    .WithName("A")
    .At(Coord.FromMm(0), Coord.FromMm(5.08))
    .Length(Coord.FromMm(2.54))
    .Orient(PinOrientation.Down)
    .Electrical(PinElectricalType.Passive)
    .Build());

resistor.AddPin(SchPin.Create("2")
    .WithName("B")
    .At(Coord.FromMm(0), Coord.FromMm(-5.08))
    .Length(Coord.FromMm(2.54))
    .Orient(PinOrientation.Up)
    .Electrical(PinElectricalType.Passive)
    .Build());

schLib.Add(resistor);

// Create an op-amp using polylines for the triangle body
var opamp = new SchComponent
{
    Name = "OPAMP",
    Description = "Operational Amplifier",
    PartCount = 1
};

opamp.AddPolyline(SchPolyline.Create()
    .LineWidth(1).Color(128)
    .From(Coord.FromMm(-2.54), Coord.FromMm(-3.81))
    .To(Coord.FromMm(-2.54), Coord.FromMm(3.81))
    .To(Coord.FromMm(5.08), Coord.FromMm(0))
    .To(Coord.FromMm(-2.54), Coord.FromMm(-3.81))
    .Build());

opamp.AddPin(SchPin.Create("3").WithName("+")
    .At(Coord.FromMm(-7.62), Coord.FromMm(1.27))
    .Length(Coord.FromMm(5.08)).Orient(PinOrientation.Right)
    .Electrical(PinElectricalType.Input).Build());

opamp.AddPin(SchPin.Create("2").WithName("-")
    .At(Coord.FromMm(-7.62), Coord.FromMm(-1.27))
    .Length(Coord.FromMm(5.08)).Orient(PinOrientation.Right)
    .Electrical(PinElectricalType.Input).Build());

opamp.AddPin(SchPin.Create("1").WithName("OUT")
    .At(Coord.FromMm(10.16), Coord.FromMm(0))
    .Length(Coord.FromMm(5.08)).Orient(PinOrientation.Left)
    .Electrical(PinElectricalType.Output).Build());

schLib.Add(opamp);

using var fs = File.Create("MySymbols.SchLib");
new SchLibWriter().Write(schLib, fs);
```

### Schematic Document (SchDoc)

```csharp
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Writers;

var schDoc = new SchDocument();

// Optional: set document header parameters
schDoc.HeaderParameters = new Dictionary<string, string>
{
    ["HEADER"] = "Protel for Windows - Schematic Capture Binary File Version 5.0",
    ["SHEETSTYLE"] = "4",
    ["SYSTEMFONT"] = "1",
    ["FONTNAME1"] = "Times New Roman",
    ["SIZE1"] = "10"
};

// Place a component
var r1 = new SchComponent
{
    Name = "R1",
    PartCount = 1,
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(50.8))
};
r1.AddPin(SchPin.Create("1").WithName("A")
    .At(Coord.FromMm(0), Coord.FromMm(5.08))
    .Length(Coord.FromMm(2.54)).Orient(PinOrientation.Down).Build());
r1.AddPin(SchPin.Create("2").WithName("B")
    .At(Coord.FromMm(0), Coord.FromMm(-5.08))
    .Length(Coord.FromMm(2.54)).Orient(PinOrientation.Up).Build());
schDoc.AddComponent(r1);

// Add wires (using fluent builder)
schDoc.AddPrimitive(SchWire.Create()
    .From(Coord.FromMm(50.8), Coord.FromMm(55.88))
    .To(Coord.FromMm(50.8), Coord.FromMm(63.5))
    .Color(128)
    .Build());

// Add net labels
schDoc.AddPrimitive(new SchNetLabel
{
    Text = "VCC",
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(63.5)),
    Color = 128
});

// Add a power ground symbol
schDoc.AddPrimitive(new SchPowerObject
{
    Text = "GND",
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(38.1)),
    Style = PowerPortStyle.Bar,
    Color = 128
});

// Add a junction
schDoc.AddPrimitive(new SchJunction
{
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(63.5)),
    Color = 128
});

// Add a sheet symbol (hierarchical design)
schDoc.AddPrimitive(new SchSheetSymbol
{
    Location = new CoordPoint(Coord.FromMm(76.2), Coord.FromMm(50.8)),
    XSize = Coord.FromMm(10.16),
    YSize = Coord.FromMm(7.62),
    FileName = "SubSheet.SchDoc",
    SheetName = "SubSheet",
    Color = 128
});

// Add a port
schDoc.AddPrimitive(new SchPort
{
    Name = "DATA_IN",
    Location = new CoordPoint(Coord.FromMm(25.4), Coord.FromMm(50.8)),
    IoType = 1,
    Style = 3,
    Width = Coord.FromMm(5.08),
    Height = Coord.FromMm(0.76),
    Color = 128
});

using var fs = File.Create("MySchematic.SchDoc");
new SchDocWriter().Write(schDoc, fs);
```

### PCB Document (PcbDoc)

```csharp
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Writers;

var pcbDoc = new PcbDocument();

// Board parameters
pcbDoc.BoardParameters = new Dictionary<string, string>
{
    ["LAYER1NAME"] = "TopLayer",
    ["LAYER32NAME"] = "BottomLayer",
    ["BOARDTHICKNESS"] = "1600000"
};

// Define nets
pcbDoc.AddNet(new PcbNet { Name = "VCC" });
pcbDoc.AddNet(new PcbNet { Name = "GND" });

// Place components
pcbDoc.AddComponent(new PcbComponent { Name = "R1", Description = "0402 Resistor" });

// Add pads
pcbDoc.AddPad(new PcbPad
{
    Location = new CoordPoint(Coord.FromMm(25.4), Coord.FromMm(25.4)),
    SizeTop = new CoordPoint(Coord.FromMm(0.6), Coord.FromMm(0.8)),
    HoleSize = Coord.FromMm(0),
    Layer = 1,
    Designator = "1"
});

// Add tracks
pcbDoc.AddTrack(new PcbTrack
{
    Start = new CoordPoint(Coord.FromMm(25.4), Coord.FromMm(25.4)),
    End = new CoordPoint(Coord.FromMm(30.48), Coord.FromMm(25.4)),
    Width = Coord.FromMm(0.254),
    Layer = 1
});

// Add vias
pcbDoc.AddVia(new PcbVia
{
    Location = new CoordPoint(Coord.FromMm(30.48), Coord.FromMm(25.4)),
    Diameter = Coord.FromMm(1.0),
    HoleSize = Coord.FromMm(0.5)
});

// Add arcs
pcbDoc.AddArc(new PcbArc
{
    Center = new CoordPoint(Coord.FromMm(25.9), Coord.FromMm(25.4)),
    Radius = Coord.FromMm(0.75),
    StartAngle = 0,
    EndAngle = 360,
    Width = Coord.FromMm(0.12),
    Layer = 21
});

// Add a copper pour polygon
var polygon = new PcbPolygon
{
    Layer = 1, Net = "GND", Name = "GND_Pour", PolygonType = 1
};
polygon.AddVertex(new CoordPoint(Coord.FromMm(22.86), Coord.FromMm(22.86)));
polygon.AddVertex(new CoordPoint(Coord.FromMm(33.02), Coord.FromMm(22.86)));
polygon.AddVertex(new CoordPoint(Coord.FromMm(33.02), Coord.FromMm(27.94)));
polygon.AddVertex(new CoordPoint(Coord.FromMm(22.86), Coord.FromMm(27.94)));
pcbDoc.AddPolygon(polygon);

// Add design rules
pcbDoc.AddRule(new PcbRule
{
    Name = "Clearance_1",
    RuleKind = "Clearance",
    Enabled = true,
    Priority = 1,
    UniqueId = "CLR001",
    Scope1Expression = "All",
    Scope2Expression = "All",
    Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["NAME"] = "Clearance_1", ["RULEKIND"] = "Clearance",
        ["ENABLED"] = "TRUE", ["PRIORITY"] = "1",
        ["SCOPE1EXPRESSION"] = "All", ["SCOPE2EXPRESSION"] = "All",
        ["GAP"] = "0.254mm"
    }
});

// Save (async high-level API)
await pcbDoc.SaveAsync("MyBoard.PcbDoc");
```

---

## Loading Files

### High-Level API (AltiumLibrary)

```csharp
using OriginalCircuit.Altium;

// Auto-detect library type from extension (.PcbLib or .SchLib only)
var library = await AltiumLibrary.OpenAsync("parts.PcbLib");  // Returns ILibrary

// Type-specific loaders
var pcbLib = await AltiumLibrary.OpenPcbLibAsync("footprints.PcbLib");
var schLib = await AltiumLibrary.OpenSchLibAsync("symbols.SchLib");
var schDoc = await AltiumLibrary.OpenSchDocAsync("schematic.SchDoc");
var pcbDoc = await AltiumLibrary.OpenPcbDocAsync("board.PcbDoc");

// With cancellation
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var lib = await AltiumLibrary.OpenPcbLibAsync("large.PcbLib", cts.Token);
```

### Low-Level Readers

```csharp
using OriginalCircuit.Altium.Serialization.Readers;

// Synchronous read from stream
using var stream = File.OpenRead("parts.PcbLib");
var lib = new PcbLibReader().Read(stream);

// Async read from file path
var lib2 = await new SchLibReader().ReadAsync("symbols.SchLib");
```

### Loading from Streams

```csharp
// In-memory round-trip
var fileBytes = await File.ReadAllBytesAsync("parts.PcbLib");
using var memStream = new MemoryStream(fileBytes);
var lib = await AltiumLibrary.OpenPcbLibAsync(memStream);
```

### Inspecting Loaded Data

```csharp
// PcbLib
var pcbLib = await AltiumLibrary.OpenPcbLibAsync("parts.PcbLib");
Console.WriteLine($"Components: {pcbLib.Count}");
Console.WriteLine($"Contains R0402: {pcbLib.Contains("R0402")}");

foreach (var component in pcbLib.Components)
{
    Console.WriteLine($"{component.Name}: {component.Description}");
    Console.WriteLine($"  Pads: {component.Pads.Count}");
    Console.WriteLine($"  Tracks: {component.Tracks.Count}");
    Console.WriteLine($"  Bounds: {component.Bounds.Width.ToMm():F2} x {component.Bounds.Height.ToMm():F2} mm");

    foreach (var pad in component.Pads)
    {
        var p = (PcbPad)pad;
        Console.WriteLine($"  Pad {p.Designator}: ({p.Location.X.ToMm():F2}, {p.Location.Y.ToMm():F2}) mm");
    }
}

// SchDoc
var schDoc = await AltiumLibrary.OpenSchDocAsync("schematic.SchDoc");
Console.WriteLine($"Components: {schDoc.Components.Count}");
Console.WriteLine($"Wires: {schDoc.Wires.Count}");
Console.WriteLine($"Net labels: {schDoc.NetLabels.Count}");
Console.WriteLine($"Power objects: {schDoc.PowerObjects.Count}");

// PcbDoc - cast to PcbDocument for advanced features
var pcbDoc = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync("board.PcbDoc");
Console.WriteLine($"Nets: {pcbDoc.Nets.Count}");
Console.WriteLine($"Polygons: {pcbDoc.Polygons.Count}");
Console.WriteLine($"Rules: {pcbDoc.Rules.Count}");

// Check diagnostics
foreach (var diag in pcbDoc.Diagnostics)
    Console.WriteLine($"[{diag.Severity}] {diag.Message}");
```

---

## Modifying Files

The typical workflow is: load -> modify -> save.

```csharp
// Load a PcbLib
var lib = (PcbLibrary)await AltiumLibrary.OpenPcbLibAsync("parts.PcbLib");

// Remove a component
lib.Remove("OLD_PART");

// Add a new component
lib.Add(PcbComponent.Create("NEW_PART")
    .WithDescription("New footprint")
    .AddPad(p => p.At(Coord.FromMm(0), Coord.FromMm(0))
        .Size(Coord.FromMm(1.27), Coord.FromMm(1.27)).WithDesignator("1").Layer(74))
    .Build());

// Save back
await lib.SaveAsync("parts.PcbLib");

// Load a SchLib and modify a component
var schLib = (SchLibrary)await AltiumLibrary.OpenSchLibAsync("symbols.SchLib");
var comp = (SchComponent)schLib.Components.First();
comp.AddLabel(new SchLabel
{
    Text = "R",
    Location = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
    FontId = 1,
    Color = 128
});

// Save with writer
using var fs = File.Create("symbols.SchLib");
new SchLibWriter().Write(schLib, fs);

// Load a SchDoc and add wires
var schDoc = (SchDocument)await AltiumLibrary.OpenSchDocAsync("schematic.SchDoc");
schDoc.AddPrimitive(SchWire.Create()
    .From(Coord.FromMm(50.8), Coord.FromMm(55.88))
    .To(Coord.FromMm(63.5), Coord.FromMm(55.88))
    .Color(128)
    .Build());

// Load a PcbDoc and add tracks
var pcbDoc = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync("board.PcbDoc");
pcbDoc.AddTrack(new PcbTrack
{
    Start = new CoordPoint(Coord.FromMm(12.7), Coord.FromMm(0)),
    End = new CoordPoint(Coord.FromMm(12.7), Coord.FromMm(7.62)),
    Width = Coord.FromMm(0.254),
    Layer = 1
});
```

---

## Rendering

### Raster (PNG)

```csharp
using OriginalCircuit.Altium.Rendering;
using OriginalCircuit.Altium.Rendering.Raster;

var renderer = new RasterRenderer();

// Render a PCB component
var pcbComponent = PcbComponent.Create("R0402")
    .AddPad(p => p.At(Coord.FromMm(0), Coord.FromMm(0))
        .Size(Coord.FromMm(1.5), Coord.FromMm(0.6)).WithDesignator("1").Layer(1))
    .Build();

using var ms = new MemoryStream();
await renderer.RenderAsync(pcbComponent, ms, new RenderOptions { Width = 512, Height = 512 });
// ms now contains PNG data

// Render to file
await renderer.RenderAsync(pcbComponent, "output.png", new RenderOptions { Width = 2048, Height = 2048 });

// Render a schematic component
var schComponent = new SchComponent { Name = "R1" };
schComponent.AddRectangle(new SchRectangle
{
    Corner1 = new CoordPoint(Coord.FromMm(-1.27), Coord.FromMm(-2.54)),
    Corner2 = new CoordPoint(Coord.FromMm(1.27), Coord.FromMm(2.54)),
    Color = 0x00FF0000,
    FillColor = 0x0000FFFF,
    IsFilled = true
});
await renderer.RenderAsync(schComponent, "sch_output.png", new RenderOptions { Width = 256, Height = 256 });
```

### SVG

```csharp
using OriginalCircuit.Altium.Rendering;
using OriginalCircuit.Altium.Rendering.Svg;

var renderer = new SvgRenderer();

using var ms = new MemoryStream();
await renderer.RenderAsync(component, ms, new RenderOptions { Width = 512, Height = 512 });

// Read the SVG string
ms.Position = 0;
var svgContent = new StreamReader(ms).ReadToEnd();
```

### RenderOptions

```csharp
var options = new RenderOptions
{
    Width = 1024,              // Output width in pixels (default: 1024)
    Height = 768,              // Output height in pixels (default: 768)
    BackgroundColor = 0xFFFFFFFF,  // ARGB background (default: white)
    AutoZoom = true,           // Auto-fit component to viewport (default: true)
    Scale = 1.0                // Scale factor (default: 1.0)
};
```

### CoordTransform

```csharp
using OriginalCircuit.Altium.Rendering;

var transform = new CoordTransform
{
    ScreenWidth = 800,
    ScreenHeight = 600
};

// Auto-zoom to fit bounds
var bounds = component.Bounds;
transform.AutoZoom(bounds);

// Convert world coordinates to screen coordinates
var (sx, sy) = transform.WorldToScreen(Coord.FromMm(0), Coord.FromMm(0));
```

### Layer Colors

```csharp
using OriginalCircuit.Altium.Rendering;

uint color = LayerColors.GetColor(1);        // Top Layer -> ARGB color
int priority = LayerColors.GetDrawPriority(1); // Draw order (higher = drawn later)
```

---

## PCB Fluent Builders

All PCB primitives have a `Create()` static method returning a fluent builder.

### PcbPad

```csharp
// SMD pad
var smd = PcbPad.Create("A1")
    .At(0.5.Mm(), 0.Mm())
    .Size(0.6.Mm(), 0.8.Mm())
    .Shape(PadShape.RoundedRectangle)
    .CornerRadius(50)               // 50% corner radius
    .Rotation(45)                   // Rotation in degrees
    .Net("VCC")                     // Assign to net
    .Smd()                          // Sets Layer=1, HoleSize=0
    .Build();

// Through-hole pad
var th = PcbPad.Create("1")
    .At(0.Mm(), 0.Mm())
    .Size(1.5.Mm(), 1.5.Mm())
    .Shape(PadShape.Round)
    .ThroughHole(0.8.Mm())         // Sets HoleSize, IsPlated=true
    .Layer(74)                      // Multi-layer for through-hole
    .Build();
```

### PcbTrack

```csharp
var track = PcbTrack.Create()
    .From(0.Mm(), 0.Mm())
    .To(2.54.Mm(), 2.54.Mm())
    .Width(0.254.Mm())
    .OnLayer(1)
    .Net("VCC")
    .Build();
```

### PcbVia

```csharp
// Through-hole via
var via = PcbVia.Create()
    .At(1.27.Mm(), 1.27.Mm())
    .Diameter(0.5.Mm())
    .HoleSize(0.25.Mm())
    .ThroughHole()                  // StartLayer=1, EndLayer=32
    .Net("GND")
    .Build();

// Blind via
var blind = PcbVia.Create()
    .At(0.Mm(), 0.Mm())
    .Diameter(0.5.Mm())
    .HoleSize(0.25.Mm())
    .Blind(1, 2)                    // Start layer, end layer
    .Tented()
    .Build();
```

### PcbArc

```csharp
var arc = PcbArc.Create()
    .Center(0.Mm(), 0.Mm())         // or .At(x, y)
    .Radius(1.27.Mm())
    .FullCircle()                   // StartAngle=0, EndAngle=360
    .Width(0.254.Mm())
    .Layer(21)                      // .OnLayer() also works
    .Build();

// Partial arc
var partial = PcbArc.Create()
    .Center(0.Mm(), 0.Mm())
    .Radius(1.Mm())
    .Angles(0, 180)                 // Start angle, end angle
    .Width(0.254.Mm())
    .Build();
```

### PcbText

```csharp
var text = PcbText.Create(".Designator")
    .At(0.Mm(), 2.Mm())
    .Height(1.Mm())
    .StrokeWidth(0.15.Mm())
    .Rotation(90)
    .Justify(TextJustification.MiddleCenter)
    .Layer(21)
    .Build();

// TrueType font text
var ttText = PcbText.Create("Hello")
    .At(0.Mm(), 0.Mm())
    .Height(1.Mm())
    .TrueType("Arial")
    .Mirrored(false)
    .Build();
```

### PcbFill

```csharp
var fill = PcbFill.Create()
    .From(0.Mm(), 0.Mm())
    .To(2.54.Mm(), 1.27.Mm())
    .OnLayer(1)
    .Rotation(45)
    .Build();
```

### PcbRegion

```csharp
var region = PcbRegion.Create()
    .AddPoint(0.Mm(), 0.Mm())
    .AddPoint(2.54.Mm(), 0.Mm())
    .AddPoint(2.54.Mm(), 1.27.Mm())
    .AddPoint(0.Mm(), 1.27.Mm())
    .OnLayer(1)
    .Net("GND")
    .Build();
```

### PcbComponentBody

```csharp
var body = PcbComponentBody.Create()
    .AddPoint(0.Mm(), 0.Mm())
    .AddPoint(2.Mm(), 0.Mm())
    .AddPoint(2.Mm(), 1.Mm())
    .AddPoint(0.Mm(), 1.Mm())
    .StandoffHeight(0.Mm())
    .OverallHeight(1.Mm())
    .WithStepModel("model-id")
    .Build();
```

### PcbComponent

```csharp
var component = PcbComponent.Create("R0402")
    .WithDescription("0402 Resistor Footprint")
    .WithHeight(0.35.Mm())
    .AddPad(pad => pad
        .At(-0.5.Mm(), 0.Mm())
        .Size(0.5.Mm(), 0.6.Mm())
        .Shape(PadShape.RoundedRectangle)
        .Smd())
    .AddPad(pad => pad
        .At(0.5.Mm(), 0.Mm())
        .Size(0.5.Mm(), 0.6.Mm())
        .Shape(PadShape.RoundedRectangle)
        .Smd())
    .AddText(".Designator", text => text
        .At(0.Mm(), 0.8.Mm())
        .Height(0.6.Mm()))
    .Build();

// Implicit conversion also works (Build() is optional in assignment)
PcbComponent comp = PcbComponent.Create("TEST").WithDescription("Test");
```

---

## Schematic Fluent Builders

### SchPin

```csharp
var pin = SchPin.Create("1")
    .WithName("INPUT")
    .At(Coord.FromMm(-5.08), Coord.FromMm(0))
    .Length(Coord.FromMm(5.08))
    .Orient(PinOrientation.Right)
    .Electrical(PinElectricalType.Input)
    .Build();
```

### SchWire

```csharp
var wire = SchWire.Create()
    .From(Coord.FromMm(0), Coord.FromMm(0))
    .To(Coord.FromMm(7.62), Coord.FromMm(0))
    .AddPoint(Coord.FromMm(7.62), Coord.FromMm(5.08))  // Multi-segment
    .Color(128)
    .Build();
```

### SchPolyline

```csharp
var polyline = SchPolyline.Create()
    .LineWidth(1)
    .Color(128)
    .From(Coord.FromMm(-2.54), Coord.FromMm(-3.81))
    .To(Coord.FromMm(-2.54), Coord.FromMm(3.81))
    .To(Coord.FromMm(5.08), Coord.FromMm(0))
    .To(Coord.FromMm(-2.54), Coord.FromMm(-3.81))
    .Build();
```

### SchPolygon

```csharp
var polygon = SchPolygon.Create()
    .AddVertex(Coord.FromMm(0), Coord.FromMm(0))
    .AddVertex(Coord.FromMm(2.54), Coord.FromMm(0))
    .AddVertex(Coord.FromMm(1.27), Coord.FromMm(2.54))
    .Color(128)
    .Filled(true)
    .FillColor(0x00FF00)
    .Build();
```

### SchBezier

```csharp
var bezier = SchBezier.Create()
    .AddPoint(Coord.FromMm(0), Coord.FromMm(0))
    .AddPoint(Coord.FromMm(1.27), Coord.FromMm(2.54))
    .AddPoint(Coord.FromMm(2.54), Coord.FromMm(2.54))
    .AddPoint(Coord.FromMm(3.81), Coord.FromMm(0))
    .Color(128)
    .Build();
```

### SchComponent (builder)

```csharp
var comp = SchComponent.Create("RESISTOR")
    .WithDescription("Generic Resistor")
    .WithDesignatorPrefix("R")
    .WithPartCount(1)
    .AddRectangle(rect => rect
        .From(Coord.FromMm(-1.27), Coord.FromMm(-2.54))
        .To(Coord.FromMm(1.27), Coord.FromMm(2.54))
        .LineWidth(Coord.FromMm(0.254)))
    .AddPin(pin => pin
        .WithName("A")
        .At(Coord.FromMm(-3.81), Coord.FromMm(0))
        .Length(Coord.FromMm(2.54))
        .Orient(PinOrientation.Right))
    .Build();

// Note: the lambda-based AddPin does not support setting the designator.
// To set pin designators, use the standalone builder and add directly:
comp.AddPin(SchPin.Create("1")
    .WithName("A")
    .At(Coord.FromMm(-3.81), Coord.FromMm(0))
    .Length(Coord.FromMm(2.54))
    .Orient(PinOrientation.Right)
    .Electrical(PinElectricalType.Passive)
    .Build());
```

---

## Error Handling

### Exception Hierarchy

```
AltiumFileException                     (base, has FilePath)
  ├── AltiumCorruptFileException        (adds StreamName)
  └── AltiumUnsupportedFeatureException (adds RecordType)
```

### Diagnostics

Non-fatal issues are collected in the `Diagnostics` property instead of throwing:

```csharp
var lib = (PcbLibrary)await AltiumLibrary.OpenPcbLibAsync("parts.PcbLib");

foreach (var diag in lib.Diagnostics)
{
    // diag.Severity: Info, Warning, Error
    // diag.Message: human-readable description
    // diag.StreamName: which stream in the compound file
    // diag.RecordIndex: which record number
    Console.WriteLine($"[{diag.Severity}] {diag.Message}");
}
```

### Handling Corrupt Files

```csharp
try
{
    var lib = await AltiumLibrary.OpenPcbLibAsync("corrupt.PcbLib");
}
catch (AltiumCorruptFileException ex)
{
    Console.WriteLine($"Corrupt file: {ex.FilePath}");
    Console.WriteLine($"Stream: {ex.StreamName}");
    Console.WriteLine($"Details: {ex.Message}");
}
```

---

## Complete Type Reference

### PCB Primitive Types

| Type | Key Properties | Builder |
|------|---------------|---------|
| `PcbPad` | `Designator`, `Location`, `SizeTop`, `SizeBottom`, `HoleSize`, `ShapeTop`, `ShapeBottom`, `Layer`, `Rotation`, `IsPlated`, `CornerRadiusPercentage` | `PcbPad.Create(designator)` |
| `PcbTrack` | `Start`, `End`, `Width`, `Layer`, `Net` | `PcbTrack.Create()` |
| `PcbVia` | `Location`, `Diameter`, `HoleSize`, `StartLayer`, `EndLayer`, `Net`, `IsTented` | `PcbVia.Create()` |
| `PcbArc` | `Center`, `Radius`, `StartAngle`, `EndAngle`, `Width`, `Layer` | `PcbArc.Create()` |
| `PcbText` | `Text`, `Location`, `Height`, `StrokeWidth`, `Rotation`, `Layer`, `FontName`, `Justification` | `PcbText.Create(text)` |
| `PcbFill` | `Corner1`, `Corner2`, `Layer`, `Rotation` | `PcbFill.Create()` |
| `PcbRegion` | `Outline` (vertices), `Layer` | `PcbRegion.Create()` |
| `PcbComponentBody` | `Outline` (vertices), `Layer` | `PcbComponentBody.Create()` |

### PCB Document Types

| Type | Key Properties |
|------|---------------|
| `PcbNet` | `Name` |
| `PcbPolygon` | `Layer`, `Net`, `Name`, `PolygonType`, `Vertices` (via `AddVertex()`) |
| `PcbRule` | `Name`, `RuleKind`, `Enabled`, `Priority`, `Comment`, `UniqueId`, `Scope1Expression`, `Scope2Expression`, `Parameters` |
| `PcbObjectClass` | `Name`, `Kind`, `Members`, `Parameters` |
| `PcbDifferentialPair` | `Name`, `PositiveNetName`, `NegativeNetName` |
| `PcbRoom` | `Name`, `UniqueId`, `Parameters` |
| `PcbEmbeddedBoard` | `DocumentPath`, `Layer`, `Rotation`, `ColCount`, `RowCount`, `ColSpacing`, `RowSpacing` |
| `PcbModel` | `Id`, `Name`, `IsEmbedded`, `StepData`, `RotationX/Y/Z`, `Dz` |

### Schematic Primitive Types

| Type | Key Properties | Builder |
|------|---------------|---------|
| `SchPin` | `Designator`, `Name`, `Location`, `Length`, `Orientation`, `ElectricalType`, `ShowName`, `ShowDesignator` | `SchPin.Create(designator)` |
| `SchWire` | `Vertices`, `Color`, `LineWidth`, `LineStyle` | `SchWire.Create()` |
| `SchLine` | `Start`, `End`, `Color`, `Width` | `SchLine.Create()` |
| `SchRectangle` | `Corner1`, `Corner2`, `Color`, `FillColor`, `IsFilled`, `LineWidth` | `SchRectangle.Create()` |
| `SchLabel` | `Text`, `Location`, `Color`, `FontId` | `SchLabel.Create(text)` |
| `SchArc` | `Center`, `Radius`, `StartAngle`, `EndAngle`, `Color`, `LineWidth` | `SchArc.Create()` |
| `SchEllipse` | `Center`, `RadiusX`, `RadiusY`, `Color`, `FillColor`, `IsFilled` | `SchEllipse.Create()` |
| `SchEllipticalArc` | `Center`, `PrimaryRadius`, `SecondaryRadius`, `StartAngle`, `EndAngle` | `SchEllipticalArc.Create()` |
| `SchPolyline` | `Vertices`, `Color`, `LineWidth` | `SchPolyline.Create()` |
| `SchPolygon` | `Vertices`, `Color`, `FillColor`, `IsFilled` | `SchPolygon.Create()` |
| `SchBezier` | `ControlPoints`, `Color`, `LineWidth` | `SchBezier.Create()` |
| `SchRoundedRectangle` | `Corner1`, `Corner2`, `CornerRadiusX`, `CornerRadiusY`, `Color`, `FillColor` | `SchRoundedRectangle.Create()` |
| `SchPie` | `Center`, `Radius`, `StartAngle`, `EndAngle`, `Color`, `FillColor` | `SchPie.Create()` |
| `SchNetLabel` | `Text`, `Location`, `Color`, `FontId`, `Orientation` | `SchNetLabel.Create(text)` |
| `SchJunction` | `Location`, `Color`, `Size` | `SchJunction.Create()` |
| `SchParameter` | `Name`, `Value`, `Location`, `Color`, `FontId`, `IsVisible` | `SchParameter.Create(name)` |
| `SchTextFrame` | `Corner1`, `Corner2`, `Text`, `FontId`, `ShowBorder`, `IsFilled` | `SchTextFrame.Create(text)` |
| `SchImage` | `Corner1`, `Corner2`, `ImageData`, `Filename`, `EmbedImage`, `KeepAspect` | `SchImage.Create()` |
| `SchPowerObject` | `Text`, `Location`, `Style`, `Color`, `ShowNetName` | `SchPowerObject.Create(netName)` |
| `SchPort` | `Name`, `Location`, `IoType`, `Style`, `Width`, `Height` | Direct construction |
| `SchSheetSymbol` | `Location`, `XSize`, `YSize`, `FileName`, `SheetName`, `Entries` | Direct construction |
| `SchSheetEntry` | `Name`, `IoType`, `Side`, `DistanceFromTop` | Direct construction |
| `SchBus` | `Vertices`, `Color`, `LineWidth` (via `AddVertex()`) | Direct construction |
| `SchBusEntry` | `Location`, `Corner`, `Color` | Direct construction |
| `SchBlanket` | `Vertices`, `Color` (via `AddVertex()`) | Direct construction |
| `SchNoErc` | `Location`, `IsActive`, `Color` | Direct construction |

### Enums

**PCB:**
| Enum | Values |
|------|--------|
| `PadShape` | `Round=1`, `Rectangular=2`, `Octagonal=3`, `RoundedRectangle=9` |
| `PadHoleType` | `Round=0`, `Square=1`, `Slot=2` |
| `PcbTextKind` | `Stroke=0`, `TrueType=1`, `BarCode=2` |
| `TextJustification` | `BottomLeft(0)` through `TopRight(8)` (3x3 grid) |

**Schematic:**
| Enum | Values |
|------|--------|
| `PinOrientation` | `Right=0`, `Up=1`, `Left=2`, `Down=3` |
| `PinElectricalType` | `Input=0`, `InputOutput=1`, `Output=2`, `OpenCollector=3`, `Passive=4`, `HiZ=5`, `OpenEmitter=6`, `Power=7` |
| `PowerPortStyle` | `Circle=0`, `Arrow=1`, `Bar=2`, `Wave=3`, `PowerGround=4`, `SignalGround=5`, `Earth=6`, `GostArrow=7`, `GostPowerGround=8`, `GostEarth=9`, `GostBar=10` |
| `SchLineStyle` | `Solid=0`, `Dashed=1`, `Dotted=2` |

---

## Layer ID Reference

Common Altium layer IDs used in the `Layer` property:

| ID | Name | Description |
|----|------|-------------|
| 1 | Top Layer | Top copper |
| 2-31 | Mid Layer 1-30 | Internal copper layers |
| 32 | Bottom Layer | Bottom copper |
| 21 | Top Overlay | Top silkscreen |
| 22 | Bottom Overlay | Bottom silkscreen |
| 25 | Top Paste | Top solder paste |
| 26 | Bottom Paste | Bottom solder paste |
| 29 | Top Solder | Top solder mask |
| 30 | Bottom Solder | Bottom solder mask |
| 33 | Drill Guide | Drill guide |
| 34 | Keep-Out Layer | Board outline |
| 39 | Mechanical 1 | Mechanical layer 1 |
| 57 | Multi Layer | Multi-layer (all copper) |
| 74 | Multi Layer | Multi-layer (pads) |

---

## Saving Files

### High-Level API (SaveAsync)

All model types support `SaveAsync`:

```csharp
// Save to file path
await library.SaveAsync("output.PcbLib");

// Save to stream
using var ms = new MemoryStream();
await library.SaveAsync(ms);

// With options and cancellation
await library.SaveAsync("output.PcbLib",
    options: new SaveOptions { Overwrite = true },  // default is true
    cancellationToken: token);
```

### Low-Level Writers

Writers have both synchronous `Write()` and asynchronous `WriteAsync()` methods.
Note: writers take **concrete types** (e.g. `PcbLibrary`), not interfaces.

```csharp
using OriginalCircuit.Altium.Serialization.Writers;

// Synchronous
using var stream = File.Create("output.PcbLib");
new PcbLibWriter().Write(pcbLibrary, stream);

// Async
await new PcbLibWriter().WriteAsync(pcbLibrary, stream);

// All writer types (sync shown, async variants also available):
new PcbLibWriter().Write(pcbLibrary, stream);       // PcbLibrary
new SchLibWriter().Write(schLibrary, stream);       // SchLibrary
new SchDocWriter().Write(schDocument, stream);      // SchDocument
new PcbDocWriter().Write(pcbDocument, stream);      // PcbDocument
```

### Factory Methods

```csharp
var pcbLib = AltiumLibrary.CreatePcbLib();     // Empty IPcbLibrary
var schLib = AltiumLibrary.CreateSchLib();      // Empty ISchLibrary
var schDoc = AltiumLibrary.CreateSchDoc();      // Empty ISchDocument
var pcbDoc = AltiumLibrary.CreatePcbDoc();      // Empty IPcbDocument
```

---

## 3D Models

PcbLib files can contain embedded STEP 3D models:

```csharp
var library = new PcbLibrary();

// Add a component
library.Add(PcbComponent.Create("SOT23").Build());

// Add an embedded 3D model
library.Models.Add(new PcbModel
{
    Id = "3D-MODEL-001",
    Name = "SOT23.step",
    IsEmbedded = true,
    ModelSource = "Undefined",         // Default for embedded models
    RotationX = 0, RotationY = 0, RotationZ = 90.0,
    Dz = 50,
    StepData = "ISO-10303-21;\nHEADER;\n..."  // STEP file content
});
```

---

## Complete Working Examples

The `examples/` directory contains 4 fully tested console applications:

| Example | Description |
|---------|-------------|
| `examples/CreateFiles/` | Creates PcbLib, SchLib, SchDoc, PcbDoc from scratch |
| `examples/LoadFiles/` | Loads and inspects all 4 file types |
| `examples/ModifyFiles/` | Loads files, makes changes, saves and verifies |
| `examples/RenderFiles/` | Renders PCB and schematic components to PNG and SVG |

Build and run any example:
```bash
dotnet run --project examples/CreateFiles/CreateFiles.csproj
dotnet run --project examples/LoadFiles/LoadFiles.csproj
dotnet run --project examples/ModifyFiles/ModifyFiles.csproj
dotnet run --project examples/RenderFiles/RenderFiles.csproj
```
