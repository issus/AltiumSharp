using OriginalCircuit.Altium.Models;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Enums;
using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Eda.Rendering;
using PinElectricalType = OriginalCircuit.Altium.Models.Sch.PinElectricalType;

namespace OriginalCircuit.Altium.Rendering;

/// <summary>
/// Renders schematic component primitives to an IRenderContext.
/// Uses interface properties only — no concrete type casts.
/// </summary>
public sealed class SchComponentRenderer
{
    private readonly CoordTransform _transform;
    private SchComponent? _currentComponent;

    // Document-level context used to resolve title-block special strings (=Title, =Revision, …).
    private IReadOnlyDictionary<string, string>? _documentParameters;

    // The document's harness colour (from its signal-harness bundles); harness ports use it instead of
    // their stored yellow port colour. 0 when the document has no harnesses.
    private uint _harnessBundleColor;

    /// <summary>
    /// File name of the document being rendered (e.g. <c>DAC.SchDoc</c>). Used to resolve the
    /// <c>=DocumentName</c> title-block special string. Optional.
    /// </summary>
    public string? DocumentFileName { get; set; }

    /// <summary>
    /// Full path of the document being rendered. Used to resolve the
    /// <c>=DocumentFullPathAndName</c> title-block special string. Optional.
    /// </summary>
    public string? DocumentFullPath { get; set; }

    private const double DefaultLineWidth = 1.0;
    private const double DefaultFontSize = 10.0;
    // Schematic font sizes are point sizes. Empirically Altium renders a "size N" schematic font
    // at roughly N*10 mils tall (≈ one 100-mil grid for the default size 10), consistent with
    // altium_monkey's point→pixel factor. This keeps text proportional and readable at any zoom.
    private const double PointToMils = 10.0;
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
    /// Populates <see cref="Fonts"/> from a component's parsed font table so text is drawn with the
    /// correct family, point size and style instead of the Arial-10 fallback.
    /// </summary>
    /// <param name="fonts">Font definitions from the schematic header (1-based FontId order).</param>
    public void SetFonts(IReadOnlyList<SchFontDefinition>? fonts)
    {
        if (fonts == null || fonts.Count == 0)
        {
            Fonts = null;
            return;
        }

        var list = new List<SchFontInfo>(fonts.Count);
        foreach (var f in fonts)
            list.Add(new SchFontInfo(string.IsNullOrWhiteSpace(f.Name) ? "Arial" : f.Name, f.Size, f.Bold, f.Italic));
        Fonts = list;
    }

    /// <summary>
    /// The document's system (default) font as a 1-based index into <see cref="Fonts"/>. Text objects
    /// that carry no FontId (pins, sheet entries) resolve to this rather than the first table entry.
    /// 0 means "use the first entry" (the per-component/library fallback).
    /// </summary>
    public int SystemFontId { get; set; }

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
    public void Render(SchComponent component, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(context);

        _currentComponent = component;
        try
        {
            // Back layer: images, filled shapes
            foreach (var image in component.Images.Cast<SchImage>())
                if (IsPartVisible(image)) RenderImage(context, image);
            foreach (var polygon in component.Polygons.Cast<SchPolygon>())
                if (IsPartVisible(polygon)) RenderPolygon(context, polygon);
            foreach (var rect in component.Rectangles.Cast<SchRectangle>())
                if (IsPartVisible(rect)) RenderRectangle(context, rect);
            foreach (var roundedRect in component.RoundedRectangles.Cast<SchRoundedRectangle>())
                if (IsPartVisible(roundedRect)) RenderRoundedRectangle(context, roundedRect);
            foreach (var ellipse in component.Ellipses.Cast<SchEllipse>())
                if (IsPartVisible(ellipse)) RenderEllipse(context, ellipse);
            foreach (var pie in component.Pies.Cast<SchPie>())
                if (IsPartVisible(pie)) RenderPie(context, pie);
            foreach (var textFrame in component.TextFrames.Cast<SchTextFrame>())
                if (IsPartVisible(textFrame)) RenderTextFrame(context, textFrame);

            // Lines and curves
            foreach (var line in component.Lines.Cast<SchLine>())
                if (IsPartVisible(line)) RenderLine(context, line);
            foreach (var arc in component.Arcs.Cast<SchArc>())
                if (IsPartVisible(arc)) RenderArc(context, arc);
            foreach (var ellipticalArc in component.EllipticalArcs.Cast<SchEllipticalArc>())
                if (IsPartVisible(ellipticalArc)) RenderEllipticalArc(context, ellipticalArc);
            foreach (var polyline in component.Polylines.Cast<SchPolyline>())
                if (IsPartVisible(polyline)) RenderPolyline(context, polyline);
            foreach (var bezier in component.Beziers.Cast<SchBezier>())
                if (IsPartVisible(bezier)) RenderBezier(context, bezier);
            foreach (var wire in component.Wires.Cast<SchWire>())
                if (IsPartVisible(wire)) RenderWire(context, wire);

            // Connection points
            foreach (var junction in component.Junctions.Cast<SchJunction>())
                if (IsPartVisible(junction)) RenderJunction(context, junction);

            // Pins
            foreach (var pin in component.Pins.Cast<SchPin>())
                if (IsPartVisible(pin)) RenderPin(context, pin);

            // Text/labels on top
            foreach (var label in component.Labels.Cast<SchLabel>())
                if (IsPartVisible(label)) RenderLabel(context, label);
            foreach (var parameter in component.Parameters.Cast<SchParameter>())
                if (IsPartVisible(parameter)) RenderParameter(context, parameter);
            foreach (var netLabel in component.NetLabels.Cast<SchNetLabel>())
                if (IsPartVisible(netLabel)) RenderNetLabel(context, netLabel);
            foreach (var powerObj in component.PowerObjects.Cast<SchPowerObject>())
                if (IsPartVisible(powerObj)) RenderPowerObject(context, powerObj);
            foreach (var symbol in component.Symbols.Cast<SchSymbol>())
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
    public void RenderPin(IRenderContext context, SchPin pin)
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

        // Pin name — drawn INSIDE the component body, just past the body end (sx,sy) and opposite
        // the connection end (ex,ey). In Altium only the pin NUMBER sits outside on the stub.
        if (pin.ShowName && !string.IsNullOrEmpty(pin.Name))
        {
            var displayText = FixTextEncoding(OverlineHelper.GetDisplayText(pin.Name));
            var font = GetFont(0); // Pin names use default font
            var fontSize = GetFontSize(0);
            double nameOffset = Math.Max(2.0, fontSize * 0.3);
            var segments = OverlineHelper.Parse(pin.Name);
            bool hasOverline = segments.Any(s => s.HasOverline);

            var options = new TextRenderOptions
            {
                FontFamily = font.FontName,
                Bold = font.Bold,
                Italic = font.Italic,
                VerticalAlignment = TextVAlign.Middle
            };

            switch (pin.Orientation)
            {
                case PinOrientation.Left:
                {
                    // Body is to the right; name starts just inside the left edge.
                    var opt = options with { HorizontalAlignment = TextHAlign.Left };
                    var nx = sx + nameOffset;
                    context.DrawText(displayText, nx, sy, fontSize, color, opt);
                    if (hasOverline) RenderOverlines(context, segments, nx, sy, fontSize, color, opt);
                    break;
                }
                case PinOrientation.Up:
                {
                    // Vertical pin; body is below — rotate the name and run it into the body.
                    context.SaveState();
                    context.Translate(sx, sy + nameOffset);
                    context.Rotate(90);
                    context.DrawText(displayText, 0, 0, fontSize, color,
                        options with { HorizontalAlignment = TextHAlign.Left });
                    context.RestoreState();
                    break;
                }
                case PinOrientation.Down:
                {
                    // Vertical pin; body is above — rotate the name and run it into the body.
                    context.SaveState();
                    context.Translate(sx, sy - nameOffset);
                    context.Rotate(-90);
                    context.DrawText(displayText, 0, 0, fontSize, color,
                        options with { HorizontalAlignment = TextHAlign.Left });
                    context.RestoreState();
                    break;
                }
                default: // Right
                {
                    // Body is to the left; name ends just inside the right edge (right-aligned).
                    var opt = options with { HorizontalAlignment = TextHAlign.Right };
                    var nx = sx - nameOffset;
                    context.DrawText(displayText, nx, sy, fontSize, color, opt);
                    if (hasOverline)
                    {
                        var w = context.MeasureText(displayText, fontSize, opt).Width;
                        RenderOverlines(context, segments, nx - w, sy, fontSize, color, opt);
                    }
                    break;
                }
            }
        }

        // Pin designator (the pin NUMBER) sits on the stub just above/beside the pin line, centered
        // along it. It's drawn smaller than the pin name and close to its own line, so on tight pitch
        // (e.g. 100-mil) it stays within the gap instead of riding up into the pin above.
        if (pin.ShowDesignator && !string.IsNullOrEmpty(pin.Designator))
        {
            double desigX, desigY;
            double fontSize = GetFontSize(0) * 0.66;
            double desigOffset = Math.Max(1.5, fontSize * 0.18);

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
    public void RenderLine(IRenderContext context, SchLine line)
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
    public void RenderRectangle(IRenderContext context, SchRectangle rect)
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
    public void RenderArc(IRenderContext context, SchArc arc)
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
    public void RenderWire(IRenderContext context, SchWire wire)
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
    public void RenderPolyline(IRenderContext context, SchPolyline polyline)
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
    public void RenderPolygon(IRenderContext context, SchPolygon polygon)
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
    public void RenderBezier(IRenderContext context, SchBezier bezier)
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
    public void RenderEllipse(IRenderContext context, SchEllipse ellipse)
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
    public void RenderRoundedRectangle(IRenderContext context, SchRoundedRectangle roundedRect)
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
    public void RenderPie(IRenderContext context, SchPie pie)
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
    public void RenderEllipticalArc(IRenderContext context, SchEllipticalArc arc)
    {
        var (cx, cy) = _transform.WorldToScreen(arc.Center.X, arc.Center.Y);
        var rx = _transform.ScaleValue(arc.PrimaryRadius);
        var ry = _transform.ScaleValue(arc.SecondaryRadius);
        var color = GetArgbColor(arc.Color);
        var lineWidth = Math.Max(_transform.ScaleValue(arc.LineWidth), DefaultLineWidth);

        var sweep = ComputeSweep(arc.StartAngle, arc.EndAngle);

        if (rx <= 0 || ry <= 0) return;

        if (Math.Abs(sweep - 360) < 1e-5)
        {
            context.DrawEllipse(cx, cy, rx, ry, color, lineWidth);
            return;
        }

        // Altium interprets the start/end angles as true geometric angles (polar form),
        // not the standard parametric ellipse angle. For rx != ry these differ, so sample
        // the arc with the polar-form ellipse to place the endpoints where Altium does.
        int steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweep) / 4.0)); // ~4 degrees per segment
        var xs = new double[steps + 1];
        var ys = new double[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            double ang = arc.StartAngle + sweep * i / steps;
            (xs[i], ys[i]) = EllipsePolarPoint(cx, cy, rx, ry, ang);
        }
        context.DrawPolyline(xs, ys, color, lineWidth);
    }

