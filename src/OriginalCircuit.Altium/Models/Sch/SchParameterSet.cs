using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic parameter set (design directive).
/// Parameter sets attach design rules and directives to net-type objects.
/// </summary>
public sealed class SchParameterSet
{
    private readonly List<SchParameter> _parameters = new();

    /// <summary>
    /// Child parameters (directives) in this set.
    /// </summary>
    public IReadOnlyList<SchParameter> Parameters => _parameters;

    /// <summary>
    /// Location of the parameter set marker.
    /// </summary>
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Orientation (0=horizontal, 1=90 degrees, 2=180, 3=270).
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    /// Display style.
    /// </summary>
    public int Style { get; set; }

    /// <summary>
    /// Border/marker color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Fill/area color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Name of the parameter set.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether hidden fields are shown.
    /// </summary>
    public bool ShowHiddenFields { get; set; }

    /// <summary>
    /// Border width.
    /// </summary>
    public int BorderWidth { get; set; }

    /// <summary>
    /// Whether the parameter set marker is filled.
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

    /// <summary>
    /// Bounding rectangle.
    /// </summary>
    public CoordRect Bounds
    {
        get
        {
            var size = Coord.FromMils(100);
            return new CoordRect(Location, new CoordPoint(Location.X + size, Location.Y + size));
        }
    }

    /// <summary>
    /// Adds a parameter to the set.
    /// </summary>
    internal void AddParameter(SchParameter parameter) => _parameters.Add(parameter);
}
