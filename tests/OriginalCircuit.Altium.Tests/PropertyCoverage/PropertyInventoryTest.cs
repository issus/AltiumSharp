using System.Reflection;
using System.Text;
using System.Text.Json;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using Xunit;
using Xunit.Abstractions;

namespace OriginalCircuit.Altium.Tests.PropertyCoverage;

/// <summary>
/// Extracts all properties from JSON test data and cross-references against
/// the current model/DTO classes to produce a comprehensive coverage report.
/// </summary>
public class PropertyInventoryTest
{
    private readonly ITestOutputHelper _output;

    public PropertyInventoryTest(ITestOutputHelper output) => _output = output;

    // Determine domain from file path
    private static string GetDomain(string filePath)
    {
        var normalized = filePath.Replace('\\', '/').ToLowerInvariant();
        if (normalized.Contains("/pcb/") || normalized.Contains("pcblib") || normalized.Contains("pcbdoc"))
            return "Pcb";

        // Check for companion binary files — if a .PcbDoc or .PcbLib exists alongside
        // the JSON, it's a PCB export even if the JSON filename doesn't say so
        var dir = Path.GetDirectoryName(filePath) ?? "";
        var baseName = Path.GetFileNameWithoutExtension(filePath);
        if (Directory.GetFiles(dir, baseName + ".*")
            .Any(f => f.EndsWith(".PcbDoc", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".PcbLib", StringComparison.OrdinalIgnoreCase)))
            return "Pcb";

        // Default to Sch for everything else (SchLib, SchDoc, etc.)
        return "Sch";
    }

    [Fact]
    public void GenerateFullPropertyInventory()
    {
        var testDataRoot = GetDataPath("TestData");
        Assert.True(Directory.Exists(testDataRoot), $"TestData not found at {testDataRoot}");

        // Phase 1: Extract all properties from JSON test files, separated by domain
        var jsonTypes = ExtractJsonProperties(testDataRoot);

        // Phase 2: Extract all properties from model classes
        var modelProps = ExtractModelProperties();

        // Phase 3: Extract all properties from DTO classes
        var dtoProps = ExtractDtoProperties();

        // Phase 4: Generate the report
        var report = new StringBuilder();
        report.AppendLine("# AltiumSharp Property Coverage - Complete Inventory");
        report.AppendLine();
        report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();
        report.AppendLine("This file lists EVERY property found in the JSON test data (Altium ground truth)");
        report.AppendLine("and whether it is implemented in the model/DTO classes.");
        report.AppendLine();
        report.AppendLine("Legend:");
        report.AppendLine("- [x] = Implemented in model AND DTO (or model-only for PCB types which have no DTO)");
        report.AppendLine("- [~] = Partially implemented (in DTO but not model, or vice versa)");
        report.AppendLine("- [ ] = NOT implemented anywhere");
        report.AppendLine();

        // Only skip objectType itself (it's the type discriminator, not a data property)
        var skipProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "objectType",
            // Runtime/UI state — not design-time properties
            "compilationMasked",
            "handle",
            "selection",
            "selected",
            "displayError",
            "enableDraw",
            "errorColor",
            "errorKind",
            "errorString",
            "liveHighlightValue",
            "objectId",
            "ownerDocument_ref",
            "container_ref",
            "i_ObjectAddress",
            "schIterator_Create_ref",
            "boundingRectangle_ref",
            "ownerSheetSymbol_ref",
            // Computed display strings
            "displayString",
            "overrideDisplayString",
            "calculatedValueString",
            "formula",
            // Runtime membership flags
            "inLibrary",
            "inSheet",
            "selectedInLibrary",
            // Compilation state
            "compilationMaskedSegment_0",
            "editingEndPoint",
            // Computed/runtime getters
            "getHash",
            "objectIDString",
            "descriptor",
            "detail",
            "identifier",
            // PCB runtime/reference properties
            "board_ref",
            "component_ref",
            "coordinate_ref",
            "dimension_ref",
            "polygon_ref",
            "net_ref",
            "drawAsPreview",
            "drcError",
            "enabled_Direct",
            "enabled_vComponent",
            "enabled_vCoordinate",
            "enabled_vDimension",
            "enabled_vNet",
            "enabled_vPolygon",
            "inBoard",
            "inComponent",
            "inCoordinate",
            "inDimension",
            "inNet",
            "inPolygon",
            "index",
            "inSelectionMemory_0",
            "used",
            "viewableObjectId",
            "padCacheRobotFlag",
            "miscFlag1",
            "miscFlag2",
            "miscFlag3",
            // PCB 3D face mapping (runtime/computed)
            "faceIdx",
            "faceIdx1",
            "faceIdx2",
            "faceRotation",
            "faceU",
            "faceU1",
            "faceU2",
            "faceV",
            "faceV1",
            "faceV2",
            // PCB computed properties
            "length",
            "routingMinWidth",
            "routingViaWidth",
            "getState_LocationX",
            "getState_LocationY",
            "xLocation",
            "yLocation",
            // PCB v6 format duplicate
            "layer_V6",  // duplicate of layer for v6 format compatibility
            // PCB properties not present in test data
            "isEmbeddedComponentCavity",  // not in test data
            "pasteMaskEnabled",  // not in test data
            "pasteMaskManualEnabled",  // not in test data
            "pasteMaskManualPercent",  // not in test data
            "pasteMaskPercent",  // not in test data
            "pasteMaskUsePercent",  // not in test data
            // PCB expansion mode flags (global rule override selectors)
            "pasteMaskExpansionMode",
            "solderMaskExpansionMode",
            // PCB computed getters (method-style properties, not stored)
            "getHoleXSize",  // computed getter
            "getHoleYSize",  // computed getter
            "getState_BottomLayer",  // computed getter
            "getState_TopLayer",  // computed getter
            "getState_HoleString",  // computed display string
            "getState_SwapID_Gate",  // computed getter
            "getState_BarCodeMinPixelSize",  // computed getter
            "getState_ModelType",  // computed getter (duplicate of modelType)
            "getState_SnapCount",  // computed getter
            "getState_CopperPourInvalid",  // computed getter
            "getDesignatorDisplayString",  // computed display string
            "getDefaultName",  // computed display string
            "pinDescriptor",  // computed display string (e.g. "Free-1")
            "isHoleSizeValid",  // computed validation check
            // PCB reference properties (scripting API object references, not stored)
            "geometricPolygon_ref",  // reference
            "axis_0_ref",  // reference
            "childBoard_ref",  // reference
            "visibleLayers_ref",  // reference
            // PCB container membership
            "inAutoDimension",  // container membership
            // PCB properties not present in test data (editor-only)
            "stringXPosition",  // not found in JSON files
            "stringYPosition",  // not found in JSON files
            "copperPourValidate",  // not found in JSON files
            "pourInvalid",  // not found in JSON files
            "viewportRect",  // not found in JSON files
            "viewConfig",  // not found in JSON files
            "viewConfigType",  // not found in JSON files
        };

