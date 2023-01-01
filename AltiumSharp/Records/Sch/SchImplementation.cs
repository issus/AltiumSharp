using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    /// <summary>
    /// Model of a component.
    /// </summary>
    public class SchImplementation : SchPrimitive
    {
        public override int Record => 45;
        public string Description { get; set; }
        public string ModelName { get; set; }
        public string ModelType { get; set; }
        public List<string> DataFile { get; internal set; }
        public bool IsCurrent { get; set; }
       
        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Description = p["DESCRIPTION"].AsStringOrDefault();
            ModelName = p["MODELNAME"].AsStringOrDefault();
            ModelType = p["MODELTYPE"].AsStringOrDefault();
            DataFile = Enumerable.Range(1, p["DATAFILECOUNT"].AsIntOrDefault())
                .Select(i => 
                    p[string.Format(CultureInfo.InvariantCulture, "MODELDATAFILEKIND{0}", i)].AsStringOrDefault())
                .ToList();
            IsCurrent = p["ISCURRENT"].AsBool();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("DESCRIPTION", Description);
            p.Add("MODELNAME", ModelName);
            p.Add("MODELTYPE", ModelType);
            p.Add("DATAFILECOUNT", DataFile.Count);
            for (var i = 0; i < DataFile.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "MODELDATAFILEKIND{0}", i), DataFile[i]);
            }
            p.Add("ISCURRENT", IsCurrent);
        }
    }
}
