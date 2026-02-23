using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Pcb;

/// <summary>
/// Data Transfer Object for PCB via records.
/// Represents a plated hole that connects copper on different layers.
/// </summary>
[AltiumRecord("Via")]
internal sealed partial record PcbViaDto
{
    /// <summary>
    /// X coordinate of the via center.
    /// </summary>
    [AltiumParameter("X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Y coordinate of the via center.
    /// </summary>
    [AltiumParameter("Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// Hole size diameter.
    /// </summary>
    [AltiumParameter("HOLESIZE")]
    [AltiumCoord]
    public int HoleSize { get; init; }

    /// <summary>
    /// Via diameter (copper annular ring size).
    /// </summary>
    [AltiumParameter("SIZE")]
    [AltiumCoord]
    public int Diameter { get; init; }

    /// <summary>
    /// Via diameter on top layer.
    /// </summary>
    [AltiumParameter("DIAMETER_TOPLAYER")]
    [AltiumCoord]
    public int DiameterTop { get; init; }

    /// <summary>
    /// Via diameter on middle layers.
    /// </summary>
    [AltiumParameter("DIAMETER_MIDLAYER")]
    [AltiumCoord]
    public int DiameterMiddle { get; init; }

    /// <summary>
    /// Via diameter on bottom layer.
    /// </summary>
    [AltiumParameter("DIAMETER_BOTTOMLAYER")]
    [AltiumCoord]
    public int DiameterBottom { get; init; }

    /// <summary>
    /// Starting layer number for the via.
    /// </summary>
    [AltiumParameter("STARTLAYER")]
    public int StartLayer { get; init; }

    /// <summary>
    /// Ending layer number for the via.
    /// </summary>
    [AltiumParameter("ENDLAYER")]
    public int EndLayer { get; init; }

    /// <summary>
    /// Layer this via is on (typically MultiLayer).
    /// </summary>
    [AltiumParameter("LAYER")]
    public int Layer { get; init; }

    /// <summary>
    /// Net name the via is connected to.
    /// </summary>
    [AltiumParameter("NET")]
    public string? Net { get; init; }

    /// <summary>
    /// Thermal relief air gap width.
    /// </summary>
    [AltiumParameter("THERMALRELIEFAIRGAPWIDTH")]
    [AltiumCoord]
    public int ThermalReliefAirGapWidth { get; init; }

    /// <summary>
    /// Number of thermal relief conductors (spokes).
    /// </summary>
    [AltiumParameter("THERMALRELIEFCONDUCTORS")]
    public int ThermalReliefConductors { get; init; }

    /// <summary>
    /// Thermal relief conductor width.
    /// </summary>
    [AltiumParameter("THERMALRELIEFCONDUCTORSWIDTH")]
    [AltiumCoord]
    public int ThermalReliefConductorsWidth { get; init; }

    /// <summary>
    /// Whether solder mask expansion is manually specified.
    /// </summary>
    [AltiumParameter("SOLDERMASKEXPANSIONMODE")]
    public int SolderMaskExpansionMode { get; init; }

    /// <summary>
    /// Solder mask expansion value.
    /// </summary>
    [AltiumParameter("SOLDERMASKEXPANSION")]
    [AltiumCoord]
    public int SolderMaskExpansion { get; init; }

    /// <summary>
    /// Diameter stack mode (0=Simple, 1=TopMiddleBottom, 2=FullStack).
    /// </summary>
    [AltiumParameter("DIAMETERSTACK")]
    public int DiameterStackMode { get; init; }

    /// <summary>
    /// Primitive flags (tenting, locked, etc.).
    /// </summary>
    [AltiumParameter("FLAGS")]
    public int Flags { get; init; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Whether this via was user routed vs auto-routed.
    /// </summary>
    [AltiumParameter("USERROUTED")]
    public bool UserRouted { get; init; }

    /// <summary>
    /// Whether the via is a free via (not attached to a net).
    /// </summary>
    [AltiumParameter("FREEVIA")]
    public bool IsFreeVia { get; init; }
}
