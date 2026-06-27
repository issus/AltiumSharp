namespace OriginalCircuit.Altium.Connectivity;

/// <summary>How a net acquired its identity / scope.</summary>
public enum NetScope
{
    /// <summary>A net confined to one sheet, named by a local net label or auto-named.</summary>
    LocalSheet,

    /// <summary>A net that crosses a sheet boundary via a port ↔ sheet-entry match.</summary>
    CrossSheetPort,

    /// <summary>A net named by a net label that is global in the active scope.</summary>
    GlobalLabel,

    /// <summary>A power net (named by a power object / hidden power pin), global by name.</summary>
    Power,

    /// <summary>A net carried inside a harness bundle.</summary>
    Harness,

    /// <summary>A net with an automatically-generated name (no explicit identifier).</summary>
    Auto,
}

/// <summary>
/// One reconstructed net: a set of electrically-common pins, the primitives that joined them, the
/// directives that apply to it, and its name / scope.
/// </summary>
public sealed class SchematicNet
{
    private readonly List<NetPin> _pins;
    private readonly List<object> _sourcePrimitives;
    private readonly List<NetIntent> _intents;

    internal SchematicNet(
        string name,
        NetScope scope,
        bool isNamedExplicitly,
        List<NetPin> pins,
        List<object> sourcePrimitives,
        List<NetIntent> intents)
    {
        Name = name;
        Scope = scope;
        IsNamedExplicitly = isNamedExplicitly;
        _pins = pins;
        _sourcePrimitives = sourcePrimitives;
        _intents = intents;
    }

    /// <summary>The net name (explicit identifier, or a stable auto-generated name).</summary>
    public string Name { get; internal set; }

    /// <summary>How the net acquired its identity / scope.</summary>
    public NetScope Scope { get; internal set; }

    /// <summary>Whether the name came from an explicit identifier (net label / power / port) vs auto-naming.</summary>
    public bool IsNamedExplicitly { get; }

    /// <summary>The pins on this net.</summary>
    public IReadOnlyList<NetPin> Pins => _pins;

    /// <summary>
    /// The primitives that contributed to this net (wires, net labels, power objects, ports, junctions,
    /// buses, bus entries, harness elements), for traceability and debugging.
    /// </summary>
    public IReadOnlyList<object> SourcePrimitives => _sourcePrimitives;

    /// <summary>The design directives bound to this net.</summary>
    public IReadOnlyList<NetIntent> Intents => _intents;

    internal void AddPin(NetPin pin) => _pins.Add(pin);
    internal void AddSource(object primitive) => _sourcePrimitives.Add(primitive);

    internal void AddIntent(NetIntent intent)
    {
        // The same directive can reach a net from several merged sheets/instances; keep one copy.
        foreach (var existing in _intents)
            if (existing.Source == intent.Source
                && string.Equals(existing.RawName, intent.RawName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.RawValue, intent.RawValue, StringComparison.Ordinal))
                return;
        _intents.Add(intent);
    }
    internal List<NetPin> PinsMutable => _pins;
    internal List<object> SourcesMutable => _sourcePrimitives;

    /// <inheritdoc />
    public override string ToString() => $"{Name} [{Scope}] ({_pins.Count} pin(s))";
}
