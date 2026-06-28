using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB 3D component body.
/// </summary>
public sealed class PcbComponentBody : IPcbComponentBody
{
    private readonly List<CoordPoint> _outline = new();

    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Outline => _outline;

    /// <summary>
    /// Layer name (e.g., "MECHANICAL1").
    /// </summary>
    public string LayerName { get; set; } = "MECHANICAL1";

    /// <summary>
    /// Mechanical layer ID (default 57 = Mechanical 1).
    /// </summary>
    public int Layer { get; set; } = 57;

    /// <summary>Net index into the board's net list (0xFFFF = no net).</summary>
    public ushort NetIndex { get; set; } = 0xFFFF;

    /// <summary>Component index into the board's component list (-1 = not part of a component).</summary>
    public int ComponentIndex { get; set; } = -1;

    /// <summary>
    /// Body name/identifier.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Kind of body (0=Extruded, 1=Generic, etc.).
    /// </summary>
    public int Kind { get; set; }

    /// <summary>
    /// Whether the body is shape-based (vs. model-based).
    /// </summary>
    public bool IsShapeBased { get; set; }

    /// <summary>
    /// Standoff height from board surface.
    /// </summary>
    public Coord StandoffHeight { get; set; }

    /// <summary>
    /// Overall height of the body.
    /// </summary>
    public Coord OverallHeight { get; set; }

    /// <summary>
    /// 3D body color (RGB).
    /// </summary>
    public int BodyColor3D { get; set; } = 0xE0E0E0; // Light gray default

    /// <summary>
    /// 3D body opacity (0.0-1.0).
    /// </summary>
    public double BodyOpacity3D { get; set; } = 1.0;

    /// <summary>
    /// Model identifier (for linked models).
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Whether the model is embedded.
    /// </summary>
    public bool ModelEmbed { get; set; } = true;

    /// <summary>
    /// Model type (0=None, 1=Embedded, etc.).
    /// </summary>
    public int ModelType { get; set; } = 1;

    /// <summary>
    /// 2D location for model placement.
    /// </summary>
    public CoordPoint Model2DLocation { get; set; }

    /// <summary>
    /// 2D rotation for model placement.
    /// </summary>
    public double Model2DRotation { get; set; }

    /// <summary>
    /// 3D rotation around X axis.
    /// </summary>
    public double Model3DRotX { get; set; }

    /// <summary>
    /// 3D rotation around Y axis.
    /// </summary>
    public double Model3DRotY { get; set; }

    /// <summary>
    /// 3D rotation around Z axis.
    /// </summary>
    public double Model3DRotZ { get; set; }

    /// <summary>
    /// 3D offset in Z direction.
    /// </summary>
    public Coord Model3DDz { get; set; }

    /// <summary>
    /// Model file name (e.g., the .step file name).
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Whether this body is enabled (active in the design).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this body is a keepout region.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Cavity height for embedded components.
    /// </summary>
    public Coord CavityHeight { get; set; }

    /// <summary>
    /// Unique identifier for this component body.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether user routed this body.
    /// </summary>
    public bool UserRouted { get; set; }

    /// <summary>
    /// Union index for grouped primitives.
    /// </summary>
    public int UnionIndex { get; set; }

    /// <summary>
    /// Whether this is a free primitive.
    /// </summary>
    public bool IsFreePrimitive { get; set; }

    /// <summary>
    /// Whether this is an electrical primitive.
    /// </summary>
    public bool IsElectricalPrim { get; set; }

    /// <summary>
    /// Whether this is a pre-route.
    /// </summary>
    public bool IsPreRoute { get; set; }

    /// <summary>
    /// Whether this body has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this body is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

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
    /// Solder mask expansion.
    /// </summary>
    public Coord SolderMaskExpansion { get; set; }

    /// <summary>
    /// Whether this is a simple region.
    /// </summary>
    public bool IsSimpleRegion { get; set; }

    /// <summary>
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Whether this body is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether this body is locked from editing.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Whether this body allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this body is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Whether the body color is overridden.
    /// </summary>
    public bool OverrideColor { get; set; }

