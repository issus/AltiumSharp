using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic bus entry record.
/// Record type 37 in Altium schematic files.
/// </summary>
[AltiumRecord("37")]
internal sealed partial record SchBusEntryDto
{
    /// <summary>
    /// Gets or sets the owner index linking this primitive to its parent.
    /// </summary>
    [AltiumParameter("OWNERINDEX")]
    public int OwnerIndex { get; init; }

    /// <summary>
    /// Gets or sets whether the primitive is not accessible.
    /// </summary>
    [AltiumParameter("ISNOTACCESIBLE")]
    public bool IsNotAccessible { get; init; }

    /// <summary>
    /// Gets or sets the index of this primitive in the sheet.
    /// </summary>
    [AltiumParameter("INDEXINSHEET")]
    public int IndexInSheet { get; init; }

    /// <summary>
    /// Gets or sets the owner part ID (for multi-part components).
    /// </summary>
    [AltiumParameter("OWNERPARTID")]
    public int OwnerPartId { get; init; }

    /// <summary>
    /// Gets or sets the owner part display mode.
    /// </summary>
    [AltiumParameter("OWNERPARTDISPLAYMODE")]
    public int OwnerPartDisplayMode { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the start point (Location).
    /// </summary>
    [AltiumParameter("LOCATION.X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the start X coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.X_FRAC")]
    public int LocationXFrac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the start point (Location).
    /// </summary>
    [AltiumParameter("LOCATION.Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the start Y coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.Y_FRAC")]
    public int LocationYFrac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the end point (Corner).
    /// </summary>
    [AltiumParameter("CORNER.X")]
    [AltiumCoord]
    public int CornerX { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the end X coordinate.
    /// </summary>
    [AltiumParameter("CORNER.X_FRAC")]
    public int CornerXFrac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the end point (Corner).
    /// </summary>
    [AltiumParameter("CORNER.Y")]
    [AltiumCoord]
    public int CornerY { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the end Y coordinate.
    /// </summary>
    [AltiumParameter("CORNER.Y_FRAC")]
    public int CornerYFrac { get; init; }

    /// <summary>
    /// Gets or sets the line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    [AltiumParameter("LINEWIDTH")]
    public int LineWidth { get; init; }

    /// <summary>
    /// Gets or sets the line color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets whether the primitive is graphically locked.
    /// </summary>
    [AltiumParameter("GRAPHICALLYLOCKED")]
    public bool GraphicallyLocked { get; init; }

    /// <summary>
    /// Gets or sets whether the primitive is disabled.
    /// </summary>
    [AltiumParameter("DISABLED")]
    public bool Disabled { get; init; }

    /// <summary>
    /// Gets or sets whether the primitive is dimmed in display.
    /// </summary>
    [AltiumParameter("DIMMED")]
    public bool Dimmed { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier for this bus entry.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }
}
