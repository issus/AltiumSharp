using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Pcb;

/// <summary>
/// Data Transfer Object for PCB component/footprint records.
/// Represents a footprint pattern that can contain pads, tracks, arcs, and other primitives.
/// </summary>
[AltiumRecord("Component")]
internal sealed partial record PcbComponentDto
{
    /// <summary>
    /// The pattern name (footprint name) of the component.
    /// </summary>
    [AltiumParameter("PATTERN")]
    public string? Pattern { get; init; }

    /// <summary>
    /// Description of the component footprint.
    /// </summary>
    [AltiumParameter("DESCRIPTION")]
    public string? Description { get; init; }

    /// <summary>
    /// Height of the component in internal coordinate units.
    /// </summary>
    [AltiumParameter("HEIGHT")]
    [AltiumCoord]
    public int Height { get; init; }

    /// <summary>
    /// Unique identifier for the component item.
    /// </summary>
    [AltiumParameter("ITEMGUID")]
    public string? ItemGuid { get; init; }

    /// <summary>
    /// Revision identifier for version tracking.
    /// </summary>
    [AltiumParameter("REVISIONGUID")]
    public string? RevisionGuid { get; init; }

    /// <summary>
    /// Source component library name.
    /// </summary>
    [AltiumParameter("SOURCECOMPONENTLIBRARY")]
    public string? SourceComponentLibrary { get; init; }

    /// <summary>
    /// Source library reference.
    /// </summary>
    [AltiumParameter("SOURCELIBRARYREFERENCE")]
    public string? SourceLibraryReference { get; init; }

    /// <summary>
    /// X coordinate of the component location.
    /// </summary>
    [AltiumParameter("X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Y coordinate of the component location.
    /// </summary>
    [AltiumParameter("Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    [AltiumParameter("ROTATION")]
    public double Rotation { get; init; }

    /// <summary>
    /// Layer the component is placed on.
    /// </summary>
    [AltiumParameter("LAYER")]
    public int Layer { get; init; }

    /// <summary>
    /// Component designator (e.g., U1, R1, C1).
    /// </summary>
    [AltiumParameter("DESIGNATOR")]
    public string? Designator { get; init; }

    /// <summary>
    /// Comment/value text for the component.
    /// </summary>
    [AltiumParameter("COMMENT")]
    public string? Comment { get; init; }

    /// <summary>
    /// Whether the component is locked from editing.
    /// </summary>
    [AltiumParameter("LOCKED")]
    public bool IsLocked { get; init; }

    /// <summary>
    /// Whether the component is mirrored.
    /// </summary>
    [AltiumParameter("MIRRORED")]
    public bool IsMirrored { get; init; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }
}
