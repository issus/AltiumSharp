using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchParameter : SchLabel
    {
        public override int Record => 41;
        public ParameterType ParamType { get; set; }
        public string Name { get; set; }
        public bool ShowName { get; set; }
        public string Description { get; set; }
        public int ReadOnlyState { get; set; }
        public string UniqueId { get; set; }
        public string Value => string.IsNullOrEmpty(Description) ? base.DisplayText : Description;
        internal override string DisplayText => ShowName ? $"{Name}: {Value}" : Value;
        public override bool IsOfCurrentPart => true;
        public override bool IsOfCurrentDisplayMode => true;

        public override bool IsVisible =>
            base.IsVisible &&
            Name?.Equals("Comment", StringComparison.InvariantCultureIgnoreCase) == false &&
            Name?.Equals("HiddenNetName", StringComparison.InvariantCultureIgnoreCase) == false;

        public SchParameter() : base()
        {
            IsNotAccesible = false;
            OwnerPartId = -1;
            Text = "*";
            IsHidden = true;
            UniqueId = Utils.GenerateUniqueId();
        }

        public override string ToString()
        {
            return $"({Name}: {Value})";
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            ParamType = p["PARAMTYPE"].AsEnumOrDefault<ParameterType>();
            Name = p["NAME"].AsStringOrDefault();
            ShowName = p["SHOWNAME"].AsBool();
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
            p.Add("SHOWNAME", ShowName);
            p.Add("DESCRIPTION", Description);
            p.Add("READONLYSTATE", ReadOnlyState);
            p.Add("UNIQUEID", UniqueId);
        }
    }

    public enum ParameterType
    {
        String,
        Boolean,
        Integer,
        Float
    }
}
