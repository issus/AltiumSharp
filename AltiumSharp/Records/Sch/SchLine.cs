using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchLine : SchGraphicalObject
    {
        public CoordPoint Corner { get; internal set; }
        public LineWidth LineWidth { get; internal set; }
        public LineStyle LineStyle { get; internal set; }
        
        public override CoordRect CalculateBounds() =>
            new CoordRect(Location, Corner);

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Corner = new CoordPoint(
                Utils.DxpFracToCoord(p["CORNER.X"].AsIntOrDefault(), p["CORNER.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["CORNER.Y"].AsIntOrDefault(), p["CORNER.Y_FRAC"].AsIntOrDefault()));
            LineWidth = p["LINEWIDTH"].AsEnumOrDefault<LineWidth>();
            LineStyle = p["LINESTYLE"].AsEnumOrDefault<LineStyle>();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            {
                var (n, f) = Utils.CoordToDxpFrac(Corner.X);
                if (n != 0 || f != 0) p.Add("CORNER.X", n);
                if (f != 0) p.Add("CORNER.X"+"_FRAC", f);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(Corner.Y);
                if (n != 0 || f != 0) p.Add("CORNER.Y", n);
                if (f != 0) p.Add("CORNER.Y"+"_FRAC", f);
            }
            p.Add("LINEWIDTH", LineWidth);
            p.Add("LINESTYLE", LineStyle);
        }
    }
}
