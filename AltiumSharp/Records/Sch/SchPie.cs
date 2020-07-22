using System;
using System.Drawing;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchPie : SchArc
    {
        public Color AreaColor { get; internal set; }
        public bool IsSolid { get; internal set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            AreaColor = p["AREACOLOR"].AsColorOrDefault();
            IsSolid = p["ISSOLID"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("AREACOLOR", AreaColor);
            p.Add("ISSOLID", IsSolid);
        }
    }
}
