using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchJunction : IPrimitive
{
    CoordPoint Location { get; }
    int Color { get; }
    Coord Size { get; }
}
