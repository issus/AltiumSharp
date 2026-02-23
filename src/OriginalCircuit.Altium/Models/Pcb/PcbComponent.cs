using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB footprint/component.
/// </summary>
public sealed class PcbComponent : IPcbComponent
{
    private readonly List<PcbPad> _pads = new();
    private readonly List<PcbTrack> _tracks = new();
    private readonly List<PcbVia> _vias = new();
    private readonly List<PcbArc> _arcs = new();
    private readonly List<PcbText> _texts = new();
    private readonly List<PcbFill> _fills = new();
    private readonly List<PcbRegion> _regions = new();
    private readonly List<PcbComponentBody> _componentBodies = new();

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public Coord Height { get; set; }

    /// <summary>
    /// Component comment (value).
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Whether the comment is displayed.
    /// </summary>
    public bool CommentOn { get; set; }

    /// <summary>
    /// Comment auto-position mode.
    /// </summary>
    public int CommentAutoPosition { get; set; }

    /// <summary>
    /// Component kind.
    /// </summary>
    public int ComponentKind { get; set; }

    /// <summary>
    /// Whether the component is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether pin swapping is enabled.
    /// </summary>
    public bool EnablePinSwapping { get; set; }

    /// <summary>
    /// Whether part swapping is enabled.
    /// </summary>
    public bool EnablePartSwapping { get; set; }

    /// <summary>
    /// Whether the component is flipped on layer.
    /// </summary>
    public bool FlippedOnLayer { get; set; }

    /// <summary>
    /// Footprint description.
    /// </summary>
    public string? FootprintDescription { get; set; }

    /// <summary>
    /// Footprint configurable parameters (encoded).
    /// </summary>
    public string? FootprintConfigurableParametersEncoded { get; set; }

    /// <summary>
    /// Footprint configurator name.
    /// </summary>
    public string? FootprintConfiguratorName { get; set; }

    /// <summary>
    /// FPGA display mode.
    /// </summary>
    public int FPGADisplayMode { get; set; }

    /// <summary>
    /// Group number.
    /// </summary>
    public int GroupNum { get; set; }

    /// <summary>
    /// Whether this is a BGA component.
    /// </summary>
    public bool IsBGA { get; set; }

    /// <summary>
    /// Whether this is a keepout.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Whether this is an electrical primitive.
    /// </summary>
    public bool IsElectricalPrim { get; set; }

    /// <summary>
    /// Whether this is a pre-route.
    /// </summary>
    public bool IsPreRoute { get; set; }

    /// <summary>
    /// Whether tenting is applied.
    /// </summary>
    public bool IsTenting { get; set; }

    /// <summary>
    /// Whether top side is tented.
    /// </summary>
    public bool IsTentingTop { get; set; }

    /// <summary>
    /// Whether bottom side is tented.
    /// </summary>
    public bool IsTentingBottom { get; set; }

    /// <summary>
    /// Whether this is a top-side test point.
    /// </summary>
    public bool IsTestpointTop { get; set; }

    /// <summary>
    /// Whether this is a bottom-side test point.
    /// </summary>
    public bool IsTestpointBottom { get; set; }

    /// <summary>
    /// Whether this is a top assembly test point.
    /// </summary>
    public bool IsAssyTestpointTop { get; set; }

    /// <summary>
    /// Whether this is a bottom assembly test point.
    /// </summary>
    public bool IsAssyTestpointBottom { get; set; }

    /// <summary>
    /// Item GUID (vault reference).
    /// </summary>
    public string? ItemGUID { get; set; }

    /// <summary>
    /// Item revision GUID.
    /// </summary>
    public string? ItemRevisionGUID { get; set; }

    /// <summary>
    /// Whether jumpers are visible.
    /// </summary>
    public bool JumpersVisible { get; set; } = true;

    /// <summary>
    /// Layer this component is on.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Whether top layer is used.
    /// </summary>
    public bool LayerUsedTop { get; set; }

    /// <summary>
    /// Whether strings are locked.
    /// </summary>
    public bool LockStrings { get; set; }

    /// <summary>
    /// Model hash.
    /// </summary>
    public string? ModelHash { get; set; }

    /// <summary>
    /// Name auto-position mode.
    /// </summary>
    public int NameAutoPosition { get; set; }

    /// <summary>
    /// Whether the name is displayed.
    /// </summary>
    public bool NameOn { get; set; }

    /// <summary>
    /// Package-specific hash.
    /// </summary>
    public string? PackageSpecificHash { get; set; }

