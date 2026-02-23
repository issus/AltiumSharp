using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

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
