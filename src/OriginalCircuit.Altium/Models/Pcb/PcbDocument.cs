using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Implementation of a PCB document (.PcbDoc file).
/// Contains flat lists of primitives organized by type.
/// </summary>
public sealed class PcbDocument : IPcbDocument
{
    /// <summary>
    /// Diagnostics collected during file reading (warnings about skipped records, parse errors, etc.).
    /// </summary>
    public IReadOnlyList<AltiumDiagnostic> Diagnostics { get; internal set; } = Array.Empty<AltiumDiagnostic>();

    private readonly List<PcbComponent> _components = new();
    private readonly List<PcbPad> _pads = new();
    private readonly List<PcbVia> _vias = new();
    private readonly List<PcbTrack> _tracks = new();
    private readonly List<PcbArc> _arcs = new();
    private readonly List<PcbText> _texts = new();
    private readonly List<PcbFill> _fills = new();
    private readonly List<PcbRegion> _regions = new();
    private readonly List<PcbComponentBody> _componentBodies = new();
    private readonly List<PcbPolygon> _polygons = new();
    private readonly List<PcbNet> _nets = new();
    private readonly List<PcbEmbeddedBoard> _embeddedBoards = new();
    private readonly List<PcbRule> _rules = new();
    private readonly List<PcbObjectClass> _classes = new();
    private readonly List<PcbDifferentialPair> _differentialPairs = new();
    private readonly List<PcbRoom> _rooms = new();

    /// <inheritdoc />
    public IReadOnlyList<IPcbComponent> Components => _components;

    /// <inheritdoc />
    public IReadOnlyList<IPcbPad> Pads => _pads;

    /// <inheritdoc />
    public IReadOnlyList<IPcbVia> Vias => _vias;

    /// <inheritdoc />
    public IReadOnlyList<IPcbTrack> Tracks => _tracks;

    /// <inheritdoc />
    public IReadOnlyList<IPcbArc> Arcs => _arcs;

    /// <inheritdoc />
    public IReadOnlyList<IPcbText> Texts => _texts;

    /// <inheritdoc />
    public IReadOnlyList<IPcbFill> Fills => _fills;

    /// <inheritdoc />
    public IReadOnlyList<IPcbRegion> Regions => _regions;

    /// <inheritdoc />
    public IReadOnlyList<IPcbComponentBody> ComponentBodies => _componentBodies;

    /// <summary>
    /// All polygons (copper pours) in this document.
    /// </summary>
    public IReadOnlyList<PcbPolygon> Polygons => _polygons;

    /// <summary>
    /// All nets in this document.
    /// </summary>
    public IReadOnlyList<PcbNet> Nets => _nets;

    /// <summary>
    /// All embedded boards in this document.
    /// </summary>
    public IReadOnlyList<PcbEmbeddedBoard> EmbeddedBoards => _embeddedBoards;

    /// <summary>
    /// The file path this document was loaded from, when opened from a path (null for stream/in-memory
    /// documents). Lets relative references — such as a panel's embedded sub-board paths — be resolved
    /// against the document's own directory.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// All design rules in this document.
    /// </summary>
    public IReadOnlyList<PcbRule> Rules => _rules;

    /// <summary>
    /// All object classes in this document.
    /// </summary>
    public IReadOnlyList<PcbObjectClass> Classes => _classes;

    private readonly List<PcbSignalClass> _signalClasses = new();
    /// <summary>The document's xSignal / net classes (SignalClasses storage).</summary>
    public IReadOnlyList<PcbSignalClass> SignalClasses => _signalClasses;
    /// <summary>Adds a signal class.</summary>
    public void AddSignalClass(PcbSignalClass signalClass) => _signalClasses.Add(signalClass);

    private readonly List<PcbSmartUnion> _smartUnions = new();
    /// <summary>The document's union groupings (SmartUnions storage).</summary>
    public IReadOnlyList<PcbSmartUnion> SmartUnions => _smartUnions;
    /// <summary>Adds a smart union.</summary>
    public void AddSmartUnion(PcbSmartUnion union) => _smartUnions.Add(union);

    private readonly List<PcbUnionName> _unionNames = new();
    /// <summary>Named unions (UnionNames storage), keyed by union index.</summary>
    public IReadOnlyList<PcbUnionName> UnionNames => _unionNames;
    /// <summary>Adds a union name.</summary>
    public void AddUnionName(PcbUnionName name) => _unionNames.Add(name);

