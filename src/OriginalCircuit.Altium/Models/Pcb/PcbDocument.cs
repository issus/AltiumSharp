using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Primitives;
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
    /// All design rules in this document.
    /// </summary>
    public IReadOnlyList<PcbRule> Rules => _rules;

    /// <summary>
    /// All object classes in this document.
    /// </summary>
    public IReadOnlyList<PcbObjectClass> Classes => _classes;

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

    private PcbLayerStack? _layerStackCache;

    /// <summary>
    /// Layer stack parsed from Board6 parameters.
    /// Lazily computed on first access. Returns null if no layer data is present.
    /// </summary>
    public PcbLayerStack? LayerStack => _layerStackCache ??= PcbLayerStack.FromBoardParameters(BoardParameters);

    /// <summary>
    /// Additional OLE storages/streams preserved for round-trip fidelity.
    /// Key format: "StorageName/StreamName" -> byte data.
    /// </summary>
    public Dictionary<string, byte[]>? AdditionalStreams { get; set; }

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
            return bounds;
        }
    }

    /// <summary>
    /// Adds a component to the document.
    /// </summary>
    public void AddComponent(PcbComponent component) => _components.Add(component);

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
    public async ValueTask SaveAsync(string path, SaveOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new SaveOptions();
        var mode = options.Overwrite ? FileMode.Create : FileMode.CreateNew;
        await using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await SaveAsync(stream, options, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(Stream stream, SaveOptions? options = null, CancellationToken cancellationToken = default)
    {
        var writer = new PcbDocWriter();
        await writer.WriteAsync(this, stream, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
