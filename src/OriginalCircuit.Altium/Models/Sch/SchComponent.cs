using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic symbol/component.
/// </summary>
public sealed class SchComponent : ISchComponent
{
    /// <summary>
    /// All child records in their original on-disk order (pins, graphics, parameters, etc.),
    /// captured on read so the writer can reproduce the exact record sequence instead of grouping
    /// by type. Empty for components built from scratch (the writer then emits by type).
    /// </summary>
    internal List<object> ReadOrderedPrimitives { get; } = new();

    /// <summary>
    /// Counts modeled child primitives plus captured opaque records — the records that follow the
    /// component-definition record in the component's Data stream. Used to detect post-load edits.
    /// </summary>
    internal int CountChildPrimitives()
        => _pins.Count + _lines.Count + _rectangles.Count + _labels.Count + _arcs.Count
         + _polygons.Count + _polylines.Count + _wires.Count + _beziers.Count + _ellipses.Count
         + _roundedRectangles.Count + _pies.Count + _ellipticalArcs.Count + _parameters.Count
         + _netLabels.Count + _junctions.Count + _textFrames.Count + _images.Count + _symbols.Count
         + _powerObjects.Count + ReadOrderedPrimitives.OfType<SchOpaqueRecord>().Count();

    private readonly List<SchPin> _pins = new();
    private readonly List<SchLine> _lines = new();
    private readonly List<SchRectangle> _rectangles = new();
    private readonly List<SchLabel> _labels = new();
    private readonly List<SchWire> _wires = new();
    private readonly List<SchPolyline> _polylines = new();
    private readonly List<SchPolygon> _polygons = new();
    private readonly List<SchArc> _arcs = new();
    private readonly List<SchBezier> _beziers = new();
    private readonly List<SchEllipse> _ellipses = new();
    private readonly List<SchRoundedRectangle> _roundedRectangles = new();
    private readonly List<SchPie> _pies = new();
    private readonly List<SchNetLabel> _netLabels = new();
    private readonly List<SchJunction> _junctions = new();
    private readonly List<SchParameter> _parameters = new();
    private readonly List<SchTextFrame> _textFrames = new();
    private readonly List<SchImage> _images = new();
    private readonly List<SchSymbol> _symbols = new();
    private readonly List<SchEllipticalArc> _ellipticalArcs = new();
    private readonly List<SchPowerObject> _powerObjects = new();

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public string? Comment { get; set; }

    /// <summary>
    /// Snapshot of <see cref="Comment"/> as it stood immediately after read (whichever source it was
    /// derived from). The writer compares the live value against this baseline to tell an explicit edit
    /// of the convenience property from an untouched load, so it only pushes a change — and only then
    /// disables the byte-faithful replay path — when the caller actually mutated <see cref="Comment"/>.
    /// Null for components built from scratch (no baseline to compare against).
    /// </summary>
    internal string? CommentAsRead { get; set; }

    /// <inheritdoc />
    public string? DesignatorPrefix { get; set; }

    /// <inheritdoc />
    public int PartCount { get; set; } = 1;

    /// <summary>
    /// Location of the component.
    /// </summary>
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Component orientation (0=0, 1=90, 2=180, 3=270 degrees).
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    /// Current part ID being displayed.
    /// </summary>
    public int CurrentPartId { get; set; } = 1;

    /// <summary>
    /// Number of display modes available.
    /// </summary>
    public int DisplayModeCount { get; set; } = 1;

    /// <summary>
    /// Current display mode.
    /// </summary>
    public int DisplayMode { get; set; }

    /// <summary>
    /// Whether hidden pins should be shown.
    /// </summary>
    public bool ShowHiddenPins { get; set; }

    /// <summary>
    /// Whether hidden fields should be shown.
    /// </summary>
    public bool ShowHiddenFields { get; set; }

    /// <summary>
    /// Library path reference.
    /// </summary>
    public string? LibraryPath { get; set; }

    /// <summary>
    /// Source library name.
    /// </summary>
    public string? SourceLibraryName { get; set; }

    /// <summary>
    /// Library reference name (component name in the library).
    /// </summary>
    public string? LibReference { get; set; }

