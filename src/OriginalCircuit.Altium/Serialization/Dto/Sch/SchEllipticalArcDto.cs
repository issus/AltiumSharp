using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic elliptical arc record.
/// Record type 11 in Altium schematic files.
/// </summary>
[AltiumRecord("11")]
internal sealed partial record SchEllipticalArcDto
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
    /// Gets or sets the X coordinate of the elliptical arc center.
    /// </summary>
    [AltiumParameter("LOCATION.X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the center X coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.X_FRAC")]
    public int LocationXFrac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the elliptical arc center.
    /// </summary>
    [AltiumParameter("LOCATION.Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the center Y coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.Y_FRAC")]
    public int LocationYFrac { get; init; }

    /// <summary>
    /// Gets or sets the primary radius in internal units.
    /// </summary>
    [AltiumParameter("RADIUS")]
    [AltiumCoord]
    public int Radius { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the primary radius.
    /// </summary>
    [AltiumParameter("RADIUS_FRAC")]
    public int RadiusFrac { get; init; }

    /// <summary>
    /// Gets or sets the secondary radius in internal units (for ellipses).
    /// </summary>
    [AltiumParameter("SECONDARYRADIUS")]
    [AltiumCoord]
    public int SecondaryRadius { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the secondary radius.
    /// </summary>
    [AltiumParameter("SECONDARYRADIUS_FRAC")]
    public int SecondaryRadiusFrac { get; init; }

    /// <summary>
    /// Gets or sets the starting angle in degrees (0-360).
    /// </summary>
    [AltiumParameter("STARTANGLE")]
    public double StartAngle { get; init; }

    /// <summary>
    /// Gets or sets the ending angle in degrees (0-360).
    /// </summary>
    [AltiumParameter("ENDANGLE")]
    public double EndAngle { get; init; }

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
    /// Gets or sets the fractional part of the line width.
    /// </summary>
    [AltiumParameter("LINEWIDTH_FRAC")]
    public int LineWidthFrac { get; init; }

    /// <summary>
    /// Gets or sets the area (fill) color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

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
    /// Gets or sets the unique identifier for this elliptical arc.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }
}
