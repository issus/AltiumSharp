using System;
using System.Collections.Generic;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchPolygon : SchGraphicalObject
    {
        public override int Record => 7;
        public bool IsSolid { get; internal set; }
        public LineWidth LineWidth { get; internal set; }
        public List<CoordPoint> Vertices { get; internal set; }
        public bool Transparent { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(
                new CoordPoint(Vertices.Min(p => p.X), Vertices.Min(p => p.Y)),
                new CoordPoint(Vertices.Max(p => p.X), Vertices.Max(p => p.Y)));

        public SchPolygon() : base()
        {
            LineWidth = LineWidth.Small;
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            IsSolid = p["ISSOLID"].AsBool();
            LineWidth = p["LINEWIDTH"].AsEnumOrDefault<LineWidth>();
            Vertices = Enumerable.Range(1, p["LOCATIONCOUNT"].AsInt())
                .Select(i => new CoordPoint(
                    Utils.DxpFracToCoord(p[$"X{i}"].AsIntOrDefault(), p[$"X{i}_FRAC"].AsIntOrDefault()),
                    Utils.DxpFracToCoord(p[$"Y{i}"].AsIntOrDefault(), p[$"Y{i}_FRAC"].AsIntOrDefault())))
                .ToList();
            Transparent = p["TRANSPARENT"].AsBool();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            p.Add("LINEWIDTH", LineWidth);
            p.MoveKeys("COLOR");
            p.Add("ISSOLID", IsSolid);
            p.Add("TRANSPARENT", Transparent);
            p.Add("LOCATIONCOUNT", Vertices.Count);
            for (var i = 0; i < Vertices.Count; i++)
            {
                {
                    var (n, f) = Utils.CoordToDxpFrac(Vertices[i].X);
                    if (n != 0 || f != 0) p.Add($"X{i+1}", n);
                    if (f != 0) p.Add($"X{i+1}"+"_FRAC", f);
                }
                {
                    var (n, f) = Utils.CoordToDxpFrac(Vertices[i].Y);
                    if (n != 0 || f != 0) p.Add($"Y{i+1}", n);
                    if (f != 0) p.Add($"Y{i+1}"+"_FRAC", f);
                }
            }
        }
    }
}
