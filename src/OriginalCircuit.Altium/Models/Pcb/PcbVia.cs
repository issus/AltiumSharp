using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB via.
/// </summary>
public sealed class PcbVia : IPcbVia
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public Coord Diameter { get; set; }

    /// <summary>Net index into the board's net list (0xFFFF = no net).</summary>
    public ushort NetIndex { get; set; } = 0xFFFF;

    /// <summary>Component index into the board's component list (-1 = not part of a component).</summary>
    public int ComponentIndex { get; set; } = -1;

    /// <inheritdoc />
    public Coord HoleSize { get; set; }

    /// <summary>
    /// Starting layer for the via.
    /// </summary>
    public int StartLayer { get; set; } = 1; // Top

    /// <summary>
    /// Ending layer for the via.
    /// </summary>
    public int EndLayer { get; set; } = 32; // Bottom

    /// <summary>
    /// Net name this via belongs to.
    /// </summary>
    public string? Net { get; set; }

    /// <summary>
    /// Whether this via is tented (covered with solder mask).
    /// </summary>
    public bool IsTented { get; set; }

    /// <summary>
    /// Whether this via is a test point.
    /// </summary>
    public bool IsTestPoint { get; set; }

    /// <summary>
    /// Solder mask expansion override.
    /// </summary>
    public Coord SolderMaskExpansion { get; set; }

    /// <summary>
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Whether this via is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether this via allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this via is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Whether the top side of the via is tented (covered with solder mask).
    /// </summary>
    public bool IsTentingTop { get; set; }

    /// <summary>
    /// Whether the bottom side of the via is tented (covered with solder mask).
    /// </summary>
    public bool IsTentingBottom { get; set; }

    /// <summary>
    /// Unique identifier for this via.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether this via is enabled (active in the design).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this via is locked from editing.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Whether this via is a keepout region.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Layer this via is on (typically 74 = MultiLayer).
    /// </summary>
    public int Layer { get; set; } = 74;

    /// <summary>
    /// Diameter stack mode (0 = Simple).
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Via barrel height (board stackup thickness between start and end layers).
    /// </summary>
    public Coord Height { get; set; }

    /// <summary>
    /// Whether this via is plated (always true for standard vias).
    /// </summary>
    public bool IsPlated { get; set; } = true;

    /// <summary>
    /// Thermal relief air gap width.
    /// </summary>
    public Coord ThermalReliefAirGap { get; set; }

    /// <summary>
    /// Number of thermal relief conductors (spokes).
    /// </summary>
    public int ThermalReliefConductors { get; set; }

    /// <summary>
    /// Thermal relief conductor width.
    /// </summary>
    public Coord ThermalReliefConductorsWidth { get; set; }

    /// <summary>
    /// Power plane clearance.
    /// </summary>
    public Coord PowerPlaneClearance { get; set; }

    /// <summary>
    /// Power plane relief expansion.
    /// </summary>
    public Coord PowerPlaneReliefExpansion { get; set; }

    /// <summary>
    /// Power plane connection style (Altium TPlaneConnectStyle: 0=Relief, 1=Direct, 2=NoConnect).
    /// </summary>
    public int PowerPlaneConnectStyle { get; set; }

    /// <summary>
    /// Whether solder mask expansion is overridden per-object.
    /// </summary>
    public bool SolderMaskExpansionManual { get; set; }

    /// <summary>
    /// Solder mask expansion mode (0 = None, 1 = From rule, 2 = Manual). Defaults to From rule.
    /// </summary>
    public int SolderMaskExpansionMode { get; set; } = 1;

    /// <summary>
    /// Drill layer pair type (0=Through, 1=BlindBuriedStart, 2=BlindBuriedMid, 3=BlindBuriedEnd).
    /// </summary>
    public int DrillLayerPairType { get; set; }

    /// <summary>
    /// Hole positive (upper) drill tolerance. Defaults to the "unset" sentinel (0x7FFFFFFF).
    /// </summary>
    public Coord HolePositiveTolerance { get; set; } = Coord.FromRaw(int.MaxValue);

    /// <summary>
    /// Hole negative (lower) drill tolerance. Defaults to the "unset" sentinel (0x7FFFFFFF).
    /// </summary>
    public Coord HoleNegativeTolerance { get; set; } = Coord.FromRaw(int.MaxValue);

    /// <summary>
    /// Whether this is a free primitive (not owned by a component). Derived from
    /// <see cref="ComponentIndex"/> (&lt; 0 means free), which is the authoritative ownership signal.
    /// </summary>
    public bool IsFreePrimitive => ComponentIndex < 0;

    /// <summary>
    /// Whether this via is part of a pre-route.
    /// </summary>
    public bool IsPreRoute { get; set; }

    /// <summary>
    /// Whether this via has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this via is a top-side test point.
    /// </summary>
    public bool IsTestpointTop { get; set; }

    /// <summary>
    /// Whether this via is a bottom-side test point.
    /// </summary>
    public bool IsTestpointBottom { get; set; }

    /// <summary>
    /// Whether this via is a top-side assembly test point.
    /// </summary>
    public bool IsAssyTestpointTop { get; set; }

    /// <summary>
    /// Whether this via is a bottom-side assembly test point.
    /// </summary>
    public bool IsAssyTestpointBottom { get; set; }

    /// <summary>
    /// Whether this via is an electrical primitive.
    /// </summary>
    public bool IsElectricalPrim { get; set; }

    /// <summary>
    /// Whether this via is a backdrill via.
    /// </summary>
    public bool IsBackdrill { get; set; }

    /// <summary>
    /// Whether this via is a counter hole.
    /// </summary>
    public bool IsCounterHole { get; set; }

    /// <summary>
    /// Whether user routed this via.
    /// </summary>
    public bool UserRouted { get; set; }

    /// <summary>
    /// Union index for grouped primitives.
    /// </summary>
    public int UnionIndex { get; set; }

    /// <summary>
    /// Whether this is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

    /// <summary>
    /// Solder mask expansion from hole edge flag.
    /// </summary>
    public bool SolderMaskExpansionFromHoleEdge { get; set; }

    /// <summary>
    /// The size of the via (alias for Diameter).
    /// </summary>
    public Coord Size
    {
        get => Diameter;
        set => Diameter = value;
    }

    /// <summary>
    /// High layer (end layer for via span).
    /// </summary>
    public int HighLayer
    {
        get => EndLayer;
        set => EndLayer = value;
    }

    /// <summary>
    /// Low layer (start layer for via span).
    /// </summary>
    public int LowLayer
    {
        get => StartLayer;
        set => StartLayer = value;
    }

    /// <summary>
    /// Per-layer diameter values (32 entries, one per signal layer).
    /// Used when Mode is TopMiddleBottom or FullStack.
    /// </summary>
    public Coord[] Diameters { get; } = new Coord[32];

    /// <summary>
    /// Per-via unique identity GUID (SubRecord-1 offsets 259-274, "uid"). Round-tripped from a loaded
    /// via; freshly generated per via when authored from scratch.
    /// </summary>
    public Guid IdentityGuid { get; set; }

    /// <summary>
    /// Footprint/component-scoped identity GUID (SubRecord-1 offsets 275-290, "sig"). Shared across a
    /// component's vias and distinct per footprint; round-tripped from a loaded via, generated when
    /// authored from scratch.
    /// </summary>
    public Guid IdentityGuidB { get; set; }

    // --- SubRecord-1 cache / reserved bytes modeled (not replayed) so a loaded via round-trips
    // byte-for-byte while a from-scratch via gets sensible defaults Altium revalidates. ---
    internal int CacheValid61 { get; set; } = 0x01010101;   // 61-64 cache-validity word
    internal byte CacheValid67 { get; set; } = 0x01;        // 67 cache-validity byte
    internal byte ReservedByte70 { get; set; }              // 70 (low-cardinality reserved/index byte)
    internal byte ReservedByte72 { get; set; }              // 72 (flag byte 0x00/0x10/0x20)
    internal int SolderMaskBackRaw { get; set; }            // 242-245 back-side mask (usually == front, 0 for some)

    /// <inheritdoc />
    public CoordRect Bounds => CoordRect.FromCenter(Location, Diameter, Diameter);

    /// <summary>
    /// Moves this via by the given offset, shifting its <see cref="Location"/>.
    /// </summary>
    /// <param name="dx">The X offset.</param>
    /// <param name="dy">The Y offset.</param>
    public void Translate(Coord dx, Coord dy) => Location = Location.Offset(dx, dy);

    /// <summary>
    /// Creates a fluent builder for a new via.
    /// </summary>
    public static ViaBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating PCB vias.
