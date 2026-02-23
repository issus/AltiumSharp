using OriginalCircuit.Altium.Models;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Renders schematic component primitives to an IRenderContext.
/// Uses interface properties only — no concrete type casts.
/// </summary>
public sealed class SchComponentRenderer
{
    private readonly CoordTransform _transform;
    private ISchComponent? _currentComponent;

    private const double DefaultLineWidth = 1.0;
    private const double DefaultFontSize = 10.0;
    private const double FontScalingAdjust = 0.667;
    private const uint DefaultPinColor = ColorHelper.DarkBlue;
    private const uint DefaultWireColor = ColorHelper.DarkBlue;
    private const uint DefaultJunctionColor = ColorHelper.Navy;
    private const uint DefaultBusColor = ColorHelper.Navy;

    /// <summary>
    /// Font table from the schematic document header. When set, FontId-based
    /// lookups will use the correct font name, size, and style.
    /// </summary>
    public IReadOnlyList<SchFontInfo>? Fonts { get; set; }

    /// <summary>
    /// When set, only primitives belonging to this part ID will be rendered.
    /// Use 0 or null to render all parts.
    /// </summary>
    public int? PartFilter { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SchComponentRenderer"/> with the specified coordinate transform.
    /// </summary>
    /// <param name="transform">The coordinate transform used to map world coordinates to screen coordinates.</param>
    public SchComponentRenderer(CoordTransform transform)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    /// <summary>
    /// Renders all visible primitives of a schematic component to the specified context.
    /// </summary>
    /// <param name="component">The schematic component to render.</param>
    /// <param name="context">The render context to draw into.</param>
    public void Render(ISchComponent component, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(context);

        _currentComponent = component;
        try
        {
            // Back layer: images, filled shapes
            foreach (var image in component.Images)
                if (IsPartVisible(image)) RenderImage(context, image);
            foreach (var polygon in component.Polygons)
                if (IsPartVisible(polygon)) RenderPolygon(context, polygon);
            foreach (var rect in component.Rectangles)
                if (IsPartVisible(rect)) RenderRectangle(context, rect);
            foreach (var roundedRect in component.RoundedRectangles)
                if (IsPartVisible(roundedRect)) RenderRoundedRectangle(context, roundedRect);
            foreach (var ellipse in component.Ellipses)
                if (IsPartVisible(ellipse)) RenderEllipse(context, ellipse);
            foreach (var pie in component.Pies)
                if (IsPartVisible(pie)) RenderPie(context, pie);
            foreach (var textFrame in component.TextFrames)
                if (IsPartVisible(textFrame)) RenderTextFrame(context, textFrame);

            // Lines and curves
            foreach (var line in component.Lines)
                if (IsPartVisible(line)) RenderLine(context, line);
            foreach (var arc in component.Arcs)
                if (IsPartVisible(arc)) RenderArc(context, arc);
            foreach (var ellipticalArc in component.EllipticalArcs)
                if (IsPartVisible(ellipticalArc)) RenderEllipticalArc(context, ellipticalArc);
            foreach (var polyline in component.Polylines)
                if (IsPartVisible(polyline)) RenderPolyline(context, polyline);
            foreach (var bezier in component.Beziers)
                if (IsPartVisible(bezier)) RenderBezier(context, bezier);
            foreach (var wire in component.Wires)
                if (IsPartVisible(wire)) RenderWire(context, wire);

            // Connection points
            foreach (var junction in component.Junctions)
                if (IsPartVisible(junction)) RenderJunction(context, junction);

            // Pins
            foreach (var pin in component.Pins)
                if (IsPartVisible(pin)) RenderPin(context, pin);

            // Text/labels on top
            foreach (var label in component.Labels)
                if (IsPartVisible(label)) RenderLabel(context, label);
            foreach (var parameter in component.Parameters)
                if (IsPartVisible(parameter)) RenderParameter(context, parameter);
            foreach (var netLabel in component.NetLabels)
                if (IsPartVisible(netLabel)) RenderNetLabel(context, netLabel);
            foreach (var powerObj in component.PowerObjects)
                if (IsPartVisible(powerObj)) RenderPowerObject(context, powerObj);
            foreach (var symbol in component.Symbols)
                if (IsPartVisible(symbol)) RenderSymbol(context, symbol);
        }
        finally
        {
            _currentComponent = null;
        }
    }

    // ── Pin ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic pin, including its line, electrical type symbol, name, and designator.
    /// </summary>
    public void RenderPin(IRenderContext context, ISchPin pin)
    {
        if (pin.IsHidden) return;

        var (sx, sy) = _transform.WorldToScreen(pin.Location.X, pin.Location.Y);
        var pinLength = _transform.ScaleValue(pin.Length);
        var color = pin.Color != 0 ? ColorHelper.BgrToArgb(pin.Color) : DefaultPinColor;

        // Pin line goes from Location (connection point) toward the component body
        double ex = sx, ey = sy;
        switch (pin.Orientation)
        {
            case PinOrientation.Right: ex = sx + pinLength; break;
            case PinOrientation.Left:  ex = sx - pinLength; break;
            case PinOrientation.Up:    ey = sy - pinLength; break;
            case PinOrientation.Down:  ey = sy + pinLength; break;
        }

        context.DrawLine(sx, sy, ex, ey, color, DefaultLineWidth);

        // Draw electrical type symbol at connection end
        RenderPinElectricalSymbol(context, pin.ElectricalType, sx, sy, pin.Orientation, color);

        // Pin name (at body end)
        if (pin.ShowName && !string.IsNullOrEmpty(pin.Name))
        {
            var displayText = OverlineHelper.GetDisplayText(pin.Name);
            var font = GetFont(0); // Pin names use default font
            double nameOffset = 2.0;

            double nameX, nameY;
            var options = new TextRenderOptions
            {
                FontFamily = font.FontName,
                Bold = font.Bold,
                Italic = font.Italic,
                VerticalAlignment = TextVAlign.Middle
            };

            switch (pin.Orientation)
            {
                case PinOrientation.Right:
                    nameX = ex + nameOffset;
                    nameY = ey;
                    options = options with { HorizontalAlignment = TextHAlign.Left };
                    break;
                case PinOrientation.Left:
                    nameX = ex - nameOffset;
                    nameY = ey;
                    options = options with { HorizontalAlignment = TextHAlign.Right };
                    break;
                case PinOrientation.Up:
                    nameX = ex;
                    nameY = ey - nameOffset;
                    options = options with { HorizontalAlignment = TextHAlign.Center };
                    break;
                default: // Down
                    nameX = ex;
                    nameY = ey + nameOffset;
                    options = options with { HorizontalAlignment = TextHAlign.Center };
                    break;
            }

            var fontSize = GetFontSize(0);
            context.DrawText(displayText, nameX, nameY, fontSize, color, options);

            // Draw overlines
            var segments = OverlineHelper.Parse(pin.Name);
            if (segments.Any(s => s.HasOverline))
            {
                RenderOverlines(context, segments, nameX, nameY, fontSize, color, options);
            }
        }

        // Pin designator (above/beside the pin line, centered)
        if (pin.ShowDesignator && !string.IsNullOrEmpty(pin.Designator))
        {
            double desigX, desigY;
            double fontSize = GetFontSize(0) * 0.8;
            double desigOffset = 2.0;

            switch (pin.Orientation)
            {
                case PinOrientation.Right:
                case PinOrientation.Left:
                    desigX = (sx + ex) / 2;
                    desigY = sy - desigOffset;
                    context.DrawText(pin.Designator, desigX, desigY, fontSize, color,
                        new TextRenderOptions { HorizontalAlignment = TextHAlign.Center, VerticalAlignment = TextVAlign.Bottom });
                    break;
                default: // Up or Down
                    desigX = sx + desigOffset;
                    desigY = (sy + ey) / 2;
                    context.DrawText(pin.Designator, desigX, desigY, fontSize, color,
                        new TextRenderOptions { HorizontalAlignment = TextHAlign.Left, VerticalAlignment = TextVAlign.Middle });
                    break;
            }
        }
    }

    private void RenderPinElectricalSymbol(IRenderContext context, PinElectricalType type,
        double x, double y, PinOrientation orientation, uint color)
    {
        if (type != PinElectricalType.Input &&
            type != PinElectricalType.Output &&
            type != PinElectricalType.InputOutput)
            return; // V1 only renders arrows for Input/Output/InputOutput

        // V1 arrow dimensions in Coord units: 60 mil wide, 20 mil tall (per wing)
        // Triangle: tip at origin, base at (arrowWidth, ±arrowHeight)
        var arrowW = _transform.ScaleValue(Coord.FromMils(60));
        var arrowH = _transform.ScaleValue(Coord.FromMils(20));
        var arrowGap = _transform.ScaleValue(Coord.FromMils(70));

        // Determine direction multiplier based on orientation and pin direction
        // V1 uses "direction" = 1 normally, -1 when flipped; pin orientation maps to rotation.
        // In V2, orientation directly tells us which way the pin extends.
        // The arrow tip is at the connection point (x, y), pointing toward the component body.
        int dir = orientation switch
        {
            PinOrientation.Right => 1,  // pin goes right, arrow points right (toward body)
            PinOrientation.Left => -1,  // pin goes left, arrow points left
            _ => 1
        };

        bool isVertical = orientation == PinOrientation.Up || orientation == PinOrientation.Down;
        int vdir = orientation switch
        {
            PinOrientation.Up => -1,    // screen Y is inverted
            PinOrientation.Down => 1,
            _ => 1
        };

        // Build the base input arrow triangle (tip at origin, pointing in pin direction)
        double[] axs, ays;

        if (type == PinElectricalType.Input || type == PinElectricalType.InputOutput)
        {
            if (!isVertical)
            {
                axs = new[] { x, x + arrowW * dir, x + arrowW * dir };
                ays = new[] { y, y - arrowH, y + arrowH };
            }
            else
            {
                axs = new[] { x, x - arrowH, x + arrowH };
                ays = new[] { y, y + arrowW * vdir, y + arrowW * vdir };
            }
            context.FillPolygon(axs, ays, 0xFFFFFFFF); // white fill
            context.DrawPolygon(axs, ays, color, 1);
        }

        if (type == PinElectricalType.Output || type == PinElectricalType.InputOutput)
        {
            // Output arrow: rotated 180° from input, offset so it sits at the base of the input arrow
            double offset = (type == PinElectricalType.InputOutput) ? arrowGap : 0;

            if (!isVertical)
            {
                // Output arrow tip points away from the body (opposite direction)
                var baseX = x + offset * dir;
                axs = new[] { baseX, baseX - arrowW * dir, baseX - arrowW * dir };
                ays = new[] { y, y - arrowH, y + arrowH };
            }
            else
            {
                var baseY = y + offset * vdir;
                axs = new[] { x, x - arrowH, x + arrowH };
                ays = new[] { baseY, baseY - arrowW * vdir, baseY - arrowW * vdir };
            }
            context.FillPolygon(axs, ays, 0xFFFFFFFF); // white fill
            context.DrawPolygon(axs, ays, color, 1);
        }
    }

    // ── Line ────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic line primitive with its color, width, and dash style.
    /// </summary>
    public void RenderLine(IRenderContext context, ISchLine line)
    {
        var (x1, y1) = _transform.WorldToScreen(line.Start.X, line.Start.Y);
        var (x2, y2) = _transform.WorldToScreen(line.End.X, line.End.Y);

        var color = GetArgbColor(line.Color);
        var lineWidth = _transform.ScaleValue(line.Width);
        if (lineWidth < DefaultLineWidth) lineWidth = DefaultLineWidth;
        var style = MapSchLineStyle(line.LineStyle);

        context.DrawLine(x1, y1, x2, y2, color, lineWidth, style);
    }

    // ── Rectangle ───────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic rectangle with optional fill and border.
    /// </summary>
    public void RenderRectangle(IRenderContext context, ISchRectangle rect)
    {
        var (x1, y1) = _transform.WorldToScreen(rect.Corner1.X, rect.Corner1.Y);
        var (x2, y2) = _transform.WorldToScreen(rect.Corner2.X, rect.Corner2.Y);

        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        var w = Math.Abs(x2 - x1);
        var h = Math.Abs(y2 - y1);

        var borderColor = GetArgbColor(rect.Color);
        var lineWidth = _transform.ScaleValue(rect.LineWidth);
        if (lineWidth < DefaultLineWidth) lineWidth = DefaultLineWidth;

        if (rect.IsFilled && !rect.IsTransparent)
        {
            var fillColor = ColorHelper.BgrToArgb(rect.FillColor);
            context.FillRectangle(x, y, w, h, fillColor);
        }

        context.DrawRectangle(x, y, w, h, borderColor, lineWidth);
    }

    // ── Arc ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic arc or full circle based on start and end angles.
    /// </summary>
    public void RenderArc(IRenderContext context, ISchArc arc)
    {
        var (cx, cy) = _transform.WorldToScreen(arc.Center.X, arc.Center.Y);
        var r = _transform.ScaleValue(arc.Radius);
        var color = GetArgbColor(arc.Color);
        var lineWidth = _transform.MapLineWidthEnum(arc.LineWidth);

        var sweep = ComputeSweep(arc.StartAngle, arc.EndAngle);

        // Full circle detection
        if (Math.Abs(sweep - 360) < 1e-5)
        {
            context.DrawEllipse(cx, cy, r, r, color, lineWidth);
        }
        else
        {
            context.DrawArc(cx, cy, r, r, -arc.StartAngle, -sweep, color, lineWidth);
        }
    }

    // ── Wire ────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic wire as a polyline through its vertices.
    /// </summary>
    public void RenderWire(IRenderContext context, ISchWire wire)
    {
        if (wire.Vertices.Count < 2) return;

        var color = wire.Color != 0 ? ColorHelper.BgrToArgb(wire.Color) : DefaultWireColor;
        var lineWidth = _transform.MapLineWidthEnum(wire.LineWidth);
        var style = MapSchLineStyleEnum(wire.LineStyle);

        var xs = new double[wire.Vertices.Count];
        var ys = new double[wire.Vertices.Count];
        for (int i = 0; i < wire.Vertices.Count; i++)
            (xs[i], ys[i]) = _transform.WorldToScreen(wire.Vertices[i].X, wire.Vertices[i].Y);

        context.DrawPolyline(xs, ys, color, lineWidth, style);
    }

    // ── Polyline ────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic polyline with optional start and end line shapes (arrows, circles, etc.).
    /// </summary>
    public void RenderPolyline(IRenderContext context, ISchPolyline polyline)
    {
        if (polyline.Vertices.Count < 2) return;

        var color = GetArgbColor(polyline.Color);
        var lineWidth = _transform.MapLineWidthEnum(polyline.LineWidth);
        var style = MapSchLineStyleEnum(polyline.LineStyle);

        var xs = new double[polyline.Vertices.Count];
        var ys = new double[polyline.Vertices.Count];
        for (int i = 0; i < polyline.Vertices.Count; i++)
            (xs[i], ys[i]) = _transform.WorldToScreen(polyline.Vertices[i].X, polyline.Vertices[i].Y);

        context.DrawPolyline(xs, ys, color, lineWidth, style);

        // Line end shapes
        if (polyline.StartLineShape != 0 || polyline.EndLineShape != 0)
        {
            double shapeSize = polyline.LineShapeSize * lineWidth;
            if (shapeSize < 3) shapeSize = 3;

            if (polyline.StartLineShape != 0 && xs.Length >= 2)
            {
                RenderLineEndShape(context, xs[0], ys[0], xs[1], ys[1],
                    polyline.StartLineShape, shapeSize, color);
            }
            if (polyline.EndLineShape != 0 && xs.Length >= 2)
            {
                int last = xs.Length - 1;
                RenderLineEndShape(context, xs[last], ys[last], xs[last - 1], ys[last - 1],
                    polyline.EndLineShape, shapeSize, color);
            }
        }
    }

    private static void RenderLineEndShape(IRenderContext context, double tipX, double tipY,
        double prevX, double prevY, int shape, double size, uint color)
    {
        // Calculate direction from previous point to tip
        var dx = tipX - prevX;
        var dy = tipY - prevY;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 0.001) return;
        dx /= len;
        dy /= len;

        // Perpendicular
        var px = -dy;
        var py = dx;

        switch (shape)
        {
            case 1: // Arrow (open)
                context.DrawLine(tipX, tipY, tipX - dx * size + px * size * 0.5, tipY - dy * size + py * size * 0.5, color, 1);
                context.DrawLine(tipX, tipY, tipX - dx * size - px * size * 0.5, tipY - dy * size - py * size * 0.5, color, 1);
                break;
            case 2: // Solid arrow (filled triangle)
                var axs = new[] { tipX, tipX - dx * size + px * size * 0.5, tipX - dx * size - px * size * 0.5 };
                var ays = new[] { tipY, tipY - dy * size + py * size * 0.5, tipY - dy * size - py * size * 0.5 };
                context.FillPolygon(axs, ays, color);
                break;
            case 3: // Tail
                context.DrawLine(tipX - px * size * 0.5, tipY - py * size * 0.5,
                    tipX + px * size * 0.5, tipY + py * size * 0.5, color, 1);
                break;
            case 4: // Solid tail
                context.DrawLine(tipX - px * size * 0.5, tipY - py * size * 0.5,
                    tipX + px * size * 0.5, tipY + py * size * 0.5, color, 2);
                break;
            case 5: // Circle
                context.DrawEllipse(tipX, tipY, size * 0.4, size * 0.4, color, 1);
                break;
            case 6: // Square
                context.DrawRectangle(tipX - size * 0.3, tipY - size * 0.3, size * 0.6, size * 0.6, color, 1);
                break;
        }
    }

