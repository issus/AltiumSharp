using System.Text.Json.Nodes;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Rendering.Gltf.Geometry;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Mech.GLTF;
using OriginalCircuit.Mech.GLTF.Step;

namespace OriginalCircuit.Altium.Rendering.Gltf;

/// <summary>
/// Turns a <see cref="PcbDocument"/> into a glTF scene. Every board feature (the laminate, each
/// copper layer, the solder mask, silkscreen and drills) and every placed component becomes its own
/// named node so a viewer can toggle them independently. All geometry is authored in board space —
/// Altium X/Y millimetres with Z as the stack height — and a single root node maps that Z-up
/// millimetre space into glTF's Y-up metre convention.
/// </summary>
internal sealed class GltfSceneBuilder
{
    private readonly PcbDocument _doc;
    private readonly GltfRenderSettings _settings;
    private readonly PcbStackup _stack;
    private readonly GltfBuilder _builder = new("OriginalCircuit.Altium.Rendering.Gltf");
    private readonly List<int> _rootChildren = [];
    private readonly Dictionary<int, int> _copperOrder = [];

    private double _cx, _cy;             // board centre (mm) subtracted from every point
    private int _matSubstrate, _matCopper, _matMask, _matSilk, _matBarrel;

    public GltfSceneBuilder(PcbDocument doc, GltfRenderSettings settings)
    {
        _doc = doc;
        _settings = settings;
        _stack = doc.GetStackup() ?? PcbStackup.CreateDefault(settings.FallbackBoardThicknessMm, InferCopperCount(doc));

        int order = 0;
        foreach (var c in _stack.CopperLayers)
            if (c.Layer is int id) _copperOrder[id] = order++;
    }

    public GltfDocument Build()
    {
        var bounds = ComputeBoundsMm();
        _cx = (bounds.minX + bounds.maxX) / 2.0;
        _cy = (bounds.minY + bounds.maxY) / 2.0;

        AddMaterials();

        if (_settings.IncludeSubstrate) BuildSubstrate(bounds);
        if (_settings.IncludeCopper) BuildCopperLayers();
        if (_settings.IncludeSolderMask) BuildSolderMask(bounds);
        if (_settings.IncludeSilkscreen) BuildSilkscreen();
        if (_settings.IncludeDrills) BuildDrills();
        if (_settings.IncludeComponents) BuildComponents();

        int root = _builder.AddNode(
            name: "Board",
            matrix: StepCoordinates.ZUpMillimetresToYUpMetres(),
            children: _rootChildren.Count > 0 ? _rootChildren : null);
        _builder.AddScene([root]);
        return _builder.Build();
    }

    // The document exposes primitives through interfaces; the instances are the concrete Altium types,
    // so narrow to them for the concrete-only geometry/shape/layer properties this renderer needs.
    private IEnumerable<PcbTrack> Tracks => _doc.Tracks.OfType<PcbTrack>();
    private IEnumerable<PcbArc> Arcs => _doc.Arcs.OfType<PcbArc>();
    private IEnumerable<PcbFill> Fills => _doc.Fills.OfType<PcbFill>();
    private IEnumerable<PcbRegion> Regions => _doc.Regions.OfType<PcbRegion>();
    private IEnumerable<PcbPad> Pads => _doc.Pads.OfType<PcbPad>();
    private IEnumerable<PcbVia> Vias => _doc.Vias.OfType<PcbVia>();

    private void AddMaterials()
    {
        _matSubstrate = _builder.AddMaterial(GltfPalette.Substrate);
        _matCopper = _builder.AddMaterial(GltfPalette.Copper(_settings.CopperFinish, doubleSided: true));
        _matMask = _builder.AddMaterial(GltfPalette.SolderMask);
        _matSilk = _builder.AddMaterial(GltfPalette.Silkscreen);
        _matBarrel = _builder.AddMaterial(GltfPalette.Copper(_settings.CopperFinish, doubleSided: true));
    }

    // ── Substrate ───────────────────────────────────────────────────────────────────────────────
    private void BuildSubstrate((double minX, double minY, double maxX, double maxY) bounds)
    {
        var ring = OutlineRing(bounds);
        if (ring.Count < 3) return;

        var diel = _stack.Layers.Where(l => l.Kind == PcbStackupLayerKind.Dielectric).ToList();
        double z0 = diel.Count > 0 ? diel.Min(d => d.Z0Mm) : 0;
        double z1 = diel.Count > 0 ? diel.Max(d => d.Z1Mm) : _stack.TotalThicknessMm;

        var mesh = new MeshBuffer();
        mesh.AddPrism(ring, holes: null, z0, z1);
        Emit(mesh, _matSubstrate, "Substrate", "substrate", null);
    }

