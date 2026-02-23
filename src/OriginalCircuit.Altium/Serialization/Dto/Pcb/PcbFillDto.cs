using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Pcb;

/// <summary>
/// Data Transfer Object for PCB fill/solid region records.
/// Represents a rectangular filled region on the PCB.
/// </summary>
[AltiumRecord("Fill")]
internal sealed partial record PcbFillDto
{
    /// <summary>
    /// X coordinate of the first corner.
    /// </summary>
    [AltiumParameter("X1")]
    [AltiumCoord]
    public int Corner1X { get; init; }

    /// <summary>
    /// Y coordinate of the first corner.
    /// </summary>
    [AltiumParameter("Y1")]
    [AltiumCoord]
    public int Corner1Y { get; init; }

    /// <summary>
    /// X coordinate of the second corner.
    /// </summary>
    [AltiumParameter("X2")]
    [AltiumCoord]
    public int Corner2X { get; init; }

    /// <summary>
    /// Y coordinate of the second corner.
    /// </summary>
    [AltiumParameter("Y2")]
    [AltiumCoord]
    public int Corner2Y { get; init; }

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    [AltiumParameter("ROTATION")]
    public double Rotation { get; init; }

    /// <summary>
    /// Layer the fill is on.
    /// </summary>
    [AltiumParameter("LAYER")]
    public int Layer { get; init; }

    /// <summary>
    /// Net name the fill is connected to.
    /// </summary>
    [AltiumParameter("NET")]
    public string? Net { get; init; }

    /// <summary>
    /// Component index if this fill belongs to a component.
    /// </summary>
    [AltiumParameter("COMPONENT")]
    public int ComponentIndex { get; init; }

    /// <summary>
    /// Primitive flags (keepout, locked, etc.).
    /// </summary>
    [AltiumParameter("FLAGS")]
    public int Flags { get; init; }

    /// <summary>
    /// Whether the fill is a keepout region.
    /// </summary>
    [AltiumParameter("KEEPOUT")]
    public bool IsKeepout { get; init; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Whether the fill was user routed vs auto-routed.
    /// </summary>
    [AltiumParameter("USERROUTED")]
    public bool UserRouted { get; init; }

    /// <summary>
    /// Polygon index if this fill is part of a polygon pour.
    /// </summary>
    [AltiumParameter("POLYGONINDEX")]
    public int PolygonIndex { get; init; }
}