    // ── Polygon ─────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic polygon with optional fill and border.
    /// </summary>
    public void RenderPolygon(IRenderContext context, ISchPolygon polygon)
    {
        if (polygon.Vertices.Count < 3) return;

        var borderColor = GetArgbColor(polygon.Color);
        var lineWidth = _transform.MapLineWidthEnum(polygon.LineWidth);

        var xs = new double[polygon.Vertices.Count];
        var ys = new double[polygon.Vertices.Count];
        for (int i = 0; i < polygon.Vertices.Count; i++)
            (xs[i], ys[i]) = _transform.WorldToScreen(polygon.Vertices[i].X, polygon.Vertices[i].Y);

        if (polygon.IsFilled && !polygon.IsTransparent)
        {
            var fillColor = ColorHelper.BgrToArgb(polygon.FillColor);
            context.FillPolygon(xs, ys, fillColor);
        }

        context.DrawPolygon(xs, ys, borderColor, lineWidth);
    }

    // ── Bezier ──────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic bezier curve from its cubic control points.
    /// </summary>
    public void RenderBezier(IRenderContext context, ISchBezier bezier)
    {
        if (bezier.ControlPoints.Count < 4) return;

        var color = GetArgbColor(bezier.Color);
        var lineWidth = _transform.MapLineWidthEnum(bezier.LineWidth);

        // Draw native bezier curves for each cubic segment
        for (int i = 0; i + 3 < bezier.ControlPoints.Count; i += 3)
        {
            var (p0x, p0y) = _transform.WorldToScreen(bezier.ControlPoints[i].X, bezier.ControlPoints[i].Y);
            var (p1x, p1y) = _transform.WorldToScreen(bezier.ControlPoints[i + 1].X, bezier.ControlPoints[i + 1].Y);
            var (p2x, p2y) = _transform.WorldToScreen(bezier.ControlPoints[i + 2].X, bezier.ControlPoints[i + 2].Y);
            var (p3x, p3y) = _transform.WorldToScreen(bezier.ControlPoints[i + 3].X, bezier.ControlPoints[i + 3].Y);

            context.DrawBezier(p0x, p0y, p1x, p1y, p2x, p2y, p3x, p3y, color, lineWidth);
        }
    }

