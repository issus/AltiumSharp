using System.Collections.Generic;
using System.Drawing;
using AltiumSharp.Records;

namespace AltiumSharp
{
    public abstract class SchData
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

    public abstract class SchData<THeader, TItem> : SchData
        where THeader : SchDocumentHeader, new()
        where TItem :  SchPrimitive, new()
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
