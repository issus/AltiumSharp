namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Options controlling how a PCB board is rendered: which side it is viewed from and which
/// layers are drawn. Pass to the PCB <c>RenderAsync</c> overloads; when omitted, a plain top view
/// with every layer is used.
/// </summary>
public sealed class PcbRenderSettings
{
    /// <summary>
    /// Which side the board is viewed from. <see cref="PcbViewSide.Bottom"/> mirrors the board
    /// horizontally (a true flipped view) and hides top-side copper/silk/paste/solder.
    /// </summary>
    public PcbViewSide ViewSide { get; set; } = PcbViewSide.Top;

    /// <summary>
    /// When set, a layer is drawn only if this predicate returns <c>true</c>. Takes precedence over
    /// the <see cref="ShowMechanical"/>/<see cref="ShowInternalCopper"/> toggles below. Compose with
    /// <see cref="PcbLayerGroups"/>, e.g. <c>l =&gt; !PcbLayerGroups.IsMechanical(l)</c>.
    /// </summary>
    public Func<int, bool>? LayerFilter { get; set; }

    /// <summary>Draw mechanical layers (57-72). Ignored when <see cref="LayerFilter"/> is set.</summary>
    public bool ShowMechanical { get; set; } = true;

    /// <summary>
    /// Draw internal copper: mid-signal layers (2-31) and power/ground planes (39-54). Ignored when
    /// <see cref="LayerFilter"/> is set.
    /// </summary>
    public bool ShowInternalCopper { get; set; } = true;

    /// <summary>
    /// Trim everything outside the physical board outline. When set, primitives that overhang the board
    /// edge — silkscreen text, notes, copper, etc. — are clipped to the board-outline polygon, so the
    /// render shows only what falls within the manufactured board area. Requires a Board6 outline
    /// (<see cref="OriginalCircuit.Altium.Models.Pcb.PcbDocument.GetBoardOutline"/>); a document with no
    /// outline is left unclipped. Default <see langword="false"/>.
    /// </summary>
    public bool ClipToBoardOutline { get; set; }

    /// <summary>
    /// Resolves whether a layer should be drawn, applying <see cref="LayerFilter"/> when present,
    /// otherwise the convenience toggles. View-side culling is handled separately by the renderer.
    /// </summary>
    public bool IsLayerAllowed(int layer)
    {
        if (LayerFilter is not null) return LayerFilter(layer);
        if (!ShowMechanical && PcbLayerGroups.IsMechanical(layer)) return false;
        if (!ShowInternalCopper &&
            (PcbLayerGroups.IsMidCopper(layer) || PcbLayerGroups.IsInternalPlane(layer))) return false;
        return true;
    }

    /// <summary>A plain top view showing every layer (the default).</summary>
    public static PcbRenderSettings Top => new();

    /// <summary>A flipped bottom view showing every (bottom-relevant) layer.</summary>
    public static PcbRenderSettings Bottom => new() { ViewSide = PcbViewSide.Bottom };
}
