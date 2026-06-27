using OriginalCircuit.Altium.Diagnostics;

namespace OriginalCircuit.Altium.Connectivity;

/// <summary>
/// The reconstructed netlist for a single schematic document: the nets, the pins that could not be
/// placed on any net, and any diagnostics raised while solving.
/// </summary>
public sealed class SchematicNetlist
{
    private readonly Dictionary<string, SchematicNet> _byPinKey;

    internal SchematicNetlist(
        IReadOnlyList<SchematicNet> nets,
        IReadOnlyList<NetPin> unconnectedPins,
        IReadOnlyList<AltiumDiagnostic> diagnostics,
        string? sheetFileName)
    {
        Nets = nets;
        UnconnectedPins = unconnectedPins;
        Diagnostics = diagnostics;
        SheetFileName = sheetFileName;

        _byPinKey = new Dictionary<string, SchematicNet>(StringComparer.OrdinalIgnoreCase);
        foreach (var net in nets)
            foreach (var pin in net.Pins)
                _byPinKey[pin.Key] = net;
    }

    /// <summary>All reconstructed nets, including single-pin and auto-named nets.</summary>
    public IReadOnlyList<SchematicNet> Nets { get; }

    /// <summary>Pins that terminate without touching any conductor (truly floating pins).</summary>
    public IReadOnlyList<NetPin> UnconnectedPins { get; }

    /// <summary>Non-fatal issues encountered while solving connectivity.</summary>
    public IReadOnlyList<AltiumDiagnostic> Diagnostics { get; }

    /// <summary>The source sheet file name (e.g. <c>"Power.SchDoc"</c>), when known.</summary>
    public string? SheetFileName { get; }

    /// <summary>
    /// Returns the net carrying the given component pin, or <see langword="null"/> when the pin is not
    /// on any net (it appears in <see cref="UnconnectedPins"/> instead).
    /// </summary>
    /// <param name="componentDesignator">The component reference designator (e.g. <c>"U1"</c>).</param>
    /// <param name="pinDesignator">The pin designator / number (e.g. <c>"3"</c>).</param>
    public SchematicNet? NetForPin(string componentDesignator, string pinDesignator) =>
        _byPinKey.TryGetValue($"{componentDesignator}.{pinDesignator}", out var net) ? net : null;
}
