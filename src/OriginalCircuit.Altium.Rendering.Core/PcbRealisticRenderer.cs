using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using PadShape = OriginalCircuit.Altium.Models.Pcb.PadShape;
using PadHoleType = OriginalCircuit.Altium.Models.Pcb.PadHoleType;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Renders a <see cref="PcbDocument"/> to an <see cref="IRenderContext"/> as a photorealistic 2D board
/// (a fab-house / gerber-viewer look), as opposed to the Altium-editor view produced by
/// <see cref="PcbComponentRenderer"/>. It composites by physical stack — substrate, copper, a translucent
/// inverse solder mask, surface finish on exposed copper, silkscreen, then drilled holes — so a single
/// top or bottom side reads like a real manufactured board. Colours come from a <see cref="PcbRealisticStyle"/>.
/// </summary>
/// <remarks>
/// PCB-only: there is no footprint (<c>PcbComponent</c>) or schematic path. Works against any
/// <see cref="IRenderContext"/> backend (so the same engine drives both PNG/JPEG and SVG output).
/// <para>
/// The solder mask is synthesized: Altium stores no mask polarity, so the mask sheet is the board outline
/// minus an <em>opening</em> grown from each non-tented pad/via copper shape. Mask openings whose expansion
/// is rule-driven use <see cref="PcbRealisticStyle.DefaultSolderMaskExpansion"/> (per-object manual
/// expansion is honoured exactly); resolving the actual Altium design rule is a deliberate v1 simplification.
/// </para>
/// </remarks>
public sealed class PcbRealisticRenderer
{
    private readonly CoordTransform _transform;
    private readonly PcbRealisticStyle _style;
    private readonly uint _backgroundArgb;

    // Per-corner samples when tessellating rounded shapes into fill contours.
    private const int ArcSteps = 8;
    private const int CircleSteps = 40;
    // Segments per 180° round end cap when stroking a solder-mask-layer track/arc into an opening contour.
    private const int CapSteps = 16;
    // A TrueType PCB string's Height is the font CELL height; Altium renders the em (point size) a bit smaller.
    private const double TtEmScale = 0.8;

