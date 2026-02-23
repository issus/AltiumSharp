using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium;

/// <summary>
/// Converts between Altium schematic line width enum indices and <see cref="Coord"/> values.
/// Altium stores line widths as 0=Small (1 mil), 1=Medium (2 mil), 2=Large (4 mil).
/// </summary>
internal static class AltiumLineWidthHelper
{
    private static readonly Coord[] LineWidths =
    {
        Coord.FromMils(1),  // 0 = Small
        Coord.FromMils(2),  // 1 = Medium
        Coord.FromMils(4)   // 2 = Large
    };

    /// <summary>
    /// Converts a line width enum index to a <see cref="Coord"/> value.
    /// </summary>
    public static Coord IndexToCoord(int index) =>
        index >= 0 && index < LineWidths.Length ? LineWidths[index] : LineWidths[0];

    /// <summary>
    /// Converts a <see cref="Coord"/> value back to the nearest line width enum index.
    /// </summary>
    public static int CoordToIndex(Coord value)
    {
        for (int i = LineWidths.Length - 1; i >= 0; i--)
        {
            if (value >= LineWidths[i])
                return i;
        }
        return 0;
    }
}
