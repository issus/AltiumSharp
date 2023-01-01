using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchEllipticalArc : SchArc
    {
        public override int Record => 11;
        public Coord SecondaryRadius { get; set; }

        public SchEllipticalArc() : base()
        {
            Radius = Utils.DxpFracToCoord(10, 0);
        }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X - Radius, Location.Y - SecondaryRadius, Radius * 2, SecondaryRadius * 2);

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            SecondaryRadius = Utils.DxpFracToCoord(p["SECONDARYRADIUS"].AsIntOrDefault(), p["SECONDARYRADIUS_FRAC"].AsIntOrDefault());
          }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            {
                var (n, f) = Utils.CoordToDxpFrac(SecondaryRadius);
                p.Add("SECONDARYRADIUS", n);
                p.Add("SECONDARYRADIUS_FRAC", f);
            }
            p.MoveKeys("LINEWIDTH");
        }
    }
}
