// C# script to extract all unique properties per object type from all JSON test files
// Run with: dotnet script extract-json-properties.csx
// Or: dotnet run in a console project

using System.Text.Json;

var testDataRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "TestData"));
if (!Directory.Exists(testDataRoot))
{
    testDataRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "TestData"));
}

Console.WriteLine($"Scanning: {testDataRoot}");

// Collect all properties per objectType
var propertiesByType = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
// propertiesByType[objectType][propertyName] = set of sample values

var jsonFiles = Directory.GetFiles(testDataRoot, "*.json", SearchOption.AllDirectories)
    .Where(f => !f.Contains("debug") && !f.Contains("export-debug"))
    .OrderBy(f => f)
    .ToList();

Console.WriteLine($"Found {jsonFiles.Count} JSON files\n");

foreach (var jsonFile in jsonFiles)
{
    try
    {
        var json = File.ReadAllText(jsonFile);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Handle both array and object root
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    // Top-level component/footprint
                    ExtractProperties(prop.Value, propertiesByType, Path.GetFileName(jsonFile));
                }
                else if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                            ExtractProperties(item, propertiesByType, Path.GetFileName(jsonFile));
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error parsing {Path.GetFileName(jsonFile)}: {ex.Message}");
    }
}

// Output results sorted by object type
Console.WriteLine("=== PROPERTY INVENTORY BY OBJECT TYPE ===\n");

foreach (var type in propertiesByType.OrderBy(t => t.Key))
{
    Console.WriteLine($"## {type.Key} ({type.Value.Count} properties)");
    foreach (var prop in type.Value.OrderBy(p => p.Key))
    {
        var samples = prop.Value.Take(3).ToList();
        var sampleStr = string.Join(", ", samples.Select(s => s.Length > 40 ? s.Substring(0, 40) + "..." : s));
        Console.WriteLine($"  - {prop.Key}: [{sampleStr}]");
    }
    Console.WriteLine();
}

static void ExtractProperties(JsonElement element, Dictionary<string, Dictionary<string, HashSet<string>>> propertiesByType, string sourceFile)
{
    if (element.ValueKind != JsonValueKind.Object) return;

    // Determine object type
    string objectType = "Unknown";
    if (element.TryGetProperty("objectType", out var otProp))
        objectType = otProp.GetString() ?? "Unknown";

    if (!propertiesByType.TryGetValue(objectType, out var props))
    {
        props = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        propertiesByType[objectType] = props;
    }

    foreach (var prop in element.EnumerateObject())
    {
        if (prop.Name == "objectType") continue;

        if (!props.TryGetValue(prop.Name, out var samples))
        {
            samples = new HashSet<string>();
            props[prop.Name] = samples;
        }

        // Store sample value for type inference
        var valueStr = prop.Value.ValueKind switch
        {
            JsonValueKind.String => $"str:\"{prop.Value.GetString()}\"",
            JsonValueKind.Number => $"num:{prop.Value.GetRawText()}",
            JsonValueKind.True => "bool:true",
            JsonValueKind.False => "bool:false",
            JsonValueKind.Array => $"array[{prop.Value.GetArrayLength()}]",
            JsonValueKind.Object => "object",
            JsonValueKind.Null => "null",
            _ => prop.Value.ValueKind.ToString()
        };

        if (samples.Count < 5)
            samples.Add(valueStr);

        // Recurse into nested objects/arrays
        if (prop.Value.ValueKind == JsonValueKind.Object)
        {
            ExtractProperties(prop.Value, propertiesByType, sourceFile);
        }
        else if (prop.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in prop.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                    ExtractProperties(item, propertiesByType, sourceFile);
            }
        }
    }
}
