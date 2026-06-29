using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB filled rectangular region.
/// </summary>
public sealed class PcbFill : IPcbFill
{
    /// <inheritdoc />
    public CoordPoint Corner1 { get; set; }

    /// <inheritdoc />
    public CoordPoint Corner2 { get; set; }

    /// <summary>
    /// Layer on which the fill resides.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Whether the fill is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Net name this fill belongs to.
    /// </summary>
    public string? Net { get; set; }

    /// <summary>
    /// Unique identifier for this fill.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether this fill is enabled (active in the design).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this fill is a keepout region.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Whether user routed this fill.
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
    /// Whether this fill has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this fill is part of a polygon outline.
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
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Keepout restriction bitmask (per object type: via/track/copper/SMD pad/TH pad).
    /// </summary>
    public byte KeepoutRestrictions { get; set; }

    /// <summary>
    /// Whether this fill is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether this fill allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this fill is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Whether this fill is redundant.
    /// </summary>
    public bool IsRedundant { get; set; }

    /// <summary>
    /// Net index into the board's net list (0xFFFF = no net).
    /// </summary>
    public ushort NetIndex { get; set; } = 0xFFFF;

    /// <summary>
    /// Component index into the board's component list (-1 = not part of a component).
    /// </summary>
    public int ComponentIndex { get; set; } = -1;

    /// <inheritdoc />
    public CoordRect Bounds => new(Corner1, Corner2);

    /// <summary>
    /// Width of the fill (distance between corners in X).
    /// </summary>
    public Coord Width => Coord.Abs(Corner2.X - Corner1.X);

    /// <summary>
    /// Height of the fill (distance between corners in Y).
    /// </summary>
    public Coord Height => Coord.Abs(Corner2.Y - Corner1.Y);

    /// <summary>
    /// Moves this fill by the given offset, shifting both <see cref="Corner1"/> and <see cref="Corner2"/>.
    /// </summary>
    /// <param name="dx">The X offset.</param>
    /// <param name="dy">The Y offset.</param>
    public void Translate(Coord dx, Coord dy)
    {
        Corner1 = Corner1.Offset(dx, dy);
        Corner2 = Corner2.Offset(dx, dy);
    }

    /// <summary>
    /// Rotates this fill counter-clockwise by <paramref name="degrees"/> about <paramref name="pivot"/>.
    /// A fill is an axis-aligned rectangle plus a separate <see cref="Rotation"/> spin, so the centre is
    /// rotated about the pivot, the width/height (corner span) are preserved, and <paramref name="degrees"/>
    /// is added to <see cref="Rotation"/> — keeping non-cardinal angles representable.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (counter-clockwise).</param>
    /// <param name="pivot">The point to rotate about.</param>
    public void Rotate(double degrees, CoordPoint pivot)
    {
        var halfW = Coord.Abs(Corner2.X - Corner1.X) / 2.0;
        var halfH = Coord.Abs(Corner2.Y - Corner1.Y) / 2.0;
        var center = new CoordPoint((Corner1.X + Corner2.X) / 2.0, (Corner1.Y + Corner2.Y) / 2.0)
            .RotateAround(pivot, degrees);
        Corner1 = new CoordPoint(center.X - halfW, center.Y - halfH);
        Corner2 = new CoordPoint(center.X + halfW, center.Y + halfH);
        Rotation = PcbRotation.Normalize360(Rotation + degrees);
    }

    /// <summary>
    /// Creates a fluent builder for a new fill.
    /// </summary>
    public static FillBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating PCB fills.
/// </summary>
public sealed class FillBuilder
{
    private readonly PcbFill _fill = new();

    internal FillBuilder() { }

    /// <summary>
    /// Sets the first corner.
    /// </summary>
    public FillBuilder From(Coord x, Coord y)
    {
        _fill.Corner1 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the second corner.
    /// </summary>
    public FillBuilder To(Coord x, Coord y)
    {
        _fill.Corner2 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the layer.
    /// </summary>
    public FillBuilder OnLayer(int layer)
    {
        _fill.Layer = layer;
        return this;
    }

    /// <summary>
    /// Sets the rotation angle.
    /// </summary>
    public FillBuilder Rotation(double degrees)
    {
        _fill.Rotation = degrees;
        return this;
    }

    /// <summary>
    /// Sets whether the fill is locked.
    /// </summary>
    public FillBuilder Locked(bool locked = true)
    {
        _fill.IsLocked = locked;
        return this;
    }

    /// <summary>
    /// Builds the fill.
    /// </summary>
    public PcbFill Build() => _fill;

    /// <summary>Implicitly converts a <see cref="FillBuilder"/> to a <see cref="PcbFill"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator PcbFill(FillBuilder builder) => builder.Build();
}
