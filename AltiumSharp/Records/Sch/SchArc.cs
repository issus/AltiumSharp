using System;
using System.Drawing;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchArc : SchGraphicalObject
    {
        public override int Record => 12;
        public Coord Radius { get; set; }
        public LineWidth LineWidth { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X - Radius, Location.Y - Radius, Radius * 2, Radius * 2);

        public SchArc() : base()
        {
            LineWidth = LineWidth.Small;
            Radius = Utils.DxpFracToCoord(10, 0);
            EndAngle = 360.0;
            Color = ColorTranslator.FromWin32(16711680);
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Location = new CoordPoint(
                Utils.DxpFracToCoord(p["LOCATION.X"].AsIntOrDefault(), p["LOCATION.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["LOCATION.Y"].AsIntOrDefault(), p["LOCATION.Y_FRAC"].AsIntOrDefault()));
            Radius = Utils.DxpFracToCoord(p["RADIUS"].AsIntOrDefault(), p["RADIUS_FRAC"].AsIntOrDefault());
            LineWidth = p["LINEWIDTH"].AsEnumOrDefault<LineWidth>();
            StartAngle = p["STARTANGLE"].AsDoubleOrDefault();
            EndAngle = p["ENDANGLE"].AsDoubleOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            {
                var (n, f) = Utils.CoordToDxpFrac(Radius);
                p.Add("RADIUS", n);
                p.Add("RADIUS_FRAC", f);
            }
            p.Add("LINEWIDTH", LineWidth);
            p.Add("STARTANGLE", StartAngle);
            p.Add("ENDANGLE", EndAngle);
            p.MoveKeys("COLOR");
        }
    }
}
