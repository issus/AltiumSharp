using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic bus (multi-signal connection path).
/// Similar to a wire but carries multiple signals.
/// </summary>
public sealed class SchBus : ISchBus
{
    private readonly List<CoordPoint> _vertices = new();

    /// <inheritdoc />
    public IReadOnlyList<CoordPoint> Vertices => _vertices;

    /// <summary>
    /// Line width index (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Line style (0=Solid, 1=Dashed, 2=Dotted, 3=DashDotted).
    /// </summary>
    public int LineStyle { get; set; }

    /// <summary>
    /// Bus color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Area/fill color (RGB).
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

    /// <summary>
    /// Adds a vertex to the bus path.
    /// </summary>
    public void AddVertex(CoordPoint vertex) => _vertices.Add(vertex);

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            if (_vertices.Count == 0)
                return new CoordRect(default, default);

            var minX = _vertices[0].X;
            var minY = _vertices[0].Y;
            var maxX = minX;
            var maxY = minY;

            foreach (var v in _vertices)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
            }

            return new CoordRect(new CoordPoint(minX, minY), new CoordPoint(maxX, maxY));
        }
    }
}
