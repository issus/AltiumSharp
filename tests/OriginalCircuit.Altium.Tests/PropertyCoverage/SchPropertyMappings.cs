namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Maps JSON property names (from test data) to C# model/DTO property names for schematic types.
/// For schematic types, JSON keys are camelCase versions of the Altium parameter names.
/// A property being in this mapping means "the DTO models this property."
/// </summary>
public static class SchPropertyMappings
{
    /// <summary>
    /// SchComponent: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Component = new(StringComparer.OrdinalIgnoreCase)
    {
        ["designator"] = "DesignatorPrefix",
        ["comment"] = "Comment",
        ["libReference"] = "LibReference",
        ["description"] = "ComponentDescription",
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["orientation"] = "Orientation",
        ["isMirrored"] = "IsMirrored",
        ["partCount"] = "PartCount",
        ["currentPartId"] = "CurrentPartId",
        ["isMultiPart"] = "computed from PartCount",
        ["color"] = "Color",
        ["areaColor"] = "AreaColor",
        ["uniqueId"] = "UniqueId",
        ["componentKind"] = "ComponentKind",
        ["libraryPath"] = "LibraryPath",
        ["sourceLibraryName"] = "SourceLibraryName",
        ["displayMode"] = "DisplayMode",
        ["displayModeCount"] = "DisplayModeCount",
        ["showHiddenPins"] = "ShowHiddenPins",
        ["overideColors"] = "OverrideColors",
        ["designatorLocked"] = "DesignatorLocked",
        ["partIdLocked"] = "PartIdLocked",
        ["designItemId"] = "DesignItemId",
        ["symbolReference"] = "SymbolReference",
        ["graphicallyLocked"] = "GraphicallyLocked",
    };

