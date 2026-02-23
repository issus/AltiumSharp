using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Pad shapes supported by Altium.
/// </summary>
public enum PadShape
{
    Round = 1,
    Rectangular = 2,
    Octagonal = 3,
    RoundedRectangle = 9
}

/// <summary>
/// Pad hole types.
/// </summary>
public enum PadHoleType
{
    Round = 0,
    Square = 1,
    Slot = 2
}

/// <summary>
/// Represents a PCB pad.
/// </summary>
public sealed class PcbPad : IPcbPad
{
    /// <inheritdoc />
    public string? Designator { get; set; }

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Size of the pad on the top layer.
    /// </summary>
    public CoordPoint SizeTop { get; set; }

    /// <summary>
    /// Size of the pad on middle layers.
    /// </summary>
    public CoordPoint SizeMiddle { get; set; }

    /// <summary>
    /// Size of the pad on the bottom layer.
    /// </summary>
    public CoordPoint SizeBottom { get; set; }

    /// <summary>
    /// Hole diameter for through-hole pads.
    /// </summary>
    public Coord HoleSize { get; set; }

    /// <summary>
    /// Shape of the pad on top layer.
    /// </summary>
    public PadShape ShapeTop { get; set; } = PadShape.Round;

    /// <summary>
    /// Shape of the pad on middle layers.
    /// </summary>
    public PadShape ShapeMiddle { get; set; } = PadShape.Round;

    /// <summary>
    /// Shape of the pad on bottom layer.
    /// </summary>
    public PadShape ShapeBottom { get; set; } = PadShape.Round;

    /// <summary>
    /// Hole type.
    /// </summary>
    public PadHoleType HoleType { get; set; } = PadHoleType.Round;

    /// <inheritdoc />
    OriginalCircuit.Eda.Enums.PadShape IPcbPad.Shape => AltiumEnumHelper.ToEdaPadShape(ShapeTop);

    /// <inheritdoc />
    CoordPoint IPcbPad.Size => SizeTop;

    /// <inheritdoc />
    OriginalCircuit.Eda.Enums.PadHoleType IPcbPad.HoleType => AltiumEnumHelper.ToEdaPadHoleType(HoleType);

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Whether this is a plated hole.
    /// </summary>
    public bool IsPlated { get; set; } = true;

    /// <summary>
    /// Layer this pad is on (for SMD pads).
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Component index this pad belongs to (0xFFFF = free primitive, not in any component).
    /// Zero-based index into the document's component list.
    /// </summary>
    public int ComponentIndex { get; set; } = -1;

    /// <summary>
    /// Net name this pad is connected to.
    /// </summary>
    public string? Net { get; set; }

    /// <summary>
    /// Corner radius for rounded rectangle shape (0-100%).
    /// </summary>
    public int CornerRadiusPercentage { get; set; } = 50;

    /// <summary>
    /// Pad stack mode (0=Simple, 1=Top-Mid-Bottom, 2=Full Stack).
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Hole width for slot holes.
    /// </summary>
    public Coord HoleWidth { get; set; }

    /// <summary>
    /// Rotation angle of the hole in degrees.
    /// </summary>
    public double HoleRotation { get; set; }

    /// <summary>
    /// Drill type (0=Simple, 1=Pressfitted).
    /// </summary>
    public int DrillType { get; set; }

    /// <summary>
    /// Power plane connection style (0=Direct, 1=Relief, 2=No Connect).
    /// </summary>
    public int PowerPlaneConnectStyle { get; set; }

    /// <summary>
    /// Width of thermal relief conductors.
    /// </summary>
    public Coord ReliefConductorWidth { get; set; }

    /// <summary>
    /// Number of thermal relief entries.
    /// </summary>
    public int ReliefEntries { get; set; }

    /// <summary>
    /// Air gap for thermal relief.
    /// </summary>
    public Coord ReliefAirGap { get; set; }

    /// <summary>
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Solder mask expansion override.
    /// </summary>
    public Coord SolderMaskExpansion { get; set; }

    /// <summary>
    /// Whether solder mask expansion is measured from the hole edge.
    /// </summary>
    public bool SolderMaskExpansionFromHoleEdge { get; set; }

    /// <summary>
    /// Whether the top side of the via is tented (covered with solder mask).
    /// </summary>
    public bool IsTentingTop { get; set; }

