using System.Text.Json;
using System.Text.Json.Nodes;

namespace OriginalCircuit.AltiumSharp.Export.Comparison;

/// <summary>
/// Options for controlling the diff behavior.
/// </summary>
public sealed record DiffOptions
{
    /// <summary>
    /// Only compare the raw MCDF section.
    /// </summary>
    public bool RawMcdfOnly { get; init; }

    /// <summary>
    /// Only compare the parsed model section.
    /// </summary>
    public bool ParsedModelOnly { get; init; }

    /// <summary>
    /// Focus on finding new parameters (for version discovery).
    /// </summary>
    public bool FindNewParametersOnly { get; init; }

    /// <summary>
    /// Ignore metadata section in comparison.
    /// </summary>
    public bool IgnoreMetadata { get; init; } = true;

    /// <summary>
    /// Paths to ignore during comparison (regex patterns).
    /// </summary>
    public List<string> IgnorePaths { get; init; } = [];

    /// <summary>
    /// Maximum number of differences to report.
    /// </summary>
    public int MaxDifferences { get; init; } = 1000;
}

/// <summary>
/// Compares two JSON export files to find differences.
/// </summary>
public sealed class JsonDiffer
{
    private readonly DiffOptions _options;
    private readonly List<Difference> _differences = [];
    private readonly List<NewParameter> _newParameters = [];
    private int _diffCount;

    public JsonDiffer(DiffOptions? options = null)
    {
        _options = options ?? new DiffOptions();
    }

    /// <summary>
    /// Compare two JSON export files.
    /// </summary>
    public DiffResult Compare(string file1Path, string file2Path)
    {
        var json1 = File.ReadAllText(file1Path);
        var json2 = File.ReadAllText(file2Path);

        return CompareJson(json1, json2, file1Path, file2Path);
    }

    /// <summary>
    /// Compare two JSON strings.
    /// </summary>
    public DiffResult CompareJson(string json1, string json2, string file1Name = "file1", string file2Name = "file2")
    {
        _differences.Clear();
        _newParameters.Clear();
        _diffCount = 0;

        var node1 = JsonNode.Parse(json1);
        var node2 = JsonNode.Parse(json2);

        if (node1 == null || node2 == null)
        {
            throw new JsonException("Failed to parse JSON files");
        }

        // Determine what sections to compare
        if (_options.RawMcdfOnly)
        {
            CompareNodes(node1["rawMcdf"], node2["rawMcdf"], "$.rawMcdf", "rawMcdf");
        }
        else if (_options.ParsedModelOnly)
        {
            CompareNodes(node1["parsedModel"], node2["parsedModel"], "$.parsedModel", "parsedModel");
        }
        else
        {
            // Compare everything (except metadata if ignored)
            if (!_options.IgnoreMetadata)
            {
                CompareNodes(node1["metadata"], node2["metadata"], "$.metadata", "metadata");
            }
            CompareNodes(node1["rawMcdf"], node2["rawMcdf"], "$.rawMcdf", "rawMcdf");
            CompareNodes(node1["parsedModel"], node2["parsedModel"], "$.parsedModel", "parsedModel");
        }

        // Look for new parameters if requested or by default
        if (_options.FindNewParametersOnly || !_options.ParsedModelOnly)
        {
            FindNewParameters(node1["rawMcdf"], node2["rawMcdf"]);
        }

        return new DiffResult
        {
            File1 = file1Name,
            File2 = file2Name,
            Differences = [.. _differences],
            NewParameters = [.. _newParameters],
            Summary = CalculateSummary()
        };
    }

    private void CompareNodes(JsonNode? node1, JsonNode? node2, string path, string context)
    {
        if (_diffCount >= _options.MaxDifferences)
            return;

        if (ShouldIgnorePath(path))
            return;

        // Handle nulls
        if (node1 == null && node2 == null)
            return;

        if (node1 == null)
        {
            AddDifference(path, DifferenceType.Added, null, node2?.ToJsonString(), context);
            return;
        }

        if (node2 == null)
        {
            AddDifference(path, DifferenceType.Removed, node1.ToJsonString(), null, context);
            return;
        }

        // Check type match
        if (node1.GetValueKind() != node2.GetValueKind())
        {
            AddDifference(path, DifferenceType.TypeChanged,
                node1.ToJsonString(), node2.ToJsonString(), context,
                $"Type changed from {node1.GetValueKind()} to {node2.GetValueKind()}");
            return;
        }

        switch (node1)
        {
            case JsonObject obj1 when node2 is JsonObject obj2:
                CompareObjects(obj1, obj2, path, context);
                break;

            case JsonArray arr1 when node2 is JsonArray arr2:
                CompareArrays(arr1, arr2, path, context);
                break;

            case JsonValue val1 when node2 is JsonValue val2:
                CompareValues(val1, val2, path, context);
                break;
        }
    }

