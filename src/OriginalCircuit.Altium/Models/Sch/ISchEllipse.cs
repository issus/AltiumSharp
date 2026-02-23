using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic ellipse primitive defined by a center point and radii.
/// This is an Altium-specific primitive with no shared equivalent.
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
