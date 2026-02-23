using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB region (polygon copper pour or keepout).
/// </summary>
public sealed class PcbRegion : IPcbRegion
{
    private readonly List<CoordPoint> _outline = new();

    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Outline => _outline;

    /// <summary>
    /// Layer on which the region resides.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Kind of region (0=Copper, 1=Cutout, etc.).
    /// </summary>
    public int Kind { get; set; }

    /// <summary>
    /// Whether the region is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Net name for copper regions.
    /// </summary>
    public string? Net { get; set; }

    /// <summary>
    /// Unique identifier for this region.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether this region is enabled (active in the design).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this region is a keepout.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Region name/identifier.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Cavity height for embedded components.
    /// </summary>
    public Coord CavityHeight { get; set; }

    /// <summary>
    /// Whether user routed this region.
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
    /// Whether this region has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this region is part of a polygon outline.
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
    /// Whether this region is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether this region allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this region is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Whether this is a simple region (no holes).
    /// </summary>
    public bool IsSimpleRegion { get; set; }

    /// <summary>
    /// Whether this is a virtual cutout.
    /// </summary>
    public bool VirtualCutout { get; set; }

    /// <summary>
    /// Number of holes in this region.
    /// </summary>
    public int HoleCount { get; set; }

    /// <summary>
    /// Total number of vertices across all contours.
    /// </summary>
    public int TotalVertexCount { get; set; }

    /// <summary>
    /// Area of the region in internal coordinate units squared.
    /// </summary>
    public long Area { get; set; }

    /// <summary>
    /// Arc approximation tolerance.
    /// </summary>
    public Coord ArcApproximation { get; set; }

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
    /// Additional parameters from the nested C-string block that are not modeled as typed properties.
    /// Preserved for round-trip fidelity.
    /// </summary>
    public Dictionary<string, string>? AdditionalParameters { get; set; }

    /// <summary>
    /// Adds a point to the region outline.
    /// </summary>
    internal void AddPoint(CoordPoint point) => _outline.Add(point);

    /// <summary>
    /// Creates a fluent builder for a new region.
    /// </summary>
    public static RegionBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating PCB regions.
/// </summary>
public sealed class RegionBuilder
{
    private readonly PcbRegion _region = new();

    internal RegionBuilder() { }

    /// <summary>
    /// Adds a point to the region outline.
    /// </summary>
    public RegionBuilder AddPoint(Coord x, Coord y)
    {
        _region.AddPoint(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Sets the layer.
    /// </summary>
    public RegionBuilder OnLayer(int layer)
    {
        _region.Layer = layer;
        return this;
    }

    /// <summary>
    /// Sets the region kind.
    /// </summary>
    public RegionBuilder Kind(int kind)
    {
        _region.Kind = kind;
        return this;
    }

    /// <summary>
    /// Sets whether the region is locked.
    /// </summary>
    public RegionBuilder Locked(bool locked = true)
    {
        _region.IsLocked = locked;
        return this;
    }

    /// <summary>
    /// Sets the net name.
    /// </summary>
    public RegionBuilder Net(string net)
    {
        _region.Net = net;
        return this;
    }

    /// <summary>
    /// Builds the region.
    /// </summary>
    public PcbRegion Build() => _region;

    /// <summary>Implicitly converts a <see cref="RegionBuilder"/> to a <see cref="PcbRegion"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator PcbRegion(RegionBuilder builder) => builder.Build();
}