    /// <summary>
    /// SchPin: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Pin = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = "Name",
        ["designator"] = "Designator",
        ["description"] = "Description",
        ["defaultValue"] = "DefaultValue",
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["orientation"] = "PinConglomerate (orientation bits)",
        ["electrical"] = "Electrical",
        ["pinLength"] = "PinLength",
        ["showName"] = "PinConglomerate (ShowName bit)",
        ["showDesignator"] = "PinConglomerate (ShowDesignator bit)",
        ["isHidden"] = "PinConglomerate (IsHidden bit)",
        ["color"] = "Color",
        ["areaColor"] = "AreaColor",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["uniqueId"] = "UniqueId",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["propagationDelay"] = "PinPropagationDelay",
        ["formalType"] = "FormalType",
        ["symbol_Inner"] = "SymbolInside",
        ["symbol_InnerEdge"] = "SymbolInnerEdge",
        ["symbol_Outer"] = "SymbolOutside",
        ["symbol_OuterEdge"] = "SymbolOuterEdge",
        ["symbol_LineWidth"] = "SymbolLineWidth",
        ["swapId_Part"] = "SwapIdPart",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["designator_CustomFontID"] = "DesignatorCustomFontId",
        ["name_CustomFontID"] = "NameCustomFontId",
        ["width"] = "Width",
    };

    /// <summary>
    /// SchWire: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Wire = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["lineStyle"] = "LineStyle",
        ["uniqueId"] = "UniqueId",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["ownerPartId"] = "OwnerPartId",
        // Vertices handled separately (x/y/x1/y1 pattern)
    };

    /// <summary>
    /// SchLine: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Line = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x1"] = "LocationX",
        ["y1"] = "LocationY",
        ["x2"] = "CornerX",
        ["y2"] = "CornerY",
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["lineStyle"] = "LineStyle",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["areaColor"] = "AreaColor",
    };

    /// <summary>
    /// SchRectangle: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Rectangle = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x1"] = "LocationX",
        ["y1"] = "LocationY",
        ["x2"] = "CornerX",
        ["y2"] = "CornerY",
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["areaColor"] = "AreaColor",
        ["isSolid"] = "IsSolid",
        ["transparent"] = "Transparent",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["uniqueId"] = "UniqueId",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["lineStyle"] = "LineStyle",
    };

    /// <summary>
    /// SchLabel: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Label = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text"] = "Text",
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["fontId"] = "FontId",
        ["justification"] = "Justification",
        ["orientation"] = "Orientation",
        ["color"] = "Color",
        ["isHidden"] = "IsHidden",
        ["isMirrored"] = "IsMirrored",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["areaColor"] = "AreaColor",
    };

    /// <summary>
    /// SchParameter: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Parameter = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["name"] = "Name",
        ["text"] = "Text",
        ["orientation"] = "Orientation",
        ["justification"] = "Justification",
        ["fontId"] = "FontId",
        ["color"] = "Color",
        ["isHidden"] = "IsHidden",
        ["readOnlyState"] = "ReadOnlyState",
        ["uniqueId"] = "UniqueId",
        ["isMirrored"] = "IsMirrored",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        // NOT YET ON DTO:
        // graphicallyLocked, isNotAccessible, dimmed, disabled
    };

    /// <summary>
    /// SchNetLabel: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> NetLabel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text"] = "Text",
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["orientation"] = "Orientation",
        ["justification"] = "Justification",
        ["fontId"] = "FontId",
        ["color"] = "Color",
        ["isMirrored"] = "IsMirrored",
        ["uniqueId"] = "UniqueId",
        ["ownerPartId"] = "OwnerPartId",
        // NOT YET ON DTO:
        // graphicallyLocked, areaColor, isNotAccessible
    };

    /// <summary>
    /// SchArc: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Arc = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["radius"] = "Radius",
        ["startAngle"] = "StartAngle",
        ["endAngle"] = "EndAngle",
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["areaColor"] = "AreaColor",
    };

    /// <summary>
    /// SchPolygon: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Polygon = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["areaColor"] = "AreaColor",
        ["isSolid"] = "IsSolid",
        ["transparent"] = "Transparent",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        // Vertices handled separately
    };

    /// <summary>
    /// SchPolyline: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Polyline = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["lineStyle"] = "LineStyle",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["endLineShape"] = "EndLineShape",
        ["startLineShape"] = "StartLineShape",
        ["lineShapeSize"] = "LineShapeSize",
        ["transparent"] = "Transparent",
        ["areaColor"] = "AreaColor",
        // Vertices handled separately
    };

    /// <summary>
    /// SchBezier: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Bezier = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        // Control points handled separately
    };

    /// <summary>
    /// SchEllipse: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Ellipse = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["xRadius"] = "Radius (primary)",
        ["yRadius"] = "SecondaryRadius",
        ["radius"] = "Radius",
        ["secondaryRadius"] = "SecondaryRadius",
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["areaColor"] = "AreaColor",
        ["isSolid"] = "IsSolid",
        ["transparent"] = "Transparent",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
    };

    /// <summary>
    /// SchPie: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Pie = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["radius"] = "Radius",
        ["startAngle"] = "StartAngle",
        ["endAngle"] = "EndAngle",
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["areaColor"] = "AreaColor",
        ["isSolid"] = "IsSolid",
        ["transparent"] = "Transparent",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
    };

    /// <summary>
    /// SchRoundedRectangle: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> RoundedRectangle = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x1"] = "LocationX",
        ["y1"] = "LocationY",
        ["x2"] = "CornerX",
        ["y2"] = "CornerY",
        ["xRadius"] = "CornerXRadius",
        ["yRadius"] = "CornerYRadius",
        ["cornerXRadius"] = "CornerXRadius",
        ["cornerYRadius"] = "CornerYRadius",
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["areaColor"] = "AreaColor",
        ["isSolid"] = "IsSolid",
        ["transparent"] = "Transparent",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["lineStyle"] = "LineStyle",
    };

    /// <summary>
    /// SchEllipticalArc: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> EllipticalArc = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["xRadius"] = "Radius (primary)",
        ["yRadius"] = "SecondaryRadius",
        ["radiusX"] = "Radius",
        ["secondaryRadius"] = "SecondaryRadius",
        ["startAngle"] = "StartAngle",
        ["endAngle"] = "EndAngle",
        ["lineWidth"] = "LineWidth",
        ["color"] = "Color",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["areaColor"] = "AreaColor",
    };

    /// <summary>
    /// SchTextFrame: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> TextFrame = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text"] = "Text",
        ["x1"] = "LocationX",
        ["y1"] = "LocationY",
        ["x2"] = "CornerX",
        ["y2"] = "CornerY",
        ["fontId"] = "FontId",
        ["color"] = "TextColor",
        ["areaColor"] = "AreaColor",
        ["showBorder"] = "ShowBorder",
        ["isSolid"] = "IsSolid",
        ["wordWrap"] = "WordWrap",
        ["clipToRect"] = "ClipToRect",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["uniqueId"] = "UniqueId",
        ["alignment"] = "Alignment",
        ["dimmed"] = "Dimmed",
        ["disabled"] = "Disabled",
        ["textColor"] = "TextColor",
        ["textMargin"] = "TextMargin",
        ["lineWidth"] = "LineWidth",
        ["lineStyle"] = "LineStyle",
        ["transparent"] = "Transparent",
    };

    /// <summary>
    /// SchImage: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Image = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x1"] = "LocationX",
        ["y1"] = "LocationY",
        ["x2"] = "CornerX",
        ["y2"] = "CornerY",
        ["keepAspect"] = "KeepAspect",
        ["embedImage"] = "EmbedImage",
        ["filename"] = "Filename",
        ["showBorder"] = "ShowBorder",
        ["lineWidth"] = "LineWidth",
        ["uniqueId"] = "UniqueId",
        ["graphicallyLocked"] = "GraphicallyLocked",
        ["ownerPartId"] = "OwnerPartId",
        ["ownerPartDisplayMode"] = "OwnerPartDisplayMode",
        // NOT YET ON DTO:
        // borderColor, imageData
    };

    /// <summary>
    /// SchSymbol: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Symbol = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["symbolType"] = "SymbolType",
        ["isMirrored"] = "IsMirrored",
        ["orientation"] = "Orientation",
        ["lineWidth"] = "LineWidth",
        ["scaleFactor"] = "ScaleFactor",
        ["color"] = "Color",
        ["uniqueId"] = "UniqueId",
        ["ownerPartId"] = "OwnerPartId",
        // NOT YET ON DTO:
        // graphicallyLocked
    };

    /// <summary>
    /// SchJunction: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> Junction = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["color"] = "Color",
        ["uniqueId"] = "UniqueId",
        ["ownerPartId"] = "OwnerPartId",
        // NOT YET ON DTO:
        // size
    };

    /// <summary>
    /// SchPowerObject: JSON key → description of mapped C# property.
    /// </summary>
    public static readonly Dictionary<string, string> PowerObject = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text"] = "Text",
        ["x"] = "LocationX",
        ["y"] = "LocationY",
        ["style"] = "Style",
        ["rotation"] = "Orientation",
        ["showNetName"] = "ShowNetName",
        ["color"] = "Color",
        ["fontId"] = "FontId",
        ["uniqueId"] = "UniqueId",
        ["ownerPartId"] = "OwnerPartId",
        // NOT YET ON DTO:
        // graphicallyLocked, isCrossSheetConnector
    };
}
