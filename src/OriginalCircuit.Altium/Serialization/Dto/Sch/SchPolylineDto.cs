using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic polyline record.
/// Record type 6 in Altium schematic files.
/// </summary>
[AltiumRecord("6")]
internal sealed partial record SchPolylineDto
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
    /// Gets or sets the line style (0=Solid, 1=Dashed, 2=Dotted).
    /// </summary>
    [AltiumParameter("LINESTYLE")]
    public int LineStyle { get; init; }

    /// <summary>
    /// Gets or sets the shape displayed at the end of the polyline.
    /// </summary>
    [AltiumParameter("ENDLINESHAPE")]
    public int EndLineShape { get; init; }

    /// <summary>
    /// Gets or sets the shape displayed at the start of the polyline.
    /// </summary>
    [AltiumParameter("STARTLINESHAPE")]
    public int StartLineShape { get; init; }

    /// <summary>
    /// Gets or sets the size of the line end shapes.
    /// </summary>
    [AltiumParameter("LINESHAPESIZE")]
    public int LineShapeSize { get; init; }

    /// <summary>
    /// Gets or sets whether the polyline fill is transparent.
    /// </summary>
    [AltiumParameter("TRANSPARENT")]
    public bool Transparent { get; init; }

    /// <summary>
    /// Gets or sets the number of vertices in the polyline.
    /// Vertex coordinates are stored as X1, Y1, X2, Y2, etc.
    /// </summary>
    [AltiumParameter("LOCATIONCOUNT")]
    public int LocationCount { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the first vertex.
    /// </summary>
    [AltiumParameter("X1")]
    [AltiumCoord]
    public int X1 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the first vertex X coordinate.
    /// </summary>
    [AltiumParameter("X1_FRAC")]
    public int X1Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the first vertex.
    /// </summary>
    [AltiumParameter("Y1")]
    [AltiumCoord]
    public int Y1 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the first vertex Y coordinate.
    /// </summary>
    [AltiumParameter("Y1_FRAC")]
    public int Y1Frac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the second vertex.
    /// </summary>
    [AltiumParameter("X2")]
    [AltiumCoord]
    public int X2 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the second vertex X coordinate.
    /// </summary>
    [AltiumParameter("X2_FRAC")]
    public int X2Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the second vertex.
    /// </summary>
    [AltiumParameter("Y2")]
    [AltiumCoord]
    public int Y2 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the second vertex Y coordinate.
    /// </summary>
    [AltiumParameter("Y2_FRAC")]
    public int Y2Frac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the third vertex.
    /// </summary>
    [AltiumParameter("X3")]
    [AltiumCoord]
    public int X3 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the third vertex X coordinate.
    /// </summary>
    [AltiumParameter("X3_FRAC")]
    public int X3Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the third vertex.
    /// </summary>
    [AltiumParameter("Y3")]
    [AltiumCoord]
    public int Y3 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the third vertex Y coordinate.
    /// </summary>
    [AltiumParameter("Y3_FRAC")]
    public int Y3Frac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the fourth vertex.
    /// </summary>
    [AltiumParameter("X4")]
    [AltiumCoord]
    public int X4 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the fourth vertex X coordinate.
    /// </summary>
    [AltiumParameter("X4_FRAC")]
    public int X4Frac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the fourth vertex.
    /// </summary>
    [AltiumParameter("Y4")]
    [AltiumCoord]
    public int Y4 { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the fourth vertex Y coordinate.
    /// </summary>
    [AltiumParameter("Y4_FRAC")]
    public int Y4Frac { get; init; }

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
    /// Gets or sets the unique identifier for this polyline.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Gets or sets whether the polyline is filled.
    /// </summary>
    [AltiumParameter("ISSOLID")]
    public bool IsSolid { get; init; }
}