    /// <summary>
    /// Whether the model has changed.
    /// </summary>
    public bool ModelHasChanged { get; set; }

    /// <summary>
    /// Body projection mode.
    /// </summary>
    public int BodyProjection { get; set; }

    /// <summary>
    /// Texture name/path (the <c>TEXTURE</c> key; always emitted, empty when none).
    /// </summary>
    public string Texture { get; set; } = string.Empty;

    /// <summary>Texture center X (the <c>TEXTURECENTERX</c> key).</summary>
    public Coord TextureCenterX { get; set; }

    /// <summary>Texture center Y (the <c>TEXTURECENTERY</c> key).</summary>
    public Coord TextureCenterY { get; set; }

    /// <summary>Texture size X (the <c>TEXTURESIZEX</c> key).</summary>
    public Coord TextureSizeX { get; set; }

    /// <summary>Texture size Y (the <c>TEXTURESIZEY</c> key).</summary>
    public Coord TextureSizeY { get; set; }

    /// <summary>Texture rotation in degrees (the <c>TEXTUREROTATION</c> key; Delphi-exponential on disk).</summary>
    public double TextureRotation { get; set; }

    /// <summary>
    /// Number of holes in this body.
    /// </summary>
    public int HoleCount { get; set; }

    /// <summary>
    /// Number of axes.
    /// </summary>
    public int AxisCount { get; set; }

    /// <summary>
    /// Area of the body outline in internal coordinate units squared.
    /// </summary>
    public long Area { get; set; }

    /// <summary>
    /// Sub-polygon index (the <c>SUBPOLYINDEX</c> key); -1 when not a polygon sub-shape.
    /// </summary>
    public int SubPolyIndex { get; set; } = -1;

    /// <summary>Arc resolution from the region-level prefix (the first <c>ARCRESOLUTION</c> key).</summary>
    public Coord ArcResolutionPrefix { get; set; }

    /// <summary>Arc resolution for the 3D body (the second, duplicate <c>ARCRESOLUTION</c> key).</summary>
    public Coord ArcResolutionBody { get; set; }

    /// <summary>
    /// Identifier string for this body (the <c>IDENTIFIER</c> key; stored as comma-separated codepoints).
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Checksum of the linked 3D model (the <c>MODEL.CHECKSUM</c> key; a 32-bit unsigned value).
    /// </summary>
    public long ModelChecksum { get; set; }

    /// <summary>
    /// Source of the linked model (the <c>MODEL.MODELSOURCE</c> key); null when absent (e.g. extruded bodies).
    /// </summary>
    public string? ModelSource { get; set; }

    /// <summary>Extruded-body minimum Z (the optional <c>MODEL.EXTRUDED.MINZ</c> key); null when absent.</summary>
    public Coord? ModelExtrudedMinZ { get; set; }

    /// <summary>Extruded-body maximum Z (the optional <c>MODEL.EXTRUDED.MAXZ</c> key); null when absent.</summary>
    public Coord? ModelExtrudedMaxZ { get; set; }

    /// <summary>
    /// The snap-point count (<c>MODEL.SNAPCOUNT</c>); <see langword="null"/> when the parameter is absent.
    /// When set, the writer emits <c>MODEL.SNAPCOUNT</c> followed by the <see cref="SnapPoints"/> raw
    /// coordinate triples, positioned immediately before <c>MODEL.MODELTYPE</c> (matching Altium).
    /// </summary>
    public int? SnapCount { get; set; }

    /// <summary>The snap points (<c>MODEL.S{i}X/Y/Z</c>) as raw coordinate triples.</summary>
    public List<(int X, int Y, int Z)> SnapPoints { get; } = new();

