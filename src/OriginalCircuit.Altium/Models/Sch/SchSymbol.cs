using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic symbol reference (built-in symbol like GND, VCC, etc.).
/// </summary>
public sealed class SchSymbol : ISchSymbol
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <summary>
    /// Symbol type ID (references a built-in symbol).
    /// </summary>
    public int SymbolType { get; set; }

    /// <summary>
    /// Whether the symbol is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Rotation angle in degrees (0, 90, 180, 270).
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    /// Line width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Scale factor for the symbol.
    /// </summary>
    public int ScaleFactor { get; set; } = 1;

    /// <summary>
    /// Symbol color (RGB).
    /// </summary>
    public int Color { get; set; }

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
            // Approximate bounds - symbols have varying sizes
            var size = Coord.FromMils(100 * ScaleFactor);
            return new CoordRect(
                new CoordPoint(Location.X - size, Location.Y - size),
                new CoordPoint(Location.X + size, Location.Y + size));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new symbol.
    /// </summary>
    public static SymbolBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic symbols.
/// </summary>
public sealed class SymbolBuilder
{
    private readonly SchSymbol _symbol = new();

    internal SymbolBuilder() { }

    /// <summary>
    /// Sets the symbol location.
    /// </summary>
    public SymbolBuilder At(Coord x, Coord y)
    {
        _symbol.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the symbol type ID.
    /// </summary>
    public SymbolBuilder Type(int symbolType)
    {
        _symbol.SymbolType = symbolType;
        return this;
    }

    /// <summary>
    /// Sets whether the symbol is mirrored.
    /// </summary>
    public SymbolBuilder Mirrored(bool mirrored = true)
    {
        _symbol.IsMirrored = mirrored;
        return this;
    }

    /// <summary>
    /// Sets the rotation orientation.
    /// </summary>
    public SymbolBuilder Orientation(int orientation)
    {
        _symbol.Orientation = orientation;
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public SymbolBuilder LineWidth(int width)
    {
        _symbol.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the scale factor.
    /// </summary>
    public SymbolBuilder Scale(int factor)
    {
        _symbol.ScaleFactor = factor;
        return this;
    }

    /// <summary>
    /// Sets the symbol color.
    /// </summary>
    public SymbolBuilder Color(int color)
    {
        _symbol.Color = color;
        return this;
    }

    /// <summary>
    /// Builds the symbol.
    /// </summary>
    public SchSymbol Build() => _symbol;

    /// <summary>Implicitly converts a <see cref="SymbolBuilder"/> to a <see cref="SchSymbol"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchSymbol(SymbolBuilder builder) => builder.Build();
}