    /// <summary>
    /// Computes a point on Altium's polar-form ellipse: r(θ) = sqrt(1/((cosθ/rx)² + (sinθ/ry)²)),
    /// point = (cx + r·cosθ, cy − r·sinθ). The angle is the true geometric direction in degrees
    /// (world CCW); screen Y is inverted so the sine term is negated.
    /// </summary>
    private static (double x, double y) EllipsePolarPoint(double cx, double cy, double rx, double ry, double angleDeg)
    {
        double a = angleDeg * Math.PI / 180.0;
        double ca = Math.Cos(a);
        double sa = Math.Sin(a);
        double denom = (ca / rx) * (ca / rx) + (sa / ry) * (sa / ry);
        double r = denom > 1e-12 ? Math.Sqrt(1.0 / denom) : 0.0;
        return (cx + r * ca, cy - r * sa);
    }

    // ── Text Frame ──────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic text frame with optional fill, border, word wrap, and clipping.
    /// </summary>
    public void RenderTextFrame(IRenderContext context, SchTextFrame textFrame)
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
            var frameText = FixTextEncoding(textFrame.Text);
            var textColor = ColorHelper.BgrToArgb(textFrame.TextColor);
            var font = GetFont(textFrame.FontId);
            var fontSize = GetFontSize(textFrame.FontId);

            // Clip if requested
            if (textFrame.ClipToRect)
            {
                context.SaveState();
                context.SetClipRect(x, y, w, h);
            }

            // Text-frame alignment is a 3-value horizontal alignment (Center/Left/Right); the renderer
            // positions each line vertically itself, so only the horizontal component is used.
            var hAlign = textFrame.Alignment switch
            {
                SchTextFrameAlignment.Left => TextHAlign.Left,
                SchTextFrameAlignment.Right => TextHAlign.Right,
                _ => TextHAlign.Center
            };
            // Text-frame text flows from the top of the frame (Altium has no vertical justification here).
            var vAlign = TextVAlign.Top;
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
                var lines = WrapText(context, frameText, fontSize, options, availableWidth);
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

