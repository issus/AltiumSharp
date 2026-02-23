using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic ellipse primitive defined by a center point and radii.
/// </summary>
public interface ISchEllipse : IPrimitive
{
    CoordPoint Center { get; }
    Coord RadiusX { get; }
    Coord RadiusY { get; }
    int Color { get; }
    int FillColor { get; }
    int LineWidth { get; }
    bool IsFilled { get; }
    bool IsTransparent { get; }
}
