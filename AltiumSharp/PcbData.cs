using System.Collections.Generic;
using System.Drawing;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp
{
    public abstract class PcbData
    {
        /// <summary>
        /// Name of the file.
        /// </summary>
        internal string FileName { get; private set; }

        /// <summary>
        /// Mapping of image file names to the actual image data for
        /// embedded images.
        /// </summary>
        public Dictionary<string, Image> EmbeddedImages { get; } = new Dictionary<string, Image>();
    }

    public abstract class PcbData<THeader, TItem> : PcbData
        where THeader : new()
        where TItem :  IContainer, new()
    {
        /// <summary>
        /// Header information for the schematic file.
        /// </summary>
        public THeader Header { get; } = new THeader();

        /// <summary>
        /// List of items present the file.
        /// </summary>
        public List<TItem> Items { get; } = new List<TItem>();

        protected PcbData()
        {

        }
    }
}
