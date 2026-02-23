using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic port (sheet-level connection point).
/// Ports connect nets between sheets in a hierarchical design.
/// </summary>
public sealed class SchPort : ISchPort
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// I/O type (0=Unspecified, 1=Output, 2=Input, 3=Bidirectional).
    /// </summary>
    public int IoType { get; set; }

    /// <summary>
    /// Port style (visual appearance).
    /// </summary>
    public int Style { get; set; }

    /// <summary>
    /// Text alignment within the port.
    /// </summary>
    public int Alignment { get; set; }

    /// <summary>
    /// Port width in internal units.
    /// </summary>
    public Coord Width { get; set; }

    /// <summary>
    /// Port height in internal units.
    /// </summary>
    public Coord Height { get; set; }

    /// <summary>
    /// Border width index (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int BorderWidth { get; set; }

    /// <summary>
    /// Whether the port auto-sizes to fit text.
    /// </summary>
    public bool AutoSize { get; set; }

    /// <summary>
    /// Which end is connected (0=None, 1=Left, 2=Right).
    /// </summary>
    public int ConnectedEnd { get; set; }

    /// <summary>
    /// Cross-reference string.
    /// </summary>
    public string? CrossReference { get; set; }

    /// <summary>
    /// Whether to show the net name.
    /// </summary>
    public bool ShowNetName { get; set; }

    /// <summary>
    /// Harness type string.
    /// </summary>
    public string? HarnessType { get; set; }

    /// <summary>
    /// Harness color (RGB).
    /// </summary>
    public int HarnessColor { get; set; }

    /// <summary>
    /// Whether the port uses a custom style.
    /// </summary>
    public bool IsCustomStyle { get; set; }

    /// <summary>
    /// Font ID reference.
    /// </summary>
    public int FontId { get; set; }

    /// <summary>
    /// Port border/line color (RGB).
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
    public CoordRect Bounds
    {
        get
        {
            return new CoordRect(
                Location,
                new CoordPoint(Location.X + Width, Location.Y + Height));
        }
    }
}
