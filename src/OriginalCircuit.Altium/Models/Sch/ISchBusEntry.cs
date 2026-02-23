using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

public interface ISchBusEntry : IPrimitive
{
    CoordPoint Location { get; }
    CoordPoint Corner { get; }
    int Color { get; }
    int LineWidth { get; }
}
