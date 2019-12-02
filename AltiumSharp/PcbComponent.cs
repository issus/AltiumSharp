using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;

namespace AltiumSharp
{
    public class PcbComponent : IContainer
    {
        public string Name => Pattern;

        public string Pattern { get; private set; }

        public string Description { get; private set; }

        public Coord Height { get; private set; }

        public int Pads => Primitives.Where(p => p is PcbPad).Count();

        public List<PcbPrimitive> Primitives { get; } = new List<PcbPrimitive>();

        public IEnumerable<T> GetPrimitivesOfType<T>(bool flatten) where T : Primitive =>
            Primitives.OfType<T>();

        public CoordRect CalculateBounds() =>
            CoordRect.Union(GetPrimitivesOfType<Primitive>(true).Select(p => p.CalculateBounds()));

        public void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            Pattern = p["PATTERN"].AsStringOrDefault();
            Height = p["HEIGHT"].AsIntOrDefault();
            Description = p["DESCRIPTION"].AsStringOrDefault();
        }

        public void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("PATTERN", Pattern);
            p.Add("HEIGHT", Height);
            p.Add("DESCRIPTION", Description);
        }
    }
}
