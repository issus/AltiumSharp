using System.Text.Json;
using System.Text.Json.Serialization;

namespace OriginalCircuit.AltiumSharp.Export.Models;

/// <summary>
/// Complete export result containing metadata, raw MCDF structure, and parsed model.
/// </summary>
public sealed class ExportResult
{
    /// <summary>
    /// Export metadata including source file info and warnings.
    /// </summary>
    public ExportMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Raw MCDF compound file structure (if IncludeRawMcdfStructure is enabled).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McdfStructure? RawMcdf { get; set; }

    /// <summary>
    /// Parsed data model (if IncludeParsedModel is enabled).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParsedModel? ParsedModel { get; set; }

    /// <summary>
    /// Serialize this result to JSON.
    /// </summary>
    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Serialize this result to a file.
    /// </summary>
    public async Task SaveToFileAsync(string path, bool indented = true, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, this, options, cancellationToken);
    }
}

/// <summary>
/// Parsed data model from an Altium file.
/// </summary>
public sealed class ParsedModel
{
    /// <summary>
    /// Type of file: "PcbLib", "SchLib", "SchDoc", "PcbDoc".
    /// </summary>
    public string FileType { get; init; } = string.Empty;

    /// <summary>
    /// Parsed PCB library data (when FileType is "PcbLib").
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParsedPcbLib? PcbLib { get; set; }

    /// <summary>
    /// Parsed schematic library data (when FileType is "SchLib").
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParsedSchLib? SchLib { get; set; }

    /// <summary>
    /// Parsed schematic document data (when FileType is "SchDoc").
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParsedSchDoc? SchDoc { get; set; }

    /// <summary>
    /// Parsed PCB document data (when FileType is "PcbDoc").
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParsedPcbDoc? PcbDoc { get; set; }
}

/// <summary>
/// Parsed PCB library data.
/// </summary>
public sealed class ParsedPcbLib
{
    /// <summary>
    /// Library header information.
    /// </summary>
    public Dictionary<string, object?> Header { get; init; } = [];

    /// <summary>
    /// Unique identifier for the library.
    /// </summary>
    public string? UniqueId { get; init; }

    /// <summary>
    /// Components (footprints) in the library.
    /// </summary>
    public List<ParsedPcbComponent> Components { get; init; } = [];
}

/// <summary>
/// Parsed PCB component (footprint).
/// </summary>
public sealed class ParsedPcbComponent
{
    /// <summary>
    /// Footprint pattern name.
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Component description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Component height.
    /// </summary>
    public CoordValue? Height { get; init; }

    /// <summary>
    /// Item GUID.
    /// </summary>
    public string? ItemGuid { get; init; }

    /// <summary>
    /// Revision GUID.
    /// </summary>
    public string? RevisionGuid { get; init; }

    /// <summary>
    /// All original parameters from the file (preserves unknown fields).
    /// </summary>
    public Dictionary<string, string> OriginalParameters { get; init; } = [];

    /// <summary>
    /// Primitives (pads, tracks, arcs, etc.) in this component.
    /// </summary>
    public List<ParsedPrimitive> Primitives { get; init; } = [];
}

/// <summary>
/// Parsed schematic library data.
/// </summary>
public sealed class ParsedSchLib
{
    /// <summary>
    /// Library header information.
    /// </summary>
    public Dictionary<string, object?> Header { get; init; } = [];

    /// <summary>
    /// Components (symbols) in the library.
    /// </summary>
    public List<ParsedSchComponent> Components { get; init; } = [];
}

/// <summary>
/// Parsed schematic component (symbol).
/// </summary>
public sealed class ParsedSchComponent
{
    /// <summary>
    /// Component name/lib ref.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Component description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Designator item ID.
    /// </summary>
    public string? DesignItemId { get; init; }

    /// <summary>
    /// Number of parts in this component.
    /// </summary>
    public int PartCount { get; init; } = 1;

    /// <summary>
    /// All original parameters from the file.
    /// </summary>
    public Dictionary<string, string> OriginalParameters { get; init; } = [];

    /// <summary>
    /// Primitives (pins, lines, shapes, etc.) in this component.
    /// </summary>
    public List<ParsedPrimitive> Primitives { get; init; } = [];
}

