using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchNoErc : IPrimitive
{
    CoordPoint Location { get; }
    int Color { get; }
    int Symbol { get; }
    bool IsActive { get; }
}