                context.DrawText(frameText, textX, textY, fontSize, textColor,
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
    public void RenderLabel(IRenderContext context, SchLabel label)
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

        // Altium never mirrors text glyphs: a mirrored component keeps its labels readable, and the
        // stored justification/location already account for the flip. Only rotation is applied here —
        // mirroring the glyphs (Scale(-1,1)) would render the text backwards, which Altium never does.
        if (label.Rotation != 0)
        {
            context.SaveState();
            context.Translate(sx, sy);
            context.Rotate(-label.Rotation);
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
    public void RenderParameter(IRenderContext context, SchParameter parameter)
    {
        if (!parameter.IsVisible) return;

        var (sx, sy) = _transform.WorldToScreen(parameter.Location.X, parameter.Location.Y);
        var color = GetArgbColor(parameter.Color);
        var font = GetFont(parameter.FontId);
        var fontSize = GetFontSize(parameter.FontId);

        // Altium's default visible-parameter display mode is value-only ("22µF", not "Value=22µF").
        // The "Name=Value" form is opt-in and rare, and our HideName flag is unreliable (inverted-key
        // serialization issue), so render the value to match Altium's typical appearance.
        var displayText = ResolveStringIndirection(parameter.Value);
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

        // As with labels, Altium keeps parameter text (designator, comment, value) readable on a
        // mirrored component — the stored justification already reflects the flip. Apply rotation only.
        double rotation = parameter.Orientation * 90.0;
        if (rotation != 0)
        {
            context.SaveState();
            context.Translate(sx, sy);
            context.Rotate(-rotation);
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
    public void RenderJunction(IRenderContext context, SchJunction junction)
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
    public void RenderNetLabel(IRenderContext context, SchNetLabel netLabel)
    {
        if (string.IsNullOrEmpty(netLabel.Text)) return;
        var netText = FixTextEncoding(netLabel.Text);

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
            context.DrawText(netText, 0, 0, fontSize, color, options);
            context.RestoreState();
        }
        else
        {
            context.DrawText(netText, sx, sy, fontSize, color, options);
        }
    }

    // ── Power Object ────────────────────────────────────────────────

    /// <summary>
    /// Renders a schematic power object with its power port symbol and optional net name text.
    /// </summary>
    public void RenderPowerObject(IRenderContext context, SchPowerObject powerObject)
    {
        var (sx, sy) = _transform.WorldToScreen(powerObject.Location.X, powerObject.Location.Y);
        var color = powerObject.Color != 0 ? ColorHelper.BgrToArgb(powerObject.Color) : ColorHelper.Black;

        // World-scaled sizes so the port tracks the drawing instead of being fixed pixels.
        var symbolSize = _transform.ScaleValue(Coord.FromMils(50));
        if (symbolSize < 4) symbolSize = 4;
        var symbolLineWidth = Math.Max(1.0, _transform.ScaleValue(Coord.FromMils(6)));
        double pinLength = symbolSize * 1.2;

        // Our symbol is drawn pointing up (north). Altium's Rotation is CCW-from-east (90°=up,
        // 270°=down), so rotate the base by (90 − Rotation) to face the right way. (Was Rotate(−Rotation),
        // which put GND/5V on their sides.)
        double symbolRotation = 90 - powerObject.Rotation;

        context.SaveState();
        context.Translate(sx, sy);
        if (symbolRotation != 0)
            context.Rotate(symbolRotation);
        if (powerObject.IsMirrored)
            context.Scale(-1, 1);

        // Pin line + symbol, drawn in the local "up" base orientation.
        context.DrawLine(0, 0, 0, -pinLength, color, symbolLineWidth);
        RenderPowerPortSymbol(context, powerObject.Style, -pinLength, color, symbolSize, symbolLineWidth);

        context.RestoreState();

        // Net name stays UPRIGHT (Altium doesn't rotate power-port text); it's placed beyond the
        // symbol in the direction the port points.
        if (powerObject.ShowNetName && !string.IsNullOrEmpty(powerObject.Text))
        {
            var netNameText = ResolveStringIndirection(powerObject.Text);
            var font = GetFont(powerObject.FontId);
            var fontSize = GetFontSize(powerObject.FontId);

            double rad = powerObject.Rotation * Math.PI / 180.0;
            double dirX = Math.Cos(rad), dirY = -Math.Sin(rad); // screen Y is inverted
            // Place the text just beyond the symbol's far end, anchored at its NEAR edge so it never
            // overlaps the port symbol — e.g. GND text sits BELOW the ground bars, not over them.
            double dist = pinLength + symbolSize * 1.25;
            double tx = sx + dist * dirX;
            double ty = sy + dist * dirY;

            TextHAlign hAlign;
            TextVAlign vAlign;
            if (Math.Abs(dirY) >= Math.Abs(dirX))
            {
                hAlign = TextHAlign.Center;
                vAlign = dirY > 0 ? TextVAlign.Top : TextVAlign.Bottom; // down → text below; up → above
            }
            else
            {
                vAlign = TextVAlign.Middle;
                hAlign = dirX > 0 ? TextHAlign.Left : TextHAlign.Right;
            }

            context.DrawText(netNameText, tx, ty, fontSize, color,
                new TextRenderOptions
                {
                    FontFamily = font.FontName,
                    Bold = font.Bold,
                    Italic = font.Italic,
                    HorizontalAlignment = hAlign,
                    VerticalAlignment = vAlign
                });
        }
    }

    private static void RenderPowerPortSymbol(IRenderContext context, PowerPortStyle style,
        double y, uint color, double s, double w)
    {
        double w2 = w * 1.5; // emphasis width for GOST styles

        switch (style)
        {
            case PowerPortStyle.Circle:
                context.DrawEllipse(0, y - s / 2, s / 2, s / 2, color, w);
                break;

            case PowerPortStyle.Arrow:
                context.DrawLine(-s * 0.4, y, 0, y - s, color, w);
                context.DrawLine(s * 0.4, y, 0, y - s, color, w);
                break;

            case PowerPortStyle.Bar:
                context.DrawLine(-s, y, s, y, color, w);
                break;

            case PowerPortStyle.Wave:
                // Approximate sine wave
                for (int i = 0; i < 8; i++)
                {
                    double x1 = -s + i * s / 4.0;
                    double x2 = -s + (i + 1) * s / 4.0;
                    double y1 = y + Math.Sin(i * Math.PI / 2) * s * 0.3;
                    double y2 = y + Math.Sin((i + 1) * Math.PI / 2) * s * 0.3;
                    context.DrawLine(x1, y1, x2, y2, color, w);
                }
                break;

            case PowerPortStyle.PowerGround:
                // Standard ground: 3 horizontal lines decreasing in width
                context.DrawLine(-s, y, s, y, color, w);
                context.DrawLine(-s * 0.6, y - s * 0.3, s * 0.6, y - s * 0.3, color, w);
                context.DrawLine(-s * 0.2, y - s * 0.6, s * 0.2, y - s * 0.6, color, w);
                break;

            case PowerPortStyle.SignalGround:
                // Triangle ground
                context.DrawLine(-s, y, s, y, color, w);
                context.DrawLine(-s, y, 0, y - s, color, w);
                context.DrawLine(s, y, 0, y - s, color, w);
                break;

            case PowerPortStyle.Earth:
                // Earth ground: horizontal line + 3 diagonal hatches
                context.DrawLine(-s, y, s, y, color, w);
                context.DrawLine(-s * 0.8, y, -s * 0.4, y - s * 0.5, color, w);
                context.DrawLine(-s * 0.2, y, s * 0.2, y - s * 0.5, color, w);
                context.DrawLine(s * 0.4, y, s * 0.8, y - s * 0.5, color, w);
                break;

            case PowerPortStyle.GostArrow:
                // Arrow pointing up (GOST style)
                context.DrawLine(0, y, 0, y - s, color, w2);
                context.DrawLine(-s * 0.3, y - s * 0.6, 0, y - s, color, w2);
                context.DrawLine(s * 0.3, y - s * 0.6, 0, y - s, color, w2);
                break;

            case PowerPortStyle.GostPowerGround:
                // GOST ground
                context.DrawLine(-s, y, s, y, color, w2);
                context.DrawLine(-s * 0.6, y - s * 0.25, s * 0.6, y - s * 0.25, color, w2);
                context.DrawLine(-s * 0.3, y - s * 0.5, s * 0.3, y - s * 0.5, color, w2);
                break;

            case PowerPortStyle.GostEarth:
                // GOST earth
                context.DrawLine(-s, y, s, y, color, w2);
                for (int i = -2; i <= 2; i++)
                {
                    double xBase = i * s * 0.4;
                    context.DrawLine(xBase, y, xBase - s * 0.2, y - s * 0.4, color, w);
                }
                break;

            case PowerPortStyle.GostBar:
                // GOST bar - thick horizontal line
                context.DrawLine(-s * 1.2, y, s * 1.2, y, color, w2);
                break;

            default:
                // Fallback: simple bar
                context.DrawLine(-s, y, s, y, color, w);
                break;
        }
    }

    // ── Image ───────────────────────────────────────────────────────

    private void RenderImage(IRenderContext context, SchImage image)
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

    private void RenderSymbol(IRenderContext context, SchSymbol symbol)
    {
        // Symbols are complex asset references - stub (V1 also doesn't render)
    }

    // ── Document (sheet) rendering ──────────────────────────────────

    /// <summary>
    /// Renders a whole schematic document (sheet): its top-level primitives plus every placed
    /// component instance, in Altium's back-to-front order.
    /// </summary>
    public void Render(SchDocument document, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        // Capture document parameters (Name→Value) so labels like "=Title" / "=Revision"
        // embedded from the sheet template resolve to their real values.
        _documentParameters = BuildDocumentParameters(document);
        DocumentFileName ??= document.FileName;
        DocumentFullPath ??= document.FilePath;
        // Objects without their own FontId (pins, sheet entries) use the sheet's system font.
        SystemFontId = document.SystemFont;
        // Harness ports take the harness colour (from the bundles), not their stored yellow port colour.
        var bundle = document.SignalHarnesses.FirstOrDefault(s => s.Color != 0);
        _harnessBundleColor = bundle != null ? ColorHelper.BgrToArgb(bundle.Color) : 0;

        // Sheet page: border frame + title-block grid, drawn behind all content.
        RenderSheetFrame(context, document);

        // The applied sheet template's own graphics (border, title-block grid, field labels, logo) are
        // owned by the template record — not the document's content collections — so render them here,
        // behind the schematic, rather than as ordinary top-level primitives.
        RenderTemplateGraphics(context, document);

        // Back: sheet symbols and filled shapes
        foreach (var sheet in document.SheetSymbols) RenderSheetSymbol(context, sheet);
        foreach (var rect in document.Rectangles) RenderRectangle(context, rect);
        foreach (var polygon in document.Polygons) RenderPolygon(context, polygon);
        foreach (var ellipse in document.Ellipses) RenderEllipse(context, ellipse);
        foreach (var roundedRect in document.RoundedRectangles) RenderRoundedRectangle(context, roundedRect);
        foreach (var pie in document.Pies) RenderPie(context, pie);
        foreach (var textFrame in document.TextFrames) RenderTextFrame(context, textFrame);
        foreach (var image in document.Images) RenderImage(context, image);

        // Lines and curves
        foreach (var line in document.Lines) RenderLine(context, line);
        foreach (var arc in document.Arcs) RenderArc(context, arc);
        foreach (var ellipticalArc in document.EllipticalArcs) RenderEllipticalArc(context, ellipticalArc);
        foreach (var polyline in document.Polylines) RenderPolyline(context, polyline);
        foreach (var bezier in document.Beziers) RenderBezier(context, bezier);

        // Buses, bus entries, signal-harness bundles, wires
        foreach (var bus in document.Buses) RenderBus(context, bus);
        foreach (var busEntry in document.BusEntries) RenderBusEntry(context, busEntry);
        foreach (var signalHarness in document.SignalHarnesses) RenderSignalHarness(context, signalHarness);
        foreach (var wire in document.Wires.Cast<SchWire>()) RenderWire(context, wire);

        // Harness connectors (box + bundle entries + type label)
        foreach (var connector in document.HarnessConnectors) RenderHarnessConnector(context, connector);

        // Component instances (each manages its own _currentComponent scope)
        foreach (var component in document.Components.Cast<SchComponent>()) Render(component, context);

        // Connection points
        foreach (var junction in document.Junctions.Cast<SchJunction>()) RenderJunction(context, junction);
        foreach (var noErc in document.NoErcs.Cast<SchNoErc>()) RenderNoErc(context, noErc);

        // Text, labels, ports on top
        foreach (var port in document.Ports) RenderPort(context, port);
        foreach (var label in document.Labels.Cast<SchLabel>()) RenderLabel(context, label);
        foreach (var netLabel in document.NetLabels.Cast<SchNetLabel>()) RenderNetLabel(context, netLabel);
        foreach (var parameter in document.Parameters.Cast<SchParameter>()) RenderParameter(context, parameter);
        foreach (var powerObject in document.PowerObjects.Cast<SchPowerObject>()) RenderPowerObject(context, powerObject);
    }

    // ── Sheet frame (border + title block) ──────────────────────────

    /// <summary>
    /// Renders the graphics embedded in the applied sheet template(s): the frame lines, title-block grid,
    /// caption/field labels and logo the <c>.SchDot</c> carries as primitives owned by the template record
    /// (type 39). They are held on <see cref="SchTemplate.OwnedPrimitives"/> — kept out of the document's
    /// own content collections so they aren't treated as schematic content — and are drawn here behind the
    /// content, using the same primitive renderers (so <c>=Title</c>-style field labels still resolve).
    /// </summary>
    private void RenderTemplateGraphics(IRenderContext context, SchDocument document)
    {
        foreach (var template in document.Templates)
        {
            foreach (var primitive in template.OwnedPrimitives)
            {
                switch (primitive)
                {
                    case SchLine line: RenderLine(context, line); break;
                    case SchPolyline polyline: RenderPolyline(context, polyline); break;
                    case SchPolygon polygon: RenderPolygon(context, polygon); break;
                    case SchRectangle rect: RenderRectangle(context, rect); break;
                    case SchRoundedRectangle roundedRect: RenderRoundedRectangle(context, roundedRect); break;
                    case SchArc arc: RenderArc(context, arc); break;
                    case SchEllipticalArc ellipticalArc: RenderEllipticalArc(context, ellipticalArc); break;
                    case SchEllipse ellipse: RenderEllipse(context, ellipse); break;
                    case SchPie pie: RenderPie(context, pie); break;
                    case SchBezier bezier: RenderBezier(context, bezier); break;
                    case SchImage image: RenderImage(context, image); break;
                    case SchTextFrame textFrame: RenderTextFrame(context, textFrame); break;
                    case SchLabel label: RenderLabel(context, label); break;
                    case SchParameter parameter: RenderParameter(context, parameter); break;
                }
            }
        }
    }

    /// <summary>Builds a case-insensitive Name→Value map from the document's top-level parameters.</summary>
    private static Dictionary<string, string> BuildDocumentParameters(SchDocument document)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in document.Parameters)
        {
            if (p is SchParameter sp && !string.IsNullOrEmpty(sp.Name) && !map.ContainsKey(sp.Name))
                map[sp.Name] = sp.Value ?? string.Empty;
        }
        return map;
    }

    // Width of the reference-zone border band (mils). The inner frame is inset by this, and the
    // title block's bottom-right corner sits on the inner frame corner.
    private const double BorderBand = 150.0;

    /// <summary>Draws the sheet border, reference zones and title-block grid behind the content.</summary>
    private void RenderSheetFrame(IRenderContext context, SchDocument document)
    {
        var sheet = document.SheetInfo;
        double sheetW = sheet.Width.ToMils();
        double sheetH = sheet.Height.ToMils();
        if (sheetW <= 0 || sheetH <= 0) return;

        uint color = ColorHelper.Black;
        double borderWidth = Math.Max(1.0, _transform.ScaleValue(Coord.FromMils(5)));
        double gridWidth = Math.Max(1.0, _transform.ScaleValue(Coord.FromMils(3)));

        // The built-in border and recognised standard Altium templates use Altium's standard zoned
        // frame + title block, which the template does NOT embed — so we draw them. Custom templates
        // (e.g. a company A3) embed their own frame/title block; we only draw the outer paper edge.
        bool customTemplate = !string.IsNullOrEmpty(sheet.TemplateFileName) && !IsStandardTemplate(sheet.TemplateFileName);
        bool standardFrame = !customTemplate;

        if (sheet.BorderOn)
            RenderSheetBorder(context, sheet, sheetW, sheetH, color, borderWidth, standardFrame);

        if (sheet.HasTitleBlock && standardFrame)
            RenderTitleBlock(context, document, sheetW, color, gridWidth);
    }

    // Recognised standard Altium sheet templates whose title block uses the built-in geometry and
    // does NOT embed its own frame lines — so we supply the grid. Custom templates embed their own.
    private static readonly HashSet<string> StandardTemplateNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "A0", "A1", "A2", "A3", "A4", "A", "B", "C", "D", "E",
        "Letter", "Legal", "Tabloid", "OrCAD_A", "OrCAD_B", "OrCAD_C", "OrCAD_D", "OrCAD_E",
    };