    /// <summary>
    /// Design item ID (typically the manufacturer part number).
    /// </summary>
    public string? DesignItemId { get; set; }

    /// <summary>
    /// Component kind (0=Standard, 1=Mechanical, etc.).
    /// </summary>
    public int ComponentKind { get; set; }

    /// <summary>
    /// Whether colors are overridden.
    /// </summary>
    public bool OverrideColors { get; set; }

    /// <summary>
    /// Line color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Area/fill color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Whether the designator is locked (cannot be auto-assigned).
    /// </summary>
    public bool DesignatorLocked { get; set; }

    /// <summary>
    /// Whether the part ID is locked.
    /// </summary>
    public bool PartIdLocked { get; set; }

    /// <summary>
    /// Symbol reference string.
    /// </summary>
    public string? SymbolReference { get; set; }

    /// <summary>
    /// Sheet part file name.
    /// </summary>
    public string? SheetPartFileName { get; set; }

    /// <summary>
    /// Target file name.
    /// </summary>
    public string? TargetFileName { get; set; }

    /// <summary>
    /// Alias list string.
    /// </summary>
    public string? AliasList { get; set; }

    /// <summary>
    /// Total pin count across all parts.
    /// </summary>
    public int AllPinCount { get; set; }

    /// <summary>
    /// Whether the component is graphically locked.
    /// </summary>
    public bool GraphicallyLocked { get; set; }

    /// <summary>
    /// Whether pins are moveable.
    /// </summary>
    public bool PinsMoveable { get; set; }

    /// <summary>
    /// Pin color override (RGB).
    /// </summary>
    public int PinColor { get; set; }

    /// <summary>
    /// Database library name for DBLib links.
    /// </summary>
    public string? DatabaseLibraryName { get; set; }

    /// <summary>
    /// Database table name for DBLib links.
    /// </summary>
    public string? DatabaseTableName { get; set; }

    /// <summary>
    /// Library identifier string.
    /// </summary>
    public string? LibraryIdentifier { get; set; }

    /// <summary>
    /// Vault GUID.
    /// </summary>
    public string? VaultGuid { get; set; }

    /// <summary>
    /// Item GUID for managed library reference.
    /// </summary>
    public string? ItemGuid { get; set; }

    /// <summary>
    /// Revision GUID for managed library reference.
    /// </summary>
    public string? RevisionGuid { get; set; }

    /// <summary>
    /// Whether the component is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the component is dimmed.
    /// </summary>
    public bool Dimmed { get; set; }

    /// <summary>
    /// Configuration parameters string.
    /// </summary>
    public string? ConfigurationParameters { get; set; }

    /// <summary>
    /// Configurator name.
    /// </summary>
    public string? ConfiguratorName { get; set; }

    /// <summary>
    /// Not-used database table name.
    /// </summary>
    public string? NotUsedBTableName { get; set; }

    /// <summary>
    /// Owner part ID (for multi-part components).
    /// </summary>
    public int OwnerPartId { get; set; } = -1;

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether to display field names.
    /// </summary>
    public bool DisplayFieldNames { get; set; }

    /// <summary>
    /// Whether the component is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Whether this is an unmanaged component.
    /// </summary>
    public bool IsUnmanaged { get; set; }

    /// <summary>
    /// Whether this component is user-configurable.
    /// </summary>
    public bool IsUserConfigurable { get; set; }

    /// <summary>
    /// Library identifier kind.
    /// </summary>
    public int LibIdentifierKind { get; set; }

    /// <summary>
    /// Display mode of the owning part.
    /// </summary>
    public int OwnerPartDisplayMode { get; set; }

    /// <summary>
    /// Revision details string.
    /// </summary>
    public string? RevisionDetails { get; set; }

    /// <summary>
    /// Revision human-readable ID.
    /// </summary>
    public string? RevisionHrid { get; set; }

    /// <summary>
    /// Revision state string.
    /// </summary>
    public string? RevisionState { get; set; }

    /// <summary>
    /// Revision status string.
    /// </summary>
    public string? RevisionStatus { get; set; }

