using System;
using System.Collections.Generic;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp.Records
{
    public class SchPrimitive : Primitive, IContainer
    {
        public virtual int Record { get; }

        public bool IsNotAccesible { get; set; }

        public int? IndexInSheet => (Owner as SchSheetHeader)?.GetPrimitiveIndexOf(this);

        internal int OwnerIndex { get; set; }

        public int? OwnerPartId { get; set; }

        public int? OwnerPartDisplayMode { get; set; }

        public bool GraphicallyLocked { get; set; }

        private List<SchPrimitive> _primitives = new List<SchPrimitive>();
        internal IReadOnlyList<SchPrimitive> Primitives => _primitives;

        public virtual bool IsOfCurrentPart =>
            OwnerPartId <= 0 || (Owner is SchComponent component && component.CurrentPartId == OwnerPartId);

        public virtual bool IsOfCurrentDisplayMode =>
            !(Owner is SchComponent) ||
            (Owner is SchComponent component && component.DisplayMode == OwnerPartDisplayMode);

        public override bool IsVisible =>
            base.IsVisible && IsOfCurrentPart && IsOfCurrentDisplayMode;

        public SchPrimitive() : base()
        {
        }

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
                return GetAllPrimitives().OfType<T>();
            }
        }

        protected int GetPrimitiveIndexOf(SchPrimitive primitive) =>
            _primitives.IndexOf(primitive);

        public override CoordRect CalculateBounds() => CoordRect.Empty;

        public virtual void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            var recordType = p["RECORD"].AsIntOrDefault();
            if (Record != 0 && recordType != Record) throw new ArgumentException($"Record type mismatch when deserializing. Expected {Record} but got {recordType}", nameof(p));

            OwnerIndex = p["OWNERINDEX"].AsIntOrDefault();
            IsNotAccesible = p["ISNOTACCESIBLE"].AsBool();
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
            p.Add("INDEXINSHEET", IndexInSheet ?? default);
            p.Add("OWNERPARTID", OwnerPartId ?? default);
            p.Add("OWNERPARTDISPLAYMODE", OwnerPartDisplayMode ?? default);
            p.Add("GRAPHICALLYLOCKED", GraphicallyLocked);
        }

        public ParameterCollection ExportToParameters()
        {
            var parameters = new ParameterCollection();
            ExportToParameters(parameters);
            return parameters;
        }

        public IEnumerable<SchPrimitive> GetAllPrimitives()
        {
            return Enumerable.Concat(Primitives, DoGetParameters());
        }

        protected virtual IEnumerable<SchPrimitive> DoGetParameters()
        {
            return Enumerable.Empty<SchPrimitive>();
        }

        public void Add(SchPrimitive primitive)
        {
            if (primitive == null) throw new ArgumentNullException(nameof(primitive));
            if (primitive == this) return;

            if (DoAdd(primitive))
            {
                primitive.Owner = this;
                _primitives.Add(primitive);
            }
        }

        protected virtual bool DoAdd(SchPrimitive primitive)
        {
            return true;
        }

        public void Remove(SchPrimitive primitive)
        {
            if (primitive == null) throw new ArgumentNullException(nameof(primitive));

            primitive.Owner = null;
            _primitives.Remove(primitive);
        }
    }
}