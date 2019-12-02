using System.Collections.Generic;
using AltiumSharp.BasicTypes;

namespace AltiumSharp
{
    public interface IContainer
    {
        IEnumerable<T> GetPrimitivesOfType<T>(bool flatten = true) where T : Primitive;

        CoordRect CalculateBounds();
    }
}
