using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchLabel : IPrimitive
{
    string Text { get; set; }
    CoordPoint Location { get; set; }
    int Color { get; }
    int FontId { get; }
    SchTextJustification Justification { get; }
    double Rotation { get; }
    bool IsMirrored { get; }
    bool IsHidden { get; }
}
