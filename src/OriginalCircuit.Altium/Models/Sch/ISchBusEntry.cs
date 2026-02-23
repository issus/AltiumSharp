using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic bus entry primitive that connects a wire or net to a bus.
/// </summary>
public interface ISchBusEntry : IPrimitive
{
    CoordPoint Location { get; }
    CoordPoint Corner { get; }
    int Color { get; }
    int LineWidth { get; }
}
