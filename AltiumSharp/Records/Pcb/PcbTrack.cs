using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class PcbTrack : PcbPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo("", Width, null);

        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Track;
        public CoordPoint Start { get; internal set; }
        public CoordPoint End { get; internal set; }
        public Coord Width { get; internal set; }

        public override CoordRect CalculateBounds()
        {
            return new CoordRect(Start, End);
        }
    }
}
