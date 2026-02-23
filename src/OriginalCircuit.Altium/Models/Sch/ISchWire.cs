using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic wire primitive that carries an electrical signal between connection points.
/// </summary>
public interface ISchWire : IPrimitive
{
    IReadOnlyList<CoordPoint> Vertices { get; }
    int Color { get; }
    int LineWidth { get; }
    SchLineStyle LineStyle { get; }
}
