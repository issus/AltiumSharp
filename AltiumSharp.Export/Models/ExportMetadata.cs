namespace OriginalCircuit.AltiumSharp.Export.Models;

/// <summary>
/// Metadata about an export operation.
/// </summary>
public sealed record ExportMetadata
{
    /// <summary>
    /// Schema version for the export format.
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>
    /// Version of the exporter tool.
    /// </summary>
    public string ExporterVersion { get; init; } = "1.0.0";

    /// <summary>
    /// Timestamp when the export was performed.
    /// </summary>
    public DateTime ExportTimestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Original source file name.
    /// </summary>
    public string SourceFileName { get; init; } = string.Empty;

    /// <summary>
    /// Size of the source file in bytes.
    /// </summary>
    public long SourceFileSize { get; init; }

    /// <summary>
    /// SHA256 hash of the source file for identification.
    /// </summary>
    public string SourceFileHash { get; init; } = string.Empty;

    /// <summary>
    /// Detected Altium Designer version from file metadata.
    /// </summary>
    public string? DetectedAltiumVersion { get; init; }

    /// <summary>
    /// File format version string from the file header.
    /// </summary>
    public string? FileVersionString { get; init; }

    /// <summary>
    /// Export options that were used.
    /// </summary>
    public ExportOptions Options { get; init; } = new();

    /// <summary>
    /// Non-fatal warnings encountered during export.
    /// </summary>
    public List<string> Warnings { get; init; } = [];

    /// <summary>
    /// Errors encountered during export.
    /// </summary>
    public List<string> Errors { get; init; } = [];
}

/// <summary>
/// Options for controlling export behavior.
/// </summary>
public sealed record ExportOptions
{
    /// <summary>
    /// Include raw binary data (base64 encoded) for each primitive.
    /// </summary>
    public bool IncludeRawData { get; init; } = true;

    /// <summary>
    /// Include the raw MCDF compound file structure.
    /// </summary>
    public bool IncludeRawMcdfStructure { get; init; } = true;

    /// <summary>
    /// Include the parsed data model.
    /// </summary>
    public bool IncludeParsedModel { get; init; } = true;

    /// <summary>
    /// Pretty print JSON output with indentation.
    /// </summary>
    public bool PrettyPrint { get; init; } = true;

    /// <summary>
    /// Include unknown/unrecognized fields in the output.
    /// </summary>
    public bool IncludeUnknownFields { get; init; } = true;
}
