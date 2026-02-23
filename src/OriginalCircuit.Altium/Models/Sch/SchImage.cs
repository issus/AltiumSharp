using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Models.Sch;

/// <summary>
/// Represents a schematic image primitive.
/// </summary>
public sealed class SchImage : ISchImage
{
    /// <inheritdoc />
    public CoordPoint Corner1 { get; set; }

    /// <inheritdoc />
    public CoordPoint Corner2 { get; set; }

    /// <summary>
    /// Whether to maintain aspect ratio when resizing.
    /// </summary>
    public bool KeepAspect { get; set; } = true;

    /// <summary>
    /// Whether the image is embedded in the file.
    /// </summary>
    public bool EmbedImage { get; set; } = true;

    /// <summary>
    /// External filename if not embedded.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// Embedded image data (raw bytes).
    /// </summary>
    public byte[]? ImageData { get; set; }

    /// <summary>
    /// Border color (RGB).
    /// </summary>
    public int BorderColor { get; set; }

    /// <summary>
    /// Whether the border is visible.
    /// </summary>
    public bool ShowBorder { get; set; }

    /// <summary>
    /// Line width for border.
    /// </summary>
    public int LineWidth { get; set; }

    /// <summary>
    /// Area/fill color (RGB).
    /// </summary>
    public int AreaColor { get; set; }

    /// <summary>
    /// Whether the image frame is filled.
    /// </summary>
    public bool IsSolid { get; set; }

    /// <summary>
    /// Line style for border.
    /// </summary>
    public int LineStyle { get; set; }

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
    public CoordRect Bounds => new(Corner1, Corner2);

    /// <summary>
    /// Creates a fluent builder for a new image.
    /// </summary>
    public static ImageBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating schematic images.
/// </summary>
public sealed class ImageBuilder
{
    private readonly SchImage _image = new();

    internal ImageBuilder() { }

    /// <summary>
    /// Sets the first corner.
    /// </summary>
    public ImageBuilder From(Coord x, Coord y)
    {
        _image.Corner1 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets the second corner.
    /// </summary>
    public ImageBuilder To(Coord x, Coord y)
    {
        _image.Corner2 = new CoordPoint(x, y);
        return this;
    }

    /// <summary>
    /// Sets whether to maintain aspect ratio.
    /// </summary>
    public ImageBuilder KeepAspect(bool keep = true)
    {
        _image.KeepAspect = keep;
        return this;
    }

    /// <summary>
    /// Sets the external filename.
    /// </summary>
    public ImageBuilder FromFile(string filename)
    {
        _image.Filename = filename;
        _image.EmbedImage = false;
        return this;
    }

    /// <summary>
    /// Sets the embedded image data.
    /// </summary>
    public ImageBuilder WithData(byte[] data)
    {
        _image.ImageData = data;
        _image.EmbedImage = true;
        return this;
    }

    /// <summary>
    /// Sets the border color.
    /// </summary>
    public ImageBuilder BorderColor(int color)
    {
        _image.BorderColor = color;
        return this;
    }

    /// <summary>
    /// Sets whether the border is visible.
    /// </summary>
    public ImageBuilder ShowBorder(bool show = true)
    {
        _image.ShowBorder = show;
        return this;
    }

    /// <summary>
    /// Sets the line width.
    /// </summary>
    public ImageBuilder LineWidth(int width)
    {
        _image.LineWidth = width;
        return this;
    }

    /// <summary>
    /// Builds the image.
    /// </summary>
    public SchImage Build() => _image;

    public static implicit operator SchImage(ImageBuilder builder) => builder.Build();
}
