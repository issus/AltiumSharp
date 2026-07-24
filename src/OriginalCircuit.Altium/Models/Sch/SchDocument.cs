using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Implementation of a schematic document (.SchDoc file).
/// Contains a flat list of primitives where components own their children
/// via OWNERINDEX relationships.
/// </summary>
public sealed class SchDocument : ISchDocument
{
    /// <summary>
    /// Diagnostics collected during file reading (warnings about skipped records, parse errors, etc.).
    /// </summary>
    public IReadOnlyList<AltiumDiagnostic> Diagnostics { get; internal set; } = Array.Empty<AltiumDiagnostic>();

    private readonly List<SchComponent> _components = new();
    private readonly List<SchWire> _wires = new();
    private readonly List<SchTemplate> _templates = new();
    private readonly List<SchNote> _notes = new();
    private readonly List<SchHyperlink> _hyperlinks = new();
    private readonly List<SchCompileMask> _compileMasks = new();
    private readonly List<SchHarnessConnector> _harnessConnectors = new();
    private readonly List<SchHarnessEntry> _harnessEntries = new();
    private readonly List<SchHarnessType> _harnessTypes = new();
    private readonly List<SchSignalHarness> _signalHarnesses = new();
    private readonly List<SchNetLabel> _netLabels = new();
    private readonly List<SchJunction> _junctions = new();
    private readonly List<SchPowerObject> _powerObjects = new();
    private readonly List<SchLabel> _labels = new();
    private readonly List<SchParameter> _parameters = new();
    private readonly List<SchLine> _lines = new();
    private readonly List<SchRectangle> _rectangles = new();
    private readonly List<SchPolygon> _polygons = new();
    private readonly List<SchPolyline> _polylines = new();
    private readonly List<SchArc> _arcs = new();
    private readonly List<SchBezier> _beziers = new();
    private readonly List<SchEllipse> _ellipses = new();
    private readonly List<SchRoundedRectangle> _roundedRectangles = new();
    private readonly List<SchPie> _pies = new();
    private readonly List<SchTextFrame> _textFrames = new();
    private readonly List<SchImage> _images = new();
    private readonly List<SchSymbol> _symbols = new();
    private readonly List<SchEllipticalArc> _ellipticalArcs = new();
    private readonly List<SchNoErc> _noErcs = new();
    private readonly List<SchBusEntry> _busEntries = new();
    private readonly List<SchBus> _buses = new();
    private readonly List<SchPort> _ports = new();
    private readonly List<SchSheetSymbol> _sheetSymbols = new();
    private readonly List<SchSheetEntry> _sheetEntries = new();
    private readonly List<SchBlanket> _blankets = new();
    private readonly List<SchParameterSet> _parameterSets = new();

    /// <summary>
    /// Document header parameters from the FileHeader record (RECORD=31 equivalent).
    /// Contains page size, font definitions, grid settings, and other document metadata.
    /// When null, defaults (HEADER + WEIGHT) are written for new files.
    /// </summary>
    public Dictionary<string, string>? HeaderParameters { get; set; }

    /// <summary>
    /// Sheet settings record (RECORD=31) containing font definitions, grid/border/title-block settings.
    /// Preserved as raw parameters for round-trip fidelity.
    /// </summary>
    public Dictionary<string, string>? SheetSettings { get; set; }

    /// <summary>
    /// Additional OLE storages/streams preserved for round-trip fidelity.
    /// Key format: "StreamName" for root streams, "StorageName/StreamName" for nested streams.
    /// </summary>
    public Dictionary<string, byte[]>? AdditionalStreams { get; set; }

    /// <summary>
    /// Opaque (unmodeled) records preserved for round-trip fidelity.
    /// Each entry is the raw parameter dictionary from an unrecognized record type.
    /// </summary>
    public List<Dictionary<string, string>> OpaqueRecords { get; } = new();

    /// <summary>
    /// The FileHeader document-header block as an ordered, authorable key/value list — the canonical
    /// representation, preserving key order and any duplicate keys the <see cref="HeaderParameters"/>
    /// dictionary collapses. Emitted verbatim when set; the typed model / <see cref="HeaderParameters"/>
    /// remain the from-scratch authoring surface.
    /// </summary>
    public List<KeyValuePair<string, string>>? HeaderParametersOrdered { get; set; }

