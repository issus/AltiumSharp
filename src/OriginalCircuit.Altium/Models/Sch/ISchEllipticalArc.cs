using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic elliptical arc primitive defined by a center, primary and secondary radii, and angular range.
/// This is an Altium-specific primitive with no shared equivalent.
/// </summary>
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
