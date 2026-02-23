using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB track (copper trace).
/// </summary>
public sealed class PcbTrack : IPcbTrack
{
    /// <inheritdoc />
    public CoordPoint Start { get; set; }

    /// <inheritdoc />
    public CoordPoint End { get; set; }

    /// <inheritdoc />
    public Coord Width { get; set; }

    /// <summary>
    /// Layer this track is on.
    /// </summary>
    public int Layer { get; set; } = 1; // Top layer

    /// <summary>
    /// Net name this track belongs to.
    /// </summary>
    public string? Net { get; set; }

    /// <summary>
    /// Whether this track is locked from editing.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Component index this track belongs to.
    /// </summary>
    public int Component { get; set; }

    /// <summary>
    /// Unique identifier for this track.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether this track was user-routed (vs auto-routed).
    /// </summary>
    public bool UserRouted { get; set; }

    /// <summary>
    /// Union index for grouped primitives.
    /// </summary>
    public int UnionIndex { get; set; }

    /// <summary>
    /// Whether this track is enabled (active in the design).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this track is a keepout region.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Whether this track is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

    /// <summary>
    /// Whether this is a free primitive (not owned by a component).
    /// </summary>
    public bool IsFreePrimitive { get; set; }

    /// <summary>
    /// Whether this track is an electrical primitive.
    /// </summary>
    public bool IsElectricalPrim { get; set; }

    /// <summary>
    /// Whether this track is a pre-route.
    /// </summary>
    public bool IsPreRoute { get; set; }

    /// <summary>
    /// Whether this track has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this track is a top-side test point.
    /// </summary>
    public bool IsTestpointTop { get; set; }

    /// <summary>
    /// Whether this track is a bottom-side test point.
    /// </summary>
    public bool IsTestpointBottom { get; set; }

    /// <summary>
    /// Whether this track is a top-side assembly test point.
    /// </summary>
    public bool IsAssyTestpointTop { get; set; }

    /// <summary>
    /// Whether this track is a bottom-side assembly test point.
    /// </summary>
    public bool IsAssyTestpointBottom { get; set; }

    /// <summary>
    /// Whether this track is tented.
    /// </summary>
    public bool IsTenting { get; set; }

    /// <summary>
    /// Whether the top side is tented.
    /// </summary>
    public bool IsTentingTop { get; set; }

    /// <summary>
    /// Whether the bottom side is tented.
    /// </summary>
    public bool IsTentingBottom { get; set; }

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
    /// Number of thermal relief entries (spokes).
    /// </summary>
    public int ReliefEntries { get; set; }

    /// <summary>
    /// Solder mask expansion.
    /// </summary>
    public Coord SolderMaskExpansion { get; set; }

    /// <summary>
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Whether this track is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether this track allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this track is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Net index (0 = no net). Stored in binary as a 2-byte value at offset 33.
    /// </summary>
    public ushort NetIndex { get; set; }

    /// <summary>
    /// Component index (0 = not in component). Stored in binary at offset 35.
    /// </summary>
    public byte ComponentIndex { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var halfWidth = Width / 2;
            var minX = Coord.Min(Start.X, End.X) - halfWidth;
            var maxX = Coord.Max(Start.X, End.X) + halfWidth;
            var minY = Coord.Min(Start.Y, End.Y) - halfWidth;
            var maxY = Coord.Max(Start.Y, End.Y) + halfWidth;
            return new CoordRect(minX, minY, maxX, maxY);
        }
    }

    /// <summary>
    /// Gets the length of this track in mils.
    /// </summary>
    public double LengthMils => Start.DistanceTo(End);

    /// <summary>
    /// Creates a fluent builder for a new track.
    /// </summary>
    public static TrackBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating PCB tracks.
/// </summary>
public sealed class TrackBuilder
{
    private readonly PcbTrack _track = new();

    internal TrackBuilder() { }

    /// <summary>
    /// Sets the starting point.
    /// </summary>
    public TrackBuilder From(Coord x, Coord y)
    {
        _track.Start = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the starting point.
    /// </summary>
    public TrackBuilder From(CoordPoint point)
    {
        _track.Start = point;
        return this;
    }

    /// <summary>
    /// Sets the ending point.
    /// </summary>
    public TrackBuilder To(Coord x, Coord y)
    {
        _track.End = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the ending point.
    /// </summary>
    public TrackBuilder To(CoordPoint point)
    {
        _track.End = point;
        return this;
    }

    /// <summary>
    /// Sets the track width.
    /// </summary>
    public TrackBuilder Width(Coord width)
    {
        _track.Width = width;
        return this;
    }

    /// <summary>
    /// Sets the layer.
    /// </summary>
    public TrackBuilder OnLayer(int layer)
    {
        _track.Layer = layer;
        return this;
    }

    /// <summary>
    /// Sets the layer (alias for OnLayer).
    /// </summary>
    public TrackBuilder Layer(int layer)
    {
        _track.Layer = layer;
        return this;
    }

    /// <summary>
    /// Assigns the track to a net.
    /// </summary>
    public TrackBuilder Net(string netName)
    {
        _track.Net = netName;
        return this;
    }

    /// <summary>
    /// Locks the track from editing.
    /// </summary>
    public TrackBuilder Locked(bool locked = true)
    {
        _track.IsLocked = locked;
        return this;
    }

    /// <summary>
    /// Builds the track.
    /// </summary>
    public PcbTrack Build() => _track;

    /// <summary>
    /// Implicit conversion to PcbTrack.
    /// </summary>
    public static implicit operator PcbTrack(TrackBuilder builder) => builder.Build();
}
