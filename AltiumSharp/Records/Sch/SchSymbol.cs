using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchSymbol : SchGraphicalObject
    {
        public override int Record => 3;
        public int Symbol { get; internal set; }
        public bool IsMirrored { get; internal set; }
        public LineWidth LineWidth { get; internal set; }
        public int ScaleFactor { get; internal set; }
        
        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Symbol = p["SYMBOL"].AsIntOrDefault();
            IsMirrored = p["ISMIRRORED"].AsBool();
            LineWidth = p["LINEWIDTH"].AsEnumOrDefault<LineWidth>();
            ScaleFactor = p["SCALEFACTOR"].AsIntOrDefault();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            p.Add("SYMBOL", Symbol);
            p.Add("ISMIRRORED", IsMirrored);
            p.Add("LINEWIDTH", LineWidth);
            p.MoveKeys("LOCATION.X");
            p.Add("SCALEFACTOR", ScaleFactor);
            p.MoveKeys("COLOR");
        }
    }
}
