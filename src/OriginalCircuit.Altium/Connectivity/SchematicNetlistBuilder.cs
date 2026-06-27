using OriginalCircuit.Altium.Connectivity.Internal;
using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Sch;

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

        var sheets = new[] { sheet };

        // Power nets unify globally by name. Net labels and ports unify within the single sheet.
        UnifyPower(sheets, uf);
        var labelReps = ComputeLabelReps(sheet, uf, diagnostics);
        MergeIdentifiers(sheets, labelReps, uf, labelsGlobal: false, portsGlobal: false);

        if (options.ResolveHarnesses)
            HarnessResolver.Resolve(sheets, uf, tolRaw);

        var labelsByRoot = BindLabelsToRoots(labelReps, uf);

        var assembler = new NetlistAssembler(elements, uf, sheets, labelsByRoot, options, diagnostics);
        var result = assembler.Assemble();

        if (options.ExtractIntents)
            NetIntentExtractor.Extract(sheets, uf, result.RootToNet, options);

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
            // A ranged label (e.g. "D[0..7]") declares a bus; it is not itself a net name. Its members
            // are the individually-labelled wires (D0..D7), which bind normally.
            if (BusRange.IsRanged(label.Text))
                continue;

            int? rep = null;
            foreach (var elem in sheet.Points.Query(label.Location)) { rep = elem; break; }
            if (rep is null)
                foreach (var elem in sheet.Segments.ElementsAt(label.Location, interiorOnly: false)) { rep = elem; break; }

            if (rep is null)
            {
                diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Info,
                    $"Net label '{label.Text}' on {sheet.FileName} is not on any wire."));
                continue;
            }
            reps.Add((label, rep.Value, sheet.SheetId));
        }
        return reps;
    }

    /// <summary>
    /// Unifies power objects / hidden power pins that share a name. Power on a non-repeated sheet is
    /// global by name. Power inside a repeated (multi-channel) instance is channel-private — keyed by
    /// the instance — so a channel-local rail (e.g. a per-channel <c>5V</c>) does not merge with the same
    /// name in another channel or at board level; it escapes only through the port↔sheet-entry boundary,
    /// which carries a truly-global rail (e.g. <c>GND</c>) up to its global power object.
    /// </summary>
    internal static void UnifyPower(IReadOnlyList<SheetGraph> sheets, UnionFind uf, IReadOnlySet<int>? repeatedInstanceIds = null)
    {
        var byGlobalName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byChannelName = new Dictionary<(int Sheet, string Name), int>();
        foreach (var sheet in sheets)
        {
            var channelScoped = repeatedInstanceIds?.Contains(sheet.SheetId) == true;
            foreach (var e in sheet.Elements)
            {
                if (e.IntrinsicScope != NetScope.Power || string.IsNullOrEmpty(e.IntrinsicName))
                    continue;
                if (channelScoped)
                {
                    if (byChannelName.TryGetValue((sheet.SheetId, e.IntrinsicName), out var first))
                        uf.Union(first, e.Id);
                    else
                        byChannelName[(sheet.SheetId, e.IntrinsicName)] = e.Id;
                }
                else
                {
                    Link(byGlobalName, e.IntrinsicName, e.Id, uf);
                }
            }
        }
    }

    /// <summary>
    /// Unifies net labels and ports that share a name. Each is merged globally across all sheets or
    /// per-sheet depending on the active net-identifier scope.
    /// </summary>
    internal static void MergeIdentifiers(
        IReadOnlyList<SheetGraph> sheets,
        IReadOnlyList<(SchNetLabel Label, int Rep, int SheetId)> labelReps,
        UnionFind uf,
        bool labelsGlobal,
        bool portsGlobal)
    {
        var globalLabels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var globalPorts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in sheets)
        {
            var ports = portsGlobal ? globalPorts : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in sheet.Elements)
                if (e.Kind == ElementKind.Port && !string.IsNullOrEmpty(e.IntrinsicName))
                    Link(ports, e.IntrinsicName, e.Id, uf);

            var labels = labelsGlobal ? globalLabels : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var (label, rep, sheetId) in labelReps)
                if (sheetId == sheet.SheetId)
                    Link(labels, label.Text, rep, uf);
        }
    }

    private static void Link(Dictionary<string, int> nameToRep, string? name, int elemId, UnionFind uf)
    {
        if (string.IsNullOrEmpty(name))
            return;
        if (nameToRep.TryGetValue(name, out var first))
            uf.Union(first, elemId);
        else
            nameToRep[name] = elemId;
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
