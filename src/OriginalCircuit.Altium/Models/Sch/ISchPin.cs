using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic pin primitive that defines an electrical connection point on a component symbol.
/// </summary>
public interface ISchPin : IPrimitive
{
    string? Name { get; set; }
    string? Designator { get; set; }
    CoordPoint Location { get; set; }
    Coord Length { get; set; }
    int Color { get; }
    PinOrientation Orientation { get; }
    PinElectricalType ElectricalType { get; }
    bool ShowName { get; }
    bool ShowDesignator { get; }
    bool IsHidden { get; }
}
