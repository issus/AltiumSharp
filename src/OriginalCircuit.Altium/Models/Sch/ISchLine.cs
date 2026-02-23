using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchLine : IPrimitive
{
    CoordPoint Start { get; set; }
    CoordPoint End { get; set; }
    int Color { get; }
    Coord Width { get; }
    int LineStyle { get; }
}
