using System.Text.Json.Serialization;

namespace OriginalCircuit.AltiumSharp.Export.Models;

/// <summary>
/// Represents the raw structure of an MCDF (COM/OLE Structured Storage) compound file.
/// </summary>
public sealed class McdfStructure
{
    /// <summary>
    /// Root storage of the compound file.
    /// </summary>
    public McdfStorage RootStorage { get; init; } = new();
}

/// <summary>
/// A storage (directory) within the compound file.
/// </summary>
public sealed class McdfStorage
{
    /// <summary>
    /// Name of this storage.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Child storages within this storage.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<McdfStorage> Storages { get; init; } = [];

    /// <summary>
    /// Streams (files) within this storage.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<McdfStream> Streams { get; init; } = [];
}

/// <summary>
/// A stream (file) within the compound file.
/// </summary>
public sealed class McdfStream
{
    /// <summary>
    /// Name of this stream.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Size of the stream in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Interpreted content of the stream.
    /// </summary>
    public McdfStreamContent Content { get; init; } = new();
}

/// <summary>
/// Interpreted content of an MCDF stream.
/// </summary>
public sealed class McdfStreamContent
{
    /// <summary>
    /// How the stream was interpreted: "Parameters", "Binary", "Text", "Mixed", "Records".
    /// </summary>
    public string InterpretedAs { get; init; } = "Binary";

    /// <summary>
    /// For parameter-based streams: key-value pairs.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Parameters { get; init; }

    /// <summary>
    /// For binary streams: base64 encoded data.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RawDataBase64 { get; init; }

    /// <summary>
    /// For text streams: decoded text content.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// For streams with identifiable binary fields.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<BinaryFieldInfo>? BinaryFields { get; init; }

    /// <summary>
    /// For record-based streams: list of records.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McdfRecord>? Records { get; init; }

    /// <summary>
    /// Any parsing notes or warnings for this stream.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Notes { get; init; }
}

/// <summary>
/// Information about a binary field within a stream.
/// </summary>
public sealed class BinaryFieldInfo
{
    /// <summary>
    /// Byte offset within the stream.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Size of the field in bytes.
    /// </summary>
    public int Size { get; init; }

    /// <summary>
    /// Inferred type: "int32", "uint32", "double", "coord", "string", "unknown".
    /// </summary>
    public string Type { get; init; } = "unknown";

    /// <summary>
    /// Raw value (as appropriate for the type).
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Human-readable representation (e.g., "100mil" for coords).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ValueHumanReadable { get; init; }

    /// <summary>
    /// Known field name if identified.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FieldName { get; init; }
}

/// <summary>
/// A record within a record-based stream.
/// </summary>
public sealed class McdfRecord
{
    /// <summary>
    /// Index of the record in the stream.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Byte offset of the record.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Size of the record in bytes.
    /// </summary>
    public int Size { get; init; }

    /// <summary>
    /// Record type identifier (if available).
    /// </summary>
    public int? RecordType { get; init; }

    /// <summary>
    /// Parsed parameters from the record.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Parameters { get; init; }

    /// <summary>
    /// Binary fields within the record.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<BinaryFieldInfo>? BinaryFields { get; init; }

    /// <summary>
    /// Raw data (base64) if not fully parsed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RawDataBase64 { get; init; }
}
