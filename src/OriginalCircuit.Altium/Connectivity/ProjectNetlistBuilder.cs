using OriginalCircuit.Altium.Connectivity.Internal;
using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Project;
using OriginalCircuit.Altium.Models.Sch;

namespace OriginalCircuit.Altium.Connectivity;

/// <summary>
/// Reconstructs a project-wide netlist by solving each sheet instance and merging across the hierarchy:
/// port ↔ sheet-entry boundaries, global power nets, and net labels scoped per the project's
/// net-identifier-scope setting.
/// </summary>
public static class ProjectNetlistBuilder
{
    /// <summary>
    /// Builds the merged netlist for an entire project.
    /// </summary>
    /// <param name="project">The loaded Altium project.</param>
    /// <param name="options">Solver options; defaults are used when <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<ProjectNetlist> BuildAsync(
        AltiumProject project, NetlistOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        options ??= NetlistOptions.Default;
        var diagnostics = new List<AltiumDiagnostic>();
        var tolRaw = options.Tolerance.ToRaw();

        // --- Load every schematic document, keyed by file name ---
        var docsByName = new Dictionary<string, SchDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var pd in project.SchematicDocuments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = project.ResolveDocumentPath(pd);
            if (path is null || !File.Exists(path))
            {
                diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning,
                    $"Schematic document not found: {pd.DocumentPath}"));
                continue;
            }
            var doc = (SchDocument)await AltiumLibrary.OpenSchDocAsync(path, cancellationToken).ConfigureAwait(false);
            doc.FileName ??= Path.GetFileName(path);
            docsByName[Path.GetFileName(path)] = doc;
        }

        if (docsByName.Count == 0)
        {
            diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Error, "Project has no readable schematic documents."));
            return new ProjectNetlist(Array.Empty<SchematicNet>(), Array.Empty<NetPin>(),
                Array.Empty<ProjectSheetInstance>(), NetIdentifierScope.Automatic, diagnostics);
        }

        // --- Determine scope ---
        var rawScope = options.ScopeIsExplicit ? options.Scope : NetIdentifierScopeReader.Read(project);
        var hasSheetSymbols = docsByName.Values.Any(d => d.SheetSymbols.Count > 0);
        var scope = NetIdentifierScopeReader.Resolve(rawScope, hasSheetSymbols);

        // --- Walk the hierarchy into sheet instances ---
        var walker = new HierarchyWalker(docsByName, diagnostics);
        walker.Walk(project);

        var instances = walker.Instances;
        if (instances.Count == 0)
        {
            diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Error, "Could not determine a root schematic."));
            return new ProjectNetlist(Array.Empty<SchematicNet>(), Array.Empty<NetPin>(),
                Array.Empty<ProjectSheetInstance>(), scope, diagnostics);
        }

        // Surface multi-channel (repeated) sheets: each channel instance is solved with its own net
        // scope so a sheet-local net is distinct per channel; channel-aware lookups disambiguate them.
        foreach (var grp in instances.GroupBy(i => i.FileName, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Info,
                $"Sheet '{grp.Key}' is instantiated {grp.Count()} times (multi-channel); each channel is a separate net scope."));

        // --- Build all sheet graphs (global element id space) ---
        var elements = new List<Element>();
        foreach (var inst in instances)
        {
            cancellationToken.ThrowIfCancellationRequested();
            inst.Graph = new SheetGraph(inst.Doc, inst.Id, inst.FileName, elements, tolRaw);
        }

        var uf = new UnionFind(elements.Count);
        foreach (var inst in instances)
        {
            cancellationToken.ThrowIfCancellationRequested();
            inst.Graph!.BuildIndexes();
            inst.Graph!.ApplyRules(uf);
        }

        var graphs = instances.Select(i => i.Graph!).ToList();

        // --- Cross-sheet merging ---
        // Power is global by name, except inside repeated (multi-channel) instances where it is
        // channel-private and escapes only through the boundary.
        var repeatedInstanceIds = instances.Where(i => i.IsRepeated).Select(i => i.Id).ToHashSet();
        SchematicNetlistBuilder.UnifyPower(graphs, uf, repeatedInstanceIds);

        var labelReps = new List<(SchNetLabel Label, int Rep, int SheetId)>();
        foreach (var g in graphs)
            labelReps.AddRange(SchematicNetlistBuilder.ComputeLabelReps(g, uf, diagnostics));

        // Map (sheet, net-label name) -> representative element, used to bridge bus members across a
        // ranged bus port / sheet entry.
        var repByNameSheet = new Dictionary<(int, string), int>();
        foreach (var (label, rep, sheetId) in labelReps)
            repByNameSheet[(sheetId, label.Text)] = rep;

        MergeBoundaries(walker.Boundaries, uf, repByNameSheet);              // ports ↔ sheet entries

        if (options.ResolveHarnesses)
            HarnessResolver.Resolve(graphs, uf, tolRaw);                     // harness-bundle members

        var labelsGlobal = scope is NetIdentifierScope.Flat or NetIdentifierScope.Global;
        var portsGlobal = scope is NetIdentifierScope.Flat or NetIdentifierScope.Global;
        SchematicNetlistBuilder.MergeIdentifiers(graphs, labelReps, uf, labelsGlobal, portsGlobal);

        var labelsByRoot = SchematicNetlistBuilder.BindLabelsToRoots(labelReps, uf);

        // --- Assemble ---
        var assembleOptions = new NetlistOptions
        {
            Scope = scope,
            Tolerance = options.Tolerance,
            ExpandBuses = options.ExpandBuses,
            ResolveHarnesses = options.ResolveHarnesses,
            ExtractIntents = options.ExtractIntents,
        };
        var assembler = new NetlistAssembler(elements, uf, graphs, labelsByRoot, assembleOptions, diagnostics);
        var result = assembler.Assemble();

        if (options.ExtractIntents)
            NetIntentExtractor.Extract(graphs, uf, result.RootToNet, assembleOptions);

        var publicSheets = instances
            .Select(i => new ProjectSheetInstance(i.Id, i.FileName, i.Designator, i.Path, i.ParentId,
                i.SymbolUidPath, i.ChannelName, i.ChannelIndex, i.IsRepeated))
            .ToList();

        return new ProjectNetlist(result.Nets, result.Unconnected, publicSheets, scope, diagnostics);
    }

    private static void MergeBoundaries(
        IReadOnlyList<Boundary> boundaries, UnionFind uf, Dictionary<(int, string), int> repByNameSheet)
    {
        foreach (var b in boundaries)
        {
            // Parent's entries for this sheet symbol.
            foreach (var (sym, entry, entryElem) in b.Parent.Graph!.SheetEntries)
            {
                if (!ReferenceEquals(sym, b.Symbol))
                    continue;

                // A ranged entry (e.g. "D[0..7]") carries a bus bundle: bridge each member net (D0..D7)
                // between the parent and child sheets by name.
                if (BusRange.TryExpand(entry.Name, out var members))
                {
                    foreach (var member in members)
                    {
                        if (repByNameSheet.TryGetValue((b.Parent.Id, member), out var pRep) &&
                            repByNameSheet.TryGetValue((b.Child.Id, member), out var cRep))
                            uf.Union(pRep, cRep);
                    }
                }

                foreach (var (port, portElem) in b.Child.Graph!.Ports)
                {
                    if (NameEq(port.Name, entry.Name))
                        uf.Union(entryElem.Id, portElem.Id);
                }
            }
        }
    }

    private static bool NameEq(string? a, string? b) =>
        string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
}
