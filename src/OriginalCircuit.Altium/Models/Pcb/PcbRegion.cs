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

    /// <summary>Net index into the board's net list (0xFFFF = no net).</summary>
    public ushort NetIndex { get; set; } = 0xFFFF;

    /// <summary>Polygon index this region belongs to (0xFFFF = none; regions in the corpus use 0).</summary>
    public ushort PolygonIndex { get; set; } = 0xFFFF;

    /// <summary>Component index into the board's component list (-1 = not part of a component).</summary>
    public int ComponentIndex { get; set; } = -1;

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
    /// The layer name as stored in the nested parameter block's <c>V7_LAYER</c> key (verbatim token,
    /// e.g. <c>TOP</c>, <c>KEEPOUT</c>). Null for regions built from scratch, in which case the writer
    /// derives the token from <see cref="Layer"/>.
    /// </summary>
    public string? V7LayerName { get; set; }

    /// <summary>Board-region layer token (the optional <c>LAYER</c> key on layer-stack regions, e.g. <c>KEEPOUT</c>); null when absent.</summary>
    public string? BoardRegionLayer { get; set; }

    /// <summary>The optional <c>KEEPOUT</c> param flag on layer-stack regions; null when absent.</summary>
    public bool? BoardRegionKeepout { get; set; }

    /// <summary>Whether this is a board cutout (the optional <c>ISBOARDCUTOUT</c> key); null when absent.</summary>
    public bool? IsBoardCutout { get; set; }

    /// <summary>Sub-polygon index (the <c>SUBPOLYINDEX</c> key); -1 when the region is not a polygon sub-shape.</summary>
    public int SubPolyIndex { get; set; } = -1;

    /// <summary>Whether the region is shape-based (the <c>ISSHAPEBASED</c> key).</summary>
    public bool IsShapeBased { get; set; }

    /// <summary>Keepout restriction flags (the optional <c>KEEPOUTRESTRICTIONS</c> key); null when absent.</summary>
    public int? KeepoutRestrictions { get; set; }

    /// <summary>Owning pad index (the optional <c>PADINDEX</c> key); null when absent.</summary>
    public int? PadIndex { get; set; }

    /// <summary>Object kind token on layer-stack regions (the optional <c>OBJECTKIND</c> key, e.g. <c>BoardRegion</c>); null when absent.</summary>
    public string? ObjectKind { get; set; }

    /// <summary>Bending-line count for rigid-flex layer-stack regions (the optional <c>BENDINGLINECOUNT</c> key); null when absent.</summary>
    public int? BendingLineCount { get; set; }

    /// <summary>The optional 3D-lock flag on layer-stack regions (the <c>LOCKED3D</c> key); null when absent.</summary>
    public bool? Locked3D { get; set; }

    /// <summary>Layer-stack id GUID for layer-stack regions (the optional <c>LAYERSTACKID</c> key); null when absent.</summary>
    public string? LayerStackId { get; set; }

    /// <summary>
    /// Additional parameters from the nested C-string block that are not modeled as typed properties.
    /// Preserved for round-trip fidelity.
    /// </summary>
    public Dictionary<string, string>? AdditionalParameters { get; set; }

    /// <summary>
    /// Exact outline vertices as the IEEE doubles Altium stores (sub-coordinate precision the integer
    /// <see cref="Outline"/> truncates). Populated on read and aligned 1:1 with <see cref="Outline"/>;
    /// the writer emits these when present so a loaded region round-trips byte-for-byte. Null for regions
    /// built from scratch, in which case the integer <see cref="Outline"/> is encoded as whole doubles.
    /// </summary>
    internal List<(double X, double Y)>? OutlineExact { get; set; }

    /// <summary>
    /// Exact hole-contour vertices as IEEE doubles, aligned 1:1 with <see cref="Holes"/>. Null from scratch.
    /// </summary>
    internal List<List<(double X, double Y)>>? HolesExact { get; set; }

    /// <summary>
    /// The raw 16-bit primitive flags word as read from the source, so unmodelled flag bits round-trip
    /// verbatim. Null for regions built from scratch. See PcbBinaryConstants.MergeFlags.
    /// </summary>
    internal ushort? RawFlags { get; set; }

    /// <summary>
    /// Hole / cutout contours. Each hole is a closed polygon of vertices subtracted from the
    /// region outline (copper-pour cutouts). Preserved for round-trip and rendering.
    /// </summary>
    public List<List<CoordPoint>> Holes { get; set; } = new();

    /// <summary>
    /// Adds a point to the region outline.
    /// </summary>
    internal void AddPoint(CoordPoint point) => _outline.Add(point);

    /// <summary>
    /// Replaces the region outline with the given vertices. Clears any captured sub-coordinate
    /// (<see cref="OutlineExact"/>) shadow so the new integer outline is authoritative on write.
    /// Hole contours are left unchanged.
    /// </summary>
    /// <param name="points">The new outline vertices, in order.</param>
    public void SetOutline(IEnumerable<CoordPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        _outline.Clear();
        _outline.AddRange(points);
        OutlineExact = null; // the integer outline now defines the region; drop the stale exact shadow
    }

    /// <summary>
    /// Moves this region by the given offset, shifting every outline and hole vertex (including the
    /// captured sub-coordinate <see cref="OutlineExact"/>/<see cref="HolesExact"/> shadows the writer
    /// emits, so the move survives a byte-faithful round-trip).
    /// </summary>
    /// <param name="dx">The X offset.</param>
    /// <param name="dy">The Y offset.</param>
    public void Translate(Coord dx, Coord dy)
    {
        for (var i = 0; i < _outline.Count; i++)
            _outline[i] = _outline[i].Offset(dx, dy);

        var rx = (double)dx.ToRaw();
        var ry = (double)dy.ToRaw();

        if (OutlineExact is { } oe)
            for (var i = 0; i < oe.Count; i++)
                oe[i] = (oe[i].X + rx, oe[i].Y + ry);

        foreach (var hole in Holes)
            for (var i = 0; i < hole.Count; i++)
                hole[i] = hole[i].Offset(dx, dy);

        if (HolesExact is { } he)
            foreach (var hole in he)
                for (var i = 0; i < hole.Count; i++)
                    hole[i] = (hole[i].X + rx, hole[i].Y + ry);
    }

    /// <summary>
    /// Rotates this region counter-clockwise by <paramref name="degrees"/> about <paramref name="pivot"/>,
    /// turning every outline and hole vertex (including the captured sub-coordinate
    /// <see cref="OutlineExact"/>/<see cref="HolesExact"/> shadows the writer emits). The region has no
    /// orientation field; its shape is defined entirely by the vertices.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (counter-clockwise).</param>
    /// <param name="pivot">The point to rotate about.</param>
    public void Rotate(double degrees, CoordPoint pivot)
    {
        for (var i = 0; i < _outline.Count; i++)
            _outline[i] = _outline[i].RotateAround(pivot, degrees);

        foreach (var hole in Holes)
            for (var i = 0; i < hole.Count; i++)
                hole[i] = hole[i].RotateAround(pivot, degrees);

        var (cos, sin) = PcbRotation.CosSin(degrees);
        double cx = pivot.X.ToRaw(), cy = pivot.Y.ToRaw();

        if (OutlineExact is { } oe)
            for (var i = 0; i < oe.Count; i++)
                oe[i] = PcbRotation.RotateRaw(oe[i].X, oe[i].Y, cx, cy, cos, sin);

        if (HolesExact is { } he)
            foreach (var hole in he)
                for (var i = 0; i < hole.Count; i++)
                    hole[i] = PcbRotation.RotateRaw(hole[i].X, hole[i].Y, cx, cy, cos, sin);
    }

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
