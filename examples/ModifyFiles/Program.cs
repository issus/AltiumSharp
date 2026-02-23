// ============================================================================
// Example: Modifying Existing Altium Files
// ============================================================================
//
// This example demonstrates the load -> modify -> save workflow for all four
// Altium file types. This is the most common real-world usage pattern:
// you load an existing file, make changes, and save it back.
//
// MODIFICATION PATTERNS
// ─────────────────────
// Libraries (PcbLib, SchLib):
//   - Add new components:     library.Add(component)
//   - Remove by name:         library.Remove("COMP_A")
//   - Modify component data:  change properties on loaded components
//
// Documents (SchDoc, PcbDoc):
//   - Add primitives:         document.AddPrimitive(wire)
//   - Add tracks/vias/etc:    document.AddTrack(track)
//   - Access existing items via typed collections (Wires, Tracks, etc.)
//
// Components (in any file type):
//   - Add pins:               component.AddPin(pin)
//   - Add graphical elements: component.AddLabel(label), AddRectangle, etc.
//   - Modify existing primitives by changing their properties directly
//
// SAVING
// ──────
// After modification, save using either:
//   - High-level:  await model.SaveAsync("path")
//   - Low-level:   new XxxWriter().Write(model, stream)
//
// VERIFICATION
// ────────────
// Each section below creates a file, loads it, modifies it, saves it,
// then reloads to verify the changes persisted through the round-trip.
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

var tempDir = Path.Combine(Path.GetTempPath(), "AltiumModifyExample");
Directory.CreateDirectory(tempDir);

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  1. Modify a PCB Library: remove a component, add a new one             ║
// ║                                                                         ║
// ║  Demonstrates: library.Remove("name"), library.Add(component),          ║
// ║  and round-trip verification via SaveAsync + reload.                     ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("=== Modifying PcbLib ===");

// Step 1: Create and save an initial library with 2 components
var pcbLib = new PcbLibrary();
pcbLib.Add(PcbComponent.Create("COMP_A").WithDescription("Component A")
    .AddPad(p => p.At(Coord.FromMm(0), Coord.FromMm(0))
        .Size(Coord.FromMm(1.27), Coord.FromMm(1.27)).WithDesignator("1").Layer(74))
    .Build());
pcbLib.Add(PcbComponent.Create("COMP_B").WithDescription("Component B")
    .AddPad(p => p.At(Coord.FromMm(0), Coord.FromMm(0))
        .Size(Coord.FromMm(1.27), Coord.FromMm(1.27)).WithDesignator("1").Layer(74))
    .Build());

var pcbLibPath = Path.Combine(tempDir, "Modified.PcbLib");
using (var fs = File.Create(pcbLibPath))
    new PcbLibWriter().Write(pcbLib, fs);
Console.WriteLine($"  Initial: {pcbLib.Count} components ({string.Join(", ", pcbLib.Components.Select(c => c.Name))})");

// Step 2: Load the saved file back. Note the cast to PcbLibrary -
// OpenPcbLibAsync returns IPcbLibrary, but we need the concrete type
// for SaveAsync and Remove.
var loadedPcbLib = (PcbLibrary)await AltiumLibrary.OpenPcbLibAsync(pcbLibPath);

// Step 3: Remove a component by name
loadedPcbLib.Remove("COMP_A");
Console.WriteLine($"  After removing COMP_A: {loadedPcbLib.Count} component(s)");

// Step 4: Add a new component to the loaded library
loadedPcbLib.Add(PcbComponent.Create("COMP_C")
    .WithDescription("Component C - added after load")
    .AddPad(p => p.At(Coord.FromMm(0), Coord.FromMm(0))
        .Size(Coord.FromMm(1.5), Coord.FromMm(1.5)).WithDesignator("1").Layer(74))
    .AddPad(p => p.At(Coord.FromMm(2.54), Coord.FromMm(0))
        .Size(Coord.FromMm(1.5), Coord.FromMm(1.5)).WithDesignator("2").Layer(74))
    .Build());
Console.WriteLine($"  After adding COMP_C: {loadedPcbLib.Count} component(s)");

// Step 5: Save and verify the round-trip
await loadedPcbLib.SaveAsync(pcbLibPath);

var verifyPcbLib = await AltiumLibrary.OpenPcbLibAsync(pcbLibPath);
Console.WriteLine($"  Verified after reload: {verifyPcbLib.Count} components ({string.Join(", ", verifyPcbLib.Components.Select(c => c.Name))})");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  2. Modify a Schematic Library: add a label to an existing component    ║
// ║                                                                         ║
// ║  Demonstrates: loading a SchLib, accessing a component by index,        ║
// ║  adding a new primitive (SchLabel) to it, and saving with the writer.   ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Modifying SchLib ===");

