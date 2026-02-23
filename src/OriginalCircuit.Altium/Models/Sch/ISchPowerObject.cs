using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

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
