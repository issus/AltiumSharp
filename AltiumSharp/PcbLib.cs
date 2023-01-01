using System.Collections;
using System.Collections.Generic;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp
{
    public class PcbLib : PcbData<PcbLibHeader, PcbComponent>, IEnumerable<PcbComponent>
    {
        /// <summary>
        /// UniqueId from the binary FileHeader entry
        /// </summary>
        public string UniqueId { get; internal set; }

        public PcbLib() : base()
        {

        }

        public void Add(PcbComponent component)
        {
            if (component == null) return;

            if (string.IsNullOrEmpty(component.Pattern))
            {
                component.Pattern = $"Component_{Items.Count + 1}";
            }

            Items.Add(component);
        }

        IEnumerator<PcbComponent> IEnumerable<PcbComponent>.GetEnumerator() => Items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
    }
}
