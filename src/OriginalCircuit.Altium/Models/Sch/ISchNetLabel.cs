using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchNetLabel : IPrimitive
{
    CoordPoint Location { get; }
    string Text { get; }
    int Color { get; }
    int FontId { get; }
    int Orientation { get; }
    SchTextJustification Justification { get; }
}
