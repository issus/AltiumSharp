using System.Collections.Generic;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class PcbRegion : PcbPrimitive
    {
        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Region;
        public ParameterCollection Parameters { get; internal set; } = new ParameterCollection();
        public List<CoordPoint> Outline { get; } = new List<CoordPoint>();

        public override CoordRect CalculateBounds()
        {
            return new CoordRect(
                new CoordPoint(Outline.Min(p => p.X), Outline.Min(p => p.Y)),
                new CoordPoint(Outline.Max(p => p.X), Outline.Max(p => p.Y)));
        }
    }
}
