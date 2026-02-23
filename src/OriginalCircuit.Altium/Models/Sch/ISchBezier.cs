using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchBezier : IPrimitive
{
    IReadOnlyList<CoordPoint> ControlPoints { get; }
    int Color { get; }
    int LineWidth { get; }
}