    /// <summary>
    /// Creates a renderer that maps world coordinates with <paramref name="transform"/> and paints with
    /// <paramref name="style"/>. <paramref name="backgroundArgb"/> is the page background, painted into
    /// milled cut-outs (mechanical "RouteToolPath" geometry) so they read as removed board.
    /// </summary>
    public PcbRealisticRenderer(CoordTransform transform, PcbRealisticStyle style, uint backgroundArgb = 0xFF000000)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
        _style = style ?? throw new ArgumentNullException(nameof(style));
        _backgroundArgb = backgroundArgb;
    }

    /// <summary>
    /// Computes the output image size and AutoZoom margin for a board render. When
    /// <paramref name="cropToBounds"/> is set and the board bounds are non-degenerate, the size is the
    /// board's aspect ratio fitted within the requested width/height and the margin is 1.0, so the rendered
    /// board fills the image with no surrounding letterbox (the image is the board's bounding box).
    /// Otherwise the requested size and the default 5% fit margin are returned.
    /// </summary>
    public static (int Width, int Height, double Margin) FitOutput(
        CoordRect bounds, int requestedWidth, int requestedHeight, bool cropToBounds)
    {
        double bw = bounds.Width.ToRaw(), bh = bounds.Height.ToRaw();
        if (!cropToBounds || bw <= 0 || bh <= 0)
            return (requestedWidth, requestedHeight, 0.95);

        double aspect = bw / bh;
        int w, h;
        if (requestedWidth / (double)requestedHeight > aspect)
        {
            h = requestedHeight;
            w = Math.Max(1, (int)Math.Round(requestedHeight * aspect));
        }
        else
        {
            w = requestedWidth;
            h = Math.Max(1, (int)Math.Round(requestedWidth / aspect));
        }
        return (w, h, 1.0);
    }

    /// <summary>
    /// Resolves the solder-mask opening expansion for a pad/via copper shape from its expansion mode:
    /// <c>0</c> = none (opening == copper), <c>2</c> = the object's manual expansion, anything else
    /// (the Altium default <c>1</c> = From-Rule) = <paramref name="defaultExpansion"/>.
    /// </summary>
    public static Coord EffectiveSolderMaskExpansion(int mode, Coord manual, Coord defaultExpansion) => mode switch
    {
        0 => Coord.Zero,
        2 => manual,
        _ => defaultExpansion,
    };

    // Resolves the From-Rule solder-mask expansion for a pad/via from the document's SolderMaskExpansion
    // design rules. A via prefers a via-scoped rule (e.g. SCOPE1=IsVia); a pad uses the most general
    // non-via rule. Falls back to the style's default when no rule is present.
    private Coord ResolveSolderMaskRuleExpansion(PcbDocument document, bool forVia)
    {
        var rules = document.Rules.OfType<PcbSolderMaskExpansionRule>().Where(r => r.Enabled).ToList();
        if (rules.Count == 0) return _style.DefaultSolderMaskExpansion;

        static bool MentionsVia(PcbSolderMaskExpansionRule r) =>
            (r.Scope1Expression?.Contains("Via", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.Scope2Expression?.Contains("Via", StringComparison.OrdinalIgnoreCase) ?? false);

        PcbSolderMaskExpansionRule? pick = forVia
            ? rules.Where(MentionsVia).OrderBy(r => r.Priority).FirstOrDefault()
            : null;
        pick ??= rules.Where(r => !MentionsVia(r)).OrderBy(r => r.Priority).FirstOrDefault();
        pick ??= rules.OrderBy(r => r.Priority).FirstOrDefault();
        return pick?.Expansion ?? _style.DefaultSolderMaskExpansion;
    }

    /// <summary>Renders the board into <paramref name="context"/> using the configured style.</summary>
    public void Render(PcbDocument document, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        bool bottom = _style.ViewSide == PcbViewSide.Bottom;
        int copperLayer = bottom ? 32 : 1;
        int silkLayer = bottom ? 34 : 33;
        int solderLayer = bottom ? 38 : 37; // solder-mask layer: objects on it mean "mask removed"
        int sideSignalLayer = bottom ? 32 : 1;

        var substrateArgb = ColorHelper.EdaColorToArgb(_style.SubstrateColor);
        var copperArgb = ColorHelper.EdaColorToArgb(_style.CopperColor);
        var maskArgb = ColorHelper.EdaColorToArgb(_style.SolderMaskColor);
        var maskOpaqueArgb = maskArgb | 0xFF000000u; // knockout colour for inverted silk text (the board shows through)
        var silkArgb = ColorHelper.EdaColorToArgb(_style.SilkscreenColor);
        var finishArgb = ColorHelper.EdaColorToArgb(_style.FinishColor);
        var holeArgb = ColorHelper.EdaColorToArgb(_style.HoleColor);

        // Bottom view: mirror about the canvas centre so the board reads as if physically flipped over.
        if (bottom) { context.SaveState(); ApplyHorizontalFlip(context); }

        var collected = Collect(document);
        var outline = MapOutline(document.GetBoardOutline());
        var padRuleExp = ResolveSolderMaskRuleExpansion(document, forVia: false);
        var viaRuleExp = ResolveSolderMaskRuleExpansion(document, forVia: true);
        var metal = CollectMetal(collected, bottom, sideSignalLayer, padRuleExp, viaRuleExp);
        var millingLayers = MillingLayers(document);

        // Plated-copper colour: exposed copper reads as the surface finish (or bare copper when finish is
        // disabled). The whole copper layer is drawn in this colour; the mask then tints whatever it covers.
        uint copperShownArgb = _style.ShowSurfaceFinish ? finishArgb : copperArgb;

        // Optionally clip the whole stack to the board outline so overhanging silk/copper/text outside the
        // manufactured board area is trimmed. Needs a real outline; a board with none is left unclipped.
        // The substrate already fills only the outline, so clipping mainly trims the silk/copper overhang.
        bool clip = _style.ClipToBoardOutline && outline is not null;
        if (clip)
        {
            context.SaveState();
            context.SetClipPath(new[] { outline.Value });
        }

        // The renderer composites by physical layer, each emitted as a named group ("substrate", "copper",
        // "soldermask", "silkscreen", "drills") so a vector (SVG) export can toggle/style them individually.

        // Layer 1 ── Substrate (bare laminate). Non-zero winding fill: a real board outline is often a
        //            non-simple polygon (tessellated arcs, slots) that an even-odd fill would cancel.
        context.BeginGroup("substrate");
        RenderSubstrate(context, outline, substrateArgb);
        context.EndGroup();

        // Layer 2 ── Copper (plated). Every copper feature on this side. Where the mask doesn't cover it,
        //            it shows as exposed plating; where the mask covers it, the translucent mask tints it.
        context.BeginGroup("copper");
        DrawCopperLayer(context, collected, metal, copperLayer, copperShownArgb, substrateArgb);
        context.EndGroup();

        // Layer 3 ── Solder mask: a SOLID translucent sheet over the whole board, then the openings are
        //            re-vealed by re-drawing the un-masked stack (substrate + copper) clipped to the UNION
        //            of the openings. Using a union clip (not an even-odd hole fill) means overlapping
        //            openings — e.g. a pad's mask expansion overlapping a solder-mask-layer clearance — do
        //            NOT cancel ("double negative") and leave the mask filled in the overlap. The sheet is
        //            translucent, so mask over copper still composites darker than mask over laminate.
        //            Openings = non-tented pad/via expansions + the negative geometry on the solder-mask
        //            layer (37/38), e.g. the clearance ring around the board outline.
        if (_style.ShowSolderMask && outline is not null)
        {
            context.BeginGroup("soldermask");
            context.FillPolygon(outline.Value.X, outline.Value.Y, maskArgb); // solid translucent sheet

            var openings = CollectOpenings(collected, metal, solderLayer);
            if (openings.Count > 0)
            {
                context.SaveState();
                context.SetClipPath(openings);                                            // union of openings
                RenderSubstrate(context, outline, substrateArgb);                         // exposed laminate
                DrawCopperLayer(context, collected, metal, copperLayer, copperShownArgb, substrateArgb); // exposed plating
                context.RestoreState();
            }
            context.EndGroup();
        }

        // Layer 4 ── Silkscreen, printed over the mask.
        if (_style.ShowSilkscreen)
        {
            context.BeginGroup("silkscreen");
            foreach (var t in collected.Tracks) if (t.Layer == silkLayer) DrawTrack(context, t, silkArgb);
            foreach (var a in collected.Arcs) if (a.Layer == silkLayer) DrawArc(context, a, silkArgb);
            foreach (var f in collected.Fills) if (f.Layer == silkLayer) DrawFill(context, f, silkArgb);
            foreach (var r in collected.Regions) if (r.Layer == silkLayer) DrawRegion(context, r, silkArgb);
            foreach (var (text, owner) in collected.Texts)
                if (text.Layer == silkLayer && IsTextVisible(text, owner)) DrawText(context, text, silkArgb, maskOpaqueArgb);
            context.EndGroup();
        }

        // Layer 5 ── Drilled holes / barrels, punched through everything.
        if (_style.ShowDrillHoles)
        {
            context.BeginGroup("drills");
            foreach (var pad in collected.Pads) DrawPadHole(context, pad, holeArgb);
            foreach (var via in collected.Vias) DrawViaHole(context, via, holeArgb);
            context.EndGroup();
        }

        // Layer 6 ── Cut-outs / slots remove board material, so they are painted in the page background to
        //            read as a hole right through the board. Two sources contribute: geometry on a
        //            mechanical milling ("RouteToolPath") layer, and any region flagged as a board cut-out
        //            (the ISBOARDCUTOUT key) — these mark a void in the board shape, live on a mechanical
        //            layer, and are otherwise drawn by no other pass. (A Cutout-kind region on a copper
        //            layer is only a void in a pour, not a hole through the board, so it is not included.)
        var boardCutouts = collected.Regions
            .Where(r => r.IsBoardCutout == true && !millingLayers.Contains(r.Layer))
            .ToList();
        if (millingLayers.Count > 0 || boardCutouts.Count > 0)
        {
            context.BeginGroup("cutouts");
            foreach (var t in collected.Tracks) if (millingLayers.Contains(t.Layer)) DrawTrack(context, t, _backgroundArgb);
            foreach (var a in collected.Arcs) if (millingLayers.Contains(a.Layer)) DrawArc(context, a, _backgroundArgb);
            foreach (var f in collected.Fills) if (millingLayers.Contains(f.Layer)) DrawFill(context, f, _backgroundArgb);
            foreach (var r in collected.Regions) if (millingLayers.Contains(r.Layer)) DrawRegion(context, r, _backgroundArgb);
            foreach (var r in boardCutouts) DrawRegion(context, r, _backgroundArgb);
            context.EndGroup();
        }

        if (clip) context.RestoreState();
        if (bottom) context.RestoreState();
    }

    // Mechanical layer ids designated for milling/routing (Board6 "LAYER{id}MECHKIND = RouteToolPath" or a
    // milling/rout kind). Geometry on these layers cuts through the board.
    private static HashSet<int> MillingLayers(PcbDocument document)
    {
        var layers = new HashSet<int>();
        if (document.BoardParameters is not { } bp) return layers;
        foreach (var (key, value) in bp)
        {
            if (!key.EndsWith("MECHKIND", StringComparison.OrdinalIgnoreCase)) continue;
            if (!(value.Contains("Rout", StringComparison.OrdinalIgnoreCase) ||
                  value.Contains("Mill", StringComparison.OrdinalIgnoreCase))) continue;
            // key forms like "LAYER64MECHKIND"; pull the plain layer id (the one matching primitive.Layer).
            var m = System.Text.RegularExpressions.Regex.Match(key, @"^LAYER(\d+)MECHKIND$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var id)) layers.Add(id);
        }
        return layers;
    }

    // Draws every copper feature on the side (tracks/arcs/fills/regions + text + pad/via metal) in one
    // colour. Used for the copper layer and, clipped to the openings, to re-veal exposed copper through
    // the mask. <paramref name="knockoutColor"/> shows through inverted copper text (bare laminate).
    private void DrawCopperLayer(IRenderContext context, Collected collected, List<MetalFeature> metal,
        int copperLayer, uint color, uint knockoutColor)
    {
        foreach (var t in collected.Tracks) if (t.Layer == copperLayer) DrawTrack(context, t, color);
        foreach (var a in collected.Arcs) if (a.Layer == copperLayer) DrawArc(context, a, color);
        foreach (var f in collected.Fills) if (f.Layer == copperLayer) DrawFill(context, f, color);
        // Skip keepout/cutout regions: they mark the ABSENCE of copper (e.g. the keepout around a fiducial
        // pad). Drawing them as copper would fill the area — leaving laminate to show through instead.
        foreach (var r in collected.Regions)
            if (r.Layer == copperLayer && !r.IsKeepout && r.Kind != 1) DrawRegion(context, r, color);
        foreach (var (text, owner) in collected.Texts)
            if (text.Layer == copperLayer && IsTextVisible(text, owner)) DrawText(context, text, color, knockoutColor);
        foreach (var m in metal) context.FillPolygon(m.CopperXs, m.CopperYs, color);
    }

    // All mask openings (screen space) for this side: non-tented pad/via expansions plus the negative
    // geometry on the solder-mask layer. Normalized to a consistent winding so the non-zero clip unions
    // overlapping openings instead of cancelling them.
    private List<(double[] X, double[] Y)> CollectOpenings(Collected collected, List<MetalFeature> metal, int solderLayer)
    {
        var openings = new List<(double[] X, double[] Y)>();
        foreach (var m in metal) if (m.HasOpening) openings.Add((m.OpeningXs, m.OpeningYs));
        AddSolderLayerOpenings(collected, solderLayer, openings);
        for (int i = 0; i < openings.Count; i++) openings[i] = NormalizeWinding(openings[i]);
        return openings;
    }

    // Ensures a contour winds in a consistent (positive shoelace) orientation, so a non-zero-winding clip
    // built from many contours unions them (overlaps stay covered) rather than subtracting on overlap.
    private static (double[] X, double[] Y) NormalizeWinding((double[] X, double[] Y) c)
    {
        var (xs, ys) = c;
        int n = xs.Length;
        if (n < 3) return c;
        double area2 = 0;
        for (int i = 0, j = n - 1; i < n; j = i++)
            area2 += (xs[j] - xs[i]) * (ys[j] + ys[i]);
        if (area2 >= 0) return c;
        var rx = new double[n];
        var ry = new double[n];
        for (int i = 0; i < n; i++) { rx[i] = xs[n - 1 - i]; ry[i] = ys[n - 1 - i]; }
        return (rx, ry);
    }

    // Adds the negative geometry on the solder-mask layer (tracks/arcs/fills/regions) as opening contours
    // (screen space), so the mask sheet is knocked back wherever the designer removed mask.
    private void AddSolderLayerOpenings(Collected c, int solderLayer, List<(double[] X, double[] Y)> contours)
    {
        foreach (var t in c.Tracks)
            if (t.Layer == solderLayer)
            {
                var (x1, y1) = _transform.WorldToScreen(t.Start.X, t.Start.Y);
                var (x2, y2) = _transform.WorldToScreen(t.End.X, t.End.Y);
                contours.Add(StrokePolyline(new[] { (x1, y1), (x2, y2) }, Math.Max(0.5, _transform.ScaleValue(t.Width) / 2.0)));
            }
        foreach (var a in c.Arcs)
            if (a.Layer == solderLayer)
                contours.Add(StrokePolyline(SampleArcScreen(a), Math.Max(0.5, _transform.ScaleValue(a.Width) / 2.0)));
        foreach (var f in c.Fills)
            if (f.Layer == solderLayer)
                contours.Add(FillRectScreen(f));
        foreach (var r in c.Regions)
            if (r.Layer == solderLayer && r.Outline.Count >= 3)
                contours.Add(MapContour(r.Outline));
        // A 2-D barcode (Data Matrix / QR) on the solder-mask layer removes mask over its foreground region,
        // revealing the copper/finish beneath. For an inverted symbol (the Coherent Digitiser's) the foreground
        // is the field around the dark modules, so the opening is a gold square with the data modules left masked.
        foreach (var (text, _) in c.Texts)
            if (text.Layer == solderLayer)
            {
                var barcode = PcbBarcodeGeometry.TryBuild(text);
                if (barcode is not null)
                    foreach (var quad in barcode.Foreground)
                        contours.Add(MapContour(quad));
            }
    }

    // ── Collection ──────────────────────────────────────────────────

    private sealed class Collected
    {
        public readonly List<PcbTrack> Tracks = new();
        public readonly List<PcbArc> Arcs = new();
        public readonly List<PcbFill> Fills = new();
        public readonly List<PcbRegion> Regions = new();
        public readonly List<PcbPad> Pads = new();
        public readonly List<PcbVia> Vias = new();
        public readonly List<(PcbText Text, PcbComponent? Owner)> Texts = new();
    }

    // Flattens the document's own primitives plus every component's children, de-duplicated by reference.
    // The PcbDoc reader assigns each component pad into BOTH document.Pads and component.Pads (the same
    // object), so a naive collect would draw component pads twice; a from-scratch board, conversely, may
    // hold primitives only under a component. Reference de-dup covers both. Component children are already
    // baked into world coordinates by the reader, so they collect flat alongside doc-level ones.
    private static Collected Collect(PcbDocument document)
    {
        var c = new Collected();
        var components = document.Components;

        var seenTracks = new HashSet<PcbTrack>();
        var seenArcs = new HashSet<PcbArc>();
        var seenFills = new HashSet<PcbFill>();
        var seenRegions = new HashSet<PcbRegion>();
        var seenPads = new HashSet<PcbPad>();
        var seenVias = new HashSet<PcbVia>();
        var seenTexts = new HashSet<PcbText>();

        void AddPrims(IEnumerable<IPcbTrack> tracks, IEnumerable<IPcbArc> arcs, IEnumerable<IPcbFill> fills,
            IEnumerable<IPcbRegion> regions, IEnumerable<IPcbPad> pads, IEnumerable<IPcbVia> vias)
        {
            foreach (var t in tracks.Cast<PcbTrack>()) if (seenTracks.Add(t)) c.Tracks.Add(t);
            foreach (var a in arcs.Cast<PcbArc>()) if (seenArcs.Add(a)) c.Arcs.Add(a);
            foreach (var f in fills.Cast<PcbFill>()) if (seenFills.Add(f)) c.Fills.Add(f);
            foreach (var r in regions.Cast<PcbRegion>()) if (seenRegions.Add(r)) c.Regions.Add(r);
            foreach (var p in pads.Cast<PcbPad>()) if (seenPads.Add(p)) c.Pads.Add(p);
            foreach (var v in vias.Cast<PcbVia>()) if (seenVias.Add(v)) c.Vias.Add(v);
        }

        AddPrims(document.Tracks, document.Arcs, document.Fills, document.Regions, document.Pads, document.Vias);
        foreach (var t in document.Texts.Cast<PcbText>())
        {
            if (!seenTexts.Add(t)) continue;
            var owner = t.ComponentIndex >= 0 && t.ComponentIndex < components.Count
                ? components[t.ComponentIndex] as PcbComponent
                : null;
            c.Texts.Add((t, owner));
        }

        foreach (var comp in components.Cast<PcbComponent>())
        {
            AddPrims(comp.Tracks, comp.Arcs, comp.Fills, comp.Regions, comp.Pads, comp.Vias);
            foreach (var t in comp.Texts.Cast<PcbText>())
                if (seenTexts.Add(t)) c.Texts.Add((t, comp));
        }
        return c;
    }

    // ── Metal features (pad/via copper + mask openings) ─────────────

    private readonly struct MetalFeature
    {
        public readonly double[] CopperXs;
        public readonly double[] CopperYs;
        public readonly double[] OpeningXs;
        public readonly double[] OpeningYs;
        public readonly bool HasOpening;

        public MetalFeature(double[] cx, double[] cy, double[] ox, double[] oy, bool hasOpening)
        {
            CopperXs = cx; CopperYs = cy; OpeningXs = ox; OpeningYs = oy; HasOpening = hasOpening;
        }
    }

    // Builds the bare-copper and (when not tented) mask-opening contour for every pad/via that has copper
    // on the rendered side. SMD pads appear only on their own layer; through-hole pads and spanning vias
    // appear on both sides.
    private List<MetalFeature> CollectMetal(Collected collected, bool bottom, int sideSignalLayer,
        Coord padRuleExp, Coord viaRuleExp)
    {
        var features = new List<MetalFeature>(collected.Pads.Count + collected.Vias.Count);

        foreach (var pad in collected.Pads)
        {
            bool throughHole = pad.HoleSize > Coord.Zero;
            if (!throughHole && pad.Layer != sideSignalLayer) continue; // SMD on the other side

            var size = bottom ? pad.SizeBottom : pad.SizeTop;
            var shape = bottom ? pad.ShapeBottom : pad.ShapeTop;
            if (size.X <= Coord.Zero || size.Y <= Coord.Zero) continue;

            var copper = PadContour(pad.Location, size.X, size.Y, shape, pad.CornerRadiusPercentage,
                Coord.Zero, pad.Rotation);

            bool tented = bottom ? pad.IsTentingBottom : pad.IsTentingTop;
            double[] ox = Array.Empty<double>(), oy = Array.Empty<double>();
            if (!tented)
            {
                // From-Rule pads use the resolved design-rule expansion; manual pads use their own value.
                var expansion = EffectiveSolderMaskExpansion(
                    pad.SolderMaskExpansionMode, pad.SolderMaskExpansion, padRuleExp);
                (ox, oy) = PadContour(pad.Location, size.X, size.Y, shape, pad.CornerRadiusPercentage,
                    expansion, pad.Rotation);
            }
            features.Add(new MetalFeature(copper.X, copper.Y, ox, oy, !tented));
        }

        foreach (var via in collected.Vias)
        {
            if (via.Diameter <= Coord.Zero) continue;
            int lo = Math.Min(via.StartLayer, via.EndLayer), hi = Math.Max(via.StartLayer, via.EndLayer);
            if (sideSignalLayer < lo || sideSignalLayer > hi) continue; // doesn't reach this side

            var (cx, cy) = _transform.WorldToScreen(via.Location.X, via.Location.Y);
            double r = _transform.ScaleValue(via.Diameter) / 2.0;
            var copper = CircleContour(cx, cy, r);

            bool tented = via.IsTented || (bottom ? via.IsTentingBottom : via.IsTentingTop);
            double[] ox = Array.Empty<double>(), oy = Array.Empty<double>();
            if (!tented)
            {
                var expansion = EffectiveSolderMaskExpansion(
                    via.SolderMaskExpansionMode, via.SolderMaskExpansion, viaRuleExp);
                var opening = CircleContour(cx, cy, r + _transform.ScaleValue(expansion));
                ox = opening.X; oy = opening.Y;
            }
            features.Add(new MetalFeature(copper.X, copper.Y, ox, oy, !tented));
        }

        return features;
    }

    // ── Substrate ───────────────────────────────────────────────────

    private void RenderSubstrate(IRenderContext context, (double[] X, double[] Y)? outline, uint substrateArgb)
    {
        if (outline is not null)
        {
            context.FillPolygon(outline.Value.X, outline.Value.Y, substrateArgb);
            return;
        }

        // No Board6 outline: fall back to filling the whole canvas so the board area still reads.
        context.FillRectangle(0, 0, _transform.ScreenWidth, _transform.ScreenHeight, substrateArgb);
    }

    private (double[] X, double[] Y)? MapOutline(IReadOnlyList<CoordPoint> outline)
        => outline.Count >= 3 ? MapContour(outline) : null;

    // ── Per-primitive copper/silk drawing ───────────────────────────

    private void DrawTrack(IRenderContext context, PcbTrack track, uint color)
    {
        var (x1, y1) = _transform.WorldToScreen(track.Start.X, track.Start.Y);
        var (x2, y2) = _transform.WorldToScreen(track.End.X, track.End.Y);
        var width = Math.Max(1, _transform.ScaleValue(track.Width));
        context.DrawLine(x1, y1, x2, y2, color, width);
    }

    private void DrawArc(IRenderContext context, PcbArc arc, uint color)
    {
        var (cx, cy) = _transform.WorldToScreen(arc.Center.X, arc.Center.Y);
        var r = _transform.ScaleValue(arc.Radius);
        var strokeWidth = Math.Max(1, _transform.ScaleValue(arc.Width));

        var startAngle = -Finite(arc.StartAngle);
        var sweep = Finite(arc.EndAngle) - Finite(arc.StartAngle);
        if (sweep <= 0) sweep += 360;

        if (Math.Abs(sweep - 360) < 0.01)
            context.DrawEllipse(cx, cy, r, r, color, strokeWidth);
        else
            context.DrawArc(cx, cy, r, r, startAngle, -sweep, color, strokeWidth);
    }

    private void DrawFill(IRenderContext context, PcbFill fill, uint color)
    {
        var (x1, y1) = _transform.WorldToScreen(fill.Corner1.X, fill.Corner1.Y);
        var (x2, y2) = _transform.WorldToScreen(fill.Corner2.X, fill.Corner2.Y);

        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        var w = Math.Max(1, Math.Abs(x2 - x1));
        var h = Math.Max(1, Math.Abs(y2 - y1));

        if (Finite(fill.Rotation) != 0)
        {
            var centerX = (x1 + x2) / 2.0;
            var centerY = (y1 + y2) / 2.0;
            var rad = -Finite(fill.Rotation) * Math.PI / 180.0;
            var cos = Math.Cos(rad);
            var sin = Math.Sin(rad);
            var halfW = w / 2.0;
            var halfH = h / 2.0;
            var corners = new[] { (-halfW, -halfH), (halfW, -halfH), (halfW, halfH), (-halfW, halfH) };
            var xs = new double[4];
            var ys = new double[4];
            for (int i = 0; i < 4; i++)
            {
                xs[i] = centerX + corners[i].Item1 * cos - corners[i].Item2 * sin;
                ys[i] = centerY + corners[i].Item1 * sin + corners[i].Item2 * cos;
            }
            context.FillPolygon(xs, ys, color);
        }
        else
        {
            context.FillRectangle(x, y, w, h, color);
        }
    }

    private void DrawRegion(IRenderContext context, PcbRegion region, uint color)
    {
        if (region.Outline.Count < 3) return;

        if (region.Holes is null || region.Holes.Count == 0)
        {
            var (xs, ys) = MapContour(region.Outline);
            context.FillPolygon(xs, ys, color);
            return;
        }

        var contours = new List<(double[] X, double[] Y)>(1 + region.Holes.Count) { MapContour(region.Outline) };
        foreach (var hole in region.Holes)
            if (hole.Count >= 3) contours.Add(MapContour(hole));
        context.FillContours(contours, color);
    }

    private static readonly string[] NewlineSeparators = { "\r\n", "\n", "\r" };

    // Draws a text primitive in <paramref name="color"/>, honouring embedded newlines (multi-line text),
    // a frame / inverted-rectangle box, and justification. <paramref name="knockoutColor"/> is shown
    // through the cut-out glyphs of inverted (negative) text, revealing what lies beneath the ink.
    private void DrawText(IRenderContext context, PcbText text, uint color, uint knockoutColor)
    {
        if (string.IsNullOrEmpty(text.Text)) return;

        // 2-D barcode (Data Matrix / QR) on an ink layer (silk/copper): paint the encoded geometry in the ink
        // colour. On the solder-mask layer the modules are handled as mask openings (AddSolderLayerOpenings).
        var barcode = PcbBarcodeGeometry.TryBuild(text);
        if (barcode is not null)
        {
            foreach (var quad in barcode.Foreground)
            {
                var (mx, my) = MapContour(quad);
                context.FillPolygon(mx, my, color);
            }
            return;
        }

        var lines = text.Text.Split(NewlineSeparators, StringSplitOptions.None);

        var (x, y) = _transform.WorldToScreen(text.Location.X, text.Location.Y);
        var height = Math.Max(1, _transform.ScaleValue(text.Height));
        // Altium's TrueType strings render at an em a bit smaller than the Height cell; stroke text fills the
        // Height exactly. (Match the inverted-rect box Altium sizes to the text.)
        bool isTrueType = text.IsTrueType || text.TextKind != PcbTextKind.Stroke;
        double glyphEm = isTrueType ? height * TtEmScale : height;
        double rotation = Finite(text.Rotation);

        bool inverted = text.UseInvertedRectangle && text.InvertedRectWidth > Coord.Zero && text.InvertedRectHeight > Coord.Zero;
        bool framed = inverted || (text.IsFrame && text.InvertedRectWidth > Coord.Zero && text.InvertedRectHeight > Coord.Zero);

        context.SaveState();
        context.Translate(x, y);
        if (rotation != 0) context.Rotate(-rotation);
        if (text.IsMirrored) context.Scale(-1, 1);

        if (framed)
        {
            // The text box is anchored bottom-left at Location; screen Y is down so it spans y ∈ [-h, 0].
            // Lines are stacked and justified within the box per InvertedRectJustification.
            double w = _transform.ScaleValue(text.InvertedRectWidth);
            double h = _transform.ScaleValue(text.InvertedRectHeight);
            if (w >= 1 && h >= 1)
            {
                if (inverted) context.FillRectangle(0, -h, w, h, color); // ink box; glyphs are knocked out
                var glyphColor = inverted ? knockoutColor : color;
                var (ha, va) = MapPcbJustification(text.InvertedRectJustification);
                // The box is sized by Altium to bound the text, so fill most of it.
                double margin = Math.Min(w, h) * 0.07;
                double glyphH = Math.Max(1, Math.Min(height * TtEmScale, (h - 2 * margin) / lines.Length * 0.92));
                double lineH = glyphH * 1.2;
                double blockH = lineH * lines.Length;
                double ax = ha == TextHAlign.Right ? w - margin : ha == TextHAlign.Center ? w / 2.0 : margin;
                double blockTop = va == TextVAlign.Top ? -h + margin
                                : va == TextVAlign.Bottom ? -margin - blockH
                                : -h / 2.0 - blockH / 2.0;
                for (int i = 0; i < lines.Length; i++)
                    DrawTextLine(context, lines[i], text, ax, blockTop + lineH * (i + 0.5) + glyphH / 2.0, glyphH, glyphColor, ha);
            }
        }
        else
        {
            // Non-framed free strings are positioned by their legacy Location anchor — Altium does not apply
            // the inverted-rect justification to them (that drives only framed text). The mirror Scale(-1,1)
            // above already reflects the glyphs about the anchor.
            var (ha, va) = MapJustification(text.Justification.ToString());
            double lineH = glyphEm * 1.2;
            int n = lines.Length;
            for (int i = 0; i < n; i++)
            {
                // i=0 is the top line. Bottom-justified text puts the bottom line's baseline at Location.
                double baseline = va == TextVAlign.Bottom ? -(n - 1 - i) * lineH
                                : va == TextVAlign.Top ? glyphEm + i * lineH
                                : (i - (n - 1) / 2.0) * lineH + glyphEm / 2.0;
                DrawTextLine(context, lines[i], text, 0, baseline, glyphEm, color, ha);
            }
        }
        context.RestoreState();
    }

    // Draws one line of text with its baseline at (ax, baseline) in the current (already translated/rotated)
    // frame, horizontally aligned per ha. Stroke text is drawn as glyph segments; TrueType via the backend.
    private void DrawTextLine(IRenderContext context, string line, PcbText text, double ax, double baseline,
        double glyphHeight, uint color, TextHAlign ha)
    {
        if (string.IsNullOrEmpty(line)) return;
        if (text.TextKind == PcbTextKind.Stroke && !text.IsTrueType)
        {
            var segments = AltiumStrokeFont.Layout(line, AltiumStrokeFont.FromStrokeFont(text.StrokeFont), out var advance);
            if (segments.Count == 0) return;
            var strokeWidth = Math.Max(1, _transform.ScaleValue(text.StrokeWidth));
            double width = advance * glyphHeight;
            double offX = ax + (ha == TextHAlign.Right ? -width : ha == TextHAlign.Center ? -width / 2.0 : 0);
            // Stroke glyphs are laid out from the baseline (y=0), ascending to -glyphHeight.
            foreach (var s in segments)
                context.DrawLine(s.X1 * glyphHeight + offX, -s.Y1 * glyphHeight + baseline,
                    s.X2 * glyphHeight + offX, -s.Y2 * glyphHeight + baseline, color, strokeWidth);
        }
        else
        {
            var options = new TextRenderOptions
            {
                FontFamily = text.FontName ?? "Arial",
                Bold = text.FontBold,
                Italic = text.FontItalic,
                HorizontalAlignment = ha,
                VerticalAlignment = TextVAlign.Baseline,
            };
            context.DrawText(line, ax, baseline, glyphHeight, color, options);
        }
    }

    // Maps a schematic-style justification name (e.g. "BottomLeft", "CenterCenter") to alignment.
    private static (TextHAlign H, TextVAlign V) MapJustification(string name)
    {
        var h = name.Contains("Right", StringComparison.OrdinalIgnoreCase) ? TextHAlign.Right
              : name.Contains("Left", StringComparison.OrdinalIgnoreCase) ? TextHAlign.Left
              : TextHAlign.Center;
        var v = name.Contains("Top", StringComparison.OrdinalIgnoreCase) ? TextVAlign.Top
              : name.Contains("Bottom", StringComparison.OrdinalIgnoreCase) ? TextVAlign.Bottom
              : TextVAlign.Middle;
        return (h, v);
    }

    // Maps the Altium PCB inverted-rect justification (column-major 1..9, Manual=0) to alignment.
    private static (TextHAlign H, TextVAlign V) MapPcbJustification(PcbTextJustification j)
    {
        int v = (int)j;
        if (v is < 1 or > 9) return (TextHAlign.Center, TextVAlign.Middle); // Manual / unknown
        var h = ((v - 1) / 3) switch { 0 => TextHAlign.Left, 1 => TextHAlign.Center, _ => TextHAlign.Right };
        var va = ((v - 1) % 3) switch { 0 => TextVAlign.Top, 1 => TextVAlign.Middle, _ => TextVAlign.Bottom };
        return (h, va);
    }

    // A component's designator/comment text shows only when the component enables that field
    // (Altium's NameOn/CommentOn). Free text and non-designator/comment text always show.
    private static bool IsTextVisible(PcbText text, PcbComponent? owner)
    {
        if (owner is null) return true;
        if (text.IsComment && !owner.CommentOn) return false;
        if (text.IsDesignator && !owner.NameOn) return false;
        return true;
    }

    // ── Holes ───────────────────────────────────────────────────────

    private void DrawPadHole(IRenderContext context, PcbPad pad, uint holeColor)
    {
        if (pad.HoleSize <= Coord.Zero) return;

        var (cx, cy) = _transform.WorldToScreen(pad.Location.X, pad.Location.Y);
        var holeR = _transform.ScaleValue(pad.HoleSize) / 2.0;
        if (holeR <= 0.5) return;

        context.SaveState();
        context.Translate(cx, cy);
        // The hole lives in the pad's frame: apply the pad rotation, then any hole-specific rotation, so
        // a square/slot hole follows the pad's orientation (matching how the pad shape itself is drawn).
        double padRotation = Finite(pad.Rotation);
        double holeRotation = Finite(pad.HoleRotation);
        if (padRotation != 0) context.Rotate(-padRotation);
        if (holeRotation != 0) context.Rotate(-holeRotation);

        if (pad.HoleType == PadHoleType.Square)
            context.FillRectangle(-holeR, -holeR, holeR * 2, holeR * 2, holeColor);
        else if (pad.HoleType == PadHoleType.Slot)
        {
            var half = _transform.ScaleValue(pad.HoleSlotLength > 0 ? Coord.FromRaw(pad.HoleSlotLength) : pad.HoleSize) / 2.0;
            if (half < holeR) half = holeR;
            context.FillRoundedRectangle(-half, -holeR, half * 2, holeR * 2, holeR, holeColor);
        }
        else
            context.FillEllipse(0, 0, holeR, holeR, holeColor);

        context.RestoreState();
    }

    private void DrawViaHole(IRenderContext context, PcbVia via, uint holeColor)
    {
        if (via.HoleSize <= Coord.Zero) return;
        var (cx, cy) = _transform.WorldToScreen(via.Location.X, via.Location.Y);
        var holeR = _transform.ScaleValue(via.HoleSize) / 2.0;
        if (holeR > 0.5) context.FillEllipse(cx, cy, holeR, holeR, holeColor);
    }

    // ── Opening-geometry helpers (solder-mask-layer negatives) ──────

    // Samples a solder-mask-layer arc's centreline to screen-space points (Y-inverted via the transform).
    private IReadOnlyList<(double X, double Y)> SampleArcScreen(PcbArc a)
    {
        double cx = a.Center.X.ToRaw(), cy = a.Center.Y.ToRaw(), r = a.Radius.ToRaw();
        double start = Finite(a.StartAngle), sweep = Finite(a.EndAngle) - Finite(a.StartAngle);
        if (sweep <= 0) sweep += 360;
        int steps = Math.Max(2, (int)(sweep / 6.0));
        var pts = new List<(double X, double Y)>(steps + 1);
        for (int i = 0; i <= steps; i++)
        {
            double ang = (start + sweep * i / steps) * Math.PI / 180.0;
            var (sx, sy) = _transform.WorldToScreen(
                Coord.FromRaw((int)(cx + r * Math.Cos(ang))), Coord.FromRaw((int)(cy + r * Math.Sin(ang))));
            pts.Add((sx, sy));
        }
        return pts;
    }

    // The rotated screen-space rectangle of a fill, as a closed contour.
    private (double[] X, double[] Y) FillRectScreen(PcbFill f)
    {
        var (x1, y1) = _transform.WorldToScreen(f.Corner1.X, f.Corner1.Y);
        var (x2, y2) = _transform.WorldToScreen(f.Corner2.X, f.Corner2.Y);
        double cx = (x1 + x2) / 2.0, cy = (y1 + y2) / 2.0;
        double hw = Math.Abs(x2 - x1) / 2.0, hh = Math.Abs(y2 - y1) / 2.0;
        double rad = -Finite(f.Rotation) * Math.PI / 180.0;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        double[] lx = { -hw, hw, hw, -hw }, ly = { -hh, -hh, hh, hh };
        var xs = new double[4];
        var ys = new double[4];
        for (int i = 0; i < 4; i++) { xs[i] = cx + lx[i] * cos - ly[i] * sin; ys[i] = cy + lx[i] * sin + ly[i] * cos; }
        return (xs, ys);
    }

    // Outlines a polyline of half-width `halfW` into a closed contour with ROUND end caps (left side
    // forward, a semicircular cap, right side back, a semicircular cap), matching how Altium renders a
    // track's rounded ends. Used to turn solder-mask-layer tracks/arcs into fillable opening shapes — the
    // copper beneath is drawn round-capped too, so the opening must be as well or the gold reads square.
    private static (double[] X, double[] Y) StrokePolyline(IReadOnlyList<(double X, double Y)> pts, double halfW)
    {
        int n = pts.Count;
        if (n == 1)
        {
            // A zero-length track is a round dot.
            var (px, py) = pts[0];
            var dx = new double[CircleSteps];
            var dy = new double[CircleSteps];
            for (int i = 0; i < CircleSteps; i++)
            {
                double a = 2.0 * Math.PI * i / CircleSteps;
                dx[i] = px + halfW * Math.Cos(a);
                dy[i] = py + halfW * Math.Sin(a);
            }
            return (dx, dy);
        }

        var left = new (double X, double Y)[n];
        var right = new (double X, double Y)[n];
        (double X, double Y) tStart = (1, 0), tEnd = (1, 0);
        for (int i = 0; i < n; i++)
        {
            double tx, ty;
            if (i == 0) { tx = pts[1].X - pts[0].X; ty = pts[1].Y - pts[0].Y; }
            else if (i == n - 1) { tx = pts[n - 1].X - pts[n - 2].X; ty = pts[n - 1].Y - pts[n - 2].Y; }
            else { tx = pts[i + 1].X - pts[i - 1].X; ty = pts[i + 1].Y - pts[i - 1].Y; }
            double len = Math.Sqrt(tx * tx + ty * ty);
            if (len < 1e-9) { tx = 1; ty = 0; len = 1; }
            tx /= len; ty /= len;
            if (i == 0) tStart = (tx, ty);
            if (i == n - 1) tEnd = (tx, ty);
            double nx = -ty * halfW, ny = tx * halfW;
            left[i] = (pts[i].X + nx, pts[i].Y + ny);
            right[i] = (pts[i].X - nx, pts[i].Y - ny);
        }

        var contour = new List<(double X, double Y)>(2 * n + 2 * CapSteps);
        for (int i = 0; i < n; i++) contour.Add(left[i]);                 // left rail, forward
        AppendRoundCap(contour, pts[n - 1], halfW, tEnd, forward: true);  // round cap at the far end
        for (int i = n - 1; i >= 0; i--) contour.Add(right[i]);           // right rail, back
        AppendRoundCap(contour, pts[0], halfW, tStart, forward: false);   // round cap at the near end

        var xs = new double[contour.Count];
        var ys = new double[contour.Count];
        for (int i = 0; i < contour.Count; i++) { xs[i] = contour[i].X; ys[i] = contour[i].Y; }
        return (xs, ys);
    }

    // Appends the intermediate points of a 180° round end cap (radius halfW) at `end`, bulging past the
    // endpoint along the tangent (the far end) or against it (the near end). The two rail points bounding
    // the cap are already in the contour, so only the in-between arc points are added.
    private static void AppendRoundCap(List<(double X, double Y)> contour, (double X, double Y) end,
        double halfW, (double X, double Y) tangent, bool forward)
    {
        double normalAngle = Math.Atan2(tangent.X, -tangent.Y); // angle of the +normal (the left rail)
        double start = forward ? normalAngle : normalAngle + Math.PI;
        for (int s = 1; s < CapSteps; s++)
        {
            double a = start - Math.PI * s / CapSteps; // 180° clockwise sweep through the cap
            contour.Add((end.X + halfW * Math.Cos(a), end.Y + halfW * Math.Sin(a)));
        }
    }

    // ── Geometry helpers ────────────────────────────────────────────

    private void ApplyHorizontalFlip(IRenderContext context)
    {
        double cx = _transform.ScreenWidth / 2.0;
        context.Translate(cx, 0);
        context.Scale(-1, 1);
        context.Translate(-cx, 0);
    }

    private (double[] X, double[] Y) MapContour(IReadOnlyList<CoordPoint> points)
    {
        var xs = new double[points.Count];
        var ys = new double[points.Count];
        for (int i = 0; i < points.Count; i++)
            (xs[i], ys[i]) = _transform.WorldToScreen(points[i].X, points[i].Y);
        return (xs, ys);
    }

    // Builds a pad's outline as screen-space contour points, matching how PcbComponentRenderer draws
    // pad shapes (rotation applied as -rotation about the pad centre; Round = obround/circle).
    private (double[] X, double[] Y) PadContour(CoordPoint center, Coord sizeX, Coord sizeY,
        PadShape shape, int cornerRadiusPercent, Coord expansion, double rotationDeg)
    {
        var (cx, cy) = _transform.WorldToScreen(center.X, center.Y);
        var exp = _transform.ScaleValue(expansion);
        double rxBase = _transform.ScaleValue(sizeX) / 2.0;
        double ryBase = _transform.ScaleValue(sizeY) / 2.0;
        double rx = Math.Max(0.5, rxBase + exp);
        double ry = Math.Max(0.5, ryBase + exp);

        var local = shape switch
        {
            PadShape.Rectangular => RectLocal(rx, ry),
            PadShape.Octagonal => OctagonLocal(rx, ry),
            // Corner radius is derived from the UNEXPANDED pad, then offset by the expansion, so the
            // opening is a true uniform outward offset of the copper (they only coincide at 50% otherwise).
            PadShape.RoundedRectangle => RoundedRectLocal(rx, ry,
                Math.Min(rxBase, ryBase) * 2.0 * cornerRadiusPercent / 100.0 + exp),
            _ => RoundedRectLocal(rx, ry, Math.Min(rx, ry)), // Round → obround/circle (already a true offset)
        };
        return Place(local, cx, cy, rotationDeg);
    }

    private (double[] X, double[] Y) CircleContour(double cx, double cy, double r)
    {
        if (r < 0.5) r = 0.5;
        var xs = new double[CircleSteps];
        var ys = new double[CircleSteps];
        for (int i = 0; i < CircleSteps; i++)
        {
            double a = 2.0 * Math.PI * i / CircleSteps;
            xs[i] = cx + r * Math.Cos(a);
            ys[i] = cy + r * Math.Sin(a);
        }
        return (xs, ys);
    }

    private static List<(double X, double Y)> RectLocal(double rx, double ry)
        => new() { (-rx, -ry), (rx, -ry), (rx, ry), (-rx, ry) };

    private static List<(double X, double Y)> OctagonLocal(double rx, double ry)
    {
        var cutX = rx / 3.0;
        var cutY = ry / 3.0;
        return new()
        {
            (rx - cutX, -ry), (rx, -ry + cutY), (rx, ry - cutY), (rx - cutX, ry),
            (-rx + cutX, ry), (-rx, ry - cutY), (-rx, -ry + cutY), (-rx + cutX, -ry),
        };
    }

    private static List<(double X, double Y)> RoundedRectLocal(double rx, double ry, double cornerR)
    {
        cornerR = Math.Max(0, Math.Min(cornerR, Math.Min(rx, ry)));
        if (cornerR <= 0.01) return RectLocal(rx, ry);

        var pts = new List<(double X, double Y)>(4 * (ArcSteps + 1));
        // Four corner centres, swept 90° each, going continuously from -90° through 270°.
        var centers = new (double X, double Y)[]
        {
            (rx - cornerR, -(ry - cornerR)),
            (rx - cornerR, ry - cornerR),
            (-(rx - cornerR), ry - cornerR),
            (-(rx - cornerR), -(ry - cornerR)),
        };
        for (int k = 0; k < 4; k++)
        {
            double baseAngle = (-90 + 90 * k) * Math.PI / 180.0;
            for (int s = 0; s <= ArcSteps; s++)
            {
                double a = baseAngle + (Math.PI / 2.0) * s / ArcSteps;
                pts.Add((centers[k].X + cornerR * Math.Cos(a), centers[k].Y + cornerR * Math.Sin(a)));
            }
        }
        return pts;
    }

    // Coerces a non-finite double (NaN/Infinity, which a corrupt file can carry in a rotation/angle
    // field) to a safe value so it never reaches the render context (Skia drops it; SVG would emit "NaN").
    private static double Finite(double v, double fallback = 0) => double.IsFinite(v) ? v : fallback;

    // Rotates local (screen-space, y-down) points by -rotation about the origin and offsets to (cx,cy),
    // matching PcbComponentRenderer's Translate(cx,cy)+Rotate(-rotation) pad drawing.
    private static (double[] X, double[] Y) Place(List<(double X, double Y)> local, double cx, double cy, double rotationDeg)
    {
        double a = -Finite(rotationDeg) * Math.PI / 180.0;
        double cos = Math.Cos(a), sin = Math.Sin(a);
        var xs = new double[local.Count];
        var ys = new double[local.Count];
        for (int i = 0; i < local.Count; i++)
        {
            var (lx, ly) = local[i];
            xs[i] = cx + lx * cos - ly * sin;
            ys[i] = cy + lx * sin + ly * cos;
        }
        return (xs, ys);
    }
}