    /// <summary>
    /// Every FileHeader record (after the document header) captured in original record order, each
    /// <em>linked to its typed model object</em> (the primitive/component it produced, or a
    /// <see cref="SchRawRecord"/> holder for sheet/marker/opaque records). The writer walks this list to
    /// reproduce the exact on-disk order, emitting each record's captured ordered parameters so unmodeled
    /// parameters round-trip exactly. This replaces the former detached whole-document record blob: the
    /// captured parameters now live with the model object, matching the PCB and SchLib pattern. Null (or
    /// after binary-pin records) falls back to typed-model serialization that supports from-scratch authoring.
    /// This is the authorable, byte-faithful representation of the document's record stream.
    /// </summary>
    public List<SchOrderedRecord>? ReadOrderedRecords { get; set; }

    /// <summary>
    /// Count of modeled top-level primitives captured immediately after the document was read. The
    /// writer compares it against the live count to decide whether the byte-faithful
    /// <see cref="ReadOrderedRecords"/> fast path is still valid: if a primitive was added or removed after
    /// load the counts differ and the writer falls back to typed serialization so the edit is not
    /// dropped. Null for documents built from scratch.
    /// </summary>
    internal int? LoadedPrimitiveCount { get; set; }

    /// <summary>
    /// Font table parsed from the sheet settings (RECORD=31) FontID table, used for rendering text.
    /// </summary>
    public IReadOnlyList<SchFontDefinition> Fonts { get; internal set; } = Array.Empty<SchFontDefinition>();

    /// <summary>
    /// The document's system (default) font — a 1-based index into <see cref="Fonts"/>. Objects that
    /// don't carry their own FontId (pins, sheet entries) render with this font, not the first entry.
    /// From the <c>SystemFont</c> sheet-settings value; defaults to 1.
    /// </summary>
    public int SystemFont { get; set; } = 1;

    /// <summary>
    /// True when harness records were read from the "Additional" OLE stream (kept verbatim in
    /// <see cref="AdditionalStreams"/>). The writer then skips re-emitting them to FileHeader so they
    /// aren't duplicated on round-trip.
    /// </summary>
    public bool HarnessesInAdditionalStream { get; set; }

    /// <summary>
    /// Parsed sheet (page) settings: paper size, orientation, border and title-block flags.
    /// Derived from <see cref="SheetSettings"/>; defaults to a landscape A4 sheet when absent.
    /// </summary>
    public SchSheetInfo SheetInfo => SchSheetInfo.Parse(SheetSettings);

