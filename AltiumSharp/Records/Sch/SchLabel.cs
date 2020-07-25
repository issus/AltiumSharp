using System;
using System.Drawing;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    [Flags]
    public enum TextOrientations
    {
        Rotated = 1, Flipped = 2
    }

    public class SchLabel : SchGraphicalObject
    {
        public int FontId { get; internal set; }
        public string Text { get; internal set; }
        public bool IsMirrored { get; internal set; }
        public bool IsHidden { get; internal set; }

        internal virtual string DisplayText => Text ?? "";
        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X, Location.Y, 1, 1);
        public override bool IsVisible => base.IsVisible && !IsHidden;

        public SchLabel()
        {
            Record = 4;
            IndexInSheet = -1;
            OwnerPartId = -1;
            FontId = 1;
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            FontId = p["FONTID"].AsIntOrDefault();
            Text = p["TEXT"].AsStringOrDefault();
            IsMirrored = p["ISMIRRORED"].AsBool();
            IsHidden = p["ISHIDDEN"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("ORIENTATION", (int)Orientation);
            p.Add("FONTID", FontId);
            p.Add("TEXT", Text);
            p.Add("ISMIRRORED", IsMirrored);
            p.Add("ISHIDDEN", IsHidden);
        }
    }
}
