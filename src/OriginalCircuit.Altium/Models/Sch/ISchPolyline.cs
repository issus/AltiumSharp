using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchPolyline : IPrimitive
{
    IReadOnlyList<CoordPoint> Vertices { get; }
    int Color { get; }
    int LineWidth { get; }
    SchLineStyle LineStyle { get; }
    int StartLineShape { get; }
    int EndLineShape { get; }
    int LineShapeSize { get; }
}
