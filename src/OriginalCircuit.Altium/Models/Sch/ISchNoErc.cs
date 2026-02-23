using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic No-ERC marker that suppresses electrical rule check violations at a location.
/// </summary>
public interface ISchNoErc : IPrimitive
{
    CoordPoint Location { get; }
    int Color { get; }
    int Symbol { get; }
    bool IsActive { get; }
}
