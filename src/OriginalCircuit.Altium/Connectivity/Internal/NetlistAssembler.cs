using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>The outcome of assembling union-find components into named nets.</summary>
internal sealed class AssembleResult
{
    public required List<SchematicNet> Nets { get; init; }
    public required List<NetPin> Unconnected { get; init; }
    public required Dictionary<int, SchematicNet> RootToNet { get; init; }
}

/// <summary>
/// Groups the solved union-find components into <see cref="SchematicNet"/> objects, choosing each net's
/// name by Altium's priority (explicit net label &gt; power &gt; port &gt; deterministic auto-name) and
/// separating truly-unconnected pins.
/// </summary>
internal sealed class NetlistAssembler
{
    private readonly List<Element> _elements;
    private readonly UnionFind _uf;
    private readonly IReadOnlyList<SheetGraph> _sheets;
    private readonly Dictionary<int, List<NetLabelBinding>> _labelsByRoot;
    private readonly NetlistOptions _options;
    private readonly List<AltiumDiagnostic> _diagnostics;

    public NetlistAssembler(
        List<Element> elements,
        UnionFind uf,
        IReadOnlyList<SheetGraph> sheets,
        Dictionary<int, List<NetLabelBinding>> labelsByRoot,
        NetlistOptions options,
        List<AltiumDiagnostic> diagnostics)
    {
        _elements = elements;
        _uf = uf;
        _sheets = sheets;
        _labelsByRoot = labelsByRoot;
        _options = options;
        _diagnostics = diagnostics;
    }

    public AssembleResult Assemble()
    {
        var groups = new Dictionary<int, List<Element>>();
        foreach (var e in _elements)
        {
            var root = _uf.Find(e.Id);
            if (!groups.TryGetValue(root, out var list))
                groups[root] = list = new List<Element>();
            list.Add(e);
        }

        var nets = new List<SchematicNet>();
        var unconnected = new List<NetPin>();
        var rootToNet = new Dictionary<int, SchematicNet>();

        foreach (var (root, group) in groups)
        {
            var pins = new List<NetPin>();
            foreach (var e in group)
                if (e.Kind == ElementKind.Pin && e.NetPin is not null)
                    pins.Add(e.NetPin);

            var hasLabel = _labelsByRoot.TryGetValue(root, out var labels) && labels.Count > 0;
            var (name, scope, explicitName) = ChooseName(root, group, pins, hasLabel ? labels! : null);

            // Truly unconnected: a lone pin touching nothing, with no name.
            if (pins.Count == 1 && group.Count == 1 && !explicitName)
            {
                unconnected.Add(pins[0]);
                continue;
            }

            // Drop dangling unnamed conductors with no pins (pure wire stubs / graphics).
            if (pins.Count == 0 && !explicitName)
                continue;

            var sources = new List<object>(group.Count);
            foreach (var e in group)
                sources.Add(e.Primitive);
            if (labels is not null)
                foreach (var b in labels)
                    sources.Add(b.Label);

            var net = new SchematicNet(name, scope, explicitName, pins, sources, new List<NetIntent>());
            nets.Add(net);
            rootToNet[root] = net;
        }

        // Deterministic ordering: named nets first (alpha), then auto nets.
        nets.Sort(static (a, b) =>
        {
            if (a.IsNamedExplicitly != b.IsNamedExplicitly)
                return a.IsNamedExplicitly ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        return new AssembleResult { Nets = nets, Unconnected = unconnected, RootToNet = rootToNet };
    }

    private (string Name, NetScope Scope, bool Explicit) ChooseName(
        int root, List<Element> group, List<NetPin> pins, List<NetLabelBinding>? labels)
    {
        // Priority 1: explicit net label.
        if (labels is { Count: > 0 })
        {
            var names = labels.Select(b => b.Label.Text)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (names.Count > 1)
                _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning,
                    $"Net has conflicting net labels: {string.Join(", ", names)}. Using '{names[0]}'."));
            if (names.Count > 0)
            {
                var scope = _options.Scope == NetIdentifierScope.Global ? NetScope.GlobalLabel : NetScope.LocalSheet;
                return (names[0], scope, true);
            }
        }

        // Priority 2: power object / hidden power-pin name.
        var powerName = group
            .Where(e => e.IntrinsicScope == NetScope.Power && !string.IsNullOrEmpty(e.IntrinsicName))
            .Select(e => e.IntrinsicName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (powerName is not null)
            return (powerName, NetScope.Power, true);

        // Priority 3: port / sheet-entry name.
        var portName = group
            .Where(e => e.IntrinsicScope == NetScope.CrossSheetPort && !string.IsNullOrEmpty(e.IntrinsicName))
            .Select(e => e.IntrinsicName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (portName is not null)
            return (portName, NetScope.CrossSheetPort, true);

        // Priority 4: deterministic auto-name.
        return (AutoName(group, pins), NetScope.Auto, false);
    }

    private static string AutoName(List<Element> group, List<NetPin> pins)
    {
        if (pins.Count > 0)
        {
            // Altium names an unnamed net "Net<Designator>_<Pin>" using the lowest pin key.
            var smallest = pins.OrderBy(p => p.Key, NaturalComparer.Instance).First();
            return $"Net{smallest.ComponentDesignator}_{smallest.PinDesignator}";
        }

        // Pinless named net fallback (should be rare): use the smallest coordinate in the group.
        CoordPoint? min = null;
        foreach (var e in group)
            foreach (var p in e.Points)
                if (min is null || Less(p, min.Value))
                    min = p;
        var pt = min ?? CoordPoint.Zero;
        return $"N$({pt.X.ToMils():0}_{pt.Y.ToMils():0})";
    }

    private static bool Less(CoordPoint a, CoordPoint b) =>
        a.X.ToRaw() != b.X.ToRaw() ? a.X.ToRaw() < b.X.ToRaw() : a.Y.ToRaw() < b.Y.ToRaw();
}
