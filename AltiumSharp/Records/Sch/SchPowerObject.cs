using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
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
        public override int Record => 17;
        public PowerPortStyle Style { get; set; }
        public bool ShowNetName { get; set; }
        public bool IsCrossSheetConnector { get; set; }

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
            p.SetBookmark();
            p.Add("STYLE", Style);
            p.Add("SHOWNETNAME", ShowNetName, false);
            p.MoveKeys("LOCATION.X");
            p.MoveKeys("TEXT");
            p.Add("ISCROSSSHEETCONNECTOR", IsCrossSheetConnector);
        }
    }
}
