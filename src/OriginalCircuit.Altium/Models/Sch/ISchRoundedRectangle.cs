using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic rounded rectangle primitive with configurable corner radii.
/// </summary>
public interface ISchRoundedRectangle : IPrimitive
{
    CoordPoint Corner1 { get; }
    CoordPoint Corner2 { get; }
    int Color { get; }
    int FillColor { get; }
    int LineWidth { get; }
    bool IsFilled { get; }
    bool IsTransparent { get; }
    Coord CornerRadiusX { get; }
    Coord CornerRadiusY { get; }
}
