using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Project;
using OriginalCircuit.Altium.Models.Sch;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>One instantiated sheet in the hierarchy walk (carries its solved graph).</summary>
internal sealed class SheetInstanceInternal
{
    public SheetInstanceInternal(int id, SchDocument doc, string fileName, string? designator, string path, int? parentId)
    {
        Id = id;
        Doc = doc;
        FileName = fileName;
        Designator = designator;
        Path = path;
        ParentId = parentId;
    }

    public int Id { get; }
    public SchDocument Doc { get; }
    public string FileName { get; }
    public string? Designator { get; }
    public string Path { get; }
    public int? ParentId { get; }
    public SheetGraph? Graph { get; set; }
}

/// <summary>A parent sheet symbol linking a parent instance to the child instance it created.</summary>
internal readonly record struct Boundary(SheetInstanceInternal Parent, SchSheetSymbol Symbol, SheetInstanceInternal Child);

/// <summary>
/// Walks a project's schematic hierarchy by following sheet symbols, producing one sheet instance per
/// instantiation (so a document reused by N sheet symbols yields N instances — the multi-channel case)
/// and the parent→child boundaries used to merge ports with sheet entries.
/// </summary>
internal sealed class HierarchyWalker
{
    private readonly Dictionary<string, SchDocument> _docsByName;
    private readonly List<AltiumDiagnostic> _diagnostics;

    public HierarchyWalker(Dictionary<string, SchDocument> docsByName, List<AltiumDiagnostic> diagnostics)
    {
        _docsByName = docsByName;
        _diagnostics = diagnostics;
    }

    public List<SheetInstanceInternal> Instances { get; } = new();
    public List<Boundary> Boundaries { get; } = new();

    public void Walk(AltiumProject project)
    {
        foreach (var (rootName, rootDoc) in DetermineRoots(project))
        {
            var ancestors = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootName };
            WalkNode(rootDoc, rootName, designator: null, parent: null, path: StripExt(rootName), ancestors);
        }
    }

    private void WalkNode(SchDocument doc, string fileName, string? designator,
        SheetInstanceInternal? parent, string path, HashSet<string> ancestors)
    {
        var inst = new SheetInstanceInternal(Instances.Count, doc, fileName, designator, path, parent?.Id);
        Instances.Add(inst);

        foreach (var sym in doc.SheetSymbols)
        {
            var childName = NormalizeFileName(sym.FileName);
            if (childName is null)
                continue;
            if (!_docsByName.TryGetValue(childName, out var childDoc))
            {
                _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning,
                    $"Sheet '{fileName}' references missing sub-sheet '{sym.FileName}'."));
                continue;
            }

            var childDesignator = string.IsNullOrEmpty(sym.SheetName) ? StripExt(childName) : sym.SheetName;
            var childPath = $"{path}/{childDesignator}";

            if (ancestors.Contains(childName))
            {
                _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Info,
                    $"Skipping re-entrant sub-sheet '{childName}' under '{path}'."));
                continue;
            }

            var childId = Instances.Count; // the id WalkNode will assign first
            ancestors.Add(childName);
            WalkNode(childDoc, childName, childDesignator, inst, childPath, ancestors);
            ancestors.Remove(childName);

            Boundaries.Add(new Boundary(inst, sym, Instances[childId]));
        }
    }

    private IEnumerable<(string Name, SchDocument Doc)> DetermineRoots(AltiumProject project)
    {
        // Prefer the compiled top-level document when available.
        var top = NormalizeFileName(project.Structure?.TopLevelDocument);
        if (top is not null && _docsByName.TryGetValue(top, out var topDoc))
        {
            yield return (top, topDoc);
            yield break;
        }

        // Otherwise: any document not referenced as a child by a sheet symbol.
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var doc in _docsByName.Values)
            foreach (var sym in doc.SheetSymbols)
            {
                var child = NormalizeFileName(sym.FileName);
                if (child is not null)
                    referenced.Add(child);
            }

        var roots = _docsByName.Where(kv => !referenced.Contains(kv.Key)).ToList();
        if (roots.Count == 0)
        {
            // Fully cyclic / single sheet: fall back to the first document.
            var first = _docsByName.First();
            yield return (first.Key, first.Value);
            yield break;
        }

        foreach (var (name, doc) in roots)
            yield return (name, doc);
    }

    private static string? NormalizeFileName(string? fileName) =>
        string.IsNullOrEmpty(fileName) ? null : System.IO.Path.GetFileName(fileName.Replace('\\', '/'));

    private static string StripExt(string fileName) => System.IO.Path.GetFileNameWithoutExtension(fileName);
}
