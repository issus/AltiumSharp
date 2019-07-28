using System;
using System.Drawing;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SymbolRecord : SchPrimitive
    {
        public int Symbol { get; internal set; }
        public CoordPoint Location { get; internal set; }
        public int ScaleFactor { get; internal set; }
        public Color Color { get; internal set; }
        
        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Symbol = p["SYMBOL"].AsIntOrDefault();
            Location = new CoordPoint(
                Utils.DxpFracToCoord(p["LOCATION.X"].AsIntOrDefault(), p["LOCATION.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["LOCATION.Y"].AsIntOrDefault(), p["LOCATION.Y_FRAC"].AsIntOrDefault()));
            ScaleFactor = p["SCALEFACTOR"].AsIntOrDefault();
            Color = p["COLOR"].AsColorOrDefault();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("SYMBOL", Symbol);
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
            p.Add("SCALEFACTOR", ScaleFactor);
            p.Add("COLOR", Color);
        }
    }
}
