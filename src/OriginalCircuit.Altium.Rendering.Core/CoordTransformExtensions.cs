using OriginalCircuit.Eda.Rendering;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Altium-specific extension methods for <see cref="CoordTransform"/>.
/// </summary>
public static class CoordTransformExtensions
{
    /// <summary>
    /// Scales a pixel-space length relative to the current zoom level.
    /// Used for zoom-independent sizes like line widths (Small=1px, Medium=3px, Large=5px).
    /// At 100% zoom the value is returned unchanged; at other zoom levels it is scaled proportionally.
    /// </summary>
    public static double ScalePixelLength(this CoordTransform transform, double pixelLength) => pixelLength;

    /// <summary>
    /// Maps the Altium schematic line width enum (0=Small, 1=Medium, 2=Large)
    /// to a screen pixel width, scaled by zoom.
    /// </summary>
    public static double MapLineWidthEnum(this CoordTransform transform, int lineWidthEnum)
    {
        double px = lineWidthEnum switch
        {
            0 => 1.0,
            1 => 3.0,
            2 => 5.0,
            _ => 1.0
        };
        return transform.ScalePixelLength(px);
    }
}
