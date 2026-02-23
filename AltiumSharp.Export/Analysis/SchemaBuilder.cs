using System.Text.Json;
using System.Text.Json.Nodes;
using OriginalCircuit.AltiumSharp.Export.Models;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Builds a comprehensive schema from multiple Altium file exports,
/// identifying all fields, their types, and value patterns.
/// </summary>
public sealed class SchemaBuilder
{
    private readonly Dictionary<string, StorageSchema> _storageSchemas = [];
    private readonly Dictionary<string, StreamSchema> _streamSchemas = [];
    private readonly Dictionary<string, RecordSchema> _recordSchemas = [];
    private readonly List<string> _analyzedFiles = [];

    /// <summary>
    /// Add an exported JSON file to the schema analysis.
    /// </summary>
    public void AddExport(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var doc = JsonNode.Parse(json);
        if (doc == null) return;

        var fileName = Path.GetFileName(filePath);
        _analyzedFiles.Add(fileName);

        // Analyze raw MCDF structure
        var rawMcdf = doc["rawMcdf"];
        if (rawMcdf != null)
        {
            AnalyzeMcdfStructure(rawMcdf, "");
        }

        // Analyze parsed model
        var parsedModel = doc["parsedModel"];
        if (parsedModel != null)
        {
            AnalyzeParsedModel(parsedModel);
        }
    }

    /// <summary>
    /// Add an ExportResult directly to the schema analysis.
    /// </summary>
    public void AddExport(ExportResult export, string fileName)
    {
        _analyzedFiles.Add(fileName);

        if (export.RawMcdf != null)
        {
            AnalyzeMcdfStructure(export.RawMcdf);
        }

        if (export.ParsedModel != null)
        {
            AnalyzeParsedModel(export.ParsedModel);
        }
    }

    /// <summary>
    /// Build and return the combined schema.
    /// </summary>
    public FileFormatSchema Build()
    {
        return new FileFormatSchema
        {
            AnalyzedFiles = _analyzedFiles.ToList(),
            FileCount = _analyzedFiles.Count,
            GeneratedAt = DateTime.UtcNow,
            Storages = _storageSchemas.Values.OrderBy(s => s.Path).ToList(),
            Streams = _streamSchemas.Values.OrderBy(s => s.Path).ToList(),
            RecordTypes = _recordSchemas.Values.OrderBy(r => r.RecordType).ToList()
        };
    }

    private void AnalyzeMcdfStructure(JsonNode node, string path)
    {
        if (node == null) return;

        var rootStorage = node["rootStorage"];
        if (rootStorage != null)
        {
            AnalyzeStorage(rootStorage, "");
        }
    }

    private void AnalyzeMcdfStructure(McdfStructure mcdf)
    {
        if (mcdf.RootStorage != null)
        {
            AnalyzeStorage(mcdf.RootStorage, "");
        }
    }

    private void AnalyzeStorage(JsonNode storage, string parentPath)
    {
        var name = storage["name"]?.GetValue<string>() ?? "Root";
        var path = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";

        // Track this storage
        if (!_storageSchemas.TryGetValue(path, out var schema))
        {
            schema = new StorageSchema { Name = name, Path = path };
            _storageSchemas[path] = schema;
        }
        schema.Occurrences++;

        // Analyze child storages
        var storages = storage["storages"]?.AsArray();
        if (storages != null)
        {
            foreach (var childStorage in storages)
            {
                if (childStorage != null)
                {
                    var childName = childStorage["name"]?.GetValue<string>();
                    if (childName != null && !schema.ChildStorages.Contains(childName))
                    {
                        schema.ChildStorages.Add(childName);
                    }
                    AnalyzeStorage(childStorage, path);
                }
            }
        }

        // Analyze streams
        var streams = storage["streams"]?.AsArray();
        if (streams != null)
        {
            foreach (var stream in streams)
            {
                if (stream != null)
                {
                    var streamName = stream["name"]?.GetValue<string>();
                    if (streamName != null && !schema.Streams.Contains(streamName))
                    {
                        schema.Streams.Add(streamName);
                    }
                    AnalyzeStream(stream, path);
                }
            }
        }
    }

