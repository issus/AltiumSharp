using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>A harness connector reduced to its bundle connection point and per-member entry elements.</summary>
internal sealed class HarnessConnectorInfo
{
    public HarnessConnectorInfo(Models.Sch.SchHarnessConnector connector, CoordPoint bundlePoint, int sheetId)
    {
        Connector = connector;
        BundlePoint = bundlePoint;
        SheetId = sheetId;
    }

    public Models.Sch.SchHarnessConnector Connector { get; }
    public CoordPoint BundlePoint { get; }
    public int SheetId { get; }

    /// <summary>The bundle members: each member name and its connection-point element (which touches the member wire).</summary>
    public List<(string Text, Element Elem)> Entries { get; } = new();
}

/// <summary>
/// Resolves harness-bundle connectivity. Inside each sheet a harness connector's bundle output is wired
/// (through signal harnesses) to a harness port or a bundle net label that names the bundle (e.g.
/// <c>MEZIO</c>). Each member entry of the connector is therefore a qualified net <c>bundle.member</c>
/// (e.g. <c>MEZIO.MEZ_SPI4_CS2</c>); the same qualified member appears wherever the bundle is broken out,
/// so unioning member entries by qualified name across the whole project reconnects harness signals.
/// </summary>
internal static class HarnessResolver
{
    public static void Resolve(IReadOnlyList<SheetGraph> sheets, UnionFind mainUf, long tolRaw)
    {
        var byQualified = new Dictionary<string, List<Element>>(StringComparer.OrdinalIgnoreCase);

        // A document instantiated more than once is a repeated (multi-channel) sheet: its bundle names
        // collide across instances, so qualify those per instance to keep channels separate. Single-
        // instance documents keep a project-global qualified name so a bundle spanning two different
        // documents (e.g. an MCU sheet and a connector sheet) still reconnects.
        var instanceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sheets)
            instanceCounts[s.FileName ?? string.Empty] = instanceCounts.GetValueOrDefault(s.FileName ?? string.Empty) + 1;

        foreach (var sheet in sheets)
        {
            if (sheet.HarnessConnectors.Count == 0)
                continue;

            var bundleName = ResolveBundleNames(sheet, tolRaw);
            var repeated = instanceCounts.GetValueOrDefault(sheet.FileName ?? string.Empty) > 1;
            var scope = repeated ? $"@{sheet.SheetId}|" : string.Empty;

            foreach (var info in sheet.HarnessConnectors)
            {
                if (!bundleName.TryGetValue(info, out var name) || string.IsNullOrEmpty(name))
                    name = info.Connector.TypeLabel?.Text;
                if (string.IsNullOrEmpty(name))
                    continue;

                foreach (var (text, elem) in info.Entries)
                {
                    if (string.IsNullOrEmpty(text))
                        continue;
                    var qualified = $"{scope}{name}.{text}";
                    if (!byQualified.TryGetValue(qualified, out var list))
                        byQualified[qualified] = list = new List<Element>();
                    list.Add(elem);
                }
            }
        }

        foreach (var list in byQualified.Values)
            for (var i = 1; i < list.Count; i++)
                mainUf.Union(list[0].Id, list[i].Id);
    }

    /// <summary>
    /// Determines each connector's bundle name by following its bundle output through signal harnesses
    /// to a harness port (whose name is the bundle name) or a bundle net label on the signal harness.
    /// </summary>
    private static Dictionary<HarnessConnectorInfo, string> ResolveBundleNames(SheetGraph sheet, long tolRaw)
    {
        // Bundle-layer nodes: connectors, harness ports, and signal-harness segments. Group them by
        // geometric coincidence so each connector shares a group with the port / label that names it.
        var nodes = new List<(CoordPoint[] Points, (CoordPoint A, CoordPoint B)[] Segs)>();
        var connectorNode = new List<(HarnessConnectorInfo Info, int Node)>();
        var portNode = new List<(string Name, int Node)>();
        var signalNodes = new List<int>();

        int AddNode(CoordPoint[] pts, (CoordPoint, CoordPoint)[] segs)
        {
            nodes.Add((pts, segs));
            return nodes.Count - 1;
        }

        foreach (var info in sheet.HarnessConnectors)
            connectorNode.Add((info, AddNode(new[] { info.BundlePoint }, Array.Empty<(CoordPoint, CoordPoint)>())));

        foreach (var (port, elem) in sheet.Ports)
        {
            if (string.IsNullOrEmpty(port.HarnessType))
                continue;
            portNode.Add((port.Name, AddNode(elem.Points.ToArray(), Array.Empty<(CoordPoint, CoordPoint)>())));
        }

        foreach (var (a, b) in sheet.SignalHarnessSegments)
            signalNodes.Add(AddNode(new[] { a, b }, new[] { (a, b) }));

        var buf = new UnionFind(nodes.Count);
        var pts = new PointIndex(tolRaw);
        var segs = new SegmentIndex(tolRaw);
        for (var i = 0; i < nodes.Count; i++)
        {
            foreach (var p in nodes[i].Points)
                pts.Add(p, i);
            foreach (var s in nodes[i].Segs)
                segs.Add(s.A, s.B, i);
        }
        pts.UnionCoincident(buf);
        for (var i = 0; i < nodes.Count; i++)
            foreach (var p in nodes[i].Points)
                foreach (var other in segs.ElementsAt(p, interiorOnly: false))
                    if (other != i)
                        buf.Union(i, other);

        // Group -> bundle name: prefer a harness port name, else a bundle net label on a signal harness.
        var groupName = new Dictionary<int, string>();
        foreach (var (name, node) in portNode)
        {
            if (string.IsNullOrEmpty(name))
                continue;
            var root = buf.Find(node);
            if (!groupName.ContainsKey(root))
                groupName[root] = name;
        }
        foreach (var label in sheet.NetLabels)
        {
            foreach (var node in segs.ElementsAt(label.Location, interiorOnly: false))
            {
                var root = buf.Find(node);
                if (!groupName.ContainsKey(root) && !string.IsNullOrEmpty(label.Text))
                    groupName[root] = label.Text;
                break;
            }
        }

        var result = new Dictionary<HarnessConnectorInfo, string>();
        foreach (var (info, node) in connectorNode)
            if (groupName.TryGetValue(buf.Find(node), out var name))
                result[info] = name;
        return result;
    }
}
