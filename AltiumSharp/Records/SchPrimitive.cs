using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchPrimitive : Primitive
    {
        public int Record { get; internal set; }

        public bool IsNotAccesible { get; internal set; }

        public int OwnerIndex { get; internal set; } = -1;

        public string UniqueId { get; internal set; }

        public int IndexInSheet { get; internal set; } = -1;

        public int CurrentPartId { get; internal set; } = -1;

        public int OwnerPartId { get; internal set; } = -1;

        public bool GraphicallyLocked { get; internal set; }

        public override CoordRect CalculateBounds() => CoordRect.Empty;

        public virtual void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            Record = p["RECORD"].AsIntOrDefault();
            IsNotAccesible = p["ISNOTACCESIBLE"].AsBool();
            OwnerIndex = p["OWNERINDEX"].AsIntOrDefault();
            UniqueId = p["UNIQUEID"].AsStringOrDefault();
            IndexInSheet = p["INDEXINSHEET"].AsIntOrDefault();
            OwnerPartId = p["OWNERPARTID"].AsIntOrDefault();
            GraphicallyLocked = p["GRAPHICALLYLOCKED"].AsBool();
        }

        public virtual void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.Add("RECORD", Record);
            p.Add("ISNOTACCESIBLE", IsNotAccesible);
            p.Add("OWNERINDEX", OwnerIndex);
            p.Add("UNIQUEID", UniqueId);
            p.Add("INDEXINSHEET", IndexInSheet);
            p.Add("OWNERPARTID", OwnerPartId);
            p.Add("GRAPHICALLYLOCKED", GraphicallyLocked);
        }
    }
}