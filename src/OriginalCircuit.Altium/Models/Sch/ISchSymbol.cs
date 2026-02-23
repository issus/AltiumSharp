using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchSymbol : IPrimitive
{
    CoordPoint Location { get; }
}