/// </summary>
public sealed class ViaBuilder
{
    private readonly PcbVia _via = new();

    internal ViaBuilder()
    {
        // Fresh identity for from-scratch authoring (loaded vias overwrite these from the file).
        _via.IdentityGuid = Guid.NewGuid();   // uid: per-via
        _via.IdentityGuidB = Guid.NewGuid();  // sig: per pad-stack
    }

    /// <summary>
    /// Sets the via location.
    /// </summary>
    public ViaBuilder At(Coord x, Coord y)
    {
        _via.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the via location.
    /// </summary>
    public ViaBuilder At(CoordPoint location)
    {
        _via.Location = location;
        return this;
    }

    /// <summary>
    /// Sets the via diameter.
    /// </summary>
    public ViaBuilder Diameter(Coord diameter)
    {
        _via.Diameter = diameter;
        return this;
    }

    /// <summary>
    /// Sets the hole size.
    /// </summary>
    public ViaBuilder HoleSize(Coord holeSize)
    {
        _via.HoleSize = holeSize;
        return this;
    }

    /// <summary>
    /// Configures as a through-hole via (top to bottom).
    /// </summary>
    public ViaBuilder ThroughHole()
    {
        _via.StartLayer = 1;
        _via.EndLayer = 32;
        return this;
    }

    /// <summary>
    /// Configures as a blind via.
    /// </summary>
    public ViaBuilder Blind(int startLayer, int endLayer)
    {
        _via.StartLayer = startLayer;
        _via.EndLayer = endLayer;
        return this;
    }

    /// <summary>
    /// Sets the start and end layers.
    /// </summary>
    public ViaBuilder Layers(int fromLayer, int toLayer)
    {
        _via.StartLayer = fromLayer;
        _via.EndLayer = toLayer;
        return this;
    }

    /// <summary>
    /// Sets whether the via is tented.
    /// </summary>
    public ViaBuilder Tented(bool tented = true)
    {
        _via.IsTented = tented;
        return this;
    }

    /// <summary>
    /// Assigns the via to a net.
    /// </summary>
    public ViaBuilder Net(string netName)
    {
        _via.Net = netName;
        return this;
    }

    /// <summary>
    /// Builds the via.
    /// </summary>
    public PcbVia Build() => _via;

    /// <summary>
    /// Implicit conversion to PcbVia.
    /// </summary>
    public static implicit operator PcbVia(ViaBuilder builder) => builder.Build();
}
