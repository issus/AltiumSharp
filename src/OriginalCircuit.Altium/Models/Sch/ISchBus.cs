using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchBus : IPrimitive
{
    IReadOnlyList<CoordPoint> Vertices { get; }
    int Color { get; }
    int LineWidth { get; }
}
