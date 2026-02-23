using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic symbol primitive that serves as the graphical part of a component.
/// </summary>
public interface ISchSymbol : IPrimitive
{
    CoordPoint Location { get; }
}
