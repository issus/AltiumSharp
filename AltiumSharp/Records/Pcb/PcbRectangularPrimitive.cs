using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public abstract class PcbRectangularPrimitive : PcbPrimitive
    {
        public CoordPoint Corner1 { get; set; }
        public CoordPoint Corner2 { get; set; }
        public double Rotation { get; set; }

        public Coord Width
        {
            get => Corner2.X - Corner1.X;
            set => Corner2 = new CoordPoint(Corner1.X + value, Corner2.Y);
        }

        public Coord Height {
            get => Corner2.Y - Corner1.Y;
            set => Corner2 = new CoordPoint(Corner1.X, Corner1.Y + value);
        }

        public override CoordRect CalculateBounds()
        {
            return CoordRect.FromRotatedRect(new CoordRect(Corner1, Corner2), Corner1, Rotation);
        }
    }
}