    /// <summary>
    /// Whether the bottom side of the via is tented (covered with solder mask).
    /// </summary>
    public bool IsTentingBottom { get; set; }

    /// <summary>
    /// Unique identifier for this pad.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Swap ID for the pad (used in pin swapping).
    /// </summary>
    public string? SwapIdPad { get; set; }

    /// <summary>
    /// Swap ID for the part (used in pin swapping).
    /// </summary>
    public string? SwapIdPart { get; set; }

    /// <summary>
    /// Whether this pad is enabled (active in the design).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this pad acts as a keepout region.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Whether this pad is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether this pad is a test point on the top side.
    /// </summary>
    public bool IsTestPointTop { get; set; }

    /// <summary>
    /// Whether this pad is a test point on the bottom side.
    /// </summary>
    public bool IsTestPointBottom { get; set; }

    /// <summary>
    /// Whether user routed this pad.
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
    /// Whether this pad has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this pad is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

    /// <summary>
    /// Whether tenting is applied.
    /// </summary>
    public bool IsTenting { get; set; }

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
    /// Power plane relief expansion.
    /// </summary>
    public Coord PowerPlaneReliefExpansion { get; set; }

    /// <summary>
    /// Whether this is a surface mount pad.
    /// </summary>
    public bool IsSurfaceMount { get; set; }

    /// <summary>
    /// Whether this pad is a pad stack.
    /// </summary>
    public bool IsPadStack { get; set; }

    /// <summary>
    /// Whether this pad has corner radius/chamfer.
    /// </summary>
    public bool HasCornerRadiusChamfer { get; set; }

    /// <summary>
    /// Whether this pad has a custom chamfered rectangle.
    /// </summary>
    public bool HasCustomChamferedRectangle { get; set; }

    /// <summary>
    /// Whether this pad has a custom donut shape.
    /// </summary>
    public bool HasCustomDonut { get; set; }

    /// <summary>
    /// Whether this pad has custom mask donut shapes.
    /// </summary>
    public bool HasCustomMaskDonutShapes { get; set; }

    /// <summary>
    /// Whether this pad has custom mask shapes.
    /// </summary>
    public bool HasCustomMaskShapes { get; set; }

    /// <summary>
    /// Whether this pad has a custom rounded rectangle.
    /// </summary>
    public bool HasCustomRoundedRectangle { get; set; }

    /// <summary>
    /// Whether this pad has custom shapes.
    /// </summary>
    public bool HasCustomShapes { get; set; }

    /// <summary>
    /// Whether this pad has rounded rectangular shapes.
    /// </summary>
    public bool HasRoundedRectangularShapes { get; set; }

    /// <summary>
    /// Multi-layer high bits.
    /// </summary>
    public int MultiLayerHighBits { get; set; }

    /// <summary>
    /// Hole positive tolerance.
    /// </summary>
    public Coord HolePositiveTolerance { get; set; }

    /// <summary>
    /// Hole negative tolerance.
    /// </summary>
    public Coord HoleNegativeTolerance { get; set; }

    /// <summary>
    /// Whether the hole size is valid.
    /// </summary>
    public bool IsHoleSizeValid { get; set; }

    /// <summary>
    /// Whether this pad is a virtual pin.
    /// </summary>
    public bool IsVirtualPin { get; set; }

    /// <summary>
    /// Whether this pad is a counter hole.
    /// </summary>
    public bool IsCounterHole { get; set; }

    /// <summary>
    /// Whether top paste is enabled.
    /// </summary>
    public bool IsTopPasteEnabled { get; set; }

    /// <summary>
    /// Whether bottom paste is enabled.
    /// </summary>
    public bool IsBottomPasteEnabled { get; set; }

    /// <summary>
    /// Whether solder mask expansion is from hole edge with rule.
    /// </summary>
    public bool SolderMaskExpansionFromHoleEdgeWithRule { get; set; }

    /// <summary>
    /// Pad name (designator alias).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Swapped pad name.
    /// </summary>
    public string? SwappedPadName { get; set; }

    /// <summary>
    /// Jumper ID.
    /// </summary>
    public int JumperID { get; set; }

    /// <summary>
    /// Owner part ID.
    /// </summary>
    public int OwnerPartID { get; set; }

