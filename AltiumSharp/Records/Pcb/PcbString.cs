using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum PcbStringFont { Stroke, TrueType, BarCode }

    public enum PcbStringJustification
    {
        BottomRight = 1, MiddleRight, TopRight,
        BottomCenter = 4, MiddleCenter, TopCenter,
        BottomLeft = 7, MiddleLeft, TopLeft
    }

    public class PcbString : PcbPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo(Text, null, null);

        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Text;
        public CoordPoint Location { get; internal set; }
        public Coord Width { get; internal set; }
        public Coord Height { get; internal set; }
        public double Rotation { get; internal set; }
        public bool Mirrored { get; internal set; }
        public PcbStringFont Font { get; internal set; }
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
        public PcbStringJustification FontInvertedRectJustification { get; internal set; }
        public Coord FontInvertedRectTextOffset { get; internal set; }
        public string Text { get; internal set; }

        internal CoordRect CalculateRect(bool useAbsolutePosition)
        {
            var w = (Font == PcbStringFont.Stroke) ? (Text.Length * Height * 12) / 13 : (Text.Length * Height / 2);
            var h = Height;
            var x = Mirrored ? -w : 0;
            var y = 0;
            if (useAbsolutePosition)
            {
                x += Location.X;
                y += Location.Y;
            }
            return new CoordRect(x, y, w, h);
        }

        public override CoordRect CalculateBounds()
        {
            return CoordRect.FromRotatedRect(CalculateRect(true), Location, Rotation);
        }
    }
}
