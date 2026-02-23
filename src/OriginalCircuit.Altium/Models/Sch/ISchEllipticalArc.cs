using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchEllipticalArc : IPrimitive
{
    CoordPoint Center { get; }
    double StartAngle { get; }
    double EndAngle { get; }
    int Color { get; }
    Coord PrimaryRadius { get; }
    Coord SecondaryRadius { get; }
    Coord LineWidth { get; }
}
