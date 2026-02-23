using System.Text;
using System.Text.RegularExpressions;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Analyzes binary data to identify field patterns and structures.
/// </summary>
public sealed class BinaryFieldAnalyzer
{
    private const double InternalUnits = 10000.0;
    private const double MilsPerMm = 39.37007874;

    /// <summary>
    /// Analyze binary data and return identified fields.
    /// </summary>
    public BinaryAnalysisResult Analyze(byte[] data, string? context = null)
    {
        var result = new BinaryAnalysisResult
        {
            TotalBytes = data.Length,
            Context = context
        };

        if (data.Length == 0)
            return result;

        // Try different analysis strategies
        AnalyzeSizePrefixedBlocks(data, result);
        AnalyzeFixedFields(data, result);
        DetectStringPatterns(data, result);
        DetectCoordinatePatterns(data, result);
        GenerateHexDump(data, result);

        return result;
    }

    /// <summary>
    /// Analyze data that appears to be size-prefixed blocks (common in Altium).
    /// </summary>
    private void AnalyzeSizePrefixedBlocks(byte[] data, BinaryAnalysisResult result)
    {
        if (data.Length < 4) return;

        int offset = 0;
        var blocks = new List<AnalyzedBlock>();

        while (offset + 4 <= data.Length)
        {
            // Read potential size (Altium uses lower 3 bytes, upper byte is flags)
            int rawSize = BitConverter.ToInt32(data, offset);
            int size = rawSize & 0x00FFFFFF;
            int flags = (rawSize >> 24) & 0xFF;

            // Check if this looks like a valid size-prefixed block
            if (size > 0 && size < 0x100000 && offset + 4 + size <= data.Length)
            {
                var blockData = new byte[size];
                Array.Copy(data, offset + 4, blockData, 0, size);

                var block = new AnalyzedBlock
                {
                    Offset = offset,
                    SizeFieldValue = rawSize,
                    ContentSize = size,
                    Flags = flags,
                    BlockType = IdentifyBlockType(blockData)
                };

                // Try to interpret the content
                if (block.BlockType == BlockType.Parameters)
                {
                    block.Parameters = TryParseParameters(blockData);
                }
                else if (block.BlockType == BlockType.Text)
                {
                    block.TextContent = Encoding.GetEncoding(1252).GetString(blockData).TrimEnd('\0');
                }

                blocks.Add(block);
                offset += 4 + size;
            }
            else
            {
                break;
            }
        }

        if (blocks.Count > 0)
        {
            result.Blocks = blocks;
            result.RemainingBytes = data.Length - offset;
        }
    }

    /// <summary>
    /// Analyze fixed-size fields at known offsets.
    /// </summary>
    private void AnalyzeFixedFields(byte[] data, BinaryAnalysisResult result)
    {
        var fields = new List<AnalyzedField>();
        int offset = 0;

        // If we have blocks, analyze the remaining data after blocks
        if (result.Blocks?.Count > 0)
        {
            offset = result.Blocks.Sum(b => 4 + b.ContentSize);
        }

        while (offset + 4 <= data.Length)
        {
            // Try to identify field at this offset
            var field = IdentifyFieldAt(data, offset);
            if (field != null)
            {
                fields.Add(field);
                offset += field.Size;
            }
            else
            {
                offset++;
            }
        }

        if (fields.Count > 0)
        {
            result.Fields = fields;
        }
    }

    private AnalyzedField? IdentifyFieldAt(byte[] data, int offset)
    {
        if (offset + 4 > data.Length) return null;

        int int32Value = BitConverter.ToInt32(data, offset);

        // Check if it looks like a coordinate (reasonable range for PCB/schematic)
        double mils = int32Value / InternalUnits;
        if (Math.Abs(mils) < 100000 && mils != 0 && int32Value != 0)
        {
            // Check if next 4 bytes also look like a coordinate (coordinate pair)
            if (offset + 8 <= data.Length)
            {
                int int32Value2 = BitConverter.ToInt32(data, offset + 4);
                double mils2 = int32Value2 / InternalUnits;

                if (Math.Abs(mils2) < 100000)
                {
                    return new AnalyzedField
                    {
                        Offset = offset,
                        Size = 8,
                        Type = FieldType.CoordPoint,
                        RawValue = $"({int32Value}, {int32Value2})",
                        InterpretedValue = $"({mils:F2}mil, {mils2:F2}mil)",
                        Confidence = CalculateCoordConfidence(mils, mils2)
                    };
                }
            }

            return new AnalyzedField
            {
                Offset = offset,
                Size = 4,
                Type = FieldType.Coord,
                RawValue = int32Value.ToString(),
                InterpretedValue = $"{mils:F2}mil ({mils / MilsPerMm:F4}mm)",
                Confidence = CalculateCoordConfidence(mils)
            };
        }

        // Check for small integers (likely enum or count)
        if (int32Value >= 0 && int32Value < 1000)
        {
            return new AnalyzedField
            {
                Offset = offset,
                Size = 4,
                Type = FieldType.Int32,
                RawValue = int32Value.ToString(),
                InterpretedValue = int32Value.ToString(),
                Confidence = 0.3
            };
        }

        // Check for color value (0xBBGGRR format)
        if ((int32Value & 0xFF000000) == 0 && int32Value > 0)
        {
            int r = int32Value & 0xFF;
            int g = (int32Value >> 8) & 0xFF;
            int b = (int32Value >> 16) & 0xFF;

            return new AnalyzedField
            {
                Offset = offset,
                Size = 4,
                Type = FieldType.Color,
                RawValue = $"0x{int32Value:X6}",
                InterpretedValue = $"RGB({r}, {g}, {b}) #{r:X2}{g:X2}{b:X2}",
                Confidence = 0.4
            };
        }

        return null;
    }

