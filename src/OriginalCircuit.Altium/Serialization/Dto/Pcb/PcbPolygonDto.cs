using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Pcb;

/// <summary>
/// Data Transfer Object for PCB polygon pour records.
/// Represents a copper polygon pour area with vertices and net assignment.
/// </summary>
[AltiumRecord("Polygon")]
internal sealed partial record PcbPolygonDto
{
    /// <summary>
    /// Name of the polygon pour.
    /// </summary>
    [AltiumParameter("NAME")]
    public string? Name { get; init; }

    /// <summary>
    /// Layer the polygon is on.
    /// </summary>
    [AltiumParameter("LAYER")]
    public int Layer { get; init; }

    /// <summary>
    /// Net name the polygon is connected to.
    /// </summary>
    [AltiumParameter("NET")]
    public string? Net { get; init; }

    /// <summary>
    /// Pour order priority (lower numbers pour first).
    /// </summary>
    [AltiumParameter("POURORDER")]
    public int PourOrder { get; init; }

    /// <summary>
    /// Grid size for the polygon hatching.
    /// </summary>
    [AltiumParameter("GRIDSIZE")]
    [AltiumCoord]
    public int GridSize { get; init; }

    /// <summary>
    /// Track width for hatched fill.
    /// </summary>
    [AltiumParameter("TRACKWIDTH")]
    [AltiumCoord]
    public int TrackWidth { get; init; }

    /// <summary>
    /// Minimum primitive length in polygon.
    /// </summary>
    [AltiumParameter("MINPRIMLENGTH")]
    [AltiumCoord]
    public int MinPrimitiveLength { get; init; }

    /// <summary>
    /// Whether to use octagonal routing.
    /// </summary>
    [AltiumParameter("USEOCTAGONS")]
    public bool UseOctagons { get; init; }

    /// <summary>
    /// Hatch style (0=None/Solid, 1=45Deg, 2=90Deg, 3=Horizontal, 4=Vertical).
    /// </summary>
    [AltiumParameter("HATCHSTYLE")]
    public int HatchStyle { get; init; }

    /// <summary>
    /// Pour mode (0=None, 1=Polygon, 2=Thermal).
    /// </summary>
    [AltiumParameter("POURMODE")]
    public int PourMode { get; init; }

    /// <summary>
    /// Thermal relief style (0=45, 1=90).
    /// </summary>
    [AltiumParameter("REMOVEDEAD")]
    public bool RemoveDeadCopper { get; init; }

    /// <summary>
    /// Whether to remove narrow necks in the pour.
    /// </summary>
    [AltiumParameter("REMOVENECKS")]
    public bool RemoveNarrowNecks { get; init; }

    /// <summary>
    /// Minimum neck width.
    /// </summary>
    [AltiumParameter("NECKWIDTH")]
    [AltiumCoord]
    public int NeckWidth { get; init; }

    /// <summary>
    /// Arc approximation quality.
    /// </summary>
    [AltiumParameter("ARCAPPROXIMATION")]
    [AltiumCoord]
    public int ArcApproximation { get; init; }

    /// <summary>
    /// Whether to pour over same net objects.
    /// </summary>
    [AltiumParameter("POUROVERSAMENETPOLYGONS")]
    public bool PourOverSameNetPolygons { get; init; }

    /// <summary>
    /// Number of vertices in the polygon outline.
    /// </summary>
    [AltiumParameter("NV")]
    public int VertexCount { get; init; }

    /// <summary>
    /// Primitive flags (locked, etc.).
    /// </summary>
    [AltiumParameter("FLAGS")]
    public int Flags { get; init; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Whether the polygon is locked.
    /// </summary>
    [AltiumParameter("LOCKED")]
    public bool IsLocked { get; init; }

    /// <summary>
    /// Whether the polygon is shelved (hidden/not poured).
    /// </summary>
    [AltiumParameter("SHELVED")]
    public bool IsShelved { get; init; }

    /// <summary>
    /// Whether to avoid obstacles.
    /// </summary>
    [AltiumParameter("AVOIDOBST")]
    public bool AvoidObstacles { get; init; }

    /// <summary>
    /// Polygon type for clearance calculations.
    /// </summary>
    [AltiumParameter("POLYGONTYPE")]
    public int PolygonType { get; init; }
}

/// <summary>
/// Data Transfer Object for a polygon vertex.
/// </summary>
[AltiumRecord("PolygonVertex")]
internal sealed partial record PcbPolygonVertexDto
{
    /// <summary>
    /// X coordinate of the vertex.
    /// </summary>
    [AltiumParameter("VX")]
    [AltiumCoord]
    public int X { get; init; }

    /// <summary>
    /// Y coordinate of the vertex.
    /// </summary>
    [AltiumParameter("VY")]
    [AltiumCoord]
    public int Y { get; init; }

    /// <summary>
    /// Vertex kind (0=Linear, 1=Arc).
    /// </summary>
    [AltiumParameter("KIND")]
    public int Kind { get; init; }

    /// <summary>
    /// Arc center X coordinate (if Kind is Arc).
    /// </summary>
    [AltiumParameter("CX")]
    [AltiumCoord]
    public int ArcCenterX { get; init; }

    /// <summary>
    /// Arc center Y coordinate (if Kind is Arc).
    /// </summary>
    [AltiumParameter("CY")]
    [AltiumCoord]
    public int ArcCenterY { get; init; }

    /// <summary>
    /// Arc start angle in degrees.
    /// </summary>
    [AltiumParameter("SA")]
    public double StartAngle { get; init; }

    /// <summary>
    /// Arc end angle in degrees.
    /// </summary>
    [AltiumParameter("EA")]
    public double EndAngle { get; init; }

    /// <summary>
    /// Arc radius.
    /// </summary>
    [AltiumParameter("R")]
    [AltiumCoord]
    public int Radius { get; init; }
}
