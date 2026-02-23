using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic power port primitive that connects to a named power or ground net.
/// </summary>
public interface ISchPowerObject : IPrimitive
{
    CoordPoint Location { get; }
    string? Text { get; }
    PowerPortStyle Style { get; }
    double Rotation { get; }
    bool ShowNetName { get; }
    int Color { get; }
    int FontId { get; }
    bool IsMirrored { get; }
}