    /// <summary>
    /// Source file name (e.g. <c>DAC.SchDoc</c>) when the document was read from a file.
    /// Used to resolve the <c>=DocumentName</c> title-block special string.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Source file full path when the document was read from a file.
    /// Used to resolve the <c>=DocumentFullPathAndName</c> title-block special string.
    /// </summary>
    public string? FilePath { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ISchComponent> Components => _components;

    /// <inheritdoc />
    public IReadOnlyList<ISchWire> Wires => _wires;

    /// <summary>Sheet-template references (record type 39) applied to this document.</summary>
    public IReadOnlyList<SchTemplate> Templates => _templates;

    /// <summary>Design notes (record type 209) on this document.</summary>
    public IReadOnlyList<SchNote> Notes => _notes;

    /// <summary>Hyperlinks (record type 226) on this document.</summary>
    public IReadOnlyList<SchHyperlink> Hyperlinks => _hyperlinks;

    /// <summary>Compile masks (record type 211) on this document.</summary>
    public IReadOnlyList<SchCompileMask> CompileMasks => _compileMasks;

    /// <summary>Harness connectors (record type 215) on this document.</summary>
    public IReadOnlyList<SchHarnessConnector> HarnessConnectors => _harnessConnectors;

    /// <summary>Harness entries (record type 216) on this document; reference a connector by owner index.</summary>
    public IReadOnlyList<SchHarnessEntry> HarnessEntries => _harnessEntries;

    /// <summary>Harness type labels (record type 217) on this document; reference a connector by owner index.</summary>
    public IReadOnlyList<SchHarnessType> HarnessTypes => _harnessTypes;

    /// <summary>Signal harnesses (record type 218): the bundle wires connecting harness connectors.</summary>
    public IReadOnlyList<SchSignalHarness> SignalHarnesses => _signalHarnesses;

    /// <inheritdoc />
    public IReadOnlyList<ISchNetLabel> NetLabels => _netLabels;

    /// <inheritdoc />
    public IReadOnlyList<ISchJunction> Junctions => _junctions;

    /// <inheritdoc />
    public IReadOnlyList<ISchPowerObject> PowerObjects => _powerObjects;

    /// <inheritdoc />
    public IReadOnlyList<ISchLabel> Labels => _labels;

    /// <inheritdoc />
    public IReadOnlyList<ISchParameter> Parameters => _parameters;

    /// <summary>
    /// All lines in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchLine> Lines => _lines;

    /// <summary>
    /// All rectangles in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchRectangle> Rectangles => _rectangles;

    /// <summary>
    /// All polygons in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchPolygon> Polygons => _polygons;

    /// <summary>
    /// All polylines in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchPolyline> Polylines => _polylines;

    /// <summary>
    /// All arcs in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchArc> Arcs => _arcs;

    /// <summary>
    /// All beziers in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchBezier> Beziers => _beziers;

    /// <summary>
    /// All ellipses in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchEllipse> Ellipses => _ellipses;

    /// <summary>
    /// All rounded rectangles in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchRoundedRectangle> RoundedRectangles => _roundedRectangles;

    /// <summary>
    /// All pies in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchPie> Pies => _pies;

    /// <summary>
    /// All text frames in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchTextFrame> TextFrames => _textFrames;

    /// <summary>
    /// All images in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchImage> Images => _images;

    /// <summary>
    /// All symbols in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchSymbol> Symbols => _symbols;

    /// <summary>
    /// All elliptical arcs in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchEllipticalArc> EllipticalArcs => _ellipticalArcs;

    /// <summary>
    /// All No-ERC markers in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchNoErc> NoErcs => _noErcs;

    /// <inheritdoc />
    public IReadOnlyList<ISchNoConnect> NoConnects => _noErcs;

    /// <summary>
    /// All bus entries in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchBusEntry> BusEntries => _busEntries;

    /// <inheritdoc />
    IReadOnlyList<ISchBusEntry> ISchDocument.BusEntries => _busEntries;

    /// <summary>
    /// All buses in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchBus> Buses => _buses;

    /// <inheritdoc />
    IReadOnlyList<ISchBus> ISchDocument.Buses => _buses;

    /// <summary>
    /// All ports in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchPort> Ports => _ports;

    /// <summary>
    /// All sheet symbols in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchSheetSymbol> SheetSymbols => _sheetSymbols;

    /// <summary>
    /// All sheet entries in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchSheetEntry> SheetEntries => _sheetEntries;

    /// <summary>
    /// All blankets in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchBlanket> Blankets => _blankets;

    /// <summary>
    /// All parameter sets (directives) in this document (top-level).
    /// </summary>
    public IReadOnlyList<SchParameterSet> ParameterSets => _parameterSets;

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            // Start from the sheet rectangle so the rendered view fits the whole page (with its
            // border/title block) like Altium, even when the content occupies only part of the sheet.
            var bounds = SheetInfo.SheetRect;
            foreach (var comp in _components) bounds = bounds.Union(comp.Bounds);
            foreach (var wire in _wires) bounds = bounds.Union(wire.Bounds);
            foreach (var netLabel in _netLabels) bounds = bounds.Union(netLabel.Bounds);
            foreach (var junction in _junctions) bounds = bounds.Union(junction.Bounds);
            foreach (var power in _powerObjects) bounds = bounds.Union(power.Bounds);
            // Hidden labels and invisible parameters (e.g. read-only title-block fields) carry only a
            // coarse text-length bounds estimate; including them would inflate the page extent and
            // throw off auto-zoom, so skip anything that isn't actually drawn.
            foreach (var label in _labels) if (!label.IsHidden) bounds = bounds.Union(label.Bounds);
            foreach (var param in _parameters) if (param.IsVisible) bounds = bounds.Union(param.Bounds);
            foreach (var line in _lines) bounds = bounds.Union(line.Bounds);
            foreach (var rect in _rectangles) bounds = bounds.Union(rect.Bounds);
            foreach (var bus in _buses) bounds = bounds.Union(bus.Bounds);
            foreach (var busEntry in _busEntries) bounds = bounds.Union(busEntry.Bounds);
            foreach (var port in _ports) bounds = bounds.Union(port.Bounds);
            foreach (var sheet in _sheetSymbols) bounds = bounds.Union(sheet.Bounds);
            foreach (var noErc in _noErcs) bounds = bounds.Union(noErc.Bounds);
            foreach (var polyline in _polylines) bounds = bounds.Union(polyline.Bounds);
            foreach (var polygon in _polygons) bounds = bounds.Union(polygon.Bounds);
            foreach (var arc in _arcs) bounds = bounds.Union(arc.Bounds);
            foreach (var bezier in _beziers) bounds = bounds.Union(bezier.Bounds);
            foreach (var ellipse in _ellipses) bounds = bounds.Union(ellipse.Bounds);
            foreach (var roundedRect in _roundedRectangles) bounds = bounds.Union(roundedRect.Bounds);
            foreach (var pie in _pies) bounds = bounds.Union(pie.Bounds);
            foreach (var textFrame in _textFrames) bounds = bounds.Union(textFrame.Bounds);
            foreach (var image in _images) bounds = bounds.Union(image.Bounds);
            foreach (var ellipticalArc in _ellipticalArcs) bounds = bounds.Union(ellipticalArc.Bounds);
            return bounds;
        }
    }

    /// <summary>
    /// Adds a component to the document.
    /// </summary>
    public void AddComponent(SchComponent component) => _components.Add(component);

    void ISchDocument.AddComponent(ISchComponent component)
    {
        if (component is not SchComponent c) throw new ArgumentException($"Expected {nameof(SchComponent)}", nameof(component));
        _components.Add(c);
    }

    bool ISchDocument.RemoveComponent(ISchComponent component) => component is SchComponent c && _components.Remove(c);

    void ISchDocument.AddWire(ISchWire wire)
    {
        if (wire is not SchWire w) throw new ArgumentException($"Expected {nameof(SchWire)}", nameof(wire));
        _wires.Add(w);
    }

    bool ISchDocument.RemoveWire(ISchWire wire) => wire is SchWire w && _wires.Remove(w);

    void ISchDocument.AddNetLabel(ISchNetLabel netLabel)
    {
        if (netLabel is not SchNetLabel nl) throw new ArgumentException($"Expected {nameof(SchNetLabel)}", nameof(netLabel));
        _netLabels.Add(nl);
    }

    bool ISchDocument.RemoveNetLabel(ISchNetLabel netLabel) => netLabel is SchNetLabel nl && _netLabels.Remove(nl);

    void ISchDocument.AddJunction(ISchJunction junction)
    {
        if (junction is not SchJunction j) throw new ArgumentException($"Expected {nameof(SchJunction)}", nameof(junction));
        _junctions.Add(j);
    }

    bool ISchDocument.RemoveJunction(ISchJunction junction) => junction is SchJunction j && _junctions.Remove(j);

    void ISchDocument.AddPowerObject(ISchPowerObject powerObject)
    {
        if (powerObject is not SchPowerObject po) throw new ArgumentException($"Expected {nameof(SchPowerObject)}", nameof(powerObject));
        _powerObjects.Add(po);
    }

    bool ISchDocument.RemovePowerObject(ISchPowerObject powerObject) => powerObject is SchPowerObject po && _powerObjects.Remove(po);

    void ISchDocument.AddLabel(ISchLabel label)
    {
        if (label is not SchLabel l) throw new ArgumentException($"Expected {nameof(SchLabel)}", nameof(label));
        _labels.Add(l);
    }

    bool ISchDocument.RemoveLabel(ISchLabel label) => label is SchLabel l && _labels.Remove(l);

    void ISchDocument.AddNoConnect(ISchNoConnect noConnect)
    {
        if (noConnect is not SchNoErc ne) throw new ArgumentException($"Expected {nameof(SchNoErc)}", nameof(noConnect));
        _noErcs.Add(ne);
    }

    bool ISchDocument.RemoveNoConnect(ISchNoConnect noConnect) => noConnect is SchNoErc ne && _noErcs.Remove(ne);

    void ISchDocument.AddBus(ISchBus bus)
    {
        if (bus is not SchBus b) throw new ArgumentException($"Expected {nameof(SchBus)}", nameof(bus));
        _buses.Add(b);
    }

    bool ISchDocument.RemoveBus(ISchBus bus) => bus is SchBus b && _buses.Remove(b);

    void ISchDocument.AddBusEntry(ISchBusEntry busEntry)
    {
        if (busEntry is not SchBusEntry be) throw new ArgumentException($"Expected {nameof(SchBusEntry)}", nameof(busEntry));
        _busEntries.Add(be);
    }

    bool ISchDocument.RemoveBusEntry(ISchBusEntry busEntry) => busEntry is SchBusEntry be && _busEntries.Remove(be);

    /// <summary>
    /// Adds a top-level primitive to the document.
    /// </summary>
    public void AddPrimitive(object primitive)
    {
        switch (primitive)
        {
            case SchWire wire: _wires.Add(wire); break;
            case SchNetLabel netLabel: _netLabels.Add(netLabel); break;
            case SchJunction junction: _junctions.Add(junction); break;
            case SchPowerObject power: _powerObjects.Add(power); break;
            case SchLabel label: _labels.Add(label); break;
            case SchParameter param: _parameters.Add(param); break;
            case SchLine line: _lines.Add(line); break;
            case SchRectangle rect: _rectangles.Add(rect); break;
            case SchPolygon polygon: _polygons.Add(polygon); break;
            case SchPolyline polyline: _polylines.Add(polyline); break;
            case SchArc arc: _arcs.Add(arc); break;
            case SchBezier bezier: _beziers.Add(bezier); break;
            case SchEllipse ellipse: _ellipses.Add(ellipse); break;
            case SchRoundedRectangle roundedRect: _roundedRectangles.Add(roundedRect); break;
            case SchPie pie: _pies.Add(pie); break;
            case SchTextFrame textFrame: _textFrames.Add(textFrame); break;
            case SchImage image: _images.Add(image); break;
            case SchSymbol symbol: _symbols.Add(symbol); break;
            case SchEllipticalArc ellipticalArc: _ellipticalArcs.Add(ellipticalArc); break;
            case SchNoErc noErc: _noErcs.Add(noErc); break;
            case SchBusEntry busEntry: _busEntries.Add(busEntry); break;
            case SchBus bus: _buses.Add(bus); break;
            case SchPort port: _ports.Add(port); break;
            case SchSheetSymbol sheetSymbol: _sheetSymbols.Add(sheetSymbol); break;
            case SchSheetEntry sheetEntry: _sheetEntries.Add(sheetEntry); break;
            case SchBlanket blanket: _blankets.Add(blanket); break;
            case SchParameterSet parameterSet: _parameterSets.Add(parameterSet); break;
            case SchComponent comp: _components.Add(comp); break;
            case SchTemplate template: _templates.Add(template); break;
            case SchNote note: _notes.Add(note); break;
            case SchHyperlink hyperlink: _hyperlinks.Add(hyperlink); break;
            case SchCompileMask compileMask: _compileMasks.Add(compileMask); break;
            case SchHarnessConnector harnessConnector: _harnessConnectors.Add(harnessConnector); break;
            case SchHarnessEntry harnessEntry: _harnessEntries.Add(harnessEntry); break;
            case SchHarnessType harnessType: _harnessTypes.Add(harnessType); break;
            case SchSignalHarness signalHarness: _signalHarnesses.Add(signalHarness); break;
        }
    }

    /// <summary>
    /// Counts the modeled primitives the writer is responsible for: document-level primitives, components,
    /// container children (component children, sheet-symbol entries, parameter-set and blanket parameters)
    /// and preserved opaque records. Used to detect structural edits made after a document was loaded (see
    /// <see cref="LoadedPrimitiveCount"/>); including container children means adding or removing one trips
    /// the captured-order fast path and falls back to typed serialization so the edit is not dropped.
    /// </summary>
    internal int CountModeledPrimitives()
        => _components.Count + _wires.Count + _netLabels.Count + _junctions.Count + _powerObjects.Count
         + _labels.Count + _parameters.Count + _lines.Count + _rectangles.Count + _polygons.Count
         + _polylines.Count + _arcs.Count + _beziers.Count + _ellipses.Count + _roundedRectangles.Count
         + _pies.Count + _textFrames.Count + _images.Count + _symbols.Count + _ellipticalArcs.Count
         + _noErcs.Count + _busEntries.Count + _buses.Count + _ports.Count + _sheetSymbols.Count
         + _sheetEntries.Count + _blankets.Count + _parameterSets.Count + _templates.Count + _notes.Count
         + _hyperlinks.Count + _compileMasks.Count + _harnessConnectors.Count + _harnessEntries.Count
         + _harnessTypes.Count + _signalHarnesses.Count + OpaqueRecords.Count
         + _components.OfType<SchComponent>().Sum(c => c.CountChildPrimitives())
         + _sheetSymbols.Sum(s => s.Entries.Count)
         + _parameterSets.Sum(p => p.Parameters.Count)
         + _blankets.Sum(b => b.Parameters.Count)
         + _templates.Sum(t => t.OwnedPrimitives.Count);

    /// <inheritdoc />
    public async ValueTask SaveAsync(string path, OriginalCircuit.Eda.Models.SaveOptions? options = null, CancellationToken cancellationToken = default)
    {
        await new SchDocWriter().WriteAsync(this, path, overwrite: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(Stream stream, OriginalCircuit.Eda.Models.SaveOptions? options = null, CancellationToken cancellationToken = default)
    {
        await new SchDocWriter().WriteAsync(this, stream, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
