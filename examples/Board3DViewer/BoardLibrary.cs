using System.Collections.Concurrent;
using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Rendering.Gltf;

namespace Board3DViewer;

/// <summary>
/// Finds the sample boards bundled in the repo's <c>TestData</c> directory and renders each one to a
/// binary glTF (<c>.glb</c>) on demand, caching the bytes so the (relatively slow) STEP tessellation
/// only happens once per board. Pure .NET — the rendering is <see cref="GltfRenderer"/>; the browser
/// just downloads the resulting <c>.glb</c>.
/// </summary>
public sealed class BoardLibrary
{
    private readonly Dictionary<string, string> _paths; // display name -> .PcbDoc path
    private readonly ConcurrentDictionary<string, Lazy<Task<byte[]?>>> _rendered = new();
    private readonly GltfRenderer _renderer = new();

    public BoardLibrary() => _paths = LocateBoards();

    /// <summary>The bundled board names, alphabetical, for the model picker.</summary>
    public IReadOnlyList<string> Names => _paths.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Renders <paramref name="name"/> to a <c>.glb</c> byte array (cached), or null if unknown.</summary>
    public Task<byte[]?> RenderAsync(string name) =>
        _rendered.GetOrAdd(name, n => new Lazy<Task<byte[]?>>(() => RenderCoreAsync(n))).Value;

    private async Task<byte[]?> RenderCoreAsync(string name)
    {
        if (!_paths.TryGetValue(name, out var path)) return null;
        var document = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(path);
        using var ms = new MemoryStream();
        await _renderer.RenderAsync(document, ms); // GLB (binary glTF) by default for a stream
        return ms.ToArray();
    }

    // Walks up from the app's base directory (and the CWD) to find the repo's TestData folder.
    private static Dictionary<string, string> LocateBoards()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var testData = Path.Combine(dir.FullName, "TestData");
                if (!Directory.Exists(testData)) continue;
                return Directory.EnumerateFiles(testData, "*.PcbDoc", SearchOption.TopDirectoryOnly)
                    .ToDictionary(f => Path.GetFileNameWithoutExtension(f), f => f, StringComparer.OrdinalIgnoreCase);
            }
        return [];
    }
}
