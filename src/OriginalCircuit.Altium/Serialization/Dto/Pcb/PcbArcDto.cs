using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Pcb;

/// <summary>
/// Data Transfer Object for PCB arc records.
/// Represents an arc or circle segment on the PCB.
/// </summary>
[AltiumRecord("Arc")]
internal sealed partial record PcbArcDto
{
    /// <summary>
    /// X coordinate of the arc center.
    /// </summary>
    [AltiumParameter("X")]
    [AltiumCoord]
    public int CenterX { get; init; }

    /// <summary>
    /// Y coordinate of the arc center.
    /// </summary>
    [AltiumParameter("Y")]
    [AltiumCoord]
    public int CenterY { get; init; }

    /// <summary>
    /// Radius of the arc.
    /// </summary>
    [AltiumParameter("RADIUS")]
    [AltiumCoord]
    public int Radius { get; init; }

    /// <summary>
    /// Starting angle of the arc in degrees (0 = right, counter-clockwise).
    /// </summary>
    [AltiumParameter("STARTANGLE")]
    public double StartAngle { get; init; }

    /// <summary>
    /// Ending angle of the arc in degrees (0 = right, counter-clockwise).
    /// </summary>
    [AltiumParameter("ENDANGLE")]
    public double EndAngle { get; init; }

    /// <summary>
    /// Width of the arc stroke.
    /// </summary>
    [AltiumParameter("WIDTH")]
    [AltiumCoord]
    public int Width { get; init; }

    /// <summary>
    /// Layer the arc is on.
    /// </summary>
    [AltiumParameter("LAYER")]
    public int Layer { get; init; }

    /// <summary>
    /// Net name the arc is connected to.
    /// </summary>
    [AltiumParameter("NET")]
    public string? Net { get; init; }

    /// <summary>
    /// Component index if this arc belongs to a component.
    /// </summary>
    [AltiumParameter("COMPONENT")]
    public int ComponentIndex { get; init; }

    /// <summary>
    /// Primitive flags (keepout, locked, etc.).
    /// </summary>
    [AltiumParameter("FLAGS")]
    public int Flags { get; init; }

    /// <summary>
    /// Whether the arc is a keepout region.
    /// </summary>
    [AltiumParameter("KEEPOUT")]
    public bool IsKeepout { get; init; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Whether the arc was user routed vs auto-routed.
    /// </summary>
    [AltiumParameter("USERROUTED")]
    public bool UserRouted { get; init; }

    /// <summary>
    /// Polygon index if this arc is part of a polygon pour.
    /// </summary>
    [AltiumParameter("POLYGONINDEX")]
    public int PolygonIndex { get; init; }

    /// <summary>
    /// Sub-polygon index within a polygon pour.
    /// </summary>
    [AltiumParameter("SUBPOLYINDEX")]
    public int SubPolygonIndex { get; init; }
}
