using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchParameter : SchLabel
    {
        public override int Record => 41;
        public int ParamType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ReadOnlyState { get; set; }
        public string UniqueId { get; internal set; }

        internal override string DisplayText =>
            !string.IsNullOrEmpty(Description) ? Description : base.DisplayText;

        public override bool IsVisible =>
            base.IsVisible && OwnerIndex > 0 &&
            Name?.Equals("HiddenNetName", StringComparison.InvariantCultureIgnoreCase) == false;

        public SchParameter() : base()
        {
            IsNotAccesible = false;
            OwnerPartId = -1;
            Text = "*";
            UniqueId = Utils.GenerateUniqueId();
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            ParamType = p["PARAMTYPE"].AsIntOrDefault();
            Name = p["NAME"].AsStringOrDefault();
            Description = p["DESCRIPTION"].AsStringOrDefault();
            ReadOnlyState = p["READONLYSTATE"].AsIntOrDefault();
            UniqueId = p["UNIQUEID"].AsStringOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("PARAMTYPE", ParamType);
            p.Add("NAME", Name);
            p.Add("DESCRIPTION", Description);
            p.Add("READONLYSTATE", ReadOnlyState);
            p.Add("UNIQUEID", UniqueId);
        }
    }
}
