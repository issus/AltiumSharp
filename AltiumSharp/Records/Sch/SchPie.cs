using System;
using System.Drawing;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchPie : SchArc
    {
        public override int Record => 9;
        public bool IsSolid { get; set; }

        public SchPie() : base()
        {
            Color = ColorTranslator.FromWin32(16711680);
            AreaColor = ColorTranslator.FromWin32(12632256);
            IsSolid = true;
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            IsSolid = p["ISSOLID"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("ISSOLID", IsSolid);
        }
    }
}
