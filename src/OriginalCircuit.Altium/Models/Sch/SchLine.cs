using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic line.
/// </summary>
public sealed class SchLine : ISchLine
{
    /// <inheritdoc />
    public CoordPoint Start { get; set; }

    /// <inheritdoc />
    public CoordPoint End { get; set; }

    /// <summary>
    /// Line width.
    /// </summary>
    public Coord Width { get; set; }

    /// <summary>
    /// Line color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Line style (0=Solid, 1=Dashed, 2=Dotted, 3=DashDot).
    /// </summary>
    public int LineStyle { get; set; }

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
    public CoordRect Bounds => new(Start, End);

    /// <summary>
    /// Creates a fluent builder for a new line.
    /// </summary>
    public static LineBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic lines.
/// </summary>
public sealed class LineBuilder
{
    private readonly SchLine _line = new();

    internal LineBuilder() { }

    /// <summary>
    /// Sets the starting point.
    /// </summary>
    public LineBuilder From(Coord x, Coord y)
    {
        _line.Start = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the ending point.
    /// </summary>
    public LineBuilder To(Coord x, Coord y)
    {
        _line.End = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public LineBuilder Width(Coord width)
    {
        _line.Width = width;
        return this;
    }

    /// <summary>
    /// Sets the line color.
    /// </summary>
    public LineBuilder Color(int color)
    {
        _line.Color = color;
        return this;
    }

    /// <summary>
    /// Builds the line.
    /// </summary>
    public SchLine Build() => _line;

    /// <summary>Implicitly converts a <see cref="LineBuilder"/> to a <see cref="SchLine"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchLine(LineBuilder builder) => builder.Build();
}
