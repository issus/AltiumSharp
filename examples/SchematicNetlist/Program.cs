// ============================================================================
// Example: Reconstructing a schematic netlist from primitive geometry
// ============================================================================
//
// AltiumSharp models a schematic as graphical primitives (wires, pins, net
// labels, power objects, ports, sheet symbols, harnesses) — there is no stored
// netlist. OriginalCircuit.Altium.Connectivity reconstructs one: it joins pins
// into nets from the geometry, merges across the sheet hierarchy, and binds
// design directives to nets.
//
//   SchematicNetlistBuilder.Build(schDoc)            — one sheet
//   ProjectNetlistBuilder.BuildAsync(project)        — whole project hierarchy
//
// RUNNING
//   dotnet run --project examples/SchematicNetlist                  (bundled SchDoc)
//   dotnet run --project examples/SchematicNetlist -- "C:\My.SchDoc"
//   dotnet run --project examples/SchematicNetlist -- "C:\My.PrjPcb"
// ============================================================================

using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Connectivity;
using OriginalCircuit.Altium.Models.Sch;

var input = ResolveInput(args);
if (input is null)
{
    Console.WriteLine("No .SchDoc/.PrjPcb supplied and no bundled TestData schematic was found.");
    Console.WriteLine("Usage: dotnet run --project examples/SchematicNetlist -- <file.SchDoc|file.PrjPcb>");
    return;
}

IReadOnlyList<SchematicNet> nets;
IReadOnlyList<NetPin> unconnected;
IReadOnlyList<OriginalCircuit.Altium.Diagnostics.AltiumDiagnostic> diagnostics;

if (input.EndsWith(".PrjPcb", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine($"Reading project: {Path.GetFileName(input)}\n");
    var project = await AltiumLibrary.OpenProjectAsync(input);
    var netlist = await ProjectNetlistBuilder.BuildAsync(project);
    nets = netlist.Nets;
    unconnected = netlist.UnconnectedPins;
    diagnostics = netlist.Diagnostics;
    Console.WriteLine($"Net-identifier scope: {netlist.Scope}   Sheet instances: {netlist.Sheets.Count}");
}
else
{
    Console.WriteLine($"Reading schematic: {Path.GetFileName(input)}\n");
    await using var idoc = await AltiumLibrary.OpenSchDocAsync(input);
    var netlist = SchematicNetlistBuilder.Build((SchDocument)idoc);
    nets = netlist.Nets;
    unconnected = netlist.UnconnectedPins;
    diagnostics = netlist.Diagnostics;
}

// ── Summary ───────────────────────────────────────────────────────────────────
var multiPin = nets.Count(n => n.Pins.Count >= 2);
var withIntent = nets.Count(n => n.Intents.Count > 0);
Console.WriteLine($"{nets.Count} net(s) — {multiPin} with 2+ pins, {nets.Count(n => n.IsNamedExplicitly)} explicitly named, "
    + $"{withIntent} carrying directives.  Unconnected pins: {unconnected.Count}.");

// ── Largest nets ──────────────────────────────────────────────────────────────
Console.WriteLine($"\n{"Net",-26} {"Scope",-14} {"Pins",5}");
Console.WriteLine(new string('-', 60));
foreach (var net in nets.OrderByDescending(n => n.Pins.Count).ThenBy(n => n.Name).Take(20))
{
    Console.WriteLine($"{Trunc(net.Name, 26),-26} {net.Scope,-14} {net.Pins.Count,5}");
    foreach (var intent in net.Intents.Take(3))
        Console.WriteLine($"      ↳ {intent.Kind}: {intent.RawName}={Trunc(intent.RawValue, 28)}");
}

// ── A worked pin lookup ───────────────────────────────────────────────────────
var sample = nets.FirstOrDefault(n => n.Pins.Count >= 2);
if (sample is not null)
{
    Console.WriteLine($"\nNet '{sample.Name}' connects:");
    foreach (var pin in sample.Pins.Take(12))
        Console.WriteLine($"    {pin.Key,-12} {pin.ElectricalType,-12} ({pin.PinName})");
}

if (diagnostics.Count > 0)
{
    Console.WriteLine($"\n{diagnostics.Count} diagnostic(s):");
    foreach (var d in diagnostics.Take(8))
        Console.WriteLine($"    [{d.Severity}] {d.Message}");
}

// ── Helpers ───────────────────────────────────────────────────────────────────
static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

static string? ResolveInput(string[] args)
{
    if (args.Length > 0)
    {
        if (File.Exists(args[0])) return args[0];
        Console.Error.WriteLine($"File not found: {args[0]}");
        return null;
    }
    var testData = FindRepoTestDataDir();
    if (testData is null) return null;
    // The SPI Isolator schematic is a self-contained single-sheet board.
    var preferred = Path.Combine(testData, "SPI Isolator.SchDoc");
    if (File.Exists(preferred)) return preferred;
    return Directory.EnumerateFiles(testData, "*.SchDoc", SearchOption.TopDirectoryOnly).FirstOrDefault();
}

static string? FindRepoTestDataDir()
{
    foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "TestData");
            if (Directory.Exists(candidate)) return candidate;
        }
    return null;
}
