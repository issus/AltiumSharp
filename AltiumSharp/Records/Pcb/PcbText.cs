using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public enum PcbTextKind { Stroke, TrueType, BarCode }

    public enum PcbTextStrokeFont { Default = 0, SansSerif = 1, Serif = 3 }

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
        public bool Mirrored { get; set; }
        public PcbTextKind TextKind { get; set; }
        public PcbTextStrokeFont StrokeFont { get; set; }
        public Coord StrokeWidth { get; set; }
        public bool FontBold { get; set; }
        public bool FontItalic { get; set; }
        public string FontName { get; set; }
        public Coord BarcodeLRMargin { get; set; }
        public Coord BarcodeTBMargin { get; set; }
        public bool FontInverted { get; set; }
        public Coord FontInvertedBorder { get; set; }
        public bool FontInvertedRect { get; set; }
        public Coord FontInvertedRectWidth { get; set; }
        public Coord FontInvertedRectHeight { get; set; }
        public PcbTextJustification FontInvertedRectJustification { get; set; }
        public Coord FontInvertedRectTextOffset { get; set; }
        public string Text { get; set; }
        internal int WideStringsIndex { get; set; }

        public PcbText() : base()
        {
            Text = "String";
            Height = Coord.FromMils(60);
            TextKind = PcbTextKind.Stroke;
            StrokeFont = PcbTextStrokeFont.SansSerif;
            StrokeWidth = Coord.FromMils(10);
            FontName = "Arial";
            FontInvertedBorder = Coord.FromMils(20);
            FontInvertedRectJustification = PcbTextJustification.MiddleCenter;
            FontInvertedRectTextOffset = Coord.FromMils(2);
        }

        internal CoordRect CalculateRect(bool useAbsolutePosition)
        {
            var w = (TextKind == PcbTextKind.Stroke) ? (Text.Length * Height * 12) / 13 : (Text.Length * Height / 2);
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
