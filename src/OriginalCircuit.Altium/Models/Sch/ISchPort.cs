using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic port primitive that provides an off-sheet or hierarchical connection point.
/// This is an Altium-specific primitive with no shared equivalent.
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
