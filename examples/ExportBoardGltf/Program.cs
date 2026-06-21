// ============================================================================
// Example: Export a PCB document to a 3D glTF model
// ============================================================================
//
// Loads a whole PCB document (.PcbDoc) and writes a 3D glTF model of it — the
// full layer stack (copper, laminate at true thickness, solder mask, silkscreen,
// drills) plus the placed component 3D bodies. Every board feature and every
// component is its own named node, so a glTF viewer (three.js, Babylon.js,
// Blender, Windows 3D Viewer) can toggle them on and off.
//
//   dotnet run --project examples/ExportBoardGltf                 (bundled sample)
//   dotnet run --project examples/ExportBoardGltf -- MyBoard.PcbDoc
//
// It writes a binary .glb (full board + components) and a bare-board .gltf
// (no components) into a temp folder and prints the paths and scene summary.
// ============================================================================

using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Rendering.Gltf;

var boardPath = args.FirstOrDefault(a => a.EndsWith(".PcbDoc", StringComparison.OrdinalIgnoreCase))
                ?? LocateBundledBoard();

if (boardPath is null)
{
    Console.WriteLine("No .PcbDoc found. Pass one as an argument:");
    Console.WriteLine("  dotnet run --project examples/ExportBoardGltf -- MyBoard.PcbDoc");
    return;
}

Console.WriteLine($"Loading {Path.GetFileName(boardPath)} ...");
var document = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(boardPath);

var outDir = Path.Combine(Path.GetTempPath(), "AltiumExportBoardGltf");
Directory.CreateDirectory(outDir);
var name = Path.GetFileNameWithoutExtension(boardPath);

var renderer = new GltfRenderer();

// Full board with components, as a single self-contained binary .glb.
var glbPath = Path.Combine(outDir, $"{name}.glb");
await renderer.RenderAsync(document, glbPath);
Console.WriteLine($"  GLB   {Path.GetFileName(glbPath)}  ({new FileInfo(glbPath).Length / 1024} KB)");

// Bare board only (no components), as a JSON .gltf with an embedded buffer.
var bareGltf = Path.Combine(outDir, $"{name}_bare.gltf");
await renderer.RenderAsync(document, bareGltf, new GltfRenderSettings { IncludeComponents = false });
Console.WriteLine($"  glTF  {Path.GetFileName(bareGltf)}  ({new FileInfo(bareGltf).Length / 1024} KB, bare board)");

// A copper-finish variant: bottom-up tweaks live on GltfRenderSettings (finish, tolerances,
// per-layer toggles). Here, a bare board with an ENIG gold finish, copper layers only.
var copperOnly = Path.Combine(outDir, $"{name}_copper.glb");
await renderer.RenderAsync(document, copperOnly, new GltfRenderSettings
{
    IncludeComponents = false,
    IncludeSolderMask = false,
    IncludeSilkscreen = false,
    CopperFinish = GltfCopperFinish.Enig,
});
Console.WriteLine($"  GLB   {Path.GetFileName(copperOnly)}  ({new FileInfo(copperOnly).Length / 1024} KB, copper + substrate)");

Console.WriteLine($"\nDone. Files are in: {outDir}");
Console.WriteLine("Open the .glb in any glTF viewer; each board feature and component is a named,");
Console.WriteLine("toggleable node (Substrate, Copper.*, SolderMask.*, Silkscreen.*, Drills, Components).");

// Finds a sample board bundled in the repo's TestData directory.
static string? LocateBundledBoard()
{
    foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            var testData = Path.Combine(dir.FullName, "TestData");
            if (!Directory.Exists(testData)) continue;
            var board = Directory.EnumerateFiles(testData, "*.PcbDoc", SearchOption.TopDirectoryOnly)
                .OrderBy(f => new FileInfo(f).Length) // smallest first — fastest to export
                .FirstOrDefault();
            if (board is not null) return board;
        }
    return null;
}
