using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Drawing
{
    /// <summary>
    /// Base class that implements common rendering functionality.
    /// </summary>
    public abstract class Renderer : IDisposable
    {
        /// <summary>
        /// Value used for scaling coordinates for showing to the screen.
        /// </summary>
        public double Scale { get; set; }

        /// <summary>
        /// Center point for the current view.
        /// </summary>
        public CoordPoint Center { get; set; }

        /// <summary>
        /// Size in pixels of the screen or output device.
        /// </summary>
        public SizeF ScreenSize { get; set; }

        /// <summary>
        /// Value used for converting coordinates at zoom 100% to pixels.
        /// This defaults to DPI_DXP_UNIT (100) so 1 pixel = 1 Dxp unit,
        /// but this can be set to the real DPI value of the device for
        /// accurate outputs, like printing a PCB in its actual size.
        /// </summary>
        public double ScreenDpi { get; set; }

        /// <summary>
        /// Component instance to be rendered.
        /// </summary>
        public IContainer Component { get; set; }

        /// <summary>
        /// List of primitives of the <see cref="Component"/> to be highlighted as selected.
        /// </summary>
        public IEnumerable<Primitive> SelectedPrimitives { get; set; }

        /// <summary>
        /// Sets the zoom using a 1 pixel to 1 Dxp unit value
        /// </summary>
        public double Zoom
        {
            get => CalculateZoomFromScale(Scale);
            set => Scale = CalculateScaleFromZoom(value);
        }

        /// <summary>
        /// Allows primitive rendering to override the default bounds using
        /// bounds calculated when drawing, which is useful for text strings.
        /// </summary>
        protected Dictionary<Primitive, RectangleF> PrimitiveScreenBounds { get; } = new Dictionary<Primitive, RectangleF>();

        /// <summary>
        /// Default style for drawing line caps.
        /// </summary>
        protected LineCap DefaultLineCap { get; set; } = LineCap.Round;

        /// <summary>
        /// Default style for drawing line joins.
        /// </summary>
        protected LineJoin DefaultLineJoin { get; set; } = LineJoin.Round;

        /// <summary>
        /// Used for storing local fonts.
        /// </summary>
        private PrivateFontCollection _fonts = new PrivateFontCollection();

        /// <summary>
        /// Font to be used as stroke.
        /// </summary>
        protected FontFamily StrokeFontFamily { get; private set; }

        /// <summary>
        /// Defines the background color to use.
        /// <para>
        /// Use <see cref="Color.Transparent"/> for a transparent background.
        /// </para>
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.White;

        /// <summary>
        /// Color of the foreground used for the selected primitives highlight box.
        /// </summary>
        public Color SelectionColor { get; set; } = Color.FromArgb(200, Color.Lime);

        /// <summary>
        /// Color of the background for the selected primitives highlight box.
        /// </summary>
        public Color SelectionColorBg { get; set; } = Color.FromArgb(200, Color.White);

        /// <summary>
        /// Color to use for pens instead of the requested color in
        /// <seealso cref="CreatePen(in Color, float, LineCap?, LineJoin?)"/>.
        /// </summary>
        protected Color? PenColorOverride { get; set; }

        /// <summary>
        /// Altium Schematic Viewer draws 100 pixels per inch,
        /// which makes a number of 1 in the Dxp format equal to 1 pixel.
        /// </summary>
        private const double DPI_DXP_UNIT = 100.0;

        public Renderer()
        {
            ScreenDpi = DPI_DXP_UNIT;
            if (System.IO.File.Exists("font.ttf"))
            {
                _fonts.AddFontFile("font.ttf");
                StrokeFontFamily = _fonts.Families[0];
            }
            else
            {
                StrokeFontFamily = new FontFamily("Courier New");
            }
        }

        /// <summary>
        /// Creates a pen using the given parameters or when null the predefined defaults.
        /// </summary>
        /// <param name="color">Pen color.</param>
        /// <param name="width">Pen width.</param>
        /// <param name="lineCap">Line caps to use.</param>
        /// <param name="lineJoin">Line join to use.</param>
        /// <returns></returns>
        protected Pen CreatePen(in Color color, float width, LineCap? lineCap = null, LineJoin? lineJoin = null)
        {
            var pen = new Pen(PenColorOverride ?? color, width);
            pen.SetLineCap(lineCap ?? DefaultLineCap, lineCap ?? DefaultLineCap, DashCap.Flat);
            pen.LineJoin = lineJoin ?? DefaultLineJoin;
            return pen;
        }

        /// <summary>
        /// Calculates the scale value for a given zoom, where zoom of 1.0 means a 1:1 size.
        /// </summary>
        /// <param name="zoom">Zoom value to be used for calculating the scale.</param>
        /// <returns>Scale value that's equivalent to the given <paramref name="zoom"/>.</returns>
        protected double CalculateScaleFromZoom(double zoom) =>
            (ScreenDpi / Coord.OneInch) * zoom;

        /// <summary>
        /// Calculates the zoom value for a given scale.
        /// </summary>
        /// <param name="scale">Scale value to be used for calculating the zoom.</param>
        /// <returns>Zoom value that's equivalent to the given <paramref name="scale"/>.</returns>
        protected double CalculateZoomFromScale(double scale) =>
            (Coord.OneInch / ScreenDpi) * scale;

        /// <summary>
        /// Scales a pixel length for 1.0 zoom given the current scale.
        /// </summary>
        /// <param name="value">
        /// A length in pixels for when zoom is 1.0 to be scaled relative to the current
        /// equivalent zoom value.</param>
        /// <returns>Scaled pixel size.</returns>
        public float ScalePixelLength(in double value) =>
            (float)(Scale / CalculateScaleFromZoom(1.0) * value);

        /// <summary>
        /// Scales a coord value to the current scale.
        /// </summary>
        /// <param name="coord">Coord in internal units.</param>
        /// <returns>Scaled length.</returns>
        public float ScaleCoord(in Coord coord) =>
            (float)(coord * Scale);

        /// <summary>
        /// Scales a coord pair to the current scale.
        /// <seealso cref="ScaleCoord(in Coord)"/>
        /// </summary>
        /// <param name="point">Point to be scaled.</param>
        /// <returns>Returns the scaled point coordinates.</returns>
        public PointF ScalePoint(in CoordPoint point) =>
            new PointF(ScaleCoord(point.X), ScaleCoord(point.Y));

        /// <summary>
        /// Scales a rectangle to the current scale.
        /// <seealso cref="ScaleCoord(in Coord)"/>
        /// </summary>
        /// <param name="rect">Values to be scaled.</param>
        /// <returns>Scaled rectangle as <see cref="RectangleF"/>.</returns>
        public RectangleF ScaleRect(in CoordRect rect) =>
            new RectangleF(ScaleCoord(rect.Location1.X), ScaleCoord(-rect.Location1.Y - rect.Height),
                           ScaleCoord(rect.Width), ScaleCoord(rect.Height));

        /// <summary>
        /// Converts from world to screen or device coordinates.
        /// </summary>
        /// <param name="x">X world coordinate value.</param>
        /// <param name="y">Y world coordinate value.</param>
        /// <returns>Screen point for the given world coordinates.</returns>
        public PointF ScreenFromWorld(in Coord x, in Coord y) =>
            ScreenFromWorld(new CoordPoint(x, y));

        public PointF ScreenFromWorld(in CoordPoint coord)
        {
            var point = new PointF((float)((coord.X - Center.X) * Scale),
                                   (float)((Center.Y - coord.Y) * Scale));
            // we round the half ScreenSize so pixels at integer Dxp coordinates are drawn crisp with anti-aliasing
            point.X += (int)(ScreenSize.Width * 0.5f);
            point.Y += (int)(ScreenSize.Height * 0.5f);
            return point;
        }

        /// <summary>
        /// Converts rectangle from world to screen or device coordinates.
        /// </summary>
        /// <param name="rect">Rectangle in world coordinates.</param>
        /// <returns>Rectangle for the given world coordinates.</returns>
        public RectangleF ScreenFromWorld(in CoordRect rect)
        {
            var location = ScreenFromWorld(rect.Location1);
            var width = ScaleCoord(rect.Width);
            var height = ScaleCoord(rect.Height);
            return new RectangleF(location.X, location.Y - height, width, height);
        }

        /// <summary>
        /// Converts from screen or device coordinates to world coordinates.
        /// </summary>
        /// <param name="x">X screen coordinate value.</param>
        /// <param name="y">Y screen coordinate value.</param>
        /// <returns>Point for the given screen coordinates.</returns>
        public CoordPoint WorldFromScreen(in float x, in float y) =>
            WorldFromScreen(new PointF(x, y));

        /// <summary>
        /// Converts from screen or device coordinates to world coordinates.
        /// </summary>
        /// <param name="point">Point in screen coordinates.</param>
        /// <returns>Point for the given screen coordinates.</returns>
        public CoordPoint WorldFromScreen(PointF point)
        {
            point.X -= (int)(ScreenSize.Width * 0.5f);
            point.Y -= (int)(ScreenSize.Height * 0.5f);
            return new CoordPoint((int)(point.X / Scale + Center.X),
                                  (int)(Center.Y - point.Y / Scale));
        }

        /// <summary>
        /// Converts from screen or device coordinates to world coordinates.
        /// </summary>
        /// <param name="rect">Rectangle in screen coordinates.</param>
        /// <returns>Rectangle for the given screen coordinates.</returns>
        public CoordRect WorldFromScreen(in RectangleF rect)
        {
            var location1 = WorldFromScreen(rect.X, rect.Y);
            var location2 = WorldFromScreen(rect.Right, rect.Bottom);
            return new CoordRect(location1, location2);
        }

        /// <summary>
        /// Pans the view by the specified amount of screen coordinate pixels.
        /// </summary>
        /// <param name="screenDeltaX">Pixels to move the viewing area horizontally.</param>
        /// <param name="screenDeltaY">Pixels to move the viewing area vertically.</param>
        public void Pan(int screenDeltaX, int screenDeltaY)
        {
            Center = new CoordPoint(Center.X - (int)(screenDeltaX / Scale),
                                    Center.Y + (int)(screenDeltaY / Scale));
        }

        /// <summary>
        /// Tests the current <see cref="Component"/>'s primitives and returns those
        /// that screen position intersects the given coordinates.
        /// </summary>
        /// <param name="screenX">Screen or device X coordinate value.</param>
        /// <param name="screenY">Screen or device Y coordinate value.</param>
        /// <returns>Primitives that are under the given screen coordinates.</returns>
        public IEnumerable<Primitive> Pick(int screenX, int screenY)
        {
            var pickRegion = new RectangleF(screenX - 2, screenY - 2, 4, 4);
            return Component?.GetPrimitivesOfType<Primitive>()
                .Where(p => IsPrimitiveVisibleInScreen(p))
                .Where(p => CalculatePrimitiveScreenBounds(p).IntersectsWith(pickRegion));
        }

        /// <summary>
        /// Renders the current <see cref="Component"/> as an image.
        /// </summary>
        /// <param name="width">Width in pixels of the resulting image.</param>
        /// <param name="height">Height in pixels of the resulting image,</param>
        /// <param name="autoZoom">
        /// If true then the view is centered and scaled to match the <see cref="Component"/> bounds.
        /// </param>
        /// <param name="fastRendering">
        /// Use faster rendering instead of high quality rendering?
        /// </param>
        /// <returns>Image with the current <see cref="Component"/> visual representation.</returns>
        public Image RenderAsImage(int width, int height, bool autoZoom, bool fastRendering = false)
        {
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                Render(graphics, width, height, autoZoom, fastRendering);
            }
            return bitmap;
        }

        /// <summary>
        /// Calculates bounds of the current <see cref="Component"/> taking into consideration
        /// what primitives are actually drawable.
        /// </summary>
        /// <returns></returns>
        private CoordRect CalculateComponentBounds()
        {
            var drawablePrimitives = Component.GetPrimitivesOfType<Primitive>()
                .Where(p => p.IsVisible && DoIsPrimitiveDrawable(p));
            return CoordRect.Union(drawablePrimitives.Select(p => p.CalculateBounds()));
        }

        /// <summary>
        /// Renders the current <see cref="Component"/> to the given <paramref name="graphics"/>
        /// instance.
        /// </summary>
        /// <param name="graphics">Target <see cref="Graphics"/> where to draw.</param>
        /// <param name="width">Width in pixels of the resulting image.</param>
        /// <param name="height">Height in pixels of the resulting image,</param>
        /// <param name="autoZoom">
        /// If true then the view is centered and scaled to match the <see cref="Component"/> bounds.
        /// </param>
        /// <param name="fastRendering">
        /// Use faster rendering instead of high quality rendering?
        /// </param>
        public void Render(Graphics graphics, int width, int height, bool autoZoom, bool fastRendering = false)
        {
            if (Component == null) throw new InvalidOperationException("Footprint property cannot be null");
            if (graphics == null) throw new ArgumentException("Graphics cannot be null", nameof(graphics));

            ScreenSize = new SizeF(width, height);
            if (autoZoom)
            {
                var bounds = CalculateComponentBounds();
                Center = bounds.Center;
                Scale = Math.Min(ScreenSize.Width / bounds.Width, ScreenSize.Height / bounds.Height) * 0.95f;
            }

            PrimitiveScreenBounds.Clear(); // reset calculated primitive screen bounds

            graphics.Clear(BackgroundColor);

            DoRender(graphics, fastRendering);

            if (SelectedPrimitives?.Any() == true)
            {
                RenderSelection(graphics, true);
            }
        }

        /// <summary>
        /// Framework hook method for derived classes to implement their own rendering specifics.
        /// </summary>
        /// <param name="graphics">Where to draw.</param>
        /// <param name="fastRendering">
        /// Use faster rendering instead of high quality rendering?
        /// </param>
        protected abstract void DoRender(Graphics graphics, bool fastRendering);

        /// <summary>
        /// Calculates if a given <paramref name="primitive"/> is visible,
        /// according to it being drawable, and their screen bounds intersecting
        /// the screen bounds.
        /// </summary>
        /// <param name="primitive">
        /// Primitive instance to have its visibility tested.
        /// </param>
        /// <returns></returns>
        protected bool IsPrimitiveVisibleInScreen(Primitive primitive)
        {
            if (primitive == null) return false;

            var screenRect = new RectangleF(0, 0, ScreenSize.Width, ScreenSize.Height);
            if (primitive.IsVisible && DoIsPrimitiveDrawable(primitive))
            {
                var rect = ScreenFromWorld(primitive.CalculateBounds());
                if (rect.Width > 0 || rect.Height > 0)
                {
                    return screenRect.IntersectsWith(rect.Inflated(10, 10)); // inflate primitive screen rect to make sure it's not clipped when on the edge
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Framework hook method for derived classes to implement renderer specific
        /// primitive visibility.
        /// <para>
        /// For example, this is used for <see cref="SchLibRenderer"/> to only show the
        /// current selected <see cref="SchLibRenderer.Part"/> of the <see cref="Component"/>.
        /// </para>
        /// </summary>
        /// <param name="primitive">Primitive to have its visibility tested.</param>
        /// <returns></returns>
        protected virtual bool DoIsPrimitiveDrawable(Primitive primitive)
        {
            return true;
        }

        /// <summary>
        /// Renders a selection highlight rectangle around the relevant primitives
        /// in <see cref="SelectedPrimitives"/>.
        /// </summary>
        /// <param name="g">Where to draw.</param>
        /// <param name="individual">
        /// If true draws individual selection boxes for each selected primitive,
        /// otherwise draws a single selection over all highlighted items.
        /// </param>
        private void RenderSelection(Graphics g, bool individual)
        {
            var visiblePrimitives = SelectedPrimitives.Where(IsPrimitiveVisibleInScreen);
            var rects = visiblePrimitives.Select(CalculatePrimitiveScreenBounds).ToArray();
            if (rects.Length == 0) return;

            if (individual)
            {
                rects = rects.Select(b => b.Inflated(2, 2)).ToArray();
            }
            else
            {
                rects = new[] { rects.Aggregate(RectangleF.Union).Inflated(2, 2) };
            }

            var penFgColor = SelectionColor;
            var penBgColor = SelectionColorBg;
            using (var penFg = CreatePen(penFgColor, 1))
            using (var penBg = CreatePen(penBgColor, 1))
            {
                penFg.DashStyle = DashStyle.Dash;
                g.DrawRectangles(penBg, rects);
                g.DrawRectangles(penFg, rects);
            }
        }

        private RectangleF CalculatePrimitiveScreenBounds(Primitive primitive)
        {
            if (PrimitiveScreenBounds.TryGetValue(primitive, out var rect))
            {
                return rect;
            }
            else
            {
                return ScreenFromWorld(primitive.CalculateBounds());
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _fonts.Dispose();
                    StrokeFontFamily.Dispose();
                }
                disposedValue = true;
            }
        }

        ~Renderer()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
