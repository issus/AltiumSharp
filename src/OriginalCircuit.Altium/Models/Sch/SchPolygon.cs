using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic polygon (closed shape with fill).
/// </summary>
public sealed class SchPolygon : ISchPolygon
{
    private readonly List<CoordPoint> _vertices = new();

    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices => _vertices;

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
    /// Whether the polygon is filled.
    /// </summary>
    public bool IsFilled { get; set; }

    /// <inheritdoc />
    EdaColor ISchPolygon.Color => AltiumColorHelper.BgrToEdaColor(Color);

    /// <inheritdoc />
    EdaColor ISchPolygon.FillColor => AltiumColorHelper.BgrToEdaColor(FillColor);

    /// <inheritdoc />
    Coord ISchPolygon.LineWidth => AltiumLineWidthHelper.IndexToCoord(LineWidth);

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
    /// Adds a vertex to the polygon.
    /// </summary>
    internal void AddVertex(CoordPoint point) => _vertices.Add(point);

    /// <summary>
    /// Creates a fluent builder for a new polygon.
    /// </summary>
    public static PolygonBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic polygons.
/// </summary>
public sealed class PolygonBuilder
{
    private readonly SchPolygon _polygon = new();

    internal PolygonBuilder() { }

    /// <summary>
    /// Adds a vertex to the polygon.
    /// </summary>
    public PolygonBuilder AddVertex(Coord x, Coord y)
    {
        _polygon.AddVertex(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public PolygonBuilder LineWidth(int width)
    {
        _polygon.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the border color.
    /// </summary>
    public PolygonBuilder Color(int color)
    {
        _polygon.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the fill color.
    /// </summary>
    public PolygonBuilder FillColor(int color)
    {
        _polygon.FillColor = color;
        return this;
    }

    /// <summary>
    /// Sets whether the polygon is filled.
    /// </summary>
    public PolygonBuilder Filled(bool filled = true)
    {
        _polygon.IsFilled = filled;
        return this;
    }

    /// <summary>
    /// Sets whether the fill is transparent.
    /// </summary>
    public PolygonBuilder Transparent(bool transparent = true)
    {
        _polygon.IsTransparent = transparent;
        return this;
    }

    /// <summary>
    /// Builds the polygon.
    /// </summary>
    public SchPolygon Build() => _polygon;

    /// <summary>Implicitly converts a <see cref="PolygonBuilder"/> to a <see cref="SchPolygon"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchPolygon(PolygonBuilder builder) => builder.Build();
}
