using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Pin electrical types.
/// </summary>
public enum PinElectricalType
{
    Input = 0,
    InputOutput = 1,
    Output = 2,
    OpenCollector = 3,
    Passive = 4,
    HiZ = 5,
    OpenEmitter = 6,
    Power = 7
}

/// <summary>
/// Pin orientation.
/// </summary>
public enum PinOrientation
{
    Right = 0,
    Up = 1,
    Left = 2,
    Down = 3
}

/// <summary>
/// Represents a schematic pin.
/// </summary>
public sealed class SchPin : ISchPin
{
    /// <inheritdoc />
    public string? Name { get; set; }

    /// <inheritdoc />
    public string? Designator { get; set; }

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public Coord Length { get; set; }

    /// <summary>
    /// Electrical type of the pin.
    /// </summary>
    public PinElectricalType ElectricalType { get; set; } = PinElectricalType.Passive;

    /// <summary>
    /// Orientation of the pin.
    /// </summary>
    public PinOrientation Orientation { get; set; } = PinOrientation.Right;

    /// <summary>
    /// Whether the pin name is visible.
    /// </summary>
    public bool ShowName { get; set; } = true;

    /// <summary>
    /// Whether the pin designator is visible.
    /// </summary>
    public bool ShowDesignator { get; set; } = true;

    /// <summary>
    /// Description of the pin.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Formal type of the pin.
    /// </summary>
    public int FormalType { get; set; }

    /// <summary>
    /// Symbol for the inner edge of the pin.
    /// </summary>
    public int SymbolInnerEdge { get; set; }

    /// <summary>
    /// Symbol for the outer edge of the pin.
    /// </summary>
    public int SymbolOuterEdge { get; set; }

    /// <summary>
    /// Symbol displayed inside the pin.
    /// </summary>
    public int SymbolInside { get; set; }

    /// <summary>
    /// Symbol displayed outside the pin.
    /// </summary>
    public int SymbolOutside { get; set; }

    /// <summary>
    /// Line width of pin symbols.
    /// </summary>
    public int SymbolLineWidth { get; set; }

    /// <summary>
    /// Swap ID part for pin swapping.
    /// </summary>
    public string? SwapIdPart { get; set; }

    /// <summary>
    /// Pin propagation delay.
    /// </summary>
    public int PinPropagationDelay { get; set; }

    /// <summary>
    /// Custom font ID for the designator text.
    /// </summary>
    public int DesignatorCustomFontId { get; set; }

    /// <summary>
    /// Custom font ID for the name text.
    /// </summary>
    public int NameCustomFontId { get; set; }

    /// <summary>
    /// Pin width.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Color value for this primitive.
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Area/background color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Default value of the pin.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether the pin is hidden.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Custom color for the designator text.
    /// </summary>
    public int DesignatorCustomColor { get; set; }

    /// <summary>
    /// Designator custom position margin.
    /// </summary>
    public int DesignatorCustomPositionMargin { get; set; }

    /// <summary>
    /// Designator rotation anchor for custom positioning.
    /// </summary>
    public int DesignatorCustomPositionRotationAnchor { get; set; }

    /// <summary>
    /// Whether designator rotation is relative to pin direction.
    /// </summary>
    public bool DesignatorCustomPositionRotationRelative { get; set; }

    /// <summary>
    /// Designator font mode (0=Default, 1=Custom).
    /// </summary>
    public int DesignatorFontMode { get; set; }

    /// <summary>
    /// Designator position mode (0=Default, 1=Custom).
    /// </summary>
    public int DesignatorPositionMode { get; set; }

    /// <summary>
    /// Custom color for the name text.
    /// </summary>
    public int NameCustomColor { get; set; }

    /// <summary>
    /// Name custom position margin.
    /// </summary>
    public int NameCustomPositionMargin { get; set; }

    /// <summary>
    /// Name rotation anchor for custom positioning.
    /// </summary>
    public int NameCustomPositionRotationAnchor { get; set; }

    /// <summary>
    /// Whether name rotation is relative to pin direction.
    /// </summary>
    public bool NameCustomPositionRotationRelative { get; set; }

    /// <summary>
    /// Name font mode (0=Default, 1=Custom).
    /// </summary>
    public int NameFontMode { get; set; }

    /// <summary>
    /// Name position mode (0=Default, 1=Custom).
    /// </summary>
    public int NamePositionMode { get; set; }

    /// <summary>
    /// Swap ID pair identifier.
    /// </summary>
    public string? SwapIdPair { get; set; }

    /// <summary>
    /// Swap ID part-pin identifier.
    /// </summary>
    public string? SwapIdPartPin { get; set; }

    /// <summary>
    /// Swap ID pin identifier.
    /// </summary>
    public string? SwapIdPin { get; set; }

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
    /// Hidden net name associated with this pin.
    /// </summary>
    public string? HiddenNetName { get; set; }

    /// <summary>
    /// Pin package length for pad-to-pin mapping.
    /// </summary>
    public Coord PinPackageLength { get; set; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var endPoint = Orientation switch
            {
                PinOrientation.Right => new CoordPoint(Location.X + Length, Location.Y),
                PinOrientation.Left => new CoordPoint(Location.X - Length, Location.Y),
                PinOrientation.Up => new CoordPoint(Location.X, Location.Y + Length),
                PinOrientation.Down => new CoordPoint(Location.X, Location.Y - Length),
                _ => Location
            };
            return new CoordRect(Location, endPoint);
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new pin.
    /// </summary>
    public static PinBuilder Create(string? designator = null) => new(designator);
}

/// <summary>
/// Fluent builder for creating schematic pins.
/// </summary>
public sealed class PinBuilder
{
    private readonly SchPin _pin = new();

    internal PinBuilder(string? designator)
    {
        _pin.Designator = designator;
    }

    /// <summary>
    /// Sets the pin name.
    /// </summary>
    public PinBuilder WithName(string name)
    {
        _pin.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the pin location.
    /// </summary>
    public PinBuilder At(Coord x, Coord y)
    {
        _pin.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the pin length.
    /// </summary>
    public PinBuilder Length(Coord length)
    {
        _pin.Length = length;
        return this;
    }

    /// <summary>
    /// Sets the electrical type.
    /// </summary>
    public PinBuilder Electrical(PinElectricalType type)
    {
        _pin.ElectricalType = type;
        return this;
    }

    /// <summary>
    /// Sets the orientation.
    /// </summary>
    public PinBuilder Orient(PinOrientation orientation)
    {
        _pin.Orientation = orientation;
        return this;
    }

    /// <summary>
    /// Hides the pin name.
    /// </summary>
    public PinBuilder HideName()
    {
        _pin.ShowName = false;
        return this;
    }

    /// <summary>
    /// Hides the pin designator.
    /// </summary>
    public PinBuilder HideDesignator()
    {
        _pin.ShowDesignator = false;
        return this;
    }

    /// <summary>
    /// Builds the pin.
    /// </summary>
    public SchPin Build() => _pin;

    public static implicit operator SchPin(PinBuilder builder) => builder.Build();
}