    private readonly List<PcbRegion> _boardRegions = new();
    /// <summary>Board-shape regions (BoardRegions storage) — same record format as ordinary regions.</summary>
    public IReadOnlyList<PcbRegion> BoardRegions => _boardRegions;
    /// <summary>Adds a board region.</summary>
    public void AddBoardRegion(PcbRegion region) => _boardRegions.Add(region);

    /// <summary>The document-level <c>PrimitiveGuids</c> object-GUID cache (typed records).</summary>
    public List<PcbPrimitiveGuid> PrimitiveGuids { get; } = new();

    /// <summary>Shape-based regions (ShapeBasedRegions6) — regions with arc-capable extended vertices.</summary>
    public List<PcbShapeBasedRegion> ShapeBasedRegions { get; } = new();

    /// <summary>Shape-based component bodies (ShapeBasedComponentBodies6) — same extended-vertex format.</summary>
    public List<PcbShapeBasedRegion> ShapeBasedComponentBodies { get; } = new();

    /// <summary>Per-component user parameter groups (PrimitiveParameters), as typed param records.</summary>
    public List<PcbParameterRecord> PrimitiveParameters { get; } = new();
    /// <summary>Captured PrimitiveParameters header value (= component count × 3); preserved for round-trip.</summary>
    internal int PrimitiveParametersHeader { get; set; }

    /// <summary>Per-primitive solder/paste-mask expansion overrides (ExtendedPrimitiveInformation).</summary>
    public List<PcbExtendedPrimitiveInfo> ExtendedPrimitiveInfo { get; } = new();

    /// <summary>
    /// Editor/DRC parameter-block storages (Design Rule Checker Options6, Advanced Placer Options6,
    /// Pin Swap Options6, SimbeorCacheSection, TMatchedNetLengthsViolation, CustomShapes,
    /// WaivedViolations, PinPairsSection), modeled as typed parameter records.
    /// </summary>
    public List<PcbNamedParameterStorage> NamedParameterStorages { get; } = new();

    /// <summary>The document-level <c>UniqueIDPrimitiveInformation</c> short-id tokens (typed records).</summary>
    public List<PcbPrimitiveUniqueId> PrimitiveUniqueIds { get; } = new();

    /// <summary>The root <c>FileVersionInfo</c> version-message cache, modeled as a typed record.</summary>
    public PcbFileVersionInfo FileVersionInfo { get; set; } = new();

    /// <summary>The root <c>LayerKindMapping</c> (typed; null when the source had none).</summary>
    public PcbLayerKindMapping? LayerKindMapping { get; set; }

    /// <summary>The root <c>PadViaLibrary</c> identity (typed; null when absent).</summary>
    public PcbPadViaLibrary? PadViaLibrary { get; set; }

    /// <summary>The root <c>PadViaLibraryCache</c> (param block + opaque binary template cache).</summary>
    public PcbPadViaLibrary? PadViaLibraryCache { get; set; }

    /// <summary>
    /// All differential pairs in this document.
    /// </summary>
    public IReadOnlyList<PcbDifferentialPair> DifferentialPairs => _differentialPairs;

    /// <summary>
    /// All rooms in this document.
    /// </summary>
    public IReadOnlyList<PcbRoom> Rooms => _rooms;

    /// <summary>
    /// Board-level parameters from the Board6 storage.
    /// Contains layer stacks, board outline, and other board metadata.
    /// When null, Board6 is not written (optional for basic documents).
    /// </summary>
    public Dictionary<string, string>? BoardParameters { get; set; }

    private IReadOnlyList<CoordPoint>? _boardOutline;

    /// <summary>
    /// The physical board outline as a closed polygon of world-space points, parsed from the
    /// Board6 parameter block (arc edges are tessellated). Empty when no outline is defined.
    /// </summary>
    public IReadOnlyList<CoordPoint> GetBoardOutline()
        => _boardOutline ??= PcbBoardOutline.Parse(BoardParameters);

    /// <summary>
    /// Board-level parameters as an ordered key/value list — the canonical, authorable representation
    /// of the Board6 record. It preserves key order and the duplicate keys that <see cref="BoardParameters"/>
    /// (a flat dictionary convenience view) collapses: the block concatenates a main board-parameter
    /// section, a board-region sub-block, and repeated <c>RECORD=</c>-delimited layer-stack/split-plane
    /// sub-records, so several keys (the common <c>SELECTION..UNIONINDEX</c> prefix, <c>SPLITLINECOUNT</c>,
    /// <c>RECORD</c>) appear more than once. When non-null this list is written verbatim (byte-faithful
    /// round-trip); set it to author a Board6 with exact ordering/duplicates. When null, the writer
    /// falls back to <see cref="BoardParameters"/>.
    /// </summary>
    public List<KeyValuePair<string, string>>? BoardParametersOrdered { get; set; }

