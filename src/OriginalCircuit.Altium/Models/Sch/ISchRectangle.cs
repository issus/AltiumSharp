using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchRectangle : IPrimitive
{
    CoordPoint Corner1 { get; set; }
    CoordPoint Corner2 { get; set; }
    int Color { get; }
    int FillColor { get; }
    Coord LineWidth { get; }
    bool IsFilled { get; }
    bool IsTransparent { get; }
}
