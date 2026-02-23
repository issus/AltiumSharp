using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB arc.
/// </summary>
public sealed class PcbArc : IPcbArc
{
    /// <inheritdoc />
    public CoordPoint Center { get; set; }

    /// <inheritdoc />
    public Coord Radius { get; set; }

    /// <inheritdoc />
    public double StartAngle { get; set; }

    /// <inheritdoc />
    public double EndAngle { get; set; }

    /// <summary>
    /// Width of the arc stroke.
    /// </summary>
    public Coord Width { get; set; }

    /// <summary>
    /// Layer this arc is on.
    /// </summary>
    public int Layer { get; set; } = 1;

    /// <summary>
    /// Net name this arc belongs to.
    /// </summary>
    public string? Net { get; set; }

    /// <summary>
    /// Component index this arc belongs to.
    /// </summary>
    public int Component { get; set; }

    /// <summary>
    /// Unique identifier for this arc.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether this arc is enabled (active in the design).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this arc is locked from editing.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Whether this arc is a keepout region.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Whether this arc was user-routed.
    /// </summary>
    public bool UserRouted { get; set; }

    /// <summary>
    /// Union index for grouped primitives.
    /// </summary>
    public int UnionIndex { get; set; }

    /// <summary>
    /// Whether this arc is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

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
    /// Whether this arc has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

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
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Whether this arc is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether this arc allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this arc is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Gets the sweep angle of the arc.
    /// </summary>
    public double SweepAngle => EndAngle - StartAngle;

    /// <summary>
    /// Net index (0 = no net).
    /// </summary>
    public ushort NetIndex { get; set; }

    /// <summary>
    /// Component index (0 = not in component).
    /// </summary>
    public byte ComponentIndex { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            // Simplified bounding box (full circle bounds)
            var extent = Radius + Width / 2;
            return CoordRect.FromCenter(Center, extent * 2, extent * 2);
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new arc.
    /// </summary>
    public static ArcBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating PCB arcs.
/// </summary>
public sealed class ArcBuilder
{
    private readonly PcbArc _arc = new();

    internal ArcBuilder() { }

    /// <summary>
    /// Sets the center point.
    /// </summary>
    public ArcBuilder Center(Coord x, Coord y)
    {
        _arc.Center = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the center point.
    /// </summary>
    public ArcBuilder Center(CoordPoint center)
    {
        _arc.Center = center;
        return this;
    }

    /// <summary>
    /// Sets the center point (alias for Center).
    /// </summary>
    public ArcBuilder At(Coord x, Coord y)
    {
        _arc.Center = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the radius.
    /// </summary>
    public ArcBuilder Radius(Coord radius)
    {
        _arc.Radius = radius;
        return this;
    }

    /// <summary>
    /// Sets the start and end angles in degrees.
    /// </summary>
    public ArcBuilder Angles(double startDegrees, double endDegrees)
    {
        _arc.StartAngle = startDegrees;
        _arc.EndAngle = endDegrees;
        return this;
    }

    /// <summary>
    /// Creates a full circle.
    /// </summary>
    public ArcBuilder FullCircle()
    {
        _arc.StartAngle = 0;
        _arc.EndAngle = 360;
        return this;
    }

    /// <summary>
    /// Sets the stroke width.
    /// </summary>
    public ArcBuilder Width(Coord width)
    {
        _arc.Width = width;
        return this;
    }

    /// <summary>
    /// Sets the layer.
    /// </summary>
    public ArcBuilder OnLayer(int layer)
    {
        _arc.Layer = layer;
        return this;
    }

    /// <summary>
    /// Sets the layer (alias for OnLayer).
    /// </summary>
    public ArcBuilder Layer(int layer)
    {
        _arc.Layer = layer;
        return this;
    }

    /// <summary>
    /// Assigns the arc to a net.
    /// </summary>
    public ArcBuilder Net(string netName)
    {
        _arc.Net = netName;
        return this;
    }

    /// <summary>
    /// Builds the arc.
    /// </summary>
    public PcbArc Build() => _arc;

    /// <summary>
    /// Implicit conversion to PcbArc.
    /// </summary>
    public static implicit operator PcbArc(ArcBuilder builder) => builder.Build();
}
