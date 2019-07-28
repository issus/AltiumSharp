using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AltiumSharp.BasicTypes;

namespace AltiumSharp
{
    public interface IComponent
    {
        string Name { get; }

        string Description { get; }

        IEnumerable<T> GetPrimitivesOfType<T>() where T : Primitive;

        CoordRect CalculateBounds();
    }
}