    // ── Ellipse ─────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic ellipse with optional fill and border.
    /// </summary>
    public void RenderEllipse(IRenderContext context, ISchEllipse ellipse)
    {
        var (cx, cy) = _transform.WorldToScreen(ellipse.Center.X, ellipse.Center.Y);
        var rx = _transform.ScaleValue(ellipse.RadiusX);
        var ry = _transform.ScaleValue(ellipse.RadiusY);
        var borderColor = GetArgbColor(ellipse.Color);
        var lineWidth = _transform.MapLineWidthEnum(ellipse.LineWidth);

        if (ellipse.IsFilled && !ellipse.IsTransparent)
        {
            var fillColor = ColorHelper.BgrToArgb(ellipse.FillColor);
            context.FillEllipse(cx, cy, rx, ry, fillColor);
        }

        context.DrawEllipse(cx, cy, rx, ry, borderColor, lineWidth);
    }

    // ── Rounded Rectangle ───────────────────────────────────────────

    /// <summary>
    /// Renders a schematic rounded rectangle with optional fill, border, and corner radii.
    /// </summary>
    public void RenderRoundedRectangle(IRenderContext context, ISchRoundedRectangle roundedRect)
    {
        var (x1, y1) = _transform.WorldToScreen(roundedRect.Corner1.X, roundedRect.Corner1.Y);
        var (x2, y2) = _transform.WorldToScreen(roundedRect.Corner2.X, roundedRect.Corner2.Y);

        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        var w = Math.Abs(x2 - x1);
        var h = Math.Abs(y2 - y1);

        var borderColor = GetArgbColor(roundedRect.Color);
        var lineWidth = _transform.MapLineWidthEnum(roundedRect.LineWidth);

        var crx = _transform.ScaleValue(roundedRect.CornerRadiusX);
        var cry = _transform.ScaleValue(roundedRect.CornerRadiusY);

        if (roundedRect.IsFilled && !roundedRect.IsTransparent)
        {
            var fillColor = ColorHelper.BgrToArgb(roundedRect.FillColor);
            if (crx > 0 || cry > 0)
                context.FillRoundedRectangle(x, y, w, h, Math.Min(crx, cry), fillColor);
            else
                context.FillRectangle(x, y, w, h, fillColor);
        }

        if (crx > 0 || cry > 0)
            context.DrawRoundedRectangle(x, y, w, h, crx, cry, borderColor, lineWidth);
        else
            context.DrawRectangle(x, y, w, h, borderColor, lineWidth);
    }

