using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB component body primitive that defines the physical 3D outline of a component.
/// This is an Altium-specific primitive with no shared equivalent.
/// </summary>
public interface IPcbComponentBody : IPrimitive
{
    IReadOnlyList<CoordPoint> Outline { get; }
    int Layer { get; }
}
