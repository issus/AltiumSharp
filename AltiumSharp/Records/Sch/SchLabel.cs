using System;
using System.Drawing;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchLabel : SchGraphicalObject
    {
        public override int Record => 4;
        public TextOrientations Orientation { get; set; }
        public TextJustification Justification { get; set; }
        public int FontId { get; set; }
        public string Text { get; set; }
        public bool IsMirrored { get; set; }
        public bool IsHidden { get; set; }

        internal virtual string DisplayText => Text ?? "";
        public override bool IsVisible => base.IsVisible && !IsHidden;

        public SchLabel() : base()
        {
            Color = ColorTranslator.FromWin32(8388608);
            FontId = 1;
            Text = "Text";
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Orientation = p["ORIENTATION"].AsEnumOrDefault<TextOrientations>();
            Justification = (TextJustification)p["JUSTIFICATION"].AsIntOrDefault();
            FontId = p["FONTID"].AsIntOrDefault();
            Text = p["TEXT"].AsStringOrDefault();
            IsMirrored = p["ISMIRRORED"].AsBool();
            IsHidden = p["ISHIDDEN"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            p.Add("ORIENTATION", Orientation);
            p.Add("JUSTIFICATION", (int)Justification);
            p.MoveKeys("COLOR");
            p.Add("FONTID", FontId);
            p.Add("ISHIDDEN", IsHidden);
            p.Add("TEXT", Text);
            p.Add("ISMIRRORED", IsMirrored);
        }
    }
}