    /// <summary>
    /// Daisy chain style.
    /// </summary>
    public int DaisyChainStyle { get; set; }

    /// <summary>
    /// Whether pad has offset on any layer.
    /// </summary>
    public bool PadHasOffsetOnAny { get; set; }

    /// <summary>
    /// X pad offset for all layers.
    /// </summary>
    public Coord XPadOffsetAll { get; set; }

    /// <summary>
    /// Y pad offset for all layers.
    /// </summary>
    public Coord YPadOffsetAll { get; set; }

    /// <summary>
    /// Pin package length.
    /// </summary>
    public Coord PinPackageLength { get; set; }

    /// <summary>
    /// Maximum C signal layers size.
    /// </summary>
    public Coord MaxCSignalLayers { get; set; }

    /// <summary>
    /// Maximum X signal layers size.
    /// </summary>
    public Coord MaxXSignalLayers { get; set; }

    /// <summary>
    /// Maximum Y signal layers size.
    /// </summary>
    public Coord MaxYSignalLayers { get; set; }

    /// <summary>
    /// Top layer X size.
    /// </summary>
    public Coord TopXSize { get; set; }

    /// <summary>
    /// Top layer Y size.
    /// </summary>
    public Coord TopYSize { get; set; }

    /// <summary>
    /// Mid layer X size.
    /// </summary>
    public Coord MidXSize { get; set; }

    /// <summary>
    /// Mid layer Y size.
    /// </summary>
    public Coord MidYSize { get; set; }

    /// <summary>
    /// Bottom layer X size.
    /// </summary>
    public Coord BotXSize { get; set; }

    /// <summary>
    /// Bottom layer Y size.
    /// </summary>
    public Coord BotYSize { get; set; }

    /// <summary>
    /// Top layer shape.
    /// </summary>
    public int TopShape { get; set; }

    /// <summary>
    /// Mid layer shape.
    /// </summary>
    public int MidShape { get; set; }

    /// <summary>
    /// Bottom layer shape.
    /// </summary>
    public int BotShape { get; set; }

    /// <summary>
    /// Whether this pad is locked from editing.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Whether this pad allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this pad is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    // --- Size/Shape block fields ---

    /// <summary>
    /// Per-layer X sizes for 29 internal copper layers (raw coord values).
    /// Index 0 = first internal layer, index 28 = last internal layer.
    /// </summary>
    public int[] LayerXSizes { get; set; } = new int[29];

    /// <summary>
    /// Per-layer Y sizes for 29 internal copper layers (raw coord values).
    /// Index 0 = first internal layer, index 28 = last internal layer.
    /// </summary>
    public int[] LayerYSizes { get; set; } = new int[29];

    /// <summary>
    /// Per-layer shapes for 29 internal copper layers.
    /// Values correspond to <see cref="PadShape"/> enum values.
    /// </summary>
    public byte[] InternalLayerShapes { get; set; } = new byte[29];

    /// <summary>
    /// Hole slot length for slot holes (raw coord value).
    /// Only meaningful when <see cref="HoleType"/> is <see cref="PadHoleType.Slot"/>.
    /// </summary>
    public int HoleSlotLength { get; set; }

    /// <summary>
    /// Per-layer X offsets from hole center (32 layers, raw coord values).
    /// Index 0 = top, index 31 = bottom.
    /// </summary>
    public int[] OffsetXFromHoleCenter { get; set; } = new int[32];

    /// <summary>
    /// Per-layer Y offsets from hole center (32 layers, raw coord values).
    /// Index 0 = top, index 31 = bottom.
    /// </summary>
    public int[] OffsetYFromHoleCenter { get; set; } = new int[32];

    /// <summary>
    /// Flag indicating per-layer rounded rectangle shape overrides are active.
    /// When non-zero, <see cref="PerLayerShapes"/> and <see cref="PerLayerCornerRadii"/> are authoritative.
    /// </summary>
    public byte HasRoundedRectByte { get; set; }

    /// <summary>
    /// Per-layer shape overrides (32 layers).
    /// Index 0 = top copper, index 31 = bottom copper.
    /// Values correspond to <see cref="PadShape"/> enum values.
    /// </summary>
    public byte[] PerLayerShapes { get; set; } = new byte[32];

