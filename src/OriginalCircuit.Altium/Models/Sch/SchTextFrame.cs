using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic text frame (multiline text with border).
/// </summary>
public sealed class SchTextFrame : ISchTextFrame
{
    /// <inheritdoc />
    public CoordPoint Corner1 { get; set; }

    /// <inheritdoc />
    public CoordPoint Corner2 { get; set; }

    /// <inheritdoc />
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text orientation.
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    /// Text alignment within the frame.
    /// </summary>
    public TextJustification Alignment { get; set; } = TextJustification.BottomLeft;

    /// <summary>
    /// Font ID reference.
    /// </summary>
    public int FontId { get; set; }

    /// <summary>
    /// Text color (RGB).
    /// </summary>
    public int TextColor { get; set; }

    /// <summary>
    /// Border color (RGB).
    /// </summary>
    public int BorderColor { get; set; }

    /// <summary>
    /// Fill color (RGB).
    /// </summary>
    public int FillColor { get; set; }

    /// <summary>
    /// Whether the frame has a visible border.
    /// </summary>
    public bool ShowBorder { get; set; } = true;

    /// <summary>
    /// Whether the frame is filled.
    /// </summary>
    public bool IsFilled { get; set; }

    /// <summary>
    /// Whether word wrapping is enabled.
    /// </summary>
    public bool WordWrap { get; set; } = true;

    /// <summary>
    /// Whether the text clips to the frame.
    /// </summary>
    public bool ClipToRect { get; set; }

    /// <summary>
    /// Line width of the border.
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Line style of the border.
    /// </summary>
    public int LineStyle { get; set; }

    /// <summary>
    /// Margin between text and frame border (in internal coords).
    /// </summary>
    public int TextMargin { get; set; }

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
    /// Whether the fill is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

    /// <summary>
    /// Unique identifier for this primitive.
    /// </summary>
    public string? UniqueId { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds => new(Corner1, Corner2);

    /// <summary>
    /// Creates a fluent builder for a new text frame.
    /// </summary>
    public static TextFrameBuilder Create(string text) => new(text);
}

/// <summary>
/// Fluent builder for creating schematic text frames.
/// </summary>
public sealed class TextFrameBuilder
{
    private readonly SchTextFrame _frame = new();

    internal TextFrameBuilder(string text)
    {
        _frame.Text = text;
    }

    /// <summary>
    /// Sets the first corner.
    /// </summary>
    public TextFrameBuilder From(Coord x, Coord y)
    {
        _frame.Corner1 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the second corner.
    /// </summary>
    public TextFrameBuilder To(Coord x, Coord y)
    {
        _frame.Corner2 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the text orientation.
    /// </summary>
    public TextFrameBuilder Orientation(int orientation)
    {
        _frame.Orientation = orientation;
        return this;
    }

    /// <summary>
    /// Sets the text alignment.
    /// </summary>
    public TextFrameBuilder Align(TextJustification alignment)
    {
        _frame.Alignment = alignment;
        return this;
    }

    /// <summary>
    /// Sets the font ID.
    /// </summary>
    public TextFrameBuilder Font(int fontId)
    {
        _frame.FontId = fontId;
        return this;
    }

    /// <summary>
    /// Sets the text color.
    /// </summary>
    public TextFrameBuilder TextColor(int color)
    {
        _frame.TextColor = color;
        return this;
    }

    /// <summary>
    /// Sets the border color.
    /// </summary>
    public TextFrameBuilder BorderColor(int color)
    {
        _frame.BorderColor = color;
        return this;
    }

    /// <summary>
    /// Sets the fill color.
    /// </summary>
    public TextFrameBuilder FillColor(int color)
    {
        _frame.FillColor = color;
        return this;
    }

    /// <summary>
    /// Sets whether the border is visible.
    /// </summary>
    public TextFrameBuilder ShowBorder(bool show = true)
    {
        _frame.ShowBorder = show;
        return this;
    }

    /// <summary>
    /// Sets whether the frame is filled.
    /// </summary>
    public TextFrameBuilder Filled(bool filled = true)
    {
        _frame.IsFilled = filled;
        return this;
    }

    /// <summary>
    /// Sets whether word wrapping is enabled.
    /// </summary>
    public TextFrameBuilder WordWrap(bool wrap = true)
    {
        _frame.WordWrap = wrap;
        return this;
    }

    /// <summary>
    /// Sets whether text clips to the frame.
    /// </summary>
    public TextFrameBuilder ClipToRect(bool clip = true)
    {
        _frame.ClipToRect = clip;
        return this;
    }

    /// <summary>
    /// Builds the text frame.
    /// </summary>
    public SchTextFrame Build() => _frame;

    /// <summary>Implicitly converts a <see cref="TextFrameBuilder"/> to a <see cref="SchTextFrame"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchTextFrame(TextFrameBuilder builder) => builder.Build();
}
