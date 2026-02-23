using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic wire segment record.
/// Record type 27 in Altium schematic files.
/// Wire segments connect pins and form electrical nets.
/// </summary>
[AltiumRecord("27")]
internal sealed partial record SchWireDto
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
    /// Gets or sets the wire color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets the line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    [AltiumParameter("LINEWIDTH")]
    public int LineWidth { get; init; }

    /// <summary>
    /// Gets or sets the line style (0=Solid, 1=Dashed, 2=Dotted).
    /// </summary>
    [AltiumParameter("LINESTYLE")]
    public int LineStyle { get; init; }

    /// <summary>
    /// Gets or sets the number of location points in the wire segment.
    /// </summary>
    [AltiumParameter("LOCATIONCOUNT")]
    public int LocationCount { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the first point.
    /// </summary>
    [AltiumParameter("X1")]
    [AltiumCoord]
    public int X1 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the first X coordinate.
    /// </summary>
    [AltiumParameter("X1_FRAC")]
    public int X1Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the first point.
    /// </summary>
    [AltiumParameter("Y1")]
    [AltiumCoord]
    public int Y1 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the first Y coordinate.
    /// </summary>
    [AltiumParameter("Y1_FRAC")]
    public int Y1Frac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the second point.
    /// </summary>
    [AltiumParameter("X2")]
    [AltiumCoord]
    public int X2 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the second X coordinate.
    /// </summary>
    [AltiumParameter("X2_FRAC")]
    public int X2Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the second point.
    /// </summary>
    [AltiumParameter("Y2")]
    [AltiumCoord]
    public int Y2 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the second Y coordinate.
    /// </summary>
    [AltiumParameter("Y2_FRAC")]
    public int Y2Frac { get; init; }

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
    /// Gets or sets the unique identifier for this wire.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Gets or sets the area (fill) color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

    /// <summary>
    /// Gets or sets whether the wire is filled (solid).
    /// </summary>
    [AltiumParameter("ISSOLID")]
    public bool IsSolid { get; init; }

    /// <summary>
    /// Gets or sets whether the wire fill is transparent.
    /// </summary>
    [AltiumParameter("TRANSPARENT")]
    public bool Transparent { get; init; }

    /// <summary>
    /// Gets or sets whether this wire was automatically placed.
    /// </summary>
    [AltiumParameter("AUTOWIRE")]
    public bool AutoWire { get; init; }

    /// <summary>
    /// Gets or sets the underline color as a Win32 color value.
    /// </summary>
    [AltiumParameter("UNDERLINECOLOR")]
    public int UnderlineColor { get; init; }
}
