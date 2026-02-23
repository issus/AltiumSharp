using System.Text;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Generates markdown documentation from schema analysis results.
/// </summary>
public sealed class DocumentationGenerator
{
    /// <summary>
    /// Generate comprehensive markdown documentation from a file format schema.
    /// </summary>
    public string GenerateMarkdown(FileFormatSchema schema, string title = "Altium File Format Documentation")
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"Generated: {schema.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Files analyzed: {schema.FileCount}");
        sb.AppendLine();

        // Table of Contents
        sb.AppendLine("## Table of Contents");
        sb.AppendLine();
        sb.AppendLine("1. [Storage Structure](#storage-structure)");
        sb.AppendLine("2. [Stream Details](#stream-details)");
        sb.AppendLine("3. [Record Types](#record-types)");
        sb.AppendLine();

        // Storage Structure
        sb.AppendLine("## Storage Structure");
        sb.AppendLine();
        sb.AppendLine("The file uses OLE Compound Document (MCDF) format with the following storage hierarchy:");
        sb.AppendLine();

        GenerateStorageTree(sb, schema.Storages);

        // Stream Details
        sb.AppendLine("## Stream Details");
        sb.AppendLine();

        foreach (var stream in schema.Streams.OrderBy(s => s.Path))
        {
            GenerateStreamDoc(sb, stream);
        }

        // Record Types
        sb.AppendLine("## Record Types");
        sb.AppendLine();

        // Group by prefix (PCB vs SCH)
        var pcbRecords = schema.RecordTypes.Where(r => r.RecordType.StartsWith("PCB:")).ToList();
        var schRecords = schema.RecordTypes.Where(r => r.RecordType.StartsWith("SCH:")).ToList();

        if (pcbRecords.Count > 0)
        {
            sb.AppendLine("### PCB Records");
            sb.AppendLine();
            foreach (var record in pcbRecords.OrderBy(r => r.ObjectType))
            {
                GenerateRecordDoc(sb, record);
            }
        }

        if (schRecords.Count > 0)
        {
            sb.AppendLine("### Schematic Records");
            sb.AppendLine();
            foreach (var record in schRecords.OrderBy(r => r.ObjectType))
            {
                GenerateRecordDoc(sb, record);
            }
        }

        // Source files
        sb.AppendLine("## Analyzed Files");
        sb.AppendLine();
        foreach (var file in schema.AnalyzedFiles)
        {
            sb.AppendLine($"- {file}");
        }
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Generate documentation for unknown fields analysis.
    /// </summary>
    public string GenerateUnknownFieldsDoc(UnknownFieldsSummary summary)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Unknown Fields Report");
        sb.AppendLine();
        sb.AppendLine($"Reports analyzed: {summary.TotalReportsAnalyzed}");
        sb.AppendLine($"Total parameters: {summary.TotalParametersAnalyzed}");
        sb.AppendLine($"Unknown fields found: {summary.TotalUnknownFields}");
        sb.AppendLine();

        if (summary.UnknownFields.Count == 0)
        {
            sb.AppendLine("No unknown fields detected.");
            return sb.ToString();
        }

        sb.AppendLine("## Unknown Fields by Frequency");
        sb.AppendLine();
        sb.AppendLine("| Field | Occurrences | Type | Possible Purpose | Sample Values |");
        sb.AppendLine("|-------|-------------|------|------------------|---------------|");

        foreach (var field in summary.UnknownFields.OrderByDescending(f => f.Occurrences).Take(50))
        {
            var samples = string.Join(", ", field.SampleValues.Take(3).Select(s =>
                s.Length > 20 ? s[..20] + "..." : s));
            var purpose = field.PossiblePurpose ?? "-";

            sb.AppendLine($"| `{field.Name}` | {field.Occurrences} | {field.InferredType} | {purpose} | {samples} |");
        }

        sb.AppendLine();

        // Detailed sections for each unknown field
        sb.AppendLine("## Field Details");
        sb.AppendLine();

        foreach (var field in summary.UnknownFields.OrderByDescending(f => f.Occurrences))
        {
            sb.AppendLine($"### {field.Name}");
            sb.AppendLine();
            sb.AppendLine($"- **Normalized**: `{field.NormalizedName}`");
            sb.AppendLine($"- **Occurrences**: {field.Occurrences}");
            sb.AppendLine($"- **Inferred Type**: {field.InferredType}");

            if (field.PossiblePurpose != null)
            {
                sb.AppendLine($"- **Possible Purpose**: {field.PossiblePurpose}");
            }

            sb.AppendLine($"- **Contexts**: {string.Join(", ", field.Contexts.Take(5))}");
            sb.AppendLine();
            sb.AppendLine("**Sample Values:**");
            foreach (var sample in field.SampleValues)
            {
                sb.AppendLine($"- `{sample}`");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate documentation for binary field analysis.
    /// </summary>
    public string GenerateBinaryAnalysisDoc(BinaryAnalysisResult analysis, string title = "Binary Data Analysis")
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"Total bytes: {analysis.TotalBytes}");

        if (analysis.Context != null)
        {
            sb.AppendLine($"Context: {analysis.Context}");
        }
        sb.AppendLine();

        // Blocks
        if (analysis.Blocks?.Count > 0)
        {
            sb.AppendLine("## Size-Prefixed Blocks");
            sb.AppendLine();
            sb.AppendLine("| Offset | Size | Flags | Type | Content Preview |");
            sb.AppendLine("|--------|------|-------|------|-----------------|");

            foreach (var block in analysis.Blocks)
            {
                var content = block.BlockType == BlockType.Parameters && block.Parameters != null
                    ? $"{block.Parameters.Count} params"
                    : block.TextContent != null
                        ? (block.TextContent.Length > 30 ? block.TextContent[..30] + "..." : block.TextContent)
                        : "-";

                sb.AppendLine($"| 0x{block.Offset:X4} | {block.ContentSize} | 0x{block.Flags:X2} | {block.BlockType} | {content} |");
            }
            sb.AppendLine();

            // Detail parameters
            foreach (var block in analysis.Blocks.Where(b => b.Parameters != null))
            {
                sb.AppendLine($"### Block at 0x{block.Offset:X4} Parameters");
                sb.AppendLine();
                foreach (var (key, value) in block.Parameters!)
                {
                    var displayValue = value.Length > 50 ? value[..50] + "..." : value;
                    sb.AppendLine($"- `{key}` = `{displayValue}`");
                }
                sb.AppendLine();
            }
        }

        // Detected fields
        if (analysis.Fields?.Count > 0)
        {
            sb.AppendLine("## Detected Fields");
            sb.AppendLine();
            sb.AppendLine("| Offset | Size | Type | Raw Value | Interpreted | Confidence |");
            sb.AppendLine("|--------|------|------|-----------|-------------|------------|");

            foreach (var field in analysis.Fields)
            {
                sb.AppendLine($"| 0x{field.Offset:X4} | {field.Size} | {field.Type} | {field.RawValue} | {field.InterpretedValue} | {field.Confidence:P0} |");
            }
            sb.AppendLine();
        }

        // Detected strings
        if (analysis.Strings?.Count > 0)
        {
            sb.AppendLine("## Detected Strings");
            sb.AppendLine();
            sb.AppendLine("| Offset | Length | Type | Value |");
            sb.AppendLine("|--------|--------|------|-------|");

            foreach (var str in analysis.Strings)
            {
                var displayValue = str.Value.Length > 50 ? str.Value[..50] + "..." : str.Value;
                sb.AppendLine($"| 0x{str.Offset:X4} | {str.Length} | {str.Type} | `{displayValue}` |");
            }
            sb.AppendLine();
        }

        // Coordinate pairs
        if (analysis.CoordinatePairs?.Count > 0)
        {
            sb.AppendLine("## Potential Coordinate Pairs");
            sb.AppendLine();
            sb.AppendLine("| Offset | X (raw) | Y (raw) | X (mils) | Y (mils) |");
            sb.AppendLine("|--------|---------|---------|----------|----------|");

            foreach (var coord in analysis.CoordinatePairs.Take(20))
            {
                sb.AppendLine($"| 0x{coord.Offset:X4} | {coord.XRaw} | {coord.YRaw} | {coord.XMils:F2} | {coord.YMils:F2} |");
            }

            if (analysis.CoordinatePairs.Count > 20)
            {
                sb.AppendLine($"| ... | ({analysis.CoordinatePairs.Count - 20} more) | | | |");
            }
            sb.AppendLine();
        }

        // Hex dump
        if (analysis.HexDump?.Count > 0)
        {
            sb.AppendLine("## Hex Dump");
            sb.AppendLine();
            sb.AppendLine("```");

            foreach (var line in analysis.HexDump)
            {
                var hex = line.Hex.PadRight(48);
                sb.AppendLine($"{line.Offset:X8}  {hex}  |{line.Ascii}|");

                if (line.Annotations?.Count > 0)
                {
                    foreach (var annotation in line.Annotations)
                    {
                        sb.AppendLine($"           ^ {annotation}");
                    }
                }
            }

            sb.AppendLine("```");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate a comparison report between two schemas.
    /// </summary>
    public string GenerateComparisonDoc(FileFormatSchema oldSchema, FileFormatSchema newSchema)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Schema Comparison Report");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Old | New |");
        sb.AppendLine("|--------|-----|-----|");
        sb.AppendLine($"| Files analyzed | {oldSchema.FileCount} | {newSchema.FileCount} |");
        sb.AppendLine($"| Storages | {oldSchema.Storages.Count} | {newSchema.Storages.Count} |");
        sb.AppendLine($"| Streams | {oldSchema.Streams.Count} | {newSchema.Streams.Count} |");
        sb.AppendLine($"| Record Types | {oldSchema.RecordTypes.Count} | {newSchema.RecordTypes.Count} |");
        sb.AppendLine();

        // New streams
        var oldStreamPaths = oldSchema.Streams.Select(s => s.Path).ToHashSet();
        var newStreamPaths = newSchema.Streams.Select(s => s.Path).ToHashSet();
        var addedStreams = newStreamPaths.Except(oldStreamPaths).ToList();
        var removedStreams = oldStreamPaths.Except(newStreamPaths).ToList();

        if (addedStreams.Count > 0)
        {
            sb.AppendLine("## New Streams");
            sb.AppendLine();
            foreach (var stream in addedStreams)
            {
                sb.AppendLine($"- `{stream}`");
            }
            sb.AppendLine();
        }

        if (removedStreams.Count > 0)
        {
            sb.AppendLine("## Removed Streams");
            sb.AppendLine();
            foreach (var stream in removedStreams)
            {
                sb.AppendLine($"- `{stream}`");
            }
            sb.AppendLine();
        }

        // New record types
        var oldRecordTypes = oldSchema.RecordTypes.Select(r => r.RecordType).ToHashSet();
        var newRecordTypes = newSchema.RecordTypes.Select(r => r.RecordType).ToHashSet();
        var addedRecords = newRecordTypes.Except(oldRecordTypes).ToList();
        var removedRecords = oldRecordTypes.Except(newRecordTypes).ToList();

        if (addedRecords.Count > 0)
        {
            sb.AppendLine("## New Record Types");
            sb.AppendLine();
            foreach (var record in addedRecords)
            {
                sb.AppendLine($"- `{record}`");
            }
            sb.AppendLine();
        }

        if (removedRecords.Count > 0)
        {
            sb.AppendLine("## Removed Record Types");
            sb.AppendLine();
            foreach (var record in removedRecords)
            {
                sb.AppendLine($"- `{record}`");
            }
            sb.AppendLine();
        }

        // New fields in existing record types
        sb.AppendLine("## New Fields in Existing Records");
        sb.AppendLine();

        var commonRecords = oldRecordTypes.Intersect(newRecordTypes);
        var hasNewFields = false;

        foreach (var recordType in commonRecords)
        {
            var oldRecord = oldSchema.RecordTypes.First(r => r.RecordType == recordType);
            var newRecord = newSchema.RecordTypes.First(r => r.RecordType == recordType);

            var oldFields = oldRecord.PropertySchema.Keys.ToHashSet();
            var newFields = newRecord.PropertySchema.Keys.Except(oldFields).ToList();

            if (newFields.Count > 0)
            {
                hasNewFields = true;
                sb.AppendLine($"### {recordType}");
                sb.AppendLine();
                foreach (var field in newFields)
                {
                    var schema = newRecord.PropertySchema[field];
                    sb.AppendLine($"- `{field}` ({schema.InferredType})");
                }
                sb.AppendLine();
            }
        }

        if (!hasNewFields)
        {
            sb.AppendLine("No new fields detected in common record types.");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void GenerateStorageTree(StringBuilder sb, List<StorageSchema> storages)
    {
        var rootStorages = storages.Where(s => !s.Path.Contains('/')).ToList();

        foreach (var storage in rootStorages)
        {
            GenerateStorageNode(sb, storage, storages, 0);
        }

        sb.AppendLine();
    }

    private void GenerateStorageNode(StringBuilder sb, StorageSchema storage, List<StorageSchema> allStorages, int depth)
    {
        var indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}- **{storage.Name}/** (seen {storage.Occurrences}x)");

        // List streams
        foreach (var stream in storage.Streams)
        {
            sb.AppendLine($"{indent}  - `{stream}`");
        }

        // List child storages recursively
        foreach (var childName in storage.ChildStorages)
        {
            var childPath = string.IsNullOrEmpty(storage.Path) || storage.Path == storage.Name
                ? $"{storage.Name}/{childName}"
                : $"{storage.Path}/{childName}";

            var childStorage = allStorages.FirstOrDefault(s => s.Path == childPath);
            if (childStorage != null)
            {
                GenerateStorageNode(sb, childStorage, allStorages, depth + 1);
            }
            else
            {
                sb.AppendLine($"{indent}  - **{childName}/**");
            }
        }
    }

    private void GenerateStreamDoc(StringBuilder sb, StreamSchema stream)
    {
        sb.AppendLine($"### {stream.Path}");
        sb.AppendLine();
        sb.AppendLine($"- **Occurrences**: {stream.Occurrences}");
        sb.AppendLine($"- **Size range**: {stream.MinSize} - {stream.MaxSize} bytes");

        if (stream.ContentTypes.Count > 0)
        {
            sb.AppendLine($"- **Content types**: {string.Join(", ", stream.ContentTypes)}");
        }

        if (stream.ParameterSchema.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Parameters:**");
            sb.AppendLine();
            sb.AppendLine("| Name | Type | Occurrences | Sample Values |");
            sb.AppendLine("|------|------|-------------|---------------|");

            foreach (var (name, field) in stream.ParameterSchema.OrderBy(p => p.Key))
            {
                var samples = string.Join(", ", field.SampleValues.Take(2).Select(s =>
                    s.Length > 15 ? s[..15] + "..." : s));
                sb.AppendLine($"| `{name}` | {field.InferredType} | {field.Occurrences} | {samples} |");
            }
        }

        sb.AppendLine();
    }

    private void GenerateRecordDoc(StringBuilder sb, RecordSchema record)
    {
        var displayName = record.ObjectType;
        if (!string.IsNullOrEmpty(record.RecordId))
        {
            displayName += $" (Record {record.RecordId})";
        }

        sb.AppendLine($"#### {displayName}");
        sb.AppendLine();
        sb.AppendLine($"- **Occurrences**: {record.Occurrences}");
        sb.AppendLine($"- **Properties**: {record.PropertySchema.Count}");
        sb.AppendLine();

        if (record.PropertySchema.Count > 0)
        {
            sb.AppendLine("| Property | Type | Occurrences | Sample Values |");
            sb.AppendLine("|----------|------|-------------|---------------|");

            foreach (var (name, field) in record.PropertySchema.OrderBy(p => p.Key))
            {
                var samples = string.Join(", ", field.SampleValues.Take(2).Select(s =>
                {
                    var str = s.ToString() ?? "";
                    return str.Length > 20 ? str[..20] + "..." : str;
                }));
                sb.AppendLine($"| `{name}` | {field.InferredType} | {field.Occurrences} | {samples} |");
            }
        }

        sb.AppendLine();
    }
}
