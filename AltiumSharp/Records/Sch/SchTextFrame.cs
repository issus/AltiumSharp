using System;
using System.Drawing;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchTextFrame : SchRectangle
    {
        public override int Record => 28;
        public Color TextColor { get; internal set; }
        public int FontId { get; internal set; }
        public bool ShowBorder { get; internal set; }
        public int Alignment { get; internal set; }
        public bool WordWrap { get; internal set; }
        public bool ClipToRect { get; internal set; }
        public string Text { get; internal set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            TextColor = p["TEXTCOLOR"].AsColorOrDefault();
            FontId = p["FONTID"].AsIntOrDefault();
            ShowBorder = p["SHOWBORDER"].AsBool();
            Alignment = p["ALIGNMENT"].AsIntOrDefault();
            WordWrap = p["WORDWRAP"].AsBool();
            ClipToRect = p["CLIPTORECT"].AsBool();
            Text = p["TEXT"].AsStringOrDefault();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            p.Add("TEXTCOLOR", TextColor);
            p.Add("FONTID", FontId);
            p.MoveKeys("ISSOLID");
            p.Add("SHOWBORDER", ShowBorder);
            p.Add("ALIGNMENT", Alignment);
            p.Add("WORDWRAP", WordWrap);
            p.Add("CLIPTORECT", ClipToRect);
            p.Add("TEXT", Text);
        }
    }
}
