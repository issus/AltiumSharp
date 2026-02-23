using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic elliptical arc (arc segment of an ellipse).
/// </summary>
public sealed class SchEllipticalArc : ISchEllipticalArc
{
    /// <inheritdoc />
    public CoordPoint Center { get; set; }

    /// <summary>
    /// Primary radius (X direction).
    /// </summary>
    public Coord PrimaryRadius { get; set; }

    /// <summary>
    /// Secondary radius (Y direction).
    /// </summary>
    public Coord SecondaryRadius { get; set; }

    /// <inheritdoc />
    public double StartAngle { get; set; }

    /// <inheritdoc />
    public double EndAngle { get; set; }

    /// <summary>
    /// Line width.
    /// </summary>
    public Coord LineWidth { get; set; }

    /// <summary>
    /// Line color (RGB).
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
            // Approximate bounds using the larger radius
            var maxRadius = PrimaryRadius > SecondaryRadius ? PrimaryRadius : SecondaryRadius;
            return new CoordRect(
                new CoordPoint(Center.X - maxRadius, Center.Y - maxRadius),
                new CoordPoint(Center.X + maxRadius, Center.Y + maxRadius));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new elliptical arc.
    /// </summary>
    public static EllipticalArcBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic elliptical arcs.
/// </summary>
public sealed class EllipticalArcBuilder
{
    private readonly SchEllipticalArc _arc = new();

    internal EllipticalArcBuilder() { }

    /// <summary>
    /// Sets the center point.
    /// </summary>
    public EllipticalArcBuilder At(Coord x, Coord y)
    {
        _arc.Center = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the primary radius (X direction).
    /// </summary>
    public EllipticalArcBuilder PrimaryRadius(Coord radius)
    {
        _arc.PrimaryRadius = radius;
        return this;
    }

    /// <summary>
    /// Sets the secondary radius (Y direction).
    /// </summary>
    public EllipticalArcBuilder SecondaryRadius(Coord radius)
    {
        _arc.SecondaryRadius = radius;
        return this;
    }

    /// <summary>
    /// Sets both radii.
    /// </summary>
    public EllipticalArcBuilder Radii(Coord primary, Coord secondary)
    {
        _arc.PrimaryRadius = primary;
        _arc.SecondaryRadius = secondary;
        return this;
    }

    /// <summary>
    /// Sets the start and end angles in degrees.
    /// </summary>
    public EllipticalArcBuilder Angles(double startAngle, double endAngle)
    {
        _arc.StartAngle = startAngle;
        _arc.EndAngle = endAngle;
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public EllipticalArcBuilder LineWidth(Coord width)
    {
        _arc.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the line color.
    /// </summary>
    public EllipticalArcBuilder Color(int color)
    {
        _arc.Color = color;
        return this;
    }

    /// <summary>
    /// Builds the elliptical arc.
    /// </summary>
    public SchEllipticalArc Build() => _arc;

    public static implicit operator SchEllipticalArc(EllipticalArcBuilder builder) => builder.Build();
}