    /// <summary>
    /// Per-layer corner radius percentages (32 layers, 0-100).
    /// Index 0 = top copper, index 31 = bottom copper.
    /// </summary>
    public byte[] PerLayerCornerRadii { get; set; } = new byte[32];

    /// <summary>
    /// Whether the extended size/shape block is present.
    /// When true, the writer outputs the full 596-byte size/shape block.
    /// When false, an empty block is written.
    /// </summary>
    public bool HasSizeShapeBlock { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => CoordRect.FromCenter(Location, SizeTop.X, SizeTop.Y);

    /// <summary>
    /// Creates a fluent builder for a new pad.
    /// </summary>
    public static PadBuilder Create(string? designator = null) => new(designator);
}

/// <summary>
/// Fluent builder for creating PCB pads.
/// </summary>
public sealed class PadBuilder
{
    private readonly PcbPad _pad = new();

    internal PadBuilder(string? designator)
    {
        _pad.Designator = designator;
    }

    /// <summary>
    /// Sets the pad location.
    /// </summary>
    public PadBuilder At(Coord x, Coord y)
    {
        _pad.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the pad location.
    /// </summary>
    public PadBuilder At(CoordPoint location)
    {
        _pad.Location = location;
        return this;
    }

    /// <summary>
    /// Sets the pad size (same for all layers).
    /// </summary>
    public PadBuilder Size(Coord width, Coord height)
    {
        var size = new CoordPoint(width, height);
        _pad.SizeTop = size;
        _pad.SizeMiddle = size;
        _pad.SizeBottom = size;
        return this;
    }

    /// <summary>
    /// Sets the pad size (same for all layers, circular).
    /// </summary>
    public PadBuilder Size(Coord diameter)
    {
        return Size(diameter, diameter);
    }

    /// <summary>
    /// Sets the pad shape (same for all layers).
    /// </summary>
    public PadBuilder Shape(PadShape shape)
    {
        _pad.ShapeTop = shape;
        _pad.ShapeMiddle = shape;
        _pad.ShapeBottom = shape;
        return this;
    }

    /// <summary>
    /// Configures as a through-hole pad with the specified hole size.
    /// </summary>
    public PadBuilder ThroughHole(Coord holeSize)
    {
        _pad.HoleSize = holeSize;
        _pad.IsPlated = true;
        return this;
    }

    /// <summary>
    /// Configures as an SMD pad on the specified layer.
    /// </summary>
    public PadBuilder Smd(int layer = 1) // 1 = Top layer
    {
        _pad.HoleSize = Coord.Zero;
        _pad.Layer = layer;
        return this;
    }

    /// <summary>
    /// Sets the rotation angle.
    /// </summary>
    public PadBuilder Rotation(double degrees)
    {
        _pad.Rotation = degrees;
        return this;
    }

    /// <summary>
    /// Assigns the pad to a net.
    /// </summary>
    public PadBuilder Net(string netName)
    {
        _pad.Net = netName;
        return this;
    }

    /// <summary>
    /// Sets the corner radius for rounded rectangle pads.
    /// </summary>
    public PadBuilder CornerRadius(int percentage)
    {
        _pad.CornerRadiusPercentage = Math.Clamp(percentage, 0, 100);
        return this;
    }

    /// <summary>
    /// Sets the hole size.
    /// </summary>
    public PadBuilder HoleSize(Coord size)
    {
        _pad.HoleSize = size;
        return this;
    }

    /// <summary>
    /// Sets whether the pad is plated.
    /// </summary>
    public PadBuilder Plated(bool isPlated = true)
    {
        _pad.IsPlated = isPlated;
        return this;
    }

    /// <summary>
    /// Sets the pad designator.
    /// </summary>
    public PadBuilder WithDesignator(string? designator)
    {
        _pad.Designator = designator;
        return this;
    }

    /// <summary>
    /// Sets the layer for SMD pads.
    /// </summary>
    public PadBuilder Layer(int layer)
    {
        _pad.Layer = layer;
        return this;
    }

    /// <summary>
    /// Builds the pad.
    /// </summary>
    public PcbPad Build() => _pad;

    /// <summary>
    /// Implicit conversion to PcbPad.
    /// </summary>
    public static implicit operator PcbPad(PadBuilder builder) => builder.Build();
}
