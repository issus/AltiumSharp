using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchWire : IPrimitive
{
    IReadOnlyList<CoordPoint> Vertices { get; }
    int Color { get; }
    int LineWidth { get; }
    SchLineStyle LineStyle { get; }
}
