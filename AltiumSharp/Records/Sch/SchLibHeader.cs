using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchLibHeader : SheetRecord
    {
        public string Header { get; internal set; }
        public int Weight { get; internal set; }
        public int MinorVersion { get; internal set; }
        public List<(string LibRef, string CompDescr, int PartCount)> Comp { get; internal set; }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            Header = p["HEADER"].AsStringOrDefault();
            Weight = p["WEIGHT"].AsIntOrDefault();
            MinorVersion = p["MINORVERSION"].AsIntOrDefault();
            base.ImportFromParameters(p);
            Comp = Enumerable.Range(0, p["COMPCOUNT"].AsIntOrDefault())
                .Select(i => (
                    p[string.Format(CultureInfo.InvariantCulture, "LIBREF{0}", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "COMPDESCR{0}", i)].AsStringOrDefault(),
                    p[string.Format(CultureInfo.InvariantCulture, "PARTCOUNT{0}", i)].AsIntOrDefault()))
                .ToList();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("HEADER", Header);
            p.Add("WEIGHT", Weight);
            p.Add("MINORVERSION", MinorVersion);
            base.ExportToParameters(p);
            p.Add("COMPCOUNT", Comp.Count);
            for (var i = 0; i < Comp.Count; i++)
            {
                p.Add(string.Format(CultureInfo.InvariantCulture, "LIBREF{0}", i), Comp[i].LibRef);
                p.Add(string.Format(CultureInfo.InvariantCulture, "COMPDESCR{0}", i), Comp[i].CompDescr);
                p.Add(string.Format(CultureInfo.InvariantCulture, "PARTCOUNT{0}", i), Comp[i].PartCount);
            }
        }
    }
}
