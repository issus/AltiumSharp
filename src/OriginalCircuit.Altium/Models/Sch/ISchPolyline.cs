using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic polyline primitive defined by a series of connected line segments.
/// </summary>
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