    private void AnalyzeStorage(McdfStorage storage, string parentPath)
    {
        var path = string.IsNullOrEmpty(parentPath) ? storage.Name : $"{parentPath}/{storage.Name}";

        if (!_storageSchemas.TryGetValue(path, out var schema))
        {
            schema = new StorageSchema { Name = storage.Name, Path = path };
            _storageSchemas[path] = schema;
        }
        schema.Occurrences++;

        foreach (var childStorage in storage.Storages)
        {
            if (!schema.ChildStorages.Contains(childStorage.Name))
            {
                schema.ChildStorages.Add(childStorage.Name);
            }
            AnalyzeStorage(childStorage, path);
        }

        foreach (var stream in storage.Streams)
        {
            if (!schema.Streams.Contains(stream.Name))
            {
                schema.Streams.Add(stream.Name);
            }
            AnalyzeStream(stream, path);
        }
    }

    private void AnalyzeStream(JsonNode stream, string parentPath)
    {
        var name = stream["name"]?.GetValue<string>() ?? "Unknown";
        var path = $"{parentPath}/{name}";

        if (!_streamSchemas.TryGetValue(path, out var schema))
        {
            schema = new StreamSchema { Name = name, Path = path };
            _streamSchemas[path] = schema;
        }
        schema.Occurrences++;

        var size = stream["size"]?.GetValue<int>() ?? 0;
        schema.MinSize = Math.Min(schema.MinSize == 0 ? int.MaxValue : schema.MinSize, size);
        schema.MaxSize = Math.Max(schema.MaxSize, size);

        // Analyze content
        var content = stream["content"];
        if (content != null)
        {
            var interpretedAs = content["interpretedAs"]?.GetValue<string>();
            if (interpretedAs != null)
            {
                schema.ContentTypes.Add(interpretedAs);
            }

            var parameters = content["parameters"];
            if (parameters != null)
            {
                AnalyzeParameters(parameters, schema.ParameterSchema, path);
            }
        }
    }

    private void AnalyzeStream(McdfStream stream, string parentPath)
    {
        var path = $"{parentPath}/{stream.Name}";

        if (!_streamSchemas.TryGetValue(path, out var schema))
        {
            schema = new StreamSchema { Name = stream.Name, Path = path };
            _streamSchemas[path] = schema;
        }
        schema.Occurrences++;

        schema.MinSize = (int)Math.Min(schema.MinSize == 0 ? int.MaxValue : schema.MinSize, stream.Size);
        schema.MaxSize = (int)Math.Max(schema.MaxSize, stream.Size);

        if (stream.Content != null)
        {
            if (stream.Content.InterpretedAs != null)
            {
                schema.ContentTypes.Add(stream.Content.InterpretedAs);
            }

            if (stream.Content.Parameters != null)
            {
                AnalyzeParametersDictionary(stream.Content.Parameters, schema.ParameterSchema, path);
            }
        }
    }

    private void AnalyzeParameters(JsonNode parameters, Dictionary<string, FieldSchema> schemaDict, string context)
    {
        if (parameters is not JsonObject obj) return;

        foreach (var (key, value) in obj)
        {
            if (!schemaDict.TryGetValue(key, out var fieldSchema))
            {
                fieldSchema = new FieldSchema { Name = key };
                schemaDict[key] = fieldSchema;
            }

            fieldSchema.Occurrences++;

            if (value != null)
            {
                var strValue = value.GetValue<string>();
                var valueType = InferValueType(strValue);
                fieldSchema.ObservedTypes.Add(valueType);

                if (fieldSchema.SampleValues.Count < 5 && !fieldSchema.SampleValues.Contains(strValue))
                {
                    fieldSchema.SampleValues.Add(strValue);
                }
            }
        }
    }

    private void AnalyzeParametersDictionary(Dictionary<string, string> parameters, Dictionary<string, FieldSchema> schemaDict, string context)
    {
        foreach (var (key, value) in parameters)
        {
            if (!schemaDict.TryGetValue(key, out var fieldSchema))
            {
                fieldSchema = new FieldSchema { Name = key };
                schemaDict[key] = fieldSchema;
            }

            fieldSchema.Occurrences++;

            var valueType = InferValueType(value);
            fieldSchema.ObservedTypes.Add(valueType);

            if (fieldSchema.SampleValues.Count < 5 && !fieldSchema.SampleValues.Contains(value))
            {
                fieldSchema.SampleValues.Add(value);
            }
        }
    }

    private void AnalyzeParsedModel(JsonNode model)
    {
        var fileType = model["fileType"]?.GetValue<string>();

        // Analyze based on file type
        var pcbLib = model["pcbLib"];
        if (pcbLib != null)
        {
            AnalyzePcbLib(pcbLib);
        }

        var schLib = model["schLib"];
        if (schLib != null)
        {
            AnalyzeSchLib(schLib);
        }

        var schDoc = model["schDoc"];
        if (schDoc != null)
        {
            AnalyzeSchDoc(schDoc);
        }
    }