    /// <summary>
    /// Footprint pattern name.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Whether this is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

    /// <summary>
    /// Power plane clearance.
    /// </summary>
    public Coord PowerPlaneClearance { get; set; }

    /// <summary>
    /// Power plane connection style.
    /// </summary>
    public int PowerPlaneConnectStyle { get; set; }

    /// <summary>
    /// Power plane relief expansion.
    /// </summary>
    public Coord PowerPlaneReliefExpansion { get; set; }

    /// <summary>
    /// Whether primitives are locked.
    /// </summary>
    public bool PrimitiveLock { get; set; }

    /// <summary>
    /// Thermal relief air gap.
    /// </summary>
    public Coord ReliefAirGap { get; set; }

    /// <summary>
    /// Thermal relief conductor width.
    /// </summary>
    public Coord ReliefConductorWidth { get; set; }

    /// <summary>
    /// Number of thermal relief entries.
    /// </summary>
    public int ReliefEntries { get; set; }

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Solder mask expansion.
    /// </summary>
    public Coord SolderMaskExpansion { get; set; }

    /// <summary>
    /// Source component design item ID.
    /// </summary>
    public string? SourceCompDesignItemID { get; set; }

    /// <summary>
    /// Source component library.
    /// </summary>
    public string? SourceComponentLibrary { get; set; }

    /// <summary>
    /// Source description.
    /// </summary>
    public string? SourceDescription { get; set; }

    /// <summary>
    /// Source designator.
    /// </summary>
    public string? SourceDesignator { get; set; }

    /// <summary>
    /// Source footprint library.
    /// </summary>
    public string? SourceFootprintLibrary { get; set; }

    /// <summary>
    /// Source hierarchical path.
    /// </summary>
    public string? SourceHierarchicalPath { get; set; }

    /// <summary>
    /// Source library reference.
    /// </summary>
    public string? SourceLibReference { get; set; }

    /// <summary>
    /// Source unique ID.
    /// </summary>
    public string? SourceUniqueId { get; set; }

    /// <summary>
    /// Whether this has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Union index for grouped primitives.
    /// </summary>
    public int UnionIndex { get; set; }

    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether user routed.
    /// </summary>
    public bool UserRouted { get; set; }

    /// <summary>
    /// Vault GUID.
    /// </summary>
    public string? VaultGUID { get; set; }

    /// <summary>
    /// Component X location.
    /// </summary>
    public Coord X { get; set; }

    /// <summary>
    /// Component Y location.
    /// </summary>
    public Coord Y { get; set; }

    /// <summary>
    /// Default PCB 3D model.
    /// </summary>
    public string? DefaultPCB3DModel { get; set; }

    /// <summary>
    /// Number of axes.
    /// </summary>
    public int AxisCount { get; set; }

    /// <summary>
    /// Channel offset.
    /// </summary>
    public int ChannelOffset { get; set; }

    /// <summary>
    /// Whether this component allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this component is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Additional component-level OLE streams preserved for round-trip fidelity.
    /// Known streams: "UniqueIdPrimitiveInformation" (primitive ID mapping),
    /// "PrimitiveGuids" (GUID mapping for primitives).
    /// Key format: "StreamName" or "SubStorageName/StreamName" -> byte data.
    /// Empty by default for from-scratch creation (these streams are optional).
    /// </summary>
    public Dictionary<string, byte[]>? AdditionalStreams { get; set; }

