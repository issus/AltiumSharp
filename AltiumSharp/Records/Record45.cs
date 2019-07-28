using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class Record45 : SchPrimitive
    {
        public string Description { get; internal set; }
        public string ModelName { get; internal set; }
        public string ModelType { get; internal set; }
        public List<string> DataFile { get; internal set; }
        public bool IsCurrent { get; internal set; }
        public string UniqueId { get; internal set; }
        
        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Description = p["DESCRIPTION"].AsStringOrDefault();
            ModelName = p["MODELNAME"].AsStringOrDefault();
            ModelType = p["MODELTYPE"].AsStringOrDefault();
            DataFile = Enumerable.Range(1, p["DATAFILECOUNT"].AsInt())
                .Select(i => 
                    p[string.Format(CultureInfo.InvariantCulture, "MODELDATAFILEKIND{0}", i)].AsStringOrDefault())
                .ToList();
            IsCurrent = p["ISCURRENT"].AsBool();
            UniqueId = p["UNIQUEID"].AsStringOrDefault();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("DESCRIPTION", Description);
            p.Add("MODELNAME", ModelName);
            p.Add("MODELTYPE", ModelType);
            for (var i = 0; i < DataFile.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "MODELDATAFILEKIND{0}", i), DataFile[i]);
            }
            p.Add("ISCURRENT", IsCurrent);
            p.Add("UNIQUEID", UniqueId);
        }
    }
}
