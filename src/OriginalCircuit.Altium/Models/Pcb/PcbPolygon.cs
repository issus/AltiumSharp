using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// One vertex of a <see cref="PcbPolygon"/> outline (the <c>KIND/VX/VY/CX/CY/SA/EA/R</c> per-vertex
/// keys). <see cref="Kind"/> 0 = line vertex, 1 = arc; the C/SA/EA/R fields apply to arcs.
/// </summary>
public sealed class PcbPolygonVertex
{
    public int Kind { get; set; }
    public Coord X { get; set; }
    public Coord Y { get; set; }
    public Coord CenterX { get; set; }
    public Coord CenterY { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }
    public Coord Radius { get; set; }
}

/// <summary>
/// Represents a PCB polygon copper pour.
/// </summary>
public sealed class PcbPolygon
{
    private readonly List<CoordPoint> _vertices = new();

    /// <summary>
    /// Polygon outline vertices (line points; projection of <see cref="OutlineVertices"/> X/Y).
    /// </summary>
    public IReadOnlyList<CoordPoint> Vertices => _vertices;

    /// <summary>Full per-vertex outline (line/arc with centers and angles), in source order.</summary>
    public List<PcbPolygonVertex> OutlineVertices { get; } = new();

    /// <summary>
    /// X location of the polygon.
    /// </summary>
    public Coord X { get; set; }

    /// <summary>
    /// Y location of the polygon.
    /// </summary>
    public Coord Y { get; set; }

    /// <summary>
    /// Layer this polygon is on.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Polygon name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Net name this polygon belongs to.
    /// </summary>
    public string? Net { get; set; }

    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <summary>
    /// Whether this polygon is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this polygon is a keepout.
    /// </summary>
    public bool IsKeepout { get; set; }

    /// <summary>
    /// Whether this is an electrical primitive.
    /// </summary>
    public bool IsElectricalPrim { get; set; }

    /// <summary>
    /// Whether this is a free primitive.
    /// </summary>
    public bool IsFreePrimitive { get; set; }

    /// <summary>
    /// Whether this is a pre-route.
    /// </summary>
    public bool IsPreRoute { get; set; }

    /// <summary>
    /// Whether this has a teardrop.
    /// </summary>
    public bool TearDrop { get; set; }

    /// <summary>
    /// Whether this is part of a polygon outline.
    /// </summary>
    public bool PolygonOutline { get; set; }

    /// <summary>
    /// Whether user routed this polygon.
    /// </summary>
    public bool UserRouted { get; set; }

    /// <summary>
    /// Union index for grouped primitives.
    /// </summary>
    public int UnionIndex { get; set; }

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
    /// Whether primitives are locked.
    /// </summary>
    public bool PrimitiveLock { get; set; }

    /// <summary>
    /// Polygon type token as stored (e.g. <c>Polygon</c>).
    /// </summary>
    public string PolygonType { get; set; } = "Polygon";

    /// <summary>
    /// Polygon hatch style token as stored (e.g. <c>Solid</c>).
    /// </summary>
    public string HatchStyle { get; set; } = "Solid";

    /// <summary>
    /// Whether the polygon pours over same-net objects (the <c>POUROVER</c> flag).
    /// </summary>
    public bool PourOver { get; set; }

    // Common primitive prefix + extra typed fields recovered for byte-exact Polygons6 round-trip.
    /// <summary>Selection flag (transient; FALSE on disk).</summary>
    public bool Selection { get; set; }
    /// <summary>Whether the polygon is locked (the <c>LOCKED</c> flag).</summary>
    public bool Locked { get; set; }
    /// <summary>Pour-over style code (the <c>POUROVERSTYLE</c> key).</summary>
    public int PourOverStyle { get; set; }
    /// <summary>Restore layer token (the <c>RESTORELAYER</c> key); typically <c>UNKNOWN</c>.</summary>
    public string RestoreLayer { get; set; } = "UNKNOWN";
    /// <summary>Restore net token (the <c>RESTORENET</c> key); typically empty.</summary>
    public string RestoreNet { get; set; } = string.Empty;
    /// <summary>Island-removal area threshold (the <c>AREATHRESHOLD</c> key); decimal for precision.</summary>
    public decimal AreaThreshold { get; set; }
    /// <summary>Whether neck width comes from a rule (optional <c>NECKWIDTHFROMRULE</c> key); null when absent.</summary>
    public bool? NeckWidthFromRule { get; set; }

    /// <summary>
    /// Border width.
    /// </summary>
    public Coord BorderWidth { get; set; }

    /// <summary>
    /// Track size for hatched polygon.
    /// </summary>
    public Coord TrackSize { get; set; }

    /// <summary>
    /// Grid size for hatched polygon.
    /// </summary>
    public Coord Grid { get; set; }

