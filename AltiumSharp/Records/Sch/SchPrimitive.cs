using System;
using System.Collections.Generic;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchPrimitive : Primitive, IContainer
    {
        public virtual int Record { get; }

        public bool IsNotAccesible { get; internal set; }

        public int OwnerIndex { get; internal set; }

        public int IndexInSheet { get; internal set; }

        public int OwnerPartId { get; internal set; }

        public int OwnerPartDisplayMode { get; internal set; }

        public bool GraphicallyLocked { get; internal set; }

        public List<SchPrimitive> Primitives { get; } = new List<SchPrimitive>();

        public IEnumerable<T> GetPrimitivesOfType<T>(bool flatten = true) where T : Primitive
        {
            if (flatten)
            {
                return Enumerable.Concat(
                    GetPrimitivesOfType<T>(false),
                    Primitives.SelectMany(p => p.GetPrimitivesOfType<T>(true)));
            }
            else
            {
                return Primitives.OfType<T>();
            }
        }

        public override CoordRect CalculateBounds() => CoordRect.Empty;

        public override bool IsVisible => base.IsVisible && ((Owner as SchComponent)?.DisplayMode ?? 0) == OwnerPartDisplayMode;

        public virtual void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            var recordType = p["RECORD"].AsIntOrDefault();
            if (recordType != Record) throw new ArgumentException($"Record type mismatch when deserializing. Expected {Record} but got {recordType}", nameof(p));

            OwnerIndex = p["OWNERINDEX"].AsIntOrDefault();
            IsNotAccesible = p["ISNOTACCESIBLE"].AsBool();
            IndexInSheet = p["INDEXINSHEET"].AsIntOrDefault();
            OwnerPartId = p["OWNERPARTID"].AsIntOrDefault();
            OwnerPartDisplayMode = p["OWNERPARTDISPLAYMODE"].AsIntOrDefault();
            GraphicallyLocked = p["GRAPHICALLYLOCKED"].AsBool();
        }

        public virtual void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("RECORD", Record);
            p.Add("OWNERINDEX", OwnerIndex);
            p.Add("ISNOTACCESIBLE", IsNotAccesible);
            p.Add("INDEXINSHEET", IndexInSheet);
            p.Add("OWNERPARTID", OwnerPartId);
            p.Add("OWNERPARTDISPLAYMODE", OwnerPartDisplayMode);
            p.Add("GRAPHICALLYLOCKED", GraphicallyLocked);
        }

        public ParameterCollection ExportToParameters()
        {
            var parameters = new ParameterCollection();
            ExportToParameters(parameters);
            return parameters;
        }
    }
}