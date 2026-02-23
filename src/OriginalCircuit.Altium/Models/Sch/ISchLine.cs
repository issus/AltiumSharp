using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic line primitive drawn between two endpoints.
/// </summary>
public interface ISchLine : IPrimitive
{
    CoordPoint Start { get; set; }
    CoordPoint End { get; set; }
    int Color { get; }
    Coord Width { get; }
    int LineStyle { get; }
}
