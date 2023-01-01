using System;
using System.Collections.Generic;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class PcbTrack : PcbPrimitive
    {
        public override PcbPrimitiveDisplayInfo GetDisplayInfo() =>
            new PcbPrimitiveDisplayInfo("", Width, null);

        public override PcbPrimitiveObjectId ObjectId => PcbPrimitiveObjectId.Track;
        public CoordPoint Start { get; set; }
        public CoordPoint End { get; set; }
        public Coord Width { get; set; }

        public PcbTrack() : base()
        {
            Width = Coord.FromMils(10);
        }

        public override CoordRect CalculateBounds()
        {
            return new CoordRect(Start, End);
        }
    }

    /// <summary>
    /// Helper class used for creating a multi-vertex track in one go.
    /// This can be used for creating a complex track with multiple point without
    /// having to manually create multiple track lines that have matching start and
    /// end property values.
    /// When this is added to a component multiple PcbTrack primitives are created
    /// in its place instead.
    /// </summary>
    public class PcbMetaTrack : PcbUnknown
    {
        public IList<CoordPoint> Vertices { get; }
        public Coord Width { get; set; }

        public PcbMetaTrack() : base(PcbPrimitiveObjectId.None)
        {
            Vertices = new List<CoordPoint>();
            Width = Coord.FromMils(10);
        }

        public PcbMetaTrack(params CoordPoint[] vertices) : this()
        {
            Vertices = vertices;
        }

        public override CoordRect CalculateBounds() => CoordRect.Empty;

        public IEnumerable<Tuple<CoordPoint, CoordPoint>> Lines =>
            Vertices.Zip(Vertices.Skip(1), Tuple.Create);
    }
}
