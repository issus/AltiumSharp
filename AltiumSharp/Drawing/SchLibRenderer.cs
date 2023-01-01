using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp.Drawing
{
    public sealed class SchLibRenderer : Renderer
    {
        private SchDocumentHeader _header;
        private SchLib _assets;

        public SchLibRenderer(SchDocumentHeader header, SchLib assets)
        {
            _header = header ?? new SchSheetHeader();
            _assets = assets;
        }

        private const double FontScalingAdjust = 0.667; // value obtained empirically

        /// <summary>
        /// Creates a font from a <paramref name="fontId"/> value.
        /// </summary>
        /// <param name="fontId">Font identifier number.</param>
        /// <returns></returns>
        private Font CreateFont(int fontId)
        {
            if (fontId < 1 || fontId > _header.FontId.Count) fontId = _header.SystemFont;
            var f = _header.FontId[fontId - 1];
            var fontStyle = FontStyle.Regular;
            if (f.Italic) fontStyle |= FontStyle.Italic;
            if (f.Bold) fontStyle |= FontStyle.Bold;

            var emSize = ScalePixelLength(f.Size * FontScalingAdjust);
            return new Font(f.FontName, emSize, fontStyle, GraphicsUnit.Point);
        }

        private IEnumerable<SchPrimitive> GetAsset(string name)
        {
            return _assets?.Items.SingleOrDefault(c => c.LibReference == name)?.GetAllPrimitives().Where(p => p.IsVisible);
        }

        /// <summary>
        /// Calculate the screen coordinate width for a <see cref="LineWidth"/> value.
        /// </summary>
        private float ScaleLineWidth(LineWidth lineWidth)
        {
            // width 1 equals to 1px at 100% zoom, width 2 equals to 2px, and so on
            switch (lineWidth)
            {
                case LineWidth.Small:
                    return ScalePixelLength(1.0f);
                case LineWidth.Medium:
                    return ScalePixelLength(3.0f);
                case LineWidth.Large:
                    return ScalePixelLength(5.0f);
                default:
                    return 0.0f;
            }
        }

        private static RectangleF CalculateScreenBounds(Graphics g, in RectangleF bounds)
        {
            var points = new PointF[] {
                new PointF(bounds.Left, bounds.Top),
                new PointF(bounds.Right, bounds.Top),
                new PointF(bounds.Right, bounds.Bottom),
                new PointF(bounds.Left, bounds.Bottom),
            };
            g.Transform.TransformPoints(points);
            var minX = points.Min(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxX = points.Max(p => p.X);
            var maxY = points.Max(p => p.Y);
            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Implements rendering of the SchLib.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="fastRendering"></param>
        protected override void DoRender(Graphics graphics, bool fastRendering = false)
        {
            if (graphics == null) throw new ArgumentNullException(nameof(graphics));

            var primitives = Component.GetPrimitivesOfType<SchPrimitive>();
            var orderedPrimitives = primitives
                .Where(p => IsPrimitiveVisibleInScreen(p));
            Debug.WriteLine($"Rendering {orderedPrimitives.Count()} / {primitives.Count()}");

            RenderPrimitives(graphics, fastRendering, orderedPrimitives);
        }

        private void RenderPrimitives(Graphics graphics, bool fastRendering, IEnumerable<SchPrimitive> primitives)
        {
            if (primitives == null) return;

            foreach (var primitive in primitives)
            {
                // using Graphics.BeginContainer() instead of Graphics.Save() because
                // the former saves the previous transform as the default, and we
                // can then call Graphics.ResetTransform() to go back to the transform
                // that was set when the container was created.
                // Save() just resets the transform to identity
                var graphicsContainer = graphics.BeginContainer();
                // Drawback is that we need to set SmothingMode, etc. for each container
                DrawingUtils.SetupGraphics(graphics, fastRendering);

                switch (primitive)
                {
                    case SchPin pin:
                        RenderPinPrimitive(graphics, pin);
                        break;
                    case SchSymbol symbol:
                        RenderSymbolPrimitive(graphics, symbol);
                        break;
                    case SchPowerObject powerPort:
                        RenderPowerPortPrimitive(graphics, powerPort);
                        break;
                    case SchLabel textString:
                        // this can handle SchDesignator and SchParameter through inheritance
                        RenderTextStringPrimitive(graphics, textString);
                        break;
                    case SchBezier bezier:
                        RenderBezierPrimitive(graphics, bezier);
                        break;
                    case SchPolyline polyline:
                        RenderBasicPolyline(graphics, polyline);
                        break;
                    case SchWire wire:
                        RenderBasicPolyline(graphics, wire);
                        break;
                    case SchPolygon polygon:
                        RenderPolygonPrimitive(graphics, polygon);
                        break;
                    case SchEllipse ellipse:
                        RenderEllipsePrimitive(graphics, ellipse);
                        break;
                    case SchPie pieChart:
                        RenderPieChartPrimitive(graphics, pieChart);
                        break;
                    case SchRoundedRectangle roundedRect:
                        RenderRoundedRectPrimitive(graphics, roundedRect);
                        break;
                    case SchArc arc:
                        // this can handle SchEllipticalArc through inheritance
                        RenderArcPrimitive(graphics, arc);
                        break;
                    case SchLine line:
                        RenderLinePrimitive(graphics, line);
                        break;
                    case SchImage image:
                        RenderImagePrimitive(graphics, image);
                        break;
                    case SchTextFrame textFrame:
                        RenderTextFramePrimitive(graphics, textFrame);
                        break;
                    case SchRectangle rectangle:
                        RenderRectanglePrimitive(graphics, rectangle);
                        break;
                    case SchJunction junction:
                        RenderJunctionPrimitive(graphics, junction);
                        break;
                }
                graphics.EndContainer(graphicsContainer);
            }
        }

        private void RenderAt(Graphics graphics, bool fastRendering, CoordPoint location, float angle, Color penColorOverride, Action callback)
        {
            var savePenColorOverride = PenColorOverride;
            var center = Center;
            try
            {
                PenColorOverride = penColorOverride;
                Center = new CoordPoint(center.X - location.X, center.Y - location.Y);

                var graphicsContainer = graphics.BeginContainer();
                DrawingUtils.SetupGraphics(graphics, fastRendering);

                using (var matrix = new Matrix())
                {
                    matrix.RotateAt(angle, ScreenFromWorld(0, 0));
                    graphics.MultiplyTransform(matrix);
                }

                callback();

                graphics.EndContainer(graphicsContainer);
            }
            finally
            {
                PenColorOverride = savePenColorOverride;
                Center = center;
            }
        }

        private void RenderPrimitivesAt(Graphics graphics, bool fastRendering, CoordPoint location, float angle, IEnumerable<SchPrimitive> primitives, Color penColorOverride)
        {
            RenderAt(graphics, fastRendering, location, angle, penColorOverride, () =>
            {
                RenderPrimitives(graphics, fastRendering, primitives);
            });
        }

        /// <summary>
        /// Draws a bar over the text of a pin to symbolize negative logic.
        /// </summary>
        private void DrawOverline(Graphics g, string text, Font font, Pen pen, float x, float y,
            StringAlignmentKind alignmentKind, StringAlignment horizontalAlignment, StringAlignment verticalAlignment)
        {
            var overlineRanges = OverlineHelper.Parse(text);
            if (overlineRanges.Length == 0) return;

            var plainText = text.Replace(@"\", "", StringComparison.Ordinal);
            var offsetX = ScalePixelLength(0.5f);
            var offsetY = 0.0f;

            var rangeBounds = DrawingUtils.CalculateTextExtent(g, plainText, x, y, font,
                alignmentKind, horizontalAlignment, verticalAlignment, overlineRanges);
            foreach (var bound in rangeBounds)
            {
                var inflatedBound = bound.Inflated(-offsetX, -offsetY);
                g.DrawLine(pen, inflatedBound.X, inflatedBound.Y, inflatedBound.Right, inflatedBound.Y);
            }
        }

        private void RenderPinPrimitive(Graphics g, SchPin pin)
        {
            var location = ScreenFromWorld(pin.Location.X, pin.Location.Y);
            g.TranslateTransform(location.X, location.Y);

            var direction = 1;
            var displayNameHorizontalAlignment = StringAlignment.Far;
            var designatorHorizontalAlignment = StringAlignment.Near;
            var penWidth = ScaleLineWidth(LineWidth.Small);

            if (pin.PinConglomerate.HasFlag(PinConglomerateFlags.Rotated))
            {
                g.RotateTransform(-90);
            }

            if (pin.PinConglomerate.HasFlag(PinConglomerateFlags.Flipped))
            {
                direction = -1;
                displayNameHorizontalAlignment = StringAlignment.Near;
                designatorHorizontalAlignment = StringAlignment.Far;
            }

            var pinLength = ScaleCoord(pin.PinLength) * direction;

            using (var pen = CreatePen(pin.Color, penWidth))
            {
                g.DrawLine(pen, 0.0f, 0.0f, pinLength, 0.0f);
            }

            using (var pen = CreatePen(pin.Color, 0))
            {
                var arrowWidth = Coord.FromMils(60);
                var arrowHeight = Coord.FromMils(20);
                var arrowOffset = 0;
                var arrowPoints = new[]
                    { new CoordPoint(0, 0), new CoordPoint(arrowWidth, -arrowHeight), new CoordPoint(arrowWidth, arrowHeight) };
                if (direction < 0f)
                {
                    Utils.RotatePoints(ref arrowPoints, CoordPoint.Zero, 180f);
                }

                if (pin.Electrical == PinElectricalType.Input || pin.Electrical == PinElectricalType.InputOutput)
                {
                    var points = arrowPoints.Select(p => ScalePoint(p)).ToArray();
                    g.FillPolygon(Brushes.White, points);
                    g.DrawPolygon(pen, points);
                    arrowOffset = Coord.FromMils(70);
                }
                if (pin.Electrical == PinElectricalType.Output || pin.Electrical == PinElectricalType.InputOutput)
                {
                    var anchor = new CoordPoint(arrowWidth * direction, 0);
                    var offset = new CoordPoint((arrowOffset - arrowWidth) * direction, 0);
                    Utils.TranslatePoints(ref Utils.RotatePoints(ref arrowPoints, anchor, 180f), offset);
                    var points = arrowPoints.Select(p => ScalePoint(p)).ToArray();
                    g.FillPolygon(Brushes.White, points);
                    g.DrawPolygon(pen, points);
                }
            }

            using (var brush = new SolidBrush(pin.Color))
            using (var font = CreateFont(0))
            {
                if (pin.PinConglomerate.HasFlag(PinConglomerateFlags.DisplayNameVisible) && !string.IsNullOrEmpty(pin.Name))
                {
                    var x = ScalePixelLength(-5.0f) * direction;
                    var displayName = pin.Name.Replace(@"\", "", StringComparison.Ordinal);
                    DrawingUtils.DrawString(g, displayName, font, brush, x, ScalePixelLength(0.5),
                        StringAlignmentKind.Default, displayNameHorizontalAlignment, StringAlignment.Center);
                    using (var pen = CreatePen(pin.Color, ScaleLineWidth(LineWidth.Small)))
                    {
                        DrawOverline(g, pin.Name, font, pen, x, ScalePixelLength(0.5),
                            StringAlignmentKind.Default, displayNameHorizontalAlignment, StringAlignment.Center);
                    }
                }

                if (pin.PinConglomerate.HasFlag(PinConglomerateFlags.DesignatorVisible) && !string.IsNullOrEmpty(pin.Designator))
                {
                    DrawingUtils.DrawString(g, pin.Designator, font, brush,
                        ScalePixelLength(8.0f) * direction, 0,
                        StringAlignmentKind.Extent, designatorHorizontalAlignment, StringAlignment.Far);
                }
            }
        }

        private void RenderSymbolPrimitive(Graphics graphics, SchSymbol symbol)
        {
            //throw new NotImplementedException();
        }

        private void RenderTextStringPrimitive(Graphics g, SchLabel textString)
        {
            var location = ScreenFromWorld(textString.Location.X, textString.Location.Y);
            using (var brush = new SolidBrush(textString.Color))
            using (var font = CreateFont(textString.FontId))
            {
                StringAlignment horizontalAlignment;
                if (textString.Justification == TextJustification.BottomLeft ||
                    textString.Justification == TextJustification.MiddleLeft ||
                    textString.Justification == TextJustification.TopLeft)
                {
                    horizontalAlignment = StringAlignment.Near;
                }
                else if (textString.Justification == TextJustification.BottomCenter ||
                    textString.Justification == TextJustification.MiddleCenter ||
                    textString.Justification == TextJustification.TopCenter)
                {
                    horizontalAlignment = StringAlignment.Center;
                }
                else
                {
                    horizontalAlignment = StringAlignment.Far;
                }

                StringAlignment verticalAlignment;
                if (textString.Justification == TextJustification.BottomLeft ||
                    textString.Justification == TextJustification.BottomCenter ||
                    textString.Justification == TextJustification.BottomRight)
                {
                    verticalAlignment = StringAlignment.Far;
                }
                else if (textString.Justification == TextJustification.MiddleLeft ||
                    textString.Justification == TextJustification.MiddleCenter ||
                    textString.Justification == TextJustification.MiddleRight)
                {
                    verticalAlignment = StringAlignment.Center;
                }
                else
                {
                    verticalAlignment = StringAlignment.Near;
                }

                g.TranslateTransform(location.X, location.Y);
                if (textString.Orientation.HasFlag(TextOrientations.Rotated))
                {
                    g.RotateTransform(-90);
                }
                if (textString.Orientation.HasFlag(TextOrientations.Flipped))
                {
                    g.RotateTransform(-180);
                }

                var displayText = ProcessStringIndirection(textString, textString.DisplayText);

                DrawingUtils.DrawString(g, displayText, font, brush, 0, 0,
                    StringAlignmentKind.Extent, horizontalAlignment, verticalAlignment);

                PrimitiveScreenBounds[textString] = CalculateScreenBounds(g,
                    DrawingUtils.CalculateTextExtent(g, displayText, font, 0, 0,
                        StringAlignmentKind.Extent, horizontalAlignment, verticalAlignment)); ;
            }
        }

        private string ProcessStringIndirection(SchPrimitive primitive, string displayText)
        {
            if (!displayText.StartsWith("=", StringComparison.InvariantCultureIgnoreCase))
            {
                return displayText;
            }

            // TODO: support predefined special strings like =CurrentDate, =CurrentTime, =DocumentFullPathAndName, etc
            // https://www.altium.com/documentation/18.0/display/ADES/Sch_Obj-Parameter((Parameter))_AD#!Parameter-SchematicPredefinedSpecialStrings
            var parameterName = displayText.Substring(1);
            var parameter = FindParameter(primitive, parameterName);

            // if no parameter was found then display the string indirection parameter name
            return parameter?.DisplayText ?? parameterName;
        }

        private SchParameter FindParameter(SchPrimitive primitive, string parameterName)
        {
            SchParameter parameterRecord = null;
            while (parameterRecord == null && primitive != null)
            {
                parameterRecord = FindParameterInContainer(primitive, parameterName);
                primitive = primitive.Owner as SchPrimitive;
            }

            // fallback to document parameters
            if (primitive == null) parameterRecord = FindParameterInContainer(Component, parameterName);

            return parameterRecord;
        }

        private static SchParameter FindParameterInContainer(IContainer container, string parameterName)
        {
            return container.GetPrimitivesOfType<SchParameter>(false)
                .FirstOrDefault(p => p.Name.ToUpperInvariant() == parameterName.ToUpperInvariant());
        }

        private void RenderBezierPrimitive(Graphics g, SchBezier bezier)
        {
            var penWidth = ScaleLineWidth(bezier.LineWidth);
            using (var pen = CreatePen(bezier.Color, penWidth))
            {
                var points = bezier.Vertices.Select(coordxy => ScreenFromWorld(coordxy)).ToArray();
                g.DrawBezier(pen, points[0], points[1], points[2], points[3]);
            }
        }

        /// <summary>
        /// Creates a custom line cap given the <paramref name="lineShape"/> and scales.
        /// </summary>
        /// <param name="lineShape">Desired line shape.</param>
        /// <param name="scaleFill">Scale value used for drawing elements that are filled.</param>
        /// <param name="scaleStroke">Scale value used for drawing elements that are just outlines.</param>
        /// <returns>
        /// New <see cref="CustomLineCap"/> configured to produce <paramref name="lineShape"/>.
        /// </returns>
        private static CustomLineCap GetLineShapeCap(LineShape lineShape, float scaleFill, float scaleStroke)
        {
            float baseInset = 0.0f;
            bool fill = true;
            using (var path = new GraphicsPath())
            {
                switch (lineShape)
                {
                    case LineShape.Arrow:
                        {
                            var size = new SizeF(2.75f * scaleStroke, 4.0f * scaleStroke);
                            var halfW = size.Width * 0.5f;
                            var height = size.Height;
                            fill = false;
                            path.AddLines(new PointF[]
                            {
                            new PointF(-halfW, -height), new PointF(0, 0), new PointF(halfW, -height)
                            });
                        }
                        break;
                    case LineShape.SolidArrow:
                        {
                            var size = new SizeF(4.25f * scaleFill, 5.57f * scaleFill);
                            var halfW = size.Width * 0.5f;
                            var height75 = size.Height * 0.73f;
                            var height25 = size.Height * 0.27f;
                            baseInset = height75;
                            path.AddLines(new PointF[]
                            {
                                new PointF(0, -height75),
                                new PointF(halfW, -height75),
                                new PointF(0, height25),
                                new PointF(-halfW, -height75),
                                new PointF(0, -height75),
                            });
                        }
                        break;
                    case LineShape.Tail:
                        {
                            var size = new SizeF(2.10f * scaleStroke, 4.0f * scaleStroke);
                            var halfW = size.Width * 0.5f;
                            var height = size.Height;
                            fill = false;
                            path.AddLines(new[] {
                                new PointF(-halfW, 0),
                                new PointF(0, -height * 0.5f),
                                new PointF( halfW, 0)
                            });
                            path.StartFigure();
                            path.AddLines(new[] {
                                new PointF(-halfW, height * 0.5f),
                                new PointF(0, 0),
                                new PointF( halfW, height * 0.5f)
                            });
                        }
                        break;
                    case LineShape.SolidTail:
                        {
                            var size = new SizeF(3.02f * scaleFill, 4.65f * scaleFill);
                            var halfW = size.Width * 0.5f;
                            var halfH = size.Height * 0.5f;
                            var height38 = size.Height * 0.38f;
                            var height62 = size.Height * 0.62f;
                            path.AddPolygon(new[] {
                                new PointF(0, -height38),
                                new PointF(halfW, 0),
                                new PointF( halfW, height62),
                                new PointF(0, height62 - height38),
                                new PointF(-halfW, height62),
                                new PointF(-halfW, 0),
                                new PointF(0, -height38),
                            });
                        }
                        break;
                    case LineShape.Circle:
                        {
                            var size = new SizeF(2.4f * scaleFill, 2.4f * scaleFill);
                            path.AddEllipse(-size.Width * 0.5f, -size.Height * 0.5f, size.Width, size.Height);
                        }
                        break;
                    case LineShape.Square:
                        {
                            var size = new SizeF(3.0f * scaleFill, 3.0f * scaleFill);
                            path.AddRectangle(new RectangleF(-size.Width * 0.5f, -size.Height * 0.5f, size.Width, size.Height));
                        }
                        break;
                    default:
                        break;
                }
                var result = new CustomLineCap(fill ? path : null, fill ? null : path, LineCap.Flat, baseInset);
                result.SetStrokeCaps(LineCap.Round, LineCap.Round);
                result.StrokeJoin = LineJoin.Round;
                return result;
            }
        }

        private void RenderBasicPolyline(Graphics g, SchBasicPolyline basicPolyline)
        {
            var penWidth = basicPolyline.LineWidth == 0 ? 1.0f : ScaleLineWidth(basicPolyline.LineWidth);

            using (var pen = CreatePen(basicPolyline.Color, penWidth))
            {
                pen.DashCap = DashCap.Round;
                switch (basicPolyline.LineStyle)
                {
                    case LineStyle.Dashed:
                        pen.DashStyle = DashStyle.Dash;
                        break;
                    case LineStyle.Dotted:
                        pen.DashStyle = DashStyle.Dot;
                        break;
                    default:
                        pen.DashStyle = DashStyle.Solid;
                        break;
                }

                if (basicPolyline is SchPolyline polyline)
                {
                    // this line cap size and scaling was accomplished after a lot of tinkering
                    // and working around limitations of the System.Drawing library that does
                    // things weird when the pen width is less than 2 pixels
                    var scaleStroke = Math.Max(1.0f, (polyline.LineShapeSize + 1));
                    var scaleFill = Math.Max(1.0f, (polyline.LineShapeSize + 1));

                    if (polyline.LineWidth == 0)
                    {
                        // if line width is 0 (hairline) then here we use 1 pixel as things go badly
                        // if the pen width is close to zero
                        scaleStroke *= ScaleLineWidth(LineWidth.Small);
                        scaleFill *= ScaleLineWidth(LineWidth.Small) * 0.5f;
                    }
                    else if (penWidth < 2.0f)
                    {
                        scaleStroke *= 1.0f;
                        scaleFill *= ScaleLineWidth(LineWidth.Small) * 0.5f;
                    }

                    if (polyline.StartLineShape != LineShape.None)
                    {
                        pen.StartCap = LineCap.Custom;
                        pen.CustomStartCap = GetLineShapeCap(polyline.StartLineShape, scaleFill, scaleStroke);
                    }

                    if (polyline.EndLineShape != LineShape.None)
                    {
                        pen.EndCap = LineCap.Custom;
                        pen.CustomEndCap = GetLineShapeCap(polyline.EndLineShape, scaleFill, scaleStroke);
                    }
                }

                var points = basicPolyline.Vertices.Select(loc => ScreenFromWorld(loc)).ToArray();
                g.DrawLines(pen, points);
            }
        }

        private void RenderPolygonPrimitive(Graphics g, SchPolygon polygon)
        {
            var penWidth = ScaleLineWidth(polygon.LineWidth);
            using (var brush = new SolidBrush(polygon.AreaColor))
            using (var pen = CreatePen(polygon.Color, penWidth))
            {
                var points = polygon.Vertices.Select(coordxy => ScreenFromWorld(coordxy)).ToArray();
                if (polygon.IsSolid)
                {
                    g.FillPolygon(brush, points);
                }
                g.DrawPolygon(pen, points);
            }
        }

        private void RenderEllipsePrimitive(Graphics g, SchEllipse ellipse)
        {
            var penWidth = ScaleLineWidth(ellipse.LineWidth);
            using (var pen = CreatePen(ellipse.Color, penWidth))
            {
                var rect = ScreenFromWorld(ellipse.CalculateBounds());
                if (ellipse.IsSolid)
                {
                    using (var brush = new SolidBrush(ellipse.AreaColor))
                    {
                        g.FillEllipse(brush, rect);
                    }
                }
                g.DrawEllipse(pen, rect);
            }
        }

        private void RenderPieChartPrimitive(Graphics g, SchPie pieChart)
        {
            var penWidth = ScaleLineWidth(pieChart.LineWidth);
            using (var brush = new SolidBrush(pieChart.AreaColor))
            using (var pen = CreatePen(pieChart.Color, penWidth))
            {
                var rect = ScreenFromWorld(pieChart.CalculateBounds());
                var startAngle = (float)-pieChart.StartAngle; // GDI+ uses clockwise angles and Altium counter-clockwise
                var sweepAngle = -(float)Utils.NormalizeAngle(pieChart.EndAngle - pieChart.StartAngle);
                if (pieChart.IsSolid)
                {
                    g.FillPie(brush, Rectangle.Round(rect), startAngle, sweepAngle);
                }
                g.DrawPie(pen, Rectangle.Round(rect), startAngle, sweepAngle);
            }
        }

        private void RenderRoundedRectPrimitive(Graphics g, SchRoundedRectangle roundedRect)
        {
            var rect = ScreenFromWorld(roundedRect.CalculateBounds());
            var radiusX = ScaleCoord(roundedRect.CornerXRadius);
            var radiusY = ScaleCoord(roundedRect.CornerYRadius);

            if (roundedRect.IsSolid)
            {
                using (var brush = new SolidBrush(roundedRect.AreaColor))
                {
                    DrawingUtils.FillRoundedRect(g, brush, rect, radiusX, radiusY);
                }
            }

            var penWidth = ScaleLineWidth(roundedRect.LineWidth);
            using (var pen = CreatePen(roundedRect.Color, penWidth))
            {
                DrawingUtils.DrawRoundedRect(g, pen, rect, radiusX, radiusY);
            }
        }

        private void RenderArcPrimitive(Graphics g, SchArc arc)
        {
            var penWidth = ScaleLineWidth(arc.LineWidth);
            using (var pen = CreatePen(arc.Color, penWidth))
            {
                var rect = ScreenFromWorld(arc.CalculateBounds());
                var startAngle = (float)-arc.StartAngle; // GDI+ uses clockwise angles and Altium counter-clockwise
                var isCircle = Math.Abs(Utils.NormalizeAngle(arc.EndAngle) - Utils.NormalizeAngle(arc.StartAngle)) <= 1e-5;
                var sweepAngle = isCircle ? 360.0f : -(float)Utils.NormalizeAngle(arc.EndAngle - arc.StartAngle);
                g.DrawArc(pen, Rectangle.Round(rect), startAngle, sweepAngle);
            }
        }

        private void RenderLinePrimitive(Graphics g, SchLine line)
        {
            var penWidth = ScaleLineWidth(line.LineWidth);
            using (var pen = CreatePen(line.Color, penWidth))
            {
                var point1 = ScreenFromWorld(line.Location);
                var point2 = ScreenFromWorld(line.Corner);
                g.DrawLine(pen, point1, point2);
            }
        }

        private void RenderRectanglePrimitive(Graphics g, SchRectangle rectangle)
        {
            var rect = ScreenFromWorld(rectangle.CalculateBounds());
            if (rectangle.IsSolid)
            {
                using (var brush = new SolidBrush(rectangle.AreaColor))
                {
                    DrawingUtils.FillRectangle(g, brush, rect);
                }
            }

            var penWidth = ScaleLineWidth(rectangle.LineWidth);
            using (var pen = CreatePen(rectangle.Color, penWidth, lineJoin: LineJoin.Miter))
            {
                DrawingUtils.DrawRectangle(g, pen, rect);
            }
        }

        private void RenderPowerPortPrimitive(Graphics g, SchPowerObject powerPort)
        {
            var location = ScreenFromWorld(powerPort.Location.X, powerPort.Location.Y);

            var assetName = $"PowerPort_{powerPort.Style}";
            var symbolPrimitives = GetAsset(assetName);

            if (symbolPrimitives == null) return;

            var netNameAnchor = symbolPrimitives?.OfType<SchLabel>()
                .SingleOrDefault(t => t.Text == "=NET");

            float angle = 0.0f;
            if (powerPort.Orientation.HasFlag(TextOrientations.Rotated))
            {
                angle -= 90.0f;
            }

            if (powerPort.Orientation.HasFlag(TextOrientations.Flipped))
            {
                angle -= 180.0f;
            }

            RenderPrimitivesAt(g, false, powerPort.Location, angle, symbolPrimitives.Where(p => p != netNameAnchor), powerPort.Color);

            if (powerPort.ShowNetName && netNameAnchor != null)
            {
                g.TranslateTransform(location.X, location.Y);

                var netNameLocation = ScalePoint(netNameAnchor.Location);
                if (powerPort.Orientation.HasFlag(TextOrientations.Rotated))
                {
                    netNameLocation = new PointF(netNameLocation.Y, -netNameLocation.X);
                }

                if (powerPort.Orientation.HasFlag(TextOrientations.Flipped))
                {
                    netNameLocation = new PointF(-netNameLocation.X, -netNameLocation.Y);
                }

                using (var brush = new SolidBrush(powerPort.Color))
                using (var font = CreateFont(powerPort.FontId))
                {
                    var justification = DrawingUtils.RotateAnchorJustification(powerPort.Orientation, netNameAnchor.Justification);
                    StringAlignment horizontalAlignment, verticalAlignment;
                    switch (justification.Horizontal)
                    {
                        case TextJustification.HorizontalValue.Center:
                            horizontalAlignment = StringAlignment.Center;
                            break;
                        case TextJustification.HorizontalValue.Right:
                            horizontalAlignment = StringAlignment.Far;
                            break;
                        case TextJustification.HorizontalValue.Left:
                        default:
                            horizontalAlignment = StringAlignment.Near;
                            break;
                    }
                    switch (justification.Vertical)
                    {
                        case TextJustification.VerticalValue.Middle:
                            verticalAlignment = StringAlignment.Center;
                            break;
                        case TextJustification.VerticalValue.Bottom:
                            verticalAlignment = StringAlignment.Far;
                            break;
                        case TextJustification.VerticalValue.Top:
                        default:
                            verticalAlignment = StringAlignment.Near;
                            break;
                    }

                    var displayText = ProcessStringIndirection(powerPort, powerPort.Text);

                    DrawingUtils.DrawString(g, displayText, font, brush,
                            netNameLocation.X, netNameLocation.Y,
                            StringAlignmentKind.Extent, horizontalAlignment, verticalAlignment);

                    var screenBounds = CalculateScreenBounds(g,
                        DrawingUtils.CalculateTextExtent(g, displayText, font,
                            netNameLocation.X, netNameLocation.Y,
                            StringAlignmentKind.Extent, horizontalAlignment, verticalAlignment));
                    PrimitiveScreenBounds[powerPort] = screenBounds;
                }
            }
        }

        private void RenderTextFramePrimitive(Graphics g, SchTextFrame textFrame)
        {
            var penWidth = ScaleLineWidth(textFrame.LineWidth);
            var rect = ScreenFromWorld(textFrame.CalculateBounds());

            if (textFrame.IsSolid)
            {
                using (var brush = new SolidBrush(textFrame.AreaColor))
                {
                    DrawingUtils.FillRectangle(g, brush, rect);
                }
            }

            if (textFrame.ShowBorder)
            {
                using (var pen = CreatePen(textFrame.Color, penWidth, lineJoin: LineJoin.Miter))
                {
                    DrawingUtils.DrawRectangle(g, pen, rect);
                }
                // reduce text area according to the penWidth
                rect.Inflate(-penWidth, -penWidth);
            }

            using (var brush = new SolidBrush(textFrame.TextColor))
            using (var font = CreateFont(textFrame.FontId))
            {
                DrawingUtils.DrawString(g, textFrame.Text, font, brush, rect, StringAlignment.Near, StringAlignment.Near, textFrame.ClipToRect, textFrame.WordWrap);
            }
        }

        private void RenderJunctionPrimitive(Graphics g, SchJunction junction)
        {
            var color = junction.IsManualJunction ? junction.Color : Color.Navy;
            using (var brush = new SolidBrush(color))
            {
                var location = ScreenFromWorld(junction.Location);
                var radius = ScalePixelLength(2);
                g.FillEllipse(brush, location.X - radius, location.Y - radius, 2 * radius, 2 * radius);
            }
        }

        private void RenderImagePrimitive(Graphics g, SchImage image)
        {
            var rect = ScreenFromWorld(image.CalculateBounds());

            g.DrawImage(image.Image, rect);

            if (image.IsSolid)
            {
                var penWidth = ScaleLineWidth(image.LineWidth);
                using (var pen = CreatePen(image.Color, penWidth, lineJoin: LineJoin.Miter))
                {
                    DrawingUtils.DrawRectangle(g, pen, rect);
                }
            }
        }
    }
}
