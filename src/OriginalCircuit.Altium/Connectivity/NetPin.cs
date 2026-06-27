using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Connectivity;

/// <summary>
/// One component pin participating in connectivity: the physical terminal identified by its component
/// reference designator and pin designator, together with its electrical type and absolute location.
/// </summary>
public sealed class NetPin : IEquatable<NetPin>
{
    internal NetPin(
        string componentDesignator,
        string pinDesignator,
        string? pinName,
        Models.Sch.PinElectricalType electricalType,
        CoordPoint location,
        int ownerPartId,
        bool isHidden,
        SchPin pin,
        SchComponent component,
        int sheetInstanceId = 0)
    {
        ComponentDesignator = componentDesignator;
        PinDesignator = pinDesignator;
        PinName = pinName;
        ElectricalType = electricalType;
        Location = location;
        OwnerPartId = ownerPartId;
        IsHidden = isHidden;
        Pin = pin;
        Component = component;
        SheetInstanceId = sheetInstanceId;
    }

    /// <summary>The owning component's reference designator (e.g. <c>"U1"</c>).</summary>
    public string ComponentDesignator { get; }

    /// <summary>The pin designator / number within the component (e.g. <c>"3"</c>, <c>"A7"</c>).</summary>
    public string PinDesignator { get; }

    /// <summary>The pin name / function (e.g. <c>"VCC"</c>, <c>"GPIO0"</c>), when present.</summary>
    public string? PinName { get; }

    /// <summary>The pin's electrical type (input, output, power, passive, …).</summary>
    public Models.Sch.PinElectricalType ElectricalType { get; }

    /// <summary>The pin's absolute electrical connection point (the pin tip), in sheet coordinates.</summary>
    public CoordPoint Location { get; }

    /// <summary>The part id (unit) this pin belongs to within a multi-part component (1-based).</summary>
    public int OwnerPartId { get; }

    /// <summary>
    /// The sheet-instance id this pin belongs to. For a multi-channel design the same component
    /// designator appears on several channel instances; this distinguishes them. 0 for a single sheet.
    /// </summary>
    public int SheetInstanceId { get; }

    /// <summary>Whether the pin is hidden (e.g. an implicit power pin).</summary>
    public bool IsHidden { get; }

    /// <summary>The underlying schematic pin primitive, for traceability.</summary>
    public SchPin Pin { get; }

    /// <summary>The owning schematic component, for traceability.</summary>
    public SchComponent Component { get; }

    /// <summary>
    /// A stable identifier of the form <c>Designator.Pin</c> (e.g. <c>"U1.3"</c>). For multi-part
    /// components this is the package-level key (the part letter is not included).
    /// </summary>
    public string Key => $"{ComponentDesignator}.{PinDesignator}";

    /// <inheritdoc />
    public override string ToString() => Key;

    /// <inheritdoc />
    public bool Equals(NetPin? other) =>
        other is not null
        && ReferenceEquals(Pin, other.Pin)
        && string.Equals(ComponentDesignator, other.ComponentDesignator, StringComparison.OrdinalIgnoreCase)
        && string.Equals(PinDesignator, other.PinDesignator, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as NetPin);

    /// <inheritdoc />
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Pin);
}
