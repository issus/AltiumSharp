using OriginalCircuit.Altium.Diagnostics;

namespace OriginalCircuit.Altium.Connectivity;

/// <summary>One instantiated sheet in a project's logical hierarchy.</summary>
public sealed class ProjectSheetInstance
{
    internal ProjectSheetInstance(
        int id, string fileName, string? designator, string path, int? parentId,
        IReadOnlyList<string> symbolUidPath, string? channelName, int? channelIndex, bool isRepeated)
    {
        Id = id;
        FileName = fileName;
        Designator = designator;
        Path = path;
        ParentId = parentId;
        SymbolUidPath = symbolUidPath;
        ChannelName = channelName;
        ChannelIndex = channelIndex;
        IsRepeated = isRepeated;
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

    /// <summary>
    /// The chain of ancestor sheet-symbol UniqueIds from the root to this instance — the channel
    /// discriminator. A PCB component's <c>SourceUniqueId</c> is this chain followed by the component's
    /// own UniqueId, so a PCB pad can be mapped to the exact channel instance it belongs to.
    /// </summary>
    public IReadOnlyList<string> SymbolUidPath { get; }

    /// <summary>The channel name (a <c>Repeat()</c> channel name, or the repeated sheet's designation), when this is a channel.</summary>
    public string? ChannelName { get; }

    /// <summary>The 1-based channel index within a repeated group, or <c>null</c> when not a channel.</summary>
    public int? ChannelIndex { get; }

    /// <summary>Whether this instance is one of several channels of the same sheet under its parent.</summary>
    public bool IsRepeated { get; }
}

/// <summary>
/// The reconstructed netlist across a whole project hierarchy: per-sheet netlists merged through the
/// port ↔ sheet-entry boundary, global power nets and the project's net-identifier scope.
/// </summary>
public sealed class ProjectNetlist
{
    private readonly Dictionary<string, SchematicNet> _byPinKey;
    private readonly Dictionary<(int, string), SchematicNet> _byInstancePinKey;
    private readonly Dictionary<string, ProjectSheetInstance> _instanceByUidPath;

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
        _byInstancePinKey = new Dictionary<(int, string), SchematicNet>();
        foreach (var net in nets)
            foreach (var pin in net.Pins)
            {
                _byPinKey[pin.Key] = net;
                _byInstancePinKey[(pin.SheetInstanceId, pin.Key)] = net;
            }

        _instanceByUidPath = new Dictionary<string, ProjectSheetInstance>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sheets)
            _instanceByUidPath[UidKey(s.SymbolUidPath)] = s;
    }

    private static string UidKey(IEnumerable<string> path) => string.Join("", path);

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

    /// <summary>
    /// Returns the net carrying a component pin on a specific sheet instance. In a multi-channel design
    /// the same designator appears on several channels; <paramref name="sheetInstanceId"/> selects the
    /// channel. Use <see cref="FindInstanceByUidPath"/> to resolve the instance from a PCB component's
    /// <c>SourceUniqueId</c> chain.
    /// </summary>
    public SchematicNet? NetForPin(int sheetInstanceId, string componentDesignator, string pinDesignator) =>
        _byInstancePinKey.TryGetValue((sheetInstanceId, $"{componentDesignator}.{pinDesignator}"), out var net) ? net : null;

    /// <summary>
    /// Finds the sheet instance whose <see cref="ProjectSheetInstance.SymbolUidPath"/> equals the given
    /// chain of sheet-symbol UniqueIds (a PCB component's <c>SourceUniqueId</c> minus its own trailing
    /// UniqueId segment), or <see langword="null"/> when none matches.
    /// </summary>
    public ProjectSheetInstance? FindInstanceByUidPath(IEnumerable<string> symbolUidPath) =>
        _instanceByUidPath.TryGetValue(UidKey(symbolUidPath), out var inst) ? inst : null;
}
