// ============================================================================
// Example: Creating Altium Files from Scratch
// ============================================================================
//
// This example demonstrates creating all four Altium file types:
//
//   .PcbLib  - PCB footprint library (physical land patterns for soldering)
//   .SchLib  - Schematic symbol library (logical symbols for circuit diagrams)
//   .SchDoc  - Schematic document (a circuit diagram sheet with placed symbols)
//   .PcbDoc  - PCB document (a board layout with routed copper)
//
// KEY CONCEPTS
// ────────────
// Libraries (.PcbLib, .SchLib) are collections of reusable components.
// Documents (.SchDoc, .PcbDoc) are design files that reference/contain components.
//
// All coordinates use the Coord struct, a fixed-point integer internally.
// Use Coord.FromMm() for metric values. The library also supports
// Coord.FromMils() (thousandths of an inch) and Coord.FromInches().
//
// There are two ways to construct objects:
//   1. Fluent builder API:  PcbComponent.Create("name").AddPad(...).Build()
//   2. Direct construction: new PcbPad { Location = ..., SizeTop = ... }
// Both approaches are shown below.
//
// SAVING FILES
// ────────────
// Two approaches for writing files to disk:
//   1. Low-level:  new PcbLibWriter().Write(model, stream)
//   2. High-level: await model.SaveAsync("path.PcbLib")
// Both are demonstrated. The low-level writer gives you control over the
// stream; the high-level API is more convenient for simple cases.
//
// ============================================================================

using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Writers;

