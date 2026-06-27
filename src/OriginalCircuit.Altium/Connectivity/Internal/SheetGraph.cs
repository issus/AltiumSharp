using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>
/// Extracts the connectable elements from a single schematic document and applies Altium's implicit
/// intra-sheet connectivity rules (coincidence, T-junctions, manual junctions, collinear overlap) into
/// a shared union-find. Cross-sheet merging (ports, power, global labels) is layered on top by the
/// project-level builder.
/// </summary>
internal sealed class SheetGraph
{
    private readonly long _tolRaw;

    public SheetGraph(SchDocument doc, int sheetId, string? fileName, List<Element> globalElements, long tolRaw)
    {
        Doc = doc;
        SheetId = sheetId;
        FileName = fileName;
        _tolRaw = tolRaw;
        Extract(globalElements);
    }

    public SchDocument Doc { get; }
    public int SheetId { get; }
    public string? FileName { get; }

    public List<Element> Elements { get; } = new();
    public List<SchNetLabel> NetLabels { get; } = new();
    public List<SchJunction> Junctions { get; } = new();
    public List<SchPowerObject> Powers { get; } = new();
    public List<(SchPort Port, Element Elem)> Ports { get; } = new();
    public List<(SchSheetSymbol Symbol, SchSheetEntry Entry, Element Elem)> SheetEntries { get; } = new();
    public List<(SchBus Bus, Element Elem)> Buses { get; } = new();
    public List<(SchBusEntry Entry, Element Elem)> BusEntries { get; } = new();
    public List<HarnessConnectorInfo> HarnessConnectors { get; } = new();
    public List<CoordPoint> SignalHarnessPoints { get; } = new();
    public List<(CoordPoint A, CoordPoint B)> SignalHarnessSegments { get; } = new();

    public PointIndex Points { get; private set; } = null!;
    public SegmentIndex Segments { get; private set; } = null!;

    private Element NewElement(ElementKind kind, object primitive, List<Element> global)
    {
        var e = new Element(global.Count, kind, primitive, SheetId);
        global.Add(e);
        Elements.Add(e);
        return e;
    }

