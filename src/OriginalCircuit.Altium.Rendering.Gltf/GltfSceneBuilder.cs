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
    private int _matSubstrate, _matCopper, _matMask, _matSilk, _matBarrel;
    private List<IReadOnlyList<Vec2>> _boardHoles = []; // see-through board openings (cutouts / NPTH)
    // Pad drill holes with their bounding box, for punching copper that runs over a barrel.
    private List<(IReadOnlyList<Vec2> Contour, double MinX, double MinY, double MaxX, double MaxY)> _drillBounds = [];
    // When set, feature meshes are collected here (for an embedded sub-board) instead of becoming
    // top-level nodes, so a panel can instance them at each array position.
    private List<(int Mesh, string Name, JsonObject Extras)>? _capture;

    public GltfSceneBuilder(PcbDocument doc, GltfRenderSettings settings, GltfBuilder? sharedBuilder = null)
    {
        _doc = doc;
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

        // A panel is just a frame holding an array of sub-boards: it has no laminate of its own, and its
        // outline-derived solder mask / silkscreen would sheet over the whole array (covering the boards).
        // So when compositing, render ONLY the embedded boards — each carries its own full stack.
        bool composites = _settings.EmbeddedBoardResolver is not null && _doc.EmbeddedBoards.Count > 0;
        if (composites)
        {
            BuildEmbeddedBoards();
        }
        else
        {
            PrepareBoardHoles(bounds);
            if (_settings.IncludeSubstrate) BuildSubstrate(bounds);
            if (_settings.IncludeCopper) BuildCopperLayers();
            if (_settings.IncludeSolderMask) BuildSolderMask(bounds);
            if (_settings.IncludeSilkscreen) BuildSilkscreen();
            if (_settings.IncludeDrills) BuildDrills();
            if (_settings.IncludeComponents) BuildComponents();
        }

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
    private IEnumerable<PcbText> Texts => _doc.Texts.OfType<PcbText>();

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
        mesh.AddPrism(ring, _boardHoles.Count > 0 ? _boardHoles : null, z0, z1);
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
        var rules = _doc.Rules.OfType<PcbSolderMaskExpansionRule>().Where(r => r.Enabled).ToList();
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
        if (text.ComponentIndex < 0 || text.ComponentIndex >= _doc.Components.Count) return true;
        if (_doc.Components[text.ComponentIndex] is not PcbComponent owner) return true;
        if (text.IsComment && !owner.CommentOn) return false;
        if (text.IsDesignator && !owner.NameOn) return false;
        return true;
    }

    // Dispatches a PCB text to the right geometry path. A text is a 2-D barcode only when its
    // TextKind is BarCode (the BarCodeKind byte merely picks the symbology and is meaningless for
    // plain text); otherwise it is inverted (negative) text, stroke-font text, or TrueType text.
    private void AddText(MeshBuffer mesh, PcbText text, double z, bool faceUp)
    {
        if (text.TextKind == PcbTextKind.BarCode)
            AddBarcode(mesh, text, z, faceUp);
        else if (text.UseInvertedRectangle && text.InvertedRectWidth > Coord.Zero && text.InvertedRectHeight > Coord.Zero)
            AddInvertedText(mesh, text, z, faceUp);
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
        double h = text.Height.ToMm();
        if (h <= 0) return;
        var lines = text.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        int n = lines.Length;
        double lineH = h * 1.2, cap = 0.72 * h;

        string justTt = text.Justification.ToString();
        double radTt = text.Rotation * Math.PI / 180.0;
        double cosTt = Math.Cos(radTt), sinTt = Math.Sin(radTt);
        var locTt = P(text.Location);

        double offY0 = justTt.Contains("Top") ? -cap : justTt.Contains("Middle") ? -cap / 2.0 : 0.0;
        double blockShift = justTt.Contains("Top") ? 0.0 : justTt.Contains("Middle") ? (n - 1) / 2.0 * lineH : (n - 1) * lineH;

        bool any = false;
        for (int li = 0; li < n; li++)
        {
            var glyphs = GltfTrueTypeText.Layout(lines[li], text.FontName, text.FontBold, text.FontItalic, h, out double advance);
            if (glyphs.Count == 0) continue;

            double offX = justTt.Contains("Right") ? -advance : justTt.Contains("Left") ? 0.0 : -advance / 2.0;
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

    // Renders inverted (negative) text: a filled solder/silk rectangle with the glyphs knocked out, so
    // the board shows through the letters. The rectangle is anchored bottom-left at the text location and
    // the glyphs are justified within it per the inverted-rect justification.
    private void AddInvertedText(MeshBuffer mesh, PcbText text, double z, bool faceUp)
    {
        double w = text.InvertedRectWidth.ToMm(), h = text.InvertedRectHeight.ToMm();
        double rad = text.Rotation * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        var loc = P(text.Location);

        // Local rect space: x right (0..w), y up (0..h), bottom-left at the location; mirrored text flips x.
        Vec2 L(double lx, double ly)
        {
            if (text.IsMirrored) lx = w - lx;
            return new Vec2(loc.X + (lx * cos) - (ly * sin), loc.Y + (lx * sin) + (ly * cos));
        }
        var rect = new List<Vec2> { L(0, 0), L(w, 0), L(w, h), L(0, h) };

        // The knocked-out glyphs (TrueType only; a stroke inverted box just stays solid).
        var knockouts = new List<IReadOnlyList<Vec2>>();
        if (text.IsTrueType || text.TextKind == PcbTextKind.TrueType)
        {
            var lines = text.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int n = lines.Length;
            double margin = Math.Min(w, h) * 0.12;
            double glyphH = Math.Min(text.Height.ToMm(), Math.Max(0.1, ((h - (2 * margin)) / n) * 0.82));
            double lineH = glyphH * 1.25;
            double blockH = lineH * n;
            (int ha, int va) = MapInvertedJustification(text.InvertedRectJustification);
            double blockTop = va == 0 ? h - margin : va == 2 ? margin + blockH : (h + blockH) / 2.0; // top edge of the text block

            for (int li = 0; li < n; li++)
            {
                var glyphs = GltfTrueTypeText.Layout(lines[li], text.FontName, text.FontBold, text.FontItalic, glyphH, out double advance);
                double offX = ha == 2 ? w - margin - advance : ha == 0 ? margin : (w - advance) / 2.0;
                double baselineY = blockTop - (lineH * (li + 1)) + ((lineH - glyphH) / 2.0);
                foreach (var glyph in glyphs)
                {
                    knockouts.Add(glyph.Outer.ConvertAll(p => L(p.X + offX, p.Y + baselineY)));
                    foreach (var bowl in glyph.Holes) knockouts.Add(bowl.ConvertAll(p => L(p.X + offX, p.Y + baselineY)));
                }
            }
        }

        foreach (var (outer, holes) in SkiaPolyTools.Difference(rect, knockouts))
            mesh.AddFlatPolygon(outer, holes.Count > 0 ? holes.ConvertAll(x => (IReadOnlyList<Vec2>)x) : null, z, faceUp);
    }

    // Altium inverted-rect justification is column-major 1..9 (Manual=0). Returns (h: 0=Left,1=Center,2=Right;
    // v: 0=Top,1=Middle,2=Bottom).
    private static (int H, int V) MapInvertedJustification(OriginalCircuit.Altium.Models.Pcb.PcbTextJustification j)
    {
        int v = (int)j;
        if (v is < 1 or > 9) return (1, 1);
        return ((v - 1) / 3, (v - 1) % 3);
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

        // Anchor per the text justification (Altium text is bottom-left by default). Glyph space is
        // normalized (height 1, baseline y=0, +x right, +y up), so the offsets are in those units.
        string just = text.Justification.ToString();
        double offX = just.Contains("Right") ? -advance : just.Contains("Left") ? 0.0 : -advance / 2.0;
        double offY = just.Contains("Top") ? -1.0 : just.Contains("Bottom") ? 0.0 : -0.5;

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
        _drillBounds = _boardHoles.ConvertAll(h =>
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in h) { if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X; if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y; }
            return (h, minX, minY, maxX, maxY);
        });
    }

    // ── Embedded boards (panel arrays) ──────────────────────────────────────────────────────────
    // Each EmbeddedBoards6 object is a sub-board (resolved through the settings' resolver) tiled in a
    // rows×cols grid. Its outline-min corner aligns to the array origin, with one step of ColSpacing /
    // RowSpacing between instances. The sub-board's board features are tessellated ONCE (centred on
    // that corner) into shared meshes; each grid cell is then a lightweight node instance referencing
    // them under a transl(/rotate) transform — so a 3×3 panel costs one board's worth of geometry.
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

        var bounds = ComputeBoundsMm();
        AddMaterials();
        PrepareBoardHoles(bounds);

        if (_settings.IncludeSubstrate) BuildSubstrate(bounds);
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

    private Vec2 P(CoordPoint p) => new(p.X.ToMm() - _cx, p.Y.ToMm() - _cy);

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