    private void CompareObjects(JsonObject obj1, JsonObject obj2, string path, string context)
    {
        var allKeys = obj1.Select(p => p.Key).Union(obj2.Select(p => p.Key)).ToHashSet();

        foreach (var key in allKeys)
        {
            if (_diffCount >= _options.MaxDifferences)
                break;

            var childPath = $"{path}.{key}";
            var childContext = GetContext(context, key, obj1, obj2);

            var has1 = obj1.TryGetPropertyValue(key, out var val1);
            var has2 = obj2.TryGetPropertyValue(key, out var val2);

            if (has1 && !has2)
            {
                AddDifference(childPath, DifferenceType.Removed, val1?.ToJsonString(), null, childContext);
            }
            else if (!has1 && has2)
            {
                AddDifference(childPath, DifferenceType.Added, null, val2?.ToJsonString(), childContext);
            }
            else
            {
                CompareNodes(val1, val2, childPath, childContext);
            }
        }
    }

    private void CompareArrays(JsonArray arr1, JsonArray arr2, string path, string context)
    {
        // For arrays, try to match by identifiable properties first
        var identifier = FindArrayIdentifier(arr1, arr2);

        if (identifier != null)
        {
            CompareArraysByIdentifier(arr1, arr2, path, context, identifier);
        }
        else
        {
            CompareArraysByIndex(arr1, arr2, path, context);
        }
    }

    private string? FindArrayIdentifier(JsonArray arr1, JsonArray arr2)
    {
        // Check common identifier properties
        var identifiers = new[] { "name", "pattern", "uniqueId", "designator", "libReference" };

        foreach (var id in identifiers)
        {
            bool allHave1 = arr1.Count == 0 || arr1.All(n => n is JsonObject obj && obj.ContainsKey(id));
            bool allHave2 = arr2.Count == 0 || arr2.All(n => n is JsonObject obj && obj.ContainsKey(id));

            if (allHave1 && allHave2)
                return id;
        }

        return null;
    }

    private void CompareArraysByIdentifier(JsonArray arr1, JsonArray arr2, string path, string context, string identifier)
    {
        var items1 = arr1
            .OfType<JsonObject>()
            .ToDictionary(o => o[identifier]?.ToString() ?? "", o => o);

        var items2 = arr2
            .OfType<JsonObject>()
            .ToDictionary(o => o[identifier]?.ToString() ?? "", o => o);

        // Find added
        foreach (var key in items2.Keys.Except(items1.Keys))
        {
            if (_diffCount >= _options.MaxDifferences) break;
            var itemPath = $"{path}[{identifier}='{key}']";
            AddDifference(itemPath, DifferenceType.ArrayItemAdded, null, items2[key].ToJsonString(), $"{context}/{key}");
        }

        // Find removed
        foreach (var key in items1.Keys.Except(items2.Keys))
        {
            if (_diffCount >= _options.MaxDifferences) break;
            var itemPath = $"{path}[{identifier}='{key}']";
            AddDifference(itemPath, DifferenceType.ArrayItemRemoved, items1[key].ToJsonString(), null, $"{context}/{key}");
        }

        // Find changed
        foreach (var key in items1.Keys.Intersect(items2.Keys))
        {
            if (_diffCount >= _options.MaxDifferences) break;
            var itemPath = $"{path}[{identifier}='{key}']";
            CompareNodes(items1[key], items2[key], itemPath, $"{context}/{key}");
        }
    }

    private void CompareArraysByIndex(JsonArray arr1, JsonArray arr2, string path, string context)
    {
        var maxLen = Math.Max(arr1.Count, arr2.Count);

        for (int i = 0; i < maxLen; i++)
        {
            if (_diffCount >= _options.MaxDifferences) break;

            var itemPath = $"{path}[{i}]";

            if (i >= arr1.Count)
            {
                AddDifference(itemPath, DifferenceType.ArrayItemAdded, null, arr2[i]?.ToJsonString(), context);
            }
            else if (i >= arr2.Count)
            {
                AddDifference(itemPath, DifferenceType.ArrayItemRemoved, arr1[i]?.ToJsonString(), null, context);
            }
            else
            {
                CompareNodes(arr1[i], arr2[i], itemPath, context);
            }
        }
    }

    private void CompareValues(JsonValue val1, JsonValue val2, string path, string context)
    {
        var str1 = val1.ToJsonString();
        var str2 = val2.ToJsonString();

        if (str1 != str2)
        {
            AddDifference(path, DifferenceType.ValueChanged, str1, str2, context);
        }
    }

    private void FindNewParameters(JsonNode? node1, JsonNode? node2)
    {
        if (node2 == null) return;

        // Look for parameters in the raw MCDF streams
        FindNewParametersInNode(node1, node2, "$.rawMcdf");
    }