    private void AnalyzeParsedModel(ParsedModel model)
    {
        if (model.PcbLib != null)
        {
            AnalyzePcbLib(model.PcbLib);
        }

        if (model.SchLib != null)
        {
            AnalyzeSchLib(model.SchLib);
        }

        if (model.SchDoc != null)
        {
            AnalyzeSchDoc(model.SchDoc);
        }
    }

    private void AnalyzePcbLib(JsonNode pcbLib)
    {
        var components = pcbLib["components"]?.AsArray();
        if (components == null) return;

        foreach (var component in components)
        {
            if (component == null) continue;

            var primitives = component["primitives"]?.AsArray();
            if (primitives == null) continue;

            foreach (var primitive in primitives)
            {
                if (primitive == null) continue;
                AnalyzePrimitive(primitive, "PCB");
            }
        }
    }

    private void AnalyzePcbLib(ParsedPcbLib pcbLib)
    {
        foreach (var component in pcbLib.Components)
        {
            foreach (var primitive in component.Primitives)
            {
                AnalyzePrimitive(primitive, "PCB");
            }
        }
    }

    private void AnalyzeSchLib(JsonNode schLib)
    {
        var components = schLib["components"]?.AsArray();
        if (components == null) return;

        foreach (var component in components)
        {
            if (component == null) continue;

            var primitives = component["primitives"]?.AsArray();
            if (primitives == null) continue;

            foreach (var primitive in primitives)
            {
                if (primitive == null) continue;
                AnalyzePrimitive(primitive, "SCH");
            }
        }
    }

    private void AnalyzeSchLib(ParsedSchLib schLib)
    {
        foreach (var component in schLib.Components)
        {
            foreach (var primitive in component.Primitives)
            {
                AnalyzePrimitive(primitive, "SCH");
            }
        }
    }

    private void AnalyzeSchDoc(JsonNode schDoc)
    {
        var primitives = schDoc["primitives"]?.AsArray();
        if (primitives == null) return;

        foreach (var primitive in primitives)
        {
            if (primitive == null) continue;
            AnalyzePrimitive(primitive, "SCH");
        }
    }

    private void AnalyzeSchDoc(ParsedSchDoc schDoc)
    {
        foreach (var primitive in schDoc.Primitives)
        {
            AnalyzePrimitive(primitive, "SCH");
        }
    }

    private void AnalyzePrimitive(JsonNode primitive, string fileTypePrefix)
    {
        var objectType = primitive["objectType"]?.GetValue<string>() ?? "Unknown";
        var recordType = primitive["recordType"]?.GetValue<string>() ?? primitive["objectId"]?.ToString() ?? "";

        var schemaKey = $"{fileTypePrefix}:{objectType}";
        if (!string.IsNullOrEmpty(recordType))
        {
            schemaKey = $"{fileTypePrefix}:{objectType}[{recordType}]";
        }

        if (!_recordSchemas.TryGetValue(schemaKey, out var schema))
        {
            schema = new RecordSchema
            {
                RecordType = schemaKey,
                ObjectType = objectType,
                RecordId = recordType
            };
            _recordSchemas[schemaKey] = schema;
        }
        schema.Occurrences++;

        // Analyze properties
        var properties = primitive["properties"];
        if (properties is JsonObject propObj)
        {
            foreach (var (key, value) in propObj)
            {
                AnalyzePropertyValue(key, value, schema.PropertySchema);
            }
        }

        // Analyze children recursively
        var children = primitive["children"]?.AsArray();
        if (children != null)
        {
            foreach (var child in children)
            {
                if (child != null)
                {
                    AnalyzePrimitive(child, fileTypePrefix);
                }
            }
        }
    }

    private void AnalyzePrimitive(ParsedPrimitive primitive, string fileTypePrefix)
    {
        var recordId = primitive.RecordType?.ToString() ?? "";
        var schemaKey = $"{fileTypePrefix}:{primitive.ObjectType}";
        if (!string.IsNullOrEmpty(recordId))
        {
            schemaKey = $"{fileTypePrefix}:{primitive.ObjectType}[{recordId}]";
        }

        if (!_recordSchemas.TryGetValue(schemaKey, out var schema))
        {
            schema = new RecordSchema
            {
                RecordType = schemaKey,
                ObjectType = primitive.ObjectType,
                RecordId = recordId
            };
            _recordSchemas[schemaKey] = schema;
        }
        schema.Occurrences++;

        foreach (var (key, value) in primitive.Properties)
        {
            AnalyzePropertyValueObject(key, value, schema.PropertySchema);
        }

        foreach (var child in primitive.Children)
        {
            AnalyzePrimitive(child, fileTypePrefix);
        }
    }

