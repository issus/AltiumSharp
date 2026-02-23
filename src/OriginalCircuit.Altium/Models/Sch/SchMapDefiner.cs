namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic map definer (record type 47).
/// Maps a schematic pin designator to one or more implementation (footprint) pin names.
/// </summary>
public sealed class SchMapDefiner : ISchMapDefiner
{
    /// <summary>
    /// The schematic-side designator interface name.
    /// </summary>
    public string? DesignatorInterface { get; set; }

    /// <summary>
    /// List of implementation-side designator names.
    /// Indexed 0-based as DESIMP0, DESIMP1, etc. in Altium format.
    /// </summary>
    public List<string> DesignatorImplementations { get; set; } = new();

    /// <summary>
    /// Whether this is a trivial (1:1) mapping.
    /// </summary>
    /// <inheritdoc />
    IReadOnlyList<string> ISchMapDefiner.DesignatorImplementations => DesignatorImplementations;

    /// <summary>
    /// Whether this is a trivial (1:1) mapping.
    /// </summary>
    public bool IsTrivial { get; set; }

    // Standard ownership fields

    /// <summary>
    /// Index of the owning record in the schematic hierarchy.
    /// </summary>
    public int OwnerIndex { get; set; }

    /// <summary>
    /// Whether this primitive is not accessible for selection.
    /// </summary>
    public bool IsNotAccessible { get; set; }

    /// <summary>
    /// Index of this primitive within its parent sheet.
    /// </summary>
    public int IndexInSheet { get; set; }

    /// <summary>
    /// Part ID of the owning component.
    /// </summary>
    public int OwnerPartId { get; set; }

    /// <summary>
    /// Display mode of the owning part.
    /// </summary>
    public int OwnerPartDisplayMode { get; set; }

    /// <summary>
    /// Whether this primitive is graphically locked.
    /// </summary>
    public bool GraphicallyLocked { get; set; }

    /// <summary>
    /// Whether this primitive is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether this primitive is dimmed.
    /// </summary>
    public bool Dimmed { get; set; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    public string? UniqueId { get; set; }
}
