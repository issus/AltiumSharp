namespace OriginalCircuit.Altium.Connectivity;

/// <summary>The classified kind of a <see cref="NetIntent"/>.</summary>
public enum NetIntentKind
{
    /// <summary>A controlled-impedance constraint (ohms).</summary>
    Impedance,

    /// <summary>A high-speed frequency annotation (hertz).</summary>
    Frequency,

    /// <summary>A net voltage annotation (volts).</summary>
    Voltage,

    /// <summary>A differential-pair binding (positive / negative net names).</summary>
    DiffPair,

    /// <summary>A net-class membership.</summary>
    NetClass,

    /// <summary>A length / propagation-delay match constraint.</summary>
    LengthMatch,

    /// <summary>A generic PCB design rule directive.</summary>
    PcbRule,

    /// <summary>Anything not recognised — preserved verbatim via <see cref="NetIntent.RawName"/>/<see cref="NetIntent.RawValue"/>.</summary>
    Other,
}

/// <summary>Where a <see cref="NetIntent"/> was extracted from.</summary>
public enum NetIntentSource
{
    /// <summary>A directive marker (<see cref="OriginalCircuit.Altium.Models.Sch.SchParameterSet"/>).</summary>
    ParameterSet,

    /// <summary>An area directive (<see cref="OriginalCircuit.Altium.Models.Sch.SchBlanket"/>).</summary>
    Blanket,

    /// <summary>A port carrying the binding.</summary>
    Port,

    /// <summary>A net label carrying the binding.</summary>
    NetLabel,

    /// <summary>A per-net property (e.g. net voltage / frequency / impedance set in the properties panel).</summary>
    NetProperty,
}

/// <summary>
/// One design directive bound to a net: a name/value pair classified into a typed
/// <see cref="NetIntentKind"/> with best-effort parsed values. The raw name and value are always
/// preserved for fidelity even when the value could not be parsed.
/// </summary>
public sealed class NetIntent
{
    internal NetIntent(
        NetIntentKind kind,
        string rawName,
        string rawValue,
        NetIntentSource source,
        object? sourcePrimitive)
    {
        Kind = kind;
        RawName = rawName;
        RawValue = rawValue;
        Source = source;
        SourcePrimitive = sourcePrimitive;
    }

    /// <summary>The classified intent kind.</summary>
    public NetIntentKind Kind { get; }

    /// <summary>The raw directive name (parameter name).</summary>
    public string RawName { get; }

    /// <summary>The raw directive value (parameter value).</summary>
    public string RawValue { get; }

    /// <summary>Where this intent was extracted from.</summary>
    public NetIntentSource Source { get; }

    /// <summary>The primitive the intent was read from (a parameter set, blanket, port, …), for traceability.</summary>
    public object? SourcePrimitive { get; }

    /// <summary>Parsed impedance in ohms, when <see cref="Kind"/> is <see cref="NetIntentKind.Impedance"/> and parseable.</summary>
    public double? Ohms { get; internal init; }

    /// <summary>Parsed frequency in hertz, when <see cref="Kind"/> is <see cref="NetIntentKind.Frequency"/> and parseable.</summary>
    public double? Hz { get; internal init; }

    /// <summary>Parsed voltage in volts, when <see cref="Kind"/> is <see cref="NetIntentKind.Voltage"/> and parseable.</summary>
    public double? Volts { get; internal init; }

    /// <summary>Parsed length in millimetres, when <see cref="Kind"/> is <see cref="NetIntentKind.LengthMatch"/> and the value is a physical length.</summary>
    public double? LengthMm { get; internal init; }

    /// <summary>Parsed propagation delay in seconds, when <see cref="Kind"/> is <see cref="NetIntentKind.LengthMatch"/> and the value is a time (e.g. "250ps").</summary>
    public double? DelaySeconds { get; internal init; }

    /// <summary>The differential-pair member net names, when <see cref="Kind"/> is <see cref="NetIntentKind.DiffPair"/>.</summary>
    public (string Positive, string Negative)? Pair { get; internal init; }

    /// <summary>The net-class name, when <see cref="Kind"/> is <see cref="NetIntentKind.NetClass"/>.</summary>
    public string? NetClass { get; internal init; }

    /// <inheritdoc />
    public override string ToString() => $"{Kind}: {RawName}={RawValue} ({Source})";
}
