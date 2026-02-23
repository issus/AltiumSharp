using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Pcb;

/// <summary>
/// Represents a PCB document (.PcbDoc file).
/// Unlike a PcbLib which groups primitives by footprint, a PcbDoc has
/// board-level primitives organized by type in separate storages.
/// </summary>
public interface IPcbDocument : IAsyncDisposable
{
    /// <summary>
    /// All placed component instances on the board.
    /// </summary>
    IReadOnlyList<IPcbComponent> Components { get; }

    /// <summary>
    /// All pads on the board.
    /// </summary>
    IReadOnlyList<IPcbPad> Pads { get; }

    /// <summary>
    /// All vias on the board.
    /// </summary>
    IReadOnlyList<IPcbVia> Vias { get; }

    /// <summary>
    /// All tracks on the board.
    /// </summary>
    IReadOnlyList<IPcbTrack> Tracks { get; }

    /// <summary>
    /// All arcs on the board.
    /// </summary>
    IReadOnlyList<IPcbArc> Arcs { get; }

    /// <summary>
    /// All text objects on the board.
    /// </summary>
    IReadOnlyList<IPcbText> Texts { get; }

    /// <summary>
    /// All fills on the board.
    /// </summary>
    IReadOnlyList<IPcbFill> Fills { get; }

    /// <summary>
    /// All regions on the board.
    /// </summary>
    IReadOnlyList<IPcbRegion> Regions { get; }

    /// <summary>
    /// All component bodies on the board.
    /// </summary>
    IReadOnlyList<IPcbComponentBody> ComponentBodies { get; }

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
