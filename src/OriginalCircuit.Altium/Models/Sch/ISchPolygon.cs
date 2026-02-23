using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchPolygon : IPrimitive
{
    IReadOnlyList<CoordPoint> Vertices { get; }
    int Color { get; }
    int FillColor { get; }
    int LineWidth { get; }
    bool IsFilled { get; }
    bool IsTransparent { get; }
}