    // ── Pie ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic pie (arc sector) with optional fill and border.
    /// </summary>
    public void RenderPie(IRenderContext context, ISchPie pie)
    {
        var (cx, cy) = _transform.WorldToScreen(pie.Center.X, pie.Center.Y);
        var r = _transform.ScaleValue(pie.Radius);
        var borderColor = GetArgbColor(pie.Color);
        var lineWidth = _transform.MapLineWidthEnum(pie.LineWidth);
        var sweep = ComputeSweep(pie.StartAngle, pie.EndAngle);

        // Negate angles for screen Y-axis
        if (pie.IsFilled && !pie.IsTransparent)
        {
            var fillColor = ColorHelper.BgrToArgb(pie.FillColor);
            context.FillPie(cx, cy, r, r, -pie.StartAngle, -sweep, fillColor);
        }

        context.DrawPie(cx, cy, r, r, -pie.StartAngle, -sweep, borderColor, lineWidth);
    }

    // ── Elliptical Arc ──────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic elliptical arc with independent primary and secondary radii.
    /// </summary>
    public void RenderEllipticalArc(IRenderContext context, ISchEllipticalArc arc)
    {
        var (cx, cy) = _transform.WorldToScreen(arc.Center.X, arc.Center.Y);
        var rx = _transform.ScaleValue(arc.PrimaryRadius);
        var ry = _transform.ScaleValue(arc.SecondaryRadius);
        var color = GetArgbColor(arc.Color);
        var lineWidth = Math.Max(_transform.ScaleValue(arc.LineWidth), DefaultLineWidth);

        var sweep = ComputeSweep(arc.StartAngle, arc.EndAngle);

        if (Math.Abs(sweep - 360) < 1e-5)
        {
            context.DrawEllipse(cx, cy, rx, ry, color, lineWidth);
        }
        else
        {
            context.DrawArc(cx, cy, rx, ry, -arc.StartAngle, -sweep, color, lineWidth);
        }
    }

