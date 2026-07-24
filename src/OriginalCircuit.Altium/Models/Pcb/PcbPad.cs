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
/// A per-layer full-stack opening entry (15 bytes) appended to a full-stack pad's size/shape
/// block. Models all bytes so the record round-trips without raw byte capture.
/// </summary>
public sealed class PadFullStackEntry
{
    /// <summary>Layer code this entry applies to.</summary>
    public byte LayerCode { get; set; }

    /// <summary>Four mode/enable flag bytes (exact semantics layer-stack dependent).</summary>
    public byte Flag1 { get; set; }

    /// <summary>Second flag byte.</summary>
    public byte Flag2 { get; set; }

    /// <summary>Third flag byte.</summary>
    public byte Flag3 { get; set; }

    /// <summary>Fourth flag byte.</summary>
    public byte Flag4 { get; set; }

    /// <summary>Opening size in X (raw internal units).</summary>
    public int SizeX { get; set; }

    /// <summary>Opening size in Y (raw internal units).</summary>
    public int SizeY { get; set; }

    /// <summary>Corner radius percentage (0-100).</summary>
    public byte CornerPercent { get; set; }

    /// <summary>Trailing reserved byte.</summary>
    public byte Trailing { get; set; }
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
    /// Net index into the board's net list (0xFFFF = no net).
    /// </summary>
    public ushort NetIndex { get; set; } = 0xFFFF;

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
    /// Power plane connection style (Altium TPlaneConnectStyle: 0=Relief, 1=Direct, 2=No Connect).
    /// For strongly-typed access use <see cref="PowerPlaneConnection"/>; to test whether thermal
    /// relief is in effect use <see cref="IsReliefEnabled"/>.
    /// </summary>
    public int PowerPlaneConnectStyle { get; set; }

    /// <summary>
    /// Strongly-typed view over <see cref="PowerPlaneConnectStyle"/>: how this pad connects to an
    /// internal power/ground plane (thermal relief, direct/solid, or no connect).
    /// </summary>
    public PlaneConnectStyle PowerPlaneConnection
    {
        get => (PlaneConnectStyle)PowerPlaneConnectStyle;
        set => PowerPlaneConnectStyle = (int)value;
    }

    /// <summary>
    /// Whether thermal relief is enabled for this pad's plane connection, i.e. whether
    /// <see cref="PowerPlaneConnection"/> is <see cref="PlaneConnectStyle.Relief"/>. When true, the
    /// relief geometry (<see cref="ReliefConductorWidth"/>, <see cref="ReliefEntries"/>,
    /// <see cref="ReliefAirGap"/> and <see cref="PowerPlaneReliefExpansion"/>) applies; when false the
    /// pad is a direct/solid connection or not connected to the plane.
    /// </summary>
    public bool IsReliefEnabled => PowerPlaneConnectStyle == (int)PlaneConnectStyle.Relief;

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
    /// Paste mask expansion mode (0 = None, 1 = From rule, 2 = Manual). Defaults to From rule.
    /// </summary>
    public int PasteMaskExpansionMode { get; set; } = 1;

    /// <summary>
    /// Solder mask expansion mode (0 = None, 1 = From rule, 2 = Manual). Defaults to From rule.
    /// </summary>
    public int SolderMaskExpansionMode { get; set; } = 1;

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
    /// Whether this is a free primitive (not owned by a component). Derived from
    /// <see cref="ComponentIndex"/> (&lt; 0 means free), which is the authoritative ownership signal.
    /// </summary>
    public bool IsFreePrimitive => ComponentIndex < 0;

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
    /// Hole positive (upper) drill tolerance. Defaults to the "unset" sentinel
    /// (<c>0x7FFFFFFF</c>), matching how Altium serializes a pad with no tolerance specified.
    /// </summary>
    public Coord HolePositiveTolerance { get; set; } = Coord.FromRaw(int.MaxValue);

    /// <summary>
    /// Hole negative (lower) drill tolerance. Defaults to the "unset" sentinel
    /// (<c>0x7FFFFFFF</c>), matching how Altium serializes a pad with no tolerance specified.
    /// </summary>
    public Coord HoleNegativeTolerance { get; set; } = Coord.FromRaw(int.MaxValue);

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
    public int[] LayerXSizes { get; } = new int[29];

