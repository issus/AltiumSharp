using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class DesignatorLabelRecord : SchLabel // TODO: figure out what schematic API interface maps to this record
    {
        public string Name { get; internal set; }
        public int ReadOnlyState { get; internal set; }
        public string UniqueId { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X, Location.Y, 1, 1);

        public override bool IsVisible => base.IsVisible && OwnerIndex > 0;

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Name = p["NAME"].AsStringOrDefault();
            ReadOnlyState = p["READONLYSTATE"].AsIntOrDefault();
            UniqueId = p["UNIQUEID"].AsStringOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("NAME", Name);
            p.Add("READONLYSTATE", ReadOnlyState);
            p.Add("UNIQUEID", UniqueId);
        }
    }
}