    /// <summary>
    /// Minimum track width.
    /// </summary>
    public Coord MinTrack { get; set; }

    /// <summary>
    /// Whether to avoid obstacles.
    /// </summary>
    public bool AvoidObstacles { get; set; }

    /// <summary>
    /// Whether to avoid obstacles (alternate spelling).
    /// </summary>
    public bool AvoidObsticles { get; set; }

    /// <summary>
    /// Whether arc pour mode is used.
    /// </summary>
    public bool ArcPourMode { get; set; }

    /// <summary>
    /// Whether the copper-fill cache is marked invalid (the optional <c>COPPERINVALIDATE</c> key,
    /// emitted between <c>IGNOREVIOLATIONS</c> and <c>AUTONAME</c>); null when the key is absent.
    /// </summary>
    public bool? CopperInvalidate { get; set; }

    /// <summary>
    /// Whether to auto-generate the name (the optional <c>AUTONAME</c> key); null when the key is
    /// absent (older polygons omit it). Presence is not inferable from the value, so it is nullable.
    /// </summary>
    public bool? AutoGenerateName { get; set; }

    /// <summary>
    /// Whether to clip acute corners.
    /// </summary>
    public bool ClipAcuteCorners { get; set; }

    /// <summary>
    /// Whether to draw dead copper.
    /// </summary>
    public bool DrawDeadCopper { get; set; }

    /// <summary>
    /// Whether to draw removed islands.
    /// </summary>
    public bool DrawRemovedIslands { get; set; }

    /// <summary>
    /// Whether to draw removed necks.
    /// </summary>
    public bool DrawRemovedNecks { get; set; }

    /// <summary>
    /// Whether to expand outline.
    /// </summary>
    public bool ExpandOutline { get; set; }

    /// <summary>
    /// Whether to ignore violations.
    /// </summary>
    public bool IgnoreViolations { get; set; }

    /// <summary>
    /// Island area threshold.
    /// </summary>
    public int IslandAreaThreshold { get; set; }

    /// <summary>
    /// Whether to mitre corners.
    /// </summary>
    public bool MitreCorners { get; set; }

    /// <summary>
    /// Neck width threshold.
    /// </summary>
    public Coord NeckWidthThreshold { get; set; }

    /// <summary>
    /// Whether to obey polygon cutout. <see langword="null"/> when the source file omitted the
    /// parameter (older Altium versions did not write it); the writer then omits it too.
    /// </summary>
    public bool? ObeyPolygonCutout { get; set; }

    /// <summary>
    /// Whether to use optimal void rotation. <see langword="null"/> when the source file omitted the
    /// parameter (older Altium versions did not write it); the writer then omits it too.
    /// </summary>
    public bool? OptimalVoidRotation { get; set; }

    /// <summary>
    /// Number of outline points.
    /// </summary>
    public int PointCount { get; set; }

    /// <summary>
    /// Whether to remove dead copper.
    /// </summary>
    public bool RemoveDead { get; set; }

    /// <summary>
    /// Whether to remove islands by area.
    /// </summary>
    public bool RemoveIslandsByArea { get; set; }

    /// <summary>
    /// Whether to remove narrow necks.
    /// </summary>
    public bool RemoveNarrowNecks { get; set; }

    /// <summary>
    /// Whether to use octagons.
    /// </summary>
    public bool UseOctagons { get; set; }

    /// <summary>
    /// Whether this polygon allows global editing.
    /// </summary>
    public bool AllowGlobalEdit { get; set; }

    /// <summary>
    /// Whether this polygon is moveable.
    /// </summary>
    public bool Moveable { get; set; }

    /// <summary>
    /// Paste mask expansion override.
    /// </summary>
    public Coord PasteMaskExpansion { get; set; }

    /// <summary>
    /// Whether this polygon is hidden from view.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether the polygon has been poured (filled with copper).
    /// </summary>
    public bool Poured { get; set; }

    /// <summary>
    /// Pour precedence index (order when multiple polygons overlap).
    /// </summary>
    public int PourIndex { get; set; }

    /// <summary>
    /// Area of the poured polygon in internal coordinate units squared.
    /// </summary>
    public long AreaSize { get; set; }

    /// <summary>
    /// Arc approximation tolerance.
    /// </summary>
    public Coord ArcApproximation { get; set; }

    /// <summary>
    /// Whether to pour over same-net polygons.
    /// </summary>
    public bool PourOverSameNetPolygons { get; set; }

    /// <summary>
    /// Additional parameters not modeled as typed properties.
    /// Preserved for round-trip fidelity.
    /// </summary>
    public Dictionary<string, string>? AdditionalParameters { get; set; }

    /// <summary>
    /// Adds a vertex to the polygon outline.
    /// </summary>
    public void AddVertex(CoordPoint point) => _vertices.Add(point);
}
