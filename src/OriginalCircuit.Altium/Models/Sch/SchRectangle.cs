using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic rectangle.
/// </summary>
public sealed class SchRectangle : ISchRectangle
{
    /// <inheritdoc />
    public CoordPoint Corner1 { get; set; }

    /// <inheritdoc />
    public CoordPoint Corner2 { get; set; }

    /// <summary>
    /// Line width.
    /// </summary>
    public Coord LineWidth { get; set; }

    /// <summary>
    /// Whether the rectangle is filled.
    /// </summary>
    public bool IsFilled { get; set; }

    /// <summary>
    /// Line color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Fill color (RGB).
    /// </summary>
    public int FillColor { get; set; }

    /// <inheritdoc />
    EdaColor ISchRectangle.Color => AltiumColorHelper.BgrToEdaColor(Color);

    /// <inheritdoc />
    EdaColor ISchRectangle.FillColor => AltiumColorHelper.BgrToEdaColor(FillColor);

    /// <summary>
    /// Whether the fill is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

    /// <summary>
    /// Line style (0=Solid, 1=Dashed, 2=Dotted, 3=DashDot).
    /// </summary>
    public int LineStyle { get; set; }

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
    public CoordRect Bounds => new(Corner1, Corner2);

    /// <summary>
    /// Creates a fluent builder for a new rectangle.
    /// </summary>
    public static RectangleBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic rectangles.
/// </summary>
public sealed class RectangleBuilder
{
    private readonly SchRectangle _rect = new();

    internal RectangleBuilder() { }

    /// <summary>
    /// Sets the first corner.
    /// </summary>
    public RectangleBuilder From(Coord x, Coord y)
    {
        _rect.Corner1 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the second corner.
    /// </summary>
    public RectangleBuilder To(Coord x, Coord y)
    {
        _rect.Corner2 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public RectangleBuilder LineWidth(Coord width)
    {
        _rect.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the rectangle as filled.
    /// </summary>
    public RectangleBuilder Filled(bool filled = true)
    {
        _rect.IsFilled = filled;
        return this;
    }

    /// <summary>
    /// Sets the line color.
    /// </summary>
    public RectangleBuilder Color(int color)
    {
        _rect.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the fill color.
    /// </summary>
    public RectangleBuilder FillColor(int color)
    {
        _rect.FillColor = color;
        return this;
    }

    /// <summary>
    /// Builds the rectangle.
    /// </summary>
    public SchRectangle Build() => _rect;

    /// <summary>Implicitly converts a <see cref="RectangleBuilder"/> to a <see cref="SchRectangle"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchRectangle(RectangleBuilder builder) => builder.Build();
}
