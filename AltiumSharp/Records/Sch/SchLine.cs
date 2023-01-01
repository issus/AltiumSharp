using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchLine : SchGraphicalObject
    {
        public override int Record => 13;
        public CoordPoint Corner { get; set; }
        public LineWidth LineWidth { get; set; }
        public LineStyle LineStyle { get; set; }
        
        public override CoordRect CalculateBounds() =>
            new CoordRect(Location, Corner);

        public SchLine() : base()
        {
            LineWidth = LineWidth.Small;
        }

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
                p.Add("CORNER.X", n);
                p.Add("CORNER.X_FRAC", f);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(Corner.Y);
                p.Add("CORNER.Y", n);
                p.Add("CORNER.Y_FRAC", f);
            }
            p.Add("LINEWIDTH", LineWidth);
            p.MoveKey("COLOR");
            p.Add("LINESTYLE", LineStyle);
        }
    }
}
