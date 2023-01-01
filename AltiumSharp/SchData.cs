using System.Collections.Generic;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp
{
    public abstract class SchData
    {
        /// <summary>
        /// Name of the file.
        /// </summary>
        internal string FileName { get; set; }
    }

    public abstract class SchData<THeader, TItem> : SchData
        where THeader : SchDocumentHeader
        where TItem :  SchPrimitive
    {
        /// <summary>
        /// Header information for the schematic file.
        /// </summary>
        public abstract THeader Header { get; }

        /// <summary>
        /// List of items present the file.
        /// </summary>
        public List<TItem> Items { get; } = new List<TItem>();

        protected SchData()
        {

        }
    }
}
