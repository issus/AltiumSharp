using System.Text.Json.Nodes;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Rendering.Gltf.Geometry;
using OriginalCircuit.Eda.Primitives;
using PcbTextKind = OriginalCircuit.Eda.Enums.PcbTextKind;
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
    private readonly GltfBuilder _builder;
    private readonly List<int> _rootChildren = [];
    private readonly Dictionary<int, int> _copperOrder = [];

    private double _cx, _cy;             // board centre (mm) subtracted from every point
    private int _matSubstrate, _matCopper, _matMask, _matSilk, _matBarrel, _matVcut;
    private List<IReadOnlyList<Vec2>> _boardHoles = []; // see-through board openings (cutouts / NPTH)
    // Placed sub-board pad drill holes (panel-centred mm). A panel's laminate is one continuous piece, so
    // each tiled sub-board's mounting/through holes must be cut from it here (the sub-board substrate itself
    // is not rendered); the drill barrels are instanced separately.
    private readonly List<IReadOnlyList<Vec2>> _placedBoardHoles = [];
    // Pad drill holes with their bounding box, for punching copper that runs over a barrel.
    private List<(IReadOnlyList<Vec2> Contour, double MinX, double MinY, double MaxX, double MaxY)> _drillBounds = [];
    // When set, feature meshes are collected here (for an embedded sub-board) instead of becoming
    // top-level nodes, so a panel can instance them at each array position.
    private List<(int Mesh, string Name, JsonObject Extras)>? _capture;

    // The document currently being READ, and a transform that maps its coordinates into the panel
    // (rotate about a reference, then translate) before centring. Used to gather a panel's placed
    // sub-board outlines and milling geometry; the layer builders themselves read the panel (identity).
    private PcbDocument _src;
    private double _trRefX, _trRefY, _trCos = 1.0, _trSin, _trOx, _trOy;
    // Routed slots (panel-centred mm): RouteToolPath/milling strokes that separate the array boards, cut
    // clean through the laminate and leaving the rout's tabs joining the boards to the panel.
    private readonly List<IReadOnlyList<Vec2>> _routs = [];
    // Board cut-outs (panel-centred mm): regions flagged ISBOARDCUTOUT — slots/windows fully inside a board.
    private readonly List<IReadOnlyList<Vec2>> _cutouts = [];
    // Placed sub-board outlines (panel-centred mm) — used by the solder-mask FRAME to leave each board's
    // mask to its own instanced layer, and to recognise (and discard) a rout boolean that spuriously rings a
    // whole board. The substrate does NOT subtract these: the boards are part of the same continuous
    // laminate, carved only by the routed slots, not by their rectangular outline.
    private readonly List<IReadOnlyList<Vec2>> _boardOutlines = [];
    // V-cut (scoring) lines (panel-centred mm): partial-depth grooves that do NOT cut through, so the
    // laminate stays continuous across them. Rendered as surface lines, never subtracted from the board.
    private readonly List<(Vec2 A, Vec2 B, double W)> _vcuts = [];
    // A TrueType PCB string's Height is the font CELL height, a bit larger than the point size (em); Altium
    // renders the em a touch smaller. Matched against the inverted-rect box Altium sizes to the text.
    private const double TtEmScale = 0.8;

    public GltfSceneBuilder(PcbDocument doc, GltfRenderSettings settings, GltfBuilder? sharedBuilder = null)
    {
        _doc = doc;
        _src = doc;
        _settings = settings;
        _builder = sharedBuilder ?? new GltfBuilder("OriginalCircuit.Altium.Rendering.Gltf");
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

        // A panel is one manufactured PCB: a single continuous laminate with the array boards routed out of
        // it (the slots that separate them, joined by tabs) and scored by V-cut lines. The substrate and
        // V-cuts are built once at panel scope; the panel's own copper/silk/tooling plus each sub-board's
        // copper/mask/silk/drills/components are instanced onto it (the sub-board's stack is tessellated
        // once and placed at every array cell). Instancing the thin layers keeps every triangulated polygon
        // simple — one merged 9-board mask polygon (hundreds of holes) overruns the ear-clip's robustness —
        // while the laminate, whose only holes are the routed slots, stays a single clean piece.
        bool composites = _settings.EmbeddedBoardResolver is not null && _doc.EmbeddedBoards.Count > 0;
        if (composites) CollectFramePlacements(); else CollectOwnCuts();

        PrepareBoardHoles(bounds);
        if (_settings.IncludeSubstrate) BuildSubstrate(bounds);
        if (_settings.IncludeSubstrate) BuildVCuts();
        if (_settings.IncludeCopper) BuildCopperLayers();
        if (_settings.IncludeSolderMask) BuildSolderMask(bounds);
        if (_settings.IncludeSilkscreen) BuildSilkscreen();
        if (_settings.IncludeDrills) BuildDrills();
        if (_settings.IncludeComponents) BuildComponents();
        if (composites) BuildEmbeddedBoards();

        int root = _builder.AddNode(
            name: "Board",
            matrix: StepCoordinates.ZUpMillimetresToYUpMetres(),
            children: _rootChildren.Count > 0 ? _rootChildren : null);
        _builder.AddScene([root]);
        return _builder.Build();
    }

    // The document exposes primitives through interfaces; the instances are the concrete Altium types,
    // so narrow to them for the concrete-only geometry/shape/layer properties this renderer needs.
    private IEnumerable<PcbTrack> Tracks => _src.Tracks.OfType<PcbTrack>();
    private IEnumerable<PcbArc> Arcs => _src.Arcs.OfType<PcbArc>();
    private IEnumerable<PcbFill> Fills => _src.Fills.OfType<PcbFill>();
    private IEnumerable<PcbRegion> Regions => _src.Regions.OfType<PcbRegion>();
    private IEnumerable<PcbPad> Pads => _src.Pads.OfType<PcbPad>();
    private IEnumerable<PcbVia> Vias => _src.Vias.OfType<PcbVia>();
    private IEnumerable<PcbText> Texts => _src.Texts.OfType<PcbText>();

    private void AddMaterials()
    {
        _matSubstrate = _builder.AddMaterial(GltfPalette.Substrate);
        _matCopper = _builder.AddMaterial(GltfPalette.Copper(_settings.CopperFinish, doubleSided: true));
        _matMask = _builder.AddMaterial(GltfPalette.SolderMask);
        _matSilk = _builder.AddMaterial(GltfPalette.Silkscreen);
        _matBarrel = _builder.AddMaterial(GltfPalette.Copper(_settings.CopperFinish, doubleSided: true));
        _matVcut = _builder.AddMaterial(GltfPalette.VCut);
    }

    // ── Substrate ───────────────────────────────────────────────────────────────────────────────
    private void BuildSubstrate((double minX, double minY, double maxX, double maxY) bounds)
    {
        var ring = OutlineRing(bounds);
        if (ring.Count < 3) return;

        var diel = _stack.Layers.Where(l => l.Kind == PcbStackupLayerKind.Dielectric).ToList();
        double z0 = diel.Count > 0 ? diel.Min(d => d.Z0Mm) : 0;
        double z1 = diel.Count > 0 ? diel.Max(d => d.Z1Mm) : _stack.TotalThicknessMm;

        // The laminate is the outline minus the drilled holes, the routed slots and the board cut-outs. For
        // a panel the slots are every placed sub-board's rout (collected in panel space): the boards are
        // carved out of one continuous laminate and joined by the rout's tabs — exactly as fabricated. The
        // sub-board outlines are NOT subtracted (the boards are not separate pieces); V-cut scoring is not
        // subtracted either (it does not cut through).
        //
        // Two passes: first resolve the rout strokes into clean, non-overlapping slot contours, discarding
        // any that spuriously ring a whole board (a board ringed by its rout but held by tabs must stay
        // solid — the union of the overlapping strokes can otherwise wind a closed loop around it). Then
        // subtract those slots together with the drills and cut-outs in one robust difference.
        var slots = ResolveRoutSlots(ring);
        var openings = new List<IReadOnlyList<Vec2>>(slots);
        openings.AddRange(_cutouts);
        openings.AddRange(_boardHoles);
        openings.AddRange(_placedBoardHoles); // tiled sub-board mounting / through holes

        var mesh = new MeshBuffer();
        if (openings.Count == 0)
            mesh.AddPrism(ring, null, z0, z1);
        else
            foreach (var (outer, holes) in SkiaPolyTools.Difference(ring, openings))
                mesh.AddPrism(outer, holes.Count > 0 ? holes.ConvertAll(h => (IReadOnlyList<Vec2>)h) : null, z0, z1);
        Emit(mesh, _matSubstrate, "Substrate", "substrate", null);
    }

    // Resolves the overlapping rout strokes into clean, non-overlapping slot contours by subtracting them
    // from the board outline and keeping the resulting holes — except any hole that encloses a placed
    // board's centre. The rout rings each board with only thin tabs joining it; the boolean union of those
    // strokes can wind a closed loop around a board, which would cut the whole board out. A real slot never
    // contains a board centre, so dropping such holes keeps the tabbed boards solid. (Plain boards have no
    // rout, so this returns empty and the laminate is just its outline.)
    private List<IReadOnlyList<Vec2>> ResolveRoutSlots(IReadOnlyList<Vec2> ring)
    {
        if (_routs.Count == 0) return [];

        var centres = _boardOutlines.ConvertAll(Centroid);
        var slots = new List<IReadOnlyList<Vec2>>();
        foreach (var (_, holes) in SkiaPolyTools.Difference(ring, _routs))
            foreach (var h in holes)
                if (!centres.Exists(c => PointInPolygon(h, c)))
                    slots.Add(h);
        return slots;
    }

    private static Vec2 Centroid(IReadOnlyList<Vec2> poly)
    {
        double a = 0, cx = 0, cy = 0;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            double cross = (poly[j].X * poly[i].Y) - (poly[i].X * poly[j].Y);
            a += cross;
            cx += (poly[j].X + poly[i].X) * cross;
            cy += (poly[j].Y + poly[i].Y) * cross;
        }
        if (Math.Abs(a) < 1e-9)
        {
            // Degenerate ring: fall back to the vertex average.
            double sx = 0, sy = 0;
            foreach (var p in poly) { sx += p.X; sy += p.Y; }
            return new Vec2(sx / poly.Count, sy / poly.Count);
        }
        return new Vec2(cx / (3 * a), cy / (3 * a));
    }

    private static bool PointInPolygon(IReadOnlyList<Vec2> poly, Vec2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
                (p.X < ((poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y)) + poly[i].X))
                inside = !inside;
        return inside;
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
                AddCopperSheet(mesh, Shapes.Capsule(P(t.Start), P(t.End), t.Width.ToMm(), Caps(t.Width.ToMm() / 2)), null, z);

        foreach (var a in Arcs)
            if (a.Layer == layerId && a.Radius.ToMm() > 0 && a.Width.ToMm() > 0)
                AddCopperSheet(mesh, ArcBand(a), null, z);

        foreach (var f in Fills)
            if (f.Layer == layerId)
                AddCopperSheet(mesh, FillRect(f), null, z);

        foreach (var r in Regions)
            if (r.Layer == layerId && r.Kind == 0 && !r.IsKeepout && r.Outline.Count >= 3)
                AddCopperSheet(mesh, Ring(r.Outline), RegionHoles(r), z); // pour clearances (around pads, fiducials, vias)

        foreach (var pad in Pads)
        {
            if (IsUnplatedHole(pad)) continue; // NPTH / mounting hole carries no copper
            var contour = PadContourForLayer(pad, layerId);
            if (contour is not null) mesh.AddSheet(contour, PadHole(pad), z);
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

        // Text etched in copper — board IDs, logos, and fab barcodes (Code 128 / QR / Data Matrix). Drawn a
        // hair above the plane so it does not z-fight a pour beneath; it reads as copper (and shows bright
        // where the solder mask is open over it, tinted where the mask covers it).
        double textZ = z + (layerId == 32 ? -0.012 : 0.012);
        foreach (var text in Texts)
            if (text.Layer == layerId && !string.IsNullOrEmpty(text.Text) && IsTextVisible(text))
                AddText(mesh, text, textZ, faceUp: layerId != 32);
    }

    // Adds a copper sheet (a track, arc, fill, or pour region) with any pad drill holes that fall
    // within it punched out, so a pour or a track running into a through-hole pad doesn't cover the
    // barrel. Pads/vias already cut their own holes. The common case (no drill overlaps the sheet)
    // takes the fast unchanged path; otherwise a robust boolean clips the drills (which can straddle
    // the sheet edge where a track meets a pad — earcut handles only fully-interior holes).
    private void AddCopperSheet(MeshBuffer mesh, IReadOnlyList<Vec2> outer, List<IReadOnlyList<Vec2>>? extraHoles, double z)
    {
        if (outer.Count < 3) return;

        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var p in outer) { if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X; if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y; }

        List<IReadOnlyList<Vec2>>? drills = null;
        foreach (var d in _drillBounds)
            if (d.MaxX >= minX && d.MinX <= maxX && d.MaxY >= minY && d.MinY <= maxY)
                (drills ??= []).Add(d.Contour);

        if (drills is null)
        {
            mesh.AddSheet(outer, extraHoles, z); // no drill overlaps this sheet — unchanged fast path
            return;
        }

        var holes = new List<IReadOnlyList<Vec2>>();
        if (extraHoles is not null) holes.AddRange(extraHoles);
        holes.AddRange(drills);
        foreach (var (o, h) in SkiaPolyTools.Difference(outer, holes))
            mesh.AddSheet(o, h.Count > 0 ? h.ConvertAll(x => (IReadOnlyList<Vec2>)x) : null, z);
    }

    // ── Solder mask (an INVERSE layer) ──────────────────────────────────────────────────────────
    // The mask is the board outline MINUS the union of its openings: non-tented pad/via copper grown
    // by the solder-mask expansion (manual override, From-Rule, or none) plus the negative geometry
    // drawn on the solder-mask layer (tracks/arcs/fills/regions, which REMOVE mask) plus the drilled
    // holes. The copper layer beneath (drawn in the finish colour) then shows through the openings
    // bright, while copper under the translucent mask reads tinted.
    private void BuildSolderMask((double minX, double minY, double maxX, double maxY) bounds)
    {
        var ring = OutlineRing(bounds);
        if (ring.Count < 3) return;

        var top = _stack.ForLayer(37);
        if (top is not null) BuildMaskSide(ring, top.CenterZMm, copperLayer: 1, solderLayer: 37, "SolderMask.Top");
        var bottom = _stack.ForLayer(38);
        if (bottom is not null) BuildMaskSide(ring, bottom.CenterZMm, copperLayer: 32, solderLayer: 38, "SolderMask.Bottom");
    }

    private void BuildMaskSide(IReadOnlyList<Vec2> ring, double z, int copperLayer, int solderLayer, string name)
    {
        var openings = CollectMaskOpenings(copperLayer, solderLayer);
        openings.AddRange(_boardHoles); // mask is open over drilled holes too
        openings.AddRange(_placedBoardHoles); // and over tiled sub-board mounting holes
        openings.AddRange(_routs);      // and removed where the board is routed away
        openings.AddRange(_cutouts);    // and over board cut-outs
        openings.AddRange(_boardOutlines); // and (panel frame) over each routed-out sub-board
        if (openings.Count == 0) return; // nothing to mask over (e.g. no copper on this side)

        var groups = SkiaPolyTools.Difference(ring, openings);
        if (groups.Count == 0) return;

        var mesh = new MeshBuffer();
        bool faceUp = solderLayer != 38; // top mask faces up, bottom faces down (single-sided is enough)
        foreach (var (outer, holes) in groups)
            mesh.AddFlatPolygon(outer, holes.Count > 0 ? holes.ConvertAll(h => (IReadOnlyList<Vec2>)h) : null, z, faceUp);
        Emit(mesh, _matMask, name, "soldermask", solderLayer);
    }

    // The mask openings for one side: non-tented pad/via copper grown by the solder-mask expansion, and
    // the negative geometry on the solder-mask layer (those features remove mask).
    private List<IReadOnlyList<Vec2>> CollectMaskOpenings(int copperLayer, int solderLayer)
    {
        bool top = copperLayer == 1;
        var openings = new List<IReadOnlyList<Vec2>>();
        double padRuleExp = ResolveMaskRuleExpansion(forVia: false);
        double viaRuleExp = ResolveMaskRuleExpansion(forVia: true);

        foreach (var pad in Pads)
        {
            if (pad.IsKeepout) continue;
            bool throughHole = pad.HoleSize.ToMm() > 0;
            if (!throughHole && pad.Layer != copperLayer) continue; // SMD on the other side
            if (top ? pad.IsTentingTop : pad.IsTentingBottom) continue;

            var size = top ? pad.SizeTop : pad.SizeBottom;
            var shape = top ? pad.ShapeTop : pad.ShapeBottom;
            double w = size.X.ToMm(), h = size.Y.ToMm();
            if (w <= 0 || h <= 0) continue;
            double exp = EffectiveMaskExpansion(pad.SolderMaskExpansionMode, pad.SolderMaskExpansion.ToMm(), padRuleExp);
            openings.Add(PadContour(P(pad.Location), w + (2 * exp), h + (2 * exp), pad.Rotation, shape, pad.CornerRadiusPercentage));
        }

        foreach (var via in Vias)
        {
            if (!ViaSpans(via, copperLayer)) continue;
            if (via.IsTented || (top ? via.IsTentingTop : via.IsTentingBottom)) continue;
            double r = via.Diameter.ToMm() / 2.0;
            if (r <= 0) continue;
            double exp = EffectiveMaskExpansion(via.SolderMaskExpansionMode, via.SolderMaskExpansion.ToMm(), viaRuleExp);
            openings.Add(Shapes.Circle(P(via.Location), r + exp, Seg(r + exp)));
        }

        // Geometry ON the solder-mask layer is negative: it removes mask wherever it is drawn.
        foreach (var t in Tracks)
            if (t.Layer == solderLayer && t.Width.ToMm() > 0)
                openings.Add(Shapes.Capsule(P(t.Start), P(t.End), t.Width.ToMm(), Caps(t.Width.ToMm() / 2)));
        foreach (var a in Arcs)
            if (a.Layer == solderLayer && a.Radius.ToMm() > 0 && a.Width.ToMm() > 0)
                openings.Add(ArcBand(a));
        foreach (var f in Fills)
            if (f.Layer == solderLayer)
                openings.Add(FillRect(f));
        foreach (var r in Regions)
            if (r.Layer == solderLayer && r.Outline.Count >= 3)
                openings.Add(Ring(r.Outline));

        // A 2-D barcode (Data Matrix / QR) on the solder-mask layer removes mask over its foreground,
        // revealing the copper/finish beneath (an inverted symbol leaves the dark modules masked).
        foreach (var t in Texts)
            if (t.Layer == solderLayer && t.TextKind == PcbTextKind.BarCode)
            {
                var barcode = PcbBarcodeGeometry.TryBuild(t);
                if (barcode is not null)
                    foreach (var quad in barcode.Foreground)
                        openings.Add(Array.ConvertAll(quad, P));
            }

        return openings;
    }

    // Resolves the effective solder-mask expansion (mm) for a pad/via: 0 = none, 2 = the object's manual
    // value, anything else (the Altium default, From-Rule) = the resolved design-rule expansion.
    private static double EffectiveMaskExpansion(int mode, double manualMm, double ruleMm) => mode switch
    {
        0 => 0.0,
        2 => manualMm,
        _ => ruleMm,
    };

    // Resolves the From-Rule solder-mask expansion (mm) from the board's SolderMaskExpansion design
    // rules: a via prefers a via-scoped rule; a pad uses the most general non-via rule.
    private double ResolveMaskRuleExpansion(bool forVia)
    {
        var rules = _src.Rules.OfType<PcbSolderMaskExpansionRule>().Where(r => r.Enabled).ToList();
        if (rules.Count == 0) return 0.05; // a sensible default when the board carries no rule

        static bool MentionsVia(PcbSolderMaskExpansionRule r) =>
            (r.Scope1Expression?.Contains("Via", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.Scope2Expression?.Contains("Via", StringComparison.OrdinalIgnoreCase) ?? false);

        PcbSolderMaskExpansionRule? pick = forVia ? rules.Where(MentionsVia).OrderBy(r => r.Priority).FirstOrDefault() : null;
        pick ??= rules.Where(r => !MentionsVia(r)).OrderBy(r => r.Priority).FirstOrDefault();
        pick ??= rules.OrderBy(r => r.Priority).FirstOrDefault();
        return pick?.Expansion.ToMm() ?? 0.05;
    }

    // Collects the see-through board openings — unplated (NPTH) mounting holes and board-cutout
    // regions — that are subtracted from the substrate and solder mask. Plated via/pad holes are NOT
    // included: their copper barrels fill them. Board-sized cutout traps are filtered out by area.
    private List<IReadOnlyList<Vec2>> CollectBoardHoles(IReadOnlyList<Vec2> outline)
    {
        var holes = new List<IReadOnlyList<Vec2>>();
        if (outline.Count < 3) return holes;

        // Drilled PAD holes punch through the board (mounting holes + through-hole component pads);
        // plated holes keep a copper barrel from BuildDrills. Vias are NOT cut: they are tiny, usually
        // tented (so the mask must stay over them), and cutting hundreds of them produces triangulation
        // slivers. Kind==1 regions on a copper layer are copper anti-fills (clearances), NOT board
        // cut-outs, so they are not subtracted from the substrate either.
        foreach (var pad in Pads)
        {
            var hole = PadHole(pad);
            if (hole is not null) holes.AddRange(hole);
        }
        return holes;
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
        foreach (var text in Texts)
            if (text.Layer == layerId && !string.IsNullOrEmpty(text.Text) && IsTextVisible(text))
                AddText(mesh, text, z, faceUp: layerId != 34);
        Emit(mesh, _matSilk, name, "silkscreen", layerId);
    }

    // A component's designator/comment text shows only when the component enables that field
    // (Altium's NameOn/CommentOn); free text always shows.
    private bool IsTextVisible(PcbText text)
    {
        if (text.ComponentIndex < 0 || text.ComponentIndex >= _src.Components.Count) return true;
        if (_src.Components[text.ComponentIndex] is not PcbComponent owner) return true;
        if (text.IsComment && !owner.CommentOn) return false;
        if (text.IsDesignator && !owner.NameOn) return false;
        return true;
    }

    // Dispatches a PCB text to the right geometry path. A text is a 2-D barcode only when its
    // TextKind is BarCode (the BarCodeKind byte merely picks the symbology and is meaningless for
    // plain text); otherwise it is inverted (negative) text, stroke-font text, or TrueType text.
    private void AddText(MeshBuffer mesh, PcbText text, double z, bool faceUp)
    {
        bool framed = (text.UseInvertedRectangle || text.IsFrame)
                      && text.InvertedRectWidth > Coord.Zero && text.InvertedRectHeight > Coord.Zero;
        if (text.TextKind == PcbTextKind.BarCode)
            AddBarcode(mesh, text, z, faceUp);
        else if (framed)
            AddFramedText(mesh, text, z, faceUp); // text justified WITHIN its frame box (Location = bottom-left)
        else if (text.TextKind == PcbTextKind.Stroke && !text.IsTrueType)
            AddStrokeText(mesh, text, z, faceUp);
        else
            AddTrueTypeText(mesh, text, z, faceUp);
    }

    // Renders a 2-D barcode (Data Matrix / QR) on an ink layer (silk/copper) as its filled foreground
    // geometry. The symbol is re-encoded from the text on demand (Altium never stores the module
    // pattern). On the solder-mask layer the modules are handled as openings (CollectMaskOpenings).
    private void AddBarcode(MeshBuffer mesh, PcbText text, double z, bool faceUp)
    {
        var barcode = PcbBarcodeGeometry.TryBuild(text);
        if (barcode is null) return;
        foreach (var quad in barcode.Foreground)
            mesh.AddFlatPolygon(Array.ConvertAll(quad, P), null, z, faceUp);
    }

    // Renders TrueType text as filled glyph geometry (its real font shapes) at the silk plane.
    private void AddTrueTypeText(MeshBuffer mesh, PcbText text, double z, bool faceUp)
    {
        double H = text.Height.ToMm();
        if (H <= 0) return;
        double em = H * TtEmScale; // Altium sizes a TrueType string so its font cell ≈ Height; the em is smaller
        var lines = text.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        int n = lines.Length;
        double lineH = em * 1.2, cap = 0.715 * em;

        // Non-framed free strings are positioned by their legacy Location anchor (Altium does NOT apply the
        // inverted-rect justification to them — that only drives FRAMED text). The glyphs are mirrored about
        // the anchor below, which by itself flips the reading direction, so the alignment is NOT swapped.
        var (ha, va) = LegacyJustification(text);
        double radTt = text.Rotation * Math.PI / 180.0;
        double cosTt = Math.Cos(radTt), sinTt = Math.Sin(radTt);
        var locTt = P(text.Location);

        double offY0 = va == 0 ? -cap : va == 1 ? -cap / 2.0 : 0.0;             // Top / Middle / Bottom anchor
        double blockShift = va == 0 ? 0.0 : va == 1 ? (n - 1) / 2.0 * lineH : (n - 1) * lineH;

        bool any = false;
        for (int li = 0; li < n; li++)
        {
            var glyphs = GltfTrueTypeText.Layout(lines[li], text.FontName, text.FontBold, text.FontItalic, em, out double advance);
            if (glyphs.Count == 0) continue;

            double offX = ha == 2 ? -advance : ha == 0 ? 0.0 : -advance / 2.0;  // Right / Left / Center
            double baselineY = offY0 - (li * lineH) + blockShift;

            Vec2 Map(Vec2 g)
            {
                double gx = g.X + offX, gy = g.Y + baselineY;
                if (text.IsMirrored) gx = -gx;
                return new Vec2(locTt.X + (gx * cosTt) - (gy * sinTt), locTt.Y + (gx * sinTt) + (gy * cosTt));
            }

            foreach (var glyph in glyphs)
            {
                var outer = glyph.Outer.ConvertAll(Map);
                List<IReadOnlyList<Vec2>>? holes = glyph.Holes.Count > 0
                    ? glyph.Holes.ConvertAll(hh => (IReadOnlyList<Vec2>)hh.ConvertAll(Map))
                    : null;
                mesh.AddFlatPolygon(outer, holes, z, faceUp);
                any = true;
            }
        }

        // If the named font yielded no geometry (font unavailable / unsupported glyphs), fall back to the
        // stroke font so the text still appears rather than silently vanishing.
        if (!any) AddStrokeText(mesh, text, z, faceUp);
    }

    // Renders FRAMED text: the glyphs are laid out and justified WITHIN the text's frame box (whose
    // bottom-left corner is the Location), not anchored at a point. When the frame is INVERTED the box is
    // filled and the glyphs are knocked out of it (negative text, the board shows through the letters);
    // otherwise the glyphs are simply drawn — this is Altium's multi-line "Frame" text mode (e.g. a centred
    // title block sitting to one side of a line because it is centred in its wide frame, not on its anchor).
    private void AddFramedText(MeshBuffer mesh, PcbText text, double z, bool faceUp)
    {
        double w = text.InvertedRectWidth.ToMm(), h = text.InvertedRectHeight.ToMm();
        double rad = text.Rotation * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        var loc = P(text.Location);

        // Local frame space: x right (0..w), y up (0..h), bottom-left at the location; mirrored text flips x.
        Vec2 L(double lx, double ly)
        {
            if (text.IsMirrored) lx = w - lx;
            return new Vec2(loc.X + (lx * cos) - (ly * sin), loc.Y + (lx * sin) + (ly * cos));
        }

        // Lay each glyph out within the frame per the frame justification.
        var glyphRegions = new List<(List<Vec2> Outer, List<List<Vec2>> Holes)>();
        if (text.IsTrueType || text.TextKind == PcbTextKind.TrueType)
        {
            var lines = text.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int n = lines.Length;
            // The box is sized by Altium to bound the text, so fill most of it: the em is the Height-derived
            // size (TtEmScale), clamped to the box height so multi-line / tight boxes still fit.
            double margin = Math.Min(w, h) * 0.07;
            double glyphH = Math.Min(text.Height.ToMm() * TtEmScale, Math.Max(0.1, ((h - (2 * margin)) / n) * 0.92));
            double lineH = glyphH * 1.2;
            double blockH = lineH * n;
            (int ha, int va) = MapInvertedJustification(text.InvertedRectJustification);
            double blockTop = va == 0 ? h - margin : va == 2 ? margin + blockH : (h + blockH) / 2.0; // top edge of the text block

            for (int li = 0; li < n; li++)
            {
                var glyphs = GltfTrueTypeText.Layout(lines[li], text.FontName, text.FontBold, text.FontItalic, glyphH, out double advance);
                double offX = ha == 2 ? w - margin - advance : ha == 0 ? margin : (w - advance) / 2.0;
                double baselineY = blockTop - (lineH * (li + 1)) + ((lineH - glyphH) / 2.0);
                foreach (var glyph in glyphs)
                    glyphRegions.Add((
                        glyph.Outer.ConvertAll(p => L(p.X + offX, p.Y + baselineY)),
                        glyph.Holes.ConvertAll(bowl => bowl.ConvertAll(p => L(p.X + offX, p.Y + baselineY)))));
            }
        }

        if (text.UseInvertedRectangle)
        {
            // Inverted: fill the box and knock the glyphs out of it. Preserve input winding so each glyph's
            // outer (CCW) and counters/bowls (CW) net winding 0 inside the counter, keeping it filled.
            var rect = new List<Vec2> { L(0, 0), L(w, 0), L(w, h), L(0, h) };
            var knockouts = new List<IReadOnlyList<Vec2>>();
            foreach (var (outer, holes) in glyphRegions) { knockouts.Add(outer); knockouts.AddRange(holes); }
            foreach (var (outer, holes) in SkiaPolyTools.Difference(rect, knockouts, normalizeWinding: false))
                mesh.AddFlatPolygon(outer, holes.Count > 0 ? holes.ConvertAll(x => (IReadOnlyList<Vec2>)x) : null, z, faceUp);
        }
        else
        {
            // Plain frame: just draw the glyphs (filled, with their counters as holes).
            foreach (var (outer, holes) in glyphRegions)
                mesh.AddFlatPolygon(outer, holes.Count > 0 ? holes.ConvertAll(x => (IReadOnlyList<Vec2>)x) : null, z, faceUp);
        }
    }

    // Altium inverted-rect justification is column-major 1..9 (Manual=0). Returns (h: 0=Left,1=Center,2=Right;
    // v: 0=Top,1=Middle,2=Bottom).
    private static (int H, int V) MapInvertedJustification(OriginalCircuit.Altium.Models.Pcb.PcbTextJustification j)
    {
        int v = (int)j;
        if (v is < 1 or > 9) return (1, 1);
        return ((v - 1) / 3, (v - 1) % 3);
    }

    // The legacy Location justification of a PCB text (the anchor point's meaning). For non-framed free
    // strings this — NOT the inverted-rect justification — is what Altium positions by; the inverted-rect
    // justification only aligns text WITHIN a frame. Returns (h: 0=Left,1=Center,2=Right; v: 0=Top,1=Middle,
    // 2=Bottom).
    private static (int H, int V) LegacyJustification(PcbText text)
    {
        string s = text.Justification.ToString();
        int h = s.Contains("Right") ? 2 : s.Contains("Left") ? 0 : 1;
        int v = s.Contains("Top") ? 0 : s.Contains("Bottom") ? 2 : 1;
        return (h, v);
    }

    // Builds the stroke geometry for a PCB text using Altium's stroke font: normalized glyph segments
    // (height 1, baseline-left) scaled to the text height, rotated, mirrored, and anchored at the text
    // location, with each stroke drawn as a thin single-sided rectangle.
    private void AddStrokeText(MeshBuffer mesh, PcbText text, double z, bool faceUp)
    {
        double h = text.Height.ToMm();
        if (h <= 0) return;
        double sw = text.StrokeWidth.ToMm();
        if (sw <= 0) sw = Math.Max(0.04, h * 0.1);

        var segments = AltiumStrokeFont.Layout(text.Text, AltiumStrokeFont.FromStrokeFont(text.StrokeFont), out float advance);
        if (segments.Count == 0) return;

        // Anchor per the legacy Location justification (the inverted-rect justification drives only FRAMED
        // text). Glyph space is normalized (height 1, baseline y=0, +x right, +y up); mirrored text is
        // reflected about the anchor below, so the alignment is not swapped.
        var (ha, va) = LegacyJustification(text);
        double offX = ha == 2 ? -advance : ha == 0 ? 0.0 : -advance / 2.0;
        double offY = va == 0 ? -1.0 : va == 2 ? 0.0 : -0.5;

        double rad = text.Rotation * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        var loc = P(text.Location);
        double half = sw / 2.0;

        Vec2 Map(float nx, float ny)
        {
            double gx = (nx + offX) * h, gy = (ny + offY) * h;
            if (text.IsMirrored) gx = -gx; // bottom-side / mirrored text
            return new Vec2(loc.X + (gx * cos) - (gy * sin), loc.Y + (gx * sin) + (gy * cos));
        }

        // Each glyph stroke is a thin single-sided rectangle (cheaper than a round-capped capsule).
        foreach (var s in segments)
        {
            var a = Map(s.X1, s.Y1);
            var b = Map(s.X2, s.Y2);
            var d = b - a;
            if (d.Length < 1e-9) continue;
            var perp = new Vec2(-d.Y, d.X).Normalized() * half;
            mesh.AddFlatPolygon([a + perp, b + perp, b - perp, a - perp], null, z, faceUp);
        }
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
            if (r > 0) mesh.AddCylinderWall(P(pad.Location), r, zBot, zTop, SegBarrel(r));
        }
        foreach (var via in Vias)
        {
            double r = via.HoleSize.ToMm() / 2.0;
            if (r <= 0) continue;
            double zt = _stack.ForLayer(via.StartLayer)?.Z1Mm ?? zTop;
            double zb = _stack.ForLayer(via.EndLayer)?.Z0Mm ?? zBot;
            mesh.AddCylinderWall(P(via.Location), r, zb, zt, SegBarrel(r));
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

    private void PrepareBoardHoles((double minX, double minY, double maxX, double maxY) bounds)
    {
        _boardHoles = CollectBoardHoles(OutlineRing(bounds));
        // Copper on the panel's own layers must also avoid the tiled sub-board barrels, so the placed holes
        // are included in the drill-bounds used by AddCopperSheet to punch copper that runs over a drill.
        var drilled = new List<IReadOnlyList<Vec2>>(_boardHoles);
        drilled.AddRange(_placedBoardHoles);
        _drillBounds = drilled.ConvertAll(h =>
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in h) { if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X; if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y; }
            return (h, minX, minY, maxX, maxY);
        });
    }

    // ── Milled cut-outs ─────────────────────────────────────────────────────────────────────────
    // The board's own milled cut-outs: RouteToolPath/milling geometry and board-cutout regions, in
    // panel-centred mm. For a plain board (or an embedded sub-board rendered standalone) this is its own
    // routing/cutouts; a panel collects each placed sub-board's instead (CollectFramePlacements).
    private void CollectOwnCuts()
    {
        _routs.Clear();
        _cutouts.Clear();
        _boardOutlines.Clear();
        _vcuts.Clear();
        _placedBoardHoles.Clear();
        CollectBoardCutouts(_cutouts); // only INTERNAL cut-outs — the surrounding rout is the frame's concern
        CollectVCuts(_vcuts);
    }

    // For a panel: each placed sub-board's outline and milled channel, in panel-centred mm, so the frame
    // substrate/mask route them out (the boards are instanced separately). The boards-in-a-frame shape
    // is the rout's tabs, which keep the boards joined to the panel.
    private void CollectFramePlacements()
    {
        _routs.Clear();
        _cutouts.Clear();
        _boardOutlines.Clear();
        _vcuts.Clear();
        _placedBoardHoles.Clear();
        // The panel's own routed slots / cut-outs / V-cut scoring (in panel space, identity transform).
        CollectMilling(_routs);
        CollectBoardCutouts(_cutouts);
        CollectVCuts(_vcuts);

        var resolve = _settings.EmbeddedBoardResolver;
        if (resolve is null) { ResetSource(); return; }

        foreach (var emb in _doc.EmbeddedBoards)
        {
            if (string.IsNullOrEmpty(emb.DocumentPath)) continue;
            PcbDocument? sub;
            try { sub = resolve(emb.DocumentPath); } catch { sub = null; }
            if (sub is null) continue;
            var outline = sub.GetBoardOutline();
            if (outline is not { Count: >= 3 }) continue;

            double refX = double.MaxValue, refY = double.MaxValue;
            foreach (var p in outline) { refX = Math.Min(refX, p.X.ToMm()); refY = Math.Min(refY, p.Y.ToMm()); }
            double a = emb.Rotation * Math.PI / 180.0;
            double cos = Math.Cos(a), sin = Math.Sin(a);
            int rows = Math.Max(1, emb.RowCount), cols = Math.Max(1, emb.ColCount);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    double ox = emb.X.ToMm() + (emb.ColSpacing.ToMm() * c);
                    double oy = emb.Y.ToMm() + (emb.RowSpacing.ToMm() * r);
                    SetSource(sub, refX, refY, cos, sin, ox, oy);
                    var mappedOutline = outline.Select(P).ToList();
                    _boardOutlines.Add(mappedOutline);
                    CollectMilling(_routs);     // the rout that carves this board out of the laminate
                    CollectBoardCutouts(_cutouts); // and any internal slots/holes in the sub-board
                    CollectVCuts(_vcuts);       // and the sub-board's own scoring, if any
                    _placedBoardHoles.AddRange(CollectBoardHoles(mappedOutline)); // its drilled/mounting holes
                }
        }
        ResetSource();
    }

    // Milling / routing geometry (RouteToolPath etc.) on the current source — the rout that separates the
    // array boards. Routed clean through the laminate at its real tool width, so the slots read as
    // see-through gaps with the rout's tabs left joining the boards to the panel.
    private void CollectMilling(List<IReadOnlyList<Vec2>> cuts)
    {
        var milling = MillingLayers(_src);
        if (milling.Count == 0) return;
        foreach (var t in Tracks)
            if (milling.Contains(t.Layer) && t.Width.ToMm() > 0)
                cuts.Add(Shapes.Capsule(P(t.Start), P(t.End), t.Width.ToMm(), Caps(t.Width.ToMm() / 2)));
        foreach (var a in Arcs)
            if (milling.Contains(a.Layer) && a.Radius.ToMm() > 0 && a.Width.ToMm() > 0)
            {
                double r = a.Radius.ToMm();
                double sweep = a.EndAngle - a.StartAngle;
                if (sweep <= 0) sweep += 360;
                int segs = Math.Max(3, (int)Math.Ceiling(SegArc(r) * sweep / 360.0));
                cuts.Add(Shapes.ArcBand(P(a.Center), r, a.StartAngle, a.EndAngle, a.Width.ToMm(), segs));
            }
        foreach (var f in Fills)
            if (milling.Contains(f.Layer))
                cuts.Add(FillRect(f));
        foreach (var r in Regions)
            if (milling.Contains(r.Layer) && r.Outline.Count >= 3)
                cuts.Add(Ring(r.Outline));
    }

    // V-cut (scoring) lines on the current source: tracks on a mechanical layer whose kind is VCut. These
    // are partial-depth grooves — the laminate is continuous across them — so they are collected as line
    // segments to draw on the surface, never subtracted from the board.
    private void CollectVCuts(List<(Vec2 A, Vec2 B, double W)> vcuts)
    {
        var layers = VCutLayers(_src);
        if (layers.Count == 0) return;
        foreach (var t in Tracks)
            if (layers.Contains(t.Layer))
                vcuts.Add((P(t.Start), P(t.End), Math.Max(0.4, t.Width.ToMm())));
    }

    // Internal board cut-outs (regions flagged ISBOARDCUTOUT) on the current source — slots/holes within
    // the board outline. These belong to the board itself (whether plain or tiled in a panel).
    private void CollectBoardCutouts(List<IReadOnlyList<Vec2>> cuts)
    {
        foreach (var r in Regions)
            if (r.IsBoardCutout == true && r.Outline.Count >= 3)
                cuts.Add(Ring(r.Outline));
    }

    // Mechanical layer ids designated for milling/routing — Board6 "LAYER{id}MECHKIND = RouteToolPath"
    // (or a Rout/Mill kind); geometry on them cuts through the board. Mirrors PcbRealisticRenderer.
    private static HashSet<int> MillingLayers(PcbDocument doc)
    {
        var layers = new HashSet<int>();
        if (doc.BoardParameters is not { } bp) return layers;
        foreach (var (key, value) in bp)
        {
            if (!key.EndsWith("MECHKIND", StringComparison.OrdinalIgnoreCase)) continue;
            if (!(value.Contains("Rout", StringComparison.OrdinalIgnoreCase) ||
                  value.Contains("Mill", StringComparison.OrdinalIgnoreCase))) continue;
            if (!key.StartsWith("LAYER", StringComparison.OrdinalIgnoreCase)) continue;
            var mid = key.Substring(5, key.Length - 5 - "MECHKIND".Length);
            if (int.TryParse(mid, out var id)) layers.Add(id);
        }
        return layers;
    }

    // Mechanical layer ids designated for V-cut scoring — Board6 "LAYER{id}MECHKIND = VCut".
    private static HashSet<int> VCutLayers(PcbDocument doc)
    {
        var layers = new HashSet<int>();
        if (doc.BoardParameters is not { } bp) return layers;
        foreach (var (key, value) in bp)
        {
            if (!key.EndsWith("MECHKIND", StringComparison.OrdinalIgnoreCase)) continue;
            if (!value.Contains("VCut", StringComparison.OrdinalIgnoreCase) &&
                !value.Contains("V-Cut", StringComparison.OrdinalIgnoreCase)) continue;
            if (!key.StartsWith("LAYER", StringComparison.OrdinalIgnoreCase)) continue;
            var mid = key.Substring(5, key.Length - 5 - "MECHKIND".Length);
            if (int.TryParse(mid, out var id)) layers.Add(id);
        }
        return layers;
    }

    // Draws the V-cut scoring as thin dark grooves on the laminate's top and bottom faces. The cuts do not
    // pierce the board, so they are surface lines clipped to the board outline (a V-cut runs panel
    // edge-to-edge; clipping keeps it off the routed slots and outside the board).
    private void BuildVCuts()
    {
        if (_vcuts.Count == 0) return;
        var ring = OutlineRing(ComputeBoundsMm());
        if (ring.Count < 3) return;

        var diel = _stack.Layers.Where(l => l.Kind == PcbStackupLayerKind.Dielectric).ToList();
        double zTop = (diel.Count > 0 ? diel.Max(d => d.Z1Mm) : _stack.TotalThicknessMm) + 0.005;
        double zBot = (diel.Count > 0 ? diel.Min(d => d.Z0Mm) : 0) - 0.005;

        var mesh = new MeshBuffer();
        foreach (var (a, b, w) in _vcuts)
        {
            var strip = Shapes.Capsule(a, b, w, 4);
            // Clip the groove to the laminate (it runs panel edge-to-edge) and drop the parts that cross an
            // open routed slot or cut-out (no material there to score).
            var openSlots = new List<IReadOnlyList<Vec2>>(_routs);
            openSlots.AddRange(_cutouts);
            foreach (var (clipped, _) in SkiaPolyTools.Intersect(strip, ring))
                foreach (var (outer, holes) in SkiaPolyTools.Difference(clipped, openSlots))
                {
                    var h = holes.Count > 0 ? holes.ConvertAll(x => (IReadOnlyList<Vec2>)x) : null;
                    mesh.AddFlatPolygon(outer, h, zTop, faceUp: true);
                    mesh.AddFlatPolygon(outer, h, zBot, faceUp: false);
                }
        }
        Emit(mesh, _matVcut, "VCut", "vcut", null);
    }

    // ── Embedded boards (panel arrays) ──────────────────────────────────────────────────────────
    // Each EmbeddedBoards6 object is a sub-board (resolved through the settings' resolver) tiled in a
    // rows×cols grid. Its outline-min corner aligns to the array origin, with one step of ColSpacing /
    // RowSpacing between instances. The sub-board's full stack is tessellated ONCE (centred on that
    // corner) into shared meshes; each grid cell is then a lightweight node instance referencing them
    // under a transl(/rotate) transform — so a populated 3×3 panel costs one board's worth of geometry.
    private void BuildEmbeddedBoards()
    {
        var resolve = _settings.EmbeddedBoardResolver;
        if (resolve is null) return;

        foreach (var emb in _doc.EmbeddedBoards)
        {
            if (string.IsNullOrEmpty(emb.DocumentPath)) continue;

            PcbDocument? sub;
            try { sub = resolve(emb.DocumentPath); } catch { sub = null; }
            if (sub is null) continue;

            var outline = sub.GetBoardOutline();
            if (outline is null || outline.Count < 3) continue;
            double refX = double.MaxValue, refY = double.MaxValue;
            foreach (var p in outline) { refX = Math.Min(refX, p.X.ToMm()); refY = Math.Min(refY, p.Y.ToMm()); }

            // Tessellate the sub-board's features (and components, each model once) into shared meshes.
            // A sub-board does not itself composite further — keep the run one level deep.
            var subSettings = _settings.Clone();
            subSettings.EmbeddedBoardResolver = null;
            var feats = new GltfSceneBuilder(sub, subSettings, _builder).CaptureFeatureMeshes(refX, refY);
            if (feats.Count == 0) continue;

            int rows = Math.Max(1, emb.RowCount), cols = Math.Max(1, emb.ColCount);
            var instances = new List<int>(rows * cols);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    double tx = emb.X.ToMm() + (emb.ColSpacing.ToMm() * c) - _cx;
                    double ty = emb.Y.ToMm() + (emb.RowSpacing.ToMm() * r) - _cy;
                    var cells = feats.ConvertAll(f => _builder.AddNode(mesh: f.Mesh, name: f.Name, extras: f.Extras));
                    instances.Add(_builder.AddNode(matrix: InstanceMatrix(tx, ty, emb.Rotation), name: $"r{r}c{c}", children: cells));
                }

            string label = System.IO.Path.GetFileNameWithoutExtension(emb.DocumentPath);
            _rootChildren.Add(_builder.AddNode(name: $"EmbeddedBoard.{label}", children: instances));
        }
    }

    // Builds this (embedded) document's board features and component bodies into the shared builder,
    // centred on (centreX, centreY) mm, and returns the shared mesh + name + extras for each so a panel
    // can instance them at every array position. No root node, no scene — just reusable meshes.
    public List<(int Mesh, string Name, JsonObject Extras)> CaptureFeatureMeshes(double centreX, double centreY)
    {
        _cx = centreX;
        _cy = centreY;
        _capture = [];

        CollectOwnCuts(); // the sub-board's internal cut-outs (its surrounding rout is outside its outline)
        var bounds = ComputeBoundsMm();
        AddMaterials();
        PrepareBoardHoles(bounds);

        // No substrate here: a panelised sub-board is part of the panel's one continuous laminate, which is
        // built once at panel scope. The sub-board contributes only its thin layers and components.
        if (_settings.IncludeCopper) BuildCopperLayers();
        if (_settings.IncludeSolderMask) BuildSolderMask(bounds);
        if (_settings.IncludeSilkscreen) BuildSilkscreen();
        if (_settings.IncludeDrills) BuildDrills();
        if (_settings.IncludeComponents)
        {
            // Each STEP model is tessellated once here and the resulting meshes are shared by every
            // array instance, so the component bodies cost no more than a single populated board.
            var placer = new GltfComponentPlacer(_doc, _settings, _stack, _builder, _cx, _cy);
            _capture.AddRange(placer.BuildMeshes());
        }

        return _capture;
    }

    // A glTF (column-major) node matrix that rotates by rotDeg about Z then translates by (tx,ty),
    // in the board-mm Z-up space the root node converts to Y-up metres.
    private static double[] InstanceMatrix(double tx, double ty, double rotDeg)
    {
        double a = rotDeg * Math.PI / 180.0;
        double cos = Math.Cos(a), sin = Math.Sin(a);
        return
        [
            cos, sin, 0, 0,
            -sin, cos, 0, 0,
            0, 0, 1, 0,
            tx, ty, 0, 1,
        ];
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────────
    private void Emit(MeshBuffer mesh, int material, string name, string role, int? altiumLayer)
    {
        if (mesh.IsEmpty) return;
        int meshIndex = _builder.AddMesh(mesh.Positions, mesh.Normals, mesh.Indices,
            [new MeshPartSpec(0, mesh.Indices.Count, material)], name);
        var extras = Extras(role, altiumLayer);
        extras["group"] = name; // stable toggle key (a viewer can't rely on node names: glTF loaders uniquify duplicates across instances)
        if (_capture is not null)
            _capture.Add((meshIndex, name, extras)); // embedded sub-board: collect for instancing, no node yet
        else
            _rootChildren.Add(_builder.AddNode(mesh: meshIndex, name: name, extras: extras));
    }

    private static JsonObject Extras(string role, int? altiumLayer)
    {
        var extras = new JsonObject { ["role"] = role };
        if (altiumLayer is int al) extras["altiumLayer"] = al;
        return extras;
    }

    private Vec2 P(CoordPoint p)
    {
        double px = p.X.ToMm() - _trRefX, py = p.Y.ToMm() - _trRefY;
        double mx = _trOx + (_trCos * px) - (_trSin * py);
        double my = _trOy + (_trSin * px) + (_trCos * py);
        return new(mx - _cx, my - _cy);
    }

    // Activates a placed sub-board source: the accessors read s.Doc, and P() maps its coordinates into
    // the panel (rotate about the outline-min reference, then translate to the array cell), then centres.
    private void SetSource(PcbDocument doc, double refX, double refY, double cos, double sin, double ox, double oy)
    {
        _src = doc; _trRefX = refX; _trRefY = refY; _trCos = cos; _trSin = sin; _trOx = ox; _trOy = oy;
    }

    private void ResetSource()
    {
        _src = _doc; _trRefX = 0; _trRefY = 0; _trCos = 1; _trSin = 0; _trOx = 0; _trOy = 0;
    }

    private List<Vec2> Ring(IReadOnlyList<CoordPoint> pts)
    {
        var ring = new List<Vec2>(pts.Count);
        foreach (var p in pts) ring.Add(P(p));
        return ring;
    }

    // A copper region's internal clearance holes (the pour pulled back around pads/fiducials/vias).
    private List<IReadOnlyList<Vec2>>? RegionHoles(PcbRegion region)
    {
        if (region.Holes is not { Count: > 0 }) return null;
        var holes = new List<IReadOnlyList<Vec2>>(region.Holes.Count);
        foreach (var h in region.Holes)
            if (h.Count >= 3) holes.Add(Ring(h));
        return holes.Count > 0 ? holes : null;
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
        int segs = Math.Max(3, (int)Math.Ceiling(SegArc(r) * sweep / 360.0));
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
        return PadContour(P(pad.Location), w, h, pad.Rotation, shape, pad.CornerRadiusPercentage);
    }

    private List<Vec2> PadContour(Vec2 center, double w, double h, double rotationDeg, PadShape shape, int cornerPercent)
    {
        switch (shape)
        {
            case PadShape.Round:
                if (Math.Abs(w - h) < 1e-6) return Shapes.Circle(center, w / 2.0, Seg(w / 2.0));
                // Oblong: a stadium along the major axis.
                double rad = rotationDeg * Math.PI / 180.0;
                var ax = new Vec2(Math.Cos(rad), Math.Sin(rad));
                if (w >= h) { double half = (w - h) / 2.0; return Shapes.Capsule(center - (ax * half), center + (ax * half), h, Caps(h / 2)); }
                var ay = new Vec2(-Math.Sin(rad), Math.Cos(rad));
                double half2 = (h - w) / 2.0;
                return Shapes.Capsule(center - (ay * half2), center + (ay * half2), w, Caps(w / 2));

            case PadShape.Octagonal:
                return Shapes.Octagon(center, w, h, rotationDeg);

            case PadShape.RoundedRectangle:
                return Shapes.RoundedRectangle(center, w, h, rotationDeg, cornerPercent, Math.Max(2, Caps(Math.Min(w, h) / 2.0) / 2));

            default:
                return Shapes.Rectangle(center, w, h, rotationDeg);
        }
    }

    // A pad that is an unplated through-hole (a mounting / tooling hole) — it carries no copper and
    // is subtracted from the board as a see-through opening instead.
    private static bool IsUnplatedHole(PcbPad pad) => pad.HoleSize.ToMm() > 0 && !pad.IsPlated;

    // The pad's drill as a hole ring to subtract from its copper (so a through-hole reads as an
    // annulus with the plated barrel / open hole showing through), or null for a holeless SMD pad.
    private List<IReadOnlyList<Vec2>>? PadHole(PcbPad pad)
    {
        double r = pad.HoleSize.ToMm() / 2.0;
        if (r <= 0) return null;
        var c = P(pad.Location);
        if (pad.HoleType == PadHoleType.Slot && pad.HoleSlotLength > 0)
        {
            double len = Coord.FromRaw(pad.HoleSlotLength).ToMm();
            double rad = pad.HoleRotation * Math.PI / 180.0;
            var ax = new Vec2(Math.Cos(rad), Math.Sin(rad));
            double half = Math.Max(0, (len / 2.0) - r);
            return [Shapes.Capsule(c - (ax * half), c + (ax * half), pad.HoleSize.ToMm(), Caps(r))];
        }
        return [Shapes.Circle(c, r, Seg(r))];
    }

    private bool ViaSpans(PcbVia via, int layerId)
    {
        if (!_copperOrder.TryGetValue(layerId, out int li)) return false;
        int s = _copperOrder.GetValueOrDefault(via.StartLayer, 0);
        int e = _copperOrder.GetValueOrDefault(via.EndLayer, _copperOrder.Count - 1);
        return li >= Math.Min(s, e) && li <= Math.Max(s, e);
    }

    private int Seg(double radiusMm) => Shapes.SegmentCount(radiusMm, _settings.ArcChordToleranceMm);
    private int Caps(double radiusMm) => Math.Max(4, Seg(radiusMm) / 2);

    // Visible arcs (silk rings, copper arcs) get a finer chord tolerance and a higher floor so small
    // full circles read as smooth rings rather than polygons; small pads/holes keep the coarser Seg.
    private int SegArc(double radiusMm) => Shapes.SegmentCount(radiusMm, Math.Min(_settings.ArcChordToleranceMm, 0.01), min: 40);

    // Via/pad drill barrels are numerous and tiny, so they use a coarser segment count than the
    // smooth-circle minimum used for silk rings and exposed pads.
    private int SegBarrel(double radiusMm) => Math.Clamp(Shapes.SegmentCount(radiusMm, _settings.ArcChordToleranceMm) / 3, 12, 32);

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
