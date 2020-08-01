using System.Collections.Generic;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;

namespace AltiumSharp
{
    public class PcbLib : PcbData<PcbLibHeader, PcbComponent>
    {
        /// <summary>
        /// UniqueId from the binary FileHeader entry
        /// </summary>
        public string UniqueId { get; internal set; }

        public PcbLib() : base()
        {

        }
    }
}
