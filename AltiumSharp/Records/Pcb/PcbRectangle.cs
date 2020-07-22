using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class PcbRectangle : PcbPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo("", Width, Height);

        public CoordPoint Corner1 { get; internal set; }
        public CoordPoint Corner2 { get; internal set; }
        public double Rotation { get; internal set; }
        public Coord Width => Corner2.X - Corner1.X;
        public Coord Height => Corner2.Y - Corner1.Y;

        public override CoordRect CalculateBounds()
        {
            return CoordRect.FromRotatedRect(new CoordRect(Corner1, Corner2), Corner1, Rotation);
        }
    }
}
