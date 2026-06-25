# Inspecting a whole project

A project file (`.PrjPcb`) is the entry point to a board design: it lists the schematic
sheets, the PCB layout(s), the libraries, the BOM and output jobs, and records the
configurations, assembly variants and compiled sheet hierarchy. This guide loads a
`.PrjPcb` and prints a "what's in this project?" report, then opens the real source
documents through the project — so you can drive whole-design tooling (rendering, BOM,
ERC, …) from one handle instead of juggling individual files.

The complete, compiling source for this guide is [Program.cs](Program.cs).

## Run

```bash
dotnet run --project examples/InspectProject -- "C:\path\to\MyBoard.PrjPcb"
```

A `.PrjPcb` references its documents by path, so point the example at a real project on
disk (the example needs the sibling `.SchDoc`/`.PcbDoc` files to open them).

## Where the data lives

`AltiumLibrary.OpenProjectAsync(path)` returns an `AltiumProject`. When a sibling
`.PrjPcbStructure` file is present (Altium writes it when the project is compiled), it is
loaded automatically into `project.Structure`.

```csharp
var project = await AltiumLibrary.OpenProjectAsync("MyBoard.PrjPcb");

// Design-wide settings ([Design] section).
Console.WriteLine(project.Design.Version);
Console.WriteLine(project.Design.DefaultConfiguration);

// Documents, classified by extension into ProjectDocumentKind.
foreach (var group in project.Documents.GroupBy(d => d.Kind))
    Console.WriteLine($"{group.Key}: {group.Count()}");

// Convenience filters.
foreach (var sch in project.SchematicDocuments) { /* … */ }
foreach (var pcb in project.PcbDocuments)       { /* … */ }
```

The typed collections — `Documents`, `Configurations`, `Variants`, `Parameters`,
`OutputGroups`, `GeneratedDocuments` — are views over the file's `[Section]`s. The raw
sections remain on `project.Sections`, which is what the writer emits, so a load → save
round-trip is byte-for-byte identical.

### Assembly variants

```csharp
foreach (var variant in project.Variants)
{
    Console.WriteLine(variant.Description);
    var notFitted = variant.Variations.Where(v => v.Kind == VariationKind.NotFitted);
    var alternate = variant.Variations.Where(v => v.Kind == VariationKind.Alternate);
    foreach (var pv in variant.ParameterVariations)
        Console.WriteLine($"{pv.Designator}.{pv.ParameterName} = {pv.VariantValue}");
}
```

### Sheet hierarchy

The compiled hierarchy comes from the `.PrjPcbStructure` file. `BuildTree()` turns the
flat sheet-symbol records into a navigable tree (each node is one sheet *instance*, so a
document instantiated several times appears at several nodes; re-entrant references are
pruned and flagged via `IsCycle`).

```csharp
if (project.Structure?.BuildTree() is { } root)
    Print(root); // root.FileName, root.Children, child.Designator, child.SheetNumber
```

### Opening the source documents

```csharp
foreach (var doc in project.SchematicDocuments)
{
    await using var sch = await project.OpenSchematicAsync(doc);
    Console.WriteLine($"{doc.FileName}: {sch.Components.Count} components");
}

foreach (var doc in project.PcbDocuments)
{
    await using var pcb = await project.OpenPcbAsync(doc);
    Console.WriteLine($"{doc.FileName}: {pcb.Pads.Count} pads");
}
```

`OpenSchematicAsync`/`OpenPcbAsync` resolve the document's path relative to the project
folder (`ResolveDocumentPath`) and open it with the existing readers. There are matching
helpers for `.SchLib`/`.PcbLib` documents.

## Creating & editing projects

The model also writes. `AltiumLibrary.CreateProject()` starts an empty project; editing a
typed property writes straight back to the underlying section.

```csharp
var project = AltiumLibrary.CreateProject();
var sheet = project.AddDocument("MyBoard.SchDoc");
project.AddDocument("MyBoard.PcbDoc");
project.Design.DefaultConfiguration = "Sources";
await project.SaveAsync("MyBoard.PrjPcb");
```
