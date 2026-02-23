using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic document (.SchDoc file).
/// Unlike a SchLib which groups primitives by component, a SchDoc has a flat
/// list of primitives with owner relationships defined by OWNERINDEX.
/// </summary>
public interface ISchDocument : IAsyncDisposable
{
    /// <summary>
    /// All components placed in this document.
    /// </summary>
    IReadOnlyList<ISchComponent> Components { get; }

    /// <summary>
    /// All wires in this document (top-level, not owned by components).
    /// </summary>
    IReadOnlyList<ISchWire> Wires { get; }

    /// <summary>
    /// All net labels in this document (top-level).
    /// </summary>
    IReadOnlyList<ISchNetLabel> NetLabels { get; }

    /// <summary>
    /// All junctions in this document (top-level).
    /// </summary>
    IReadOnlyList<ISchJunction> Junctions { get; }

    /// <summary>
    /// All power objects in this document (top-level).
    /// </summary>
    IReadOnlyList<ISchPowerObject> PowerObjects { get; }

    /// <summary>
    /// All labels in this document (top-level).
    /// </summary>
    IReadOnlyList<ISchLabel> Labels { get; }

    /// <summary>
    /// All parameters in this document (top-level).
    /// </summary>
    IReadOnlyList<ISchParameter> Parameters { get; }

    /// <summary>
    /// Gets the bounding box encompassing all primitives.
    /// </summary>
    CoordRect Bounds { get; }

    /// <summary>
    /// Saves the document to a file.
    /// </summary>
    ValueTask SaveAsync(string path, SaveOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the document to a stream.
    /// </summary>
    ValueTask SaveAsync(Stream stream, SaveOptions? options = null, CancellationToken cancellationToken = default);
}
