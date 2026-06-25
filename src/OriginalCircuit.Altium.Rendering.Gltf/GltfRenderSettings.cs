namespace OriginalCircuit.Altium.Rendering.Gltf;

/// <summary>
/// Surface finish applied to exposed copper, controlling its appearance in the rendered board.
/// </summary>
public enum GltfCopperFinish
{
    /// <summary>Hot air solder levelling — dull tin/grey.</summary>
    Hasl,

    /// <summary>Electroless nickel / immersion gold — bright gold.</summary>
    Enig,

    /// <summary>Bare copper, no finish.</summary>
    BareCopper,
}

/// <summary>
/// Output container for the generated glTF model.
/// </summary>
public enum GltfOutputFormat
{
    /// <summary>Infer from the destination file extension (.glb =&gt; binary, otherwise JSON).</summary>
    Auto,

    /// <summary>Binary glTF (.glb), single self-contained file.</summary>
    Glb,

    /// <summary>JSON glTF (.gltf) with the buffer embedded as a base64 data URI.</summary>
    GltfEmbedded,

    /// <summary>JSON glTF (.gltf) with an external sibling .bin buffer.</summary>
    GltfExternal,
}

/// <summary>
/// Settings controlling how a <c>PcbDocument</c> is converted to a glTF 3D model. All board
/// features are included by default; individual layers/roles can be switched off, and components
/// can be excluded for a bare-board model.
/// </summary>
public sealed class GltfRenderSettings
{
    /// <summary>Emit the FR4 laminate substrate slab. Default <see langword="true"/>.</summary>
    public bool IncludeSubstrate { get; set; } = true;

    /// <summary>Emit copper layers (signal layers 1-32). Default <see langword="true"/>.</summary>
    public bool IncludeCopper { get; set; } = true;

    /// <summary>Emit solder mask layers (top/bottom). Default <see langword="true"/>.</summary>
    public bool IncludeSolderMask { get; set; } = true;

    /// <summary>Emit silkscreen / overlay layers (top/bottom). Default <see langword="true"/>.</summary>
    public bool IncludeSilkscreen { get; set; } = true;

    /// <summary>Emit plated holes and via barrels. Default <see langword="true"/>.</summary>
    public bool IncludeDrills { get; set; } = true;

    /// <summary>Emit placed component 3D bodies (embedded STEP models). Default <see langword="true"/>.</summary>
    public bool IncludeComponents { get; set; } = true;

    /// <summary>
    /// Trim the flat board layers — copper and silkscreen, including their text and barcodes — to the
    /// physical board outline, so geometry overhanging the board edge is removed and the model shows
    /// only what falls within the manufactured board area. The substrate, solder mask and drills are
    /// already bounded by the outline; placed component 3D bodies are <em>not</em> clipped, since a part
    /// (e.g. a connector or card-edge) may legitimately overhang the edge. Requires a board outline;
    /// a board with none is left unclipped. Default <see langword="false"/>.
    /// </summary>
    public bool ClipToBoardOutline { get; set; }

    /// <summary>
    /// Optional explicit set of copper layer IDs (1-32) to include. When <see langword="null"/>,
    /// every copper layer present in the board's layer stack is included.
    /// </summary>
    public IReadOnlyCollection<int>? CopperLayerFilter { get; set; }

    /// <summary>Surface finish used to colour exposed copper. Default <see cref="GltfCopperFinish.Enig"/>.</summary>
    public GltfCopperFinish CopperFinish { get; set; } = GltfCopperFinish.Enig;

    /// <summary>
    /// Maximum chord deviation, in millimetres, when tessellating arcs and circles (board outline,
    /// rounded pads, drill barrels). Smaller is finer. Default 0.05 mm.
    /// </summary>
    public double ArcChordToleranceMm { get; set; } = 0.05;

    /// <summary>
    /// Chord tolerance, in millimetres, passed to the STEP tessellator for component 3D bodies.
    /// Default 0.05 mm.
    /// </summary>
    public double ComponentChordToleranceMm { get; set; } = 0.05;

    /// <summary>
    /// Fallback total board thickness, in millimetres, used when the file carries no usable layer
    /// stack thickness data. Default 1.6 mm (a standard 2-layer FR4 board).
    /// </summary>
    public double FallbackBoardThicknessMm { get; set; } = 1.6;

    /// <summary>
    /// Output container format. <see cref="GltfOutputFormat.Auto"/> infers it from the destination
    /// path's extension. When rendering to a <see cref="System.IO.Stream"/>, <c>Auto</c> means GLB.
    /// </summary>
    public GltfOutputFormat Format { get; set; } = GltfOutputFormat.Auto;

    /// <summary>
    /// Resolves an embedded-board reference (a panel's <c>EmbeddedBoards6</c> <c>DocumentPath</c>) to the
    /// referenced sub-board, so panels can be composited from their tiled sub-boards. Returns the loaded
    /// document, or null to skip that reference. When rendering from a file path, a default resolver that
    /// loads sibling <c>.PcbDoc</c> files from the panel's directory is used if this is left null; when
    /// rendering from a stream there is no directory, so the caller must supply one to see the sub-boards.
    /// </summary>
    public Func<string, OriginalCircuit.Altium.Models.Pcb.PcbDocument?>? EmbeddedBoardResolver { get; set; }

    /// <summary>A shallow copy, so a derived settings can be tweaked without mutating the caller's.</summary>
    internal GltfRenderSettings Clone() => (GltfRenderSettings)MemberwiseClone();
}
