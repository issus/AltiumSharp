using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic rounded rectangle.
/// </summary>
public sealed class SchRoundedRectangle : ISchRoundedRectangle
{
    /// <inheritdoc />
    public CoordPoint Corner1 { get; set; }

    /// <inheritdoc />
    public CoordPoint Corner2 { get; set; }

    /// <summary>
    /// Horizontal corner radius.
    /// </summary>
    public Coord CornerRadiusX { get; set; }

    /// <summary>
    /// Vertical corner radius.
    /// </summary>
    public Coord CornerRadiusY { get; set; }

    /// <summary>
    /// Line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Border color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Fill color (RGB).
    /// </summary>
    public int FillColor { get; set; }

    /// <summary>
    /// Whether the rectangle is filled.
    /// </summary>
    public bool IsFilled { get; set; }

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
    /// Creates a fluent builder for a new rounded rectangle.
    /// </summary>
    public static RoundedRectangleBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic rounded rectangles.
/// </summary>
public sealed class RoundedRectangleBuilder
{
    private readonly SchRoundedRectangle _rect = new();

    internal RoundedRectangleBuilder() { }

    /// <summary>
    /// Sets the first corner.
    /// </summary>
    public RoundedRectangleBuilder From(Coord x, Coord y)
    {
        _rect.Corner1 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the second corner.
    /// </summary>
    public RoundedRectangleBuilder To(Coord x, Coord y)
    {
        _rect.Corner2 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the horizontal corner radius.
    /// </summary>
    public RoundedRectangleBuilder CornerRadiusX(Coord radius)
    {
        _rect.CornerRadiusX = radius;
        return this;
    }

    /// <summary>
    /// Sets the vertical corner radius.
    /// </summary>
    public RoundedRectangleBuilder CornerRadiusY(Coord radius)
    {
        _rect.CornerRadiusY = radius;
        return this;
    }

    /// <summary>
    /// Sets both corner radii.
    /// </summary>
    public RoundedRectangleBuilder CornerRadius(Coord radius)
    {
        _rect.CornerRadiusX = radius;
        _rect.CornerRadiusY = radius;
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public RoundedRectangleBuilder LineWidth(int width)
    {
        _rect.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the border color.
    /// </summary>
    public RoundedRectangleBuilder Color(int color)
    {
        _rect.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the fill color.
    /// </summary>
    public RoundedRectangleBuilder FillColor(int color)
    {
        _rect.FillColor = color;
        return this;
    }

    /// <summary>
    /// Sets whether the rectangle is filled.
    /// </summary>
    public RoundedRectangleBuilder Filled(bool filled = true)
    {
        _rect.IsFilled = filled;
        return this;
    }

    /// <summary>
    /// Sets whether the fill is transparent.
    /// </summary>
    public RoundedRectangleBuilder Transparent(bool transparent = true)
    {
        _rect.IsTransparent = transparent;
        return this;
    }

    /// <summary>
    /// Builds the rounded rectangle.
    /// </summary>
    public SchRoundedRectangle Build() => _rect;

    /// <summary>Implicitly converts a <see cref="RoundedRectangleBuilder"/> to a <see cref="SchRoundedRectangle"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchRoundedRectangle(RoundedRectangleBuilder builder) => builder.Build();
}
