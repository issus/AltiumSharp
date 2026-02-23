using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic net label primitive that assigns a net name to a wire or bus.
/// </summary>
public interface ISchNetLabel : IPrimitive
{
    CoordPoint Location { get; }
    string Text { get; }
    int Color { get; }
    int FontId { get; }
    int Orientation { get; }
    SchTextJustification Justification { get; }
}
