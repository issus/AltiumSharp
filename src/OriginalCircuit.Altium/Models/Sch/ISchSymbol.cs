using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic symbol primitive that serves as the graphical part of a component.
/// This is an Altium-specific primitive with no shared equivalent.
/// </summary>
public interface ISchSymbol : IPrimitive
{
    CoordPoint Location { get; }
}
