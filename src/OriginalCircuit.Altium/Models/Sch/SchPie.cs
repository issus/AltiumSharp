using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic pie (filled arc segment).
/// </summary>
public sealed class SchPie : ISchPie
{
    /// <inheritdoc />
    public CoordPoint Center { get; set; }

    /// <inheritdoc />
    public Coord Radius { get; set; }

    /// <summary>
    /// Starting angle in degrees (0-360).
    /// </summary>
    public double StartAngle { get; set; }

    /// <summary>
    /// Ending angle in degrees (0-360).
    /// </summary>
    public double EndAngle { get; set; }

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
    /// Whether the pie is filled.
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
    public CoordRect Bounds
    {
        get
        {
            var r = Radius;
            return new CoordRect(
                new CoordPoint(Center.X - r, Center.Y - r),
                new CoordPoint(Center.X + r, Center.Y + r));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new pie.
    /// </summary>
    public static PieBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic pies.
/// </summary>
public sealed class PieBuilder
{
    private readonly SchPie _pie = new();

    internal PieBuilder() { }

    /// <summary>
    /// Sets the pie center point.
    /// </summary>
    public PieBuilder At(Coord x, Coord y)
    {
        _pie.Center = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the pie radius.
    /// </summary>
    public PieBuilder Radius(Coord radius)
    {
        _pie.Radius = radius;
        return this;
    }

    /// <summary>
    /// Sets the starting angle in degrees.
    /// </summary>
    public PieBuilder StartAngle(double degrees)
    {
        _pie.StartAngle = degrees;
        return this;
    }

    /// <summary>
    /// Sets the ending angle in degrees.
    /// </summary>
    public PieBuilder EndAngle(double degrees)
    {
        _pie.EndAngle = degrees;
        return this;
    }

    /// <summary>
    /// Sets the angle range.
    /// </summary>
    public PieBuilder Angles(double startDegrees, double endDegrees)
    {
        _pie.StartAngle = startDegrees;
        _pie.EndAngle = endDegrees;
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public PieBuilder LineWidth(int width)
    {
        _pie.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the border color.
    /// </summary>
    public PieBuilder Color(int color)
    {
        _pie.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the fill color.
    /// </summary>
    public PieBuilder FillColor(int color)
    {
        _pie.FillColor = color;
        return this;
    }

    /// <summary>
    /// Sets whether the pie is filled.
    /// </summary>
    public PieBuilder Filled(bool filled = true)
    {
        _pie.IsFilled = filled;
        return this;
    }

    /// <summary>
    /// Sets whether the fill is transparent.
    /// </summary>
    public PieBuilder Transparent(bool transparent = true)
    {
        _pie.IsTransparent = transparent;
        return this;
    }

    /// <summary>
    /// Builds the pie.
    /// </summary>
    public SchPie Build() => _pie;

    public static implicit operator SchPie(PieBuilder builder) => builder.Build();
}
