using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchParameter : DesignatorLabelRecord
    {
        public int ParamType { get; internal set; }
        public string Description { get; internal set; }

        internal override string DisplayText => !string.IsNullOrEmpty(Description) ? Description : base.DisplayText;
        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X, Location.Y, 1, 1);

        public override bool IsVisible =>
            base.IsVisible && Name?.Equals("HiddenNetName", StringComparison.InvariantCultureIgnoreCase) == false;

        public SchParameter()
        {
            Record = 41;
            Location = new CoordPoint(-5, -15);
        }

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