    private double CalculateCoordConfidence(double mils, double mils2 = 0)
    {
        // Higher confidence for "round" values (common in designs)
        double confidence = 0.5;

        if (mils % 1 == 0) confidence += 0.1; // Whole mils
        if (mils % 5 == 0) confidence += 0.1; // Multiple of 5 mils
        if (mils % 10 == 0) confidence += 0.1; // Multiple of 10 mils
        if (mils % 100 == 0) confidence += 0.1; // Multiple of 100 mils

        if (mils2 != 0)
        {
            if (mils2 % 1 == 0) confidence += 0.05;
            if (mils2 % 10 == 0) confidence += 0.05;
        }

        return Math.Min(confidence, 1.0);
    }

    /// <summary>
    /// Detect string patterns in binary data.
    /// </summary>
    private void DetectStringPatterns(byte[] data, BinaryAnalysisResult result)
    {
        var strings = new List<DetectedString>();

        // Look for Pascal-style strings (length-prefixed)
        for (int i = 0; i < data.Length - 4; i++)
        {
            int len = BitConverter.ToInt32(data, i) & 0x00FFFFFF;
            if (len > 0 && len < 1000 && i + 4 + len <= data.Length)
            {
                var bytes = new byte[len];
                Array.Copy(data, i + 4, bytes, 0, len);

                if (IsPrintableString(bytes))
                {
                    var str = Encoding.GetEncoding(1252).GetString(bytes).TrimEnd('\0');
                    if (str.Length >= 3)
                    {
                        strings.Add(new DetectedString
                        {
                            Offset = i,
                            Length = len,
                            Value = str,
                            Type = StringType.PascalString
                        });
                        i += 3 + len; // Skip past this string
                    }
                }
            }
        }

        // Look for null-terminated strings
        int start = -1;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] >= 0x20 && data[i] < 0x7F)
            {
                if (start == -1) start = i;
            }
            else if (data[i] == 0 && start != -1)
            {
                int len = i - start;
                if (len >= 4)
                {
                    var str = Encoding.ASCII.GetString(data, start, len);
                    // Only add if not already found as Pascal string
                    if (!strings.Any(s => s.Offset <= start && s.Offset + s.Length >= i))
                    {
                        strings.Add(new DetectedString
                        {
                            Offset = start,
                            Length = len,
                            Value = str,
                            Type = StringType.NullTerminated
                        });
                    }
                }
                start = -1;
            }
            else
            {
                start = -1;
            }
        }

        if (strings.Count > 0)
        {
            result.Strings = strings.OrderBy(s => s.Offset).ToList();
        }
    }

    /// <summary>
    /// Detect coordinate patterns (sequences that look like X,Y pairs).
    /// </summary>
    private void DetectCoordinatePatterns(byte[] data, BinaryAnalysisResult result)
    {
        var coordPairs = new List<DetectedCoordPair>();

        for (int i = 0; i <= data.Length - 8; i += 4)
        {
            int x = BitConverter.ToInt32(data, i);
            int y = BitConverter.ToInt32(data, i + 4);

            double xMils = x / InternalUnits;
            double yMils = y / InternalUnits;

            // Check if both look like reasonable coordinates
            if (Math.Abs(xMils) < 50000 && Math.Abs(yMils) < 50000 &&
                (x != 0 || y != 0))
            {
                // Check for "round" values indicating likely coordinates
                bool xRound = xMils % 1 == 0 || xMils % 0.5 == 0;
                bool yRound = yMils % 1 == 0 || yMils % 0.5 == 0;

                if (xRound && yRound)
                {
                    coordPairs.Add(new DetectedCoordPair
                    {
                        Offset = i,
                        XRaw = x,
                        YRaw = y,
                        XMils = xMils,
                        YMils = yMils
                    });
                }
            }
        }

        if (coordPairs.Count > 0)
        {
            result.CoordinatePairs = coordPairs;
        }
    }

    /// <summary>
    /// Generate annotated hex dump.
    /// </summary>
    private void GenerateHexDump(byte[] data, BinaryAnalysisResult result)
    {
        var lines = new List<HexDumpLine>();
        const int bytesPerLine = 16;

        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            int count = Math.Min(bytesPerLine, data.Length - i);
            var lineBytes = new byte[count];
            Array.Copy(data, i, lineBytes, 0, count);

            var line = new HexDumpLine
            {
                Offset = i,
                Bytes = lineBytes,
                Hex = BitConverter.ToString(lineBytes).Replace("-", " "),
                Ascii = GetAsciiRepresentation(lineBytes)
            };

            // Add annotations for known fields
            var annotations = new List<string>();

            if (result.Fields != null)
            {
                foreach (var field in result.Fields.Where(f => f.Offset >= i && f.Offset < i + bytesPerLine))
                {
                    annotations.Add($"@{field.Offset - i}: {field.Type} = {field.InterpretedValue}");
                }
            }

            if (result.Strings != null)
            {
                foreach (var str in result.Strings.Where(s => s.Offset >= i && s.Offset < i + bytesPerLine))
                {
                    annotations.Add($"@{str.Offset - i}: \"{str.Value}\"");
                }
            }

            if (annotations.Count > 0)
            {
                line.Annotations = annotations;
            }

            lines.Add(line);
        }

        result.HexDump = lines;
    }

    private BlockType IdentifyBlockType(byte[] data)
    {
        if (data.Length == 0) return BlockType.Empty;

        // Check if it looks like parameters (contains | and =)
        try
        {
            var text = Encoding.GetEncoding(1252).GetString(data);
            if (text.Contains('|') && text.Contains('='))
                return BlockType.Parameters;
            if (IsPrintableString(data))
                return BlockType.Text;
        }
        catch { }

        return BlockType.Binary;
    }

    private Dictionary<string, string>? TryParseParameters(byte[] data)
    {
        try
        {
            var text = Encoding.GetEncoding(1252).GetString(data);
            if (!text.Contains('|') || !text.Contains('='))
                return null;

            var result = new Dictionary<string, string>();
            var entries = text.Split('|', StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                var parts = entry.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].TrimEnd('\0', '\r', '\n');
                    var value = parts[1].TrimEnd('\0', '\r', '\n');

                    if (key.StartsWith("%UTF8%", StringComparison.OrdinalIgnoreCase))
                    {
                        key = key[6..];
                    }

                    if (!result.ContainsKey(key))
                    {
                        result[key] = value;
                    }
                }
            }

            return result.Count > 0 ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPrintableString(byte[] data)
    {
        if (data.Length == 0) return false;

        int printable = 0;
        foreach (byte b in data)
        {
            if ((b >= 0x20 && b < 0x7F) || b == '\r' || b == '\n' || b == '\t' || b == 0)
                printable++;
        }

        return printable > data.Length * 0.8;
    }

    private static string GetAsciiRepresentation(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (byte b in data)
        {
            sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
        }
        return sb.ToString();
    }
}

