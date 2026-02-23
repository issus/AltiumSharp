using OriginalCircuit.Altium.Generators;

namespace OriginalCircuit.Altium.Serialization.Dto.Sch;

/// <summary>
/// Data transfer object representing a schematic parameter record.
/// Record type 41 in Altium schematic files.
/// Parameters store name-value pairs attached to components.
/// </summary>
[AltiumRecord("41")]
internal sealed partial record SchParameterDto
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
    /// Gets or sets the X coordinate of the parameter location.
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
    /// Gets or sets the Y coordinate of the parameter location.
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
    /// Gets or sets the text color as a Win32 color value.
    /// </summary>
    [AltiumParameter("COLOR")]
    public int Color { get; init; }

    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    [AltiumParameter("NAME")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the parameter text value.
    /// </summary>
    [AltiumParameter("TEXT")]
    public string? Text { get; init; }

    /// <summary>
    /// Gets or sets the parameter description (alternative value representation).
    /// </summary>
    [AltiumParameter("DESCRIPTION")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// 0=String, 1=Boolean, 2=Integer, 3=Float.
    /// </summary>
    [AltiumParameter("PARAMTYPE")]
    public int ParamType { get; init; }

    /// <summary>
    /// Gets or sets the font ID referencing a font definition in the document.
    /// </summary>
    [AltiumParameter("FONTID")]
    public int FontId { get; init; }

    /// <summary>
    /// Gets or sets the text orientation (0=None, 1=Rotated, 2=Flipped, 3=Both).
    /// </summary>
    [AltiumParameter("ORIENTATION")]
    public int Orientation { get; init; }

    /// <summary>
    /// Gets or sets the text justification.
    /// 0=BottomLeft, 1=BottomCenter, 2=BottomRight, 3=MiddleLeft, 4=MiddleCenter,
    /// 5=MiddleRight, 6=TopLeft, 7=TopCenter, 8=TopRight.
    /// </summary>
    [AltiumParameter("JUSTIFICATION")]
    public int Justification { get; init; }

    /// <summary>
    /// Gets or sets whether the parameter name should be shown along with the value.
    /// </summary>
    [AltiumParameter("SHOWNAME")]
    public bool ShowName { get; init; }

    /// <summary>
    /// Gets or sets whether the text is mirrored.
    /// </summary>
    [AltiumParameter("ISMIRRORED")]
    public bool IsMirrored { get; init; }

    /// <summary>
    /// Gets or sets whether the parameter is hidden.
    /// </summary>
    [AltiumParameter("ISHIDDEN")]
    public bool IsHidden { get; init; }

    /// <summary>
    /// Gets or sets the read-only state of the parameter.
    /// </summary>
    [AltiumParameter("READONLYSTATE")]
    public int ReadOnlyState { get; init; }

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
    /// Gets or sets the unique identifier for this parameter.
    /// </summary>
    [AltiumParameter("UNIQUEID")]
    public string? UniqueId { get; init; }

    /// <summary>
    /// Gets or sets the area (fill) color as a Win32 color value.
    /// </summary>
    [AltiumParameter("AREACOLOR")]
    public int AreaColor { get; init; }

    /// <summary>
    /// Gets or sets the automatic positioning mode for the parameter.
    /// </summary>
    [AltiumParameter("AUTOPOSITION")]
    public int AutoPosition { get; init; }

    /// <summary>
    /// Gets or sets whether the parameter is configurable.
    /// </summary>
    [AltiumParameter("ISCONFIGURABLE")]
    public bool IsConfigurable { get; init; }

    /// <summary>
    /// Gets or sets whether the parameter represents a design rule.
    /// </summary>
    [AltiumParameter("ISRULE")]
    public bool IsRule { get; init; }

    /// <summary>
    /// Gets or sets whether the parameter is a system parameter.
    /// </summary>
    [AltiumParameter("ISSYSTEMPARAMETER")]
    public bool IsSystemParameter { get; init; }

    /// <summary>
    /// Gets or sets the horizontal text anchor mode.
    /// </summary>
    [AltiumParameter("TEXTHORZANCHOR")]
    public int TextHorzAnchor { get; init; }

    /// <summary>
    /// Gets or sets the vertical text anchor mode.
    /// </summary>
    [AltiumParameter("TEXTVERTANCHOR")]
    public int TextVertAnchor { get; init; }

    /// <summary>
    /// Gets or sets whether to hide the parameter name in display.
    /// </summary>
    [AltiumParameter("HIDENAME")]
    public bool HideName { get; init; }

    /// <summary>
    /// Gets or sets whether to allow database synchronization.
    /// </summary>
    [AltiumParameter("ALLOWDATABASESYNCHRONIZE")]
    public bool AllowDatabaseSynchronize { get; init; }

    /// <summary>
    /// Gets or sets whether to allow library synchronization.
    /// </summary>
    [AltiumParameter("ALLOWLIBRARYSYNCHRONIZE")]
    public bool AllowLibrarySynchronize { get; init; }

    /// <summary>
    /// Gets or sets whether the parameter name is read-only.
    /// </summary>
    [AltiumParameter("NAMEISREADONLY")]
    public bool NameIsReadOnly { get; init; }

    /// <summary>
    /// Gets or sets the physical designator for the parameter.
    /// </summary>
    [AltiumParameter("PHYSICALDESIGNATOR")]
    public string? PhysicalDesignator { get; init; }

    /// <summary>
    /// Gets or sets whether the parameter value is read-only.
    /// </summary>
    [AltiumParameter("VALUEISREADONLY")]
    public bool ValueIsReadOnly { get; init; }

    /// <summary>
    /// Gets or sets the variant option for this parameter.
    /// </summary>
    [AltiumParameter("VARIANTOPTION")]
    public string? VariantOption { get; init; }
}
