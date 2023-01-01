using System;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchTemplate : SchGraphicalObject
    {
        public override int Record => 39;
        public string FileName { get; set; }
        
        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            FileName = p["FILENAME"].AsStringOrDefault();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            
            p.Add("FILENAME", FileName);
        }
    }
}
