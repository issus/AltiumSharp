using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic blanket (group directive region).
/// Blankets apply directives to multiple nets covered by their area.
/// </summary>
public sealed class SchBlanket
{
    private readonly List<CoordPoint> _vertices = new();
    private readonly List<SchParameter> _parameters = new();

    /// <summary>
    /// Vertices defining the blanket boundary.
    /// </summary>
    public IReadOnlyList<CoordPoint> Vertices => _vertices;

    /// <summary>
    /// Parameters (directives) attached to this blanket.
    /// </summary>
    public IReadOnlyList<SchParameter> Parameters => _parameters;

    /// <summary>
    /// Whether the blanket is collapsed in the UI.
    /// </summary>
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// Line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Line style (0=Solid, 1=Dashed, 2=Dotted, 3=DashDot).
    /// </summary>
    public int LineStyle { get; set; }

    /// <summary>
    /// Whether the blanket area is filled.
    /// </summary>
    public bool IsSolid { get; set; }

    /// <summary>
    /// Whether the fill is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

    /// <summary>
    /// Border color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Fill/area color (RGB).
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
    /// Bounding rectangle of the blanket.
    /// </summary>
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
    /// Adds a vertex to the blanket boundary.
    /// </summary>
    internal void AddVertex(CoordPoint point) => _vertices.Add(point);

    /// <summary>
    /// Adds a parameter to the blanket.
    /// </summary>
    internal void AddParameter(SchParameter parameter) => _parameters.Add(parameter);
}
