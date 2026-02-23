// Standalone program to extract all properties from JSON test files
// Compile and run: dotnet run
// Or just run inline from the test project

using System.Text.Json;

class PropertyExtractor
{
    static Dictionary<string, Dictionary<string, PropertyInfo>> _types = new(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, HashSet<string>> _typeSourceFiles = new(StringComparer.OrdinalIgnoreCase);

    static void Main(string[] args)
    {
        var testDataRoot = args.Length > 0 ? args[0] : FindTestData();
        Console.WriteLine($"Scanning: {testDataRoot}");

        var jsonFiles = Directory.GetFiles(testDataRoot, "*.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("debug", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        Console.WriteLine($"Found {jsonFiles.Count} JSON files\n");

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(jsonFile);
                using var doc = JsonDocument.Parse(json);
                ProcessElement(doc.RootElement, Path.GetFileName(jsonFile));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {Path.GetFileName(jsonFile)}: {ex.Message}");
            }
        }

        // Output
        foreach (var type in _types.OrderBy(t => t.Key))
        {
            var files = _typeSourceFiles.TryGetValue(type.Key, out var f) ? f.Count : 0;
            Console.WriteLine($"\n## {type.Key} ({type.Value.Count} properties, found in {files} files)");
            foreach (var prop in type.Value.OrderBy(p => p.Key))
            {
                var info = prop.Value;
                Console.WriteLine($"  {prop.Key} | {info.InferredType} | samples: {string.Join(", ", info.Samples.Take(3))}");
            }
        }
    }

    static void ProcessElement(JsonElement el, string sourceFile)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            // Check if this is a typed object
            if (el.TryGetProperty("objectType", out _))
            {
                ExtractObject(el, sourceFile);
            }
            else
            {
                // Recurse into all properties
                foreach (var prop in el.EnumerateObject())
                {
                    ProcessElement(prop.Value, sourceFile);
                }
            }
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
                ProcessElement(item, sourceFile);
        }
    }

    static void ExtractObject(JsonElement obj, string sourceFile)
    {
        var objectType = obj.GetProperty("objectType").GetString() ?? "Unknown";

        if (!_types.TryGetValue(objectType, out var props))
        {
            props = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            _types[objectType] = props;
        }

        if (!_typeSourceFiles.TryGetValue(objectType, out var files))
        {
            files = new HashSet<string>();
            _typeSourceFiles[objectType] = files;
        }
        files.Add(sourceFile);

        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Name == "objectType") continue;

            if (!props.TryGetValue(prop.Name, out var info))
            {
                info = new PropertyInfo();
                props[prop.Name] = info;
            }

            info.RecordValue(prop.Value);

            // Recurse
            if (prop.Value.ValueKind == JsonValueKind.Object)
                ProcessElement(prop.Value, sourceFile);
            else if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                    ProcessElement(item, sourceFile);
            }
        }
    }

    static string FindTestData()
    {
        var dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 6; i++)
        {
            var td = Path.Combine(dir, "TestData");
            if (Directory.Exists(td)) return td;
            dir = Path.GetDirectoryName(dir) ?? dir;
        }
        return Path.Combine(Directory.GetCurrentDirectory(), "TestData");
    }

    class PropertyInfo
    {
        public HashSet<string> Samples { get; } = new();
        public HashSet<JsonValueKind> ValueKinds { get; } = new();
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
                if (ValueKinds.Count == 0) return "unknown";
                if (ValueKinds.Contains(JsonValueKind.Array)) return "array";
                if (ValueKinds.Contains(JsonValueKind.Object)) return "object";
                if (ValueKinds.Contains(JsonValueKind.String)) return "string";
                if (ValueKinds.Contains(JsonValueKind.True) || ValueKinds.Contains(JsonValueKind.False)) return "bool";
                if (ValueKinds.Contains(JsonValueKind.Number))
                {
                    // Check if any sample has a decimal point
                    if (Samples.Any(s => s.Contains('.'))) return "float";
                    return "int";
                }
                return "mixed";
            }
        }

        static string Truncate(string s, int max) => s.Length > max ? s[..max] + "..." : s;
    }
}
