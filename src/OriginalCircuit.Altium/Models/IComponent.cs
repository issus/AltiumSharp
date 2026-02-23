using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models;

/// <summary>
/// Represents a component (PCB footprint or schematic symbol).
/// </summary>
public interface IComponent
{
    /// <summary>
    /// The name/identifier of this component.
    /// For PCB footprints this is the pattern name, for schematic symbols this is the lib reference.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Optional description of this component.
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Gets the bounding box encompassing all primitives.
    /// </summary>
    CoordRect Bounds { get; }
}
