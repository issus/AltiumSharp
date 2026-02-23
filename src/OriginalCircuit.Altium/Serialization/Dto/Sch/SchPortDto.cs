using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic port record.
/// Record type 18 in Altium schematic files.
/// </summary>
[AltiumRecord("18")]
internal sealed partial record SchPortDto
{
    /// <summary>
    /// Gets or sets the owner index linking this primitive to its parent.
    /// </summary>
    [AltiumParameter("OWNERINDEX")]
    public int OwnerIndex { get; init; }

    /// <summary>
    /// Gets or sets whether the primitive is not accessible.
    /// </summary>
    [AltiumParameter("ISNOTACCESIBLE")]
    public bool IsNotAccessible { get; init; }

    /// <summary>
    /// Gets or sets the index of this primitive in the sheet.
    /// </summary>
    [AltiumParameter("INDEXINSHEET")]
    public int IndexInSheet { get; init; }

    /// <summary>
    /// Gets or sets the owner part ID (for multi-part components).
    /// </summary>
    [AltiumParameter("OWNERPARTID")]
    public int OwnerPartId { get; init; }

    /// <summary>
    /// Gets or sets the owner part display mode.
    /// </summary>
    [AltiumParameter("OWNERPARTDISPLAYMODE")]
    public int OwnerPartDisplayMode { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the port location.
    /// </summary>
    [AltiumParameter("LOCATION.X")]
    [AltiumCoord]
    public int LocationX { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the X coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.X_FRAC")]
    public int LocationXFrac { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate of the port location.
    /// </summary>
    [AltiumParameter("LOCATION.Y")]
    [AltiumCoord]
    public int LocationY { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the Y coordinate.
    /// </summary>
    [AltiumParameter("LOCATION.Y_FRAC")]
    public int LocationYFrac { get; init; }

    /// <summary>
    /// Gets or sets the port name.
    /// </summary>
    [AltiumParameter("NAME")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the port I/O type (0=Unspecified, 1=Output, 2=Input, 3=Bidirectional).
    /// </summary>
    [AltiumParameter("IOTYPE")]
    public int IoType { get; init; }

    /// <summary>
    /// Gets or sets the port style.
    /// </summary>
    [AltiumParameter("STYLE")]
    public int Style { get; init; }

    /// <summary>
    /// Gets or sets the port alignment (0=Left, 1=Right).
    /// </summary>
    [AltiumParameter("ALIGNMENT")]
    public int Alignment { get; init; }

    /// <summary>
    /// Gets or sets the port width in internal units.
    /// </summary>
    [AltiumParameter("WIDTH")]
    [AltiumCoord]
    public int Width { get; init; }

    /// <summary>
    /// Gets or sets the port height in internal units.
    /// </summary>
    [AltiumParameter("HEIGHT")]
    [AltiumCoord]
    public int Height { get; init; }

    /// <summary>
    /// Gets or sets the border width (0=Small, 1=Medium, 2=Large).
    /// </summary>
    [AltiumParameter("BORDERWIDTH")]
    public int BorderWidth { get; init; }

    /// <summary>
    /// Gets or sets whether the port should automatically size to fit text.
    /// </summary>
    [AltiumParameter("AUTOSIZE")]
    public bool AutoSize { get; init; }

    /// <summary>
    /// Gets or sets which end of the port is connected to the net.
    /// </summary>
    [AltiumParameter("CONNECTEDEND")]
    public int ConnectedEnd { get; init; }

    /// <summary>
    /// Gets or sets the cross-reference text for hierarchical connections.
    /// </summary>
    [AltiumParameter("CROSSREFERENCE")]
    public string? CrossReference { get; init; }

    /// <summary>
    /// Gets or sets whether to show the net name on the port.
    /// </summary>
    [AltiumParameter("SHOWNETNAME")]
    public bool ShowNetName { get; init; }

    /// <summary>
    /// Gets or sets the harness type for harness ports.
    /// </summary>
    [AltiumParameter("HARNESSTYPE")]
    public string? HarnessType { get; init; }

    /// <summary>
    /// Gets or sets the harness color as a Win32 color value.
    /// </summary>
    [AltiumParameter("HARNESSCOLOR")]
    public int HarnessColor { get; init; }

    /// <summary>
    /// Gets or sets whether the port uses a custom style.
    /// </summary>
    [AltiumParameter("ISCUSTOMSTYLE")]
    public bool IsCustomStyle { get; init; }

    /// <summary>
    /// Gets or sets the font ID referencing a font definition in the document.
    /// </summary>
    [AltiumParameter("FONTID")]
    public int FontId { get; init; }

    /// <summary>
    /// Gets or sets the border color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets the area (fill) color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

    /// <summary>
    /// Gets or sets the text color as a Win32 color value.
    /// </summary>
    [AltiumParameter("TEXTCOLOR")]
    public int TextColor { get; init; }

    /// <summary>
    /// Gets or sets whether the primitive is graphically locked.
    /// </summary>
    [AltiumParameter("GRAPHICALLYLOCKED")]
    public bool GraphicallyLocked { get; init; }

    /// <summary>
    /// Gets or sets whether the primitive is disabled.
    /// </summary>
    [AltiumParameter("DISABLED")]
    public bool Disabled { get; init; }

    /// <summary>
    /// Gets or sets whether the primitive is dimmed in display.
    /// </summary>
    [AltiumParameter("DIMMED")]
    public bool Dimmed { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier for this port.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }
}
