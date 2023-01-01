using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchSymbol : SchGraphicalObject
    {
        public override int Record => 3;
        public int Symbol { get; set; }
        public bool IsMirrored { get; set; }
        public LineWidth LineWidth { get; set; }
        public int ScaleFactor { get; set; }
        
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