/// <summary>
/// Parsed schematic document data.
/// </summary>
public sealed class ParsedSchDoc
{
    /// <summary>
    /// Document header information.
    /// </summary>
    public Dictionary<string, object?> Header { get; init; } = [];

    /// <summary>
    /// All primitives in the document.
    /// </summary>
    public List<ParsedPrimitive> Primitives { get; init; } = [];
}

/// <summary>
/// Parsed PCB document data.
/// </summary>
public sealed class ParsedPcbDoc
{
    /// <summary>
    /// Components placed on the board.
    /// </summary>
    public List<ParsedPcbComponent> Components { get; init; } = [];

    /// <summary>
    /// All primitives on the board, grouped by type.
    /// </summary>
    public ParsedPcbDocPrimitives Primitives { get; init; } = new();
}

/// <summary>
/// PCB document primitives grouped by type.
/// </summary>
public sealed class ParsedPcbDocPrimitives
{
    public int PadCount { get; init; }
    public int ViaCount { get; init; }
    public int TrackCount { get; init; }
    public int ArcCount { get; init; }
    public int TextCount { get; init; }
    public int FillCount { get; init; }
    public int RegionCount { get; init; }
    public int ComponentBodyCount { get; init; }
    public List<ParsedPrimitive> Pads { get; init; } = [];
    public List<ParsedPrimitive> Vias { get; init; } = [];
    public List<ParsedPrimitive> Tracks { get; init; } = [];
    public List<ParsedPrimitive> Arcs { get; init; } = [];
    public List<ParsedPrimitive> Texts { get; init; } = [];
    public List<ParsedPrimitive> Fills { get; init; } = [];
    public List<ParsedPrimitive> Regions { get; init; } = [];
    public List<ParsedPrimitive> ComponentBodies { get; init; } = [];
}

/// <summary>
/// Generic parsed primitive that preserves all properties.
/// </summary>
public sealed record ParsedPrimitive
{
    /// <summary>
    /// Type of primitive (e.g., "Pad", "Track", "Pin", "Line").
    /// </summary>
    public string ObjectType { get; init; } = string.Empty;

    /// <summary>
    /// Numeric object ID (for PCB primitives).
    /// </summary>
    public int? ObjectId { get; init; }

    /// <summary>
    /// Record type number (for schematic primitives).
    /// </summary>
    public int? RecordType { get; init; }

    /// <summary>
    /// Layer name (for PCB primitives).
    /// </summary>
    public string? Layer { get; init; }

    /// <summary>
    /// Flags value.
    /// </summary>
    public int? Flags { get; init; }

    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string? UniqueId { get; init; }

    /// <summary>
    /// Owner index (for hierarchical primitives).
    /// </summary>
    public int? OwnerIndex { get; init; }

    /// <summary>
    /// All properties extracted via reflection.
    /// </summary>
    public Dictionary<string, object?> Properties { get; init; } = [];

    /// <summary>
    /// Raw binary data (base64 encoded) if available.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RawDataBase64 { get; init; }

    /// <summary>
    /// Child primitives (for container types).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<ParsedPrimitive> Children { get; init; } = [];
}

/// <summary>
/// Coordinate value with both internal and human-readable representations.
/// </summary>
public readonly struct CoordValue
{
    /// <summary>
    /// Internal coordinate value (fixed-point integer).
    /// </summary>
    public int Internal { get; init; }

    /// <summary>
    /// Value in mils (thousandths of an inch).
    /// </summary>
    public double Mils { get; init; }

    /// <summary>
    /// Value in millimeters.
    /// </summary>
    public double Mm { get; init; }

    /// <summary>
    /// Create from an AltiumSharp Coord value.
    /// </summary>
    public static CoordValue FromCoord(int coord)
    {
        // Internal units: 10000 per mil
        const double InternalUnits = 10000.0;
        const double MilsPerMm = 39.37007874;

        double mils = coord / InternalUnits;
        return new CoordValue
        {
            Internal = coord,
            Mils = mils,
            Mm = mils / MilsPerMm
        };
    }
}

/// <summary>
/// Coordinate point with both internal and human-readable representations.
/// </summary>
public sealed class CoordPointValue
{
    /// <summary>
    /// X coordinate.
    /// </summary>
    public CoordValue X { get; init; }

    /// <summary>
    /// Y coordinate.
    /// </summary>
    public CoordValue Y { get; init; }
}
