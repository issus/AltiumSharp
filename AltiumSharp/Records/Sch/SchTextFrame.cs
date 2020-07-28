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
        public Coord TextMargin { get; internal set; }

        public SchTextFrame() : base()
        {
            Color = ColorTranslator.FromWin32(0);
            AreaColor = ColorTranslator.FromWin32(16777215);
            FontId = 1;
            IsSolid = true;
            Alignment = 1;
            WordWrap = true;
            ClipToRect = true;
            Text = "Text";
            TextMargin = Utils.DxpFracToCoord(0, 5);
        }

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
            TextMargin = Utils.DxpFracToCoord(p["TEXTMARGIN"].AsIntOrDefault(), p["TEXTMARGIN_FRAC"].AsIntOrDefault());
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
            {
                var (n, f) = Utils.CoordToDxpFrac(TextMargin);
                p.Add("TEXTMARGIN.Y", n);
                p.Add("TEXTMARGIN_FRAC", f);
            }
        }
    }
}
