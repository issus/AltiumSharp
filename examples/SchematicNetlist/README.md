# SchematicNetlist

Reconstruct a **netlist** from a schematic's primitive geometry.

A `.SchDoc` stores only graphical primitives — wires, pins, net labels, power
objects, ports, sheet symbols, harnesses. There is no stored netlist. The
`OriginalCircuit.Altium.Connectivity` namespace reconstructs one by joining pins
into nets from the geometry, merging across the sheet hierarchy, and binding
design directives to the nets they apply to.

## Run

```bash
# Bundled single-sheet board (SPI Isolator)
dotnet run --project examples/SchematicNetlist

# Your own single sheet
dotnet run --project examples/SchematicNetlist -- "C:\Path\My.SchDoc"

# A whole hierarchical project
dotnet run --project examples/SchematicNetlist -- "C:\Path\My.PrjPcb"
```

## What it shows

- The reconstructed nets with their **scope** (`LocalSheet`, `Power`,
  `CrossSheetPort`, `Harness`, `Auto`, …) and pin counts.
- The directives (`NetIntent`) bound to each net — net class, impedance,
  differential pair, PCB rule, etc.
- A worked pin lookup for one net and any unconnected pins.

## API

```csharp
// One sheet
await using var idoc = await AltiumLibrary.OpenSchDocAsync(path);
SchematicNetlist netlist = SchematicNetlistBuilder.Build((SchDocument)idoc);

foreach (SchematicNet net in netlist.Nets)
    Console.WriteLine($"{net.Name}: {string.Join(", ", net.Pins.Select(p => p.Key))}");

SchematicNet? n = netlist.NetForPin("U1", "3");   // which net is U1 pin 3 on?

// Whole project (hierarchy + scope + global power + harnesses)
var project = await AltiumLibrary.OpenProjectAsync(prjPath);
ProjectNetlist pnl = await ProjectNetlistBuilder.BuildAsync(project);
```

## How it works

The solver builds connection points from every primitive, then union-finds them
under Altium's implicit rules:

- **Coincidence** — pins, wire endpoints, power objects and ports that share a
  point connect.
- **T-junctions** — a wire endpoint or pin tip on another wire's interior
  connects; a 4-way crossover connects **only** if a manual junction is present.
- **Named identifiers** — same-name net labels, power objects and ports unify
  (net labels scoped per the project's net-identifier setting; power is global).
- **Hierarchy** — ports merge with matching sheet entries across boundaries.
- **Buses** — ranged labels (`D[0..7]`) expand to members; a bus is a visual
  bundle and never shorts its members.
- **Harnesses** — bundle members reconnect by their qualified `bundle.member`
  name across the project.
- **Multi-channel** — a sheet reused by several sheet symbols (or a `Repeat()`
  directive) becomes one channel instance each, with its own net scope. A
  sheet-local net is distinct per channel; power inside a channel is
  channel-private and escapes only through the port/sheet-entry boundary (so
  `GND` stays global). Each `ProjectSheetInstance` exposes its channel identity
  (`SymbolUidPath`, matching a PCB component's `SourceUniqueId`), and
  `ProjectNetlist.NetForPin(sheetInstanceId, …)` selects a specific channel's net.

A pin's electrical tip is computed as `Location + Length` along its orientation;
for a placed multi-part component only the displayed part's pins are used.
