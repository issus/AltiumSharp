using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic net label (names a net/wire).
/// </summary>
public sealed class SchNetLabel : ISchNetLabel
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text orientation (0=horizontal, 1=90 degrees, 2=180, 3=270).
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    /// Text justification.
    /// </summary>
    public TextJustification Justification { get; set; } = TextJustification.BottomLeft;

    /// <summary>
    /// Font ID reference.
    /// </summary>
    public int FontId { get; set; }

    /// <summary>
    /// Label color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <inheritdoc />
    EdaColor ISchNetLabel.Color => AltiumColorHelper.BgrToEdaColor(Color);

    /// <summary>
    /// Whether the label is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Fill/area color value.
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
            // Approximate bounds based on text length (better calculation requires font metrics)
            var approxWidth = Coord.FromMils(Text.Length * 50);
            var approxHeight = Coord.FromMils(80);
            return new CoordRect(Location, new CoordPoint(Location.X + approxWidth, Location.Y + approxHeight));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new net label.
    /// </summary>
    public static NetLabelBuilder Create(string text) => new(text);
}

/// <summary>
/// Fluent builder for creating schematic net labels.
/// </summary>
public sealed class NetLabelBuilder
{
    private readonly SchNetLabel _label = new();

    internal NetLabelBuilder(string text)
    {
        _label.Text = text;
    }

    /// <summary>
    /// Sets the label location.
    /// </summary>
    public NetLabelBuilder At(Coord x, Coord y)
    {
        _label.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the text orientation.
    /// </summary>
    public NetLabelBuilder Orientation(int orientation)
    {
        _label.Orientation = orientation;
        return this;
    }

    /// <summary>
    /// Sets the text justification.
    /// </summary>
    public NetLabelBuilder Justify(TextJustification justification)
    {
        _label.Justification = justification;
        return this;
    }

    /// <summary>
    /// Sets the font ID.
    /// </summary>
    public NetLabelBuilder Font(int fontId)
    {
        _label.FontId = fontId;
        return this;
    }

    /// <summary>
    /// Sets the label color.
    /// </summary>
    public NetLabelBuilder Color(int color)
    {
        _label.Color = color;
        return this;
    }

    /// <summary>
    /// Sets whether the label is mirrored.
    /// </summary>
    public NetLabelBuilder Mirrored(bool mirrored = true)
    {
        _label.IsMirrored = mirrored;
        return this;
    }

    /// <summary>
    /// Builds the net label.
    /// </summary>
    public SchNetLabel Build() => _label;

    /// <summary>Implicitly converts a <see cref="NetLabelBuilder"/> to a <see cref="SchNetLabel"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchNetLabel(NetLabelBuilder builder) => builder.Build();
}
