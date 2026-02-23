using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic Bezier curve.
/// </summary>
public sealed class SchBezier : ISchBezier
{
    private readonly List<CoordPoint> _controlPoints = new();

    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> ControlPoints => _controlPoints;

    /// <summary>
    /// Line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Line color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <inheritdoc />
    EdaColor ISchBezier.Color => AltiumColorHelper.BgrToEdaColor(Color);

    /// <inheritdoc />
    Coord ISchBezier.LineWidth => AltiumLineWidthHelper.IndexToCoord(LineWidth);

    /// <summary>
    /// Area/background color (RGB).
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
            if (_controlPoints.Count == 0)
                return CoordRect.Empty;

            var minX = _controlPoints[0].X;
            var maxX = _controlPoints[0].X;
            var minY = _controlPoints[0].Y;
            var maxY = _controlPoints[0].Y;

            for (var i = 1; i < _controlPoints.Count; i++)
            {
                var v = _controlPoints[i];
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
            }

            return new CoordRect(new CoordPoint(minX, minY), new CoordPoint(maxX, maxY));
        }
    }

    /// <summary>
    /// Adds a control point to the bezier curve.
    /// </summary>
    internal void AddControlPoint(CoordPoint point) => _controlPoints.Add(point);

    /// <summary>
    /// Creates a fluent builder for a new bezier curve.
    /// </summary>
    public static BezierBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic bezier curves.
/// </summary>
public sealed class BezierBuilder
{
    private readonly SchBezier _bezier = new();

    internal BezierBuilder() { }

    /// <summary>
    /// Adds a control point to the bezier curve.
    /// </summary>
    public BezierBuilder AddPoint(Coord x, Coord y)
    {
        _bezier.AddControlPoint(new CoordPoint(x, y));
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public BezierBuilder LineWidth(int width)
    {
        _bezier.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the line color.
    /// </summary>
    public BezierBuilder Color(int color)
    {
        _bezier.Color = color;
        return this;
    }

    /// <summary>
    /// Builds the bezier curve.
    /// </summary>
    public SchBezier Build() => _bezier;

    /// <summary>Implicitly converts a <see cref="BezierBuilder"/> to a <see cref="SchBezier"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchBezier(BezierBuilder builder) => builder.Build();
}
