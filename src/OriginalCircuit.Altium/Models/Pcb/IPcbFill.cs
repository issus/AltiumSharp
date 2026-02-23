using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB fill primitive that defines a solid rectangular copper region.
/// This is an Altium-specific primitive with no shared equivalent.
/// </summary>
public interface IPcbFill : IPrimitive
{
    CoordPoint Corner1 { get; }
    CoordPoint Corner2 { get; }
    int Layer { get; }
    double Rotation { get; }
}
