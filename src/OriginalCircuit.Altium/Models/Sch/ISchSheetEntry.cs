using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchSheetEntry : IPrimitive
{
    string Name { get; }
    int Color { get; }
    int AreaColor { get; }
    int TextColor { get; }
    int Side { get; }
    Coord DistanceFromTop { get; }
}
