using System.Collections.Generic;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class PcbPolygon : PcbPrimitive
    {
        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Region;
        public ParameterCollection Attributes { get; internal set; }
        public List<CoordPoint> Outline { get; } = new List<CoordPoint>();

        public override CoordRect CalculateBounds()
        {
            return new CoordRect(
                new CoordPoint(Outline.Min(p => p.X), Outline.Min(p => p.Y)),
                new CoordPoint(Outline.Max(p => p.X), Outline.Max(p => p.Y)));
        }
    }
}
