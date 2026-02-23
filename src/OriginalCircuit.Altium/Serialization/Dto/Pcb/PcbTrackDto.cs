using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Pcb;

/// <summary>
/// Data Transfer Object for PCB track/trace records.
/// Represents a copper trace segment between two points.
/// </summary>
[AltiumRecord("Track")]
internal sealed partial record PcbTrackDto
{
    /// <summary>
    /// X coordinate of the track start point.
    /// </summary>
    [AltiumParameter("X1")]
    [AltiumCoord]
    public int StartX { get; init; }

    /// <summary>
    /// Y coordinate of the track start point.
    /// </summary>
    [AltiumParameter("Y1")]
    [AltiumCoord]
    public int StartY { get; init; }

    /// <summary>
    /// X coordinate of the track end point.
    /// </summary>
    [AltiumParameter("X2")]
    [AltiumCoord]
    public int EndX { get; init; }

    /// <summary>
    /// Y coordinate of the track end point.
    /// </summary>
    [AltiumParameter("Y2")]
    [AltiumCoord]
    public int EndY { get; init; }

    /// <summary>
    /// Width of the track.
    /// </summary>
    [AltiumParameter("WIDTH")]
    [AltiumCoord]
    public int Width { get; init; }

    /// <summary>
    /// Layer the track is on.
    /// </summary>
    [AltiumParameter("LAYER")]
    public int Layer { get; init; }

    /// <summary>
    /// Net name the track is connected to.
    /// </summary>
    [AltiumParameter("NET")]
    public string? Net { get; init; }

    /// <summary>
    /// Component index if this track belongs to a component.
    /// </summary>
    [AltiumParameter("COMPONENT")]
    public int ComponentIndex { get; init; }

    /// <summary>
    /// Primitive flags (keepout, locked, etc.).
    /// </summary>
    [AltiumParameter("FLAGS")]
    public int Flags { get; init; }

    /// <summary>
    /// Whether the track is a keepout region.
    /// </summary>
    [AltiumParameter("KEEPOUT")]
    public bool IsKeepout { get; init; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Whether the track was user routed vs auto-routed.
    /// </summary>
    [AltiumParameter("USERROUTED")]
    public bool UserRouted { get; init; }

    /// <summary>
    /// Polygon index if this track is part of a polygon pour.
    /// </summary>
    [AltiumParameter("POLYGONINDEX")]
    public int PolygonIndex { get; init; }

    /// <summary>
    /// Sub-polygon index within a polygon pour.
    /// </summary>
    [AltiumParameter("SUBPOLYINDEX")]
    public int SubPolygonIndex { get; init; }
}
