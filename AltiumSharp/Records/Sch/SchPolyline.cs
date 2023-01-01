using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public enum LineShape
    {
        None, Arrow, SolidArrow, Tail, SolidTail, Circle, Square
    }

    public class SchPolyline : SchBasicPolyline
    {
        public override int Record => 6;
        public LineShape StartLineShape { get; set; }
        public LineShape EndLineShape { get; set; }
        public int LineShapeSize { get; set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            StartLineShape = p["STARTLINESHAPE"].AsEnumOrDefault<LineShape>();
            EndLineShape = p["ENDLINESHAPE"].AsEnumOrDefault<LineShape>();
            LineShapeSize = p["LINESHAPESIZE"].AsIntOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("STARTLINESHAPE", StartLineShape);
            p.Add("ENDLINESHAPE", EndLineShape);
            p.Add("LINESHAPESIZE", LineShapeSize);
            p.MoveKeys("COLOR");
        }
    }
}