    /// <summary>
    /// Per-layer Y sizes for 29 internal copper layers (raw coord values).
    /// Index 0 = first internal layer, index 28 = last internal layer.
    /// </summary>
    public int[] LayerYSizes { get; } = new int[29];

    /// <summary>
    /// Per-layer shapes for 29 internal copper layers.
    /// Values correspond to <see cref="PadShape"/> enum values.
    /// </summary>
    public byte[] InternalLayerShapes { get; } = new byte[29];

    /// <summary>
    /// Hole slot length for slot holes (raw coord value).
    /// Only meaningful when <see cref="HoleType"/> is <see cref="PadHoleType.Slot"/>.
    /// </summary>
    public int HoleSlotLength { get; set; }

    /// <summary>
    /// Per-layer X offsets from hole center (32 layers, raw coord values).
    /// Index 0 = top, index 31 = bottom.
    /// </summary>
    public int[] OffsetXFromHoleCenter { get; } = new int[32];

    /// <summary>
    /// Per-layer Y offsets from hole center (32 layers, raw coord values).
    /// Index 0 = top, index 31 = bottom.
    /// </summary>
    public int[] OffsetYFromHoleCenter { get; } = new int[32];

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
    public byte[] PerLayerShapes { get; } = new byte[32];

    /// <summary>
    /// Per-layer corner radius percentages (32 layers, 0-100).
    /// Index 0 = top copper, index 31 = bottom copper.
    /// </summary>
    public byte[] PerLayerCornerRadii { get; } = new byte[32];

    /// <summary>
    /// Whether the extended size/shape block is present.
    /// When true, the writer outputs the full 596-byte size/shape block.
    /// When false, an empty block is written.
    /// </summary>
    public bool HasSizeShapeBlock { get; set; }

    /// <summary>
    /// Length of SubRecord 5 (the main pad block) as read from the source. PcbLib pads use 202
    /// bytes; PcbDoc pads use 194. Captured so the writer reproduces the original length rather
    /// than always emitting 202. Defaults to 202 for pads created from scratch.
    /// </summary>
    internal int Sr5Length { get; set; } = 202;

    /// <summary>
    /// SubRecord 2 (a Pascal string, usually empty) captured from the source so non-default values
    /// round-trip. Null for pads built from scratch (written as an empty string).
    /// </summary>
    internal string? PadSubrecord2 { get; set; }

    /// <summary>
    /// SubRecord 3 (a Pascal string, usually "|&|0") captured from the source so non-default values
    /// round-trip. Null for pads built from scratch (written as "|&|0").
    /// </summary>
    internal string? PadNetString { get; set; }

    /// <summary>
    /// Per-layer full-stack opening entries appended after the 596-byte size/shape block for
    /// full-stack pads (e.g. rounded-rectangle SMD pads). Each entry carries a layer code, flags,
    /// X/Y size and corner percentage. Empty for simple pads.
    /// </summary>
    public List<PadFullStackEntry> FullStackEntries { get; } = new();

    /// <summary>
    /// Per-pad unique identity GUID (SubRecord-5 offsets 126-141, "GUID-A"). Round-tripped from a
    /// loaded pad; freshly generated per pad when authored from scratch (Altium does not enforce
    /// uniqueness — duplicates load fine — but distinct ids match how Altium authors new primitives).
    /// </summary>
    public Guid IdentityGuid { get; set; }

    /// <summary>
    /// Pad-stack / footprint-scoped identity GUID (SubRecord-5 offsets 142-157, "GUID-B"). Shared by
    /// all pads of one footprint and distinct per footprint; round-tripped from a loaded pad, generated
    /// once per component when authored from scratch.
    /// </summary>
    public Guid IdentityGuidB { get; set; }

