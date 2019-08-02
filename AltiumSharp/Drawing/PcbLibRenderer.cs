using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;

namespace AltiumSharp.Drawing
{
    public sealed class PcbLibRenderer : Renderer
    {
        public PcbLibRenderer() : base()
        {
        }

        protected override void DoRender(Graphics graphics, bool fastRendering = false)
        {
            if (graphics == null) throw new ArgumentNullException(nameof(graphics));

            var primitives = Component.GetPrimitivesOfType<PcbPrimitive>();
            var orderedPrimitives = primitives
                .Where(p => IsPrimitiveVisibleInScreen(p))
                .OrderByDescending(p => p.Layer.DrawPriority)
                .ThenByDescending(p => p.Layer);
            Debug.WriteLine($"Rendering {orderedPrimitives.Count()} / {primitives.Count()}");

            foreach (var primitive in orderedPrimitives)
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
                    case PcbArc arc:
                        RenderArcPrimitive(graphics, arc);
                        break;

                    case PcbPad pad:
                        RenderPadPrimitive(graphics, pad);
                        break;

                    case PcbTrack track:
                        RenderTrackPrimitive(graphics, track);
                        break;

                    case PcbString @string:
                        RenderStringPrimitive(graphics, @string);
                        break;

                    case PcbRectangle rectangle:
                        RenderRectanglePrimitive(graphics, rectangle);
                        break;

                    case PcbPolygon polygon:
                        RenderPolygonPrimitive(graphics, polygon);
                        break;
                }
                graphics.EndContainer(graphicsContainer);
            }
        }

        private void RenderArcPrimitive(Graphics g, PcbArc arc)
        {
            var penColor = arc.Layer.Color;
            var penWidth = ScaleCoord(arc.Width);
            using (var pen = CreatePen(penColor, penWidth))
            {
                var center = ScreenFromWorld(arc.Location.X, arc.Location.Y);
                var radius = ScaleCoord(arc.Radius);
                var startAngle = (float)-arc.StartAngle; // GDI+ uses clockwise angles and Altium counter-clockwise
                var sweepAngle = -(float)Utils.NormalizeAngle(arc.EndAngle - arc.StartAngle);
                g.DrawArc(pen,
                    center.X - radius, center.Y - radius, radius * 2.0f, radius * 2.0f,
                    startAngle, sweepAngle);
            }
        }

        private void RenderPadPrimitive(Graphics g, PcbPad pad)
        {
            var holeCenter = ScreenFromWorld(pad.Location.X, pad.Location.Y);

            g.TranslateTransform(holeCenter.X, holeCenter.Y);
            g.RotateTransform(-(float)pad.Rotation);

            DrawPad(g, pad, PcbPadPart.BottomSolder);
            DrawPad(g, pad, PcbPadPart.TopSolder);
            DrawPad(g, pad, PcbPadPart.BottomLayer);
            DrawPad(g, pad, PcbPadPart.TopLayer);

            if (pad.HasHole)
            {
                g.RotateTransform(-(float)pad.HoleRotation);
                using (var brush = new SolidBrush(Layer.GetLayerColor("PadHoleLayer")))
                {
                    var rect = ScaleRect(pad.CalculatePartRect(PcbPadPart.Hole, false));
                    switch (pad.HoleShape)
                    {
                        case PcbPadHoleShape.Round:
                            g.FillEllipse(brush, rect);
                            break;
                        case PcbPadHoleShape.Square:
                            DrawingUtils.FillRectangle(g, brush, rect);
                            break;
                        case PcbPadHoleShape.Slot:
                            DrawingUtils.FillRoundedRect(g, brush, rect, 100);
                            break;
                        default:
                            return;
                    }
                }
            }

            g.ResetTransform();

            const float MIN_FONT_DESCRIPTOR = 7;
            var fontSize = Math.Min(29f, ScaleCoord(pad.HasHole ? pad.HoleSize : pad.SizeTop.Y) * 0.5f);
            if (fontSize > MIN_FONT_DESCRIPTOR)
            {
                var fontColor = pad.HasHole ? Color.FromArgb(255, 227, 143) : Color.FromArgb(255, 181, 181);
                using (var brush = new SolidBrush(fontColor)) // TODO: add constant
                using (var font = new Font("Arial", fontSize))
                {
                    DrawingUtils.DrawString(g, pad.Designator, font, brush, holeCenter.X, holeCenter.Y,
                        StringAlignmentKind.Tight, StringAlignment.Center, StringAlignment.Center);
                }
            }
        }

        private void DrawPad(Graphics g, PcbPad pad, PcbPadPart padPart)
        {
            PcbPadShape shape;
            int cornerRadiusPercent;
            Color color;
            // setup parameters according to the current padPart
            switch (padPart)
            {
                case PcbPadPart.TopLayer:
                    shape = pad.ShapeTop;
                    cornerRadiusPercent = pad.CornerRadiusTop;
                    color = LayerMetadata.GetColor(pad.Layer);
                    break;
                case PcbPadPart.BottomLayer:
                    shape = pad.ShapeBottom;
                    cornerRadiusPercent = pad.CornerRadiusBot;
                    color = LayerMetadata.GetColor(pad.Layer);
                    break;
                case PcbPadPart.TopSolder:
                    shape = pad.ShapeTop;
                    cornerRadiusPercent = pad.CornerRadiusTop;
                    color = LayerMetadata.GetColor("TopSolder");
                    break;
                case PcbPadPart.BottomSolder:
                    shape = pad.ShapeBottom;
                    cornerRadiusPercent = pad.CornerRadiusBot;
                    color = LayerMetadata.GetColor("BottomSolder");
                    break;
                default:
                    return;
            }
            cornerRadiusPercent = cornerRadiusPercent > 0 ? cornerRadiusPercent : 100;

            var rect = ScaleRect(pad.CalculatePartRect(padPart, false));
            using (var brush = new SolidBrush(color))
            {
                switch (shape)
                {
                    case PcbPadShape.Round:
                        DrawingUtils.FillRoundedRect(g, brush, rect, cornerRadiusPercent);
                        break;
                    case PcbPadShape.Rectangular:
                        DrawingUtils.FillRectangle(g, brush, rect);
                        break;
                    case PcbPadShape.Octogonal:
                        DrawingUtils.FillOctagon(g, brush, rect);
                        break;
                    default:
                        return;
                }
            }
        }

        private void RenderTrackPrimitive(Graphics g, PcbTrack track)
        {
            var penColor = LayerMetadata.GetColor(track.Layer);
            var penWidth = ScaleCoord(track.Width);
            using (var pen = CreatePen(penColor, penWidth))
            {
                g.DrawLine(pen,
                    ScreenFromWorld(track.Start.X, track.Start.Y),
                    ScreenFromWorld(track.End.X, track.End.Y));
            }
        }

        private void RenderStringPrimitive(Graphics g, PcbString @string)
        {
            var location = ScreenFromWorld(@string.Location.X, @string.Location.Y);
            var color = LayerMetadata.GetColor(@string.Layer);
            var height = ScaleCoord(@string.Height);
            var fontStyle = (@string.FontItalic ? FontStyle.Italic : FontStyle.Regular) | (@string.FontBold ? FontStyle.Bold : FontStyle.Regular);
            float fontWidth;
            float fontSize;
            if (@string.Font == PcbStringFont.Stroke)
            {
                fontWidth = ScaleCoord(@string.Width);
                fontSize = DrawingUtils.CalculateFontSizeForBaseline(g, StrokeFontFamily, fontStyle, height + fontWidth);
            }
            else
            {
                fontWidth = 0;
                fontSize = DrawingUtils.CalculateFontSizeForHeight(g, StrokeFontFamily, fontStyle, height);
            }

            g.TranslateTransform(location.X, location.Y);
            g.RotateTransform(-(float)@string.Rotation);

            using (var brush = new SolidBrush(color))
            using (var fontFamily = new FontFamily(@string.FontName))
            using (var font = new Font(@string.Font == PcbStringFont.Stroke ? StrokeFontFamily : fontFamily, fontSize, fontStyle))
            {
                var size = g.MeasureString(@string.Text, font);
                if (size.Height < 5)
                {
                    using (var pen = new Pen(brush))
                    {
                        var rect = ScaleRect(@string.CalculateRect(false));
                        g.DrawRectangle(pen, rect.Left, rect.Top, rect.Width, rect.Height);
                    }
                }
                else
                {
                    if (@string.Mirrored) g.ScaleTransform(-1.0f, 1.0f);
                    DrawingUtils.DrawString(g, @string.Text, font, brush, 0, fontWidth * 0.5f,
                        StringAlignmentKind.Tight, StringAlignment.Near, StringAlignment.Far);
                }
            }

            g.ResetTransform();
        }

        private void RenderPolygonPrimitive(Graphics g, PcbPolygon polygon)
        {
            var brushColor = LayerMetadata.GetColor(polygon.Layer);
            using (var brush = new SolidBrush(brushColor))
            {
                var outline = polygon.Outline.Select(coordxy => ScreenFromWorld(coordxy)).ToArray();
                g.FillPolygon(brush, outline);
            }
        }

        private void RenderRectanglePrimitive(Graphics g, PcbRectangle rectangle)
        {
            var brushColor = LayerMetadata.GetColor(rectangle.Layer);
            using (var brush = new SolidBrush(brushColor))
            {
                var worldRect = new CoordRect(rectangle.Corner1, rectangle.Corner2);
                var worldPoints = worldRect.RotatedPoints(worldRect.Center, rectangle.Rotation);
                var screenPoints = worldPoints.Select(cp => ScreenFromWorld(cp)).ToArray();
                g.FillPolygon(brush, screenPoints);
            }
        }
    }
}
