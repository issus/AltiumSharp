using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class Record41 : Record34
    {
        public int ParamType { get; internal set; }
        public string Description { get; internal set; }
        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X, Location.Y, 1, 1);

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            ParamType = p["PARAMTYPE"].AsIntOrDefault();
            Description = p["DESCRIPTION"].AsStringOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("PARAMTYPE", ParamType);
            p.Add("DESCRIPTION", Description);
        }
    }
}
