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
    public string FontFamily { get; init; } = "Arial";
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public TextHAlign HorizontalAlignment { get; init; } = TextHAlign.Left;
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
    void Clear(uint argbColor);

    void DrawLine(double x1, double y1, double x2, double y2, uint color, double width,
        LineStyle style = LineStyle.Solid);

    void DrawArc(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color, double width);

    void DrawRectangle(double x, double y, double width, double height, uint color, double lineWidth);
    void FillRectangle(double x, double y, double width, double height, uint color);

    void DrawRoundedRectangle(double x, double y, double width, double height,
        double cornerRadiusX, double cornerRadiusY, uint color, double lineWidth);

    void FillRoundedRectangle(double x, double y, double width, double height,
        double cornerRadius, uint color);

    void DrawEllipse(double cx, double cy, double rx, double ry, uint color, double width);
    void FillEllipse(double cx, double cy, double rx, double ry, uint color);

    void DrawPolygon(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color, double width);
    void FillPolygon(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color);

    void DrawPolyline(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color, double width,
        LineStyle style = LineStyle.Solid);

    void DrawBezier(double x0, double y0, double x1, double y1, double x2, double y2,
        double x3, double y3, uint color, double width);

    void FillPie(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color);

    void DrawPie(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color, double lineWidth);

    void DrawText(string text, double x, double y, double fontSize, uint color,
        string fontFamily = "Arial");

    void DrawText(string text, double x, double y, double fontSize, uint color,
        TextRenderOptions options);

    TextMetrics MeasureText(string text, double fontSize, TextRenderOptions? options = null);

    void DrawImage(ReadOnlySpan<byte> imageData, double x, double y, double width, double height);

    void SetClipRect(double x, double y, double w, double h);
    void ResetClip();

    void SaveState();
    void RestoreState();
    void Translate(double dx, double dy);
    void Rotate(double angleDegrees);
    void Scale(double sx, double sy);
}
