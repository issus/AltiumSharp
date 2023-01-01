using System;
using System.Collections.Generic;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchPolygon : SchGraphicalObject
    {
        public override int Record => 7;
        public bool IsSolid { get; set; }
        public LineWidth LineWidth { get; set; }
        public List<CoordPoint> Vertices { get; set; } = new List<CoordPoint>();
        public bool Transparent { get; set; }

        public SchPolygon() : base()
        {
            LineWidth = LineWidth.Small;
        }

        public override CoordRect CalculateBounds() =>
            new CoordRect(
                new CoordPoint(Vertices.Min(p => p.X), Vertices.Min(p => p.Y)),
                new CoordPoint(Vertices.Max(p => p.X), Vertices.Max(p => p.Y)));

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
                    p.Add($"X{i+1}", n);
                    p.Add($"X{i+1}_FRAC", f);
                }
                {
                    var (n, f) = Utils.CoordToDxpFrac(Vertices[i].Y);
                    p.Add($"Y{i+1}", n);
                    p.Add($"Y{i+1}_FRAC", f);
                }
            }
        }
    }
}