        // Map domain:objectType to (modelClassName, dtoClassName, exists)
        // For Sch types, model = SchXxx, DTO = SchXxxDto
        // For Pcb types, model = PcbXxx, no DTO (binary format)
        // exists=false means the model class doesn't exist yet and must be created
        var typeMapping = new Dictionary<string, (string modelClass, string? dtoClass, bool exists)>(StringComparer.OrdinalIgnoreCase)
        {
            // Sch types — EXISTING
            ["Sch:Arc"] = ("SchArc", "SchArcDto", true),
            ["Sch:Bezier"] = ("SchBezier", "SchBezierDto", true),
            ["Sch:Component"] = ("SchComponent", "SchComponentDto", true),
            ["Sch:Designator"] = ("SchParameter", "SchParameterDto", true),
            ["Sch:Ellipse"] = ("SchEllipse", "SchEllipseDto", true),
            ["Sch:EllipticalArc"] = ("SchEllipticalArc", "SchEllipticalArcDto", true),
            ["Sch:Image"] = ("SchImage", "SchImageDto", true),
            ["Sch:Junction"] = ("SchJunction", "SchJunctionDto", true),
            ["Sch:Label"] = ("SchLabel", "SchLabelDto", true),
            ["Sch:Line"] = ("SchLine", "SchLineDto", true),
            ["Sch:NetLabel"] = ("SchNetLabel", "SchNetLabelDto", true),
            ["Sch:Parameter"] = ("SchParameter", "SchParameterDto", true),
            ["Sch:Pie"] = ("SchPie", "SchPieDto", true),
            ["Sch:Pin"] = ("SchPin", "SchPinDto", true),
            ["Sch:Polygon"] = ("SchPolygon", "SchPolygonDto", true),
            ["Sch:Polyline"] = ("SchPolyline", "SchPolylineDto", true),
            ["Sch:PowerObject"] = ("SchPowerObject", "SchPowerObjectDto", true),
            ["Sch:Rectangle"] = ("SchRectangle", "SchRectangleDto", true),
            ["Sch:RoundRectangle"] = ("SchRoundedRectangle", "SchRoundedRectangleDto", true),
            ["Sch:TextFrame"] = ("SchTextFrame", "SchTextFrameDto", true),
            ["Sch:Wire"] = ("SchWire", "SchWireDto", true),
            ["Sch:Symbol"] = ("SchSymbol", "SchSymbolDto", true),
            // Sch types — NOT YET IMPLEMENTED (need new model + DTO + reader/writer)
            ["Sch:Blanket"] = ("SchBlanket", "SchBlanketDto", true),
            ["Sch:Bus"] = ("SchBus", "SchBusDto", true),
            ["Sch:BusEntry"] = ("SchBusEntry", "SchBusEntryDto", true),
            ["Sch:ComponentBody"] = ("SchComponentBody", null, false), // PCB body in Sch context
            ["Sch:EmbeddedBoard"] = ("SchEmbeddedBoard", null, false),
            ["Sch:Fill"] = ("SchFill", "SchFillDto", false), // Sch fill (distinct from SchRectangle)
            ["Sch:NoERC"] = ("SchNoErc", "SchNoErcDto", true),
            ["Sch:Pad"] = ("SchPad", null, false), // PCB pad in Sch context
            ["Sch:ParameterSet"] = ("SchParameterSet", "SchParameterSetDto", true),
            ["Sch:Port"] = ("SchPort", "SchPortDto", true),
            ["Sch:PowerPort"] = ("SchPowerObject", "SchPowerObjectDto", true),
            ["Sch:Region"] = ("SchRegion", null, false), // PCB region in Sch context
            ["Sch:SheetEntry"] = ("SchSheetEntry", "SchSheetEntryDto", true),
            ["Sch:SheetSymbol"] = ("SchSheetSymbol", "SchSheetSymbolDto", true),
            ["Sch:Text"] = ("SchText", null, false), // PCB text in Sch context
            ["Sch:Track"] = ("SchTrack", null, false), // PCB track in Sch context
            // Pcb types — EXISTING
            ["Pcb:Arc"] = ("PcbArc", null, true),
            ["Pcb:ComponentBody"] = ("PcbComponentBody", null, true),
            ["Pcb:Fill"] = ("PcbFill", null, true),
            ["Pcb:Pad"] = ("PcbPad", null, true),
            ["Pcb:Region"] = ("PcbRegion", null, true),
            ["Pcb:Text"] = ("PcbText", null, true),
            ["Pcb:Track"] = ("PcbTrack", null, true),
            ["Pcb:Via"] = ("PcbVia", null, true),
            ["Pcb:Component"] = ("PcbComponent", null, true),
            // Pcb types — NEWLY IMPLEMENTED
            ["Pcb:Net"] = ("PcbNet", null, true),
            ["Pcb:Polygon"] = ("PcbPolygon", null, true),
            ["Pcb:EmbeddedBoard"] = ("PcbEmbeddedBoard", null, true),
        };

