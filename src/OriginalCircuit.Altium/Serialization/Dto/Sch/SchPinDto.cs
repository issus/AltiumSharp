using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic pin record.
/// Record type 2 in Altium schematic files.
/// </summary>
[AltiumRecord("2")]
internal sealed partial record SchPinDto
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
    /// Gets or sets the index of this primitive in the sheet.
    /// </summary>
    [AltiumParameter("INDEXINSHEET")]
    public int IndexInSheet { get; init; }

    /// <summary>
    /// Gets or sets the X coordinate of the pin location.
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
    /// Gets or sets the Y coordinate of the pin location.
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
    /// Gets or sets the pin color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets the pin name.
    /// </summary>
    [AltiumParameter("NAME")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the pin designator (e.g., "1", "2", "A1").
    /// </summary>
    [AltiumParameter("DESIGNATOR")]
    public string? Designator { get; init; }

    /// <summary>
    /// Gets or sets the pin description.
    /// </summary>
    [AltiumParameter("DESCRIPTION")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the pin length in internal units.
    /// </summary>
    [AltiumParameter("PINLENGTH")]
    [AltiumCoord]
    public int PinLength { get; init; }

    /// <summary>
    /// Gets or sets the fractional part of the pin length.
    /// </summary>
    [AltiumParameter("PINLENGTH_FRAC")]
    public int PinLengthFrac { get; init; }

    /// <summary>
    /// Gets or sets the pin conglomerate flags (orientation, visibility, etc.).
    /// Bit 0: Rotated, Bit 1: Flipped, Bit 2: Hide, Bit 3: DisplayNameVisible, Bit 4: DesignatorVisible.
    /// </summary>
    [AltiumParameter("PINCONGLOMERATE")]
    public int PinConglomerate { get; init; }

    /// <summary>
    /// Gets or sets the electrical type of the pin.
    /// 0=Input, 1=InputOutput, 2=Output, 3=OpenCollector, 4=Passive, 5=HiZ, 6=OpenEmitter, 7=Power.
    /// </summary>
    [AltiumParameter("ELECTRICAL")]
    public int Electrical { get; init; }

    /// <summary>
    /// Gets or sets the formal type of the pin.
    /// </summary>
    [AltiumParameter("FORMALTYPE")]
    public int FormalType { get; init; }

    /// <summary>
    /// Gets or sets the symbol for the inner edge of the pin.
    /// </summary>
    [AltiumParameter("SYMBOL_INNEREDGE")]
    public int SymbolInnerEdge { get; init; }

    /// <summary>
    /// Gets or sets the symbol for the outer edge of the pin.
    /// </summary>
    [AltiumParameter("SYMBOL_OUTEREDGE")]
    public int SymbolOuterEdge { get; init; }

    /// <summary>
    /// Gets or sets the symbol displayed inside the pin.
    /// </summary>
    [AltiumParameter("SYMBOL_INSIDE")]
    public int SymbolInside { get; init; }

    /// <summary>
    /// Gets or sets the symbol displayed outside the pin.
    /// </summary>
    [AltiumParameter("SYMBOL_OUTSIDE")]
    public int SymbolOutside { get; init; }

    /// <summary>
    /// Gets or sets the line width of pin symbols.
    /// </summary>
    [AltiumParameter("SYMBOL_LINEWIDTH")]
    public int SymbolLineWidth { get; init; }

    /// <summary>
    /// Gets or sets the swap ID part for pin swapping.
    /// </summary>
    [AltiumParameter("SWAPIDPART")]
    public int SwapIdPart { get; init; }

    /// <summary>
    /// Gets or sets the pin propagation delay.
    /// </summary>
    [AltiumParameter("PINPROPAGATIONDELAY")]
    public double PinPropagationDelay { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier for this pin.
    /// </summary>
    /// <summary>
    /// Gets or sets the custom font ID for the designator text.
    /// </summary>
    [AltiumParameter("DESIGNATOR.CUSTOMFONTID")]
    public int DesignatorCustomFontId { get; init; }

    /// <summary>
    /// Gets or sets the custom font ID for the name text.
    /// </summary>
    [AltiumParameter("NAME.CUSTOMFONTID")]
    public int NameCustomFontId { get; init; }

    /// <summary>
    /// Gets or sets the pin width.
    /// </summary>
    [AltiumParameter("WIDTH")]
    public int Width { get; init; }

    /// <summary>
    /// Gets or sets the area (fill) color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

    /// <summary>
    /// Gets or sets the default value for the pin.
    /// </summary>
    [AltiumParameter("DEFAULTVALUE")]
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets or sets whether the pin is hidden.
    /// </summary>
    [AltiumParameter("ISHIDDEN")]
    public bool IsHidden { get; init; }

    /// <summary>
    /// Gets or sets the custom color for the designator text as a Win32 color value.
    /// </summary>
    [AltiumParameter("DESIGNATOR.CUSTOMCOLOR")]
    public int DesignatorCustomColor { get; init; }

    /// <summary>
    /// Gets or sets the margin for the custom designator position.
    /// </summary>
    [AltiumParameter("DESIGNATOR.CUSTOMPOSITION.MARGIN")]
    public int DesignatorCustomPositionMargin { get; init; }

    /// <summary>
    /// Gets or sets the rotation anchor for the custom designator position.
    /// </summary>
    [AltiumParameter("DESIGNATOR.CUSTOMPOSITION.ROTATIONANCHOR")]
    public int DesignatorCustomPositionRotationAnchor { get; init; }

    /// <summary>
    /// Gets or sets whether the designator rotation is relative to the pin.
    /// </summary>
    [AltiumParameter("DESIGNATOR.CUSTOMPOSITION.ROTATIONRELATIVE")]
    public bool DesignatorCustomPositionRotationRelative { get; init; }

    /// <summary>
    /// Gets or sets the font mode for the designator text.
    /// </summary>
    [AltiumParameter("DESIGNATOR.FONTMODE")]
    public int DesignatorFontMode { get; init; }

    /// <summary>
    /// Gets or sets the position mode for the designator text.
    /// </summary>
    [AltiumParameter("DESIGNATOR.POSITIONMODE")]
    public int DesignatorPositionMode { get; init; }

    /// <summary>
    /// Gets or sets the custom color for the name text as a Win32 color value.
    /// </summary>
    [AltiumParameter("NAME.CUSTOMCOLOR")]
    public int NameCustomColor { get; init; }

    /// <summary>
    /// Gets or sets the margin for the custom name position.
    /// </summary>
    [AltiumParameter("NAME.CUSTOMPOSITION.MARGIN")]
    public int NameCustomPositionMargin { get; init; }

    /// <summary>
    /// Gets or sets the rotation anchor for the custom name position.
    /// </summary>
    [AltiumParameter("NAME.CUSTOMPOSITION.ROTATIONANCHOR")]
    public int NameCustomPositionRotationAnchor { get; init; }

    /// <summary>
    /// Gets or sets whether the name rotation is relative to the pin.
    /// </summary>
    [AltiumParameter("NAME.CUSTOMPOSITION.ROTATIONRELATIVE")]
    public bool NameCustomPositionRotationRelative { get; init; }

    /// <summary>
    /// Gets or sets the font mode for the name text.
    /// </summary>
    [AltiumParameter("NAME.FONTMODE")]
    public int NameFontMode { get; init; }

    /// <summary>
    /// Gets or sets the position mode for the name text.
    /// </summary>
    [AltiumParameter("NAME.POSITIONMODE")]
    public int NamePositionMode { get; init; }

    /// <summary>
    /// Gets or sets the swap ID for pin pair swapping.
    /// </summary>
    [AltiumParameter("SWAPID_PAIR")]
    public string? SwapIdPair { get; init; }

    /// <summary>
    /// Gets or sets the swap ID for part-pin swapping.
    /// </summary>
    [AltiumParameter("SWAPID_PARTPIN")]
    public string? SwapIdPartPin { get; init; }

    /// <summary>
    /// Gets or sets the swap ID for pin swapping.
    /// </summary>
    [AltiumParameter("SWAPID_PIN")]
    public string? SwapIdPin { get; init; }

    /// <summary>
    /// Gets or sets the hidden net name associated with this pin.
    /// </summary>
    [AltiumParameter("HIDDENNETNAME")]
    public string? HiddenNetName { get; init; }

    /// <summary>
    /// Gets or sets the pin package length in internal units.
    /// </summary>
    [AltiumParameter("PINPACKAGELENGTH")]
    [AltiumCoord]
    public int PinPackageLength { get; init; }

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
    /// Gets or sets the unique identifier for this pin.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }
}
