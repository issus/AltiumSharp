using System;
using System.Collections.Generic;
using System.Text;
using AltiumSharp.BasicTypes;

namespace AltiumSharp
{
    public abstract class Primitive
    {
        public IEnumerable<byte> RawData { get; internal set; }

        internal void SetRawData(in byte[] rawData) => RawData = rawData;

        public abstract CoordRect CalculateBounds();
    }
}
