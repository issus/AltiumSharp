using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic sheet symbol primitive that references a sub-sheet in a hierarchical design.
/// </summary>
public interface ISchSheetSymbol : IPrimitive
{
    CoordPoint Location { get; }
    int Color { get; }
    int AreaColor { get; }
    Coord XSize { get; }
    Coord YSize { get; }
    bool IsSolid { get; }
    int LineWidth { get; }
}
