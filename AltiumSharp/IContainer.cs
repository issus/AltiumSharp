using System.Collections.Generic;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp
{
    public interface IContainer
    {
        IEnumerable<T> GetPrimitivesOfType<T>(bool flatten = true) where T : Primitive;

        CoordRect CalculateBounds();
    }
}