    /// <summary>
    /// Names of root storages that were present in the source file, so the writer can reproduce
    /// known storages that exist but are empty (e.g. an empty DifferentialPairs6) rather than
    /// omitting them.
    /// </summary>
    internal HashSet<string> PresentStorages { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Per-document GUID from the modern <c>FileHeaderSix</c> stream. Round-tripped from a loaded
    /// document; a freshly created document defaults to a new GUID so it is authored with the
    /// identity Altium expects. Set to <c>null</c> only when a loaded document had no
    /// <c>FileHeaderSix</c> stream (so the writer reproduces its absence exactly).
    /// </summary>
    public Guid? FileGuid { get; set; } = Guid.NewGuid();

    private PcbLayerStack? _layerStackCache;

    /// <summary>
    /// Layer stack parsed from Board6 parameters.
    /// Lazily computed on first access. Returns null if no layer data is present.
    /// </summary>
    public PcbLayerStack? LayerStack => _layerStackCache ??= PcbLayerStack.FromBoardParameters(BoardParameters);

    private PcbStackup? _stackupCache;

    /// <summary>
    /// The physical board stack-up — an ordered (top-to-bottom) list of copper, dielectric, solder-mask
    /// and silkscreen layers with true millimetre thicknesses and absolute Z positions — parsed from the
    /// modern <c>V9_STACK_LAYER</c> Board6 parameters. Lazily computed; returns <see langword="null"/>
    /// when the file carries no usable stack data (callers can fall back to
    /// <see cref="PcbStackup.CreateDefault"/>). Unlike <see cref="LayerStack"/>, this models the
    /// dielectric cores and exposes per-layer thickness, which is what 3D rendering needs.
    /// </summary>
    public PcbStackup? GetStackup() => _stackupCache ??= PcbStackup.FromBoardParameters(BoardParameters);

    /// <summary>
    /// Additional OLE storages/streams preserved for round-trip fidelity.
    /// Key format: "StorageName/StreamName" -> byte data.
    /// </summary>
    public Dictionary<string, byte[]>? AdditionalStreams { get; set; }

    /// <summary>
    /// Embedded 3D STEP models referenced by component bodies (via <see cref="PcbComponentBody.ModelId"/>),
    /// modeled from the root <c>Models</c> storage (<c>Models/Data</c> metadata + numbered
    /// <c>Models/&lt;n&gt;</c> zlib STEP payloads). Mirrors <see cref="PcbLibrary.Models"/>.
    /// <para>
    /// Populated by the reader and reconstructed by the writer, so models added here are written to the
    /// file. The metadata round-trips byte-for-byte; the numbered STEP streams round-trip their decoded
    /// content (the zlib bytes themselves may differ, the accepted library-wide limitation). Empty when
    /// the document carries no embedded models.
    /// </para>
    /// </summary>
    public List<PcbModel> Models { get; } = new();

    /// <summary>True when the source file carried a <c>Models</c> storage (so the writer reproduces it
    /// even when empty). Set by the reader.</summary>
    internal bool ModelsStoragePresent { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var bounds = CoordRect.Empty;
            foreach (var pad in _pads) bounds = bounds.Union(pad.Bounds);
            foreach (var via in _vias) bounds = bounds.Union(via.Bounds);
            foreach (var track in _tracks) bounds = bounds.Union(track.Bounds);
            foreach (var arc in _arcs) bounds = bounds.Union(arc.Bounds);
            foreach (var text in _texts) bounds = bounds.Union(text.Bounds);
            foreach (var fill in _fills) bounds = bounds.Union(fill.Bounds);
            foreach (var region in _regions) bounds = bounds.Union(region.Bounds);
            foreach (var body in _componentBodies) bounds = bounds.Union(body.Bounds);
            // Components carry most of a board's primitives, so include them too.
            foreach (var component in _components) bounds = bounds.Union(component.Bounds);
            // Embedded board placements (panels): include the full array extent.
            foreach (var eb in _embeddedBoards)
            {
                if (eb.X1Location == eb.X2Location && eb.Y1Location == eb.Y2Location) continue;
                var dx = Coord.FromRaw(eb.ColSpacing.ToRaw() * Math.Max(0, eb.ColCount - 1));
                var dy = Coord.FromRaw(eb.RowSpacing.ToRaw() * Math.Max(0, eb.RowCount - 1));
                bounds = bounds.Union(new CoordRect(
                    new CoordPoint(eb.X1Location, eb.Y1Location),
                    new CoordPoint(eb.X2Location + dx, eb.Y2Location + dy)));
            }
            return bounds;
        }
    }

