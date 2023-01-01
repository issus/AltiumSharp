using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class PcbArc : PcbPrimitive
    {
        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Arc;
        public CoordPoint Location { get; set; }
        public Coord Radius { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
        public Coord Width { get; set; }

        public PcbArc() : base()
        {
            Radius = Utils.DxpFracToCoord(10, 0);
            StartAngle = 0;
            EndAngle = 360;
        }

        public override CoordRect CalculateBounds()
        {
            return new CoordRect(Location.X - Radius, Location.Y - Radius, Radius * 2, Radius * 2);
        }
    }
}
