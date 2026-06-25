using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// The board's surface finish — the plating applied to exposed copper (mask openings). Drives the
/// default <see cref="PcbRealisticStyle.FinishColor"/> for the bare-pad look in a photorealistic render.
/// </summary>
public enum SurfaceFinish
{
    /// <summary>Hot Air Solder Levelling — a dull silver/tin finish.</summary>
    Hasl,
    /// <summary>Electroless Nickel Immersion Gold — a flat gold finish (the JLC/​OSH-Park default look).</summary>
    Enig,
    /// <summary>Organic Solderability Preservative — leaves bare copper a coppery tone.</summary>
    Osp,
    /// <summary>Immersion Silver — a bright, slightly grey silver.</summary>
    ImmersionSilver,
    /// <summary>Immersion Tin — a pale matte silver.</summary>
    ImmersionTin,
}

/// <summary>
/// Configurable appearance for a photorealistic 2D PCB render (a fab-house / gerber-viewer look),
/// as opposed to the Altium-editor colours of <see cref="PcbRenderSettings"/>. Controls the substrate,
/// copper, solder-mask, silkscreen and surface-finish colours, plus the viewed side and a few toggles.
/// </summary>
/// <remarks>
/// <para>
/// The solder mask is rendered as a translucent sheet (its <see cref="SolderMaskColor"/> carries an
/// alpha channel) painted over the substrate and copper, so mask over copper comes out slightly darker
/// than mask over bare laminate automatically — no separate "mask over copper" colour is needed.
/// </para>
/// <para>
/// Static factory properties provide common presets (e.g. <see cref="GreenEnig"/>); each returns a fresh,
/// freely-mutable instance. Pass an instance to <c>RenderRealisticAsync</c> on the raster or SVG renderer.
/// </para>
/// </remarks>
public sealed class PcbRealisticStyle
{
    /// <summary>
    /// Which physical side of the board to render. <see cref="PcbViewSide.Bottom"/> mirrors the board
    /// horizontally (a true flipped view) and uses the bottom copper/overlay/mask. <see cref="PcbViewSide.Both"/>
    /// is treated as <see cref="PcbViewSide.Top"/> (a 2D photorealistic view shows one side at a time).
    /// </summary>
    public PcbViewSide ViewSide { get; set; } = PcbViewSide.Top;

    /// <summary>Bare laminate (FR-4) colour shown where there is no copper and no mask — a light beige.</summary>
    public EdaColor SubstrateColor { get; set; } = EdaColor.FromRgb(0xC8, 0xB9, 0x8C);

    /// <summary>Bare copper colour, seen (tinted) through the translucent solder mask. A coppery brown.</summary>
    public EdaColor CopperColor { get; set; } = EdaColor.FromRgb(0xB0, 0x77, 0x42);

    /// <summary>
    /// Solder-mask colour <em>including its alpha channel</em> (the mask is translucent). The alpha sets how
    /// strongly the mask tints the copper/substrate beneath; the default green is ~84% opaque.
    /// </summary>
    public EdaColor SolderMaskColor { get; set; } = EdaColor.FromArgb(0xD6, 0x1B, 0x6E, 0x3C);

    /// <summary>Silkscreen (overlay) ink colour. Usually white.</summary>
    public EdaColor SilkscreenColor { get; set; } = EdaColor.FromRgb(0xF2, 0xF2, 0xF2);

    /// <summary>The surface finish, which selects the default <see cref="FinishColor"/> in the presets.</summary>
    public SurfaceFinish Finish { get; set; } = SurfaceFinish.Enig;

    /// <summary>Colour of exposed, plated copper inside mask openings (pads/vias). Defaults to ENIG gold.</summary>
    public EdaColor FinishColor { get; set; } = DefaultFinishColor(SurfaceFinish.Enig);

    /// <summary>Colour of drilled holes (pad/via barrels), painted last. A near-black.</summary>
    public EdaColor HoleColor { get; set; } = EdaColor.FromRgb(0x1A, 0x1A, 0x1A);

    /// <summary>Draw the solder mask sheet. When false the bare copper and substrate are shown.</summary>
    public bool ShowSolderMask { get; set; } = true;

    /// <summary>Draw silkscreen (overlay tracks/arcs/fills/regions and text).</summary>
    public bool ShowSilkscreen { get; set; } = true;

    /// <summary>Paint the surface-finish colour on copper exposed by mask openings (pads/vias).</summary>
    public bool ShowSurfaceFinish { get; set; } = true;

    /// <summary>Punch drilled holes (pad/via barrels) through the stack in <see cref="HoleColor"/>.</summary>
    public bool ShowDrillHoles { get; set; } = true;

    /// <summary>
    /// Trim everything outside the physical board outline. When set, the whole composited stack —
    /// substrate, copper, solder mask, silkscreen and drills — is clipped to the board-outline polygon,
    /// so silk/copper/text overhanging the board edge is removed and the image shows only the
    /// manufactured board area. Requires a Board6 outline; a board with none is left unclipped.
    /// Independent of <see cref="CropToBoardBounds"/> (which only frames the image to the board's
    /// bounding box). Default <see langword="false"/>.
    /// </summary>
    public bool ClipToBoardOutline { get; set; }

