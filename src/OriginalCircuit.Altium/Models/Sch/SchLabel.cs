using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Text justification for schematic labels.
/// </summary>
public enum SchTextJustification
{
    BottomLeft = 0,
    BottomCenter = 1,
    BottomRight = 2,
    MiddleLeft = 3,
    MiddleCenter = 4,
    MiddleRight = 5,
    TopLeft = 6,
    TopCenter = 7,
    TopRight = 8
}

/// <summary>
/// Represents a schematic text label.
/// </summary>
public sealed class SchLabel : ISchLabel
{
    /// <inheritdoc />
    public string Text { get; set; } = string.Empty;

    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Font ID.
    /// </summary>
    public int FontId { get; set; } = 1;

    /// <summary>
    /// Text justification.
    /// </summary>
    public SchTextJustification Justification { get; set; } = SchTextJustification.BottomLeft;

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Text color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Whether the label text is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Whether the label is hidden.
    /// </summary>
    public bool IsHidden { get; set; }

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
            // Approximate bounds - would need font metrics for exact calculation
            var width = Coord.FromMils(Text.Length * 50);
            var height = Coord.FromMils(60);
            return new CoordRect(Location, new CoordPoint(Location.X + width, Location.Y + height));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new label.
    /// </summary>
    public static LabelBuilder Create(string text) => new(text);
}

/// <summary>
/// Fluent builder for creating schematic labels.
/// </summary>
public sealed class LabelBuilder
{
    private readonly SchLabel _label = new();

    internal LabelBuilder(string text)
    {
        _label.Text = text;
    }

    /// <summary>
    /// Sets the label location.
    /// </summary>
    public LabelBuilder At(Coord x, Coord y)
    {
        _label.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the font ID.
    /// </summary>
    public LabelBuilder Font(int fontId)
    {
        _label.FontId = fontId;
        return this;
    }

    /// <summary>
    /// Sets the justification.
    /// </summary>
    public LabelBuilder Justify(SchTextJustification justification)
    {
        _label.Justification = justification;
        return this;
    }

    /// <summary>
    /// Sets the rotation.
    /// </summary>
    public LabelBuilder Rotation(double degrees)
    {
        _label.Rotation = degrees;
        return this;
    }

    /// <summary>
    /// Sets the text color.
    /// </summary>
    public LabelBuilder Color(int color)
    {
        _label.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the label as hidden.
    /// </summary>
    public LabelBuilder Hidden(bool hidden = true)
    {
        _label.IsHidden = hidden;
        return this;
    }

    /// <summary>
    /// Builds the label.
    /// </summary>
    public SchLabel Build() => _label;

    /// <summary>Implicitly converts a <see cref="LabelBuilder"/> to a <see cref="SchLabel"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchLabel(LabelBuilder builder) => builder.Build();
}
