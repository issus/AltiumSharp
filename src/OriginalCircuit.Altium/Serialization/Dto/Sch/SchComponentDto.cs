using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic component (symbol) record.
/// Record type 1 in Altium schematic files.
/// </summary>
[AltiumRecord("1")]
internal sealed partial record SchComponentDto
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
    /// Gets or sets the X coordinate of the component location.
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
    /// Gets or sets the Y coordinate of the component location.
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
    /// Gets or sets the library reference name for this component.
    /// </summary>
    [AltiumParameter("LIBREFERENCE")]
    public string? LibReference { get; init; }

    /// <summary>
    /// Gets or sets the component description.
    /// </summary>
    [AltiumParameter("COMPONENTDESCRIPTION")]
    public string? ComponentDescription { get; init; }

    /// <summary>
    /// Gets or sets the total number of parts in this component (stored as count + 1).
    /// </summary>
    [AltiumParameter("PARTCOUNT")]
    public int PartCount { get; init; }

    /// <summary>
    /// Gets or sets the number of display modes available.
    /// </summary>
    [AltiumParameter("DISPLAYMODECOUNT")]
    public int DisplayModeCount { get; init; }

    /// <summary>
    /// Gets or sets the current display mode.
    /// </summary>
    [AltiumParameter("DISPLAYMODE")]
    public int DisplayMode { get; init; }

    /// <summary>
    /// Gets or sets the component orientation (0=None, 1=Rotated, 2=Flipped, 3=Both).
    /// </summary>
    [AltiumParameter("ORIENTATION")]
    public int Orientation { get; init; }

    /// <summary>
    /// Gets or sets the current part ID being displayed.
    /// </summary>
    [AltiumParameter("CURRENTPARTID")]
    public int CurrentPartId { get; init; }

    /// <summary>
    /// Gets or sets whether hidden pins should be shown.
    /// </summary>
    [AltiumParameter("SHOWHIDDENPINS")]
    public bool ShowHiddenPins { get; init; }

    /// <summary>
    /// Gets or sets the library path.
    /// </summary>
    [AltiumParameter("LIBRARYPATH")]
    public string? LibraryPath { get; init; }

    /// <summary>
    /// Gets or sets the source library name.
    /// </summary>
    [AltiumParameter("SOURCELIBRARYNAME")]
    public string? SourceLibraryName { get; init; }

    /// <summary>
    /// Gets or sets the sheet part file name.
    /// </summary>
    [AltiumParameter("SHEETPARTFILENAME")]
    public string? SheetPartFileName { get; init; }

    /// <summary>
    /// Gets or sets the target file name.
    /// </summary>
    [AltiumParameter("TARGETFILENAME")]
    public string? TargetFileName { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier for this component.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Gets or sets the area color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

    /// <summary>
    /// Gets or sets the line color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets whether colors are overridden.
    /// </summary>
    [AltiumParameter("OVERIDECOLORS")]
    public bool OverrideColors { get; init; }

    /// <summary>
    /// Gets or sets the designator prefix (e.g., "R" for resistors, "C" for capacitors).
    /// </summary>
    [AltiumParameter("DESIGNATORPREFIX")]
    public string? DesignatorPrefix { get; init; }

    /// <summary>
    /// Gets or sets whether the designator is locked.
    /// </summary>
    [AltiumParameter("DESIGNATORLOCKED")]
    public bool DesignatorLocked { get; init; }

    /// <summary>
    /// Gets or sets whether the part ID is locked.
    /// </summary>
    [AltiumParameter("PARTIDLOCKED")]
    public bool PartIdLocked { get; init; }

    /// <summary>
    /// Gets or sets the design item ID (typically same as LibReference).
    /// </summary>
    [AltiumParameter("DESIGNITEMID")]
    public string? DesignItemId { get; init; }

    /// <summary>
    /// Gets or sets the component kind.
    /// </summary>
    [AltiumParameter("COMPONENTKIND")]
    public int ComponentKind { get; init; }

    /// <summary>
    /// Gets or sets the alias list for this component.
    /// </summary>
    [AltiumParameter("ALIASLIST")]
    public string? AliasList { get; init; }

    /// <summary>
    /// Gets or sets the total count of all pins in this component.
    /// </summary>
    [AltiumParameter("ALLPINCOUNT")]
    public int AllPinCount { get; init; }

    /// <summary>
    /// Gets or sets the symbol reference for this component.
    /// </summary>
    [AltiumParameter("SYMBOLREFERENCE")]
    public string SymbolReference { get; init; } = "";

    /// <summary>
    /// Gets or sets whether the component is graphically locked.
    /// </summary>
    [AltiumParameter("GRAPHICALLYLOCKED")]
    public bool GraphicallyLocked { get; init; }

    /// <summary>
    /// Gets or sets the database library name for DbLib components.
    /// </summary>
    [AltiumParameter("DATABASELIBRARYNAME")]
    public string? DatabaseLibraryName { get; init; }

    /// <summary>
    /// Gets or sets the database table name for DbLib components.
    /// </summary>
    [AltiumParameter("DATABASETABLENAME")]
    public string? DatabaseTableName { get; init; }

    /// <summary>
    /// Gets or sets the library identifier string.
    /// </summary>
    [AltiumParameter("LIBRARYIDENTIFIER")]
    public string? LibraryIdentifier { get; init; }

    /// <summary>
    /// Gets or sets the vault GUID for managed components.
    /// </summary>
    [AltiumParameter("VAULTGUID")]
    public string? VaultGuid { get; init; }

    /// <summary>
    /// Gets or sets the item GUID for managed components.
    /// </summary>
    [AltiumParameter("ITEMGUID")]
    public string? ItemGuid { get; init; }

    /// <summary>
    /// Gets or sets the revision GUID for managed components.
    /// </summary>
    [AltiumParameter("REVISIONGUID")]
    public string? RevisionGuid { get; init; }

    /// <summary>
    /// Gets or sets whether pins are moveable.
    /// </summary>
    [AltiumParameter("PINSMOVEABLE")]
    public bool PinsMoveable { get; init; }

    /// <summary>
    /// Gets or sets the pin color as a Win32 color value.
    /// </summary>
    [AltiumParameter("PINCOLOR")]
    public int PinColor { get; init; }

    /// <summary>
    /// Gets or sets whether hidden fields should be shown.
    /// </summary>
    [AltiumParameter("SHOWHIDDENFIELDS")]
    public bool ShowHiddenFields { get; init; }

    /// <summary>
    /// Gets or sets the unused database table name placeholder.
    /// </summary>
    [AltiumParameter("NOTUSEDBTABLENAME")]
    public string? NotUsedBTableName { get; init; }

    /// <summary>
    /// Gets or sets the configuration parameters string.
    /// </summary>
    [AltiumParameter("CONFIGURATIONPARAMETERS")]
    public string? ConfigurationParameters { get; init; }

    /// <summary>
    /// Gets or sets the configurator name.
    /// </summary>
    [AltiumParameter("CONFIGURATORNAME")]
    public string? ConfiguratorName { get; init; }

    /// <summary>
    /// Gets or sets whether this component is disabled.
    /// </summary>
    [AltiumParameter("DISABLED")]
    public bool Disabled { get; init; }

    /// <summary>
    /// Gets or sets whether this component is dimmed in display.
    /// </summary>
    [AltiumParameter("DIMMED")]
    public bool Dimmed { get; init; }

    /// <summary>
    /// Gets or sets whether to display field names.
    /// </summary>
    [AltiumParameter("DISPLAYFIELDNAMES")]
    public bool DisplayFieldNames { get; init; }

    /// <summary>
    /// Gets or sets whether the component is mirrored.
    /// </summary>
    [AltiumParameter("ISMIRRORED")]
    public bool IsMirrored { get; init; }

    /// <summary>
    /// Gets or sets whether this is an unmanaged component.
    /// </summary>
    [AltiumParameter("ISUNMANAGED")]
    public bool IsUnmanaged { get; init; }

    /// <summary>
    /// Gets or sets whether this component is user-configurable.
    /// </summary>
    [AltiumParameter("ISUSERCONFIGURABLE")]
    public bool IsUserConfigurable { get; init; }

    /// <summary>
    /// Gets or sets the library identifier kind.
    /// </summary>
    [AltiumParameter("LIBIDENTIFIERKIND")]
    public int LibIdentifierKind { get; init; }

    /// <summary>
    /// Gets or sets the display mode of the owning part.
    /// </summary>
    [AltiumParameter("OWNERPARTDISPLAYMODE")]
    public int OwnerPartDisplayMode { get; init; }

    /// <summary>
    /// Gets or sets the revision details for managed components.
    /// </summary>
    [AltiumParameter("REVISIONDETAILS")]
    public string? RevisionDetails { get; init; }

    /// <summary>
    /// Gets or sets the revision human-readable ID.
    /// </summary>
    [AltiumParameter("REVISIONHRID")]
    public string? RevisionHrid { get; init; }

    /// <summary>
    /// Gets or sets the revision state for managed components.
    /// </summary>
    [AltiumParameter("REVISIONSTATE")]
    public string? RevisionState { get; init; }

    /// <summary>
    /// Gets or sets the revision status for managed components.
    /// </summary>
    [AltiumParameter("REVISIONSTATUS")]
    public string? RevisionStatus { get; init; }

    /// <summary>
    /// Gets or sets the symbol item GUID for managed components.
    /// </summary>
    [AltiumParameter("SYMBOLITEMGUID")]
    public string? SymbolItemGuid { get; init; }

    /// <summary>
    /// Gets or sets the symbol items GUID for managed components.
    /// </summary>
    [AltiumParameter("SYMBOLITEMSGUID")]
    public string? SymbolItemsGuid { get; init; }

    /// <summary>
    /// Gets or sets the symbol revision GUID for managed components.
    /// </summary>
    [AltiumParameter("SYMBOLREVISIONGUID")]
    public string? SymbolRevisionGuid { get; init; }

    /// <summary>
    /// Gets or sets the symbol vault GUID for managed components.
    /// </summary>
    [AltiumParameter("SYMBOLVAULTGUID")]
    public string? SymbolVaultGuid { get; init; }

    /// <summary>
    /// Gets or sets whether to use the database table name.
    /// </summary>
    [AltiumParameter("USEDBTABLENAME")]
    public bool UseDbTableName { get; init; }

    /// <summary>
    /// Gets or sets whether to use the library name.
    /// </summary>
    [AltiumParameter("USELIBRARYNAME")]
    public bool UseLibraryName { get; init; }

    /// <summary>
    /// Gets or sets the variant option for this component.
    /// </summary>
    [AltiumParameter("VARIANTOPTION")]
    public int VariantOption { get; init; }

    /// <summary>
    /// Gets or sets the vault human-readable ID.
    /// </summary>
    [AltiumParameter("VAULTHRID")]
    public string? VaultHrid { get; init; }

    /// <summary>
    /// Gets or sets the generic component template GUID.
    /// </summary>
    [AltiumParameter("GENERICCOMPONENTTEMPLATEGUID")]
    public string? GenericComponentTemplateGuid { get; init; }
}
