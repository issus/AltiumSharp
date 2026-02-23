using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic sheet symbol record.
/// Record type 15 in Altium schematic files.
/// </summary>
[AltiumRecord("15")]
internal sealed partial record SchSheetSymbolDto
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
    /// Gets or sets the X coordinate of the sheet symbol location.
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
    /// Gets or sets the Y coordinate of the sheet symbol location.
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
    /// Gets or sets the horizontal size in internal units.
    /// </summary>
    [AltiumParameter("XSIZE")]
    [AltiumCoord]
    public int XSize { get; init; }

    /// <summary>
    /// Gets or sets the vertical size in internal units.
    /// </summary>
    [AltiumParameter("YSIZE")]
    [AltiumCoord]
    public int YSize { get; init; }

    /// <summary>
    /// Gets or sets whether the sheet symbol is mirrored.
    /// </summary>
    [AltiumParameter("ISMIRRORED")]
    public bool IsMirrored { get; init; }

    /// <summary>
    /// Gets or sets the target schematic file name.
    /// </summary>
    [AltiumParameter("FILENAME")]
    public string? FileName { get; init; }

    /// <summary>
    /// Gets or sets the sheet name displayed on the symbol.
    /// </summary>
    [AltiumParameter("SHEETNAME")]
    public string? SheetName { get; init; }

    /// <summary>
    /// Gets or sets the line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    [AltiumParameter("LINEWIDTH")]
    public int LineWidth { get; init; }

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
    /// Gets or sets whether the sheet symbol is filled.
    /// </summary>
    [AltiumParameter("ISSOLID")]
    public bool IsSolid { get; init; }

    /// <summary>
    /// Gets or sets whether to show hidden fields on the sheet symbol.
    /// </summary>
    [AltiumParameter("SHOWHIDDENFIELDS")]
    public bool ShowHiddenFields { get; init; }

    /// <summary>
    /// Gets or sets the symbol type.
    /// </summary>
    [AltiumParameter("SYMBOLTYPE")]
    public int SymbolType { get; init; }

    /// <summary>
    /// Gets or sets the design item ID.
    /// </summary>
    [AltiumParameter("DESIGNITEMID")]
    public string? DesignItemId { get; init; }

    /// <summary>
    /// Gets or sets the item GUID for managed sheets.
    /// </summary>
    [AltiumParameter("ITEMGUID")]
    public string? ItemGuid { get; init; }

    /// <summary>
    /// Gets or sets the library identifier kind.
    /// </summary>
    [AltiumParameter("LIBIDENTIFIERKIND")]
    public int LibIdentifierKind { get; init; }

    /// <summary>
    /// Gets or sets the library identifier string.
    /// </summary>
    [AltiumParameter("LIBRARYIDENTIFIER")]
    public string? LibraryIdentifier { get; init; }

    /// <summary>
    /// Gets or sets the revision GUID for managed sheets.
    /// </summary>
    [AltiumParameter("REVISIONGUID")]
    public string? RevisionGuid { get; init; }

    /// <summary>
    /// Gets or sets the source library name.
    /// </summary>
    [AltiumParameter("SOURCELIBNAME")]
    public string? SourceLibraryName { get; init; }

    /// <summary>
    /// Gets or sets the vault GUID for managed sheets.
    /// </summary>
    [AltiumParameter("VAULTGUID")]
    public string? VaultGuid { get; init; }

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
    /// Gets or sets the unique identifier for this sheet symbol.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }
}
