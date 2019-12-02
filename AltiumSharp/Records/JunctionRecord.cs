using System;
using System.Drawing;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;

namespace AltiumSharp.Records
{
    internal class JunctionRecord : SchPrimitive
    {
        public CoordPoint Location { get; internal set; }
        public Color Color { get; internal set; }

        public bool IsManualJunction => IndexInSheet != -1;

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X - Utils.DxpToCoord(2), Location.Y - Utils.DxpToCoord(2), Utils.DxpToCoord(4), Utils.DxpToCoord(4));

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Location = new CoordPoint(
                Utils.DxpFracToCoord(p["LOCATION.X"].AsIntOrDefault(), p["LOCATION.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["LOCATION.Y"].AsIntOrDefault(), p["LOCATION.Y_FRAC"].AsIntOrDefault()));
            Color = p["COLOR"].AsColorOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            {
                var (n, f) = Utils.CoordToDxpFrac(Location.X);
                if (n != 0 || f != 0) p.Add("LOCATION.X", n);
                if (f != 0) p.Add("LOCATION.X" + "_FRAC", f);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(Location.Y);
                if (n != 0 || f != 0) p.Add("LOCATION.Y", n);
                if (f != 0) p.Add("LOCATION.Y" + "_FRAC", f);
            }
            p.Add("COLOR", Color);
        }
    }
}