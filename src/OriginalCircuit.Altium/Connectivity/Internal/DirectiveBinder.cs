using OriginalCircuit.Altium.Models.Sch;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// Binds schematic directives to nets: parameter sets by geometric coincidence with a conductor, and
/// blankets by point-in-polygon over every net's conductor segments.
/// </summary>
internal static class DirectiveBinder
{
    public static void Bind(
        IReadOnlyList<SheetGraph> sheets,
        UnionFind uf,
        Dictionary<int, SchematicNet> rootToNet)
    {
        foreach (var sheet in sheets)
        {
            BindParameterSets(sheet, uf, rootToNet);
            BindBlankets(sheet, uf, rootToNet);
        }
    }

    private static void BindParameterSets(SheetGraph sheet, UnionFind uf, Dictionary<int, SchematicNet> rootToNet)
    {
        foreach (var ps in sheet.Doc.ParameterSets)
        {
            if (ps.Parameters.Count == 0)
                continue;

            var net = NetAt(sheet, ps.Location, uf, rootToNet);
            if (net is null)
                continue;

            foreach (var param in ps.Parameters)
            {
                if (string.IsNullOrEmpty(param.Name))
                    continue;
                net.AddIntent(NetIntentClassifier.Classify(param.Name, param.Value, NetIntentSource.ParameterSet, ps));
            }
        }
    }

    private static void BindBlankets(SheetGraph sheet, UnionFind uf, Dictionary<int, SchematicNet> rootToNet)
    {
        foreach (var blanket in sheet.Doc.Blankets)
        {
            if (blanket.Parameters.Count == 0 || blanket.Vertices.Count < 3)
                continue;

            // Find every net whose conductor falls inside the blanket polygon.
            var hitRoots = new HashSet<int>();
            foreach (var e in sheet.Elements)
            {
                if (!e.IsConductor && e.Kind != ElementKind.Pin)
                    continue;
                foreach (var p in e.Points)
                {
                    if (ConnectivityGeometry.PointInPolygon(p, blanket.Vertices))
                    {
                        hitRoots.Add(uf.Find(e.Id));
                        break;
                    }
                }
            }

            foreach (var root in hitRoots)
            {
                if (!rootToNet.TryGetValue(root, out var net))
                    continue;
                foreach (var param in blanket.Parameters)
                {
                    if (string.IsNullOrEmpty(param.Name))
                        continue;
                    net.AddIntent(NetIntentClassifier.Classify(param.Name, param.Value, NetIntentSource.Blanket, blanket));
                }
            }
        }
    }

    private static SchematicNet? NetAt(SheetGraph sheet, Eda.Primitives.CoordPoint location, UnionFind uf,
        Dictionary<int, SchematicNet> rootToNet)
    {
        // Coincident with a conductor / connection point.
        foreach (var elem in sheet.Points.Query(location))
            if (rootToNet.TryGetValue(uf.Find(elem), out var net))
                return net;
        foreach (var elem in sheet.Segments.ElementsAt(location, interiorOnly: false))
            if (rootToNet.TryGetValue(uf.Find(elem), out var net))
                return net;
        return null;
    }
}