    /// <summary>
    /// Additional parameters from the Parameters stream that are not modeled as typed properties.
    /// Preserved for round-trip fidelity.
    /// </summary>
    public Dictionary<string, string>? AdditionalParameters { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IPcbPad> Pads => _pads;

    /// <inheritdoc />
    public IReadOnlyList<IPcbTrack> Tracks => _tracks;

    /// <inheritdoc />
    public IReadOnlyList<IPcbVia> Vias => _vias;

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

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var bounds = CoordRect.Empty;
            foreach (var pad in _pads) bounds = bounds.Union(pad.Bounds);
            foreach (var track in _tracks) bounds = bounds.Union(track.Bounds);
            foreach (var via in _vias) bounds = bounds.Union(via.Bounds);
            foreach (var arc in _arcs) bounds = bounds.Union(arc.Bounds);
            foreach (var text in _texts) bounds = bounds.Union(text.Bounds);
            foreach (var fill in _fills) bounds = bounds.Union(fill.Bounds);
            foreach (var region in _regions) bounds = bounds.Union(region.Bounds);
            foreach (var body in _componentBodies) bounds = bounds.Union(body.Bounds);
            return bounds;
        }
    }

    /// <summary>
    /// Adds a pad to the component.
    /// </summary>
    public void AddPad(PcbPad pad) => _pads.Add(pad);

    /// <summary>
    /// Adds a track to the component.
    /// </summary>
    public void AddTrack(PcbTrack track) => _tracks.Add(track);

    /// <summary>
    /// Adds a via to the component.
    /// </summary>
    public void AddVia(PcbVia via) => _vias.Add(via);

    /// <summary>
    /// Adds an arc to the component.
    /// </summary>
    public void AddArc(PcbArc arc) => _arcs.Add(arc);

    /// <summary>
    /// Adds text to the component.
    /// </summary>
    public void AddText(PcbText text) => _texts.Add(text);

    /// <summary>
    /// Adds a fill to the component.
    /// </summary>
    public void AddFill(PcbFill fill) => _fills.Add(fill);

    /// <summary>
    /// Adds a region to the component.
    /// </summary>
    public void AddRegion(PcbRegion region) => _regions.Add(region);

    /// <summary>
    /// Adds a component body to the component.
    /// </summary>
    public void AddComponentBody(PcbComponentBody body) => _componentBodies.Add(body);

    /// <summary>
    /// Creates a fluent builder for a new component.
    /// </summary>
    public static ComponentBuilder Create(string name) => new(name);
}

/// <summary>
/// Fluent builder for creating PCB components.
/// </summary>
public sealed class ComponentBuilder
{
    private readonly PcbComponent _component = new();

    internal ComponentBuilder(string name)
    {
        _component.Name = name;
    }

    /// <summary>
    /// Sets the component description.
    /// </summary>
    public ComponentBuilder WithDescription(string description)
    {
        _component.Description = description;
        return this;
    }

    /// <summary>
    /// Sets the component height.
    /// </summary>
    public ComponentBuilder WithHeight(Coord height)
    {
        _component.Height = height;
        return this;
    }

    /// <summary>
    /// Adds a pad to the component using a builder action.
    /// </summary>
    public ComponentBuilder AddPad(Action<PadBuilder> configure)
    {
        var builder = PcbPad.Create();
        configure(builder);
        _component.AddPad(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a pad to the component.
    /// </summary>
    public ComponentBuilder AddPad(PcbPad pad)
    {
        _component.AddPad(pad);
        return this;
    }

    /// <summary>
    /// Adds a track to the component using a builder action.
    /// </summary>
    public ComponentBuilder AddTrack(Action<TrackBuilder> configure)
    {
        var builder = PcbTrack.Create();
        configure(builder);
        _component.AddTrack(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a track to the component.
    /// </summary>
    public ComponentBuilder AddTrack(PcbTrack track)
    {
        _component.AddTrack(track);
        return this;
    }

    /// <summary>
    /// Adds a via to the component using a builder action.
    /// </summary>
    public ComponentBuilder AddVia(Action<ViaBuilder> configure)
    {
        var builder = PcbVia.Create();
        configure(builder);
        _component.AddVia(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an arc to the component using a builder action.
    /// </summary>
    public ComponentBuilder AddArc(Action<ArcBuilder> configure)
    {
        var builder = PcbArc.Create();
        configure(builder);
        _component.AddArc(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds text to the component using a builder action.
    /// </summary>
    public ComponentBuilder AddText(string text, Action<TextBuilder> configure)
    {
        var builder = PcbText.Create(text);
        configure(builder);
        _component.AddText(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a fill to the component using a builder action.
    /// </summary>
    public ComponentBuilder AddFill(Action<FillBuilder> configure)
    {
        var builder = PcbFill.Create();
        configure(builder);
        _component.AddFill(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a region to the component using a builder action.
    /// </summary>
    public ComponentBuilder AddRegion(Action<RegionBuilder> configure)
    {
        var builder = PcbRegion.Create();
        configure(builder);
        _component.AddRegion(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a component body to the component using a builder action.
    /// </summary>
    public ComponentBuilder AddComponentBody(Action<ComponentBodyBuilder> configure)
    {
        var builder = PcbComponentBody.Create();
        configure(builder);
        _component.AddComponentBody(builder.Build());
        return this;
    }

    /// <summary>
    /// Builds the component.
    /// </summary>
    public PcbComponent Build() => _component;

    /// <summary>
    /// Implicit conversion to PcbComponent.
    /// </summary>
    public static implicit operator PcbComponent(ComponentBuilder builder) => builder.Build();
}
