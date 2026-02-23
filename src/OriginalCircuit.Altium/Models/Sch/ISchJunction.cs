using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic junction primitive that marks an electrical connection point between wires.
/// </summary>
public interface ISchJunction : IPrimitive
{
    CoordPoint Location { get; }
    int Color { get; }
    Coord Size { get; }
}
