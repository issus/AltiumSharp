using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic junction (connection point for wires).
/// </summary>
public sealed class SchJunction : ISchJunction
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Junction size (radius or diameter depending on style).
    /// </summary>
    public Coord Size { get; set; } = Coord.FromMils(25);

    /// <summary>
    /// Junction color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Whether the junction is locked.
    /// </summary>
    public bool Locked { get; set; }

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
            var halfSize = Size / 2;
            return new CoordRect(
                new CoordPoint(Location.X - halfSize, Location.Y - halfSize),
                new CoordPoint(Location.X + halfSize, Location.Y + halfSize));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new junction.
    /// </summary>
    public static JunctionBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic junctions.
/// </summary>
public sealed class JunctionBuilder
{
    private readonly SchJunction _junction = new();

    internal JunctionBuilder() { }

    /// <summary>
    /// Sets the junction location.
    /// </summary>
    public JunctionBuilder At(Coord x, Coord y)
    {
        _junction.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the junction size.
    /// </summary>
    public JunctionBuilder Size(Coord size)
    {
        _junction.Size = size;
        return this;
    }

    /// <summary>
    /// Sets the junction color.
    /// </summary>
    public JunctionBuilder Color(int color)
    {
        _junction.Color = color;
        return this;
    }

    /// <summary>
    /// Builds the junction.
    /// </summary>
    public SchJunction Build() => _junction;

    /// <summary>Implicitly converts a <see cref="JunctionBuilder"/> to a <see cref="SchJunction"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchJunction(JunctionBuilder builder) => builder.Build();
}
