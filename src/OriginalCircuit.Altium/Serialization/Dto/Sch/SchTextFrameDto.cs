using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic text frame record.
/// Record type 28 in Altium schematic files.
/// </summary>
[AltiumRecord("28")]
internal sealed partial record SchTextFrameDto
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
    /// Gets or sets the X coordinate of the first corner (Location).
    /// </summary>
    [AltiumParameter("LOCATION.X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the first corner X coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.X_FRAC")]
    public int LocationXFrac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the first corner (Location).
    /// </summary>
    [AltiumParameter("LOCATION.Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the first corner Y coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.Y_FRAC")]
    public int LocationYFrac { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the second corner (Corner).
    /// </summary>
    [AltiumParameter("CORNER.X")]
    [AltiumCoord]
    public int CornerX { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the second corner X coordinate.
    /// </summary>
    [AltiumParameter("CORNER.X_FRAC")]
    public int CornerXFrac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the second corner (Corner).
    /// </summary>
    [AltiumParameter("CORNER.Y")]
    [AltiumCoord]
    public int CornerY { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the second corner Y coordinate.
    /// </summary>
    [AltiumParameter("CORNER.Y_FRAC")]
    public int CornerYFrac { get; init; }

    /// <summary>
    /// Gets or sets the border color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets the area (fill) color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

    /// <summary>
    /// Gets or sets the text color as a Win32 color value.
    /// </summary>
    [AltiumParameter("TEXTCOLOR")]
    public int TextColor { get; init; }

    /// <summary>
    /// Gets or sets the text margin in internal units.
    /// </summary>
    [AltiumParameter("TEXTMARGIN")]
    [AltiumCoord]
    public int TextMargin { get; init; }

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
    /// Gets or sets whether the fill is transparent.
    /// </summary>
    [AltiumParameter("TRANSPARENT")]
    public bool Transparent { get; init; }

    /// <summary>
    /// Gets or sets the text content of the text frame.
    /// </summary>
    [AltiumParameter("TEXT")]
    public string? Text { get; init; }

    /// <summary>
    /// Gets or sets the font ID referencing a font definition in the document.
    /// </summary>
    [AltiumParameter("FONTID")]
    public int FontId { get; init; }

    /// <summary>
    /// Gets or sets the text orientation (0=None, 1=Rotated, 2=Flipped, 3=Both).
    /// </summary>
    [AltiumParameter("ORIENTATION")]
    public int Orientation { get; init; }

    /// <summary>
    /// Gets or sets the text alignment within the frame.
    /// </summary>
    [AltiumParameter("ALIGNMENT")]
    public int Alignment { get; init; }

    /// <summary>
    /// Gets or sets whether the text frame is filled.
    /// </summary>
    [AltiumParameter("ISSOLID")]
    public bool IsSolid { get; init; }

    /// <summary>
    /// Gets or sets whether to show the border around the text frame.
    /// </summary>
    [AltiumParameter("SHOWBORDER")]
    public bool ShowBorder { get; init; }

    /// <summary>
    /// Gets or sets whether text should wrap within the frame.
    /// </summary>
    [AltiumParameter("WORDWRAP")]
    public bool WordWrap { get; init; }

    /// <summary>
    /// Gets or sets whether to clip text to the rectangle bounds.
    /// </summary>
    [AltiumParameter("CLIPTORECT")]
    public bool ClipToRect { get; init; }

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
    /// Gets or sets the unique identifier for this text frame.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }
}