    // ── Text Frame ──────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic text frame with optional fill, border, word wrap, and clipping.
    /// </summary>
    public void RenderTextFrame(IRenderContext context, ISchTextFrame textFrame)
    {
        var (x1, y1) = _transform.WorldToScreen(textFrame.Corner1.X, textFrame.Corner1.Y);
        var (x2, y2) = _transform.WorldToScreen(textFrame.Corner2.X, textFrame.Corner2.Y);

        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        var w = Math.Abs(x2 - x1);
        var h = Math.Abs(y2 - y1);

        if (textFrame.IsFilled)
        {
            var fillColor = ColorHelper.BgrToArgb(textFrame.FillColor);
            context.FillRectangle(x, y, w, h, fillColor);
        }

        if (textFrame.ShowBorder)
        {
            var borderColor = ColorHelper.BgrToArgb(textFrame.BorderColor);
            var lineWidth = _transform.MapLineWidthEnum(textFrame.LineWidth);
            context.DrawRectangle(x, y, w, h, borderColor, lineWidth);
        }

        if (!string.IsNullOrEmpty(textFrame.Text))
        {
            var textColor = ColorHelper.BgrToArgb(textFrame.TextColor);
            var font = GetFont(textFrame.FontId);
            var fontSize = GetFontSize(textFrame.FontId);

            // Clip if requested
            if (textFrame.ClipToRect)
            {
                context.SaveState();
                context.SetClipRect(x, y, w, h);
            }

            var (hAlign, vAlign) = MapJustification(textFrame.Alignment);
            var options = new TextRenderOptions
            {
                FontFamily = font.FontName,
                Bold = font.Bold,
                Italic = font.Italic,
                HorizontalAlignment = hAlign,
                VerticalAlignment = TextVAlign.Top // we position each line ourselves
            };

            const double padding = 2.0;
            var availableWidth = w - padding * 2;

            if (textFrame.WordWrap && availableWidth > 0)
            {
                // Word-wrap: split into lines that fit within the frame width
                var lines = WrapText(context, textFrame.Text, fontSize, options, availableWidth);
                var lineHeight = context.MeasureText("Ag", fontSize, options).Height * 1.2;
                var totalTextHeight = lines.Count * lineHeight;

                double startY = vAlign switch
                {
                    TextVAlign.Middle => y + (h - totalTextHeight) / 2,
                    TextVAlign.Bottom => y + h - totalTextHeight - padding,
                    _ => y + padding // Top
                };

                double lineX = hAlign switch
                {
                    TextHAlign.Center => x + w / 2,
                    TextHAlign.Right => x + w - padding,
                    _ => x + padding
                };

                for (int i = 0; i < lines.Count; i++)
                {
                    context.DrawText(lines[i], lineX, startY + i * lineHeight, fontSize, textColor, options);
                }
            }
            else
            {
                // No word wrap: single text draw
                double textX = hAlign switch
                {
                    TextHAlign.Left => x + padding,
                    TextHAlign.Center => x + w / 2,
                    TextHAlign.Right => x + w - padding,
                    _ => x + padding
                };
                double textY = vAlign switch
                {
                    TextVAlign.Top => y + padding,
                    TextVAlign.Middle => y + h / 2,
                    TextVAlign.Bottom => y + h - padding,
                    _ => y + padding
                };

                context.DrawText(textFrame.Text, textX, textY, fontSize, textColor,
                    new TextRenderOptions
                    {
                        FontFamily = font.FontName,
                        Bold = font.Bold,
                        Italic = font.Italic,
                        HorizontalAlignment = hAlign,
                        VerticalAlignment = vAlign
                    });
            }

            if (textFrame.ClipToRect)
            {
                context.ResetClip();
                context.RestoreState();
            }
        }
    }

