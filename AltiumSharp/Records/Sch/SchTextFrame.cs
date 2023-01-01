using System;
using System.Drawing;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchTextFrame : SchRectangle
    {
        public override int Record => 28;
        public Color TextColor { get; set; }
        public int FontId { get; set; }
        public bool ShowBorder { get; set; }
        public int Alignment { get; set; }
        public bool WordWrap { get; set; }
        public bool ClipToRect { get; set; }
        public string Text { get; set; }
        public Coord TextMargin { get; set; }

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
            TextMargin = Utils.DxpFracToCoord(p["TEXTMARGIN"].AsIntOrDefault(), p["TEXTMARGIN_FRAC"].AsIntOrDefault(5));
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