    private static bool IsStandardTemplate(string templateFileName)
    {
        if (string.IsNullOrEmpty(templateFileName)) return false;
        return StandardTemplateNames.Contains(Path.GetFileNameWithoutExtension(templateFileName));
    }

    /// <summary>
    /// Draws the alphanumeric reference zones (1,2,3… along top/bottom; A,B,C… along left/right) in the
    /// border band — for every template, since Altium computes them. The outer + inner frame rectangles
    /// are drawn only for the built-in/standard frame; custom templates embed their own.
    /// </summary>
    private void RenderSheetBorder(IRenderContext context, SchSheetInfo sheet,
        double sheetW, double sheetH, uint color, double lineWidth, bool standardFrame)
    {
        double m = sheet.MarginWidth.ToMils();
        if (m <= 0 || m * 2 >= Math.Min(sheetW, sheetH)) m = BorderBand;

        // Outer paper edge + inner frame belong to the built-in/standard frame. Custom templates embed
        // their own outer+inner rectangles (RECORD=13 lines), so re-drawing here would duplicate them.
        if (standardFrame)
        {
            DrawMilsRect(context, 0, 0, sheetW, sheetH, color, lineWidth);
            DrawMilsRect(context, m, m, sheetW - m, sheetH - m, color, lineWidth);
        }

        // Reference zones (1..n top/bottom, A.. left/right, + tick divisions) are COMPUTED by Altium for
        // BOTH standard and custom templates (the template never embeds them), so draw them regardless
        // of the frame, gated only on ReferenceZonesOn.
        if (!sheet.ReferenceZonesOn) return;
        if (m * 2 >= Math.Min(sheetW, sheetH)) return;

        int zx = Math.Max(1, sheet.ZonesX);
        int zy = Math.Max(1, sheet.ZonesY);
        double fontSize = _transform.ScaleValue(Coord.FromMils(90));
        var textOpts = new TextRenderOptions
        {
            FontFamily = GetFont(0).FontName,
            HorizontalAlignment = TextHAlign.Center,
            VerticalAlignment = TextVAlign.Middle,
        };

        // Numbered zones along the top and bottom edges (1,2,3… left→right).
        double innerW = sheetW - 2 * m;
        for (int i = 0; i < zx; i++)
        {
            double x0 = m + innerW * i / zx;
            double xc = m + innerW * (i + 0.5) / zx;
            if (i > 0)
            {
                DrawMilsLine(context, x0, 0, x0, m, color, lineWidth);
                DrawMilsLine(context, x0, sheetH - m, x0, sheetH, color, lineWidth);
            }
            string label = (i + 1).ToString();
            DrawMilsText(context, label, xc, m / 2, fontSize, color, textOpts);
            DrawMilsText(context, label, xc, sheetH - m / 2, fontSize, color, textOpts);
        }

        // Lettered zones along the left and right edges (A at the top, descending).
        double innerH = sheetH - 2 * m;
        for (int i = 0; i < zy; i++)
        {
            double y0 = m + innerH * i / zy;
            double yc = m + innerH * (i + 0.5) / zy;
            if (i > 0)
            {
                DrawMilsLine(context, 0, y0, m, y0, color, lineWidth);
                DrawMilsLine(context, sheetW - m, y0, sheetW, y0, color, lineWidth);
            }
            string label = ((char)('A' + (zy - 1 - i))).ToString();
            DrawMilsText(context, label, m / 2, yc, fontSize, color, textOpts);
            DrawMilsText(context, label, sheetW - m / 2, yc, fontSize, color, textOpts);
        }
    }