    /// <summary>
    /// Additional parameters from the nested C-string block that are not modeled as typed properties
    /// (e.g. the trailing indexed <c>FPARTDRCDISABLED.N.n</c> keys). Preserved for round-trip fidelity.
    /// </summary>
    public Dictionary<string, string>? AdditionalParameters { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            if (_outline.Count == 0)
                return CoordRect.Empty;

            var minX = _outline[0].X;
            var maxX = _outline[0].X;
            var minY = _outline[0].Y;
            var maxY = _outline[0].Y;

            for (var i = 1; i < _outline.Count; i++)
            {
                var p = _outline[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return new CoordRect(new CoordPoint(minX, minY), new CoordPoint(maxX, maxY));
        }
    }

    /// <summary>
    /// Adds a point to the body outline.
    /// </summary>
    internal void AddPoint(CoordPoint point) => _outline.Add(point);

    /// <summary>
    /// Creates a fluent builder for a new component body.
    /// </summary>
    public static ComponentBodyBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating PCB component bodies.
/// </summary>
public sealed class ComponentBodyBuilder
{
    private readonly PcbComponentBody _body = new();

    internal ComponentBodyBuilder() { }

    /// <summary>
    /// Adds a point to the body outline.
    /// </summary>
    public ComponentBodyBuilder AddPoint(Coord x, Coord y)
    {
        _body.AddPoint(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Sets the layer.
    /// </summary>
    public ComponentBodyBuilder OnLayer(string layer)
    {
        _body.LayerName = layer;
        return this;
    }

    /// <summary>
    /// Sets the body name.
    /// </summary>
    public ComponentBodyBuilder WithName(string name)
    {
        _body.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the body kind.
    /// </summary>
    public ComponentBodyBuilder Kind(int kind)
    {
        _body.Kind = kind;
        return this;
    }

    /// <summary>
    /// Sets whether the body is shape-based.
    /// </summary>
    public ComponentBodyBuilder ShapeBased(bool shapeBased = true)
    {
        _body.IsShapeBased = shapeBased;
        return this;
    }

    /// <summary>
    /// Sets the standoff height.
    /// </summary>
    public ComponentBodyBuilder StandoffHeight(Coord height)
    {
        _body.StandoffHeight = height;
        return this;
    }

    /// <summary>
    /// Sets the overall height.
    /// </summary>
    public ComponentBodyBuilder OverallHeight(Coord height)
    {
        _body.OverallHeight = height;
        return this;
    }

    /// <summary>
    /// Sets the 3D body color.
    /// </summary>
    public ComponentBodyBuilder Color3D(int color)
    {
        _body.BodyColor3D = color;
        return this;
    }

    /// <summary>
    /// Sets the 3D body opacity.
    /// </summary>
    public ComponentBodyBuilder Opacity3D(double opacity)
    {
        _body.BodyOpacity3D = opacity;
        return this;
    }

    /// <summary>
    /// Sets the model ID.
    /// </summary>
    public ComponentBodyBuilder ModelId(string modelId)
    {
        _body.ModelId = modelId;
        return this;
    }

    /// <summary>
    /// Sets the 2D location.
    /// </summary>
    public ComponentBodyBuilder At2D(Coord x, Coord y)
    {
        _body.Model2DLocation = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the 2D rotation.
    /// </summary>
    public ComponentBodyBuilder Rotation2D(double degrees)
    {
        _body.Model2DRotation = degrees;
        return this;
    }

    /// <summary>
    /// Sets the 3D rotation.
    /// </summary>
    public ComponentBodyBuilder Rotation3D(double rotX, double rotY, double rotZ)
    {
        _body.Model3DRotX = rotX;
        _body.Model3DRotY = rotY;
        _body.Model3DRotZ = rotZ;
        return this;
    }

    /// <summary>
    /// Sets the 3D Z offset.
    /// </summary>
    public ComponentBodyBuilder OffsetZ(Coord dz)
    {
        _body.Model3DDz = dz;
        return this;
    }

    /// <summary>
    /// Builds the component body.
    /// </summary>
    public PcbComponentBody Build() => _body;

    /// <summary>Implicitly converts a <see cref="ComponentBodyBuilder"/> to a <see cref="PcbComponentBody"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator PcbComponentBody(ComponentBodyBuilder builder) => builder.Build();
}
