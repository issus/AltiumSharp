using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic port primitive that provides an off-sheet or hierarchical connection point.
/// </summary>
public interface ISchPort : IPrimitive
{
    CoordPoint Location { get; }
    string Name { get; }
    int Color { get; }
    int AreaColor { get; }
    int TextColor { get; }
    int FontId { get; }
    int IoType { get; }
    int Style { get; }
    Coord Width { get; }
    Coord Height { get; }
}
