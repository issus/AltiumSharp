using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic pie (filled arc sector) primitive.
/// This is an Altium-specific primitive with no shared equivalent.
/// </summary>
public interface ISchPie : IPrimitive
{
    CoordPoint Center { get; }
    Coord Radius { get; }
    int Color { get; }
    int FillColor { get; }
    int LineWidth { get; }
    double StartAngle { get; }
    double EndAngle { get; }
    bool IsFilled { get; }
    bool IsTransparent { get; }
}
