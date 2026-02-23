using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic sheet entry (connection point on a sheet symbol).
/// </summary>
public sealed class SchSheetEntry : ISchSheetPin
{
    /// <summary>
    /// Which side of the sheet symbol this entry is on (0=Left, 1=Right, 2=Top, 3=Bottom).
    /// </summary>
    public int Side { get; set; }

    /// <summary>
    /// Distance from the top of the sheet symbol (in internal units).
    /// </summary>
    public Coord DistanceFromTop { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// I/O type (0=Unspecified, 1=Output, 2=Input, 3=Bidirectional).
    /// </summary>
    public int IoType { get; set; }

    /// <summary>
    /// Entry style.
    /// </summary>
    public int Style { get; set; }

    /// <summary>
    /// Arrow display kind.
    /// </summary>
    public int ArrowKind { get; set; }

    /// <summary>
    /// Harness type string.
    /// </summary>
    public string? HarnessType { get; set; }

    /// <summary>
    /// Harness color (RGB).
    /// </summary>
    public int HarnessColor { get; set; }

    /// <summary>
    /// Font ID reference.
    /// </summary>
    public int FontId { get; set; }

    /// <summary>
    /// Entry border/line color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Fill/area color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Text color (RGB).
    /// </summary>
    public int TextColor { get; set; }

    /// <summary>
    /// Text style index.
    /// </summary>
    public int TextStyle { get; set; }

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
    CoordPoint ISchSheetPin.Location => default;

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            // Sheet entries don't have independent bounds; they're positioned relative to their owner
            return new CoordRect(default, default);
        }
    }
}
