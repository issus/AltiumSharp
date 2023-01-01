using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp
{
    public class PcbComponent : IComponent, IEnumerable<PcbPrimitive>
    {
        public string Pattern { get; set; }
        public string Description { get; set; }
        public Coord Height { get; set; }
        public string ItemGuid { get; set; }
        public string RevisionGuid { get; set; }

        string IComponent.Name => Pattern;
        string IComponent.Description => Description;

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
            Height = p["HEIGHT"].AsCoord();
            Description = p["DESCRIPTION"].AsStringOrDefault();
            ItemGuid = p["ITEMGUID"].AsStringOrDefault();
            RevisionGuid = p["REVISIONGUID"].AsStringOrDefault();
        }

        public void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("PATTERN", Pattern);
            p.Add("HEIGHT", Height, false);
            p.Add("DESCRIPTION", Description, false);
            p.Add("ITEMGUID", ItemGuid, false);
            p.Add("REVISIONGUID", RevisionGuid, false);
        }

        public ParameterCollection ExportToParameters()
        {
            var parameters = new ParameterCollection();
            ExportToParameters(parameters);
            return parameters;
        }

        public void Add(PcbPrimitive primitive)
        {
            if (primitive is PcbPad pad)
            {
                pad.Designator = pad.Designator ??
                    Utils.GenerateDesignator(GetPrimitivesOfType<PcbPad>(false).Select(p => p.Designator));
            }
            else if (primitive is PcbMetaTrack metaTrack)
            {
                foreach (var line in metaTrack.Lines)
                {
                    Primitives.Add(new PcbTrack
                    {
                        Layer = metaTrack.Layer,
                        Flags = metaTrack.Flags,
                        Start = line.Item1,
                        End = line.Item2
                    });
                }
                return; // ignore the actual primitive
            }

            Primitives.Add(primitive);
        }

        IEnumerator<PcbPrimitive> IEnumerable<PcbPrimitive>.GetEnumerator() => Primitives.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Primitives.GetEnumerator();
    }
}