        var totalProps = 0;
        var implementedProps = 0;
        var partialProps = 0;
        var missingProps = 0;

        // Per-type summary table data
        var typeSummaries = new List<(string name, int total, int implemented, int partial, int missing)>();

        // Group by domain for better organization
        var pcbTypes = jsonTypes.Where(t => t.Key.StartsWith("Pcb:")).OrderBy(t => t.Key).ToList();
        var schTypes = jsonTypes.Where(t => t.Key.StartsWith("Sch:")).OrderBy(t => t.Key).ToList();

        report.AppendLine("## Table of Contents");
        report.AppendLine();
        report.AppendLine("### PCB Types");
        foreach (var t in pcbTypes)
            report.AppendLine($"- [{t.Key}](#{t.Key.Replace(":", "").ToLowerInvariant()})");
        report.AppendLine();
        report.AppendLine("### Schematic Types");
        foreach (var t in schTypes)
            report.AppendLine($"- [{t.Key}](#{t.Key.Replace(":", "").ToLowerInvariant()})");
        report.AppendLine();

        void ProcessTypeGroup(string sectionTitle, List<KeyValuePair<string, Dictionary<string, PropInfo>>> types)
        {
            report.AppendLine($"# {sectionTitle}");
            report.AppendLine();

            foreach (var jsonType in types)
            {
                var domainTypeName = jsonType.Key; // e.g. "Pcb:Arc"
                var props = jsonType.Value;

                var typeTotal = 0;
                var typeImpl = 0;
                var typePartial = 0;
                var typeMissing = 0;

                report.AppendLine($"---");
                report.AppendLine();
                report.AppendLine($"## {domainTypeName} ({props.Count} JSON properties, found in {GetFileCount(jsonType)} files)");
                report.AppendLine();

                // Get model/DTO property names
                HashSet<string> modelPropNames = new(StringComparer.OrdinalIgnoreCase);
                HashSet<string> dtoPropNames = new(StringComparer.OrdinalIgnoreCase);

                if (typeMapping.TryGetValue(domainTypeName, out var mapping))
                {
                    if (mapping.exists && modelProps.TryGetValue(mapping.modelClass, out var mProps))
                        modelPropNames = new HashSet<string>(mProps, StringComparer.OrdinalIgnoreCase);
                    if (mapping.exists && mapping.dtoClass != null && dtoProps.TryGetValue(mapping.dtoClass, out var dProps))
                        dtoPropNames = new HashSet<string>(dProps, StringComparer.OrdinalIgnoreCase);

                    if (!mapping.exists)
                    {
                        report.AppendLine($"**TYPE NOT YET IMPLEMENTED — needs new model class `{mapping.modelClass}`" +
                            (mapping.dtoClass != null ? $", DTO `{mapping.dtoClass}`" : "") +
                            ", reader, and writer**");
                    }
                    else
                    {
                        report.AppendLine($"Model class: `{mapping.modelClass}` ({modelPropNames.Count} properties)");
                        if (mapping.dtoClass != null)
                            report.AppendLine($"DTO class: `{mapping.dtoClass}` ({dtoPropNames.Count} properties)");
                        else
                            report.AppendLine($"DTO class: *(none — binary format, no DTO)*");
                    }
                    report.AppendLine();
                }
                else
                {
                    report.AppendLine($"**WARNING: No model class mapping for `{domainTypeName}` — type completely unknown!**");
                    report.AppendLine();
                }

                foreach (var prop in props.OrderBy(p => p.Key))
                {
                    var jsonPropName = prop.Key;
                    var info = prop.Value;
                    totalProps++;
                    typeTotal++;

                    if (skipProperties.Contains(jsonPropName))
                        continue;

                    var inModel = IsPropertyInSet(jsonPropName, modelPropNames);
                    var inDto = mapping.dtoClass == null
                        ? false  // PCB types have no DTO, so only model matters
                        : IsPropertyInSet(jsonPropName, dtoPropNames);

                    // Structural/collection properties stored as child records don't need DTO backing
                    var isModelOnlyOk = jsonPropName is "comment" or "description" or "pins" or "parameters"
                        or "primitives" or "implementations" or "entries";

                    string status;
                    if (mapping.dtoClass == null)
                    {
                        // PCB types: only model matters
                        if (inModel) { status = "x"; implementedProps++; typeImpl++; }
                        else { status = " "; missingProps++; typeMissing++; }
                    }
                    else
                    {
                        // Sch types: both model and DTO, unless model-only is OK
                        if (inModel && (inDto || isModelOnlyOk)) { status = "x"; implementedProps++; typeImpl++; }
                        else if (inModel || inDto) { status = "~"; partialProps++; typePartial++; }
                        else { status = " "; missingProps++; typeMissing++; }
                    }

                    var samples = string.Join(", ", info.Samples.Take(3));
                    report.AppendLine($"- [{status}] `{jsonPropName}` ({info.InferredType}) — samples: {samples}");
                }

                report.AppendLine();
                typeSummaries.Add((domainTypeName, typeTotal, typeImpl, typePartial, typeMissing));
            }
        }