    // ── Copper ──────────────────────────────────────────────────────────────────────────────────
    private void BuildCopperLayers()
    {
        foreach (var layer in _stack.CopperLayers)
        {
            int id = layer.Layer ?? 0;
            if (id == 0) continue;
            if (_settings.CopperLayerFilter is { } filter && !filter.Contains(id)) continue;

            var mesh = new MeshBuffer();
            GatherCopper(mesh, id, layer.CenterZMm);
            Emit(mesh, _matCopper, $"Copper.{layer.Name}", "copper", id);
        }
    }

    private void GatherCopper(MeshBuffer mesh, int layerId, double z)
    {
        foreach (var t in Tracks)
            if (t.Layer == layerId && t.Width.ToMm() > 0)
                mesh.AddSheet(Shapes.Capsule(P(t.Start), P(t.End), t.Width.ToMm(), Caps(t.Width.ToMm() / 2)), null, z);

        foreach (var a in Arcs)
            if (a.Layer == layerId && a.Radius.ToMm() > 0 && a.Width.ToMm() > 0)
                mesh.AddSheet(ArcBand(a), null, z);

        foreach (var f in Fills)
            if (f.Layer == layerId)
                mesh.AddSheet(FillRect(f), null, z);

        foreach (var r in Regions)
            if (r.Layer == layerId && r.Kind == 0 && !r.IsKeepout && r.Outline.Count >= 3)
                mesh.AddSheet(Ring(r.Outline), null, z);

        foreach (var pad in Pads)
        {
            var contour = PadContourForLayer(pad, layerId);
            if (contour is not null) mesh.AddSheet(contour, null, z);
        }

        foreach (var via in Vias)
        {
            if (!ViaSpans(via, layerId)) continue;
            double outer = via.Diameter.ToMm() / 2.0, inner = via.HoleSize.ToMm() / 2.0;
            if (outer <= 0) continue;
            var center = P(via.Location);
            var ring = Shapes.Circle(center, outer, Seg(outer));
            var holes = inner > 0 ? new List<IReadOnlyList<Vec2>> { Shapes.Circle(center, inner, Seg(inner)) } : null;
            mesh.AddSheet(ring, holes, z);
        }
    }

    // ── Solder mask ─────────────────────────────────────────────────────────────────────────────
    private void BuildSolderMask((double minX, double minY, double maxX, double maxY) bounds)
    {
        var ring = OutlineRing(bounds);
        if (ring.Count < 3) return;

        var top = _stack.ForLayer(37);
        if (top is not null && HasGeometryOnLayer(37))
            EmitSheet(ring, _matMask, top.CenterZMm, "SolderMask.Top", "soldermask", 37);

        var bottom = _stack.ForLayer(38);
        if (bottom is not null && HasGeometryOnLayer(38))
            EmitSheet(ring, _matMask, bottom.CenterZMm, "SolderMask.Bottom", "soldermask", 38);
    }

    // ── Silkscreen ──────────────────────────────────────────────────────────────────────────────
    private void BuildSilkscreen()
    {
        double topZ = (_stack.ForLayer(37)?.Z1Mm ?? _stack.TotalThicknessMm) + 0.01;
        double botZ = (_stack.ForLayer(38)?.Z0Mm ?? 0) - 0.01;
        BuildOverlay(33, topZ, "Silkscreen.Top");
        BuildOverlay(34, botZ, "Silkscreen.Bottom");
    }

    private void BuildOverlay(int layerId, double z, string name)
    {
        var mesh = new MeshBuffer();
        foreach (var t in Tracks)
            if (t.Layer == layerId && t.Width.ToMm() > 0)
                mesh.AddSheet(Shapes.Capsule(P(t.Start), P(t.End), t.Width.ToMm(), Caps(t.Width.ToMm() / 2)), null, z);
        foreach (var a in Arcs)
            if (a.Layer == layerId && a.Radius.ToMm() > 0 && a.Width.ToMm() > 0)
                mesh.AddSheet(ArcBand(a), null, z);
        foreach (var f in Fills)
            if (f.Layer == layerId)
                mesh.AddSheet(FillRect(f), null, z);
        Emit(mesh, _matSilk, name, "silkscreen", layerId);
    }

