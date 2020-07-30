using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum PcbTextFont { Stroke, TrueType, BarCode }

    public enum PcbTextJustification
    {
        BottomRight = 1, MiddleRight, TopRight,
        BottomCenter = 4, MiddleCenter, TopCenter,
        BottomLeft = 7, MiddleLeft, TopLeft
    }

    public class PcbText : PcbRectangularPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo(Text, null, null);

        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Text;
        public bool Mirrored { get; internal set; }
        public PcbTextFont Font { get; internal set; }
        public bool FontBold { get; internal set; }
        public bool FontItalic { get; internal set; }
        public string FontName { get; internal set; }
        public Coord BarcodeLRMargin { get; internal set; }
        public Coord BarcodeTBMargin { get; internal set; }
        public bool FontInverted { get; internal set; }
        public Coord FontInvertedBorder { get; internal set; }
        public bool FontInvertedRect { get; internal set; }
        public Coord FontInvertedRectWidth { get; internal set; }
        public Coord FontInvertedRectHeight { get; internal set; }
        public PcbTextJustification FontInvertedRectJustification { get; internal set; }
        public Coord FontInvertedRectTextOffset { get; internal set; }
        public string Text { get; internal set; }

        internal CoordRect CalculateRect(bool useAbsolutePosition)
        {
            var w = (Font == PcbTextFont.Stroke) ? (Text.Length * Height * 12) / 13 : (Text.Length * Height / 2);
            var h = Height;
            var x = Mirrored ? -w : 0;
            var y = 0;
            if (useAbsolutePosition)
            {
                x += Corner1.X;
                y += Corner1.Y;
            }
            return new CoordRect(x, y, w, h);
        }

        public override CoordRect CalculateBounds()
        {
            return CoordRect.FromRotatedRect(CalculateRect(true), Corner1, Rotation);
        }
    }
}
