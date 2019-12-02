using System;
using System.Drawing;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public enum PowerPortStyle
    {
        Circle,
        Arrow,
        Bar,
        Wave,
        PowerGround,
        SignalGround,
        Earth,
        GostArrow,
        GostPowerGround,
        GostEarth,
        GostBar
    }

    public class PowerPortRecord : SchPrimitive
    {
        public PowerPortStyle Style { get; internal set; }
        public CoordPoint Location { get; internal set; }
        public Color Color { get; internal set; }
        public TextOrientations Orientation { get; internal set; }
        public int FontId { get; internal set; }
        public string Text { get; internal set; }
        public bool ShowNetName { get; internal set; }
        public bool IsCrossSheetConnector { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X - Utils.DxpToCoord(2), Location.Y - Utils.DxpToCoord(2), Utils.DxpToCoord(4), Utils.DxpToCoord(4));

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Style = p["STYLE"].AsEnumOrDefault<PowerPortStyle>();
            Location = new CoordPoint(
                Utils.DxpFracToCoord(p["LOCATION.X"].AsIntOrDefault(), p["LOCATION.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["LOCATION.Y"].AsIntOrDefault(), p["LOCATION.Y_FRAC"].AsIntOrDefault()));
            Color = p["COLOR"].AsColorOrDefault();
            Orientation = (TextOrientations)p["ORIENTATION"].AsIntOrDefault();
            FontId = p["FONTID"].AsIntOrDefault();
            Text = p["TEXT"].AsStringOrDefault();
            ShowNetName = p["SHOWNETNAME"].AsBool();
            IsCrossSheetConnector = p["ISCROSSSHEETCONNECTOR"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);

            p.Add("STYLE", Style);
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
            p.Add("ORIENTATION", (int)Orientation);
            p.Add("FONTID", FontId);
            p.Add("TEXT", Text);
            p.Add("SHOWNETNAME", ShowNetName);
            p.Add("ISCROSSSHEETCONNECTOR", IsCrossSheetConnector);
        }
    }
}