    /// <summary>
    /// The world-space rectangle a renderer should frame the whole board to when auto-zooming.
    /// Unlike <see cref="Bounds"/> (which measures only placed/free primitives and components),
    /// this also folds in the physical board outline from <see cref="GetBoardOutline"/>, so the
    /// board edge — which usually extends past the outermost copper (edge clearance, mounting
    /// margins, tabs) — is fully visible rather than cropped.
    /// </summary>
    /// <remarks>
    /// Falls back to <see cref="Bounds"/> when the document has no Board6 outline, so boards built
    /// from primitives alone still frame correctly. When an outline is present the result is always
    /// non-degenerate, which also fixes the outline-only board that would otherwise render tiny
    /// (<see cref="CoordRect.Empty"/> makes <c>CoordTransform.AutoZoom</c> early-return).
    /// <para>
    /// Primitives lying <em>entirely</em> outside the board outline — off-board notes, title blocks
    /// and (auto-placed) hidden component designators/comments — are excluded, so a board that carries
    /// such off-sheet clutter still frames tightly to the physical board instead of zooming far out.
    /// Content that touches the board (the usual edge overhang: clearance, tabs, mounting margins) is
    /// kept, since its bounding box still intersects the outline's.
    /// </para>
    /// </remarks>
    public CoordRect GetFramingBounds()
    {
        var outline = GetBoardOutline();
        if (outline.Count == 0) return Bounds;

        // Seed from the outline points (Union(IEnumerable) avoids the origin-sentinel pull), then widen
        // to include only content on or at the board — anything whose bounds touch the outline's box.
        var outlineBounds = CoordRect.Union(outline.Select(p => new CoordRect(p, p)));
        var result = outlineBounds;
        void Fold(CoordRect r) { if (r.Intersects(outlineBounds)) result = result.Union(r); }

        foreach (var pad in _pads) Fold(pad.Bounds);
        foreach (var via in _vias) Fold(via.Bounds);
        foreach (var track in _tracks) Fold(track.Bounds);
        foreach (var arc in _arcs) Fold(arc.Bounds);
        foreach (var text in _texts) Fold(text.Bounds);
        foreach (var fill in _fills) Fold(fill.Bounds);
        foreach (var region in _regions) Fold(region.Bounds);
        foreach (var body in _componentBodies) Fold(body.Bounds);
        foreach (var component in _components) Fold(component.Bounds);
        // Embedded-board placements (panels): the tiled sub-board array is real board content, so fold in
        // its full extent the same way the Bounds getter does (filtered, so a stray off-panel placement
        // still can't blow up the frame).
        foreach (var eb in _embeddedBoards)
        {
            if (eb.X1Location == eb.X2Location && eb.Y1Location == eb.Y2Location) continue;
            var dx = Coord.FromRaw(eb.ColSpacing.ToRaw() * Math.Max(0, eb.ColCount - 1));
            var dy = Coord.FromRaw(eb.RowSpacing.ToRaw() * Math.Max(0, eb.RowCount - 1));
            Fold(new CoordRect(
                new CoordPoint(eb.X1Location, eb.Y1Location),
                new CoordPoint(eb.X2Location + dx, eb.Y2Location + dy)));
        }
        return result;
    }

    /// <summary>
    /// Adds a component to the document.
    /// </summary>
    public void AddComponent(PcbComponent component) => _components.Add(component);

    void IPcbDocument.AddComponent(IPcbComponent component)
    {
        if (component is not PcbComponent c) throw new ArgumentException($"Expected {nameof(PcbComponent)}", nameof(component));
        _components.Add(c);
    }

    bool IPcbDocument.RemoveComponent(IPcbComponent component) => component is PcbComponent c && _components.Remove(c);

    void IPcbDocument.AddPad(IPcbPad pad)
    {
        if (pad is not PcbPad p) throw new ArgumentException($"Expected {nameof(PcbPad)}", nameof(pad));
        _pads.Add(p);
    }

    bool IPcbDocument.RemovePad(IPcbPad pad) => pad is PcbPad p && _pads.Remove(p);

