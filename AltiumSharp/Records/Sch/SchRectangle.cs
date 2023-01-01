using System;
using System.Drawing;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchRectangle : SchGraphicalObject
    {
        public override int Record => 14;
        public CoordPoint Corner { get; set; }
        public LineWidth LineWidth { get; set; }
        public bool IsSolid { get; set; }
        public bool Transparent { get; set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location, Corner);

        public SchRectangle() : base()
        {
            Corner = new CoordPoint(Utils.DxpFracToCoord(50, 0), Utils.DxpFracToCoord(50, 0));
            Color = ColorTranslator.FromWin32(128);
            AreaColor = ColorTranslator.FromWin32(11599871);
            IsSolid = true;
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Corner = new CoordPoint(
                Utils.DxpFracToCoord(p["CORNER.X"].AsIntOrDefault(), p["CORNER.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["CORNER.Y"].AsIntOrDefault(), p["CORNER.Y_FRAC"].AsIntOrDefault()));
            LineWidth = p["LINEWIDTH"].AsEnumOrDefault<LineWidth>();
            IsSolid = p["ISSOLID"].AsBool();
            Transparent = p["TRANSPARENT"].AsBool();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
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
            p.MoveKeys("COLOR");
            p.Add("ISSOLID", IsSolid);
            p.Add("TRANSPARENT", Transparent);
            p.Add("UNIQUEID", Utils.GenerateUniqueId());
        }
    }
}