    /// <summary>
    /// Symbol item GUID for managed library reference.
    /// </summary>
    public string? SymbolItemGuid { get; set; }

    /// <summary>
    /// Symbol items GUID for managed library reference.
    /// </summary>
    public string? SymbolItemsGuid { get; set; }

    /// <summary>
    /// Symbol revision GUID for managed library reference.
    /// </summary>
    public string? SymbolRevisionGuid { get; set; }

    /// <summary>
    /// Symbol vault GUID for managed library reference.
    /// </summary>
    public string? SymbolVaultGuid { get; set; }

    /// <summary>
    /// Whether to use database table name.
    /// </summary>
    public bool UseDbTableName { get; set; }

    /// <summary>
    /// Whether to use library name.
    /// </summary>
    public bool UseLibraryName { get; set; }

    /// <summary>
    /// Variant option for this component.
    /// </summary>
    public int VariantOption { get; set; }

    /// <summary>
    /// Vault human-readable ID.
    /// </summary>
    public string? VaultHrid { get; set; }

    /// <summary>
    /// Generic component template GUID.
    /// </summary>
    public string? GenericComponentTemplateGuid { get; set; }

    /// <summary>
    /// Font table from the owning library/document header (FontID table). Records reference
    /// entries by 1-based FontId. Populated on read; empty for components built from scratch.
    /// Used by the renderer to draw text with the correct font name, size and style.
    /// </summary>
    public IReadOnlyList<SchFontDefinition> Fonts { get; set; } = Array.Empty<SchFontDefinition>();

    /// <inheritdoc />
    public IReadOnlyList<ISchPin> Pins => _pins;

    /// <inheritdoc />
    public IReadOnlyList<ISchLine> Lines => _lines;

    /// <inheritdoc />
    public IReadOnlyList<ISchRectangle> Rectangles => _rectangles;

    /// <inheritdoc />
    public IReadOnlyList<ISchLabel> Labels => _labels;

    /// <inheritdoc />
    public IReadOnlyList<ISchWire> Wires => _wires;

    /// <inheritdoc />
    public IReadOnlyList<ISchPolyline> Polylines => _polylines;

    /// <inheritdoc />
    public IReadOnlyList<ISchPolygon> Polygons => _polygons;

    /// <inheritdoc />
    public IReadOnlyList<ISchArc> Arcs => _arcs;

    /// <inheritdoc />
    public IReadOnlyList<ISchCircle> Circles => Array.Empty<ISchCircle>();

    /// <inheritdoc />
    public IReadOnlyList<ISchBezier> Beziers => _beziers;

    /// <inheritdoc />
    public IReadOnlyList<ISchEllipse> Ellipses => _ellipses;

    /// <inheritdoc />
    public IReadOnlyList<ISchRoundedRectangle> RoundedRectangles => _roundedRectangles;

    /// <inheritdoc />
    public IReadOnlyList<ISchPie> Pies => _pies;

    /// <inheritdoc />
    public IReadOnlyList<ISchNetLabel> NetLabels => _netLabels;

    /// <inheritdoc />
    public IReadOnlyList<ISchJunction> Junctions => _junctions;

    /// <inheritdoc />
    public IReadOnlyList<ISchParameter> Parameters => _parameters;

    /// <inheritdoc />
    public IReadOnlyList<ISchTextFrame> TextFrames => _textFrames;

    /// <inheritdoc />
    public IReadOnlyList<ISchImage> Images => _images;

    /// <inheritdoc />
    public IReadOnlyList<ISchSymbol> Symbols => _symbols;

    /// <inheritdoc />
    public IReadOnlyList<ISchEllipticalArc> EllipticalArcs => _ellipticalArcs;

    /// <inheritdoc />
    public IReadOnlyList<ISchPowerObject> PowerObjects => _powerObjects;

    /// <summary>
    /// List of implementation links (e.g., footprint mappings).
    /// </summary>
    public IReadOnlyList<ISchImplementation> Implementations => _implementations;
    private readonly List<SchImplementation> _implementations = new();

