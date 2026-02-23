using OriginalCircuit.Altium.Rendering;
using SkiaSharp;

namespace OriginalCircuit.Altium.Rendering.Raster;

/// <summary>
/// SkiaSharp implementation of <see cref="IRenderContext"/>.
/// Wraps an <see cref="SKCanvas"/> and translates abstract drawing calls to Skia API calls.
/// </summary>
public sealed class SkiaRenderContext : IRenderContext, IDisposable
{
    private readonly SKCanvas _canvas;
    private readonly Stack<int> _saveStack = new();

    public SkiaRenderContext(SKCanvas canvas)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
    }

    private static SKColor ToColor(uint argb) => new(
        (byte)((argb >> 16) & 0xFF),
        (byte)((argb >> 8) & 0xFF),
        (byte)(argb & 0xFF),
        (byte)((argb >> 24) & 0xFF));

    private static void ApplyLineStyle(SKPaint paint, LineStyle style)
    {
        paint.PathEffect = style switch
        {
            LineStyle.Dashed => SKPathEffect.CreateDash(new[] { 8f, 4f }, 0),
            LineStyle.Dotted => SKPathEffect.CreateDash(new[] { 2f, 4f }, 0),
            LineStyle.DashDot => SKPathEffect.CreateDash(new[] { 8f, 4f, 2f, 4f }, 0),
            _ => null
        };
    }

    private static SKFontStyle ToFontStyle(bool bold, bool italic)
    {
        if (bold && italic) return SKFontStyle.BoldItalic;
        if (bold) return SKFontStyle.Bold;
        if (italic) return SKFontStyle.Italic;
        return SKFontStyle.Normal;
    }

    public void Clear(uint argbColor)
    {
        _canvas.Clear(ToColor(argbColor));
    }

    public void DrawLine(double x1, double y1, double x2, double y2, uint color, double width,
        LineStyle style = LineStyle.Solid)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)width,
            StrokeCap = SKStrokeCap.Round,
        };
        ApplyLineStyle(paint, style);
        _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, paint);
    }

    public void DrawArc(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color, double width)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)width,
            StrokeCap = SKStrokeCap.Round,
        };

        var rect = new SKRect(
            (float)(cx - rx), (float)(cy - ry),
            (float)(cx + rx), (float)(cy + ry));

        using var path = new SKPath();
        path.AddArc(rect, (float)startAngle, (float)sweepAngle);
        _canvas.DrawPath(path, paint);
    }

    public void DrawRectangle(double x, double y, double width, double height, uint color, double lineWidth)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)lineWidth,
        };
        _canvas.DrawRect((float)x, (float)y, (float)width, (float)height, paint);
    }

    public void FillRectangle(double x, double y, double width, double height, uint color)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        _canvas.DrawRect((float)x, (float)y, (float)width, (float)height, paint);
    }

    public void DrawRoundedRectangle(double x, double y, double width, double height,
        double cornerRadiusX, double cornerRadiusY, uint color, double lineWidth)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)lineWidth,
        };
        var rect = new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height));
        var rr = new SKRoundRect(rect, (float)cornerRadiusX, (float)cornerRadiusY);
        _canvas.DrawRoundRect(rr, paint);
    }

    public void FillRoundedRectangle(double x, double y, double width, double height,
        double cornerRadius, uint color)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        var rect = new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height));
        var rr = new SKRoundRect(rect, (float)cornerRadius);
        _canvas.DrawRoundRect(rr, paint);
    }

    public void DrawEllipse(double cx, double cy, double rx, double ry, uint color, double width)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)width,
        };
        _canvas.DrawOval((float)cx, (float)cy, (float)rx, (float)ry, paint);
    }

    public void FillEllipse(double cx, double cy, double rx, double ry, uint color)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        _canvas.DrawOval((float)cx, (float)cy, (float)rx, (float)ry, paint);
    }

    public void DrawPolygon(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color, double width)
    {
        if (xPoints.Length < 2 || xPoints.Length != yPoints.Length) return;

        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)width,
            StrokeJoin = SKStrokeJoin.Round,
        };

        using var path = BuildPolygonPath(xPoints, yPoints);
        _canvas.DrawPath(path, paint);
    }

    public void FillPolygon(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color)
    {
        if (xPoints.Length < 3 || xPoints.Length != yPoints.Length) return;

        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        using var path = BuildPolygonPath(xPoints, yPoints);
        _canvas.DrawPath(path, paint);
    }

    public void DrawPolyline(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints, uint color, double width,
        LineStyle style = LineStyle.Solid)
    {
        if (xPoints.Length < 2 || xPoints.Length != yPoints.Length) return;

        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)width,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };
        ApplyLineStyle(paint, style);

        using var path = new SKPath();
        path.MoveTo((float)xPoints[0], (float)yPoints[0]);
        for (int i = 1; i < xPoints.Length; i++)
            path.LineTo((float)xPoints[i], (float)yPoints[i]);
        // No Close() - open path
        _canvas.DrawPath(path, paint);
    }

    public void DrawBezier(double x0, double y0, double x1, double y1, double x2, double y2,
        double x3, double y3, uint color, double width)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)width,
            StrokeCap = SKStrokeCap.Round,
        };

        using var path = new SKPath();
        path.MoveTo((float)x0, (float)y0);
        path.CubicTo((float)x1, (float)y1, (float)x2, (float)y2, (float)x3, (float)y3);
        _canvas.DrawPath(path, paint);
    }

    public void FillPie(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        var rect = new SKRect(
            (float)(cx - rx), (float)(cy - ry),
            (float)(cx + rx), (float)(cy + ry));

        using var path = new SKPath();
        path.MoveTo((float)cx, (float)cy);
        path.ArcTo(rect, (float)startAngle, (float)sweepAngle, false);
        path.Close();
        _canvas.DrawPath(path, paint);
    }

    public void DrawPie(double cx, double cy, double rx, double ry, double startAngle, double sweepAngle,
        uint color, double lineWidth)
    {
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)lineWidth,
            StrokeJoin = SKStrokeJoin.Round,
        };

        var rect = new SKRect(
            (float)(cx - rx), (float)(cy - ry),
            (float)(cx + rx), (float)(cy + ry));

        using var path = new SKPath();
        path.MoveTo((float)cx, (float)cy);
        path.ArcTo(rect, (float)startAngle, (float)sweepAngle, false);
        path.Close();
        _canvas.DrawPath(path, paint);
    }

    public void DrawText(string text, double x, double y, double fontSize, uint color,
        string fontFamily = "Arial")
    {
        if (string.IsNullOrEmpty(text)) return;

        using var font = new SKFont(SKTypeface.FromFamilyName(fontFamily), (float)fontSize);
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        _canvas.DrawText(text, (float)x, (float)y, font, paint);
    }

    public void DrawText(string text, double x, double y, double fontSize, uint color,
        TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(text)) return;

        var typeface = SKTypeface.FromFamilyName(
            options.FontFamily,
            ToFontStyle(options.Bold, options.Italic));

        using var font = new SKFont(typeface, (float)fontSize);
        using var paint = new SKPaint
        {
            Color = ToColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        // Adjust position for alignment
        float drawX = (float)x;
        float drawY = (float)y;

        if (options.HorizontalAlignment != TextHAlign.Left ||
            options.VerticalAlignment != TextVAlign.Baseline)
        {
            var metrics = font.Metrics;
            var textWidth = font.MeasureText(text, out _);

            drawX = options.HorizontalAlignment switch
            {
                TextHAlign.Center => drawX - textWidth / 2,
                TextHAlign.Right => drawX - textWidth,
                _ => drawX
            };

            drawY = options.VerticalAlignment switch
            {
                TextVAlign.Top => drawY - metrics.Ascent,
                TextVAlign.Middle => drawY - (metrics.Ascent + metrics.Descent) / 2,
                TextVAlign.Bottom => drawY - metrics.Descent,
                _ => drawY // Baseline
            };
        }

        _canvas.DrawText(text, drawX, drawY, font, paint);
    }

    public TextMetrics MeasureText(string text, double fontSize, TextRenderOptions? options = null)
    {
        if (string.IsNullOrEmpty(text)) return new TextMetrics(0, 0);

        var fontFamily = options?.FontFamily ?? "Arial";
        var fontStyle = options != null
            ? ToFontStyle(options.Bold, options.Italic)
            : SKFontStyle.Normal;

        var typeface = SKTypeface.FromFamilyName(fontFamily, fontStyle);
        using var font = new SKFont(typeface, (float)fontSize);

        var width = font.MeasureText(text, out _);
        var metrics = font.Metrics;
        var height = metrics.Descent - metrics.Ascent;

        return new TextMetrics(width, height);
    }

    public void DrawImage(ReadOnlySpan<byte> imageData, double x, double y, double width, double height)
    {
        if (imageData.IsEmpty) return;

        using var skData = SKData.CreateCopy(imageData);
        using var bitmap = SKBitmap.Decode(skData);
        if (bitmap == null) return;

        var dest = new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height));
        _canvas.DrawBitmap(bitmap, dest);
    }

    public void SetClipRect(double x, double y, double w, double h)
    {
        _canvas.ClipRect(new SKRect((float)x, (float)y, (float)(x + w), (float)(y + h)));
    }

    public void ResetClip()
    {
        _canvas.RestoreToCount(_canvas.Save());
    }

    public void SaveState()
    {
        _saveStack.Push(_canvas.Save());
    }

    public void RestoreState()
    {
        if (_saveStack.Count > 0)
        {
            _canvas.RestoreToCount(_saveStack.Pop());
        }
        else
        {
            _canvas.Restore();
        }
    }

    public void Translate(double dx, double dy)
    {
        _canvas.Translate((float)dx, (float)dy);
    }

    public void Rotate(double angleDegrees)
    {
        _canvas.RotateDegrees((float)angleDegrees);
    }

    public void Scale(double sx, double sy)
    {
        _canvas.Scale((float)sx, (float)sy);
    }

    public void Dispose()
    {
        // We do not own the canvas; the caller disposes it.
    }

    private static SKPath BuildPolygonPath(ReadOnlySpan<double> xPoints, ReadOnlySpan<double> yPoints)
    {
        var path = new SKPath();
        path.MoveTo((float)xPoints[0], (float)yPoints[0]);
        for (int i = 1; i < xPoints.Length; i++)
        {
            path.LineTo((float)xPoints[i], (float)yPoints[i]);
        }
        path.Close();
        return path;
    }
}