var outputDir = Path.Combine(Path.GetTempPath(), "AltiumExamples");
Directory.CreateDirectory(outputDir);
Console.WriteLine($"Output directory: {outputDir}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  1. PCB Footprint Library (.PcbLib)                                     ║
// ║                                                                         ║
// ║  A PcbLib contains one or more "footprints" - the physical copper       ║
// ║  patterns on a PCB that components are soldered onto. Each footprint    ║
// ║  is a PcbComponent containing pads, tracks (lines), arcs, and text.    ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Creating PcbLib ===");

var pcbLib = new PcbLibrary();

// ── Approach 1: Fluent builder API ──────────────────────────────────────────
//
// PcbComponent.Create("name") starts a builder chain. Each .AddXxx() method
// takes a lambda that configures the primitive. Call .Build() at the end.
//
// This creates an 0402-size SMD resistor footprint with:
//   - Two surface-mount pads (no hole, Layer 1 = Top Layer)
//   - A silkscreen courtyard outline (four tracks on Layer 21 = Top Overlay)
//   - A designator text label (".Designator" is a special token that Altium
//     replaces with the actual reference designator like "R1", "R2", etc.)

var resistorFp = PcbComponent.Create("R0402")
    .WithDescription("0402 Resistor Footprint")
    .WithHeight(Coord.FromMm(0.4))            // Component height (for 3D)

    // Pad 1: left side. HoleSize=0 means SMD (surface-mount, no drill hole).
    // Layer 1 = Top Copper. For through-hole pads, use Layer 74 (Multi-layer).
    .AddPad(pad => pad
        .At(Coord.FromMm(-0.5), Coord.FromMm(0))     // Center position
        .Size(Coord.FromMm(0.6), Coord.FromMm(0.8))   // Width x Height
        .HoleSize(Coord.FromMm(0))                     // 0 = SMD pad
        .WithDesignator("1")                            // Pad number
        .Layer(1))                                      // 1 = Top Layer

    // Pad 2: right side, mirrored
    .AddPad(pad => pad
        .At(Coord.FromMm(0.5), Coord.FromMm(0))
        .Size(Coord.FromMm(0.6), Coord.FromMm(0.8))
        .HoleSize(Coord.FromMm(0))
        .WithDesignator("2")
        .Layer(1))

    // Silkscreen courtyard: four tracks forming a rectangle on the overlay layer.
    // Layer 21 = Top Overlay (silkscreen). Width = line thickness.
    .AddTrack(track => track
        .From(Coord.FromMm(-0.75), Coord.FromMm(-0.5))
        .To(Coord.FromMm(0.75), Coord.FromMm(-0.5))
        .Width(Coord.FromMm(0.12))
        .Layer(21))                                     // 21 = Top Overlay
    .AddTrack(track => track
        .From(Coord.FromMm(0.75), Coord.FromMm(-0.5))
        .To(Coord.FromMm(0.75), Coord.FromMm(0.5))
        .Width(Coord.FromMm(0.12))
        .Layer(21))
    .AddTrack(track => track
        .From(Coord.FromMm(0.75), Coord.FromMm(0.5))
        .To(Coord.FromMm(-0.75), Coord.FromMm(0.5))
        .Width(Coord.FromMm(0.12))
        .Layer(21))
    .AddTrack(track => track
        .From(Coord.FromMm(-0.75), Coord.FromMm(0.5))
        .To(Coord.FromMm(-0.75), Coord.FromMm(-0.5))
        .Width(Coord.FromMm(0.12))
        .Layer(21))

    // ".Designator" is a special string: Altium shows the component's reference
    // designator (e.g. "R1") in its place on the PCB.
    .AddText(".Designator", text => text
        .At(Coord.FromMm(0), Coord.FromMm(1.0))
        .Height(Coord.FromMm(0.8))
        .Layer(21))

    .Build();

pcbLib.Add(resistorFp);

// ── Approach 2: Build skeleton, then add primitives imperatively ────────────
//
// You can also call .Build() early to get a PcbComponent, then use .AddPad()
// directly. This is useful when creating pads in a loop.
//
// DIP-8: a classic through-hole package. Pads 1-4 on the bottom row,
// pads 5-8 on the top row (mirrored), at 2.54mm pitch (standard DIP).

var dip8 = PcbComponent.Create("DIP8")
    .WithDescription("8-pin DIP Package")
    .WithHeight(Coord.FromMm(3.0))
    .Build();

// Through-hole pads use Layer 74 (Multi-layer), meaning the pad exists on
// all copper layers. HoleSize > 0 creates a drill hole.
for (var i = 0; i < 4; i++)
{
    // Bottom row: pins 1-4, left to right
    dip8.AddPad(new PcbPad
    {
        Location = new CoordPoint(Coord.FromMm(-3.81 + i * 2.54), Coord.FromMm(-3.81)),
        SizeTop = new CoordPoint(Coord.FromMm(1.5), Coord.FromMm(1.5)),
        HoleSize = Coord.FromMm(0.8),
        Layer = 74,                     // 74 = Multi-layer (through-hole)
        Designator = (i + 1).ToString()
    });
    // Top row: pins 5-8, right to left (DIP convention)
    dip8.AddPad(new PcbPad
    {
        Location = new CoordPoint(Coord.FromMm(3.81 - i * 2.54), Coord.FromMm(3.81)),
        SizeTop = new CoordPoint(Coord.FromMm(1.5), Coord.FromMm(1.5)),
        HoleSize = Coord.FromMm(0.8),
        Layer = 74,
        Designator = (i + 5).ToString()
    });
}

pcbLib.Add(dip8);

// Save using the low-level writer: create a stream, pass the model.
var pcbLibPath = Path.Combine(outputDir, "MyComponents.PcbLib");
using (var fs = File.Create(pcbLibPath))
    new PcbLibWriter().Write(pcbLib, fs);

Console.WriteLine($"  Created: {pcbLibPath}");
Console.WriteLine($"  Components: {pcbLib.Count}");
foreach (var comp in pcbLib.Components)
    Console.WriteLine($"    - {comp.Name}: {comp.Pads.Count} pads, {comp.Tracks.Count} tracks");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  2. Schematic Symbol Library (.SchLib)                                  ║
// ║                                                                         ║
// ║  A SchLib contains schematic symbols - the logical representations      ║
// ║  of components used in circuit diagrams. Each symbol (SchComponent)     ║
// ║  has pins, graphical primitives (rectangles, lines, polylines), and     ║
// ║  labels. Pins connect to wires in the schematic.                        ║
// ║                                                                         ║
// ║  Schematic coordinates use the same Coord system but values are         ║
// ║  typically on a 2.54mm (100-mil) grid, the standard schematic grid.    ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Creating SchLib ===");

var schLib = new SchLibrary();

// ── Resistor symbol: rectangle body + 2 passive pins ────────────────────────
//
// SchComponent is created with direct construction (not a builder).
// PartCount = 1 means this is a single-part component (vs multi-part ICs
// where PartCount might be 4 for a quad op-amp, etc.).

var resistorSym = new SchComponent
{
    Name = "RES",
    Description = "Generic Resistor",
    PartCount = 1                       // Single-part component
};

// Graphical body: a filled rectangle. Corner1/Corner2 are opposite corners.
// Color is a 24-bit BGR value (Altium convention). 128 = dark green.
resistorSym.AddRectangle(new SchRectangle
{
    Corner1 = new CoordPoint(Coord.FromMm(-1.0), Coord.FromMm(-2.54)),
    Corner2 = new CoordPoint(Coord.FromMm(1.0), Coord.FromMm(2.54)),
    Color = 128,                        // Line color (BGR: dark green)
    IsFilled = true,
    FillColor = 0xFFFFFF                // Fill color (white)
});

// Pin positions are relative to the component origin (0,0).
// Orient specifies which direction the pin "points" - i.e. which direction
// the wire extends from the component body. A pin with Orient=Down at Y=5.08
// means the pin sticks out downward from the top of the body.
// Length is how far the pin line extends from its connection point.
//
// PinElectricalType affects ERC (Electrical Rules Check) in Altium:
//   Passive  = resistors, capacitors (no direction)
//   Input    = signal goes in
//   Output   = signal comes out
//   Power    = power pins (VCC, GND)
resistorSym.AddPin(SchPin.Create("1")           // "1" = designator
    .WithName("1")                               // Display name on schematic
    .At(Coord.FromMm(0), Coord.FromMm(5.08))    // Position (top)
    .Length(Coord.FromMm(2.54))                  // Pin line length
    .Orient(PinOrientation.Down)                 // Pin extends downward
    .Electrical(PinElectricalType.Passive)       // Passive (for ERC)
    .Build());

resistorSym.AddPin(SchPin.Create("2")
    .WithName("2")
    .At(Coord.FromMm(0), Coord.FromMm(-5.08))   // Position (bottom)
    .Length(Coord.FromMm(2.54))
    .Orient(PinOrientation.Up)                   // Pin extends upward
    .Electrical(PinElectricalType.Passive)
    .Build());

schLib.Add(resistorSym);

// ── Op-amp symbol: polyline triangle body + 3 pins ──────────────────────────
//
// More complex symbols use polylines for arbitrary shapes. Here we draw
// the classic op-amp triangle using a closed polyline (4 points, last = first).

var opamp = new SchComponent
{
    Name = "OPAMP",
    Description = "Operational Amplifier",
    PartCount = 1
};

// SchPolyline.Create() starts a fluent builder for polylines.
// .From() sets the first point, .To() appends subsequent points.
// Closing the shape (last point = first point) draws a triangle.
opamp.AddPolyline(SchPolyline.Create()
    .LineWidth(1)                               // Line width in schematic units
    .Color(128)
    .From(Coord.FromMm(-2.54), Coord.FromMm(-3.81))    // Bottom-left
    .To(Coord.FromMm(-2.54), Coord.FromMm(3.81))       // Top-left
    .To(Coord.FromMm(5.08), Coord.FromMm(0))            // Right (tip)
    .To(Coord.FromMm(-2.54), Coord.FromMm(-3.81))       // Close triangle
    .Build());

// Non-inverting input (+): pin extends rightward from the left side.
// Pin location is where the wire connects; the pin line extends from there
// toward the component body for the specified Length.
opamp.AddPin(SchPin.Create("3")
    .WithName("+")
    .At(Coord.FromMm(-7.62), Coord.FromMm(1.27))       // Left, slightly above center
    .Length(Coord.FromMm(5.08))                          // 5.08mm pin length
    .Orient(PinOrientation.Right)                        // Pin extends rightward
    .Electrical(PinElectricalType.Input)
    .Build());

// Inverting input (-)
opamp.AddPin(SchPin.Create("2")
    .WithName("-")
    .At(Coord.FromMm(-7.62), Coord.FromMm(-1.27))      // Left, slightly below center
    .Length(Coord.FromMm(5.08))
    .Orient(PinOrientation.Right)
    .Electrical(PinElectricalType.Input)
    .Build());

// Output pin: extends leftward from the right side
opamp.AddPin(SchPin.Create("1")
    .WithName("OUT")
    .At(Coord.FromMm(10.16), Coord.FromMm(0))           // Right side
    .Length(Coord.FromMm(5.08))
    .Orient(PinOrientation.Left)                         // Pin extends leftward
    .Electrical(PinElectricalType.Output)
    .Build());

schLib.Add(opamp);

var schLibPath = Path.Combine(outputDir, "MySymbols.SchLib");
using (var fs = File.Create(schLibPath))
    new SchLibWriter().Write(schLib, fs);

Console.WriteLine($"  Created: {schLibPath}");
Console.WriteLine($"  Components: {schLib.Count}");
foreach (var comp in schLib.Components)
    Console.WriteLine($"    - {comp.Name}: {comp.Pins.Count} pins");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  3. Schematic Document (.SchDoc)                                        ║
// ║                                                                         ║
// ║  A SchDoc is a single schematic sheet. It contains placed component     ║
// ║  instances, wires connecting them, net labels (naming nets), power       ║
// ║  symbols (VCC, GND), and junctions (wire crossing points).              ║
// ║                                                                         ║
// ║  Components placed here are instances (with Location) rather than       ║
// ║  library definitions. Wires create electrical connections between pins. ║
// ║  Net labels assign net names to wire segments.                          ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Creating SchDoc ===");

var schDoc = new SchDocument();

// Optional header parameters control the sheet format and fonts.
// These mirror what Altium stores internally. The HEADER string is required
// for Altium to recognize the file format version.
schDoc.HeaderParameters = new Dictionary<string, string>
{
    ["HEADER"] = "Protel for Windows - Schematic Capture Binary File Version 5.0",
    ["SHEETSTYLE"] = "4",              // Sheet size (4 = A4)
    ["SYSTEMFONT"] = "1",
    ["FONTNAME1"] = "Times New Roman",
    ["SIZE1"] = "10"
};

// Place a resistor component instance at position (50.8, 50.8) mm.
// The component's pins are at positions relative to this Location.
var r1 = new SchComponent
{
    Name = "RES",
    Description = "10k Resistor",
    PartCount = 1,
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(50.8))
};
r1.AddPin(SchPin.Create("1")
    .WithName("1")
    .At(Coord.FromMm(0), Coord.FromMm(5.08))    // Relative to component origin
    .Length(Coord.FromMm(2.54))
    .Orient(PinOrientation.Down)
    .Build());