    // ── Label ───────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic label with font, color, rotation, and mirroring support.
    /// </summary>
    public void RenderLabel(IRenderContext context, ISchLabel label)
    {
        if (label.IsHidden) return;
        if (string.IsNullOrEmpty(label.Text)) return;

        var (sx, sy) = _transform.WorldToScreen(label.Location.X, label.Location.Y);
        var color = GetArgbColor(label.Color);
        var font = GetFont(label.FontId);
        var fontSize = GetFontSize(label.FontId);
        var (hAlign, vAlign) = MapJustification(label.Justification);

        var options = new TextRenderOptions
        {
            FontFamily = font.FontName,
            Bold = font.Bold,
            Italic = font.Italic,
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign
        };

        var displayText = ResolveStringIndirection(label.Text);
        if (string.IsNullOrEmpty(displayText)) return;

        if (label.Rotation != 0 || label.IsMirrored)
        {
            context.SaveState();
            context.Translate(sx, sy);
            if (label.Rotation != 0) context.Rotate(-label.Rotation);
            if (label.IsMirrored) context.Scale(-1, 1);
            context.DrawText(displayText, 0, 0, fontSize, color, options);
            context.RestoreState();
        }
        else
        {
            context.DrawText(displayText, sx, sy, fontSize, color, options);
        }
    }

    // ── Parameter ───────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic parameter as text, resolving string indirection and applying rotation.
    /// </summary>
    public void RenderParameter(IRenderContext context, ISchParameter parameter)
    {
        if (!parameter.IsVisible) return;

        var (sx, sy) = _transform.WorldToScreen(parameter.Location.X, parameter.Location.Y);
        var color = GetArgbColor(parameter.Color);
        var font = GetFont(parameter.FontId);
        var fontSize = GetFontSize(parameter.FontId);

        var resolvedValue = ResolveStringIndirection(parameter.Value);
        string displayText;
        if (parameter.HideName)
            displayText = resolvedValue;
        else
            displayText = string.IsNullOrEmpty(parameter.Name)
                ? resolvedValue
                : $"{parameter.Name}={resolvedValue}";

        if (string.IsNullOrEmpty(displayText)) return;

        var (hAlign, vAlign) = MapJustification(parameter.Justification);
        var options = new TextRenderOptions
        {
            FontFamily = font.FontName,
            Bold = font.Bold,
            Italic = font.Italic,
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign
        };

        double rotation = parameter.Orientation * 90.0;
        if (rotation != 0 || parameter.IsMirrored)
        {
            context.SaveState();
            context.Translate(sx, sy);
            if (rotation != 0) context.Rotate(-rotation);
            if (parameter.IsMirrored) context.Scale(-1, 1);
            context.DrawText(displayText, 0, 0, fontSize, color, options);
            context.RestoreState();
        }
        else
        {
            context.DrawText(displayText, sx, sy, fontSize, color, options);
        }
    }

    // ── Junction ────────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic junction as a filled circle at the connection point.
    /// </summary>
    public void RenderJunction(IRenderContext context, ISchJunction junction)
    {
        var (sx, sy) = _transform.WorldToScreen(junction.Location.X, junction.Location.Y);
        var color = junction.Color != 0 ? ColorHelper.BgrToArgb(junction.Color) : DefaultJunctionColor;
        var size = _transform.ScaleValue(junction.Size);
        if (size < 2.0) size = 2.0;

        context.FillEllipse(sx, sy, size / 2, size / 2, color);
    }

    // ── Net Label ───────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic net label as text with font, alignment, and rotation.
    /// </summary>
    public void RenderNetLabel(IRenderContext context, ISchNetLabel netLabel)
    {
        if (string.IsNullOrEmpty(netLabel.Text)) return;

        var (sx, sy) = _transform.WorldToScreen(netLabel.Location.X, netLabel.Location.Y);
        var color = GetArgbColor(netLabel.Color);
        var font = GetFont(netLabel.FontId);
        var fontSize = GetFontSize(netLabel.FontId);
        var (hAlign, vAlign) = MapJustification(netLabel.Justification);

        var options = new TextRenderOptions
        {
            FontFamily = font.FontName,
            Bold = font.Bold,
            Italic = font.Italic,
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign
        };

        double rotation = netLabel.Orientation * 90.0;
        if (rotation != 0)
        {
            context.SaveState();
            context.Translate(sx, sy);
            context.Rotate(-rotation);
            context.DrawText(netLabel.Text, 0, 0, fontSize, color, options);
            context.RestoreState();
        }
        else
        {
            context.DrawText(netLabel.Text, sx, sy, fontSize, color, options);
        }
    }

    // ── Power Object ────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic power object with its power port symbol and optional net name text.
    /// </summary>
    public void RenderPowerObject(IRenderContext context, ISchPowerObject powerObject)
    {
        var (sx, sy) = _transform.WorldToScreen(powerObject.Location.X, powerObject.Location.Y);
        var color = powerObject.Color != 0 ? ColorHelper.BgrToArgb(powerObject.Color) : ColorHelper.Black;

        context.SaveState();
        context.Translate(sx, sy);
        if (powerObject.Rotation != 0)
            context.Rotate(-powerObject.Rotation);
        if (powerObject.IsMirrored)
            context.Scale(-1, 1);

        // Pin line from connection point going up (default orientation)
        const double pinLength = 10.0;
        context.DrawLine(0, 0, 0, -pinLength, color, DefaultLineWidth);

        // Draw symbol at the end of the pin
        double symbolY = -pinLength;
        RenderPowerPortSymbol(context, powerObject.Style, symbolY, color);

        context.RestoreState();

        // Draw net name text
        if (powerObject.ShowNetName && !string.IsNullOrEmpty(powerObject.Text))
        {
            var netNameText = ResolveStringIndirection(powerObject.Text);
            var font = GetFont(powerObject.FontId);
            var fontSize = GetFontSize(powerObject.FontId);

            // Position text above/beside the symbol
            context.SaveState();
            context.Translate(sx, sy);
            if (powerObject.Rotation != 0)
                context.Rotate(-powerObject.Rotation);
            if (powerObject.IsMirrored)
                context.Scale(-1, 1);

            double textY = symbolY - 12;
            context.DrawText(netNameText, 0, textY, fontSize, color,
                new TextRenderOptions
                {
                    FontFamily = font.FontName,
                    Bold = font.Bold,
                    Italic = font.Italic,
                    HorizontalAlignment = TextHAlign.Center,
                    VerticalAlignment = TextVAlign.Bottom
                });

            context.RestoreState();
        }
    }

