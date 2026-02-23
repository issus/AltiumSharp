using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic arc primitive.
/// </summary>
public sealed class SchArc : ISchArc
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
    /// Arc color (RGB).
    /// </summary>
    public int Color { get; set; }

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
            // For a full circle or arc, calculate bounds from center and radius
            var r = Radius;
            return new CoordRect(
                new CoordPoint(Center.X - r, Center.Y - r),
                new CoordPoint(Center.X + r, Center.Y + r));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new arc.
    /// </summary>
    public static ArcBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic arcs.
/// </summary>
public sealed class ArcBuilder
{
    private readonly SchArc _arc = new();

    internal ArcBuilder() { }

    /// <summary>
    /// Sets the arc center point.
    /// </summary>
    public ArcBuilder At(Coord x, Coord y)
    {
        _arc.Center = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the arc radius.
    /// </summary>
    public ArcBuilder Radius(Coord radius)
    {
        _arc.Radius = radius;
        return this;
    }

    /// <summary>
    /// Sets the starting angle in degrees.
    /// </summary>
    public ArcBuilder StartAngle(double degrees)
    {
        _arc.StartAngle = degrees;
        return this;
    }

    /// <summary>
    /// Sets the ending angle in degrees.
    /// </summary>
    public ArcBuilder EndAngle(double degrees)
    {
        _arc.EndAngle = degrees;
        return this;
    }

    /// <summary>
    /// Sets the angle range (convenience method for start and end).
    /// </summary>
    public ArcBuilder Angles(double startDegrees, double endDegrees)
    {
        _arc.StartAngle = startDegrees;
        _arc.EndAngle = endDegrees;
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public ArcBuilder LineWidth(int width)
    {
        _arc.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the arc color.
    /// </summary>
    public ArcBuilder Color(int color)
    {
        _arc.Color = color;
        return this;
    }

    /// <summary>
    /// Builds the arc.
    /// </summary>
    public SchArc Build() => _arc;

    public static implicit operator SchArc(ArcBuilder builder) => builder.Build();
}
