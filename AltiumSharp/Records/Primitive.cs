using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AltiumSharp.BasicTypes;

namespace AltiumSharp
{
    public abstract class Primitive
    {
        public IEnumerable<byte> RawData { get; internal set; }

        public Primitive Owner { get; internal set; }

        [Conditional("DEBUG")]
        internal void SetRawData(in byte[] rawData) => RawData = rawData;

        public abstract CoordRect CalculateBounds();

        public virtual bool IsVisible => Owner?.IsVisible ?? true;
    }
}