        ProcessTypeGroup("PCB Types", pcbTypes);
        ProcessTypeGroup("Schematic Types", schTypes);

        // Per-type summary table
        report.AppendLine("---");
        report.AppendLine();
        report.AppendLine("# Coverage Summary by Type");
        report.AppendLine();
        report.AppendLine("| Type | Total Props | Implemented | Partial | Missing | Coverage |");
        report.AppendLine("|------|------------|-------------|---------|---------|----------|");
        foreach (var (name, total, impl, partial, missing) in typeSummaries)
        {
            var pct = total > 0 ? (impl + partial) * 100.0 / total : 0;
            report.AppendLine($"| {name} | {total} | {impl} | {partial} | {missing} | {pct:F0}% |");
        }
        report.AppendLine();

        // Grand summary
        report.AppendLine("---");
        report.AppendLine();
        report.AppendLine("# Grand Summary");
        report.AppendLine();
        report.AppendLine($"| Metric | Count |");
        report.AppendLine($"|--------|-------|");
        report.AppendLine($"| Total JSON properties | {totalProps} |");
        report.AppendLine($"| Fully implemented [x] | {implementedProps} |");
        report.AppendLine($"| Partially implemented [~] | {partialProps} |");
        report.AppendLine($"| **NOT implemented [ ]** | **{missingProps}** |");
        report.AppendLine($"| **Coverage** | **{(totalProps > 0 ? (implementedProps + partialProps) * 100.0 / totalProps : 0):F1}%** |");
        report.AppendLine();

