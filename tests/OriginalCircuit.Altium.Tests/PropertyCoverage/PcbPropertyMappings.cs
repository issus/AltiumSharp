namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Maps JSON property names (from test data) to C# model property names.
/// A property being in this mapping means "v2 models this property."
/// A property NOT in this mapping (and not in the skip list) means "v2 is missing this."
///
/// NOTE: Properties in CoverageTestBase.SkipProperties are filtered out before
/// mapping lookup, so they should NOT appear here. Common skipped properties include:
/// allowGlobalEdit, moveable, isFreePrimitive, isPreRoute, userRouted, unionIndex,
/// polygonOutline, tearDrop, layer_V6, isTestpoint_*, isAssyTestpoint_*, isTenting,
/// pasteMaskExpansion, solderMaskExpansion, powerPlane*, relief*, isHidden, net
/// </summary>
public static class PcbPropertyMappings
{
    /// <summary>
    /// PcbPad: JSON key → C# property name on PcbPad model.
    /// </summary>
    public static readonly Dictionary<string, string> Pad = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = "Designator",
        ["x"] = "Location.X",
        ["y"] = "Location.Y",
        ["topXSize"] = "SizeTop.X",
        ["topYSize"] = "SizeTop.Y",
        ["midXSize"] = "SizeMiddle.X",
        ["midYSize"] = "SizeMiddle.Y",
        ["botXSize"] = "SizeBottom.X",
        ["botYSize"] = "SizeBottom.Y",
        ["holeSize"] = "HoleSize",
        ["topShape"] = "ShapeTop",
        ["midShape"] = "ShapeMiddle",
        ["botShape"] = "ShapeBottom",
        ["holeType"] = "HoleType",
        ["rotation"] = "Rotation",
        ["plated"] = "IsPlated",
        ["layer"] = "Layer",
        ["mode"] = "Mode",
        ["holeWidth"] = "HoleWidth",
        ["holeRotation"] = "HoleRotation",
        ["drillType"] = "DrillType",
        ["uniqueId"] = "UniqueId",
        ["swapID_Pad"] = "SwapIdPad",
        ["swapID_Part"] = "SwapIdPart",
        ["enabled"] = "Enabled",
        ["isKeepout"] = "IsKeepout",
        // NOT YET ON MODEL (these VARY in test data):
        // pinDescriptor, isSurfaceMount, isPadStack,
        // isTopPasteEnabled, isBottomPasteEnabled,
        // hasCornerRadiusChamfer, hasRoundedRectangularShapes,
        // maxXSignalLayers, maxYSignalLayers
    };

    /// <summary>
    /// PcbVia: JSON key → C# property name on PcbVia model.
    /// </summary>
    public static readonly Dictionary<string, string> Via = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "Location.X",
        ["y"] = "Location.Y",
        ["size"] = "Diameter",
        ["holeSize"] = "HoleSize",
        ["lowLayer"] = "StartLayer",
        ["highLayer"] = "EndLayer",
        ["uniqueId"] = "UniqueId",
        ["enabled"] = "Enabled",
        ["isKeepout"] = "IsKeepout",
        ["layer"] = "Layer",
        ["mode"] = "Mode",
        ["height"] = "Height",
        ["plated"] = "IsPlated",
    };

    /// <summary>
    /// PcbTrack: JSON key → C# property name on PcbTrack model.
    /// </summary>
    public static readonly Dictionary<string, string> Track = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x1"] = "Start.X",
        ["y1"] = "Start.Y",
        ["x2"] = "End.X",
        ["y2"] = "End.Y",
        ["width"] = "Width",
        ["layer"] = "Layer",
        ["uniqueId"] = "UniqueId",
        ["enabled"] = "Enabled",
        ["isKeepout"] = "IsKeepout",
        // NOTE: userRouted, unionIndex, polygonOutline now in global skip list
        // NOT YET ON MODEL:
        // isTenting_Top, isTenting_Bottom
    };

    /// <summary>
    /// PcbArc: JSON key → C# property name on PcbArc model.
    /// </summary>
    public static readonly Dictionary<string, string> Arc = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "Center.X",
        ["y"] = "Center.Y",
        ["radius"] = "Radius",
        ["startAngle"] = "StartAngle",
        ["endAngle"] = "EndAngle",
        ["width"] = "Width",
        ["layer"] = "Layer",
        ["uniqueId"] = "UniqueId",
        ["enabled"] = "Enabled",
        ["isKeepout"] = "IsKeepout",
        ["lineWidth"] = "Width", // Same as width, naming alias in Altium API
    };

    /// <summary>
    /// PcbText: JSON key → C# property name on PcbText model.
    /// </summary>
    public static readonly Dictionary<string, string> Text = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = "Location.X",
        ["y"] = "Location.Y",
        ["height"] = "Height",
        ["strokeWidth"] = "StrokeWidth",
        ["rotation"] = "Rotation",
        ["layer"] = "Layer",
        ["isMirrored"] = "IsMirrored",
        ["isTrueType"] = "IsTrueType",
        ["fontName"] = "FontName",
        ["text"] = "Text",
        ["justification"] = "Justification",
        ["uniqueId"] = "UniqueId",
        ["enabled"] = "Enabled",
        ["textKind"] = "TextKind",
        ["bold"] = "FontBold",
        ["italic"] = "FontItalic",
        ["inverted"] = "IsInverted",
        ["useInvertedRectangle"] = "UseInvertedRectangle",
        ["fontID"] = "FontId",
        ["size"] = "Size",
        ["width"] = "Width",
        ["multiLine"] = "MultiLine",
        ["wordWrap"] = "WordWrap",
        ["useTTFonts"] = "UseTTFonts",
        ["mirrorFlag"] = "MirrorFlag",
        ["isKeepout"] = "IsKeepout",
        ["barCodeKind"] = "BarCodeKind",
        ["barCodeShowText"] = "BarCodeShowText",
        ["multilineTextHeight"] = "MultilineTextHeight",
        ["multilineTextWidth"] = "MultilineTextWidth",
        ["multilineTextResizeEnabled"] = "MultilineTextResizeEnabled",
        // Computed by Altium (now in skip list):
        // snapPointX/Y, x1/y1/x2/y2Location, ttfTextHeight/Width,
        // barCodeBitPattern, barCodeFullHeight/Width, barCodeMinWidth,
        // invRectHeight/Width
    };

    /// <summary>
    /// PcbFill: JSON key → C# property name on PcbFill model.
    /// </summary>
    public static readonly Dictionary<string, string> Fill = new(StringComparer.OrdinalIgnoreCase)
    {
        ["x1"] = "Corner1.X",
        ["y1"] = "Corner1.Y",
        ["x2"] = "Corner2.X",
        ["y2"] = "Corner2.Y",
        ["layer"] = "Layer",
        ["rotation"] = "Rotation",
        ["uniqueId"] = "UniqueId",
        ["enabled"] = "Enabled",
        ["isKeepout"] = "IsKeepout",
        ["width"] = "Width (computed from Corner2.X - Corner1.X)",
    };

    /// <summary>
    /// PcbRegion: JSON key → C# property name on PcbRegion model.
    /// </summary>
    public static readonly Dictionary<string, string> Region = new(StringComparer.OrdinalIgnoreCase)
    {
        ["layer"] = "Layer",
        ["kind"] = "Kind",
        ["uniqueId"] = "UniqueId",
        ["enabled"] = "Enabled",
        ["isKeepout"] = "IsKeepout",
        ["name"] = "Name",
        ["cavityHeight"] = "CavityHeight",
        // Outline vertices/contourPoints handled separately (in skip list)
    };

    /// <summary>
    /// PcbComponentBody: JSON key → C# property name on PcbComponentBody model.
    /// </summary>
    public static readonly Dictionary<string, string> ComponentBody = new(StringComparer.OrdinalIgnoreCase)
    {
        ["layer"] = "Layer",
        ["uniqueId"] = "UniqueId",
        ["kind"] = "Kind",
        ["name"] = "Name",
        ["standoffHeight"] = "StandoffHeight",
        ["overallHeight"] = "OverallHeight",
        ["bodyColor3D"] = "BodyColor3D",
        ["bodyOpacity3D"] = "BodyOpacity3D",
        ["enabled"] = "Enabled",
        ["isKeepout"] = "IsKeepout",
        ["cavityHeight"] = "CavityHeight",
    };
}
