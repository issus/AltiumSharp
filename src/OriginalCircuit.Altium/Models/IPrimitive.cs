using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models;

/// <summary>
/// Base interface for all primitive elements (pads, tracks, pins, wires, etc.).
/// </summary>
public interface IPrimitive
{
    /// <summary>
    /// Gets the bounding box of this primitive.
    /// </summary>
    CoordRect Bounds { get; }
}
