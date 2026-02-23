using System.Text.Json;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Base class for property coverage tests. Reads JSON test data and compares
/// against v2 reader output to identify missing properties per primitive type.
/// </summary>
public abstract class CoverageTestBase
{
    /// <summary>
    /// Properties that are runtime state / computed / UI-only and should NOT be modeled.
    /// </summary>
    protected static readonly HashSet<string> SkipProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        // Runtime state
        "selected", "used", "handle", "drcError",
        // Computed display strings
        "descriptor", "detail", "identifier", "objectIDString",
        // Computed getters (getState_*, getXxx patterns)
        "getHoleXSize", "getHoleYSize",
        "getState_BottomLayer", "getState_TopLayer",
        "getState_HoleString", "getState_SwapID_Gate",
        "getState_LocationX", "getState_LocationY",
        "getState_BarCodeMinPixelSize",
        "getState_ModelType", "getState_SnapCount",
        "getDesignatorDisplayString",
        // Container membership (set by parent, not stored per primitive)
        "inBoard", "inCoordinate", "inDimension", "inPolygon", "inAutoDimension",
        "inComponent", "inNet",
        // Computed electrical state
        "isElectricalPrim",
        // UI rendering hints
        "drawAsPreview", "enableDraw",
        // Position in file (implicit)
        "index",
        // Container membership
        "inLibrary", "inSheet",
        // Compilation state
        "compilationMasked",
        // Computed state
        "isHoleSizeValid", "isRedundant", "modelHasChanged",
        // Computed geometry
        "area",
        // Computed arc endpoints (derived from center/radius/angles)
        "xCenter", "yCenter", "startX", "startY", "endX", "endY",
        // Computed display strings
        "calculatedValueString", "displayString", "overrideDisplayString",
        "convertedString", "underlyingString", "formula",
        // Internal cache flags
        "padCacheRobotFlag",
        // Unknown internal flags
        "miscFlag1", "miscFlag2", "miscFlag3",
        // Type discriminator (handled separately)
        "objectType",
        // Computed from endpoints
        "length",
        // Child primitive array (handled by container logic)
        "primitives",
        // Vertex/contour arrays (handled structurally, not as key→value)
        "vertices", "vertexCount", "contourPoints", "holeCount",
        // PCB editor state (always default values in library context)
        "allowGlobalEdit", "moveable", "isFreePrimitive", "isPreRoute",
        "userRouted", "unionIndex", "polygonOutline", "tearDrop",
        // Redundant layer (identical to "layer")
        "layer_V6",
        // PCB test/assembly points (always false in libraries)
        "isTestpoint_Top", "isTestpoint_Bottom",
        "isAssyTestpoint_Top", "isAssyTestpoint_Bottom",
        // PCB tenting (always false in library context)
        "isTenting", "isTenting_Top", "isTenting_Bottom",
        // PCB mask/relief defaults (always zero/default on non-pad types,
        // already modeled on Pad/Via where they matter)
        "pasteMaskExpansion", "solderMaskExpansion",
        "powerPlaneClearance", "powerPlaneReliefExpansion",
        "powerPlaneConnectStyle", "reliefAirGap",
        "reliefConductorWidth", "reliefEntries",
        // PCB visibility (always false in library context)
        "isHidden",
        // Net assignment (board-level, not in libraries)
        "net",
        // Pad/Via defaults (always constant in test data)
        "pinPackageLength", "xPadOffsetAll", "yPadOffsetAll",
        "holePositiveTolerance", "holeNegativeTolerance",
        "solderMaskExpansionFromHoleEdgeWithRule",
        "swappedPadName", "jumperID", "ownerPartID",
        "isVirtualPin", "isCounterHole",
        "hasCustomChamferedRectangle", "hasCustomDonut",
        "hasCustomMaskDonutShapes", "hasCustomMaskShapes",
        "hasCustomRoundedRectangle", "hasCustomShapes",
        "multiLayerHighBits", "daisyChainStyle", "padHasOffsetOnAny",
        // Via-specific defaults (always constant in library context)
        "solderMaskExpansionFromHoleEdge", "drillLayerPairType",
        "isBackdrill",
        // ComponentBody defaults (always constant in test data)
        "bodyProjection", "overrideColor", "axisCount",
        "isSimpleRegion", "texture", "textureCenter",
        "textureRotation", "textureSize",
        // Text defaults (always constant in test data)
        "charSet", "isComment", "isDesignator",
        "multilineTextAutoPosition", "disableSpecialStringConversion",
        "advanceSnapping", "borderSpaceType", "canEditMultilineRectSize",
        "ttfInvertedTextJustify", "ttfOffsetFromInvertedRect",
        "invertedTTTextBorder",
        "barCodeFontName", "barCodeInverted", "barCodeRenderMode",
        "barCodeXMargin", "barCodeYMargin",
        // SchPin defaults (always constant in test data)
        "designator_CustomColor", "designator_CustomPosition_Margin",
        "designator_CustomPosition_RotationAnchor", "designator_CustomPosition_RotationRelative",
        "designator_FontMode", "designator_PositionMode",
        "name_CustomColor", "name_CustomPosition_Margin",
        "name_CustomPosition_RotationAnchor", "name_CustomPosition_RotationRelative",
        "name_FontMode", "name_PositionMode",
        "hiddenNetName", "swapId_Pair", "swapId_PartPin", "swapId_Pin",
        // SchComponent defaults (always constant in test data)
        "libraryIdentifier", "showHiddenFields", "pinsMoveable", "pinColor",
        "isUnmanaged", "libIdentifierKind",
        "configurationParameters", "configuratorName", "variantOption",
        "databaseLibraryName", "databaseTableName",
        "vaultGUID", "vaultHRID", "itemGUID",
        "revisionGUID", "revisionHRID", "revisionState", "revisionStatus",
        "symbolItemGUID", "symbolRevisionGUID",
        // SchPolyline defaults (always constant in test data)
        "isSolid",
        // PCB Text computed properties (derived by Altium, not stored in binary)
        "snapPointX", "snapPointY",
        "x1Location", "y1Location", "x2Location", "y2Location",
        "ttfTextHeight", "ttfTextWidth",
        "barCodeBitPattern", "barCodeFullHeight", "barCodeFullWidth", "barCodeMinWidth",
        "invRectHeight", "invRectWidth",
        // PcbFill computed (derived from Corner1/Corner2)
        "xLocation", "yLocation",
        // PcbVia computed/constant
        // PcbPad computed/derived
        "hasCornerRadiusChamfer", // derived from shape + corner radius
        "isPadStack", // derived: Mode != 0
        "isSurfaceMount", // derived: HoleSize == 0
        "maxXSignalLayers", "maxYSignalLayers", // max across signal layer sizes
        "pinDescriptor", // constructed string, not stored
        "hasRoundedRectangularShapes", // serialization flag, not design property
        "isTopPasteEnabled", "isBottomPasteEnabled", // in unknown binary bytes
    };

    protected static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }

    protected static string GetPcbTestDataPath() =>
        Path.Combine(GetTestDataPath(), "Generated", "Individual", "PCB");

    protected static string GetSchTestDataPath() =>
        Path.Combine(GetTestDataPath(), "Generated", "Individual", "SchLib");

    /// <summary>
    /// Load and parse a JSON test file. Returns null if the JSON is malformed.
    /// </summary>
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    protected static JsonDocument? LoadJson(string path)
    {
        var json = File.ReadAllText(path);
        try
        {
            return JsonDocument.Parse(json, JsonOptions);
        }
        catch (JsonException)
        {
            // Fix common generator bugs: missing commas between sibling properties.
            // Patterns like `]\n          "key"` or `}\n          "key"` need a comma.
            json = System.Text.RegularExpressions.Regex.Replace(
                json, @"(\]|\})(\s*\n\s*"")", "$1,$2");
            try
            {
                return JsonDocument.Parse(json, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Extract all design-time property keys from a JSON primitive element.
    /// Filters out runtime/skip properties.
    /// </summary>
    protected static HashSet<string> GetDesignTimePropertyKeys(JsonElement primitive)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in primitive.EnumerateObject())
        {
            if (!SkipProperties.Contains(prop.Name))
                keys.Add(prop.Name);
        }
        return keys;
    }

    /// <summary>
    /// Get all primitives of a specific type from a PCB JSON file.
    /// </summary>
    protected static List<JsonElement> GetPcbPrimitives(JsonDocument doc, string objectType)
    {
        var result = new List<JsonElement>();
        foreach (var footprint in doc.RootElement.GetProperty("footprints").EnumerateArray())
        {
            foreach (var prim in footprint.GetProperty("primitives").EnumerateArray())
            {
                if (prim.GetProperty("objectType").GetString() == objectType)
                    result.Add(prim);
            }
        }
        return result;
    }

    /// <summary>
    /// Get all primitives of a specific type from a SchLib JSON file.
    /// </summary>
    protected static List<JsonElement> GetSchPrimitives(JsonDocument doc, string objectType)
    {
        var result = new List<JsonElement>();
        foreach (var symbol in doc.RootElement.GetProperty("symbols").EnumerateArray())
        {
            if (symbol.TryGetProperty("primitives", out var primitives))
            {
                foreach (var prim in primitives.EnumerateArray())
                {
                    if (prim.GetProperty("objectType").GetString() == objectType)
                        result.Add(prim);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Get all component (symbol) elements from a SchLib JSON file.
    /// </summary>
    protected static List<JsonElement> GetSchComponents(JsonDocument doc)
    {
        var result = new List<JsonElement>();
        foreach (var symbol in doc.RootElement.GetProperty("symbols").EnumerateArray())
        {
            if (symbol.GetProperty("objectType").GetString() == "Component")
                result.Add(symbol);
        }
        return result;
    }

    /// <summary>
    /// Collect the union of all design-time property keys across all primitives of a type
    /// in all JSON files matching a glob pattern.
    /// Returns (allKeys, fileCount, primitiveCount).
    /// </summary>
    protected static (HashSet<string> allKeys, int fileCount, int primitiveCount) CollectPropertyKeys(
        string directory, string filePattern, string objectType, bool isPcb)
    {
        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fileCount = 0;
        var primitiveCount = 0;

        if (!Directory.Exists(directory))
            return (allKeys, 0, 0);

        foreach (var jsonFile in Directory.GetFiles(directory, filePattern))
        {
            using var doc = LoadJson(jsonFile);
            if (doc == null) continue; // Skip malformed JSON files

            var primitives = isPcb
                ? GetPcbPrimitives(doc, objectType)
                : GetSchPrimitives(doc, objectType);

            if (primitives.Count > 0) fileCount++;
            primitiveCount += primitives.Count;

            foreach (var prim in primitives)
            {
                var keys = GetDesignTimePropertyKeys(prim);
                allKeys.UnionWith(keys);
            }
        }

        return (allKeys, fileCount, primitiveCount);
    }
}

/// <summary>
/// Result of a coverage check for a single primitive type.
/// </summary>
public sealed class CoverageResult
{
    public string TypeName { get; init; } = "";
    public int TotalJsonProperties { get; init; }
    public int ModeledProperties { get; init; }
    public int MissingProperties => TotalJsonProperties - ModeledProperties;
    public double CoveragePercent => TotalJsonProperties == 0 ? 100 :
        Math.Round(100.0 * ModeledProperties / TotalJsonProperties, 1);
    public HashSet<string> AllJsonKeys { get; init; } = [];
    public HashSet<string> MappedKeys { get; init; } = [];
    public HashSet<string> UnmappedKeys { get; init; } = [];
    public int FileCount { get; init; }
    public int PrimitiveCount { get; init; }

    public override string ToString() =>
        $"{TypeName}: {ModeledProperties}/{TotalJsonProperties} ({CoveragePercent}%) " +
        $"[{FileCount} files, {PrimitiveCount} primitives] " +
        $"Missing: {string.Join(", ", UnmappedKeys.Order())}";
}
