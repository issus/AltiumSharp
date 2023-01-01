using System;
using System.Drawing;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchCircle : SchGraphicalObject
    {
        public Coord Radius { get; internal set; }
        public LineWidth LineWidth { get; internal set; }
        public bool IsSolid { get; internal set; }
        public bool Transparent { get; internal set; }

        public SchCircle() : base()
        {
            Radius = 10;
            Color = ColorTranslator.FromWin32(16711680);
            AreaColor = ColorTranslator.FromWin32(12632256);
            IsSolid = true;
        }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X - Radius, Location.Y - Radius, Radius * 2, Radius * 2);

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Radius = Utils.DxpFracToCoord(p["RADIUS"].AsIntOrDefault(), p["RADIUS_FRAC"].AsIntOrDefault());
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
                var (n, f) = Utils.CoordToDxpFrac(Radius);
                if (n != 0 || f != 0) p.Add("RADIUS", n);
                if (f != 0) p.Add("RADIUS" + "_FRAC", f);
            }
            p.Add("LINEWIDTH", LineWidth);
            p.MoveKeys("COLOR");
            p.Add("ISSOLID", IsSolid);
            p.Add("TRANSPARENT", Transparent);
        }
    }
}
