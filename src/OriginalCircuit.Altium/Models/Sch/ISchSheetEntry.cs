using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic sheet entry primitive that defines a connection point on a sheet symbol.
/// </summary>
public interface ISchSheetEntry : IPrimitive
{
    string Name { get; }
    int Color { get; }
    int AreaColor { get; }
    int TextColor { get; }
    int Side { get; }
    Coord DistanceFromTop { get; }
}
