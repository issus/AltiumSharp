using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchTemplate : SchGraphicalObject
    {
        public string FileName { get; internal set; }
        
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
