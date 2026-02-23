using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic No-ERC (No Electrical Rule Check) marker.
/// Suppresses ERC violations at a specific location.
/// </summary>
public sealed class SchNoErc : ISchNoConnect
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Marker orientation (0=0, 1=90, 2=180, 3=270 degrees).
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    /// Marker color as packed BGR integer.
    /// </summary>
    public int Color { get; set; }

    /// <inheritdoc />
    EdaColor ISchNoConnect.Color => AltiumColorHelper.BgrToEdaColor(Color);

    /// <summary>
    /// Whether the marker is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Symbol style (0=small cross, 1=large cross).
    /// </summary>
    public int Symbol { get; set; }

    /// <summary>
    /// Area/background color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

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
            var size = Coord.FromMils(20);
            return new CoordRect(
                new CoordPoint(Location.X - size, Location.Y - size),
                new CoordPoint(Location.X + size, Location.Y + size));
        }
    }
}