// Create a starting SchLib with a resistor symbol
var schLib = new SchLibrary();
var resistor = new SchComponent
{
    Name = "RES",
    Description = "Resistor",
    PartCount = 1
};
resistor.AddRectangle(new SchRectangle
{
    Corner1 = new CoordPoint(Coord.FromMm(-1.0), Coord.FromMm(-2.0)),
    Corner2 = new CoordPoint(Coord.FromMm(1.0), Coord.FromMm(2.0)),
    Color = 128
});
resistor.AddPin(SchPin.Create("1").WithName("A")
    .At(Coord.FromMm(0), Coord.FromMm(4.57))
    .Length(Coord.FromMm(2.54)).Orient(PinOrientation.Down)
    .Electrical(PinElectricalType.Passive).Build());
resistor.AddPin(SchPin.Create("2").WithName("B")
    .At(Coord.FromMm(0), Coord.FromMm(-4.57))
    .Length(Coord.FromMm(2.54)).Orient(PinOrientation.Up)
    .Electrical(PinElectricalType.Passive).Build());
schLib.Add(resistor);

var schLibPath = Path.Combine(tempDir, "Modified.SchLib");
using (var fs = File.Create(schLibPath))
    new SchLibWriter().Write(schLib, fs);
Console.WriteLine($"  Initial: {schLib.Components.First().Pins.Count} pins");

// Load, find the component, and add a text label to it.
// Components.First() returns an IComponent; cast to SchComponent
// for access to AddLabel, AddRectangle, AddPolyline, etc.
var loadedSchLib = (SchLibrary)await AltiumLibrary.OpenSchLibAsync(schLibPath);
var loadedComp = (SchComponent)loadedSchLib.Components.First();

// SchLabel is a text annotation on the symbol body.
// FontId references a font defined in the file header (1 = default font).
loadedComp.AddLabel(new SchLabel
{
    Text = "R",
    Location = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
    FontId = 1,
    Color = 128
});

Console.WriteLine($"  After modification:");
Console.WriteLine($"    Pins: {loadedComp.Pins.Count}");
Console.WriteLine($"    Labels: {loadedComp.Labels.Count}");

// Save with the low-level writer and verify
using (var fs = File.Create(schLibPath))
    new SchLibWriter().Write(loadedSchLib, fs);

var verifySchLib = (SchLibrary)await AltiumLibrary.OpenSchLibAsync(schLibPath);
var verifiedComp = (SchComponent)verifySchLib.Components.First();
Console.WriteLine($"  Verified after reload:");
Console.WriteLine($"    Labels: {verifiedComp.Labels.Count}");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  3. Modify a Schematic Document: add wires, net labels, and ports       ║
// ║                                                                         ║
// ║  Demonstrates: loading a SchDoc, adding connectivity primitives         ║
// ║  using AddPrimitive(), and multi-segment wires with .AddPoint().        ║
// ║                                                                         ║
// ║  AddPrimitive() is the general method for adding any schematic          ║
// ║  primitive (wires, net labels, junctions, ports, power objects, etc.)   ║
// ║  to a SchDoc. Use AddComponent() specifically for SchComponent.         ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Modifying SchDoc ===");

// Create an initial document with just one component
var schDoc = new SchDocument();
schDoc.AddComponent(new SchComponent
{
    Name = "U1",
    PartCount = 1,
    Location = new CoordPoint(Coord.FromMm(50.8), Coord.FromMm(50.8))
});

var schDocPath = Path.Combine(tempDir, "Modified.SchDoc");
using (var fs = File.Create(schDocPath))
    new SchDocWriter().Write(schDoc, fs);
Console.WriteLine($"  Initial: {schDoc.Components.Count} component, {schDoc.Wires.Count} wires");

// Load and add connectivity elements
var loadedSchDoc = (SchDocument)await AltiumLibrary.OpenSchDocAsync(schDocPath);

// Multi-segment wire: .From() -> .To() creates the first segment,
// then .AddPoint() adds additional vertices for bends.
// This wire goes right then up (L-shaped).
var newWire = SchWire.Create()
    .From(Coord.FromMm(50.8), Coord.FromMm(55.88))     // Start point
    .To(Coord.FromMm(63.5), Coord.FromMm(55.88))        // First bend (horizontal)
    .AddPoint(Coord.FromMm(63.5), Coord.FromMm(63.5))   // Second segment (vertical)
    .Color(128)
    .Build();
loadedSchDoc.AddPrimitive(newWire);

// Net labels name a wire segment. Place at the endpoint of a wire.
loadedSchDoc.AddPrimitive(new SchNetLabel
{
    Text = "DATA_BUS",
    Location = new CoordPoint(Coord.FromMm(63.5), Coord.FromMm(63.5)),
    Color = 128
});

// Ports are connection points for hierarchical designs (sheet-to-sheet).
// IoType: 0=Unspecified, 1=Output, 2=Input, 3=Bidirectional
// Style controls the graphical shape of the port arrow.
loadedSchDoc.AddPrimitive(new SchPort
{
    Name = "CLK_IN",
    Location = new CoordPoint(Coord.FromMm(38.1), Coord.FromMm(55.88)),
    IoType = 1,                          // Output
    Style = 3,                           // Arrow style
    Width = Coord.FromMm(5.08),
    Height = Coord.FromMm(0.76),
    Color = 128
});

