using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class PcbArc : PcbPrimitive
    {
        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Arc;
        public CoordPoint Location { get; internal set; }
        public Coord Radius { get; internal set; }
        public double StartAngle { get; internal set; }
        public double EndAngle { get; internal set; }
        public Coord Width { get; internal set; }

        public override CoordRect CalculateBounds()
        {
            return new CoordRect(Location.X - Radius, Location.Y - Radius, Radius * 2, Radius * 2);
        }
    }
}
