using System;
using System.Collections.Generic;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchMapDefiner : SchPrimitive
    {
        public override int Record => 47;
        public string DesignatorInterface { get; set; }
        public List<string> DesignatorImplementation { get; private set; }
        public bool IsTrivial { get; set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            DesignatorInterface = p["DESINTF"].AsStringOrDefault();
            DesignatorImplementation = Enumerable.Range(0, p["DESIMPCOUNT"].AsIntOrDefault())
                .Select(i =>
                    p[$"DESIMP{i}"].AsStringOrDefault())
                .ToList();
            IsTrivial = p["ISTRIVIAL"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("DESINTF", DesignatorInterface);
            p.Add("DESIMPCOUNT", DesignatorImplementation.Count);
            for (var i = 0; i < DesignatorImplementation.Count; ++i)
            {
                p.Add($"DESIMP{i}", DesignatorImplementation[i]);
            }
            p.Add("ISTRIVIAL", IsTrivial);
        }
    }
}
