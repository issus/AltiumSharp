namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic implementation link (record type 45).
/// Links a schematic symbol to a PCB footprint, simulation model, or signal integrity model.
/// </summary>
public interface ISchImplementation
{
    /// <summary>
    /// Description of the implementation.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Name of the model (e.g., footprint name).
    /// </summary>
    string? ModelName { get; }

    /// <summary>
    /// Type of the model (e.g., "PCBLIB", "SIM", "SI").
    /// </summary>
    string? ModelType { get; }

    /// <summary>
    /// List of data file kinds associated with this implementation.
    /// </summary>
    IReadOnlyList<string> DataFileKinds { get; }

    /// <summary>
    /// Whether this is the current (active) implementation.
    /// </summary>
    bool IsCurrent { get; }

    /// <summary>
    /// Child map definers that map schematic pin designators to implementation pin names.
    /// </summary>
    IReadOnlyList<ISchMapDefiner> MapDefiners { get; }
}

/// <summary>
/// Represents a schematic map definer (record type 47).
/// Maps a schematic pin designator to one or more implementation (footprint) pin names.
/// </summary>
public interface ISchMapDefiner
{
    /// <summary>
    /// The schematic-side designator interface name.
    /// </summary>
    string? DesignatorInterface { get; }

    /// <summary>
    /// List of implementation-side designator names.
    /// </summary>
    IReadOnlyList<string> DesignatorImplementations { get; }

    /// <summary>
    /// Whether this is a trivial (1:1) mapping.
    /// </summary>
    bool IsTrivial { get; }
}