    private void Extract(List<Element> global)
    {
        // --- Wires ---
        foreach (var wire in Doc.Wires)
        {
            var e = NewElement(ElementKind.Wire, wire, global);
            AddPolyline(e, wire.Vertices);
        }

        // --- Buses (conductors; members resolved in the bus phase) ---
        foreach (var bus in Doc.Buses)
        {
            var e = NewElement(ElementKind.Bus, bus, global);
            AddPolyline(e, bus.Vertices);
            Buses.Add((bus, e));
        }

        // --- Bus entries (wire<->bus adapters) ---
        foreach (var be in Doc.BusEntries)
        {
            var e = NewElement(ElementKind.BusEntry, be, global);
            e.Points.Add(be.Location);
            e.Points.Add(be.Corner);
            e.Segments.Add((be.Location, be.Corner));
            BusEntries.Add((be, e));
        }

        // --- Component pins ---
        foreach (var comp in Doc.Components)
        {
            if (comp is not SchComponent sc)
                continue;
            var designator = SchDesignators.GetDesignator(sc);
            foreach (var ip in sc.Pins)
            {
                if (ip is not SchPin pin)
                    continue;

                // A multi-part component record carries pins from every part, but only the displayed
                // part (CurrentPartId) is actually placed on this sheet — the other parts' pins keep
                // stale positions. Include only the current part's pins (plus part-shared pins).
                if (sc.PartCount > 1 && sc.CurrentPartId > 0
                    && pin.OwnerPartId > 0 && pin.OwnerPartId != sc.CurrentPartId)
                    continue;

                var tip = SchDesignators.PinTip(pin);
                var e = NewElement(ElementKind.Pin, pin, global);
                e.Points.Add(tip);
                e.ComponentDesignator = designator;
                e.PinDesignator = pin.Designator;
                e.NetPin = new NetPin(
                    designator ?? "?",
                    pin.Designator ?? "?",
                    pin.Name,
                    pin.ElectricalType,
                    tip,
                    pin.OwnerPartId,
                    pin.IsHidden,
                    pin,
                    sc,
                    SheetId);

                // Hidden pins with an explicit net name join that named net globally (implicit power pins).
                if (pin.IsHidden && !string.IsNullOrEmpty(pin.HiddenNetName))
                {
                    e.IntrinsicName = pin.HiddenNetName;
                    e.IntrinsicScope = NetScope.Power;
                }
            }
        }

        // --- Power objects (global by name) ---
        foreach (var ipo in Doc.PowerObjects)
        {
            if (ipo is not SchPowerObject po)
                continue;
            var e = NewElement(ElementKind.Power, po, global);
            e.Points.Add(po.Location);
            e.IntrinsicName = po.Text;
            e.IntrinsicScope = NetScope.Power;
            Powers.Add(po);
        }

        // --- Ports (off-sheet connection points) ---
        foreach (var port in Doc.Ports)
        {
            var e = NewElement(ElementKind.Port, port, global);
            foreach (var pt in PortConnectionPoints(port))
                e.Points.Add(pt);
            e.IntrinsicName = port.Name;
            e.IntrinsicScope = NetScope.CrossSheetPort;
            Ports.Add((port, e));
        }

        // --- Sheet symbols: each entry is a cross-boundary connection point ---
        foreach (var sym in Doc.SheetSymbols)
        {
            foreach (var entry in sym.Entries)
            {
                var e = NewElement(ElementKind.SheetEntry, entry, global);
                e.Points.Add(SheetEntryPoint(sym, entry));
                e.IntrinsicName = entry.Name;
                e.IntrinsicScope = NetScope.CrossSheetPort;
                SheetEntries.Add((sym, entry, e));
            }
        }

        // --- Signal harnesses (bundle conductors; resolved in the harness layer, not the net layer) ---
        foreach (var sh in Doc.SignalHarnesses)
        {
            var v = sh.Vertices;
            for (var i = 0; i < v.Count; i++)
            {
                SignalHarnessPoints.Add(v[i]);
                if (i + 1 < v.Count)
                    SignalHarnessSegments.Add((v[i], v[i + 1]));
            }
        }

        // --- Harness connectors: bundle point + per-member entry connection points ---
        foreach (var hc in Doc.HarnessConnectors)
        {
            var bundlePoint = new CoordPoint(hc.Location.X, hc.Location.Y - hc.PrimaryConnectionPosition);
            var info = new HarnessConnectorInfo(hc, bundlePoint, SheetId);
            foreach (var entry in hc.Entries)
            {
                var ex = entry.Side == 1 ? hc.Location.X + hc.XSize : hc.Location.X;
                var ey = hc.Location.Y - entry.DistanceFromTop;
                var elem = NewElement(ElementKind.HarnessEntry, entry, global);
                elem.Points.Add(new CoordPoint(ex, ey));
                info.Entries.Add((entry.Text ?? string.Empty, elem));
            }
            HarnessConnectors.Add(info);
        }

        // --- Namers / forcers ---
        foreach (var nl in Doc.NetLabels)
            if (nl is SchNetLabel label)
                NetLabels.Add(label);
        foreach (var j in Doc.Junctions)
            if (j is SchJunction junction)
                Junctions.Add(junction);
    }

    private void AddPolyline(Element e, IReadOnlyList<CoordPoint> verts)
    {
        for (var i = 0; i < verts.Count; i++)
        {
            e.Points.Add(verts[i]);
            if (i + 1 < verts.Count)
                e.Segments.Add((verts[i], verts[i + 1]));
        }
    }

    private static IEnumerable<CoordPoint> PortConnectionPoints(SchPort port)
    {
        // Per the renderer, Location IS the wire connection point at the body's vertical centre; the
        // body extends right by Width on the same centre line. ConnectedEnd selects the wired end.
        var left = port.Location;
        var right = new CoordPoint(port.Location.X + port.Width, port.Location.Y);
        switch (port.ConnectedEnd)
        {
            case 1: yield return left; break;
            case 2: yield return right; break;
            default:
                yield return left;
                yield return right;
                break;
        }
    }

