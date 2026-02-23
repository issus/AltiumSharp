using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Line style for wires and polylines.
/// </summary>
public enum SchLineStyle
{
    Solid = 0,
    Dashed = 1,
    Dotted = 2
}

/// <summary>
/// Represents a schematic wire segment (electrical connection).
/// </summary>
public sealed class SchWire : ISchWire
{
    private readonly List<CoordPoint> _vertices = new();

    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices => _vertices;

    /// <summary>
    /// Line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Wire color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Line style (Solid, Dashed, Dotted).
    /// </summary>
    public SchLineStyle LineStyle { get; set; } = SchLineStyle.Solid;

    /// <summary>
    /// Area/background color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Whether the wire is solid filled.
    /// </summary>
    public bool IsSolid { get; set; }

    /// <summary>
    /// Whether the fill is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

    /// <summary>
    /// Whether auto-wire mode was used.
    /// </summary>
    public bool AutoWire { get; set; }

    /// <summary>
    /// Underline color (RGB).
    /// </summary>
    public int UnderlineColor { get; set; }

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
            if (_vertices.Count == 0)
                return CoordRect.Empty;

            var minX = _vertices[0].X;
            var maxX = _vertices[0].X;
            var minY = _vertices[0].Y;
            var maxY = _vertices[0].Y;

            for (var i = 1; i < _vertices.Count; i++)
            {
                var v = _vertices[i];
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
            }

            return new CoordRect(new CoordPoint(minX, minY), new CoordPoint(maxX, maxY));
        }
    }

    /// <summary>
    /// Adds a vertex to the wire.
    /// </summary>
    internal void AddVertex(CoordPoint point) => _vertices.Add(point);

    /// <summary>
    /// Creates a fluent builder for a new wire.
    /// </summary>
    public static WireBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic wires.
/// </summary>
public sealed class WireBuilder
{
    private readonly SchWire _wire = new();

    internal WireBuilder() { }

    /// <summary>
    /// Sets the starting point of the wire.
    /// </summary>
    public WireBuilder From(Coord x, Coord y)
    {
        if (_wire.Vertices.Count == 0)
            _wire.AddVertex(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Sets the ending point of the wire.
    /// </summary>
    public WireBuilder To(Coord x, Coord y)
    {
        _wire.AddVertex(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Adds a point to the wire path.
    /// </summary>
    public WireBuilder AddPoint(Coord x, Coord y)
    {
        _wire.AddVertex(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public WireBuilder LineWidth(int width)
    {
        _wire.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the wire color.
    /// </summary>
    public WireBuilder Color(int color)
    {
        _wire.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the line style.
    /// </summary>
    public WireBuilder Style(SchLineStyle style)
    {
        _wire.LineStyle = style;
        return this;
    }

    /// <summary>
    /// Builds the wire.
    /// </summary>
    public SchWire Build() => _wire;

    public static implicit operator SchWire(WireBuilder builder) => builder.Build();
}
