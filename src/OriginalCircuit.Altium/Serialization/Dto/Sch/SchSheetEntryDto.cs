using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic sheet entry record.
/// Record type 16 in Altium schematic files.
/// Sheet entries are the connection points on a sheet symbol.
/// </summary>
[AltiumRecord("16")]
internal sealed partial record SchSheetEntryDto
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
    /// Gets or sets which side of the sheet symbol the entry is on (0=Left, 1=Right, 2=Top, 3=Bottom).
    /// </summary>
    [AltiumParameter("SIDE")]
    public int Side { get; init; }

    /// <summary>
    /// Gets or sets the distance from the top of the sheet symbol side in internal units.
    /// </summary>
    [AltiumParameter("DISTANCEFROMTOP")]
    [AltiumCoord]
    public int DistanceFromTop { get; init; }

    /// <summary>
    /// Gets or sets the sheet entry name.
    /// </summary>
    [AltiumParameter("NAME")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the sheet entry I/O type (0=Unspecified, 1=Output, 2=Input, 3=Bidirectional).
    /// </summary>
    [AltiumParameter("IOTYPE")]
    public int IoType { get; init; }

    /// <summary>
    /// Gets or sets the sheet entry style.
    /// </summary>
    [AltiumParameter("STYLE")]
    public int Style { get; init; }

    /// <summary>
    /// Gets or sets the arrow kind displayed on the entry.
    /// </summary>
    [AltiumParameter("ARROWKIND")]
    public int ArrowKind { get; init; }

    /// <summary>
    /// Gets or sets the harness type for harness entries.
    /// </summary>
    [AltiumParameter("HARNESSTYPE")]
    public string? HarnessType { get; init; }

    /// <summary>
    /// Gets or sets the font ID referencing a font definition in the document.
    /// </summary>
    [AltiumParameter("FONTID")]
    public int FontId { get; init; }

    /// <summary>
    /// Gets or sets the entry border color as a Win32 color value.
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
    /// Gets or sets the text style flags.
    /// </summary>
    [AltiumParameter("TEXTSTYLE")]
    public int TextStyle { get; init; }

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
    /// Gets or sets the unique identifier for this sheet entry.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Gets or sets the harness color as a Win32 color value.
    /// </summary>
    [AltiumParameter("HARNESSCOLOR")]
    public int HarnessColor { get; init; }
}