    private static CoordPoint SheetEntryPoint(SchSheetSymbol sym, SchSheetEntry entry)
    {
        // Symbol is anchored top-left: Location is the top edge; body extends down by YSize, right by XSize.
        var left = sym.Location.X;
        var right = sym.Location.X + sym.XSize;
        var top = sym.Location.Y;
        var bottom = sym.Location.Y - sym.YSize;

        // The renderer anchors every entry to the LEFT edge except Side==1 (Right); the connection
        // point sits at y = top - DistanceFromTop. Mirroring swaps which edge is the "right" one.
        var isRight = entry.Side == 1;
        if (sym.IsMirrored)
            isRight = !isRight;
        var x = isRight ? right : left;
        _ = bottom;
        return new CoordPoint(x, top - entry.DistanceFromTop);
    }

    /// <summary>Builds the point and segment indexes over this sheet's elements.</summary>
    public void BuildIndexes()
    {
        Points = new PointIndex(_tolRaw);
        Segments = new SegmentIndex(_tolRaw);
        foreach (var e in Elements)
        {
            // Buses and bus entries are inert: they must not merge member nets geometrically.
            if (!e.ParticipatesInGeometry)
                continue;
            foreach (var p in e.Points)
                Points.Add(p, e.Id);
            foreach (var (a, b) in e.Segments)
                Segments.Add(a, b, e.Id);
        }
    }

    /// <summary>Applies the implicit intra-sheet connectivity rules into <paramref name="uf"/>.</summary>
    public void ApplyRules(UnionFind uf)
    {
        // Rule 1 & 3a: coincident connection points connect (shared endpoints, pin-tip on wire vertex).
        Points.UnionCoincident(uf);

        // Rule 2 & 3b: a connection point on the INTERIOR of a conductor connects (T-junction).
        // Applies to any element's points landing on a different conductor's interior.
        foreach (var e in Elements)
        {
            if (!e.ParticipatesInGeometry)
                continue;
            foreach (var p in e.Points)
            {
                foreach (var other in Segments.ElementsAt(p, interiorOnly: true))
                {
                    if (other != e.Id)
                        uf.Union(e.Id, other);
                }
            }
        }

        // Rule 3c: manual junctions force every conductor / connection passing through to connect
        // (legitimises a 4-way crossover that the T-rule deliberately leaves open).
        foreach (var j in Junctions)
        {
            var hits = new List<int>();
            hits.AddRange(Points.Query(j.Location));
            hits.AddRange(Segments.ElementsAt(j.Location, interiorOnly: false));
            for (var i = 1; i < hits.Count; i++)
                uf.Union(hits[0], hits[i]);
        }

        // Rule 3d: collinear overlapping conductor segments connect.
        ApplyCollinearOverlap(uf);
    }

    private void ApplyCollinearOverlap(UnionFind uf)
    {
        // Conductors only; pairwise within shared coarse buckets would be ideal, but conductor counts
        // per sheet are modest — compare each conductor's segments against candidates sharing a point
        // cell is approximated here by an O(n^2) guarded scan over conductor segments.
        var conductors = Elements.Where(e => e.IsConductor).ToList();
        for (var i = 0; i < conductors.Count; i++)
        {
            for (var k = i + 1; k < conductors.Count; k++)
            {
                var a = conductors[i];
                var b = conductors[k];
                if (uf.Connected(a.Id, b.Id))
                    continue;
                if (SegmentsOverlap(a, b))
                    uf.Union(a.Id, b.Id);
            }
        }
    }

    private bool SegmentsOverlap(Element a, Element b)
    {
        foreach (var (a1, a2) in a.Segments)
            foreach (var (b1, b2) in b.Segments)
                if (ConnectivityGeometry.SegmentsCollinearOverlap(a1, a2, b1, b2, _tolRaw))
                    return true;
        return false;
    }
}
