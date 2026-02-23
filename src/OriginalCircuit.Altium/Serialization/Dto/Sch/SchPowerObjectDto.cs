using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic power object (power port) record.
/// Record type 17 in Altium schematic files.
/// </summary>
[AltiumRecord("17")]
internal sealed partial record SchPowerObjectDto
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
    /// Gets or sets the X coordinate of the power object location.
    /// </summary>
    [AltiumParameter("LOCATION.X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the X coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.X_FRAC")]
    public int LocationXFrac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the power object location.
    /// </summary>
    [AltiumParameter("LOCATION.Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the Y coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.Y_FRAC")]
    public int LocationYFrac { get; init; }

    /// <summary>
    /// Gets or sets the power object color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets the power net name text.
    /// </summary>
    [AltiumParameter("TEXT")]
    public string? Text { get; init; }

    /// <summary>
    /// Gets or sets the power object style (e.g., Arrow, Bar, Wave, etc.).
    /// </summary>
    [AltiumParameter("STYLE")]
    public int Style { get; init; }

    /// <summary>
    /// Gets or sets the power object orientation (0=None, 1=Rotated, 2=Flipped, 3=Both).
    /// </summary>
    [AltiumParameter("ORIENTATION")]
    public int Orientation { get; init; }

    /// <summary>
    /// Gets or sets whether to show the net name on the power object.
    /// </summary>
    [AltiumParameter("SHOWNETNAME")]
    public bool ShowNetName { get; init; }

    /// <summary>
    /// Gets or sets whether this is a cross-sheet connector.
    /// </summary>
    [AltiumParameter("ISCROSSSHEETCONNECTOR")]
    public bool IsCrossSheetConnector { get; init; }

    /// <summary>
    /// Gets or sets the font ID referencing a font definition in the document.
    /// </summary>
    [AltiumParameter("FONTID")]
    public int FontId { get; init; }

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
    /// Gets or sets the unique identifier for this power object.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Gets or sets the area (fill) color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

    /// <summary>
    /// Gets or sets whether the power object uses a custom style.
    /// </summary>
    [AltiumParameter("ISCUSTOMSTYLE")]
    public bool IsCustomStyle { get; init; }

    /// <summary>
    /// Gets or sets whether the power object is mirrored.
    /// </summary>
    [AltiumParameter("ISMIRRORED")]
    public bool IsMirrored { get; init; }

    /// <summary>
    /// Gets or sets the text justification.
    /// 0=BottomLeft, 1=BottomCenter, 2=BottomRight, 3=MiddleLeft, 4=MiddleCenter,
    /// 5=MiddleRight, 6=TopLeft, 7=TopCenter, 8=TopRight.
    /// </summary>
    [AltiumParameter("JUSTIFICATION")]
    public int Justification { get; init; }
}
