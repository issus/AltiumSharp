using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Project;
using OriginalCircuit.Altium.Models.Sch;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>One instantiated sheet in the hierarchy walk (carries its solved graph).</summary>
internal sealed class SheetInstanceInternal
{
    public SheetInstanceInternal(
        int id, SchDocument doc, string fileName, string? designator, string path, int? parentId,
        IReadOnlyList<string> symbolUidPath, string? channelName, int? channelIndex, bool isRepeated)
    {
        Id = id;
        Doc = doc;
        FileName = fileName;
        Designator = designator;
        Path = path;
        ParentId = parentId;
        SymbolUidPath = symbolUidPath;
        ChannelName = channelName;
        ChannelIndex = channelIndex;
        IsRepeated = isRepeated;
    }

    public int Id { get; }
    public SchDocument Doc { get; }
    public string FileName { get; }
    public string? Designator { get; }
    public string Path { get; }
    public int? ParentId { get; }

    /// <summary>The chain of ancestor sheet-symbol UniqueIds from the root to this instance — the
    /// channel discriminator. Matches a PCB component's <c>SourceUniqueId</c> prefix.</summary>
    public IReadOnlyList<string> SymbolUidPath { get; }

    /// <summary>The channel name (Repeat channel name, or the repeated sheet symbol's designation).</summary>
    public string? ChannelName { get; }

    /// <summary>The 1-based channel index within a repeated group, or <c>null</c> when not a channel.</summary>
    public int? ChannelIndex { get; }

    /// <summary>Whether this instance is one of several channels of the same sheet under its parent.</summary>
    public bool IsRepeated { get; }

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
            WalkNode(rootDoc, rootName, designator: null, parent: null, path: StripExt(rootName),
                symbolUidPath: Array.Empty<string>(), channelName: null, channelIndex: null,
                isRepeated: false, ancestors);
        }
    }

    private void WalkNode(SchDocument doc, string fileName, string? designator,
        SheetInstanceInternal? parent, string path, IReadOnlyList<string> symbolUidPath,
        string? channelName, int? channelIndex, bool isRepeated, HashSet<string> ancestors)
    {
        var inst = new SheetInstanceInternal(Instances.Count, doc, fileName, designator, path, parent?.Id,
            symbolUidPath, channelName, channelIndex, isRepeated);
        Instances.Add(inst);

        // Count how many instances each child file produces under this sheet (duplicate symbols and
        // Repeat() both contribute) so each channel can be flagged as repeated.
        var childInstanceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in doc.SheetSymbols)
        {
            var cn = NormalizeFileName(s.FileName);
            if (cn is not null)
                childInstanceCounts[cn] = childInstanceCounts.GetValueOrDefault(cn) + s.Repeat.InstanceCount;
        }

        var channelOrdinal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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

            if (ancestors.Contains(childName))
            {
                _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Info,
                    $"Skipping re-entrant sub-sheet '{childName}' under '{path}'."));
                continue;
            }

            var repeat = sym.Repeat;
            var repeatedChild = childInstanceCounts.GetValueOrDefault(childName) > 1;
            var symbolName = string.IsNullOrEmpty(sym.SheetName) ? StripExt(childName) : sym.SheetName;
            var symUid = sym.UniqueId ?? symbolName;

            // One channel per Repeat instance; a non-repeated symbol is a single instance. Duplicate
            // symbols of the same child are separate iterations of this loop (already distinct).
            for (var k = repeat.FirstInstance; k <= repeat.LastInstance; k++)
            {
                var ordinal = channelOrdinal.GetValueOrDefault(childName) + 1;
                channelOrdinal[childName] = ordinal;

                // The channel name: a Repeat names the channel; duplicate symbols share the symbol name.
                var chanName = repeat.IsRepeated ? repeat.ChannelName : symbolName;
                var chanIndex = repeatedChild ? (repeat.IsRepeated ? k : ordinal) : (int?)null;

                // The UID path entry. Duplicate symbols carry distinct UniqueIds (matches the PCB
                // SourceUniqueId chain). Repeat channels share one UniqueId, so disambiguate by index.
                var uidEntry = repeat.IsRepeated && repeat.InstanceCount > 1 ? $"{symUid}~{k}" : symUid;
                var childUidPath = Append(symbolUidPath, uidEntry);

                var indexSuffix = repeatedChild ? ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                var childPath = $"{path}/{symbolName}{indexSuffix}";

                var childId = Instances.Count; // the id the recursive WalkNode assigns first
                ancestors.Add(childName);
                WalkNode(childDoc, childName, symbolName, inst, childPath, childUidPath,
                    chanName, chanIndex, repeatedChild, ancestors);
                ancestors.Remove(childName);

                Boundaries.Add(new Boundary(inst, sym, Instances[childId]));
            }
        }
    }

    private static IReadOnlyList<string> Append(IReadOnlyList<string> path, string entry)
    {
        var list = new List<string>(path.Count + 1);
        list.AddRange(path);
        list.Add(entry);
        return list;
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