    private static void RenderPowerPortSymbol(IRenderContext context, PowerPortStyle style,
        double y, uint color)
    {
        const double s = 8.0; // Symbol size

        switch (style)
        {
            case PowerPortStyle.Circle:
                context.DrawEllipse(0, y - s / 2, s / 2, s / 2, color, 1);
                break;

            case PowerPortStyle.Arrow:
                context.DrawLine(-s * 0.4, y, 0, y - s, color, 1);
                context.DrawLine(s * 0.4, y, 0, y - s, color, 1);
                break;

            case PowerPortStyle.Bar:
                context.DrawLine(-s, y, s, y, color, 1);
                break;

            case PowerPortStyle.Wave:
                // Approximate sine wave
                for (int i = 0; i < 8; i++)
                {
                    double x1 = -s + i * s / 4.0;
                    double x2 = -s + (i + 1) * s / 4.0;
                    double y1 = y + Math.Sin(i * Math.PI / 2) * s * 0.3;
                    double y2 = y + Math.Sin((i + 1) * Math.PI / 2) * s * 0.3;
                    context.DrawLine(x1, y1, x2, y2, color, 1);
                }
                break;

            case PowerPortStyle.PowerGround:
                // Standard ground: 3 horizontal lines decreasing in width
                context.DrawLine(-s, y, s, y, color, 1);
                context.DrawLine(-s * 0.6, y - s * 0.3, s * 0.6, y - s * 0.3, color, 1);
                context.DrawLine(-s * 0.2, y - s * 0.6, s * 0.2, y - s * 0.6, color, 1);
                break;

            case PowerPortStyle.SignalGround:
                // Triangle ground
                context.DrawLine(-s, y, s, y, color, 1);
                context.DrawLine(-s, y, 0, y - s, color, 1);
                context.DrawLine(s, y, 0, y - s, color, 1);
                break;

            case PowerPortStyle.Earth:
                // Earth ground: horizontal line + 3 diagonal hatches
                context.DrawLine(-s, y, s, y, color, 1);
                context.DrawLine(-s * 0.8, y, -s * 0.4, y - s * 0.5, color, 1);
                context.DrawLine(-s * 0.2, y, s * 0.2, y - s * 0.5, color, 1);
                context.DrawLine(s * 0.4, y, s * 0.8, y - s * 0.5, color, 1);
                break;

            case PowerPortStyle.GostArrow:
                // Arrow pointing up (GOST style)
                context.DrawLine(0, y, 0, y - s, color, 2);
                context.DrawLine(-s * 0.3, y - s * 0.6, 0, y - s, color, 2);
                context.DrawLine(s * 0.3, y - s * 0.6, 0, y - s, color, 2);
                break;

            case PowerPortStyle.GostPowerGround:
                // GOST ground
                context.DrawLine(-s, y, s, y, color, 2);
                context.DrawLine(-s * 0.6, y - s * 0.25, s * 0.6, y - s * 0.25, color, 2);
                context.DrawLine(-s * 0.3, y - s * 0.5, s * 0.3, y - s * 0.5, color, 2);
                break;

            case PowerPortStyle.GostEarth:
                // GOST earth
                context.DrawLine(-s, y, s, y, color, 2);
                for (int i = -2; i <= 2; i++)
                {
                    double xBase = i * s * 0.4;
                    context.DrawLine(xBase, y, xBase - s * 0.2, y - s * 0.4, color, 1);
                }
                break;

            case PowerPortStyle.GostBar:
                // GOST bar - thick horizontal line
                context.DrawLine(-s * 1.2, y, s * 1.2, y, color, 2);
                break;

            default:
                // Fallback: simple bar
                context.DrawLine(-s, y, s, y, color, 1);
                break;
        }
    }

    // ── Image ───────────────────────────────────────────────────────

    private void RenderImage(IRenderContext context, ISchImage image)
    {
        var (x1, y1) = _transform.WorldToScreen(image.Corner1.X, image.Corner1.Y);
        var (x2, y2) = _transform.WorldToScreen(image.Corner2.X, image.Corner2.Y);
        var x = Math.Min(x1, x2);
        var y = Math.Min(y1, y2);
        var w = Math.Abs(x2 - x1);
        var h = Math.Abs(y2 - y1);

        // Try to render actual image data
        if (image.ImageData != null && image.ImageData.Length > 0)
        {
            context.DrawImage(image.ImageData, x, y, w, h);
        }
        else
        {
            // Placeholder: X cross
            const uint frameColor = 0xFF808080;
            context.DrawRectangle(x, y, w, h, frameColor, 1);
            context.DrawLine(x, y, x + w, y + h, frameColor, 1);
            context.DrawLine(x + w, y, x, y + h, frameColor, 1);
        }

        // Border
        if (image.ShowBorder)
        {
            var borderColor = ColorHelper.BgrToArgb(image.BorderColor);
            var lineWidth = _transform.MapLineWidthEnum(image.LineWidth);
            context.DrawRectangle(x, y, w, h, borderColor, lineWidth);
        }
    }

    // ── Symbol ──────────────────────────────────────────────────────

