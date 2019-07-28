using System;
using System.Drawing;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum TextJustification
    {
        BottomLeft, BottomCenter, BottomRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        TopLeft, TopCenter, TopRight
    }

    [Flags]
    public enum TextOrientations
    {
        Rotated = 1, Flipped = 2
    }

    public class TextStringRecord : SchPrimitive
    {
        public CoordPoint Location { get; internal set; }
        public Color Color { get; internal set; }
        public TextJustification Justification { get; internal set; }
        public TextOrientations Orientations { get; internal set; }
        public int FontId { get; internal set; }
        public string Text { get; internal set; }
        public bool IsMirrored { get; internal set; }
        public bool IsHidden { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X, Location.Y, 1, 1);

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Location = new CoordPoint(
                Utils.DxpFracToCoord(p["LOCATION.X"].AsIntOrDefault(), p["LOCATION.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["LOCATION.Y"].AsIntOrDefault(), p["LOCATION.Y_FRAC"].AsIntOrDefault()));
            Color = p["COLOR"].AsColorOrDefault();
            Justification = (TextJustification)p["JUSTIFICATION"].AsIntOrDefault();
            Orientations = (TextOrientations)p["ORIENTATION"].AsIntOrDefault();
            FontId = p["FONTID"].AsIntOrDefault();
            Text = p["TEXT"].AsStringOrDefault();
            IsMirrored = p["ISMIRRORED"].AsBool();
            IsHidden = p["ISHIDDEN"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            {
                var (n, f) = Utils.CoordToDxpFrac(Location.X);
                if (n != 0 || f != 0) p.Add("LOCATION.X", n);
                if (f != 0) p.Add("LOCATION.X" + "_FRAC", f);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(Location.Y);
                if (n != 0 || f != 0) p.Add("LOCATION.Y", n);
                if (f != 0) p.Add("LOCATION.Y" + "_FRAC", f);
            }
            p.Add("COLOR", Color);
            p.Add("JUSTIFICATION", (int)Justification);
            p.Add("ORIENTATION", (int)Orientations);
            p.Add("FONTID", FontId);
            p.Add("TEXT", Text);
            p.Add("ISMIRRORED", IsMirrored);
            p.Add("ISHIDDEN", IsHidden);
        }
    }
}
