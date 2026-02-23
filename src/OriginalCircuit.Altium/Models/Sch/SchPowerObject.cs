using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Power port symbol styles.
/// </summary>
public enum PowerPortStyle
{
    Circle = 0,
    Arrow = 1,
    Bar = 2,
    Wave = 3,
    PowerGround = 4,
    SignalGround = 5,
    Earth = 6,
    GostArrow = 7,
    GostPowerGround = 8,
    GostEarth = 9,
    GostBar = 10
}

/// <summary>
/// Represents a schematic power port/object (GND, VCC, etc.).
/// </summary>
public sealed class SchPowerObject : ISchPowerObject
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Net name displayed by the power port.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Power port style/symbol.
    /// </summary>
    public PowerPortStyle Style { get; set; }

    /// <summary>
    /// Orientation in degrees (0, 90, 180, 270).
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Whether to show the net name.
    /// </summary>
    public bool ShowNetName { get; set; } = true;

    /// <summary>
    /// Whether this is a cross-sheet connector.
    /// </summary>
    public bool IsCrossSheetConnector { get; set; }

    /// <summary>
    /// Text color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Font ID for the net name text.
    /// </summary>
    public int FontId { get; set; } = 1;

    /// <summary>
    /// Area/background color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Whether this uses a custom style.
    /// </summary>
    public bool IsCustomStyle { get; set; }

    /// <summary>
    /// Whether the symbol is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Text justification.
    /// </summary>
    public int Justification { get; set; }

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
            // Power objects have a fixed size symbol
            var size = Coord.FromMils(20);
            return new CoordRect(
                new CoordPoint(Location.X - size, Location.Y - size),
                new CoordPoint(Location.X + size, Location.Y + size));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new power object.
    /// </summary>
    public static PowerObjectBuilder Create(string netName) => new(netName);
}

/// <summary>
/// Fluent builder for creating schematic power objects.
/// </summary>
public sealed class PowerObjectBuilder
{
    private readonly SchPowerObject _power = new();

    internal PowerObjectBuilder(string netName)
    {
        _power.Text = netName;
    }

    /// <summary>
    /// Sets the location.
    /// </summary>
    public PowerObjectBuilder At(Coord x, Coord y)
    {
        _power.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the power port style.
    /// </summary>
    public PowerObjectBuilder Style(PowerPortStyle style)
    {
        _power.Style = style;
        return this;
    }

    /// <summary>
    /// Sets the rotation in degrees.
    /// </summary>
    public PowerObjectBuilder Rotation(double degrees)
    {
        _power.Rotation = degrees;
        return this;
    }

    /// <summary>
    /// Hides the net name.
    /// </summary>
    public PowerObjectBuilder HideNetName()
    {
        _power.ShowNetName = false;
        return this;
    }

    /// <summary>
    /// Marks as a cross-sheet connector.
    /// </summary>
    public PowerObjectBuilder CrossSheetConnector(bool isCrossSheet = true)
    {
        _power.IsCrossSheetConnector = isCrossSheet;
        return this;
    }

    /// <summary>
    /// Sets the text color.
    /// </summary>
    public PowerObjectBuilder Color(int color)
    {
        _power.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the font ID.
    /// </summary>
    public PowerObjectBuilder FontId(int fontId)
    {
        _power.FontId = fontId;
        return this;
    }

    /// <summary>
    /// Builds the power object.
    /// </summary>
    public SchPowerObject Build() => _power;

    public static implicit operator SchPowerObject(PowerObjectBuilder builder) => builder.Build();
}
