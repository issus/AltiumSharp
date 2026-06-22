// ============================================================================
// Example: Altium Board 3D Viewer — an ASP.NET Blazor Server app
// ============================================================================
//
// A pure-.NET interactive 3D viewer for Altium PCB documents. The server renders
// each bundled board to a glTF model with OriginalCircuit.Altium.Rendering.Gltf
// and streams it to a small three.js scene in the browser (loaded from a CDN —
// there is NO Node/npm/Python in the build or at runtime; the app is 100% .NET).
//
// The Blazor component owns the UI: a model picker and a per-feature layer-toggle
// panel (Substrate, each Copper layer, SolderMask, Silkscreen, Drills, Components),
// driving the WebGL scene through JS interop.
//
//   dotnet run --project examples/Board3DViewer
//   → open the printed URL (e.g. http://localhost:5000) and pick a board.
// ============================================================================

using Board3DViewer;
using Board3DViewer.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<BoardLibrary>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

// Renders the requested bundled board to a binary glTF on demand (cached in BoardLibrary).
// A query parameter avoids any route-encoding trouble with the spaces in board names.
app.MapGet("/board.glb", async (string name, BoardLibrary library) =>
{
    var bytes = await library.RenderAsync(name);
    return bytes is null
        ? Results.NotFound($"No bundled board named '{name}'.")
        : Results.File(bytes, "model/gltf-binary");
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