r1.AddPin(SchPin.Create("2")
    .WithName("2")
    .At(Coord.FromMm(0), Coord.FromMm(-5.08))
    .Length(Coord.FromMm(2.54))
    .Orient(PinOrientation.Up)
    .Build());
schDoc.AddComponent(r1);

// Wires create electrical connections. SchWire.Create() returns a fluent
// builder. Use .From() and .To() for two-point wires, or chain multiple
// .AddPoint() calls for multi-segment wires.
// Wire 1: from the top pin upward to a VCC connection point
var wire1 = SchWire.Create()
    .From(Coord.FromMm(50.8), Coord.FromMm(55.88))   // Start (near pin 1)
    .To(Coord.FromMm(50.8), Coord.FromMm(63.5))       // End (VCC location)
    .Color(128)
    .Build();
schDoc.AddPrimitive(wire1);

// Wire 2: from the bottom pin downward to a GND connection point
var wire2 = SchWire.Create()
    .From(Coord.FromMm(50.8), Coord.FromMm(45.72))
    .To(Coord.FromMm(50.8), Coord.FromMm(38.1))
    .Color(128)
    .Build();
schDoc.AddPrimitive(wire2);

// Net labels assign a name to a wire segment. All wires/pins touching the
// same net label location become part of that named net.
schDoc.AddPrimitive(new SchNetLabel
{
    Text = "VCC",
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(63.5)),
    Color = 128
});

