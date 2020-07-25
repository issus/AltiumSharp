using System;
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

    public class SchPowerObject : SchLabel
    {
        public PowerPortStyle Style { get; internal set; }
        public bool ShowNetName { get; internal set; }
        public bool IsCrossSheetConnector { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X - Utils.DxpToCoord(2), Location.Y - Utils.DxpToCoord(2), Utils.DxpToCoord(4), Utils.DxpToCoord(4));

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Style = p["STYLE"].AsEnumOrDefault<PowerPortStyle>();
            ShowNetName = p["SHOWNETNAME"].AsBool();
            IsCrossSheetConnector = p["ISCROSSSHEETCONNECTOR"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("STYLE", Style);
            p.Add("SHOWNETNAME", ShowNetName, false);
            p.Add("ISCROSSSHEETCONNECTOR", IsCrossSheetConnector);
        }
    }
}
