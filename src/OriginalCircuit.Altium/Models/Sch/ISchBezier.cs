using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic Bezier curve primitive defined by a set of control points.
/// </summary>
public interface ISchBezier : IPrimitive
{
    IReadOnlyList<CoordPoint> ControlPoints { get; }
    int Color { get; }
    int LineWidth { get; }
}
