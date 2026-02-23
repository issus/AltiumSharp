using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic arc primitive defined by a center point, radius, and angular range.
/// </summary>
public interface ISchArc : IPrimitive
{
    CoordPoint Center { get; }
    Coord Radius { get; }
    int Color { get; }
    int LineWidth { get; }
    double StartAngle { get; }
    double EndAngle { get; }
}
