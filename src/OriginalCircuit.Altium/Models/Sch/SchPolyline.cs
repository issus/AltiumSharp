using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic polyline (multi-segment line, not closed).
/// </summary>
public sealed class SchPolyline : ISchPolyline
{
    private readonly List<CoordPoint> _vertices = new();

    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices => _vertices;

    /// <summary>
    /// Line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Line color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Line style (Solid, Dashed, Dotted).
    /// </summary>
    public SchLineStyle LineStyle { get; set; } = SchLineStyle.Solid;

    /// <inheritdoc />
    EdaColor ISchPolyline.Color => AltiumColorHelper.BgrToEdaColor(Color);

    /// <inheritdoc />
    Coord ISchPolyline.LineWidth => AltiumLineWidthHelper.IndexToCoord(LineWidth);

    /// <inheritdoc />
    LineStyle ISchPolyline.LineStyle => AltiumEnumHelper.SchLineStyleToEdaLineStyle(LineStyle);

    /// <summary>
    /// Shape at the start of the polyline (0=None, 1=Arrow, etc.).
    /// </summary>
    public int StartLineShape { get; set; }

    /// <summary>
    /// Shape at the end of the polyline (0=None, 1=Arrow, etc.).
    /// </summary>
    public int EndLineShape { get; set; }

    /// <summary>
    /// Size of the line end shapes.
    /// </summary>
    public int LineShapeSize { get; set; }

    /// <summary>
    /// Area/fill color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Whether the fill is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

    /// <summary>
    /// Whether the polyline is solid filled.
    /// </summary>
    public bool IsSolid { get; set; }

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
    /// Adds a vertex to the polyline.
    /// </summary>
    internal void AddVertex(CoordPoint point) => _vertices.Add(point);

    /// <summary>
    /// Creates a fluent builder for a new polyline.
    /// </summary>
    public static PolylineBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic polylines.
/// </summary>
public sealed class PolylineBuilder
{
    private readonly SchPolyline _polyline = new();

    internal PolylineBuilder() { }

    /// <summary>
    /// Adds a vertex to the polyline.
    /// </summary>
    public PolylineBuilder AddVertex(Coord x, Coord y)
    {
        _polyline.AddVertex(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Sets the starting point of the polyline.
    /// </summary>
    public PolylineBuilder From(Coord x, Coord y)
    {
        if (_polyline.Vertices.Count == 0)
            _polyline.AddVertex(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Adds a point to the polyline path.
    /// </summary>
    public PolylineBuilder To(Coord x, Coord y)
    {
        _polyline.AddVertex(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public PolylineBuilder LineWidth(int width)
    {
        _polyline.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the line color.
    /// </summary>
    public PolylineBuilder Color(int color)
    {
        _polyline.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the line style.
    /// </summary>
    public PolylineBuilder Style(SchLineStyle style)
    {
        _polyline.LineStyle = style;
        return this;
    }

    /// <summary>
    /// Builds the polyline.
    /// </summary>
    public SchPolyline Build() => _polyline;

    /// <summary>Implicitly converts a <see cref="PolylineBuilder"/> to a <see cref="SchPolyline"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchPolyline(PolylineBuilder builder) => builder.Build();
}
