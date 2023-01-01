using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchRoundedRectangle : SchRectangle
    {
        public override int Record => 10;
        public Coord CornerXRadius { get; set; }
        public Coord CornerYRadius { get; set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            CornerXRadius = Utils.DxpFracToCoord(p["CORNERXRADIUS"].AsIntOrDefault(), p["CORNERXRADIUS_FRAC"].AsIntOrDefault());
            CornerYRadius = Utils.DxpFracToCoord(p["CORNERYRADIUS"].AsIntOrDefault(), p["CORNERYRADIUS_FRAC"].AsIntOrDefault());
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            {
                var (n, f) = Utils.CoordToDxpFrac(CornerXRadius);
                p.Add("CORNERXRADIUS", n);
                p.Add("CORNERXRADIUS_FRAC", f);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(CornerYRadius);
                p.Add("CORNERYRADIUS", n);
                p.Add("CORNERYRADIUS_FRAC", f);
            }
            p.MoveKeys("LINEWIDTH");
        }
    }
}