// Power objects are special symbols (ground bars, power flags, etc.).
// PowerPortStyle controls the visual style of the symbol.
schDoc.AddPrimitive(new SchPowerObject
{
    Text = "GND",
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(38.1)),
    Style = PowerPortStyle.Bar,         // Bar-style ground symbol
    Color = 128
});

// Junctions mark intentional wire crossings/connections (the filled dot
// where wires meet). Without a junction, crossing wires are not connected.
schDoc.AddPrimitive(new SchJunction
{
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(63.5)),
    Color = 128
});

var schDocPath = Path.Combine(outputDir, "MySchematic.SchDoc");
using (var fs = File.Create(schDocPath))
    new SchDocWriter().Write(schDoc, fs);

Console.WriteLine($"  Created: {schDocPath}");
Console.WriteLine($"  Components: {schDoc.Components.Count}");
Console.WriteLine($"  Wires: {schDoc.Wires.Count}");
Console.WriteLine($"  Net labels: {schDoc.NetLabels.Count}");
Console.WriteLine($"  Power objects: {schDoc.PowerObjects.Count}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  4. PCB Document (.PcbDoc)                                              ║
// ║                                                                         ║
// ║  A PcbDoc represents a physical PCB layout. It contains:                ║
// ║    - Nets (named electrical connections, e.g. "VCC", "GND")             ║
// ║    - Components (placed footprint instances)                            ║
// ║    - Pads, Tracks, Vias, Arcs (copper primitives)                       ║
// ║    - Polygons (copper pour regions)                                     ║
// ║    - Rules (design rules like clearances)                               ║
// ║                                                                         ║
// ║  Common layer IDs:                                                      ║
// ║    1  = Top Copper       32 = Bottom Copper                             ║
// ║    21 = Top Overlay       22 = Bottom Overlay (silkscreen)              ║
// ║    29 = Top Solder Mask   30 = Bottom Solder Mask                       ║
// ║    74 = Multi-layer (through-hole pads/vias, all copper layers)         ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Creating PcbDoc ===");

var pcbDoc = new PcbDocument();

// Board-level parameters are stored as a string dictionary.
// BOARDTHICKNESS is in internal Altium units (raw Coord value).
pcbDoc.BoardParameters = new Dictionary<string, string>
{
    ["LAYER1NAME"] = "TopLayer",
    ["LAYER32NAME"] = "BottomLayer",
    ["BOARDTHICKNESS"] = "1600000"      // ~1.6mm in raw units
};

// Nets define the electrical connectivity. Every pad, track, and via can
// reference a net by name to indicate which signal it carries.
pcbDoc.AddNet(new PcbNet { Name = "VCC" });
pcbDoc.AddNet(new PcbNet { Name = "GND" });

// Component placement: this is just the component's reference on the board.
// The actual pads are added separately (in a real file, they'd be linked).
pcbDoc.AddComponent(new PcbComponent
{
    Name = "R1",
    Description = "0402 Resistor"
});

// SMD pads (HoleSize = 0, Layer 1 = Top Copper)
pcbDoc.AddPad(new PcbPad
{
    Location = new CoordPoint(Coord.FromMm(25.4), Coord.FromMm(25.4)),
    SizeTop = new CoordPoint(Coord.FromMm(0.6), Coord.FromMm(0.8)),
    HoleSize = Coord.FromMm(0),         // 0 = surface-mount pad
    Layer = 1,                           // Top copper only
    Designator = "1"
});

pcbDoc.AddPad(new PcbPad
{
    Location = new CoordPoint(Coord.FromMm(26.4), Coord.FromMm(25.4)),
    SizeTop = new CoordPoint(Coord.FromMm(0.6), Coord.FromMm(0.8)),
    HoleSize = Coord.FromMm(0),
    Layer = 1,
    Designator = "2"
});

// Tracks are copper traces connecting pads. Width = trace width.
pcbDoc.AddTrack(new PcbTrack
{
    Start = new CoordPoint(Coord.FromMm(26.4), Coord.FromMm(25.4)),
    End = new CoordPoint(Coord.FromMm(30.48), Coord.FromMm(25.4)),
    Width = Coord.FromMm(0.254),         // 0.254mm = 10 mil trace
    Layer = 1
});

// Vias connect copper between layers (e.g., route a signal from top to bottom).
// Diameter = annular ring outer diameter, HoleSize = drill diameter.
pcbDoc.AddVia(new PcbVia
{
    Location = new CoordPoint(Coord.FromMm(30.48), Coord.FromMm(25.4)),
    Diameter = Coord.FromMm(1.0),        // Outer copper ring
    HoleSize = Coord.FromMm(0.5)         // Drill hole
});

// Arcs can be silkscreen markings or copper traces. StartAngle=0, EndAngle=360
// draws a full circle. This creates a circular marker on the overlay layer.
pcbDoc.AddArc(new PcbArc
{
    Center = new CoordPoint(Coord.FromMm(25.9), Coord.FromMm(25.4)),
    Radius = Coord.FromMm(0.75),
    StartAngle = 0,
    EndAngle = 360,                      // Full circle
    Width = Coord.FromMm(0.12),          // Line thickness
    Layer = 21                           // Top Overlay (silkscreen)
});

// Polygons define copper pour regions (e.g., ground planes). Vertices define
// the pour boundary; Altium fills the interior with copper connected to the
// specified net, respecting clearance rules.
var polygon = new PcbPolygon
{
    Layer = 1,                           // Top copper
    Net = "GND",                         // Connected to GND net
    Name = "GND_Pour",
    PolygonType = 1                      // Solid fill
};
polygon.AddVertex(new CoordPoint(Coord.FromMm(22.86), Coord.FromMm(22.86)));
polygon.AddVertex(new CoordPoint(Coord.FromMm(33.02), Coord.FromMm(22.86)));
polygon.AddVertex(new CoordPoint(Coord.FromMm(33.02), Coord.FromMm(27.94)));
polygon.AddVertex(new CoordPoint(Coord.FromMm(22.86), Coord.FromMm(27.94)));
pcbDoc.AddPolygon(polygon);

// Save using the high-level async API (alternative to the Writer approach).
var pcbDocPath = Path.Combine(outputDir, "MyBoard.PcbDoc");
await pcbDoc.SaveAsync(pcbDocPath);

Console.WriteLine($"  Created: {pcbDocPath}");
Console.WriteLine($"  Components: {pcbDoc.Components.Count}");
Console.WriteLine($"  Tracks: {pcbDoc.Tracks.Count}");
Console.WriteLine($"  Pads: {pcbDoc.Pads.Count}");
Console.WriteLine($"  Vias: {pcbDoc.Vias.Count}");
Console.WriteLine($"  Nets: {pcbDoc.Nets.Count}");
Console.WriteLine($"  Polygons: {pcbDoc.Polygons.Count}");

Console.WriteLine("\nAll files created successfully!");