    // ── Drills / via barrels ────────────────────────────────────────────────────────────────────
    private void BuildDrills()
    {
        var mesh = new MeshBuffer();
        double zTop = _stack.ForLayer(1)?.Z1Mm ?? _stack.TotalThicknessMm;
        double zBot = _stack.ForLayer(32)?.Z0Mm ?? 0;

        foreach (var pad in Pads)
        {
            double r = pad.HoleSize.ToMm() / 2.0;
            if (r > 0) mesh.AddCylinderWall(P(pad.Location), r, zBot, zTop, Seg(r));
        }
        foreach (var via in Vias)
        {
            double r = via.HoleSize.ToMm() / 2.0;
            if (r <= 0) continue;
            double zt = _stack.ForLayer(via.StartLayer)?.Z1Mm ?? zTop;
            double zb = _stack.ForLayer(via.EndLayer)?.Z0Mm ?? zBot;
            mesh.AddCylinderWall(P(via.Location), r, zb, zt, Seg(r));
        }
        Emit(mesh, _matBarrel, "Drills", "drills", null);
    }

    // ── Components (filled in by GltfComponentPlacer) ───────────────────────────────────────────
    private void BuildComponents()
    {
        var placer = new GltfComponentPlacer(_doc, _settings, _stack, _builder, _cx, _cy);
        int? group = placer.Build();
        if (group is int node) _rootChildren.Add(node);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────────
    private void Emit(MeshBuffer mesh, int material, string name, string role, int? altiumLayer)
    {
        if (mesh.IsEmpty) return;
        int meshIndex = _builder.AddMesh(mesh.Positions, mesh.Normals, mesh.Indices,
            [new MeshPartSpec(0, mesh.Indices.Count, material)], name);
        _rootChildren.Add(_builder.AddNode(mesh: meshIndex, name: name, extras: Extras(role, altiumLayer)));
    }

    private void EmitSheet(IReadOnlyList<Vec2> ring, int material, double z, string name, string role, int? altiumLayer)
    {
        var mesh = new MeshBuffer();
        mesh.AddSheet(ring, null, z);
        Emit(mesh, material, name, role, altiumLayer);
    }

    private static JsonObject Extras(string role, int? altiumLayer)
    {
        var extras = new JsonObject { ["role"] = role };
        if (altiumLayer is int al) extras["altiumLayer"] = al;
        return extras;
    }

    private Vec2 P(CoordPoint p) => new(p.X.ToMm() - _cx, p.Y.ToMm() - _cy);

    private List<Vec2> Ring(IReadOnlyList<CoordPoint> pts)
    {
        var ring = new List<Vec2>(pts.Count);
        foreach (var p in pts) ring.Add(P(p));
        return ring;
    }

    private List<Vec2> FillRect(PcbFill f)
    {
        double cx = (f.Corner1.X.ToMm() + f.Corner2.X.ToMm()) / 2.0;
        double cy = (f.Corner1.Y.ToMm() + f.Corner2.Y.ToMm()) / 2.0;
        double w = Math.Abs(f.Corner2.X.ToMm() - f.Corner1.X.ToMm());
        double h = Math.Abs(f.Corner2.Y.ToMm() - f.Corner1.Y.ToMm());
        return Shapes.Rectangle(new Vec2(cx - _cx, cy - _cy), w, h, f.Rotation);
    }

    private List<Vec2> ArcBand(PcbArc a)
    {
        double r = a.Radius.ToMm();
        double sweep = a.EndAngle - a.StartAngle;
        if (sweep <= 0) sweep += 360;
        int segs = Math.Max(2, (int)(Seg(r) * sweep / 360.0));
        return Shapes.ArcBand(P(a.Center), r, a.StartAngle, a.EndAngle, a.Width.ToMm(), segs);
    }

    // Returns the copper contour a pad contributes to the given layer, or null.
    private List<Vec2>? PadContourForLayer(PcbPad pad, int layerId)
    {
        bool throughHole = pad.HoleSize.ToMm() > 0;
        CoordPoint size;
        PadShape shape;
        if (throughHole)
        {
            if (layerId == 1) { size = pad.SizeTop; shape = pad.ShapeTop; }
            else if (layerId == 32) { size = pad.SizeBottom; shape = pad.ShapeBottom; }
            else return null; // inner-layer through-hole openings not modelled
        }
        else
        {
            if (pad.Layer != layerId) return null;
            size = pad.SizeTop;
            shape = pad.ShapeTop;
        }

        double w = size.X.ToMm(), h = size.Y.ToMm();
        if (w <= 0 || h <= 0) return null;
        return PadContour(P(pad.Location), w, h, pad.Rotation, shape);
    }

    private List<Vec2> PadContour(Vec2 center, double w, double h, double rotationDeg, PadShape shape)
    {
        if (shape == PadShape.Round)
        {
            if (Math.Abs(w - h) < 1e-6) return Shapes.Circle(center, w / 2.0, Seg(w / 2.0));
            // Oblong: a stadium along the major axis.
            double rad = rotationDeg * Math.PI / 180.0;
            var ax = new Vec2(Math.Cos(rad), Math.Sin(rad));
            if (w >= h) { double half = (w - h) / 2.0; return Shapes.Capsule(center - (ax * half), center + (ax * half), h, Caps(h / 2)); }
            var ay = new Vec2(-Math.Sin(rad), Math.Cos(rad));
            double half2 = (h - w) / 2.0;
            return Shapes.Capsule(center - (ay * half2), center + (ay * half2), w, Caps(w / 2));
        }
        // Rectangular / Octagonal / RoundedRectangle are approximated by their bounding rectangle.
        return Shapes.Rectangle(center, w, h, rotationDeg);
    }

    private bool ViaSpans(PcbVia via, int layerId)
    {
        if (!_copperOrder.TryGetValue(layerId, out int li)) return false;
        int s = _copperOrder.GetValueOrDefault(via.StartLayer, 0);
        int e = _copperOrder.GetValueOrDefault(via.EndLayer, _copperOrder.Count - 1);
        return li >= Math.Min(s, e) && li <= Math.Max(s, e);
    }

    private bool HasGeometryOnLayer(int layerId)
        => Tracks.Any(t => t.Layer == layerId) || Arcs.Any(a => a.Layer == layerId)
           || Fills.Any(f => f.Layer == layerId) || Regions.Any(r => r.Layer == layerId)
           // Mask layers are present whenever the board has any copper pads/vias to open over.
           || layerId is 37 or 38;

    private int Seg(double radiusMm) => Shapes.SegmentCount(radiusMm, _settings.ArcChordToleranceMm);
    private int Caps(double radiusMm) => Math.Max(4, Seg(radiusMm) / 2);

    private List<Vec2> OutlineRing((double minX, double minY, double maxX, double maxY) bounds)
    {
        var outline = _doc.GetBoardOutline();
        if (outline.Count >= 3) return Ring(outline);
        // No board outline: fall back to the bounding rectangle of all geometry.
        double w = bounds.maxX - bounds.minX, h = bounds.maxY - bounds.minY;
        if (w <= 0 || h <= 0) return [];
        return Shapes.Rectangle(new Vec2(0, 0), w, h, 0);
    }

    private (double minX, double minY, double maxX, double maxY) ComputeBoundsMm()
    {
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        void Acc(double x, double y)
        {
            if (x < minX) minX = x; if (y < minY) minY = y;
            if (x > maxX) maxX = x; if (y > maxY) maxY = y;
        }

        var outline = _doc.GetBoardOutline();
        if (outline.Count > 0)
        {
            foreach (var p in outline) Acc(p.X.ToMm(), p.Y.ToMm());
        }
        else
        {
            foreach (var t in Tracks) { Acc(t.Start.X.ToMm(), t.Start.Y.ToMm()); Acc(t.End.X.ToMm(), t.End.Y.ToMm()); }
            foreach (var pd in Pads) Acc(pd.Location.X.ToMm(), pd.Location.Y.ToMm());
            foreach (var v in Vias) Acc(v.Location.X.ToMm(), v.Location.Y.ToMm());
        }

        if (double.IsInfinity(minX)) return (0, 0, 0, 0);
        return (minX, minY, maxX, maxY);
    }

    private static int InferCopperCount(PcbDocument doc)
    {
        var layers = new HashSet<int>();
        foreach (var t in doc.Tracks) if (t.Layer is >= 1 and <= 32) layers.Add(t.Layer);
        foreach (var r in doc.Regions) if (r.Layer is >= 1 and <= 32) layers.Add(r.Layer);
        return Math.Max(2, layers.Count);
    }
}
