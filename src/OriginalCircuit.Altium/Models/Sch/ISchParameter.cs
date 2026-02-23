using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic parameter primitive that stores a name-value pair on a component or sheet.
/// </summary>
public interface ISchParameter : IPrimitive
{
    CoordPoint Location { get; }
    string Name { get; }
    string Value { get; }
    int Color { get; }
    int FontId { get; }
    int Orientation { get; }
    SchTextJustification Justification { get; }
    bool IsVisible { get; }
    bool HideName { get; }
    bool IsMirrored { get; }
}