    private void AnalyzePropertyValue(string key, JsonNode? value, Dictionary<string, FieldSchema> schemaDict)
    {
        if (!schemaDict.TryGetValue(key, out var fieldSchema))
        {
            fieldSchema = new FieldSchema { Name = key };
            schemaDict[key] = fieldSchema;
        }

        fieldSchema.Occurrences++;

        if (value != null)
        {
            var valueType = InferJsonValueType(value);
            fieldSchema.ObservedTypes.Add(valueType);

            // Store sample for simple values
            if (value is JsonValue jv && fieldSchema.SampleValues.Count < 5)
            {
                var strValue = jv.ToString();
                if (!fieldSchema.SampleValues.Contains(strValue))
                {
                    fieldSchema.SampleValues.Add(strValue);
                }
            }
        }
    }

    private void AnalyzePropertyValueObject(string key, object? value, Dictionary<string, FieldSchema> schemaDict)
    {
        if (!schemaDict.TryGetValue(key, out var fieldSchema))
        {
            fieldSchema = new FieldSchema { Name = key };
            schemaDict[key] = fieldSchema;
        }

        fieldSchema.Occurrences++;

        if (value != null)
        {
            var valueType = InferObjectValueType(value);
            fieldSchema.ObservedTypes.Add(valueType);

            if (fieldSchema.SampleValues.Count < 5)
            {
                var strValue = value.ToString() ?? "";
                if (!string.IsNullOrEmpty(strValue) && !fieldSchema.SampleValues.Contains(strValue))
                {
                    fieldSchema.SampleValues.Add(strValue);
                }
            }
        }
    }

    private static string InferValueType(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "empty";

        if (value.Equals("T", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("F", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            return "boolean";

        if (int.TryParse(value, out _))
            return "integer";

        if (double.TryParse(value, out _))
            return "number";

        return "string";
    }

    private static string InferJsonValueType(JsonNode node)
    {
        return node switch
        {
            JsonValue jv when jv.TryGetValue<bool>(out _) => "boolean",
            JsonValue jv when jv.TryGetValue<long>(out _) => "integer",
            JsonValue jv when jv.TryGetValue<double>(out _) => "number",
            JsonValue jv when jv.TryGetValue<string>(out _) => "string",
            JsonObject => "object",
            JsonArray => "array",
            _ => "unknown"
        };
    }

    private static string InferObjectValueType(object value)
    {
        return value switch
        {
            bool => "boolean",
            int or long or short or byte => "integer",
            float or double or decimal => "number",
            string => "string",
            IDictionary<string, object?> => "object",
            IEnumerable<object> => "array",
            _ => value.GetType().Name
        };
    }
}

#region Schema Types

public sealed class FileFormatSchema
{
    public List<string> AnalyzedFiles { get; init; } = [];
    public int FileCount { get; init; }
    public DateTime GeneratedAt { get; init; }
    public List<StorageSchema> Storages { get; init; } = [];
    public List<StreamSchema> Streams { get; init; } = [];
    public List<RecordSchema> RecordTypes { get; init; } = [];
}

public sealed class StorageSchema
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public int Occurrences { get; set; }
    public List<string> ChildStorages { get; init; } = [];
    public List<string> Streams { get; init; } = [];
}

public sealed class StreamSchema
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public int Occurrences { get; set; }
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
    public HashSet<string> ContentTypes { get; init; } = [];
    public Dictionary<string, FieldSchema> ParameterSchema { get; init; } = [];
}

public sealed class RecordSchema
{
    public string RecordType { get; init; } = "";
    public string ObjectType { get; init; } = "";
    public string RecordId { get; init; } = "";
    public int Occurrences { get; set; }
    public Dictionary<string, FieldSchema> PropertySchema { get; init; } = [];
}

public sealed class FieldSchema
{
    public string Name { get; init; } = "";
    public int Occurrences { get; set; }
    public HashSet<string> ObservedTypes { get; init; } = [];
    public List<string> SampleValues { get; set; } = [];

    public string InferredType => ObservedTypes.Count == 1
        ? ObservedTypes.First()
        : ObservedTypes.Count > 0
            ? $"union({string.Join("|", ObservedTypes)})"
            : "unknown";

    public bool IsRequired { get; set; } = true;
}

#endregion
