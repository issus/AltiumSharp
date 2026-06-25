// ============================================================================
// Example: Inspecting a whole project (.PrjPcb)
// ============================================================================
//
// A project file ties a board design together: the schematic sheets, PCB layout(s),
// libraries, BOM, output jobs, plus configurations, assembly variants and the compiled
// sheet hierarchy. This example loads a .PrjPcb and prints a "what's in this project?"
// report, then opens the actual source documents through the project to show that you
// can drive whole-project tooling (rendering, BOM extraction, ERC, …) from one entry point.
//
// WHERE THE DATA LIVES
// ────────────────────
// AltiumLibrary.OpenProjectAsync(path) returns an AltiumProject. Its Documents,
// Configurations, Variants, Parameters and OutputGroups are typed views over the file's
// sections. The sibling .PrjPcbStructure file (loaded automatically when present) gives
// the logical sheet hierarchy via project.Structure.BuildTree().
//
// project.OpenSchematicAsync(doc) / OpenPcbAsync(doc) resolve a document's path relative
// to the project folder and open it with the existing readers.
//
// RUNNING
// ───────
//   dotnet run --project examples/InspectProject -- "C:\path\to\MyBoard.PrjPcb"
//
// ============================================================================

using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Project;

if (args.Length == 0 || !File.Exists(args[0]))
{
    Console.WriteLine("Usage: dotnet run --project examples/InspectProject -- <project.PrjPcb>");
    Console.WriteLine();
    Console.WriteLine("Point it at any Altium .PrjPcb file to see its documents, variants,");
    Console.WriteLine("configurations and the schematic sheet hierarchy.");
    return;
}

var project = await AltiumLibrary.OpenProjectAsync(args[0]);

Console.WriteLine($"Project: {project.Name}");
Console.WriteLine($"  Version            : {project.Design.Version}");
Console.WriteLine($"  Hierarchy mode     : {project.Design.HierarchyMode}");
Console.WriteLine($"  Default config     : {project.Design.DefaultConfiguration}");
if (project.Design.ManagedProjectGuid is { } guid)
    Console.WriteLine($"  Managed project    : {guid} (vault: {project.Design.ReleaseVaultName})");

// ── Documents, grouped by kind ──────────────────────────────────────────────
Console.WriteLine($"\nDocuments ({project.Documents.Count}):");
foreach (var group in project.Documents.GroupBy(d => d.Kind).OrderBy(g => g.Key.ToString()))
{
    Console.WriteLine($"  {group.Key} ({group.Count()}):");
    foreach (var doc in group)
        Console.WriteLine($"     {doc.DocumentPath}");
}

if (project.GeneratedDocuments.Count > 0)
{
    Console.WriteLine($"\nGenerated documents ({project.GeneratedDocuments.Count}):");
    foreach (var doc in project.GeneratedDocuments)
        Console.WriteLine($"     {doc.DocumentPath}");
}

// ── Configurations ──────────────────────────────────────────────────────────
if (project.Configurations.Count > 0)
{
    Console.WriteLine($"\nConfigurations ({project.Configurations.Count}):");
    foreach (var config in project.Configurations)
        Console.WriteLine($"     {config.Name}  [type={config.ConfigurationType}, variant={config.Variant}]");
}

// ── Assembly variants ───────────────────────────────────────────────────────
if (project.Variants.Count > 0)
{
    Console.WriteLine($"\nVariants ({project.Variants.Count}):");
    foreach (var variant in project.Variants)
    {
        Console.WriteLine($"  {variant.Description}  (fabrication {(variant.AllowFabrication ? "allowed" : "blocked")})");
        var notFitted = variant.Variations.Where(v => v.Kind == VariationKind.NotFitted).Select(v => v.Designator).ToList();
        var alternate = variant.Variations.Where(v => v.Kind == VariationKind.Alternate).Select(v => v.Designator).ToList();
        if (notFitted.Count > 0)
            Console.WriteLine($"     not fitted : {string.Join(", ", notFitted)}");
        if (alternate.Count > 0)
            Console.WriteLine($"     alternate  : {string.Join(", ", alternate)}");
        if (variant.ParameterVariations.Count > 0)
            Console.WriteLine($"     parameter overrides: {variant.ParameterVariations.Count}");
    }
}

// ── Project parameters ──────────────────────────────────────────────────────
if (project.Parameters.Count > 0)
{
    Console.WriteLine($"\nParameters ({project.Parameters.Count}):");
    foreach (var p in project.Parameters)
        Console.WriteLine($"     {p.Name} = {p.Value}");
}

// ── Output jobs ─────────────────────────────────────────────────────────────
if (project.OutputGroups.Count > 0)
{
    var totalOutputs = project.OutputGroups.Sum(g => g.Outputs.Count);
    Console.WriteLine($"\nOutput groups ({project.OutputGroups.Count}, {totalOutputs} outputs):");
    foreach (var group in project.OutputGroups)
        Console.WriteLine($"     {group.Name} ({group.Outputs.Count})");
}

// ── Logical sheet hierarchy (from the .PrjPcbStructure file) ─────────────────
if (project.Structure?.BuildTree() is { } root)
{
    Console.WriteLine("\nSheet hierarchy:");
    PrintTree(root, 0);
}
else
{
    Console.WriteLine("\nSheet hierarchy: (no .PrjPcbStructure file — compile the project in Altium to generate it)");
}

// ── Open the real documents through the project ─────────────────────────────
// This is the payoff: from one project handle you can reach every source document and
// drive existing readers/renderers over the whole design.
Console.WriteLine("\nOpening source documents:");
foreach (var doc in project.SchematicDocuments)
{
    var path = project.ResolveDocumentPath(doc);
    if (path is null || !File.Exists(path)) { Console.WriteLine($"   • {doc.FileName}  (file not found)"); continue; }
    try
    {
        await using var sch = await project.OpenSchematicAsync(doc);
        Console.WriteLine($"   • {doc.FileName,-32} {sch.Components.Count} components");
    }
    catch (Exception ex) { Console.WriteLine($"   • {doc.FileName}  (read error: {ex.Message})"); }
}
foreach (var doc in project.PcbDocuments)
{
    var path = project.ResolveDocumentPath(doc);
    if (path is null || !File.Exists(path)) { Console.WriteLine($"   • {doc.FileName}  (file not found)"); continue; }
    try
    {
        await using var pcb = await project.OpenPcbAsync(doc);
        Console.WriteLine($"   • {doc.FileName,-32} {pcb.Components.Count} components, {pcb.Pads.Count} pads");
    }
    catch (Exception ex) { Console.WriteLine($"   • {doc.FileName}  (read error: {ex.Message})"); }
}

if (project.Diagnostics.Count > 0)
{
    Console.WriteLine("\nDiagnostics:");
    foreach (var d in project.Diagnostics)
        Console.WriteLine($"   [{d.Severity}] {d.Message}");
}

// Recursively prints a sheet and the child sheets its symbols instantiate.
static void PrintTree(ProjectSheetNode node, int depth)
{
    var indent = new string(' ', depth * 3);
    var label = node.Designator is null ? node.FileName : $"{node.Designator} -> {node.FileName}";
    Console.WriteLine($"   {indent}• {label}{(node.IsCycle ? "  [cycle]" : "")}");
    foreach (var child in node.Children)
        PrintTree(child, depth + 1);
}
