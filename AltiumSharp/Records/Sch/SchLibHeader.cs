using System;
using System.Linq;
using System.Collections.Generic;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchLibHeader : SchDocumentHeader
    {
        private IList<SchComponent> _components;

        public static string Header => "Protel for Windows - Schematic Library Editor Binary File Version 5.0";
        public int MinorVersion { get; internal set; }

        public SchLibHeader(IList<SchComponent> components)
        {
            _components = components;

            MinorVersion = 2;
            SheetStyle = 9;
            SheetNumberSpaceSize = 12;
            CustomX = 18000;
            CustomY = 18000;
            UseCustomSheet = true;
            ReferenceZonesOn = true;
            DisplayUnit = Unit.Mil;
        }

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) 
                throw new ArgumentNullException(nameof(p));

            var header = p["HEADER"].AsStringOrDefault();
            if (header == null || !string.Equals(header, Header, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentOutOfRangeException($"{nameof(p)}[\"HEADER\"]");

            MinorVersion = p["MINORVERSION"].AsIntOrDefault();

            base.ImportFromParameters(p);
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("HEADER", Header);
            var totalPrimitiveCount = _components.Count +
                _components.SelectMany(e => e.GetAllPrimitives()).Count();
            p.Add("WEIGHT", totalPrimitiveCount + 1); // weight is the number of primitives + 1, for some reason
            p.Add("MINORVERSION", MinorVersion);

            base.ExportToParameters(p);

            p.Add("COMPCOUNT", _components.Count);
            for (var i = 0; i < _components.Count; ++i)
            {
                p.Add($"LIBREF{i}", _components[i].LibReference);
                p.Add($"COMPDESCR{i}", _components[i].ComponentDescription);
                p.Add($"PARTCOUNT{i}", _components[i].PartCount + 1); // part count is stored + 1
            }
        }
    }
}
