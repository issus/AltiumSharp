using System.Reflection;
using System.Text.RegularExpressions;
using OriginalCircuit.Altium.Models;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Tracks unknown/undocumented fields by comparing raw parameters against
/// what the AltiumSharp library actually parses and uses.
/// </summary>
public sealed class UnknownFieldTracker
{
    private readonly HashSet<string> _knownPcbParameters = [];
    private readonly HashSet<string> _knownSchParameters = [];
    private readonly Dictionary<string, HashSet<string>> _knownParametersByRecordType = [];
    private bool _initialized;

    /// <summary>
    /// Initialize the tracker by scanning AltiumSharp types for known parameters.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;

        // Scan PCB primitive types
        ScanPrimitiveTypes(typeof(PcbPad), _knownPcbParameters);

        // Scan Schematic primitive types
        ScanPrimitiveTypes(typeof(SchPin), _knownSchParameters);

        // Build per-record-type knowledge
        BuildRecordTypeKnowledge();

        _initialized = true;
    }

    /// <summary>
    /// Analyze parameters from a raw MCDF stream and identify unknown fields.
    /// </summary>
    public UnknownFieldReport AnalyzeParameters(
        Dictionary<string, string> rawParameters,
        string context,
        bool isPcb = true)
    {
        Initialize();

        var report = new UnknownFieldReport
        {
            Context = context,
            TotalParameters = rawParameters.Count
        };

        var knownParams = isPcb ? _knownPcbParameters : _knownSchParameters;

        foreach (var (key, value) in rawParameters)
        {
            var normalizedKey = NormalizeParameterName(key);

            if (IsKnownParameter(normalizedKey, knownParams))
            {
                report.KnownParameters.Add(new ParameterInfo
                {
                    Name = key,
                    NormalizedName = normalizedKey,
                    Value = value,
                    ValueType = InferValueType(value)
                });
            }
            else
            {
                report.UnknownParameters.Add(new ParameterInfo
                {
                    Name = key,
                    NormalizedName = normalizedKey,
                    Value = value,
                    ValueType = InferValueType(value),
                    PossiblePurpose = GuessPurpose(key, value)
                });
            }
        }

        report.UnknownPercentage = report.TotalParameters > 0
            ? (double)report.UnknownParameters.Count / report.TotalParameters * 100
            : 0;

        return report;
    }

    /// <summary>
    /// Analyze a specific record type's parameters against known fields.
    /// </summary>
    public UnknownFieldReport AnalyzeRecordParameters(
        Dictionary<string, string> rawParameters,
        string recordType)
    {
        Initialize();

        var report = new UnknownFieldReport
        {
            Context = $"Record: {recordType}",
            TotalParameters = rawParameters.Count
        };

        // Get known parameters for this specific record type
        var recordKnown = _knownParametersByRecordType.GetValueOrDefault(recordType.ToUpperInvariant())
            ?? [];

        foreach (var (key, value) in rawParameters)
        {
            var normalizedKey = NormalizeParameterName(key);

            var isKnown = recordKnown.Contains(normalizedKey) ||
                          IsCommonKnownParameter(normalizedKey);

            var info = new ParameterInfo
            {
                Name = key,
                NormalizedName = normalizedKey,
                Value = value,
                ValueType = InferValueType(value),
                PossiblePurpose = isKnown ? null : GuessPurpose(key, value)
            };

            if (isKnown)
                report.KnownParameters.Add(info);
            else
                report.UnknownParameters.Add(info);
        }

        report.UnknownPercentage = report.TotalParameters > 0
            ? (double)report.UnknownParameters.Count / report.TotalParameters * 100
            : 0;

        return report;
    }

    /// <summary>
    /// Compare two sets of parameters to find newly added fields.
    /// </summary>
    public FieldComparisonResult CompareFields(
        Dictionary<string, string> oldParams,
        Dictionary<string, string> newParams,
        string context)
    {
        var result = new FieldComparisonResult { Context = context };

        var oldKeys = oldParams.Keys.Select(NormalizeParameterName).ToHashSet();
        var newKeys = newParams.Keys.Select(NormalizeParameterName).ToHashSet();

        // Find added fields
        foreach (var key in newKeys.Except(oldKeys))
        {
            var originalKey = newParams.Keys.First(k => NormalizeParameterName(k) == key);
            result.AddedFields.Add(new ParameterInfo
            {
                Name = originalKey,
                NormalizedName = key,
                Value = newParams[originalKey],
                ValueType = InferValueType(newParams[originalKey])
            });
        }

        // Find removed fields
        foreach (var key in oldKeys.Except(newKeys))
        {
            var originalKey = oldParams.Keys.First(k => NormalizeParameterName(k) == key);
            result.RemovedFields.Add(new ParameterInfo
            {
                Name = originalKey,
                NormalizedName = key,
                Value = oldParams[originalKey],
                ValueType = InferValueType(oldParams[originalKey])
            });
        }

        // Find changed fields
        foreach (var key in oldKeys.Intersect(newKeys))
        {
            var oldKey = oldParams.Keys.First(k => NormalizeParameterName(k) == key);
            var newKey = newParams.Keys.First(k => NormalizeParameterName(k) == key);

            if (oldParams[oldKey] != newParams[newKey])
            {
                result.ChangedFields.Add(new FieldChange
                {
                    Name = newKey,
                    OldValue = oldParams[oldKey],
                    NewValue = newParams[newKey],
                    OldType = InferValueType(oldParams[oldKey]),
                    NewType = InferValueType(newParams[newKey])
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Get a summary of all unknown fields found across multiple analyses.
    /// </summary>
    public UnknownFieldsSummary GetSummary(IEnumerable<UnknownFieldReport> reports)
    {
        var summary = new UnknownFieldsSummary();
        var allUnknown = new Dictionary<string, UnknownFieldOccurrence>();

        foreach (var report in reports)
        {
            summary.TotalReportsAnalyzed++;
            summary.TotalParametersAnalyzed += report.TotalParameters;

            foreach (var param in report.UnknownParameters)
            {
                if (!allUnknown.TryGetValue(param.NormalizedName, out var occurrence))
                {
                    occurrence = new UnknownFieldOccurrence
                    {
                        Name = param.Name,
                        NormalizedName = param.NormalizedName,
                        InferredType = param.ValueType,
                        PossiblePurpose = param.PossiblePurpose
                    };
                    allUnknown[param.NormalizedName] = occurrence;
                }

                occurrence.Occurrences++;
                occurrence.Contexts.Add(report.Context);
                occurrence.SampleValues.Add(param.Value);

                // Keep only first 5 sample values
                if (occurrence.SampleValues.Count > 5)
                {
                    occurrence.SampleValues = occurrence.SampleValues.Take(5).ToList();
                }
            }
        }

        summary.UnknownFields = allUnknown.Values
            .OrderByDescending(o => o.Occurrences)
            .ToList();

        summary.TotalUnknownFields = summary.UnknownFields.Count;

        return summary;
    }

    private void ScanPrimitiveTypes(Type sampleType, HashSet<string> knownParams)
    {
        var assembly = sampleType.Assembly;
        var primitiveInterface = typeof(IPrimitive);
        var types = assembly.GetTypes()
            .Where(t => primitiveInterface.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface
                        && t.Namespace == sampleType.Namespace);

        foreach (var type in types)
        {
            // Get properties that map to parameters
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var paramName = NormalizeParameterName(prop.Name);
                knownParams.Add(paramName);

                // Also add common variations
                knownParams.Add(paramName.ToUpperInvariant());

                // Handle indexed properties like X1, X2, Y1, Y2
                if (prop.Name.EndsWith("1") || prop.Name.EndsWith("2"))
                {
                    var baseName = prop.Name[..^1];
                    knownParams.Add(NormalizeParameterName(baseName));
                }
            }

            // Scan for ImportFromParameters method to find string literals
            var importMethod = type.GetMethod("ImportFromParameters",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (importMethod != null)
            {
                // We can't easily scan method bodies, but we know common patterns
                AddCommonParameterPatterns(knownParams);
            }
        }
    }

    private void BuildRecordTypeKnowledge()
    {
        // Map record types to their known parameters
        // This is built from analyzing the AltiumSharp source code

        // Common schematic record types
        AddRecordKnowledge("1", ["LIBREFERENCE", "COMPONENTDESCRIPTION", "PARTCOUNT",
            "DISPLAYMODECOUNT", "INDEXINSHEET", "OWNERPARTID", "LOCATION.X", "LOCATION.Y",
            "CURRENTPARTID", "LIBRARYPATH", "SOURCELIBRARYNAME", "SHEETPARTFILENAME",
            "TARGETFILENAME", "UNIQUEID", "AABORB", "PARTIDLOCKED", "NOTUSEDBTABLENAME",
            "DESIGNITEMID", "DATABASETABLENAME", "ALIASLIST"]);

        AddRecordKnowledge("2", ["LOCATION.X", "LOCATION.Y", "PINLENGTH", "PINCONGLOMERATE",
            "PINELECTRICAL", "DESCRIPTION", "FORMALTYPE", "NAME", "DESIGNATOR", "OWNERPARTID",
            "OWNERPARTDISPLAYMODE", "GRAPHICALLYLOCKED", "SWAPIDPIN", "HIDDEN", "SHOWPINNAME"]);

        AddRecordKnowledge("4", ["TEXT", "LOCATION.X", "LOCATION.Y", "COLOR", "FONTID",
            "JUSTIFICATION", "ORIENTATION", "ISMIRRORED", "ISHIDDEN", "OWNERPARTID"]);

        AddRecordKnowledge("6", ["LOCATION.X", "LOCATION.Y", "CORNER.X", "CORNER.Y",
            "OWNERPARTID", "LINEWIDTH", "COLOR", "ISSOLID", "TRANSPARENT"]);

        AddRecordKnowledge("7", ["LOCATION.X", "LOCATION.Y", "CORNER.X", "CORNER.Y",
            "OWNERPARTID", "LINEWIDTH", "COLOR", "ISSOLID", "TRANSPARENT"]);

        AddRecordKnowledge("8", ["LOCATIONCOUNT", "X1", "Y1", "X2", "Y2", "OWNERPARTID",
            "LINEWIDTH", "COLOR", "ISSOLID"]);

        AddRecordKnowledge("12", ["LOCATION.X", "LOCATION.Y", "RADIUS", "SECONDARYRADIUS",
            "STARTANGLE", "ENDANGLE", "LINEWIDTH", "COLOR", "ISSOLID", "OWNERPARTID"]);

        AddRecordKnowledge("13", ["LOCATION.X", "LOCATION.Y", "XSIZE", "YSIZE", "OWNERPARTID",
            "LINEWIDTH", "COLOR", "ISSOLID", "TRANSPARENT", "CORNER.X", "CORNER.Y"]);

        AddRecordKnowledge("14", ["LOCATIONCOUNT", "X1", "Y1", "X2", "Y2", "OWNERPARTID",
            "LINEWIDTH", "COLOR", "ISSOLID"]);

        // PCB record types
        AddRecordKnowledge("PAD", ["NAME", "X", "Y", "XSIZE", "YSIZE", "HOLESIZE", "LAYER",
            "PLATED", "ROTATION", "SHAPE", "TOPXSIZE", "TOPYSIZE", "TOPSHAPE", "MIDXSIZE",
            "MIDYSIZE", "MIDSHAPE", "BOTXSIZE", "BOTYSIZE", "BOTSHAPE", "PASTEMASKEXPANSION",
            "SOLDERMASKEXPANSION", "JUMPERID", "NET"]);

        AddRecordKnowledge("TRACK", ["X1", "Y1", "X2", "Y2", "WIDTH", "LAYER", "NET",
            "COMPONENT", "KEEPOUT", "UNIONINDEX"]);

        AddRecordKnowledge("ARC", ["X", "Y", "RADIUS", "STARTANGLE", "ENDANGLE", "WIDTH",
            "LAYER", "NET", "COMPONENT", "KEEPOUT"]);

        AddRecordKnowledge("VIA", ["X", "Y", "HOLESIZE", "SIZE", "STARTLAYER", "ENDLAYER",
            "NET", "THERMALRELIEF", "SOLDERMASKEXPANSION"]);

        AddRecordKnowledge("FILL", ["X1", "Y1", "X2", "Y2", "ROTATION", "LAYER", "NET",
            "COMPONENT", "KEEPOUT"]);

        AddRecordKnowledge("REGION", ["LAYER", "NET", "COMPONENT", "KEEPOUT", "SUBPOLYINDEX",
            "UNIONINDEX", "KIND"]);
    }

    private void AddRecordKnowledge(string recordType, IEnumerable<string> parameters)
    {
        var key = recordType.ToUpperInvariant();
        if (!_knownParametersByRecordType.TryGetValue(key, out var set))
        {
            set = [];
            _knownParametersByRecordType[key] = set;
        }

        foreach (var param in parameters)
        {
            set.Add(NormalizeParameterName(param));
        }
    }

    private static void AddCommonParameterPatterns(HashSet<string> knownParams)
    {
        // Common parameter patterns used across Altium files
        var common = new[]
        {
            "RECORD", "OWNERINDEX", "INDEXINSHEET", "OWNERPARTID", "OWNERPARTDISPLAYMODE",
            "LOCATION.X", "LOCATION.Y", "CORNER.X", "CORNER.Y",
            "X", "Y", "X1", "Y1", "X2", "Y2",
            "WIDTH", "HEIGHT", "LINEWIDTH", "COLOR", "AREACOLOR",
            "ISSOLID", "TRANSPARENT", "ISMIRRORED", "ISHIDDEN", "ISNOTACCESIBLE",
            "FONTID", "FONTNAME", "SIZE", "BOLD", "ITALIC", "UNDERLINE",
            "TEXT", "NAME", "DESCRIPTION", "UNIQUEID",
            "LAYER", "NET", "COMPONENT", "DESIGNATOR",
            "ROTATION", "ORIENTATION", "JUSTIFICATION",
            "GRAPHICALLYLOCKED", "USERROUTED", "KEEPOUT",
            "HOLESIZE", "PLATED", "SHAPE", "RADIUS",
            "STARTANGLE", "ENDANGLE", "PRIMARYRADIUS", "SECONDARYRADIUS",
            "PASTEMASKEXPANSION", "SOLDERMASKEXPANSION",
            "POLYGONOUTLINE", "REMOVELOOP", "AVOIDOBSTACLES"
        };

        foreach (var param in common)
        {
            knownParams.Add(NormalizeParameterName(param));
        }
    }

    private static string NormalizeParameterName(string name)
    {
        // Remove common prefixes
        if (name.StartsWith("%UTF8%", StringComparison.OrdinalIgnoreCase))
            name = name[6..];

        // Normalize to uppercase without special characters
        return name.ToUpperInvariant()
            .Replace(".", "")
            .Replace("_", "")
            .Replace(" ", "");
    }

    private bool IsKnownParameter(string normalizedName, HashSet<string> knownParams)
    {
        if (knownParams.Contains(normalizedName))
            return true;

        // Check for indexed variations (e.g., X1, X2, VERTEX1, VERTEX2)
        var match = Regex.Match(normalizedName, @"^(.+?)(\d+)$");
        if (match.Success)
        {
            var baseName = match.Groups[1].Value;
            if (knownParams.Contains(baseName))
                return true;
        }

        return IsCommonKnownParameter(normalizedName);
    }

    private static bool IsCommonKnownParameter(string normalizedName)
    {
        // Parameters that are commonly known across all record types
        return normalizedName switch
        {
            "RECORD" => true,
            "OWNERINDEX" => true,
            "INDEXINSHEET" => true,
            "OWNERPARTID" => true,
            "OWNERPARTDISPLAYMODE" => true,
            "UNIQUEID" => true,
            _ => false
        };
    }

    private static string InferValueType(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "empty";

        // Boolean
        if (value.Equals("T", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("F", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            return "boolean";

        // Integer
        if (int.TryParse(value, out _))
            return "integer";

        // Float
        if (double.TryParse(value, out _))
            return "float";

        // Coordinate (ends with mil or mm)
        if (value.EndsWith("mil", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            return "coordinate";

        // Color (hex format)
        if (Regex.IsMatch(value, @"^[0-9A-Fa-f]{6}$"))
            return "color";

        // GUID
        if (Guid.TryParse(value, out _))
            return "guid";

        // Base64 (likely binary data)
        if (value.Length > 20 && Regex.IsMatch(value, @"^[A-Za-z0-9+/]+=*$"))
            return "base64";

        return "string";
    }

    private static string? GuessPurpose(string key, string value)
    {
        var upperKey = key.ToUpperInvariant();

        // Location/position related
        if (upperKey.Contains("LOCATION") || upperKey.Contains("POSITION") ||
            upperKey.Contains("CORNER") || upperKey.Contains("OFFSET"))
            return "Position/coordinate field";

        // Size related
        if (upperKey.Contains("SIZE") || upperKey.Contains("WIDTH") ||
            upperKey.Contains("HEIGHT") || upperKey.Contains("LENGTH"))
            return "Dimension field";

        // Color related
        if (upperKey.Contains("COLOR") || upperKey.Contains("COLOUR"))
            return "Color field";

        // Index/ID related
        if (upperKey.Contains("INDEX") || upperKey.Contains("ID") ||
            upperKey.EndsWith("COUNT"))
            return "Index/identifier field";

        // Boolean flag
        if (upperKey.StartsWith("IS") || upperKey.StartsWith("HAS") ||
            upperKey.StartsWith("USE") || upperKey.StartsWith("SHOW") ||
            upperKey.StartsWith("HIDE") || upperKey.StartsWith("ENABLE"))
            return "Boolean flag";

        // Font related
        if (upperKey.Contains("FONT"))
            return "Font property";

        // Layer related
        if (upperKey.Contains("LAYER"))
            return "Layer specification";

        // Style/appearance
        if (upperKey.Contains("STYLE") || upperKey.Contains("MODE"))
            return "Style/appearance setting";

        // Mask related
        if (upperKey.Contains("MASK") || upperKey.Contains("EXPANSION"))
            return "Mask/expansion setting";

        return null;
    }
}

#region Result Types

public sealed class UnknownFieldReport
{
    public string Context { get; init; } = "";
    public int TotalParameters { get; init; }
    public double UnknownPercentage { get; set; }
    public List<ParameterInfo> KnownParameters { get; init; } = [];
    public List<ParameterInfo> UnknownParameters { get; init; } = [];
}

public sealed class ParameterInfo
{
    public string Name { get; init; } = "";
    public string NormalizedName { get; init; } = "";
    public string Value { get; init; } = "";
    public string ValueType { get; init; } = "";
    public string? PossiblePurpose { get; init; }
}

public sealed class FieldComparisonResult
{
    public string Context { get; init; } = "";
    public List<ParameterInfo> AddedFields { get; init; } = [];
    public List<ParameterInfo> RemovedFields { get; init; } = [];
    public List<FieldChange> ChangedFields { get; init; } = [];
}

public sealed class FieldChange
{
    public string Name { get; init; } = "";
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
    public string OldType { get; init; } = "";
    public string NewType { get; init; } = "";
}

public sealed class UnknownFieldsSummary
{
    public int TotalReportsAnalyzed { get; set; }
    public int TotalParametersAnalyzed { get; set; }
    public int TotalUnknownFields { get; set; }
    public List<UnknownFieldOccurrence> UnknownFields { get; set; } = [];
}

public sealed class UnknownFieldOccurrence
{
    public string Name { get; init; } = "";
    public string NormalizedName { get; init; } = "";
    public int Occurrences { get; set; }
    public string InferredType { get; init; } = "";
    public string? PossiblePurpose { get; init; }
    public HashSet<string> Contexts { get; init; } = [];
    public List<string> SampleValues { get; set; } = [];
}

#endregion
