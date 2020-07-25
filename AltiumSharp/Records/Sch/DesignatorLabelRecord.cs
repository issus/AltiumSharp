using System;
using System.Drawing;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class DesignatorLabelRecord : SchLabel // TODO: figure out what schematic API interface maps to this record
    {
        public override int Record => 34;
        public string Name { get; internal set; }
        public int ReadOnlyState { get; internal set; }
        public string UniqueId { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X, Location.Y, 1, 1);

        public override bool IsVisible => base.IsVisible && OwnerIndex > 0;

        public DesignatorLabelRecord()
        {
            Location = new CoordPoint(-5, 5);
            Color = ColorTranslator.FromWin32(8388608);
            Text = "*";
            UniqueId = Utils.GenerateUniqueId();
        }

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