    /// <summary>
    /// Adds an implementation to the component.
    /// </summary>
    internal void AddImplementation(SchImplementation implementation) => _implementations.Add(implementation);

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var bounds = CoordRect.Empty;
            foreach (var pin in _pins) bounds = bounds.Union(pin.Bounds);
            foreach (var line in _lines) bounds = bounds.Union(line.Bounds);
            foreach (var rect in _rectangles) bounds = bounds.Union(rect.Bounds);
            foreach (var label in _labels) bounds = bounds.Union(label.Bounds);
            foreach (var wire in _wires) bounds = bounds.Union(wire.Bounds);
            foreach (var polyline in _polylines) bounds = bounds.Union(polyline.Bounds);
            foreach (var polygon in _polygons) bounds = bounds.Union(polygon.Bounds);
            foreach (var arc in _arcs) bounds = bounds.Union(arc.Bounds);
            foreach (var bezier in _beziers) bounds = bounds.Union(bezier.Bounds);
            foreach (var ellipse in _ellipses) bounds = bounds.Union(ellipse.Bounds);
            foreach (var roundedRect in _roundedRectangles) bounds = bounds.Union(roundedRect.Bounds);
            foreach (var pie in _pies) bounds = bounds.Union(pie.Bounds);
            foreach (var netLabel in _netLabels) bounds = bounds.Union(netLabel.Bounds);
            foreach (var junction in _junctions) bounds = bounds.Union(junction.Bounds);
            foreach (var param in _parameters) bounds = bounds.Union(param.Bounds);
            foreach (var textFrame in _textFrames) bounds = bounds.Union(textFrame.Bounds);
            foreach (var image in _images) bounds = bounds.Union(image.Bounds);
            foreach (var symbol in _symbols) bounds = bounds.Union(symbol.Bounds);
            foreach (var ellipticalArc in _ellipticalArcs) bounds = bounds.Union(ellipticalArc.Bounds);
            foreach (var powerObject in _powerObjects) bounds = bounds.Union(powerObject.Bounds);
            return bounds;
        }
    }

    /// <summary>
    /// Adds a pin to the component.
    /// </summary>
    public void AddPin(SchPin pin) => _pins.Add(pin);

    /// <summary>
    /// Adds a line to the component.
    /// </summary>
    public void AddLine(SchLine line) => _lines.Add(line);

    /// <summary>
    /// Adds a rectangle to the component.
    /// </summary>
    public void AddRectangle(SchRectangle rectangle) => _rectangles.Add(rectangle);

    /// <summary>
    /// Adds a label to the component.
    /// </summary>
    public void AddLabel(SchLabel label) => _labels.Add(label);

    /// <summary>
    /// Adds a wire to the component.
    /// </summary>
    public void AddWire(SchWire wire) => _wires.Add(wire);

    /// <summary>
    /// Adds a polyline to the component.
    /// </summary>
    public void AddPolyline(SchPolyline polyline) => _polylines.Add(polyline);

    /// <summary>
    /// Adds a polygon to the component.
    /// </summary>
    public void AddPolygon(SchPolygon polygon) => _polygons.Add(polygon);

    /// <summary>
    /// Adds an arc to the component.
    /// </summary>
    public void AddArc(SchArc arc) => _arcs.Add(arc);

    /// <summary>
    /// Adds a bezier curve to the component.
    /// </summary>
    public void AddBezier(SchBezier bezier) => _beziers.Add(bezier);

    /// <summary>
    /// Adds an ellipse to the component.
    /// </summary>
    public void AddEllipse(SchEllipse ellipse) => _ellipses.Add(ellipse);

    /// <summary>
    /// Adds a rounded rectangle to the component.
    /// </summary>
    public void AddRoundedRectangle(SchRoundedRectangle roundedRect) => _roundedRectangles.Add(roundedRect);

    /// <summary>
    /// Adds a pie to the component.
    /// </summary>
    public void AddPie(SchPie pie) => _pies.Add(pie);

    /// <summary>
    /// Adds a net label to the component.
    /// </summary>
    public void AddNetLabel(SchNetLabel netLabel) => _netLabels.Add(netLabel);

    /// <summary>
    /// Adds a junction to the component.
    /// </summary>
    public void AddJunction(SchJunction junction) => _junctions.Add(junction);

    /// <summary>
    /// Adds a parameter to the component.
    /// </summary>
    public void AddParameter(SchParameter parameter) => _parameters.Add(parameter);

    /// <summary>
    /// Adds a text frame to the component.
    /// </summary>
    public void AddTextFrame(SchTextFrame textFrame) => _textFrames.Add(textFrame);

    /// <summary>
    /// Adds an image to the component.
    /// </summary>
    public void AddImage(SchImage image) => _images.Add(image);

    /// <summary>
    /// Adds a symbol reference to the component.
    /// </summary>
    public void AddSymbol(SchSymbol symbol) => _symbols.Add(symbol);

    /// <summary>
    /// Adds an elliptical arc to the component.
    /// </summary>
    public void AddEllipticalArc(SchEllipticalArc ellipticalArc) => _ellipticalArcs.Add(ellipticalArc);

    /// <summary>
    /// Adds a power object to the component.
    /// </summary>
    public void AddPowerObject(SchPowerObject powerObject) => _powerObjects.Add(powerObject);

    // Explicit interface implementations for ISchComponent
    void ISchComponent.AddPin(ISchPin pin)
    {
        if (pin is not SchPin p) throw new ArgumentException($"Expected {nameof(SchPin)}", nameof(pin));
        _pins.Add(p);
    }

    bool ISchComponent.RemovePin(ISchPin pin) => pin is SchPin p && _pins.Remove(p);

    void ISchComponent.AddLine(ISchLine line)
    {
        if (line is not SchLine l) throw new ArgumentException($"Expected {nameof(SchLine)}", nameof(line));
        _lines.Add(l);
    }

    bool ISchComponent.RemoveLine(ISchLine line) => line is SchLine l && _lines.Remove(l);

    void ISchComponent.AddRectangle(ISchRectangle rectangle)
    {
        if (rectangle is not SchRectangle r) throw new ArgumentException($"Expected {nameof(SchRectangle)}", nameof(rectangle));
        _rectangles.Add(r);
    }

    bool ISchComponent.RemoveRectangle(ISchRectangle rectangle) => rectangle is SchRectangle r && _rectangles.Remove(r);

    void ISchComponent.AddLabel(ISchLabel label)
    {
        if (label is not SchLabel l) throw new ArgumentException($"Expected {nameof(SchLabel)}", nameof(label));
        _labels.Add(l);
    }

    bool ISchComponent.RemoveLabel(ISchLabel label) => label is SchLabel l && _labels.Remove(l);

    void ISchComponent.AddWire(ISchWire wire)
    {
        if (wire is not SchWire w) throw new ArgumentException($"Expected {nameof(SchWire)}", nameof(wire));
        _wires.Add(w);
    }

    bool ISchComponent.RemoveWire(ISchWire wire) => wire is SchWire w && _wires.Remove(w);

    void ISchComponent.AddPolyline(ISchPolyline polyline)
    {
        if (polyline is not SchPolyline p) throw new ArgumentException($"Expected {nameof(SchPolyline)}", nameof(polyline));
        _polylines.Add(p);
    }

    bool ISchComponent.RemovePolyline(ISchPolyline polyline) => polyline is SchPolyline p && _polylines.Remove(p);

    void ISchComponent.AddPolygon(ISchPolygon polygon)
    {
        if (polygon is not SchPolygon p) throw new ArgumentException($"Expected {nameof(SchPolygon)}", nameof(polygon));
        _polygons.Add(p);
    }

    bool ISchComponent.RemovePolygon(ISchPolygon polygon) => polygon is SchPolygon p && _polygons.Remove(p);

    void ISchComponent.AddArc(ISchArc arc)
    {
        if (arc is not SchArc a) throw new ArgumentException($"Expected {nameof(SchArc)}", nameof(arc));
        _arcs.Add(a);
    }

    bool ISchComponent.RemoveArc(ISchArc arc) => arc is SchArc a && _arcs.Remove(a);

    void ISchComponent.AddCircle(ISchCircle circle) => throw new NotSupportedException("Altium schematic components do not support circles. Use ellipses instead.");

    bool ISchComponent.RemoveCircle(ISchCircle circle) => false;

    void ISchComponent.AddBezier(ISchBezier bezier)
    {
        if (bezier is not SchBezier b) throw new ArgumentException($"Expected {nameof(SchBezier)}", nameof(bezier));
        _beziers.Add(b);
    }

    bool ISchComponent.RemoveBezier(ISchBezier bezier) => bezier is SchBezier b && _beziers.Remove(b);

    void ISchComponent.AddNetLabel(ISchNetLabel netLabel)
    {
        if (netLabel is not SchNetLabel nl) throw new ArgumentException($"Expected {nameof(SchNetLabel)}", nameof(netLabel));
        _netLabels.Add(nl);
    }

    bool ISchComponent.RemoveNetLabel(ISchNetLabel netLabel) => netLabel is SchNetLabel nl && _netLabels.Remove(nl);

    void ISchComponent.AddJunction(ISchJunction junction)
    {
        if (junction is not SchJunction j) throw new ArgumentException($"Expected {nameof(SchJunction)}", nameof(junction));
        _junctions.Add(j);
    }

    bool ISchComponent.RemoveJunction(ISchJunction junction) => junction is SchJunction j && _junctions.Remove(j);

    void ISchComponent.AddImage(ISchImage image)
    {
        if (image is not SchImage i) throw new ArgumentException($"Expected {nameof(SchImage)}", nameof(image));
        _images.Add(i);
    }

    bool ISchComponent.RemoveImage(ISchImage image) => image is SchImage i && _images.Remove(i);

    void ISchComponent.AddParameter(ISchParameter parameter)
    {
        if (parameter is not SchParameter p) throw new ArgumentException($"Expected {nameof(SchParameter)}", nameof(parameter));
        _parameters.Add(p);
    }

    bool ISchComponent.RemoveParameter(ISchParameter parameter) => parameter is SchParameter p && _parameters.Remove(p);

    /// <summary>
    /// Creates a fluent builder for a new component.
    /// </summary>
    public static SchComponentBuilder Create(string name) => new(name);
}

