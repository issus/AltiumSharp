namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Line dash style for rendering.
/// </summary>
public enum LineStyle
{
    Solid,
    Dashed,
    Dotted,
    DashDot
}

/// <summary>
/// Horizontal text alignment.
/// </summary>
public enum TextHAlign
{
    Left,
    Center,
    Right
}

/// <summary>
/// Vertical text alignment.
/// </summary>
public enum TextVAlign
{
    Top,
    Middle,
    Bottom,
    Baseline
}

/// <summary>
/// Options for text rendering including font style and alignment.
/// </summary>
public sealed record TextRenderOptions
{
    /// <summary>Font family name (e.g. "Arial").</summary>
    public string FontFamily { get; init; } = "Arial";
    /// <summary>Whether the font is bold.</summary>
    public bool Bold { get; init; }
    /// <summary>Whether the font is italic.</summary>
    public bool Italic { get; init; }
    /// <summary>Horizontal text alignment relative to the anchor point.</summary>
    public TextHAlign HorizontalAlignment { get; init; } = TextHAlign.Left;
    /// <summary>Vertical text alignment relative to the anchor point.</summary>
    public TextVAlign VerticalAlignment { get; init; } = TextVAlign.Baseline;
}

/// <summary>
/// Result of measuring text dimensions.
/// </summary>
public readonly record struct TextMetrics(double Width, double Height);

/// <summary>
/// Abstract drawing operations for rendering primitives.
/// Implemented by SkiaSharp, SVG, etc. backends.
/// </summary>
public interface IRenderContext
{
    /// <summary>Clears the entire canvas with the specified background color.</summary>
    void Clear(uint argbColor);

    /// <summary>Draws a line between two points.</summary>
    void DrawLine(double x1, double y1, double x2, double y2, uint color, double width,
        LineStyle style = LineStyle.Solid);

    /// <summary>Draws an elliptical arc.</summary>
    void DrawArc(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color, double width);

    /// <summary>Draws a rectangle outline.</summary>
    void DrawRectangle(double x, double y, double width, double height, uint color, double lineWidth);
    /// <summary>Fills a rectangle.</summary>
    void FillRectangle(double x, double y, double width, double height, uint color);

    /// <summary>Draws a rounded rectangle outline with independent corner radii.</summary>
    void DrawRoundedRectangle(double x, double y, double width, double height,
        double cornerRadiusX, double cornerRadiusY, uint color, double lineWidth);

    /// <summary>Fills a rounded rectangle.</summary>
    void FillRoundedRectangle(double x, double y, double width, double height,
        double cornerRadius, uint color);

    /// <summary>Draws an ellipse outline.</summary>
    void DrawEllipse(double cx, double cy, double rx, double ry, uint color, double width);
    /// <summary>Fills an ellipse.</summary>
    void FillEllipse(double cx, double cy, double rx, double ry, uint color);

    /// <summary>Draws a closed polygon outline.</summary>
    void DrawPolygon(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color, double width);
    /// <summary>Fills a closed polygon.</summary>
    void FillPolygon(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color);

    /// <summary>Draws an open polyline through the given points.</summary>
    void DrawPolyline(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color, double width,
        LineStyle style = LineStyle.Solid);

    /// <summary>Draws a cubic bezier curve.</summary>
    void DrawBezier(double x0, double y0, double x1, double y1, double x2, double y2,
        double x3, double y3, uint color, double width);

    /// <summary>Fills a pie (arc sector) shape.</summary>
    void FillPie(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color);

    /// <summary>Draws a pie (arc sector) outline.</summary>
    void DrawPie(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color, double lineWidth);

    /// <summary>Draws text at the specified position using the given font family.</summary>
    void DrawText(string text, double x, double y, double fontSize, uint color,
        string fontFamily = "Arial");

    /// <summary>Draws text at the specified position using detailed text rendering options.</summary>
    void DrawText(string text, double x, double y, double fontSize, uint color,
        TextRenderOptions options);

    /// <summary>Measures the dimensions of the specified text without rendering it.</summary>
    TextMetrics MeasureText(string text, double fontSize, TextRenderOptions? options = null);

    /// <summary>Draws a raster image from raw byte data into the specified rectangle.</summary>
    void DrawImage(ReadOnlySpan<byte> imageData, double x, double y, double width, double height);

    /// <summary>Sets a rectangular clipping region.</summary>
    void SetClipRect(double x, double y, double w, double h);
    /// <summary>Resets the clipping region to the full canvas.</summary>
    void ResetClip();

    /// <summary>Saves the current graphics state (transform, clip) onto a stack.</summary>
    void SaveState();
    /// <summary>Restores the most recently saved graphics state from the stack.</summary>
    void RestoreState();
    /// <summary>Applies a translation to the current transform.</summary>
    void Translate(double dx, double dy);
    /// <summary>Applies a rotation (in degrees) to the current transform.</summary>
    void Rotate(double angleDegrees);
    /// <summary>Applies a scale to the current transform.</summary>
    void Scale(double sx, double sy);
}