    // --- SubRecord-5 thermal/mask cache-validity bytes (offsets 94-104) ---
    // Altium revalidates these on load, so the exact value is not required for a from-scratch pad to
    // open correctly; they are modeled (not replayed) only so a loaded pad round-trips byte-for-byte.
    // Defaults are Altium's "needs revalidation" template values.
    internal byte CachePlaneConnectionValid { get; set; }              // 96
    internal byte CacheReliefConductorWidthValid { get; set; }         // 97
    internal byte CacheReliefEntriesValid { get; set; }                // 98
    internal byte CacheReliefAirGapValid { get; set; }                 // 99
    internal byte CachePowerPlaneReliefExpansionValid { get; set; }    // 100
    internal byte CachePasteMaskExpansionValid { get; set; }           // 103
    internal byte CacheSolderMaskExpansionValid { get; set; }          // 104

    /// <summary>
    /// SubRecord-5 solder-mask cache word (offsets 121-124). Usually mirrors the manual
    /// <see cref="SolderMaskExpansion"/> but is 0 for some pads, so it is modeled (round-tripped)
    /// rather than derived. Altium revalidates it on load.
    /// </summary>
    internal int SolderMaskCache { get; set; }

    /// <summary>
    /// SubRecord-5 reserved marker byte at offset 185 (observed values 0x03/0x01/0x11; default 0x03).
    /// Modeled so a loaded pad round-trips exactly; Altium does not require a specific value here.
    /// </summary>
    internal byte ReservedMarker185 { get; set; } = 0x03;

    /// <summary>
    /// The main-block base shapes (ShapeTop/Middle/Bottom) modeled separately from the typed
    /// <see cref="ShapeTop"/> etc., which the reader overrides with the per-layer shape for rendering
    /// when overrides are active (<see cref="HasRoundedRectByte"/> set). Altium keeps a base shape in
    /// the main block while the real per-layer shape lives in <see cref="PerLayerShapes"/>; these
    /// preserve the base shapes for a byte-faithful round-trip. Null when the typed shapes are the base.
    /// </summary>
    internal (byte Top, byte Middle, byte Bottom)? MainBlockBaseShapes { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            if (Rotation == 0)
                return CoordRect.FromCenter(Location, SizeTop.X, SizeTop.Y);
            // Axis-aligned bounding box of the rotated pad rectangle: a rotated W x H rectangle
            // spans (W*|cos| + H*|sin|) x (W*|sin| + H*|cos|). Without this, a rotated pad reports
            // a too-small extent and gets clipped by AutoZoom framing.
            var rad = Rotation * System.Math.PI / 180.0;
            var cos = System.Math.Abs(System.Math.Cos(rad));
            var sin = System.Math.Abs(System.Math.Sin(rad));
            var width = SizeTop.X * cos + SizeTop.Y * sin;
            var height = SizeTop.X * sin + SizeTop.Y * cos;
            return CoordRect.FromCenter(Location, width, height);
        }
    }

    /// <summary>
    /// Moves this pad by the given offset, shifting its <see cref="Location"/>. Net assignment,
    /// size, shape and ownership are unchanged.
    /// </summary>
    /// <param name="dx">The X offset.</param>
    /// <param name="dy">The Y offset.</param>
    public void Translate(Coord dx, Coord dy) => Location = Location.Offset(dx, dy);

    /// <summary>
    /// Rotates this pad counter-clockwise by <paramref name="degrees"/> about <paramref name="pivot"/>,
    /// moving its <see cref="Location"/> and spinning its own <see cref="Rotation"/>.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (counter-clockwise).</param>
    /// <param name="pivot">The point to rotate about.</param>
    public void Rotate(double degrees, CoordPoint pivot)
    {
        Location = Location.RotateAround(pivot, degrees);
        Rotation = PcbRotation.Normalize360(Rotation + degrees);
    }

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
        // Fresh identity for from-scratch authoring (loaded pads overwrite these from the file).
        _pad.IdentityGuid = Guid.NewGuid();   // GUID-A: per-pad
        _pad.IdentityGuidB = Guid.NewGuid();  // GUID-B: per pad-stack (Altium tolerates per-pad ids)
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
    /// Sets how the pad connects to internal power/ground planes (thermal relief, direct or no connect).
    /// </summary>
    public PadBuilder PowerPlaneConnection(PlaneConnectStyle style)
    {
        _pad.PowerPlaneConnection = style;
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
