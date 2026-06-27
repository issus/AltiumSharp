using OriginalCircuit.Altium.Connectivity.Internal;
using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Enums;

namespace OriginalCircuit.Altium.Connectivity;

/// <summary>
/// Reconstructs a netlist from the primitive geometry of a single schematic document. This is the
/// per-sheet foundation; cross-sheet merging lives in <see cref="ProjectNetlistBuilder"/>.
/// </summary>
public static class SchematicNetlistBuilder
{
    /// <summary>
    /// Builds the netlist for one schematic document by reconstructing nets from wires, pins, net
    /// labels, power objects, junctions and ports.
    /// </summary>
    /// <param name="document">The schematic document to analyse.</param>
    /// <param name="options">Solver options; defaults are used when <see langword="null"/>.</param>
    /// <returns>The reconstructed <see cref="SchematicNetlist"/>.</returns>
    public static SchematicNetlist Build(SchDocument document, NetlistOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= NetlistOptions.Default;
        var diagnostics = new List<AltiumDiagnostic>();
        var tolRaw = options.Tolerance.ToRaw();

        var elements = new List<Element>();
        var sheet = new SheetGraph(document, sheetId: 0, document.FileName, elements, tolRaw);
        sheet.BuildIndexes();

        var uf = new UnionFind(elements.Count);
        sheet.ApplyRules(uf);

        // Net identifiers (net labels, power objects, ports) unify nets that share a name, even with no
        // geometric contact. On a single sheet every identifier is in scope.
        var labelReps = ComputeLabelReps(sheet, uf, diagnostics);
        MergeNamedIdentifiers(new[] { sheet }, labelReps, uf, mergeLabels: true, mergePorts: true);

        var labelsByRoot = BindLabelsToRoots(labelReps, uf);

        var assembler = new NetlistAssembler(elements, uf, new[] { sheet }, labelsByRoot, options, diagnostics);
        var result = assembler.Assemble();

        if (options.ExtractIntents)
            NetIntentExtractor.Extract(new[] { sheet }, uf, result.RootToNet, options);

        return new SchematicNetlist(result.Nets, result.Unconnected, diagnostics, document.FileName);
    }

    /// <summary>
    /// Finds, for each net label, the conductor element it sits on (its representative id before any
    /// name merging). Labels not on a wire are reported as info diagnostics and skipped.
    /// </summary>
    internal static List<(SchNetLabel Label, int Rep, int SheetId)> ComputeLabelReps(
        SheetGraph sheet, UnionFind uf, List<AltiumDiagnostic> diagnostics)
    {
        var reps = new List<(SchNetLabel, int, int)>();
        foreach (var label in sheet.NetLabels)
        {
            int? rep = null;
            foreach (var elem in sheet.Points.Query(label.Location)) { rep = elem; break; }
            if (rep is null)
                foreach (var elem in sheet.Segments.ElementsAt(label.Location, interiorOnly: false)) { rep = elem; break; }

            if (rep is null)
            {
                diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Info,
                    $"Net label '{label.Text}' at {label.Location} is not on any wire."));
                continue;
            }
            reps.Add((label, rep.Value, sheet.SheetId));
        }
        return reps;
    }

    /// <summary>
    /// Unifies elements that carry the same net identifier within each sheet: power objects (and hidden
    /// power pins), optionally ports, and optionally net labels. Names are compared case-insensitively.
    /// </summary>
    internal static void MergeNamedIdentifiers(
        IReadOnlyList<SheetGraph> sheets,
        IReadOnlyList<(SchNetLabel Label, int Rep, int SheetId)> labelReps,
        UnionFind uf,
        bool mergeLabels,
        bool mergePorts)
    {
        foreach (var sheet in sheets)
        {
            var nameToRep = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            void Link(string? name, int elemId)
            {
                if (string.IsNullOrEmpty(name))
                    return;
                if (nameToRep.TryGetValue(name, out var first))
                    uf.Union(first, elemId);
                else
                    nameToRep[name] = elemId;
            }

            foreach (var e in sheet.Elements)
            {
                if (e.IntrinsicScope == NetScope.Power && !string.IsNullOrEmpty(e.IntrinsicName))
                    Link(e.IntrinsicName, e.Id);
                else if (mergePorts && e.Kind == ElementKind.Port && !string.IsNullOrEmpty(e.IntrinsicName))
                    Link(e.IntrinsicName, e.Id);
            }

            if (mergeLabels)
                foreach (var (label, rep, sheetId) in labelReps)
                    if (sheetId == sheet.SheetId)
                        Link(label.Text, rep);
        }
    }

    /// <summary>Maps each net label to its net root after all merging, for naming.</summary>
    internal static Dictionary<int, List<NetLabelBinding>> BindLabelsToRoots(
        IReadOnlyList<(SchNetLabel Label, int Rep, int SheetId)> labelReps, UnionFind uf)
    {
        var result = new Dictionary<int, List<NetLabelBinding>>();
        foreach (var (label, rep, sheetId) in labelReps)
        {
            var root = uf.Find(rep);
            if (!result.TryGetValue(root, out var list))
                result[root] = list = new List<NetLabelBinding>();
            list.Add(new NetLabelBinding(label, sheetId));
        }
        return result;
    }
}

/// <summary>A net label bound to a net root, with the sheet it came from.</summary>
internal readonly record struct NetLabelBinding(SchNetLabel Label, int SheetId);
