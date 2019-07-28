using System;
using System.Drawing;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class RectangleRecord : SchPrimitive
    {
        public CoordPoint Location { get; internal set; }
        public CoordPoint Corner { get; internal set; }
        public LineWidth LineWidth { get; internal set; }
        public Color Color { get; internal set; }
        public Color AreaColor { get; internal set; }
        public bool IsSolid { get; internal set; }
        public bool Transparent { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location, Corner);

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Location = new CoordPoint(
                Utils.DxpFracToCoord(p["LOCATION.X"].AsIntOrDefault(), p["LOCATION.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["LOCATION.Y"].AsIntOrDefault(), p["LOCATION.Y_FRAC"].AsIntOrDefault()));
            Corner = new CoordPoint(
                Utils.DxpFracToCoord(p["CORNER.X"].AsIntOrDefault(), p["CORNER.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["CORNER.Y"].AsIntOrDefault(), p["CORNER.Y_FRAC"].AsIntOrDefault()));
            LineWidth = (LineWidth)p["LINEWIDTH"].AsIntOrDefault();
            Color = p["COLOR"].AsColorOrDefault();
            AreaColor = p["AREACOLOR"].AsColorOrDefault();
            IsSolid = p["ISSOLID"].AsBool();
            Transparent = p["TRANSPARENT"].AsBool();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            {
                var (n, f) = Utils.CoordToDxpFrac(Location.X);
                if (n != 0 || f != 0) p.Add("LOCATION.X", n);
                if (f != 0) p.Add("LOCATION.X"+"_FRAC", f);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(Location.Y);
                if (n != 0 || f != 0) p.Add("LOCATION.Y", n);
                if (f != 0) p.Add("LOCATION.Y"+"_FRAC", f);
            }
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
            p.Add("LINEWIDTH", (int)LineWidth);
            p.Add("COLOR", Color);
            p.Add("AREACOLOR", AreaColor);
            p.Add("ISSOLID", IsSolid);
            p.Add("TRANSPARENT", Transparent);
        }
    }
}