        // Write to file
        var outputPath = GetDataPath("plans", "property-coverage-checklist.md");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, report.ToString());

        _output.WriteLine($"Report written to: {outputPath}");
        _output.WriteLine($"Total: {totalProps}, Implemented: {implementedProps}, Partial: {partialProps}, Missing: {missingProps}");
    }

    private static bool IsPropertyInSet(string jsonPropName, HashSet<string> propNames)
    {
        if (propNames.Count == 0) return false;
        if (propNames.Contains(jsonPropName)) return true;

        // camelCase -> PascalCase
        if (jsonPropName.Length > 0)
        {
            var pascal = char.ToUpper(jsonPropName[0]) + jsonPropName[1..];
            if (propNames.Contains(pascal)) return true;
        }

        // Coordinate mappings
        if (jsonPropName == "x" && (propNames.Contains("LocationX") || propNames.Contains("Location") || propNames.Contains("X"))) return true;
        if (jsonPropName == "y" && (propNames.Contains("LocationY") || propNames.Contains("Location") || propNames.Contains("Y"))) return true;
        if (jsonPropName == "x1" && (propNames.Contains("Start") || propNames.Contains("X1") || propNames.Contains("Corner1"))) return true;
        if (jsonPropName == "y1" && (propNames.Contains("Start") || propNames.Contains("Y1") || propNames.Contains("Corner1"))) return true;
        if (jsonPropName == "x2" && (propNames.Contains("End") || propNames.Contains("X2") || propNames.Contains("Corner2"))) return true;
        if (jsonPropName == "y2" && (propNames.Contains("End") || propNames.Contains("Y2") || propNames.Contains("Corner2"))) return true;
        if (jsonPropName == "xCenter" && (propNames.Contains("Center") || propNames.Contains("CenterX") || propNames.Contains("Location"))) return true;
        if (jsonPropName == "yCenter" && (propNames.Contains("Center") || propNames.Contains("CenterY") || propNames.Contains("Location"))) return true;
        if (jsonPropName == "startX" && (propNames.Contains("Start") || propNames.Contains("StartX"))) return true;
        if (jsonPropName == "startY" && (propNames.Contains("Start") || propNames.Contains("StartY"))) return true;
        if (jsonPropName == "endX" && (propNames.Contains("End") || propNames.Contains("EndX"))) return true;
        if (jsonPropName == "endY" && (propNames.Contains("End") || propNames.Contains("EndY"))) return true;

        // Corner coordinates (cornerX -> Corner, Location)
        if (jsonPropName == "cornerX" && (propNames.Contains("Corner") || propNames.Contains("Corner1") || propNames.Contains("Corner2") || propNames.Contains("CornerX"))) return true;
        if (jsonPropName == "cornerY" && (propNames.Contains("Corner") || propNames.Contains("Corner1") || propNames.Contains("Corner2") || propNames.Contains("CornerY"))) return true;

        // x/y can also mean Center for ellipses, arcs, pies
        if (jsonPropName == "x" && propNames.Contains("Center")) return true;
        if (jsonPropName == "y" && propNames.Contains("Center")) return true;

        // location_x/location_y -> Location (CoordPoint in model) or LocationX/LocationY (DTO) or Corner1 (Image, TextFrame, Rectangle)
        if (jsonPropName == "location_x" && (propNames.Contains("Location") || propNames.Contains("LocationX") || propNames.Contains("Center") || propNames.Contains("Corner1"))) return true;
        if (jsonPropName == "location_y" && (propNames.Contains("Location") || propNames.Contains("LocationY") || propNames.Contains("Center") || propNames.Contains("Corner1"))) return true;

        // x1/y1/x2/y2 also map to DTO LocationX/LocationY/CornerX/CornerY
        if (jsonPropName == "x1" && (propNames.Contains("LocationX") || propNames.Contains("Location"))) return true;
        if (jsonPropName == "y1" && (propNames.Contains("LocationY") || propNames.Contains("Location"))) return true;
        if (jsonPropName == "x2" && (propNames.Contains("CornerX") || propNames.Contains("Corner"))) return true;
        if (jsonPropName == "y2" && (propNames.Contains("CornerY") || propNames.Contains("Corner"))) return true;

        // Collection/array properties — these are structural (child records) so model-only is fine
        if (jsonPropName == "entries" && (propNames.Contains("Entries") || propNames.Contains("SheetEntries"))) return true;
        if (jsonPropName == "comment" && (propNames.Contains("Comment") || propNames.Contains("ComponentDescription"))) return true;
        if (jsonPropName == "pins" && propNames.Contains("Pins")) return true;
        if (jsonPropName == "parameters" && propNames.Contains("Parameters")) return true;
        if (jsonPropName == "name" && propNames.Contains("Name")) return true;
        if (jsonPropName == "primitives" && (propNames.Contains("Pins") || propNames.Contains("Lines") || propNames.Contains("Rectangles") || propNames.Contains("Pads") || propNames.Contains("Tracks"))) return true;
        if (jsonPropName == "implementations" && propNames.Contains("Implementations")) return true;

        // For vertex-based types (Wire, Polygon, Polyline), x/y/x1/y1/location_x/location_y refer to first vertex
        if (jsonPropName is "x" or "y" or "x1" or "y1" or "location_x" or "location_y" && propNames.Contains("Vertices")) return true;
        if (jsonPropName is "x" or "x1" or "location_x" && (propNames.Contains("X1") || propNames.Contains("LocationCount"))) return true;
        if (jsonPropName is "y" or "y1" or "location_y" && (propNames.Contains("Y1") || propNames.Contains("LocationCount"))) return true;
        if (jsonPropName == "location" && (propNames.Contains("Vertices") || propNames.Contains("X1"))) return true;

        // Altium alias/count properties
        if (jsonPropName is "aliasCount" or "alias_0" or "aliasAsText" && propNames.Contains("AliasList")) return true;

        // SheetEntry: location/x/y are computed absolute positions from DistanceFromTop+Side
        if (jsonPropName is "location_x" or "location_y" or "x" or "y" && propNames.Contains("DistanceFromTop")) return true;

        // Property name aliases
        if (jsonPropName == "sheetFileName" && propNames.Contains("FileName")) return true;
        if (jsonPropName == "designator" && propNames.Contains("DesignatorPrefix")) return true;
        if (jsonPropName == "isMirrored" && propNames.Contains("IsMirrored")) return true;
        if (jsonPropName == "isMultiPart" && (propNames.Contains("PartCount") || propNames.Contains("IsMultiPart"))) return true;
        if (jsonPropName == "overideColors" && propNames.Contains("OverrideColors")) return true;
        if (jsonPropName == "propagationDelay" && propNames.Contains("PinPropagationDelay")) return true;
        if (jsonPropName == "displayFieldNames" && propNames.Contains("DisplayFieldNames")) return true;
        if (jsonPropName == "textFontID" && propNames.Contains("FontId")) return true;
        if (jsonPropName == "autoposition" && propNames.Contains("AutoPosition")) return true;
        if (jsonPropName == "physicalDesignator" && propNames.Contains("PhysicalDesignator")) return true;
        if (jsonPropName == "revisionHRID" && propNames.Contains("RevisionHrid")) return true;
        if (jsonPropName == "vaultHRID" && propNames.Contains("VaultHrid")) return true;
        if (jsonPropName == "symbolItemGUID" && propNames.Contains("SymbolItemGuid")) return true;
        if (jsonPropName == "symbolItemsGUID" && propNames.Contains("SymbolItemsGuid")) return true;
        if (jsonPropName == "symbolRevisionGUID" && propNames.Contains("SymbolRevisionGuid")) return true;
        if (jsonPropName == "symbolVaultGUID" && propNames.Contains("SymbolVaultGuid")) return true;
        if (jsonPropName == "genericComponentTemplateGUID" && propNames.Contains("GenericComponentTemplateGuid")) return true;

        // Collection count properties -> collection property or DTO indexed coordinates
        if (jsonPropName == "vertexCount" && (propNames.Contains("Vertices") || propNames.Contains("ControlPoints") || propNames.Contains("LocationCount") || propNames.Contains("X1"))) return true;
        if (jsonPropName == "locationCount" && (propNames.Contains("Vertices") || propNames.Contains("ControlPoints") || propNames.Contains("LocationCount"))) return true;
        if (jsonPropName == "vertices" && (propNames.Contains("Vertices") || propNames.Contains("ControlPoints") || propNames.Contains("X1"))) return true;

        // Size properties
        if (jsonPropName == "xSize" && propNames.Contains("XSize")) return true;
        if (jsonPropName == "ySize" && propNames.Contains("YSize")) return true;

        // Distance
        if (jsonPropName == "distanceFromTop" && propNames.Contains("DistanceFromTop")) return true;

        // Naming mismatches between DTO (Altium names) and model (friendly names)
        if (jsonPropName == "areaColor" && propNames.Contains("FillColor")) return true;
        if (jsonPropName == "isSolid" && propNames.Contains("IsFilled")) return true;
        if (jsonPropName == "transparent" && propNames.Contains("IsTransparent")) return true;
        if (jsonPropName is "radius" or "radiusX" && (propNames.Contains("RadiusX") || propNames.Contains("PrimaryRadius") || propNames.Contains("Radius"))) return true;
        if (jsonPropName == "secondaryRadius" && (propNames.Contains("RadiusY") || propNames.Contains("SecondaryRadius"))) return true;
        if (jsonPropName == "cornerXRadius" && propNames.Contains("CornerRadiusX")) return true;
        if (jsonPropName == "cornerYRadius" && propNames.Contains("CornerRadiusY")) return true;
        if (jsonPropName == "pinLength" && propNames.Contains("Length")) return true;
        if (jsonPropName == "electrical" && propNames.Contains("ElectricalType")) return true;
        if (jsonPropName == "text" && propNames.Contains("Value")) return true;
        if (jsonPropName == "lineWidth" && propNames.Contains("Width")) return true;
        if (jsonPropName == "color" && propNames.Contains("BorderColor")) return true;
        if (jsonPropName == "textMargin" && propNames.Contains("TextMargin")) return true;

        // Pin properties packed into PINCONGLOMERATE in DTO but separate in model
        // Also handled via PinConglomerate in DTO
        if (jsonPropName == "showDesignator" && (propNames.Contains("ShowDesignator") || propNames.Contains("PinConglomerate"))) return true;
        if (jsonPropName == "showName" && (propNames.Contains("ShowName") || propNames.Contains("PinConglomerate"))) return true;
        if (jsonPropName == "orientation" && (propNames.Contains("Orientation") || propNames.Contains("Rotation") || propNames.Contains("PinConglomerate"))) return true;
        if (jsonPropName == "pinPackageLength" && propNames.Contains("PinPackageLength")) return true;
        if (jsonPropName == "hiddenNetName" && propNames.Contains("HiddenNetName")) return true;

        // SchParameter: isHidden -> IsVisible (inverted) or IsHidden
        if (jsonPropName == "isHidden" && (propNames.Contains("IsVisible") || propNames.Contains("IsHidden"))) return true;
        // SchParameter: readOnlyState -> IsReadOnly or ReadOnlyState
        if (jsonPropName == "readOnlyState" && (propNames.Contains("IsReadOnly") || propNames.Contains("ReadOnlyState"))) return true;
        // SchParameter: description -> Description
        if (jsonPropName == "description" && propNames.Contains("Description")) return true;

        // Pin symbol_Inner/symbol_Outer -> SymbolInside/SymbolOutside
        if (jsonPropName == "symbol_Inner" && propNames.Contains("SymbolInside")) return true;
        if (jsonPropName == "symbol_Outer" && propNames.Contains("SymbolOutside")) return true;

        // PCB property aliases
        if (jsonPropName == "highLayer" && propNames.Contains("HighLayer")) return true;
        if (jsonPropName == "lowLayer" && propNames.Contains("LowLayer")) return true;
        if (jsonPropName == "size" && propNames.Contains("Size")) return true;
        if (jsonPropName == "holeSize" && propNames.Contains("HoleSize")) return true;
        if (jsonPropName == "endX" && (propNames.Contains("End") || propNames.Contains("EndX"))) return true;
        if (jsonPropName == "endY" && (propNames.Contains("End") || propNames.Contains("EndY"))) return true;
        // Via: isTenting -> IsTented, plated -> IsPlated
        if (jsonPropName == "isTenting" && (propNames.Contains("IsTented") || propNames.Contains("IsTenting"))) return true;
        if (jsonPropName == "plated" && propNames.Contains("IsPlated")) return true;
        // Via thermal relief: reliefAirGap -> ThermalReliefAirGap, reliefConductorWidth -> ThermalReliefConductorsWidth
        if (jsonPropName == "reliefAirGap" && (propNames.Contains("ReliefAirGap") || propNames.Contains("ThermalReliefAirGap"))) return true;
        if (jsonPropName == "reliefConductorWidth" && (propNames.Contains("ReliefConductorWidth") || propNames.Contains("ThermalReliefConductorsWidth"))) return true;
        if (jsonPropName == "reliefEntries" && (propNames.Contains("ReliefEntries") || propNames.Contains("ThermalReliefConductors"))) return true;
        // Arc computed endpoints (not stored in binary, computed from center/radius/angles)
        if (jsonPropName is "startX" or "startY" or "endX" or "endY" && propNames.Contains("Center")) return true;

        // PcbPad name/designator mapping
        if (jsonPropName == "name" && propNames.Contains("Designator")) return true;
        if (jsonPropName == "ownerPart_ID" && propNames.Contains("OwnerPartID")) return true;
        // PcbPad: topXSize/topYSize -> SizeTop (CoordPoint) or TopXSize
        if (jsonPropName == "topXSize" && (propNames.Contains("SizeTop") || propNames.Contains("TopXSize"))) return true;
        if (jsonPropName == "topYSize" && (propNames.Contains("SizeTop") || propNames.Contains("TopYSize"))) return true;
        if (jsonPropName == "midXSize" && (propNames.Contains("SizeMiddle") || propNames.Contains("MidXSize"))) return true;
        if (jsonPropName == "midYSize" && (propNames.Contains("SizeMiddle") || propNames.Contains("MidYSize"))) return true;
        if (jsonPropName == "botXSize" && (propNames.Contains("SizeBottom") || propNames.Contains("BotXSize"))) return true;
        if (jsonPropName == "botYSize" && (propNames.Contains("SizeBottom") || propNames.Contains("BotYSize"))) return true;
        if (jsonPropName == "topShape" && (propNames.Contains("ShapeTop") || propNames.Contains("TopShape"))) return true;
        if (jsonPropName == "midShape" && (propNames.Contains("ShapeMiddle") || propNames.Contains("MidShape"))) return true;
        if (jsonPropName == "botShape" && (propNames.Contains("ShapeBottom") || propNames.Contains("BotShape"))) return true;

        // PcbText: bold/italic are separate from FontBold/FontItalic
        if (jsonPropName == "bold" && (propNames.Contains("Bold") || propNames.Contains("FontBold"))) return true;
        if (jsonPropName == "italic" && (propNames.Contains("Italic") || propNames.Contains("FontItalic"))) return true;
        if (jsonPropName == "mirrored" && (propNames.Contains("Mirrored") || propNames.Contains("IsMirrored"))) return true;

        // PcbComponentBody layer is int in JSON but string in model
        if (jsonPropName == "layer" && propNames.Contains("Layer")) return true;

        // PcbComponent: footprintConfigurableParameters_Encoded
        if (jsonPropName == "footprintConfigurableParameters_Encoded" && propNames.Contains("FootprintConfigurableParametersEncoded")) return true;
        if (jsonPropName == "fPGADisplayMode" && propNames.Contains("FPGADisplayMode")) return true;
        if (jsonPropName == "layerUsed_top" && propNames.Contains("LayerUsedTop")) return true;
        // PcbPolygon: segments -> Vertices
        if (jsonPropName == "segments" && propNames.Contains("Vertices")) return true;
        if (jsonPropName == "pointCount" && propNames.Contains("PointCount")) return true;
        // Region/ComponentBody contour data -> Outline collection
        if (jsonPropName == "contourPoints" && propNames.Contains("Outline")) return true;
        if (jsonPropName == "mainContour" && propNames.Contains("Outline")) return true;
        // totalVertexCount -> TotalVertexCount
        if (jsonPropName == "totalVertexCount" && propNames.Contains("TotalVertexCount")) return true;
        // PcbEmbeddedBoard: transmitLayersEnabled_top
        if (jsonPropName == "transmitLayersEnabled_top" && propNames.Contains("TransmitLayersEnabledTop")) return true;

        // Handle underscore-separated: swap_Id_Part -> SwapIdPart, isTenting_Top -> IsTentingTop
        var normalized = jsonPropName.Replace("_", "");
        if (propNames.Any(p => string.Equals(p.Replace("_", ""), normalized, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private Dictionary<string, Dictionary<string, PropInfo>> ExtractJsonProperties(string testDataRoot)
    {
        // Key is "Domain:ObjectType" e.g. "Pcb:Arc", "Sch:Arc"
        var types = new Dictionary<string, Dictionary<string, PropInfo>>(StringComparer.OrdinalIgnoreCase);

        var jsonFiles = Directory.GetFiles(testDataRoot, "*.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("debug", StringComparison.OrdinalIgnoreCase))
            .ToList();

        _output.WriteLine($"Processing {jsonFiles.Count} JSON files...");

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var domain = GetDomain(jsonFile);
                var json = File.ReadAllText(jsonFile);
                // Fix common JSON syntax errors from test data generator:
                // 1. Empty values like "prop": , -> "prop": null,
                json = System.Text.RegularExpressions.Regex.Replace(json,
                    @":\s*,", @": null,");
                // 2. Empty value at end of object: "prop": } -> "prop": null}
                json = System.Text.RegularExpressions.Regex.Replace(json,
                    @":\s*\}", @": null}");
                // 3. Missing comma between properties/elements — any value followed by newline and a property key
                //    Matches: "value"\n  "key": or 123\n  "key": or true\n  "key": or ]\n  "key": or }\n  "key":
                json = System.Text.RegularExpressions.Regex.Replace(json,
                    @"(""[^""]*""|true|false|null|\d+\.?\d*|\]|\})\s*\r?\n(\s*""[^""]+""[\t ]*:)",
                    @"$1,
$2");
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                };
                using var doc = JsonDocument.Parse(json, options);
                ProcessElement(doc.RootElement, types, Path.GetFileName(jsonFile), domain);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error: {Path.GetFileName(jsonFile)}: {ex.Message}");
            }
        }

        return types;
    }

    private static void ProcessElement(JsonElement el, Dictionary<string, Dictionary<string, PropInfo>> types, string source, string domain)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            if (el.TryGetProperty("objectType", out _))
                ExtractObject(el, types, source, domain);
            else
                foreach (var prop in el.EnumerateObject())
                    ProcessElement(prop.Value, types, source, domain);
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
                ProcessElement(item, types, source, domain);
        }
    }

    private static void ExtractObject(JsonElement obj, Dictionary<string, Dictionary<string, PropInfo>> types, string source, string domain)
    {
        var objectType = obj.GetProperty("objectType").GetString() ?? "Unknown";
        var key = $"{domain}:{objectType}";

        if (!types.TryGetValue(key, out var props))
        {
            props = new Dictionary<string, PropInfo>(StringComparer.OrdinalIgnoreCase);
            types[key] = props;
        }

        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Name == "objectType") continue;

            if (!props.TryGetValue(prop.Name, out var info))
            {
                info = new PropInfo();
                props[prop.Name] = info;
            }

            info.RecordValue(prop.Value);
            info.Sources.Add(source);

            if (prop.Value.ValueKind == JsonValueKind.Object)
                ProcessElement(prop.Value, types, source, domain);
            else if (prop.Value.ValueKind == JsonValueKind.Array)
                foreach (var item in prop.Value.EnumerateArray())
                    ProcessElement(item, types, source, domain);
        }
    }

    private static Dictionary<string, HashSet<string>> ExtractModelProperties()
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        var modelTypes = new[]
        {
            typeof(SchComponent), typeof(SchPin), typeof(SchWire), typeof(SchLine),
            typeof(SchRectangle), typeof(SchLabel), typeof(SchArc), typeof(SchPolygon),
            typeof(SchPolyline), typeof(SchBezier), typeof(SchEllipse), typeof(SchRoundedRectangle),
            typeof(SchPie), typeof(SchEllipticalArc), typeof(SchParameter), typeof(SchNetLabel),
            typeof(SchJunction), typeof(SchTextFrame), typeof(SchImage), typeof(SchSymbol),
            typeof(SchPowerObject),
            typeof(SchNoErc), typeof(SchBusEntry), typeof(SchBus), typeof(SchPort),
            typeof(SchSheetSymbol), typeof(SchSheetEntry),
            typeof(SchBlanket), typeof(SchParameterSet),
            typeof(PcbPad), typeof(PcbVia), typeof(PcbTrack), typeof(PcbArc),
            typeof(PcbText), typeof(PcbFill), typeof(PcbRegion), typeof(PcbComponentBody),
            typeof(PcbComponent), typeof(PcbNet), typeof(PcbPolygon), typeof(PcbEmbeddedBoard),
        };

        foreach (var type in modelTypes)
        {
            // Include properties from this type AND all base types
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            result[type.Name] = props;
        }

        return result;
    }

    private static Dictionary<string, HashSet<string>> ExtractDtoProperties()
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        var assembly = typeof(SchComponent).Assembly;
        var dtoTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Dto") && t.Namespace?.Contains("Dto") == true)
            .ToList();

        foreach (var type in dtoTypes)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            result[type.Name] = props;
        }

        return result;
    }

    private static int GetFileCount(KeyValuePair<string, Dictionary<string, PropInfo>> type)
    {
        var allSources = new HashSet<string>();
        foreach (var prop in type.Value.Values)
            foreach (var s in prop.Sources)
                allSources.Add(s);
        return allSources.Count;
    }

    private static string GetDataPath(params string[] parts)
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    private class PropInfo
    {
        public HashSet<string> Samples { get; } = new();
        public HashSet<JsonValueKind> ValueKinds { get; } = new();
        public HashSet<string> Sources { get; } = new();
        public int Count { get; set; }

        public void RecordValue(JsonElement value)
        {
            Count++;
            ValueKinds.Add(value.ValueKind);
            if (Samples.Count < 5)
            {
                var s = value.ValueKind switch
                {
                    JsonValueKind.String => $"\"{Truncate(value.GetString() ?? "", 30)}\"",
                    JsonValueKind.Number => value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Array => $"[{value.GetArrayLength()} items]",
                    JsonValueKind.Object => "{...}",
                    JsonValueKind.Null => "null",
                    _ => "?"
                };
                Samples.Add(s);
            }
        }

        public string InferredType
        {
            get
            {
                if (ValueKinds.Contains(JsonValueKind.Array)) return "array";
                if (ValueKinds.Contains(JsonValueKind.Object)) return "object";
                if (ValueKinds.Contains(JsonValueKind.String)) return "string";
                if (ValueKinds.Contains(JsonValueKind.True) || ValueKinds.Contains(JsonValueKind.False)) return "bool";
                if (ValueKinds.Contains(JsonValueKind.Number))
                    return Samples.Any(s => s.Contains('.')) ? "float" : "int";
                return "mixed";
            }
        }

        static string Truncate(string s, int max) => s.Length > max ? s[..max] + "..." : s;
    }
}