    /// <summary>
    /// Solder-mask opening expansion applied to a pad/via copper shape when the object's expansion is
    /// rule-driven (<c>SolderMaskExpansionMode == 1</c>) and no matching design rule is found. A small
    /// positive value (a few mils) leaves the realistic ring of bare laminate around each opening.
    /// </summary>
    public Coord DefaultSolderMaskExpansion { get; set; } = Coord.FromMils(2);

    /// <summary>
    /// Anti-aliasing supersample factor (1–4): the raster renderer draws at this multiple of the output
    /// size and downsamples, for smoother silk/copper edges. Ignored by the SVG renderer. Higher is
    /// sharper but costs N² more memory and time.
    /// </summary>
    public int Supersample { get; set; } = 1;

    /// <summary>
    /// Crop the output to the board's bounding box. When set (the default), the output image takes the
    /// board's aspect ratio fitted within the requested <c>RenderOptions.Width</c>/<c>Height</c> and the
    /// board fills it, so there is no surrounding margin/letterbox — the image <em>is</em> the board's
    /// bounding box. When clear, the requested width/height are used verbatim with a small fit margin.
    /// Only applies when <c>RenderOptions.AutoZoom</c> is enabled.
    /// </summary>
    public bool CropToBoardBounds { get; set; } = true;

    /// <summary>
    /// The default plating colour for a given surface finish. These are kept a touch muted/darker than the
    /// bare-laminate substrate so that, with the whole copper layer drawn in this colour, mask-over-copper
    /// composites slightly darker than mask-over-laminate (the "copper under mask is darker" effect).
    /// </summary>
    public static EdaColor DefaultFinishColor(SurfaceFinish finish) => finish switch
    {
        SurfaceFinish.Hasl => EdaColor.FromRgb(0xB4, 0xB8, 0xBE),           // dull tin/silver
        SurfaceFinish.Enig => EdaColor.FromRgb(0xBE, 0x90, 0x42),           // muted flat gold
        SurfaceFinish.Osp => EdaColor.FromRgb(0xB0, 0x76, 0x44),            // bare copper
        SurfaceFinish.ImmersionSilver => EdaColor.FromRgb(0xC2, 0xC6, 0xCC),// bright silver
        SurfaceFinish.ImmersionTin => EdaColor.FromRgb(0xB2, 0xB4, 0xBA),   // pale matte silver
        _ => EdaColor.FromRgb(0xBE, 0x90, 0x42),
    };

    /// <summary>Returns a copy of this style configured for the given board side.</summary>
    public PcbRealisticStyle For(PcbViewSide side)
    {
        var clone = (PcbRealisticStyle)MemberwiseClone();
        clone.ViewSide = side;
        return clone;
    }

    // ── Presets (each returns a fresh, mutable instance) ─────────────────────────────

    /// <summary>Green solder mask, ENIG (gold) finish, white silkscreen — the default fab look.</summary>
    public static PcbRealisticStyle GreenEnig => new();

    /// <summary>Green solder mask, HASL (silver) finish, white silkscreen.</summary>
    public static PcbRealisticStyle GreenHasl => new()
    {
        Finish = SurfaceFinish.Hasl,
        FinishColor = DefaultFinishColor(SurfaceFinish.Hasl),
    };

    /// <summary>Matte black solder mask, ENIG (gold) finish, white silkscreen.</summary>
    public static PcbRealisticStyle BlackEnig => new()
    {
        SolderMaskColor = EdaColor.FromArgb(0xF0, 0x1C, 0x1E, 0x22),
        Finish = SurfaceFinish.Enig,
        FinishColor = DefaultFinishColor(SurfaceFinish.Enig),
        SilkscreenColor = EdaColor.FromRgb(0xF2, 0xF2, 0xF2),
    };

    /// <summary>Blue solder mask, HASL (silver) finish, white silkscreen.</summary>
    public static PcbRealisticStyle BlueHasl => new()
    {
        SolderMaskColor = EdaColor.FromArgb(0xDC, 0x16, 0x47, 0x9A),
        Finish = SurfaceFinish.Hasl,
        FinishColor = DefaultFinishColor(SurfaceFinish.Hasl),
    };

    /// <summary>Red solder mask, HASL (silver) finish, white silkscreen.</summary>
    public static PcbRealisticStyle RedHasl => new()
    {
        SolderMaskColor = EdaColor.FromArgb(0xDC, 0xA3, 0x24, 0x24),
        Finish = SurfaceFinish.Hasl,
        FinishColor = DefaultFinishColor(SurfaceFinish.Hasl),
    };

    /// <summary>White solder mask, ENIG (gold) finish, black silkscreen.</summary>
    public static PcbRealisticStyle WhiteEnig => new()
    {
        SolderMaskColor = EdaColor.FromArgb(0xE6, 0xE2, 0xE2, 0xDE),
        Finish = SurfaceFinish.Enig,
        FinishColor = DefaultFinishColor(SurfaceFinish.Enig),
        SilkscreenColor = EdaColor.FromRgb(0x20, 0x20, 0x20),
    };

    /// <summary>Purple solder mask (OSH Park style), ENIG (gold) finish, white silkscreen.</summary>
    public static PcbRealisticStyle PurpleEnig => new()
    {
        SolderMaskColor = EdaColor.FromArgb(0xDC, 0x4A, 0x1E, 0x6E),
        Finish = SurfaceFinish.Enig,
        FinishColor = DefaultFinishColor(SurfaceFinish.Enig),
    };
}
