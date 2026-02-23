using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic sheet symbol (reference to a sub-sheet in hierarchical designs).
/// </summary>
public sealed class SchSheetSymbol : ISchSheetSymbol
{
    private readonly List<SchSheetEntry> _entries = new();

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Width of the sheet symbol.
    /// </summary>
    public Coord XSize { get; set; }

    /// <summary>
    /// Height of the sheet symbol.
    /// </summary>
    public Coord YSize { get; set; }

    /// <summary>
    /// Whether the symbol is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Referenced sheet file name.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Display name of the sheet.
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Line width index (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Border color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Fill color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Whether the symbol is filled.
    /// </summary>
    public bool IsSolid { get; set; }

    /// <summary>
    /// Whether hidden fields are shown.
    /// </summary>
    public bool ShowHiddenFields { get; set; }

    /// <summary>
    /// Symbol type identifier.
    /// </summary>
    public int SymbolType { get; set; }

    /// <summary>
    /// Design item ID for managed components.
    /// </summary>
    public string? DesignItemId { get; set; }

    /// <summary>
    /// Item GUID for vault/managed library reference.
    /// </summary>
    public string? ItemGuid { get; set; }

    /// <summary>
    /// Library identifier kind.
    /// </summary>
    public int LibIdentifierKind { get; set; }

    /// <summary>
    /// Library identifier string.
    /// </summary>
    public string? LibraryIdentifier { get; set; }

    /// <summary>
    /// Revision GUID for vault reference.
    /// </summary>
    public string? RevisionGuid { get; set; }

    /// <summary>
    /// Source library name.
    /// </summary>
    public string? SourceLibraryName { get; set; }

    /// <summary>
    /// Vault GUID.
    /// </summary>
    public string? VaultGuid { get; set; }

    /// <summary>
    /// Sheet entries (connection points on this symbol).
    /// </summary>
    public IReadOnlyList<SchSheetEntry> Entries => _entries;

    /// <summary>
    /// Adds a sheet entry to this symbol.
    /// </summary>
    public void AddEntry(SchSheetEntry entry) => _entries.Add(entry);

    /// <summary>
    /// Index of the owning record in the schematic hierarchy.
    /// </summary>
    public int OwnerIndex { get; set; }

    /// <summary>
    /// Whether this primitive is not accessible for selection.
    /// </summary>
    public bool IsNotAccessible { get; set; }

    /// <summary>
    /// Index of this primitive within its parent sheet.
    /// </summary>
    public int IndexInSheet { get; set; }

    /// <summary>
    /// Part ID of the owning component (for multi-part components).
    /// </summary>
    public int OwnerPartId { get; set; }

    /// <summary>
    /// Display mode of the owning part.
    /// </summary>
    public int OwnerPartDisplayMode { get; set; }

    /// <summary>
    /// Whether this primitive is graphically locked.
    /// </summary>
    public bool GraphicallyLocked { get; set; }

    /// <summary>
    /// Whether this primitive is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether this primitive is dimmed in display.
    /// </summary>
    public bool Dimmed { get; set; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Location, new CoordPoint(Location.X + XSize, Location.Y + YSize));
}