Console.WriteLine($"  After modification: {loadedSchDoc.Wires.Count} wires, " +
                  $"{loadedSchDoc.NetLabels.Count} net labels, {loadedSchDoc.Ports.Count} ports");

// Save and verify
using (var fs = File.Create(schDocPath))
    new SchDocWriter().Write(loadedSchDoc, fs);

var verifySchDoc = (SchDocument)await AltiumLibrary.OpenSchDocAsync(schDocPath);
Console.WriteLine($"  Verified: {verifySchDoc.Wires.Count} wires, " +
                  $"{verifySchDoc.NetLabels.Count} net labels, {verifySchDoc.Ports.Count} ports");

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  4. Modify a PCB Document: add tracks, vias, and design rules           ║
// ║                                                                         ║
// ║  Demonstrates: adding copper primitives to a loaded PcbDoc,             ║
// ║  creating design rules with parameter dictionaries.                     ║
// ║                                                                         ║
// ║  Design rules use a Parameters dictionary that mirrors Altium's         ║
// ║  internal key-value storage. The keys must match Altium's expected      ║
// ║  parameter names exactly (case-insensitive).                            ║
// ╚═══════════════════════════════════════════════════════════════════════════╝

Console.WriteLine("\n=== Modifying PcbDoc ===");

// Create an initial board with one track and two nets
var pcbDoc = new PcbDocument();
pcbDoc.AddNet(new PcbNet { Name = "VCC" });
pcbDoc.AddNet(new PcbNet { Name = "GND" });
pcbDoc.AddTrack(new PcbTrack
{
    Start = new CoordPoint(Coord.FromMm(0), Coord.FromMm(0)),
    End = new CoordPoint(Coord.FromMm(12.7), Coord.FromMm(0)),
    Width = Coord.FromMm(0.254),
    Layer = 1
});

var pcbDocPath = Path.Combine(tempDir, "Modified.PcbDoc");
using (var fs = File.Create(pcbDocPath))
    new PcbDocWriter().Write(pcbDoc, fs);
Console.WriteLine($"  Initial: {pcbDoc.Tracks.Count} tracks, {pcbDoc.Nets.Count} nets");

// Load and add more routing
var loadedPcbDoc = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(pcbDocPath);

// Add a vertical track extending from the end of the first track
loadedPcbDoc.AddTrack(new PcbTrack
{
    Start = new CoordPoint(Coord.FromMm(12.7), Coord.FromMm(0)),
    End = new CoordPoint(Coord.FromMm(12.7), Coord.FromMm(7.62)),
    Width = Coord.FromMm(0.254),
    Layer = 1
});

// Add a horizontal track continuing from the bend
loadedPcbDoc.AddTrack(new PcbTrack
{
    Start = new CoordPoint(Coord.FromMm(12.7), Coord.FromMm(7.62)),
    End = new CoordPoint(Coord.FromMm(25.4), Coord.FromMm(7.62)),
    Width = Coord.FromMm(0.254),
    Layer = 1
});

// Add a via at the corner junction (layer transition point)
loadedPcbDoc.AddVia(new PcbVia
{
    Location = new CoordPoint(Coord.FromMm(12.7), Coord.FromMm(0)),
    Diameter = Coord.FromMm(1.0),
    HoleSize = Coord.FromMm(0.5)
});

// Design rules control DRC (Design Rule Check) in Altium.
// The Parameters dictionary must contain all the fields Altium expects.
// Scope expressions filter which objects the rule applies to
// ("All" = applies to everything, or use queries like "InNet('VCC')").
loadedPcbDoc.AddRule(new PcbRule
{
    Name = "Clearance_1",
    RuleKind = "Clearance",              // Rule category
    Comment = "Default clearance rule",
    Enabled = true,
    Priority = 1,                        // Lower = higher priority
    UniqueId = "CLR001",
    Scope1Expression = "All",            // First object scope
    Scope2Expression = "All",            // Second object scope
    Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["NAME"] = "Clearance_1",
        ["RULEKIND"] = "Clearance",
        ["ENABLED"] = "TRUE",
        ["PRIORITY"] = "1",
        ["UNIQUEID"] = "CLR001",
        ["SCOPE1EXPRESSION"] = "All",
        ["SCOPE2EXPRESSION"] = "All",
        ["GAP"] = "0.254mm"             // Minimum clearance between objects
    }
});

Console.WriteLine($"  After modification: {loadedPcbDoc.Tracks.Count} tracks, " +
                  $"{loadedPcbDoc.Vias.Count} vias, {loadedPcbDoc.Rules.Count} rules");

// Save and verify
using (var fs = File.Create(pcbDocPath))
    new PcbDocWriter().Write(loadedPcbDoc, fs);

var verifyPcbDoc = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(pcbDocPath);
Console.WriteLine($"  Verified: {verifyPcbDoc.Tracks.Count} tracks, " +
                  $"{verifyPcbDoc.Vias.Count} vias, {verifyPcbDoc.Rules.Count} rules");

// Clean up
Directory.Delete(tempDir, recursive: true);
Console.WriteLine("\nAll modifications verified successfully!");