#region Result Types

public sealed class BinaryAnalysisResult
{
    public int TotalBytes { get; init; }
    public string? Context { get; init; }
    public List<AnalyzedBlock>? Blocks { get; set; }
    public List<AnalyzedField>? Fields { get; set; }
    public List<DetectedString>? Strings { get; set; }
    public List<DetectedCoordPair>? CoordinatePairs { get; set; }
    public List<HexDumpLine>? HexDump { get; set; }
    public int RemainingBytes { get; set; }
}

public sealed class AnalyzedBlock
{
    public int Offset { get; init; }
    public int SizeFieldValue { get; init; }
    public int ContentSize { get; init; }
    public int Flags { get; init; }
    public BlockType BlockType { get; init; }
    public Dictionary<string, string>? Parameters { get; set; }
    public string? TextContent { get; set; }
}

public enum BlockType
{
    Empty,
    Parameters,
    Text,
    Binary
}

public sealed class AnalyzedField
{
    public int Offset { get; init; }
    public int Size { get; init; }
    public FieldType Type { get; init; }
    public string RawValue { get; init; } = "";
    public string InterpretedValue { get; init; } = "";
    public double Confidence { get; init; }
    public string? FieldName { get; set; }
}

public enum FieldType
{
    Unknown,
    Int32,
    UInt32,
    Int16,
    UInt16,
    Byte,
    Coord,
    CoordPoint,
    Color,
    Float,
    Double,
    Guid
}

public sealed class DetectedString
{
    public int Offset { get; init; }
    public int Length { get; init; }
    public string Value { get; init; } = "";
    public StringType Type { get; init; }
}

public enum StringType
{
    NullTerminated,
    PascalString,
    FixedLength
}

public sealed class DetectedCoordPair
{
    public int Offset { get; init; }
    public int XRaw { get; init; }
    public int YRaw { get; init; }
    public double XMils { get; init; }
    public double YMils { get; init; }
}

public sealed class HexDumpLine
{
    public int Offset { get; init; }
    public byte[] Bytes { get; init; } = [];
    public string Hex { get; init; } = "";
    public string Ascii { get; init; } = "";
    public List<string>? Annotations { get; set; }
}

#endregion
