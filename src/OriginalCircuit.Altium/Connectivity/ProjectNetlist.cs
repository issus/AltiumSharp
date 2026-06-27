using OriginalCircuit.Altium.Diagnostics;

namespace OriginalCircuit.Altium.Connectivity;

/// <summary>One instantiated sheet in a project's logical hierarchy.</summary>
public sealed class ProjectSheetInstance
{
    internal ProjectSheetInstance(int id, string fileName, string? designator, string path, int? parentId)
    {
        Id = id;
        FileName = fileName;
        Designator = designator;
        Path = path;
        ParentId = parentId;
    }

    /// <summary>The instance id (unique per sheet instantiation).</summary>
    public int Id { get; }

    /// <summary>The source document file name (e.g. <c>"ADC.SchDoc"</c>).</summary>
    public string FileName { get; }

    /// <summary>The sheet-symbol instance designator that created this sheet, or <c>null</c> for the root.</summary>
    public string? Designator { get; }

    /// <summary>The hierarchical path of designators to this instance (e.g. <c>"Digitiser/U_ADC1"</c>).</summary>
    public string Path { get; }

    /// <summary>The parent instance id, or <c>null</c> for a root.</summary>
    public int? ParentId { get; }
}

/// <summary>
/// The reconstructed netlist across a whole project hierarchy: per-sheet netlists merged through the
/// port ↔ sheet-entry boundary, global power nets and the project's net-identifier scope.
/// </summary>
public sealed class ProjectNetlist
{
    private readonly Dictionary<string, SchematicNet> _byPinKey;

    internal ProjectNetlist(
        IReadOnlyList<SchematicNet> nets,
        IReadOnlyList<NetPin> unconnectedPins,
        IReadOnlyList<ProjectSheetInstance> sheets,
        NetIdentifierScope scope,
        IReadOnlyList<AltiumDiagnostic> diagnostics)
    {
        Nets = nets;
        UnconnectedPins = unconnectedPins;
        Sheets = sheets;
        Scope = scope;
        Diagnostics = diagnostics;

        _byPinKey = new Dictionary<string, SchematicNet>(StringComparer.OrdinalIgnoreCase);
        foreach (var net in nets)
            foreach (var pin in net.Pins)
                _byPinKey[pin.Key] = net;
    }

    /// <summary>The merged project-wide nets.</summary>
    public IReadOnlyList<SchematicNet> Nets { get; }

    /// <summary>Pins not connected to anything.</summary>
    public IReadOnlyList<NetPin> UnconnectedPins { get; }

    /// <summary>The sheet instances that make up the hierarchy.</summary>
    public IReadOnlyList<ProjectSheetInstance> Sheets { get; }

    /// <summary>The net-identifier scope used when merging.</summary>
    public NetIdentifierScope Scope { get; }

    /// <summary>Non-fatal issues encountered while solving the project.</summary>
    public IReadOnlyList<AltiumDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Returns the net carrying the given component pin (by reference designator and pin), or
    /// <see langword="null"/> when the pin is not on any net.
    /// </summary>
    public SchematicNet? NetForPin(string componentDesignator, string pinDesignator) =>
        _byPinKey.TryGetValue($"{componentDesignator}.{pinDesignator}", out var net) ? net : null;
}