    /// <summary>
    /// Draws the standard Altium title-block grid (the layout from the A-series templates) flush in
    /// the inner-frame's bottom-right corner. The grid matches the embedded template labels; for the
    /// built-in title block (no embedded labels) the captions and resolved values are drawn too.
    /// </summary>
    private void RenderTitleBlock(IRenderContext context, SchDocument document,
        double sheetW, uint color, double lineWidth)
    {
        // When the document carries embedded title-block labels, the sheet template ALSO embeds the
        // title-block grid itself (as polylines/lines that render normally) — so we must draw nothing
        // here, or we double every line. We only reconstruct the grid for the built-in title block
        // (TitleBlockOn with no template and no embedded fields), handled below.
        if (HasEmbeddedTitleBlock(document)) return;

        // Fixed physical size; bottom-right corner sits on the inner-frame corner so it touches the
        // frame exactly like Altium. Local coordinates: origin = block bottom-left, +x right, +y up.
        const double BlockW = 4650, BlockH = 600, Addr = 2500, Logo = 3600;
        double right = sheetW - BorderBand;
        double bottom = BorderBand;
        double left = right - BlockW;
        if (left < BorderBand) return;     // sheet too small for a title block

        void L(double x0, double y0, double x1, double y1) =>
            DrawMilsLine(context, left + x0, bottom + y0, left + x1, bottom + y1, color, lineWidth);

        // Outer box.
        L(0, 0, BlockW, 0); L(0, BlockH, BlockW, BlockH);
        L(0, 0, 0, BlockH); L(BlockW, 0, BlockW, BlockH);
        // Horizontal dividers — LEFT SECTION ONLY (the organization/address and logo columns to the
        // right are full-height single cells with no horizontal lines crossing them).
        const double FileTop = 100, DateTop = 220, SizeTop = 400;
        L(0, FileTop, Addr, FileTop);   // File | Date
        L(0, DateTop, Addr, DateTop);   // Date | Size
        L(0, SizeTop, Addr, SizeTop);   // Size | Title
        // Vertical dividers (left section).
        L(700, DateTop, 700, SizeTop);  // Size | Number
        L(1600, FileTop, 1600, SizeTop);// Number | Revision  and  Time | Sheet
        L(850, FileTop, 850, DateTop);  // Date | Time
        // Organization/address column + logo cell — both full height, no internal lines.
        L(Addr, 0, Addr, BlockH);   // left section | address column
        L(Logo, 0, Logo, BlockH);   // address text | logo

        double capFont = _transform.ScaleValue(Coord.FromMils(55));
        double valFont = _transform.ScaleValue(Coord.FromMils(75));
        var capOpts = new TextRenderOptions { FontFamily = GetFont(0).FontName, HorizontalAlignment = TextHAlign.Left, VerticalAlignment = TextVAlign.Middle };

        void Cap(double x, double y, string t) => DrawMilsText(context, t, left + x, bottom + y, capFont, color, capOpts);
        void Val(double x, double y, string field) =>
            DrawMilsText(context, ResolveStringIndirection(field), left + x, bottom + y, valFont, color, capOpts);

        Cap(40, 500, "Title");   Val(420, 500, "=Title");
        Cap(40, 310, "Size:");   Val(330, 310, "A4");
        Cap(740, 310, "Number:"); Val(740, 250, "=DocumentNumber");
        Cap(1640, 310, "Rev:");  Val(1980, 310, "=Revision");
        Cap(40, 160, "Date:");   Val(330, 160, "=CurrentDate");
        Cap(890, 160, "Time:");  Val(1180, 160, "=CurrentTime");
        Cap(1640, 160, "Sheet"); Val(1880, 160, "=SheetNumber"); Cap(2020, 160, "of"); Val(2180, 160, "=SheetTotal");
        Cap(40, 50, "File:");    Val(330, 50, "=DocumentName");
        Cap(Addr + 40, 540, "Organization"); Val(Addr + 40, 450, "=Organization");
    }

    /// <summary>
    /// True when the sheet already carries its own title block, so the built-in one must not be drawn.
    /// That happens when an applied sheet template embeds its own graphics (its title-block grid, captions,
    /// field labels and logo live on <see cref="SchTemplate.OwnedPrimitives"/>), or when the document
    /// itself carries title-block special-string labels (e.g. <c>=Title</c>).
    /// </summary>
    private static bool HasEmbeddedTitleBlock(SchDocument document)
    {
        // A template that embeds graphics supplies the whole title block (grid + captions + fields + logo),
        // which the built-in reconstruction can't reproduce — so defer to it entirely.
        foreach (var template in document.Templates)
            if (template.OwnedPrimitives.Count > 0)
                return true;

        foreach (var l in document.Labels)
            if (l is SchLabel sl && !string.IsNullOrEmpty(sl.Text) && sl.Text.StartsWith('='))
                return true;
        return false;
    }

    private (double, double) WorldMilsToScreen(double xMils, double yMils) =>
        _transform.WorldToScreen(Coord.FromMils(xMils), Coord.FromMils(yMils));

    private void DrawMilsLine(IRenderContext context, double x1, double y1, double x2, double y2, uint color, double width)
    {
        var (a, b) = WorldMilsToScreen(x1, y1);
        var (c, d) = WorldMilsToScreen(x2, y2);
        context.DrawLine(a, b, c, d, color, width);
    }

    private void DrawMilsRect(IRenderContext context, double xMin, double yMin, double xMax, double yMax, uint color, double width)
    {
        var (a, b) = WorldMilsToScreen(xMin, yMin);
        var (c, d) = WorldMilsToScreen(xMax, yMax);
        context.DrawRectangle(Math.Min(a, c), Math.Min(b, d), Math.Abs(c - a), Math.Abs(d - b), color, width);
    }

