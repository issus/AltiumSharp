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

        /// <summary>
        /// List of model information read from the library, including positioning parameters
        /// and the raw model data.
        /// </summary>
        public Dictionary<string, (ParameterCollection positioning, string step)> Models { get; } =
            new Dictionary<string, (ParameterCollection positioning, string step)>();

        public PcbLib() : base()
        {

        }
    }
}