    private void FindNewParametersInNode(JsonNode? node1, JsonNode? node2, string path)
    {
        if (node2 is JsonObject obj2)
        {
            // Check if this is a parameters object
            if (obj2.ContainsKey("parameters") && obj2["parameters"] is JsonObject params2)
            {
                var params1 = (node1 as JsonObject)?["parameters"] as JsonObject;

                foreach (var prop in params2)
                {
                    if (params1 == null || !params1.ContainsKey(prop.Key))
                    {
                        _newParameters.Add(new NewParameter
                        {
                            Path = $"{path}.parameters.{prop.Key}",
                            Name = prop.Key,
                            Value = prop.Value?.ToString() ?? "",
                            Context = ExtractStreamContext(path),
                            LikelyPurpose = InferParameterPurpose(prop.Key, prop.Value?.ToString())
                        });
                    }
                }
            }

            // Recurse into child objects
            foreach (var prop in obj2)
            {
                var child1 = (node1 as JsonObject)?[prop.Key];
                FindNewParametersInNode(child1, prop.Value, $"{path}.{prop.Key}");
            }
        }
        else if (node2 is JsonArray arr2)
        {
            var arr1 = node1 as JsonArray;
            for (int i = 0; i < arr2.Count; i++)
            {
                var child1 = arr1 != null && i < arr1.Count ? arr1[i] : null;
                FindNewParametersInNode(child1, arr2[i], $"{path}[{i}]");
            }
        }
    }

    private static string ExtractStreamContext(string path)
    {
        // Extract meaningful context from path like "$.rawMcdf.rootStorage.storages[0].streams[1]"
        var parts = path.Split('.');
        return string.Join("/", parts.Skip(2).Take(3));
    }

    private static string? InferParameterPurpose(string name, string? value)
    {
        var nameLower = name.ToLowerInvariant();

        if (nameLower.Contains("version")) return "Version identifier";
        if (nameLower.Contains("guid") || nameLower.Contains("uuid")) return "Unique identifier";
        if (nameLower.Contains("date") || nameLower.Contains("time")) return "Timestamp";
        if (nameLower.Contains("color")) return "Color value";
        if (nameLower.Contains("size") || nameLower.Contains("width") || nameLower.Contains("height")) return "Dimension";
        if (nameLower.Contains("name") || nameLower.Contains("ref")) return "Reference/Name";
        if (nameLower.Contains("layer")) return "Layer identifier";
        if (nameLower.Contains("font")) return "Font setting";
        if (nameLower.StartsWith("is") || nameLower.StartsWith("has") || nameLower.StartsWith("enable")) return "Boolean flag";

        return null;
    }

    private string GetContext(string parentContext, string key, JsonObject obj1, JsonObject obj2)
    {
        // Try to find an identifying property
        foreach (var id in new[] { "name", "pattern", "libReference" })
        {
            var val = obj1[id]?.ToString() ?? obj2[id]?.ToString();
            if (!string.IsNullOrEmpty(val))
            {
                return $"{parentContext}/{val}";
            }
        }
        return $"{parentContext}/{key}";
    }

    private bool ShouldIgnorePath(string path)
    {
        foreach (var pattern in _options.IgnorePaths)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(path, pattern))
                return true;
        }
        return false;
    }

    private void AddDifference(string path, DifferenceType type, string? oldValue, string? newValue, string context, string? notes = null)
    {
        if (_options.FindNewParametersOnly && type != DifferenceType.Added)
            return;

        _differences.Add(new Difference
        {
            Path = path,
            Type = type,
            OldValue = TruncateValue(oldValue),
            NewValue = TruncateValue(newValue),
            Context = context,
            Notes = notes
        });
        _diffCount++;
    }

    private static string? TruncateValue(string? value, int maxLength = 200)
    {
        if (value == null) return null;
        if (value.Length <= maxLength) return value;
        return value[..(maxLength - 3)] + "...";
    }

    private DiffSummary CalculateSummary()
    {
        var bySection = _differences
            .GroupBy(d => GetSection(d.Path))
            .ToDictionary(g => g.Key, g => g.Count());

        return new DiffSummary
        {
            TotalDifferences = _differences.Count + _newParameters.Count,
            AddedFields = _differences.Count(d => d.Type == DifferenceType.Added || d.Type == DifferenceType.ArrayItemAdded),
            RemovedFields = _differences.Count(d => d.Type == DifferenceType.Removed || d.Type == DifferenceType.ArrayItemRemoved),
            ChangedValues = _differences.Count(d => d.Type == DifferenceType.ValueChanged || d.Type == DifferenceType.ArrayItemChanged),
            TypeChanges = _differences.Count(d => d.Type == DifferenceType.TypeChanged),
            BySection = bySection
        };
    }

    private static string GetSection(string path)
    {
        var parts = path.Split('.');
        if (parts.Length >= 2)
        {
            return parts[1]; // e.g., "rawMcdf" or "parsedModel"
        }
        return "root";
    }
}
