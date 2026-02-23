using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic bezier curve record.
/// Record type 5 in Altium schematic files.
/// </summary>
[AltiumRecord("5")]
internal sealed partial record SchBezierDto
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
    /// Gets or sets the line color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets the line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    [AltiumParameter("LINEWIDTH")]
    public int LineWidth { get; init; }

    /// <summary>
    /// Gets or sets the area (fill) color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

    /// <summary>
    /// Gets or sets the number of control points in the bezier curve.
    /// </summary>
    [AltiumParameter("LOCATIONCOUNT")]
    public int LocationCount { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the first control point.
    /// </summary>
    [AltiumParameter("X1")]
    [AltiumCoord]
    public int X1 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the first control point X coordinate.
    /// </summary>
    [AltiumParameter("X1_FRAC")]
    public int X1Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the first control point.
    /// </summary>
    [AltiumParameter("Y1")]
    [AltiumCoord]
    public int Y1 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the first control point Y coordinate.
    /// </summary>
    [AltiumParameter("Y1_FRAC")]
    public int Y1Frac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the second control point.
    /// </summary>
    [AltiumParameter("X2")]
    [AltiumCoord]
    public int X2 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the second control point X coordinate.
    /// </summary>
    [AltiumParameter("X2_FRAC")]
    public int X2Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the second control point.
    /// </summary>
    [AltiumParameter("Y2")]
    [AltiumCoord]
    public int Y2 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the second control point Y coordinate.
    /// </summary>
    [AltiumParameter("Y2_FRAC")]
    public int Y2Frac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the third control point.
    /// </summary>
    [AltiumParameter("X3")]
    [AltiumCoord]
    public int X3 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the third control point X coordinate.
    /// </summary>
    [AltiumParameter("X3_FRAC")]
    public int X3Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the third control point.
    /// </summary>
    [AltiumParameter("Y3")]
    [AltiumCoord]
    public int Y3 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the third control point Y coordinate.
    /// </summary>
    [AltiumParameter("Y3_FRAC")]
    public int Y3Frac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the fourth control point.
    /// </summary>
    [AltiumParameter("X4")]
    [AltiumCoord]
    public int X4 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the fourth control point X coordinate.
    /// </summary>
    [AltiumParameter("X4_FRAC")]
    public int X4Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the fourth control point.
    /// </summary>
    [AltiumParameter("Y4")]
    [AltiumCoord]
    public int Y4 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the fourth control point Y coordinate.
    /// </summary>
    [AltiumParameter("Y4_FRAC")]
    public int Y4Frac { get; init; }

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
    /// Gets or sets the unique identifier for this bezier curve.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }
}
