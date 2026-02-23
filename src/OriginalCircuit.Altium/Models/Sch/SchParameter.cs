using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic parameter (key-value annotation).
/// </summary>
public sealed class SchParameter : ISchParameter
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Text orientation (0=horizontal, 1=90 degrees, 2=180, 3=270).
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    /// Text justification.
    /// </summary>
    public SchTextJustification Justification { get; set; } = SchTextJustification.BottomLeft;

    /// <summary>
    /// Font ID reference.
    /// </summary>
    public int FontId { get; set; }

    /// <summary>
    /// Parameter color (RGB).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Whether the parameter is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether the parameter name is hidden (only value shown).
    /// </summary>
    public bool HideName { get; set; }

    /// <summary>
    /// Parameter type (0=String, 1=Boolean, 2=Integer, 3=Float).
    /// </summary>
    public int ParamType { get; set; }

    /// <summary>
    /// Whether the parameter name is shown along with the value.
    /// </summary>
    public bool ShowName { get; set; }

    /// <summary>
    /// Whether the parameter text is mirrored.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Whether the parameter is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Parameter description text.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Area/background color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Auto-position setting (0=Manual, 1-4=Auto positions).
    /// </summary>
    public int AutoPosition { get; set; }

    /// <summary>
    /// Whether this parameter is configurable in variants.
    /// </summary>
    public bool IsConfigurable { get; set; }

    /// <summary>
    /// Whether this parameter is a design rule.
    /// </summary>
    public bool IsRule { get; set; }

    /// <summary>
    /// Whether this is a system parameter.
    /// </summary>
    public bool IsSystemParameter { get; set; }

    /// <summary>
    /// Horizontal text anchor position.
    /// </summary>
    public int TextHorzAnchor { get; set; }

    /// <summary>
    /// Vertical text anchor position.
    /// </summary>
    public int TextVertAnchor { get; set; }

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
    /// Whether database synchronization is allowed (Designator specific).
    /// </summary>
    public bool AllowDatabaseSynchronize { get; set; }

    /// <summary>
    /// Whether library synchronization is allowed (Designator specific).
    /// </summary>
    public bool AllowLibrarySynchronize { get; set; }

    /// <summary>
    /// Whether the parameter name is read-only (Designator specific).
    /// </summary>
    public bool NameIsReadOnly { get; set; }

    /// <summary>
    /// Physical designator string (Designator specific).
    /// </summary>
    public string? PhysicalDesignator { get; set; }

    /// <summary>
    /// Whether the parameter value is read-only (Designator specific).
    /// </summary>
    public bool ValueIsReadOnly { get; set; }

    /// <summary>
    /// Variant option (Designator specific).
    /// </summary>
    public string? VariantOption { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            // Approximate bounds based on text length
            var text = HideName ? Value : $"{Name}={Value}";
            var approxWidth = Coord.FromMils(text.Length * 50);
            var approxHeight = Coord.FromMils(80);
            return new CoordRect(Location, new CoordPoint(Location.X + approxWidth, Location.Y + approxHeight));
        }
    }

    /// <summary>
    /// Creates a fluent builder for a new parameter.
    /// </summary>
    public static ParameterBuilder Create(string name) => new(name);
}

/// <summary>
/// Fluent builder for creating schematic parameters.
/// </summary>
public sealed class ParameterBuilder
{
    private readonly SchParameter _param = new();

    internal ParameterBuilder(string name)
    {
        _param.Name = name;
    }

    /// <summary>
    /// Sets the parameter location.
    /// </summary>
    public ParameterBuilder At(Coord x, Coord y)
    {
        _param.Location = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the parameter value.
    /// </summary>
    public ParameterBuilder WithValue(string value)
    {
        _param.Value = value;
        return this;
    }

    /// <summary>
    /// Sets the text orientation.
    /// </summary>
    public ParameterBuilder Orientation(int orientation)
    {
        _param.Orientation = orientation;
        return this;
    }

    /// <summary>
    /// Sets the text justification.
    /// </summary>
    public ParameterBuilder Justify(SchTextJustification justification)
    {
        _param.Justification = justification;
        return this;
    }

    /// <summary>
    /// Sets the font ID.
    /// </summary>
    public ParameterBuilder Font(int fontId)
    {
        _param.FontId = fontId;
        return this;
    }

    /// <summary>
    /// Sets the parameter color.
    /// </summary>
    public ParameterBuilder Color(int color)
    {
        _param.Color = color;
        return this;
    }

    /// <summary>
    /// Sets whether the parameter is visible.
    /// </summary>
    public ParameterBuilder Visible(bool visible = true)
    {
        _param.IsVisible = visible;
        return this;
    }

    /// <summary>
    /// Sets whether to hide the parameter name.
    /// </summary>
    public ParameterBuilder HideName(bool hide = true)
    {
        _param.HideName = hide;
        return this;
    }

    /// <summary>
    /// Sets whether the parameter is read-only.
    /// </summary>
    public ParameterBuilder ReadOnly(bool readOnly = true)
    {
        _param.IsReadOnly = readOnly;
        return this;
    }

    /// <summary>
    /// Builds the parameter.
    /// </summary>
    public SchParameter Build() => _param;

    /// <summary>Implicitly converts a <see cref="ParameterBuilder"/> to a <see cref="SchParameter"/>.</summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator SchParameter(ParameterBuilder builder) => builder.Build();
}