    private void RenderSymbol(IRenderContext context, ISchSymbol symbol)
    {
        // Symbols are complex asset references - stub (V1 also doesn't render)
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static uint GetArgbColor(int bgrColor)
    {
        return bgrColor != 0 ? ColorHelper.BgrToArgb(bgrColor) : ColorHelper.Black;
    }

    private static double ComputeSweep(double startAngle, double endAngle)
    {
        var sweep = endAngle - startAngle;
        if (sweep <= 0) sweep += 360.0;
        return sweep;
    }

    private static LineStyle MapSchLineStyle(int lineStyle)
    {
        return lineStyle switch
        {
            1 => LineStyle.Dashed,
            2 => LineStyle.Dotted,
            3 => LineStyle.DashDot,
            _ => LineStyle.Solid
        };
    }

    private static LineStyle MapSchLineStyleEnum(SchLineStyle lineStyle)
    {
        return lineStyle switch
        {
            SchLineStyle.Dashed => LineStyle.Dashed,
            SchLineStyle.Dotted => LineStyle.Dotted,
            _ => LineStyle.Solid
        };
    }

    private static (TextHAlign h, TextVAlign v) MapJustification(SchTextJustification justification)
    {
        return justification switch
        {
            SchTextJustification.BottomLeft => (TextHAlign.Left, TextVAlign.Bottom),
            SchTextJustification.BottomCenter => (TextHAlign.Center, TextVAlign.Bottom),
            SchTextJustification.BottomRight => (TextHAlign.Right, TextVAlign.Bottom),
            SchTextJustification.MiddleLeft => (TextHAlign.Left, TextVAlign.Middle),
            SchTextJustification.MiddleCenter => (TextHAlign.Center, TextVAlign.Middle),
            SchTextJustification.MiddleRight => (TextHAlign.Right, TextVAlign.Middle),
            SchTextJustification.TopLeft => (TextHAlign.Left, TextVAlign.Top),
            SchTextJustification.TopCenter => (TextHAlign.Center, TextVAlign.Top),
            SchTextJustification.TopRight => (TextHAlign.Right, TextVAlign.Top),
            _ => (TextHAlign.Left, TextVAlign.Bottom)
        };
    }

    private SchFontInfo GetFont(int fontId)
    {
        if (Fonts != null && fontId > 0 && fontId <= Fonts.Count)
            return Fonts[fontId - 1]; // FontId is 1-based
        return new SchFontInfo("Arial", DefaultFontSize, false, false);
    }

    private double GetFontSize(int fontId)
    {
        var font = GetFont(fontId);
        return font.Size * FontScalingAdjust;
    }

    private static List<string> WrapText(IRenderContext context, string text, double fontSize,
        TextRenderOptions options, double maxWidth)
    {
        var lines = new List<string>();
        // First split on explicit newlines
        var paragraphs = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ');
            var currentLine = words[0];

            for (int i = 1; i < words.Length; i++)
            {
                var testLine = currentLine + " " + words[i];
                var metrics = context.MeasureText(testLine, fontSize, options);
                if (metrics.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = words[i];
                }
                else
                {
                    currentLine = testLine;
                }
            }
            lines.Add(currentLine);
        }

        return lines;
    }

    private void RenderOverlines(IRenderContext context, List<OverlineHelper.TextSegment> segments,
        double startX, double startY, double fontSize, uint color, TextRenderOptions options)
    {
        double currentX = startX;
        double overlineY = startY - fontSize; // Above text

        foreach (var segment in segments)
        {
            var metrics = context.MeasureText(segment.Text, fontSize, options);
            if (segment.HasOverline)
            {
                context.DrawLine(currentX, overlineY, currentX + metrics.Width, overlineY, color, 1);
            }
            currentX += metrics.Width;
        }
    }

    /// <summary>
    /// Resolves string indirection: if the text starts with "=" it looks up the
    /// parameter value from the current component's parameter list.
    /// For example, "=Value" resolves to the Value parameter's text.
    /// </summary>
    private string ResolveStringIndirection(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.StartsWith('='))
            return text;

        if (_currentComponent == null)
            return text;

        var parameterName = text.Substring(1);

        // Search component parameters for matching name (case-insensitive)
        foreach (var param in _currentComponent.Parameters)
        {
            if (string.Equals(param.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                return param.Value;
        }

        // If no parameter found, return the parameter name without the "="
        return parameterName;
    }

    /// <summary>
    /// Checks whether a primitive should be rendered based on the current PartFilter.
    /// Uses dynamic property access since OwnerPartId is not on the IPrimitive interface.
    /// </summary>
    private bool IsPartVisible(IPrimitive primitive)
    {
        if (PartFilter is not { } partId || partId <= 0)
            return true;

        // All schematic concrete types have OwnerPartId — use dynamic check
        var ownerPartId = GetOwnerPartId(primitive);
        // OwnerPartId 0 means "all parts" (shared across all parts)
        return ownerPartId == 0 || ownerPartId == partId;
    }

    /// <summary>
    /// Gets the OwnerPartId from a primitive via reflection/pattern matching.
    /// Returns 0 (visible in all parts) if the property is not found.
    /// </summary>
    private static int GetOwnerPartId(IPrimitive primitive)
    {
        // Use a type switch for the concrete schematic types
        // This avoids reflection overhead while covering all known types
        return primitive switch
        {
            SchPin p => p.OwnerPartId,
            SchLine p => p.OwnerPartId,
            SchRectangle p => p.OwnerPartId,
            SchLabel p => p.OwnerPartId,
            SchWire p => p.OwnerPartId,
            SchPolyline p => p.OwnerPartId,
            SchPolygon p => p.OwnerPartId,
            SchArc p => p.OwnerPartId,
            SchBezier p => p.OwnerPartId,
            SchEllipse p => p.OwnerPartId,
            SchRoundedRectangle p => p.OwnerPartId,
            SchPie p => p.OwnerPartId,
            SchNetLabel p => p.OwnerPartId,
            SchJunction p => p.OwnerPartId,
            SchParameter p => p.OwnerPartId,
            SchTextFrame p => p.OwnerPartId,
            SchImage p => p.OwnerPartId,
            SchSymbol p => p.OwnerPartId,
            SchEllipticalArc p => p.OwnerPartId,
            SchPowerObject p => p.OwnerPartId,
            _ => 0 // Unknown type — treat as visible in all parts
        };
    }
}
