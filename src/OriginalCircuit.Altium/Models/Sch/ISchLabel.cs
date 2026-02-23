using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic text label primitive used for annotation.
/// </summary>
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
