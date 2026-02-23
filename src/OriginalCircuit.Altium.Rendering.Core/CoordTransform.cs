using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Transforms coordinates between Altium world space and screen/pixel space.
/// </summary>
public sealed class CoordTransform
{
    public double Scale { get; set; } = 1.0;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double ScreenWidth { get; set; }
    public double ScreenHeight { get; set; }

    /// <summary>
    /// Screen DPI used for zoom-independent pixel size calculations.
    /// Matches Altium's internal DPI_DXP_UNIT constant (100.0).
    /// </summary>
    public double ScreenDpi { get; set; } = 100.0;

    // Altium internal units per mil = 10000 (Coord.FromMils(1).ToRaw() = 10000)
    private const double UnitsPerMil = 10000.0;

    public (double x, double y) WorldToScreen(Coord worldX, Coord worldY)
    {
        var sx = (worldX.ToRaw() - CenterX) * Scale + ScreenWidth / 2.0;
        var sy = (CenterY - worldY.ToRaw()) * Scale + ScreenHeight / 2.0; // Y inverted
        return (sx, sy);
    }

    public double ScaleValue(Coord value) => value.ToRaw() * Scale;

    /// <summary>
    /// Scales a pixel-space length relative to the current zoom level.
    /// Used for zoom-independent sizes like line widths (Small=1px, Medium=3px, Large=5px).
    /// At 100% zoom the value is returned unchanged; at other zoom levels it is scaled proportionally.
    /// </summary>
    public double ScalePixelLength(double pixelLength) => pixelLength;

    /// <summary>
    /// Maps the Altium schematic line width enum (0=Small, 1=Medium, 2=Large)
    /// to a screen pixel width, scaled by zoom.
    /// </summary>
    public double MapLineWidthEnum(int lineWidthEnum)
    {
        double px = lineWidthEnum switch
        {
            0 => 1.0,
            1 => 3.0,
            2 => 5.0,
            _ => 1.0
        };
        return ScalePixelLength(px);
    }

    public void AutoZoom(CoordRect bounds, double margin = 0.95)
    {
        if (bounds.Width.ToRaw() == 0 && bounds.Height.ToRaw() == 0) return;

        CenterX = (bounds.Min.X.ToRaw() + bounds.Max.X.ToRaw()) / 2.0;
        CenterY = (bounds.Min.Y.ToRaw() + bounds.Max.Y.ToRaw()) / 2.0;

        var scaleX = bounds.Width.ToRaw() > 0 ? ScreenWidth / (double)bounds.Width.ToRaw() : 1.0;
        var scaleY = bounds.Height.ToRaw() > 0 ? ScreenHeight / (double)bounds.Height.ToRaw() : 1.0;
        Scale = Math.Min(scaleX, scaleY) * margin;
    }
}
