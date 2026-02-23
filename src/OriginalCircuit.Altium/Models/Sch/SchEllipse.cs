using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic ellipse.
/// </summary>
public sealed class SchEllipse : ISchEllipse
{
    /// <inheritdoc />
    public CoordPoint Center { get; set; }

    /// <inheritdoc />
    public Coord RadiusX { get; set; }

    /// <inheritdoc />
    public Coord RadiusY { get; set; }

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
    /// Whether the ellipse is filled.
    /// </summary>
    public bool IsFilled { get; set; }

    /// <summary>
    /// Whether the fill is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

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
    public CoordRect Bounds => new(
        new CoordPoint(Center.X - RadiusX, Center.Y - RadiusY),
        new CoordPoint(Center.X + RadiusX, Center.Y + RadiusY));

    /// <summary>
    /// Creates a fluent builder for a new ellipse.
    /// </summary>
    public static EllipseBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic ellipses.
/// </summary>
public sealed class EllipseBuilder
{
    private readonly SchEllipse _ellipse = new();

    internal EllipseBuilder() { }

    /// <summary>
    /// Sets the ellipse center point.
    /// </summary>
    public EllipseBuilder At(Coord x, Coord y)
    {
        _ellipse.Center = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the horizontal radius.
    /// </summary>
    public EllipseBuilder RadiusX(Coord radius)
    {
        _ellipse.RadiusX = radius;
        return this;
    }

    /// <summary>
    /// Sets the vertical radius.
    /// </summary>
    public EllipseBuilder RadiusY(Coord radius)
    {
        _ellipse.RadiusY = radius;
        return this;
    }

    /// <summary>
    /// Sets both radii (for a circle).
    /// </summary>
    public EllipseBuilder Radius(Coord radius)
    {
        _ellipse.RadiusX = radius;
        _ellipse.RadiusY = radius;
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public EllipseBuilder LineWidth(int width)
    {
        _ellipse.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the border color.
    /// </summary>
    public EllipseBuilder Color(int color)
    {
        _ellipse.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the fill color.
    /// </summary>
    public EllipseBuilder FillColor(int color)
    {
        _ellipse.FillColor = color;
        return this;
    }

    /// <summary>
    /// Sets whether the ellipse is filled.
    /// </summary>
    public EllipseBuilder Filled(bool filled = true)
    {
        _ellipse.IsFilled = filled;
        return this;
    }

    /// <summary>
    /// Sets whether the fill is transparent.
    /// </summary>
    public EllipseBuilder Transparent(bool transparent = true)
    {
        _ellipse.IsTransparent = transparent;
        return this;
    }

    /// <summary>
    /// Builds the ellipse.
    /// </summary>
    public SchEllipse Build() => _ellipse;

    /// <summary>Implicitly converts a <see cref="EllipseBuilder"/> to a <see cref="SchEllipse"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchEllipse(EllipseBuilder builder) => builder.Build();
}
