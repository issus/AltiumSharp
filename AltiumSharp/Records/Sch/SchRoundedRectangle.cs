using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchRoundedRectangle : SchRectangle
    {
        public int CornerXRadius { get; internal set; }
        public int CornerYRadius { get; internal set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            CornerXRadius = p["CORNERXRADIUS"].AsIntOrDefault();
            CornerYRadius = p["CORNERYRADIUS"].AsIntOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("CORNERXRADIUS", CornerXRadius);
            p.Add("CORNERYRADIUS", CornerYRadius);
        }
    }
}
