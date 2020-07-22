using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum LineStyle
    {
        Solid, Dashed, Dotted
    }

    public abstract class SchBasicPolyline : SchPolygon
    {
        public LineStyle LineStyle { get; internal set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            LineStyle = p["LINESTYLE"].AsEnumOrDefault<LineStyle>();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("LINESTYLE", LineStyle);
        }
    }
}