    private void DrawMilsText(IRenderContext context, string text, double xMils, double yMils,
        double fontSize, uint color, TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(text)) return;
        var (sx, sy) = WorldMilsToScreen(xMils, yMils);
        context.DrawText(text, sx, sy, fontSize, color, options);
    }

    // ── Bus ─────────────────────────────────────────────────────────

    /// <summary>Renders a schematic bus as a thick polyline.</summary>
    public void RenderBus(IRenderContext context, SchBus bus)
    {
        if (bus.Vertices.Count < 2) return;

        var color = bus.Color != 0 ? ColorHelper.BgrToArgb(bus.Color) : DefaultBusColor;
        // Buses are drawn thicker than wires (Altium uses ~4 mil minimum).
        var lineWidth = Math.Max(_transform.MapLineWidthEnum(bus.LineWidth),
            _transform.ScaleValue(Coord.FromMils(4)));
        if (lineWidth < 2) lineWidth = 2;
        var style = MapSchLineStyle(bus.LineStyle);

        var xs = new double[bus.Vertices.Count];
        var ys = new double[bus.Vertices.Count];
        for (int i = 0; i < bus.Vertices.Count; i++)
            (xs[i], ys[i]) = _transform.WorldToScreen(bus.Vertices[i].X, bus.Vertices[i].Y);

        context.DrawPolyline(xs, ys, color, lineWidth, style);
    }

    /// <summary>Renders a schematic bus entry (the diagonal stub joining a wire to a bus).</summary>
    public void RenderBusEntry(IRenderContext context, SchBusEntry entry)
    {
        var (x1, y1) = _transform.WorldToScreen(entry.Location.X, entry.Location.Y);
        var (x2, y2) = _transform.WorldToScreen(entry.Corner.X, entry.Corner.Y);
        var color = entry.Color != 0 ? ColorHelper.BgrToArgb(entry.Color) : DefaultBusColor;
        var lineWidth = _transform.MapLineWidthEnum(entry.LineWidth);
        context.DrawLine(x1, y1, x2, y2, color, lineWidth);
    }

    // ── No-ERC ──────────────────────────────────────────────────────

    /// <summary>Renders a No-ERC marker as an X cross at the connection point.</summary>
    public void RenderNoErc(IRenderContext context, SchNoErc noErc)
    {
        var (sx, sy) = _transform.WorldToScreen(noErc.Location.X, noErc.Location.Y);
        var color = noErc.Color != 0 ? ColorHelper.BgrToArgb(noErc.Color) : ColorHelper.Red;
        var s = _transform.ScaleValue(Coord.FromMils(noErc.Symbol == 1 ? 8 : 4));
        if (s < 3) s = 3;
        var w = Math.Max(1.0, _transform.ScaleValue(Coord.FromMils(4)));
        context.DrawLine(sx - s, sy - s, sx + s, sy + s, color, w);
        context.DrawLine(sx - s, sy + s, sx + s, sy - s, color, w);
    }

    // ── Port ────────────────────────────────────────────────────────

    /// <summary>Renders a sheet port as a flag/hexagon outline with its net name.</summary>
    public void RenderPort(IRenderContext context, SchPort port)
    {
        var w = port.Width > Coord.Zero ? port.Width : Coord.FromMils(100);
        var h = port.Height > Coord.Zero ? port.Height : Coord.FromMils(20);
        // The port's Location is the wire connection point: it sits at the vertical centre of the
        // port body's end, so the body is centred on Location.Y (was drawn upward from it, leaving
        // the wire attached to the bottom edge ~½·height = 50 mil too low).
        var half = Coord.FromRaw(h.ToRaw() / 2);
        var (sx0, sy0) = _transform.WorldToScreen(port.Location.X, port.Location.Y - half);
        var (sx1, sy1) = _transform.WorldToScreen(port.Location.X + w, port.Location.Y + half);

        double left = Math.Min(sx0, sx1), right = Math.Max(sx0, sx1);
        double top = Math.Min(sy0, sy1), bottom = Math.Max(sy0, sy1);
        double midY = (top + bottom) / 2.0;
        double cham = Math.Min((bottom - top) / 2.0, (right - left) / 2.0);

        var xs = new[] { left + cham, right - cham, right, right - cham, left + cham, left };
        var ys = new[] { top, top, midY, bottom, bottom, midY };

        // A harness port (one carrying a HarnessType) is drawn in the harness colour — a light blue —
        // not the yellow signal-port colour, so it visually matches its bundle and connector.
        uint fill, border;
        if (!string.IsNullOrEmpty(port.HarnessType))
        {
            fill = _harnessBundleColor != 0 ? _harnessBundleColor : ColorHelper.FromRgb(184, 201, 230);
            border = Darken(fill, 0.6);
        }
        else
        {
            fill = port.AreaColor != 0 ? ColorHelper.BgrToArgb(port.AreaColor) : ColorHelper.FromRgb(255, 255, 180);
            border = port.Color != 0 ? ColorHelper.BgrToArgb(port.Color) : DefaultBusColor;
        }
        context.FillPolygon(xs, ys, fill);
        context.DrawPolygon(xs, ys, border, _transform.MapLineWidthEnum(port.BorderWidth));

        if (!string.IsNullOrEmpty(port.Name))
        {
            var (cx, cy) = _transform.WorldToScreen(port.Location.X + w, port.Location.Y + h);
            var textColor = port.TextColor != 0 ? ColorHelper.BgrToArgb(port.TextColor) : ColorHelper.Black;
            var font = GetFont(port.FontId);
            var fontSize = GetFontSize(port.FontId);
            context.DrawText(port.Name, (left + right) / 2.0, midY, fontSize, textColor,
                new TextRenderOptions
                {
                    FontFamily = font.FontName,
                    Bold = font.Bold,
                    Italic = font.Italic,
                    HorizontalAlignment = TextHAlign.Center,
                    VerticalAlignment = TextVAlign.Middle
                });
        }
    }

    // ── Sheet symbol ────────────────────────────────────────────────

    /// <summary>Renders a hierarchical sheet symbol (box + names) and its sheet entries.</summary>
    public void RenderSheetSymbol(IRenderContext context, SchSheetSymbol sheet)
    {
        // Altium anchors the symbol at its TOP-left corner: Location is the top edge and the body
        // extends DOWNWARD by YSize. (The old code extended upward, mis-placing the box and entries.)
        var topY = sheet.Location.Y;
        var bottomY = sheet.Location.Y - sheet.YSize;
        var (sx0, sy0) = _transform.WorldToScreen(sheet.Location.X, topY);
        var (sx1, sy1) = _transform.WorldToScreen(sheet.Location.X + sheet.XSize, bottomY);
        double x = Math.Min(sx0, sx1), y = Math.Min(sy0, sy1);
        double w = Math.Abs(sx1 - sx0), h = Math.Abs(sy1 - sy0);

        var border = sheet.Color != 0 ? ColorHelper.BgrToArgb(sheet.Color) : DefaultBusColor;
        var lineWidth = _transform.MapLineWidthEnum(sheet.LineWidth);

        if (sheet.IsSolid)
            context.FillRectangle(x, y, w, h, ColorHelper.BgrToArgb(sheet.AreaColor));
        context.DrawRectangle(x, y, w, h, border, lineWidth);

        foreach (var entry in sheet.Entries)
            RenderSheetEntry(context, sheet, entry);

        // The sheet name (designator) and file name are separate, positioned child labels with their
        // own location, colour and font — render them as labels rather than auto-placing the strings.
        if (sheet.NameLabel != null)
            RenderLabel(context, sheet.NameLabel);
        else if (!string.IsNullOrEmpty(sheet.SheetName))
            DrawSheetSymbolStringFallback(context, sheet.SheetName!, x, y, border, above: true);

        if (sheet.FileNameLabel != null)
            RenderLabel(context, sheet.FileNameLabel);
        else if (!string.IsNullOrEmpty(sheet.FileName))
            DrawSheetSymbolStringFallback(context, sheet.FileName!, x, y, border, above: false);
    }

    /// <summary>Auto-places a sheet symbol's name/file string when no positioned label record exists.</summary>
    private void DrawSheetSymbolStringFallback(IRenderContext context, string text,
        double boxX, double boxTopY, uint color, bool above)
    {
        var font = GetFont(0);
        var fontSize = GetFontSize(0);
        // Both the name and the file name sit ABOVE the box in Altium (name on top, file just under it).
        double ty = above ? boxTopY - fontSize * 1.4 : boxTopY - fontSize * 0.2;
        context.DrawText(text, boxX, ty, fontSize, color, new TextRenderOptions
        {
            FontFamily = font.FontName,
            Bold = font.Bold,
            Italic = font.Italic,
            HorizontalAlignment = TextHAlign.Left,
            VerticalAlignment = TextVAlign.Bottom
        });
    }

    // Sheet-entry symbol dimensions (world mils): L = depth into the body, H = half height,
    // A = the triangular arrow notch depth. Chosen to match Altium's standard "Block & Triangle" entry.
    private const double EntryLengthMils = 100.0;
    private const double EntryHalfHeightMils = 25.0;
    private const double EntryArrowMils = 50.0;

    private void RenderSheetEntry(IRenderContext context, SchSheetSymbol sheet, SchSheetEntry entry)
    {
        // The connection point sits on the symbol edge; DistanceFromTop is measured DOWN from the top.
        var ey = sheet.Location.Y - entry.DistanceFromTop;
        bool left = entry.Side != 1; // 1 = Right; everything else anchors to the left edge
        var ex = left ? sheet.Location.X : sheet.Location.X + sheet.XSize;

        var (px, py) = _transform.WorldToScreen(ex, ey);
        var fill = entry.AreaColor != 0 ? ColorHelper.BgrToArgb(entry.AreaColor) : ColorHelper.FromRgb(255, 255, 180);
        var border = entry.Color != 0 ? ColorHelper.BgrToArgb(entry.Color) : DefaultBusColor;
        double lineWidth = Math.Max(1.0, _transform.ScaleValue(Coord.FromMils(3)));

        double dir = left ? 1 : -1; // screen-x direction pointing INTO the body
        double L = _transform.ScaleValue(Coord.FromMils(EntryLengthMils));
        double H = _transform.ScaleValue(Coord.FromMils(EntryHalfHeightMils));
        double A = _transform.ScaleValue(Coord.FromMils(EntryArrowMils));

        double xEdge = px;                 // at the body edge (wire connection)
        double xFar = px + dir * L;        // deepest point inside the body
        double xInnerEdge = px + dir * A;  // arrow notch near the edge
        double xInnerFar = px + dir * (L - A); // arrow notch near the far end

        double[] xs, ys;
        switch (entry.IoType)
        {
            case 2: // Input — flat at the edge, arrow pointing INTO the body
                xs = new[] { xInnerFar, xFar, xInnerFar, xEdge, xEdge };
                ys = new[] { py - H, py, py + H, py + H, py - H };
                break;
            case 1: // Output — arrow pointing OUT toward the connection, flat far side
                xs = new[] { xFar, xFar, xInnerEdge, xEdge, xInnerEdge };
                ys = new[] { py - H, py + H, py + H, py, py - H };
                break;
            case 3: // Bidirectional — double-pointed hexagon
                xs = new[] { xInnerFar, xFar, xInnerFar, xInnerEdge, xEdge, xInnerEdge };
                ys = new[] { py - H, py, py + H, py + H, py, py - H };
                break;
            default: // Unspecified — plain rectangle
                xs = new[] { xEdge, xFar, xFar, xEdge };
                ys = new[] { py - H, py - H, py + H, py + H };
                break;
        }

        context.FillPolygon(xs, ys, fill);
        context.DrawPolygon(xs, ys, border, lineWidth);

        if (!string.IsNullOrEmpty(entry.Name))
        {
            var font = GetFont(entry.FontId);
            var fontSize = GetFontSize(entry.FontId);
            var textColor = entry.TextColor != 0 ? ColorHelper.BgrToArgb(entry.TextColor) : border;
            double gap = _transform.ScaleValue(Coord.FromMils(20));
            context.DrawText(entry.Name, xFar + dir * gap, py, fontSize, textColor,
                new TextRenderOptions
                {
                    FontFamily = font.FontName,
                    Bold = font.Bold,
                    Italic = font.Italic,
                    HorizontalAlignment = left ? TextHAlign.Left : TextHAlign.Right,
                    VerticalAlignment = TextVAlign.Middle
                });
        }
    }

    // ── Harness ─────────────────────────────────────────────────────

    /// <summary>Renders a signal harness (the thick bundle wire connecting a port to a harness connector).</summary>
    public void RenderSignalHarness(IRenderContext context, SchSignalHarness harness)
    {
        if (harness.Vertices.Count < 2) return;
        var color = harness.Color != 0 ? ColorHelper.BgrToArgb(harness.Color) : DefaultBusColor;
        // A signal harness is much thicker than a normal wire (and a light harness colour). LineWidth is
        // a 0-3 index; map it generously so the bundle reads as a fat band, not a wire.
        var lineWidth = Math.Max(_transform.ScaleValue(Coord.FromMils(Math.Max(harness.LineWidth, 1) * 8.0)), 3.0);
        var xs = new double[harness.Vertices.Count];
        var ys = new double[harness.Vertices.Count];
        for (int i = 0; i < harness.Vertices.Count; i++)
            (xs[i], ys[i]) = _transform.WorldToScreen(harness.Vertices[i].X, harness.Vertices[i].Y);
        context.DrawPolyline(xs, ys, color, lineWidth);
    }

    /// <summary>Renders a harness connector: its rounded body, type label, and bundle entries.</summary>
    public void RenderHarnessConnector(IRenderContext context, SchHarnessConnector connector)
    {
        // Location is the TOP-left; the body extends right by XSize and down by YSize.
        var topY = connector.Location.Y;
        var bottomY = connector.Location.Y - connector.YSize;
        var (sx0, sy0) = _transform.WorldToScreen(connector.Location.X, topY);
        var (sx1, sy1) = _transform.WorldToScreen(connector.Location.X + connector.XSize, bottomY);
        double x = Math.Min(sx0, sx1), y = Math.Min(sy0, sy1);
        double w = Math.Abs(sx1 - sx0), h = Math.Abs(sy1 - sy0);
        if (w <= 0 || h <= 0) return;

        var border = connector.Color != 0 ? ColorHelper.BgrToArgb(connector.Color) : DefaultBusColor;
        var fill = ColorHelper.BgrToArgb(connector.AreaColor);
        var lineWidth = _transform.MapLineWidthEnum(connector.LineWidth);
        double radius = Math.Min(Math.Min(w, h) / 2.0, _transform.ScaleValue(Coord.FromMils(50)));

        context.FillRoundedRectangle(x, y, w, h, radius, fill);
        context.DrawRoundedRectangle(x, y, w, h, radius, radius, border, lineWidth);

        if (connector.TypeLabel is { } tl && !string.IsNullOrEmpty(tl.Text))
        {
            var (tx, ty) = _transform.WorldToScreen(tl.Location.X, tl.Location.Y);
            var tcolor = tl.Color != 0 ? ColorHelper.BgrToArgb(tl.Color) : border;
            var font = GetFont(tl.FontId);
            // Altium anchors the harness-type label at the RIGHT of the text (the label's Location is
            // its right edge), so the name extends leftward from there.
            context.DrawText(FixTextEncoding(tl.Text), tx, ty, GetFontSize(tl.FontId), tcolor,
                new TextRenderOptions
                {
                    FontFamily = font.FontName,
                    Bold = font.Bold,
                    Italic = font.Italic,
                    HorizontalAlignment = TextHAlign.Right,
                    VerticalAlignment = TextVAlign.Bottom
                });
        }

        foreach (var entry in connector.Entries)
            RenderHarnessEntry(context, connector, x, y, w, h, border, entry);
    }

    private void RenderHarnessEntry(IRenderContext context, SchHarnessConnector connector,
        double boxX, double boxY, double boxW, double boxH, uint defaultColor, SchHarnessEntry entry)
    {
        bool right = entry.Side == 1; // 0 = Left edge, 1 = Right edge
        var ey = connector.Location.Y - entry.DistanceFromTop;
        var ex = right ? connector.Location.X + connector.XSize : connector.Location.X;
        var (px, py) = _transform.WorldToScreen(ex, ey);

        var textColor = entry.TextColor != 0 ? ColorHelper.BgrToArgb(entry.TextColor) : defaultColor;
        var font = GetFont(entry.TextFontId);
        var fontSize = GetFontSize(entry.TextFontId);

        // Connection dot on the body edge where the entry's wire attaches.
        double dot = Math.Max(2.0, _transform.ScaleValue(Coord.FromMils(6)));
        context.FillEllipse(px, py, dot / 2, dot / 2, textColor);

        if (string.IsNullOrEmpty(entry.Text)) return;
        // Entry name sits INSIDE the body, aligned to its connection edge (Altium lists the bundle
        // members inside the connector box).
        double margin = _transform.ScaleValue(Coord.FromMils(15));
        double textX = right ? boxX + boxW - margin : boxX + margin;
        context.DrawText(FixTextEncoding(entry.Text), textX, py, fontSize, textColor,
            new TextRenderOptions
            {
                FontFamily = font.FontName,
                Bold = font.Bold,
                Italic = font.Italic,
                HorizontalAlignment = right ? TextHAlign.Right : TextHAlign.Left,
                VerticalAlignment = TextVAlign.Middle
            });
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static uint GetArgbColor(int bgrColor)
    {
        return bgrColor != 0 ? ColorHelper.BgrToArgb(bgrColor) : ColorHelper.Black;
    }

    /// <summary>Returns the ARGB colour with its RGB channels scaled by <paramref name="factor"/> (alpha kept).</summary>
    private static uint Darken(uint argb, double factor)
    {
        uint a = (argb >> 24) & 0xFF;
        uint r = (uint)(((argb >> 16) & 0xFF) * factor);
        uint g = (uint)(((argb >> 8) & 0xFF) * factor);
        uint b = (uint)((argb & 0xFF) * factor);
        return (a << 24) | (r << 16) | (g << 8) | b;
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
            1 => LineStyle.Dash,
            2 => LineStyle.Dot,
            3 => LineStyle.DashDot,
            _ => LineStyle.Solid
        };
    }

    private static LineStyle MapSchLineStyleEnum(SchLineStyle lineStyle)
    {
        return lineStyle switch
        {
            SchLineStyle.Dashed => LineStyle.Dash,
            SchLineStyle.Dotted => LineStyle.Dot,
            _ => LineStyle.Solid
        };
    }

    private static (TextHAlign h, TextVAlign v) MapJustification(TextJustification justification)
    {
        return justification switch
        {
            TextJustification.BottomLeft => (TextHAlign.Left, TextVAlign.Bottom),
            TextJustification.BottomCenter => (TextHAlign.Center, TextVAlign.Bottom),
            TextJustification.BottomRight => (TextHAlign.Right, TextVAlign.Bottom),
            TextJustification.MiddleLeft => (TextHAlign.Left, TextVAlign.Middle),
            TextJustification.MiddleCenter => (TextHAlign.Center, TextVAlign.Middle),
            TextJustification.MiddleRight => (TextHAlign.Right, TextVAlign.Middle),
            TextJustification.TopLeft => (TextHAlign.Left, TextVAlign.Top),
            TextJustification.TopCenter => (TextHAlign.Center, TextVAlign.Top),
            TextJustification.TopRight => (TextHAlign.Right, TextVAlign.Top),
            _ => (TextHAlign.Left, TextVAlign.Bottom)
        };
    }

    private SchFontInfo GetFont(int fontId)
    {
        // FontId is 1-based. An out-of-range / zero id (e.g. pin name & designator text, sheet entries)
        // resolves to the document's SYSTEM font — NOT blindly the first table entry. Altium's first
        // table slot is often Times New Roman while the sheet's real default is e.g. Trebuchet MS.
        if (Fonts != null && Fonts.Count > 0)
        {
            if (fontId < 1 || fontId > Fonts.Count)
                fontId = (SystemFontId >= 1 && SystemFontId <= Fonts.Count) ? SystemFontId : 1;
            return Fonts[fontId - 1];
        }
        return new SchFontInfo("Arial", DefaultFontSize, false, false);
    }

    private double GetFontSize(int fontId)
    {
        var font = GetFont(fontId);
        // Convert the point size to a world height (mils) and scale by zoom so schematic text
        // tracks the drawing instead of being a fixed pixel size. Floor at 1px for visibility.
        var px = _transform.ScaleValue(Coord.FromMils(font.Size * PointToMils));
        return px < 1.0 ? 1.0 : px;
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
        // Text is drawn vertically centered on startY, so the cap tops are ~0.4·fontSize above it.
        // Place the overline just above the caps (was a full fontSize too high, so it clipped).
        double overlineY = startY - fontSize * 0.46;
        double overlineWidth = Math.Max(1.0, fontSize * 0.06);

        foreach (var segment in segments)
        {
            var metrics = context.MeasureText(segment.Text, fontSize, options);
            if (segment.HasOverline)
            {
                context.DrawLine(currentX, overlineY, currentX + metrics.Width, overlineY, color, overlineWidth);
            }
            currentX += metrics.Width;
        }
    }

    /// <summary>
    /// Resolves string indirection: if the text starts with "=" it looks up the
    /// parameter value from the current component's parameter list.
    /// For example, "=Value" resolves to the Value parameter's text.
    /// </summary>
    private string ResolveStringIndirection(string text) => FixTextEncoding(Resolve(text));

    private string Resolve(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.StartsWith('='))
            return text;

        var parameterName = text.Substring(1);

        // 1. Component parameters (when rendering inside a component scope).
        if (_currentComponent != null)
        {
            foreach (var param in _currentComponent.Parameters)
            {
                if (string.Equals(param.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                    return param.Value;
            }
        }

        // 2. Live-computed special strings. Altium evaluates these at render time and they are NOT
        //    backed by a stored parameter, so they must win over any same-named "*" placeholder param.
        if (TryResolveComputedString(parameterName, out var computed))
            return computed;

        // 3. Document parameters (title-block fields: Title, Revision, Organization, SheetNumber, …).
        if (_documentParameters != null &&
            _documentParameters.TryGetValue(parameterName, out var docValue) &&
            !string.IsNullOrEmpty(docValue))
        {
            return docValue;
        }

        // 4. Sheet numbering fallback when no parameter supplies it.
        if (string.Equals(parameterName, "SheetNumber", StringComparison.OrdinalIgnoreCase)) return "1";
        if (string.Equals(parameterName, "SheetTotal", StringComparison.OrdinalIgnoreCase)) return "1";

        // 5. Unknown — show the field name without the leading "=".
        return parameterName;
    }

    /// <summary>
    /// Resolves Altium's live special strings (current date/time, document name/path) that Altium
    /// computes at render time rather than reading from a parameter. Returns false for other names.
    /// </summary>
    private bool TryResolveComputedString(string name, out string value)
    {
        var now = DateTime.Now;
        switch (name.ToUpperInvariant())
        {
            case "CURRENTDATE":
                value = now.ToShortDateString();
                return true;
            case "CURRENTTIME":
                value = now.ToLongTimeString();
                return true;
            case "DOCUMENTNAME":
            case "SHEETNAME":
                value = DocumentFileName ?? string.Empty;
                return true;
            case "DOCUMENTFULLPATHANDNAME":
                value = DocumentFullPath ?? DocumentFileName ?? string.Empty;
                return true;
            default:
                value = string.Empty;
                return false;
        }
    }

    /// <summary>
    /// Repairs UTF-8 parameter values that were decoded as Windows-1252 (Altium stores some text,
    /// e.g. "µF", UTF-8-encoded behind a %UTF8% marker the reader doesn't decode, so "µ" arrives as
    /// "Âµ"). Re-interprets the Latin-1 bytes as UTF-8 when that yields a valid string.
    /// </summary>
    internal static string FixTextEncoding(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        bool suspect = false;
        foreach (var c in text)
        {
            if (c > 0xFF) return text;              // already real Unicode — leave it
            if (c is 'Â' or 'Ã') suspect = true; // UTF-8 lead bytes seen as Â / Ã
        }
        if (!suspect) return text;

        try
        {
            var bytes = System.Text.Encoding.Latin1.GetBytes(text);
            return new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch
        {
            return text;
        }
    }

    /// <summary>
    /// Checks whether a primitive should be rendered based on the current PartFilter.
    /// Uses dynamic property access since OwnerPartId is not on the IPrimitive interface.
    /// </summary>
    private bool IsPartVisible(object primitive)
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
    private static int GetOwnerPartId(object primitive)
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