    void IPcbDocument.AddVia(IPcbVia via)
    {
        if (via is not PcbVia v) throw new ArgumentException($"Expected {nameof(PcbVia)}", nameof(via));
        _vias.Add(v);
    }

    bool IPcbDocument.RemoveVia(IPcbVia via) => via is PcbVia v && _vias.Remove(v);

    void IPcbDocument.AddTrack(IPcbTrack track)
    {
        if (track is not PcbTrack t) throw new ArgumentException($"Expected {nameof(PcbTrack)}", nameof(track));
        _tracks.Add(t);
    }

    bool IPcbDocument.RemoveTrack(IPcbTrack track) => track is PcbTrack t && _tracks.Remove(t);

    void IPcbDocument.AddArc(IPcbArc arc)
    {
        if (arc is not PcbArc a) throw new ArgumentException($"Expected {nameof(PcbArc)}", nameof(arc));
        _arcs.Add(a);
    }

    bool IPcbDocument.RemoveArc(IPcbArc arc) => arc is PcbArc a && _arcs.Remove(a);

    void IPcbDocument.AddText(IPcbText text)
    {
        if (text is not PcbText t) throw new ArgumentException($"Expected {nameof(PcbText)}", nameof(text));
        _texts.Add(t);
    }

    bool IPcbDocument.RemoveText(IPcbText text) => text is PcbText t && _texts.Remove(t);

    void IPcbDocument.AddRegion(IPcbRegion region)
    {
        if (region is not PcbRegion r) throw new ArgumentException($"Expected {nameof(PcbRegion)}", nameof(region));
        _regions.Add(r);
    }

    bool IPcbDocument.RemoveRegion(IPcbRegion region) => region is PcbRegion r && _regions.Remove(r);

    /// <summary>
    /// Adds a pad to the document.
    /// </summary>
    public void AddPad(PcbPad pad) => _pads.Add(pad);

    /// <summary>
    /// Adds a via to the document.
    /// </summary>
    public void AddVia(PcbVia via) => _vias.Add(via);

    /// <summary>
    /// Adds a track to the document.
    /// </summary>
    public void AddTrack(PcbTrack track) => _tracks.Add(track);

    /// <summary>
    /// Adds an arc to the document.
    /// </summary>
    public void AddArc(PcbArc arc) => _arcs.Add(arc);

    /// <summary>
    /// Adds a text object to the document.
    /// </summary>
    public void AddText(PcbText text) => _texts.Add(text);

    /// <summary>
    /// Adds a fill to the document.
    /// </summary>
    public void AddFill(PcbFill fill) => _fills.Add(fill);

    /// <summary>
    /// Adds a region to the document.
    /// </summary>
    public void AddRegion(PcbRegion region) => _regions.Add(region);

    /// <summary>
    /// Adds a component body to the document.
    /// </summary>
    public void AddComponentBody(PcbComponentBody body) => _componentBodies.Add(body);

    /// <summary>
    /// Adds a polygon to the document.
    /// </summary>
    public void AddPolygon(PcbPolygon polygon) => _polygons.Add(polygon);

    /// <summary>
    /// Adds a net to the document.
    /// </summary>
    public void AddNet(PcbNet net) => _nets.Add(net);

    /// <summary>
    /// Adds an embedded board to the document.
    /// </summary>
    public void AddEmbeddedBoard(PcbEmbeddedBoard board) => _embeddedBoards.Add(board);

    /// <summary>
    /// Adds a rule to the document.
    /// </summary>
    public void AddRule(PcbRule rule) => _rules.Add(rule);

    /// <summary>
    /// Adds an object class to the document.
    /// </summary>
    public void AddClass(PcbObjectClass objectClass) => _classes.Add(objectClass);

    /// <summary>
    /// Adds a differential pair to the document.
    /// </summary>
    public void AddDifferentialPair(PcbDifferentialPair pair) => _differentialPairs.Add(pair);

    /// <summary>
    /// Adds a room to the document.
    /// </summary>
    public void AddRoom(PcbRoom room) => _rooms.Add(room);

    /// <inheritdoc />
    public async ValueTask SaveAsync(string path, OriginalCircuit.Eda.Models.SaveOptions? options = null, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await SaveAsync(stream, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(Stream stream, OriginalCircuit.Eda.Models.SaveOptions? options = null, CancellationToken cancellationToken = default)
    {
        var writer = new PcbDocWriter();
        await writer.WriteAsync(this, stream, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
