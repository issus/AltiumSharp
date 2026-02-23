namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic implementation link (record type 45).
/// Links a schematic symbol to a PCB footprint, simulation model, or signal integrity model.
/// </summary>
public sealed class SchImplementation : ISchImplementation
{
    /// <summary>
    /// Description of the implementation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Name of the model (e.g., footprint name).
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Type of the model (e.g., "PCBLIB", "SIM", "SI").
    /// </summary>
    public string? ModelType { get; set; }

    /// <summary>
    /// List of data file kinds associated with this implementation.
    /// Indexed 1-based as MODELDATAFILEKIND1, MODELDATAFILEKIND2, etc. in Altium format.
    /// </summary>
    public List<string> DataFileKinds { get; set; } = new();

    /// <summary>
    /// Whether this is the current (active) implementation.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Child map definers that map schematic pin designators to implementation pin names.
    /// </summary>
    public IReadOnlyList<ISchMapDefiner> MapDefiners => _mapDefiners;

    /// <inheritdoc />
    IReadOnlyList<string> ISchImplementation.DataFileKinds => DataFileKinds;
    private readonly List<SchMapDefiner> _mapDefiners = new();

    /// <summary>
    /// Adds a map definer to this implementation.
    /// </summary>
    internal void AddMapDefiner(SchMapDefiner mapDefiner) => _mapDefiners.Add(mapDefiner);

    // Standard ownership fields (used in SchDoc flat format)

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
