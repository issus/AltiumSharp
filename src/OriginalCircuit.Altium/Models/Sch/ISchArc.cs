using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchArc : IPrimitive
{
    CoordPoint Center { get; }
    Coord Radius { get; }
    int Color { get; }
    int LineWidth { get; }
    double StartAngle { get; }
    double EndAngle { get; }
}