/// <summary>
/// Fluent builder for creating schematic components.
/// </summary>
public sealed class SchComponentBuilder
{
    private readonly SchComponent _component = new();

    internal SchComponentBuilder(string name)
    {
        _component.Name = name;
    }

    /// <summary>
    /// Sets the component description.
    /// </summary>
    public SchComponentBuilder WithDescription(string description)
    {
        _component.Description = description;
        return this;
    }

    /// <summary>
    /// Sets the component comment/value.
    /// </summary>
    public SchComponentBuilder WithComment(string comment)
    {
        _component.Comment = comment;
        return this;
    }

    /// <summary>
    /// Sets the designator prefix.
    /// </summary>
    public SchComponentBuilder WithDesignatorPrefix(string prefix)
    {
        _component.DesignatorPrefix = prefix;
        return this;
    }

    /// <summary>
    /// Sets the part count for multi-part components.
    /// </summary>
    public SchComponentBuilder WithPartCount(int count)
    {
        _component.PartCount = count;
        return this;
    }

    /// <summary>
    /// Adds a pin using a builder action.
    /// </summary>
    public SchComponentBuilder AddPin(Action<PinBuilder> configure)
    {
        var builder = SchPin.Create();
        configure(builder);
        _component.AddPin(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a line using a builder action.
    /// </summary>
    public SchComponentBuilder AddLine(Action<LineBuilder> configure)
    {
        var builder = SchLine.Create();
        configure(builder);
        _component.AddLine(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a rectangle using a builder action.
    /// </summary>
    public SchComponentBuilder AddRectangle(Action<RectangleBuilder> configure)
    {
        var builder = SchRectangle.Create();
        configure(builder);
        _component.AddRectangle(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a label using a builder action.
    /// </summary>
    public SchComponentBuilder AddLabel(string text, Action<LabelBuilder> configure)
    {
        var builder = SchLabel.Create(text);
        configure(builder);
        _component.AddLabel(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a wire using a builder action.
    /// </summary>
    public SchComponentBuilder AddWire(Action<WireBuilder> configure)
    {
        var builder = SchWire.Create();
        configure(builder);
        _component.AddWire(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a polyline using a builder action.
    /// </summary>
    public SchComponentBuilder AddPolyline(Action<PolylineBuilder> configure)
    {
        var builder = SchPolyline.Create();
        configure(builder);
        _component.AddPolyline(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a polygon using a builder action.
    /// </summary>
    public SchComponentBuilder AddPolygon(Action<PolygonBuilder> configure)
    {
        var builder = SchPolygon.Create();
        configure(builder);
        _component.AddPolygon(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an arc using a builder action.
    /// </summary>
    public SchComponentBuilder AddArc(Action<ArcBuilder> configure)
    {
        var builder = SchArc.Create();
        configure(builder);
        _component.AddArc(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a bezier curve using a builder action.
    /// </summary>
    public SchComponentBuilder AddBezier(Action<BezierBuilder> configure)
    {
        var builder = SchBezier.Create();
        configure(builder);
        _component.AddBezier(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an ellipse using a builder action.
    /// </summary>
    public SchComponentBuilder AddEllipse(Action<EllipseBuilder> configure)
    {
        var builder = SchEllipse.Create();
        configure(builder);
        _component.AddEllipse(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a rounded rectangle using a builder action.
    /// </summary>
    public SchComponentBuilder AddRoundedRectangle(Action<RoundedRectangleBuilder> configure)
    {
        var builder = SchRoundedRectangle.Create();
        configure(builder);
        _component.AddRoundedRectangle(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a pie using a builder action.
    /// </summary>
    public SchComponentBuilder AddPie(Action<PieBuilder> configure)
    {
        var builder = SchPie.Create();
        configure(builder);
        _component.AddPie(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a net label using a builder action.
    /// </summary>
    public SchComponentBuilder AddNetLabel(string text, Action<NetLabelBuilder> configure)
    {
        var builder = SchNetLabel.Create(text);
        configure(builder);
        _component.AddNetLabel(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a junction using a builder action.
    /// </summary>
    public SchComponentBuilder AddJunction(Action<JunctionBuilder> configure)
    {
        var builder = SchJunction.Create();
        configure(builder);
        _component.AddJunction(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a parameter using a builder action.
    /// </summary>
    public SchComponentBuilder AddParameter(string name, Action<ParameterBuilder> configure)
    {
        var builder = SchParameter.Create(name);
        configure(builder);
        _component.AddParameter(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a text frame using a builder action.
    /// </summary>
    public SchComponentBuilder AddTextFrame(string text, Action<TextFrameBuilder> configure)
    {
        var builder = SchTextFrame.Create(text);
        configure(builder);
        _component.AddTextFrame(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an image using a builder action.
    /// </summary>
    public SchComponentBuilder AddImage(Action<ImageBuilder> configure)
    {
        var builder = SchImage.Create();
        configure(builder);
        _component.AddImage(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a symbol reference using a builder action.
    /// </summary>
    public SchComponentBuilder AddSymbol(Action<SymbolBuilder> configure)
    {
        var builder = SchSymbol.Create();
        configure(builder);
        _component.AddSymbol(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an elliptical arc using a builder action.
    /// </summary>
    public SchComponentBuilder AddEllipticalArc(Action<EllipticalArcBuilder> configure)
    {
        var builder = SchEllipticalArc.Create();
        configure(builder);
        _component.AddEllipticalArc(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a power object using a builder action.
    /// </summary>
    public SchComponentBuilder AddPowerObject(string netName, Action<PowerObjectBuilder> configure)
    {
        var builder = SchPowerObject.Create(netName);
        configure(builder);
        _component.AddPowerObject(builder.Build());
        return this;
    }

    /// <summary>
    /// Builds the component.
    /// </summary>
    public SchComponent Build() => _component;

    /// <summary>
    /// Implicit conversion to <see cref="SchComponent"/>.
    /// </summary>
    public static implicit operator SchComponent(SchComponentBuilder builder) => builder.Build();
}
