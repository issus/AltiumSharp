using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class PolygonRecord : SchPrimitive
    {
        public LineWidth LineWidth { get; internal set; }
        public Color Color { get; internal set; }
        public Color AreaColor { get; internal set; }
        public bool IsSolid { get; internal set; }
        public bool Transparent { get; internal set; }
        public List<CoordPoint> Location { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(
                new CoordPoint(Location.Min(p => p.X), Location.Min(p => p.Y)),
                new CoordPoint(Location.Max(p => p.X), Location.Max(p => p.Y)));

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            LineWidth = (LineWidth)p["LINEWIDTH"].AsIntOrDefault();
            Color = p["COLOR"].AsColorOrDefault();
            AreaColor = p["AREACOLOR"].AsColorOrDefault();
            IsSolid = p["ISSOLID"].AsBool();
            Transparent = p["TRANSPARENT"].AsBool();
            Location = Enumerable.Range(1, p["LOCATIONCOUNT"].AsInt())
                .Select(i => new CoordPoint(
                    Utils.DxpFracToCoord(p[$"X{i}"].AsIntOrDefault(), p[$"X{i}_FRAC"].AsIntOrDefault()),
                    Utils.DxpFracToCoord(p[$"Y{i}"].AsIntOrDefault(), p[$"Y{i}_FRAC"].AsIntOrDefault())))
                .ToList();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("LINEWIDTH", (int)LineWidth);
            p.Add("COLOR", Color);
            p.Add("AREACOLOR", AreaColor);
            p.Add("ISSOLID", IsSolid);
            p.Add("TRANSPARENT", Transparent);
            for (var i = 0; i < Location.Count; i++)
            {
                {
                    var (n, f) = Utils.CoordToDxpFrac(Location[i].X);
                    if (n != 0 || f != 0) p.Add($"X{i}", n);
                    if (f != 0) p.Add($"X{i}"+"_FRAC", f);
                }
                {
                    var (n, f) = Utils.CoordToDxpFrac(Location[i].Y);
                    if (n != 0 || f != 0) p.Add($"Y{i}", n);
                    if (f != 0) p.Add($"Y{i}"+"_FRAC", f);
                }
            }
        }
    }
}
