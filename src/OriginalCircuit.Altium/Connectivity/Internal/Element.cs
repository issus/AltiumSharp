using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity.Internal;

/// <summary>The kind of a connectable element in the connectivity graph.</summary>
internal enum ElementKind
{
    Wire,
    Bus,
    BusEntry,
    Pin,
    Power,
    Port,
    SheetEntry,
    SignalHarness,
    HarnessConnector,
    HarnessEntry,
}

/// <summary>
/// One node in the connectivity graph: a primitive reduced to its connection points and (for
/// conductors) its segments, plus any intrinsic net name it carries. Each element is one union-find id.
/// </summary>
internal sealed class Element
{
    public Element(int id, ElementKind kind, object primitive, int sheetId)
    {
        Id = id;
        Kind = kind;
        Primitive = primitive;
        SheetId = sheetId;
    }

    /// <summary>Dense union-find id.</summary>
    public int Id { get; }

    public ElementKind Kind { get; }

    /// <summary>The source primitive (SchWire, SchPin, SchPowerObject, …) for traceability.</summary>
    public object Primitive { get; }

    /// <summary>Which sheet instance this element came from (used by the project-level merge).</summary>
    public int SheetId { get; }

    /// <summary>The element's connection points (wire/bus vertices, pin tip, power/port/entry points).</summary>
    public List<CoordPoint> Points { get; } = new();

    /// <summary>Conductor segments (wire/bus/bus-entry); empty for point elements.</summary>
    public List<(CoordPoint A, CoordPoint B)> Segments { get; } = new();

    /// <summary>An intrinsic net name carried by this element (power text, port/sheet-entry name, hidden-net name).</summary>
    public string? IntrinsicName { get; set; }

    /// <summary>The scope of <see cref="IntrinsicName"/>.</summary>
    public NetScope IntrinsicScope { get; set; }

    // ---- pin metadata (Kind == Pin) ----
    public string? ComponentDesignator { get; set; }
    public string? PinDesignator { get; set; }
    public NetPin? NetPin { get; set; }

    /// <summary>
    /// Whether the element is a plain wire conductor that connects things along its length. Buses and
    /// bus entries are deliberately excluded: a bus is a visual bundle, and its members connect by their
    /// individual net labels — treating the bus as a conductor would short every member together.
    /// </summary>
    public bool IsConductor => Kind is ElementKind.Wire;

    /// <summary>Whether the element participates in geometric coincidence/T/junction merging.</summary>
    public bool ParticipatesInGeometry => Kind is not (ElementKind.Bus or ElementKind.BusEntry);
}
