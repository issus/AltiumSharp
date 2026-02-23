using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Binary template system for defining and applying structure templates
/// to parse unknown binary data (similar to 010 Editor or Kaitai Struct).
/// </summary>
public sealed class BinaryTemplateEngine
{
    private readonly Dictionary<string, BinaryTemplate> _templates = [];
    private const double InternalUnits = 10000.0;

    /// <summary>
    /// Register a template.
    /// </summary>
    public void RegisterTemplate(BinaryTemplate template)
    {
        _templates[template.Name] = template;
    }

    /// <summary>
    /// Load templates from a JSON file.
    /// </summary>
    public void LoadTemplates(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var templates = JsonSerializer.Deserialize<List<BinaryTemplate>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });

        if (templates != null)
        {
            foreach (var template in templates)
            {
                _templates[template.Name] = template;
            }
        }
    }

    /// <summary>
    /// Save templates to a JSON file.
    /// </summary>
    public async Task SaveTemplatesAsync(string filePath)
    {
        var json = JsonSerializer.Serialize(_templates.Values.ToList(), new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        });
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Apply a template to binary data.
    /// </summary>
    public TemplateParseResult Apply(string templateName, byte[] data, int offset = 0)
    {
        if (!_templates.TryGetValue(templateName, out var template))
        {
            return new TemplateParseResult
            {
                Success = false,
                Error = $"Template not found: {templateName}"
            };
        }

        return ApplyTemplate(template, data, offset);
    }

    /// <summary>
    /// Apply a template directly.
    /// </summary>
    public TemplateParseResult ApplyTemplate(BinaryTemplate template, byte[] data, int offset = 0)
    {
        var result = new TemplateParseResult
        {
            TemplateName = template.Name,
            StartOffset = offset
        };

        try
        {
            var currentOffset = offset;

            foreach (var field in template.Fields)
            {
                var fieldResult = ParseField(field, data, currentOffset, result.Fields);
                if (!fieldResult.Success)
                {
                    result.Success = false;
                    result.Error = $"Failed to parse field '{field.Name}' at offset {currentOffset}: {fieldResult.Error}";
                    return result;
                }

                result.Fields.Add(fieldResult);
                currentOffset = fieldResult.EndOffset;
            }

            result.Success = true;
            result.EndOffset = currentOffset;
            result.TotalSize = currentOffset - offset;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Try to auto-detect which template best matches the data.
    /// </summary>
    public List<TemplateMatch> DetectTemplates(byte[] data, int offset = 0)
    {
        var matches = new List<TemplateMatch>();

        foreach (var template in _templates.Values)
        {
            var result = ApplyTemplate(template, data, offset);
            if (result.Success)
            {
                var confidence = CalculateConfidence(result, data.Length - offset);
                matches.Add(new TemplateMatch
                {
                    TemplateName = template.Name,
                    Confidence = confidence,
                    ParseResult = result
                });
            }
        }

        return matches.OrderByDescending(m => m.Confidence).ToList();
    }

    /// <summary>
    /// Get all registered templates.
    /// </summary>
    public IReadOnlyCollection<BinaryTemplate> GetTemplates() => _templates.Values;

    /// <summary>
    /// Create built-in Altium templates.
    /// </summary>
    public void RegisterAltiumTemplates()
    {
        // Size-prefixed block (common Altium pattern)
        RegisterTemplate(new BinaryTemplate
        {
            Name = "SizePrefixedBlock",
            Description = "Common Altium size-prefixed block with 4-byte header",
            Fields =
            [
                new TemplateField { Name = "Size", Type = FieldDataType.Int32, Description = "Block size (lower 24 bits) and flags (upper 8 bits)" },
                new TemplateField { Name = "Content", Type = FieldDataType.Bytes, SizeReference = "Size", SizeMask = 0x00FFFFFF, Description = "Block content" }
            ]
        });

        // Coordinate pair
        RegisterTemplate(new BinaryTemplate
        {
            Name = "CoordPair",
            Description = "X,Y coordinate pair in internal units",
            Fields =
            [
                new TemplateField { Name = "X", Type = FieldDataType.Coord, Description = "X coordinate" },
                new TemplateField { Name = "Y", Type = FieldDataType.Coord, Description = "Y coordinate" }
            ]
        });

        // Bounding box
        RegisterTemplate(new BinaryTemplate
        {
            Name = "BoundingBox",
            Description = "Rectangle defined by two corner coordinates",
            Fields =
            [
                new TemplateField { Name = "X1", Type = FieldDataType.Coord, Description = "Left X" },
                new TemplateField { Name = "Y1", Type = FieldDataType.Coord, Description = "Bottom Y" },
                new TemplateField { Name = "X2", Type = FieldDataType.Coord, Description = "Right X" },
                new TemplateField { Name = "Y2", Type = FieldDataType.Coord, Description = "Top Y" }
            ]
        });

        // Pascal string (length-prefixed)
        RegisterTemplate(new BinaryTemplate
        {
            Name = "PascalString",
            Description = "Length-prefixed string",
            Fields =
            [
                new TemplateField { Name = "Length", Type = FieldDataType.Int32, SizeMask = 0x00FFFFFF },
                new TemplateField { Name = "Text", Type = FieldDataType.String, SizeReference = "Length", Encoding = "windows-1252" }
            ]
        });

        // PCB Pad basic structure
        RegisterTemplate(new BinaryTemplate
        {
            Name = "PcbPadHeader",
            Description = "PCB Pad primitive header",
            Fields =
            [
                new TemplateField { Name = "RecordSize", Type = FieldDataType.Int32, SizeMask = 0x00FFFFFF },
                new TemplateField { Name = "Layer", Type = FieldDataType.Byte },
                new TemplateField { Name = "Flags", Type = FieldDataType.UInt16 },
                new TemplateField { Name = "Unknown1", Type = FieldDataType.Byte },
                new TemplateField { Name = "X", Type = FieldDataType.Coord },
                new TemplateField { Name = "Y", Type = FieldDataType.Coord },
                new TemplateField { Name = "TopXSize", Type = FieldDataType.Coord },
                new TemplateField { Name = "TopYSize", Type = FieldDataType.Coord },
                new TemplateField { Name = "MidXSize", Type = FieldDataType.Coord },
                new TemplateField { Name = "MidYSize", Type = FieldDataType.Coord },
                new TemplateField { Name = "BotXSize", Type = FieldDataType.Coord },
                new TemplateField { Name = "BotYSize", Type = FieldDataType.Coord },
                new TemplateField { Name = "HoleSize", Type = FieldDataType.Coord },
                new TemplateField { Name = "TopShape", Type = FieldDataType.Byte },
                new TemplateField { Name = "MidShape", Type = FieldDataType.Byte },
                new TemplateField { Name = "BotShape", Type = FieldDataType.Byte }
            ]
        });

        // PCB Track
        RegisterTemplate(new BinaryTemplate
        {
            Name = "PcbTrack",
            Description = "PCB Track primitive",
            Fields =
            [
                new TemplateField { Name = "Layer", Type = FieldDataType.Byte },
                new TemplateField { Name = "Flags", Type = FieldDataType.UInt16 },
                new TemplateField { Name = "Unknown1", Type = FieldDataType.Byte },
                new TemplateField { Name = "X1", Type = FieldDataType.Coord },
                new TemplateField { Name = "Y1", Type = FieldDataType.Coord },
                new TemplateField { Name = "X2", Type = FieldDataType.Coord },
                new TemplateField { Name = "Y2", Type = FieldDataType.Coord },
                new TemplateField { Name = "Width", Type = FieldDataType.Coord }
            ]
        });

        // Color value
        RegisterTemplate(new BinaryTemplate
        {
            Name = "ColorValue",
            Description = "BGR color value",
            Fields =
            [
                new TemplateField { Name = "Blue", Type = FieldDataType.Byte },
                new TemplateField { Name = "Green", Type = FieldDataType.Byte },
                new TemplateField { Name = "Red", Type = FieldDataType.Byte },
                new TemplateField { Name = "Alpha", Type = FieldDataType.Byte }
            ]
        });
    }

    private FieldParseResult ParseField(TemplateField field, byte[] data, int offset, List<FieldParseResult> previousFields)
    {
        var result = new FieldParseResult
        {
            FieldName = field.Name,
            StartOffset = offset,
            DataType = field.Type
        };

        try
        {
            int size = GetFieldSize(field, previousFields);

            if (offset + size > data.Length)
            {
                result.Success = false;
                result.Error = $"Not enough data (need {size} bytes, have {data.Length - offset})";
                return result;
            }

            switch (field.Type)
            {
                case FieldDataType.Byte:
                    result.RawValue = data[offset];
                    result.InterpretedValue = data[offset].ToString();
                    result.Size = 1;
                    break;

                case FieldDataType.Int16:
                    var int16 = BitConverter.ToInt16(data, offset);
                    result.RawValue = int16;
                    result.InterpretedValue = int16.ToString();
                    result.Size = 2;
                    break;

                case FieldDataType.UInt16:
                    var uint16 = BitConverter.ToUInt16(data, offset);
                    result.RawValue = uint16;
                    result.InterpretedValue = uint16.ToString();
                    result.Size = 2;
                    break;

                case FieldDataType.Int32:
                    var int32 = BitConverter.ToInt32(data, offset);
                    var maskedValue = field.SizeMask.HasValue ? int32 & field.SizeMask.Value : int32;
                    result.RawValue = int32;
                    result.InterpretedValue = maskedValue.ToString();
                    if (field.SizeMask.HasValue)
                    {
                        result.InterpretedValue += $" (raw: 0x{int32:X8})";
                    }
                    result.Size = 4;
                    break;

                case FieldDataType.UInt32:
                    var uint32 = BitConverter.ToUInt32(data, offset);
                    result.RawValue = uint32;
                    result.InterpretedValue = uint32.ToString();
                    result.Size = 4;
                    break;

                case FieldDataType.Int64:
                    var int64 = BitConverter.ToInt64(data, offset);
                    result.RawValue = int64;
                    result.InterpretedValue = int64.ToString();
                    result.Size = 8;
                    break;

                case FieldDataType.Float:
                    var floatVal = BitConverter.ToSingle(data, offset);
                    result.RawValue = floatVal;
                    result.InterpretedValue = floatVal.ToString("G");
                    result.Size = 4;
                    break;

                case FieldDataType.Double:
                    var doubleVal = BitConverter.ToDouble(data, offset);
                    result.RawValue = doubleVal;
                    result.InterpretedValue = doubleVal.ToString("G");
                    result.Size = 8;
                    break;

                case FieldDataType.Coord:
                    var coordInt = BitConverter.ToInt32(data, offset);
                    var mils = coordInt / InternalUnits;
                    result.RawValue = coordInt;
                    result.InterpretedValue = $"{mils:F2}mil ({mils / 39.37007874:F4}mm)";
                    result.Size = 4;
                    break;

                case FieldDataType.Color:
                    var colorInt = BitConverter.ToInt32(data, offset);
                    var r = colorInt & 0xFF;
                    var g = (colorInt >> 8) & 0xFF;
                    var b = (colorInt >> 16) & 0xFF;
                    result.RawValue = colorInt;
                    result.InterpretedValue = $"#{r:X2}{g:X2}{b:X2} (RGB: {r}, {g}, {b})";
                    result.Size = 4;
                    break;

                case FieldDataType.Bool:
                    result.RawValue = data[offset];
                    result.InterpretedValue = data[offset] != 0 ? "true" : "false";
                    result.Size = 1;
                    break;

                case FieldDataType.String:
                    var encoding = Encoding.GetEncoding(field.Encoding ?? "windows-1252");
                    var str = encoding.GetString(data, offset, size).TrimEnd('\0');
                    result.RawValue = str;
                    result.InterpretedValue = $"\"{str}\"";
                    result.Size = size;
                    break;

                case FieldDataType.Bytes:
                    var bytes = new byte[size];
                    Array.Copy(data, offset, bytes, 0, size);
                    result.RawValue = bytes;
                    result.InterpretedValue = $"[{size} bytes]";
                    if (size <= 32)
                    {
                        result.InterpretedValue = BitConverter.ToString(bytes).Replace("-", " ");
                    }
                    result.Size = size;
                    break;

                case FieldDataType.Guid:
                    var guidBytes = new byte[16];
                    Array.Copy(data, offset, guidBytes, 0, 16);
                    var guid = new Guid(guidBytes);
                    result.RawValue = guid;
                    result.InterpretedValue = guid.ToString();
                    result.Size = 16;
                    break;

                default:
                    result.Success = false;
                    result.Error = $"Unknown field type: {field.Type}";
                    return result;
            }

            result.EndOffset = offset + result.Size;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    private int GetFieldSize(TemplateField field, List<FieldParseResult> previousFields)
    {
        if (field.FixedSize.HasValue)
        {
            return field.FixedSize.Value;
        }

        if (!string.IsNullOrEmpty(field.SizeReference))
        {
            var referenced = previousFields.FirstOrDefault(f => f.FieldName == field.SizeReference);
            if (referenced != null && referenced.RawValue is int intVal)
            {
                var size = field.SizeMask.HasValue ? intVal & field.SizeMask.Value : intVal;
                return size;
            }
        }

        // Default sizes by type
        return field.Type switch
        {
            FieldDataType.Byte => 1,
            FieldDataType.Bool => 1,
            FieldDataType.Int16 => 2,
            FieldDataType.UInt16 => 2,
            FieldDataType.Int32 => 4,
            FieldDataType.UInt32 => 4,
            FieldDataType.Int64 => 8,
            FieldDataType.Float => 4,
            FieldDataType.Double => 8,
            FieldDataType.Coord => 4,
            FieldDataType.Color => 4,
            FieldDataType.Guid => 16,
            _ => 0
        };
    }

    private double CalculateConfidence(TemplateParseResult result, int availableBytes)
    {
        if (!result.Success) return 0;

        // Higher confidence if we consumed most of the data
        var coverageRatio = (double)result.TotalSize / availableBytes;

        // Check for reasonable values
        var reasonableFields = 0;
        foreach (var field in result.Fields)
        {
            if (IsReasonableValue(field))
                reasonableFields++;
        }

        var reasonableRatio = result.Fields.Count > 0
            ? (double)reasonableFields / result.Fields.Count
            : 0;

        return (coverageRatio * 0.3) + (reasonableRatio * 0.7);
    }

    private bool IsReasonableValue(FieldParseResult field)
    {
        if (field.DataType == FieldDataType.Coord && field.RawValue is int coordVal)
        {
            var mils = coordVal / InternalUnits;
            return Math.Abs(mils) < 100000;
        }

        if (field.DataType == FieldDataType.Byte && field.RawValue is byte byteVal)
        {
            return true; // All byte values are reasonable
        }

        if ((field.DataType == FieldDataType.Int16 || field.DataType == FieldDataType.Int32) &&
            field.RawValue is int intVal)
        {
            return intVal >= -10000000 && intVal <= 10000000;
        }

        return true;
    }
}

#region Template Types

public sealed class BinaryTemplate
{
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public List<TemplateField> Fields { get; init; } = [];
}

public sealed class TemplateField
{
    public string Name { get; init; } = "";
    public FieldDataType Type { get; init; }
    public string? Description { get; init; }
    public int? FixedSize { get; init; }
    public string? SizeReference { get; init; }
    public int? SizeMask { get; init; }
    public string? Encoding { get; init; }
    public int? BitOffset { get; init; }
    public int? BitSize { get; init; }
}

public enum FieldDataType
{
    Byte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    Float,
    Double,
    Coord,
    Color,
    Bool,
    String,
    Bytes,
    Guid
}

#endregion

#region Result Types

public sealed class TemplateParseResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string TemplateName { get; init; } = "";
    public int StartOffset { get; init; }
    public int EndOffset { get; set; }
    public int TotalSize { get; set; }
    public List<FieldParseResult> Fields { get; init; } = [];
}

public sealed class FieldParseResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string FieldName { get; init; } = "";
    public FieldDataType DataType { get; init; }
    public int StartOffset { get; init; }
    public int EndOffset { get; set; }
    public int Size { get; set; }
    public object? RawValue { get; set; }
    public string? InterpretedValue { get; set; }
}

public sealed class TemplateMatch
{
    public string TemplateName { get; init; } = "";
    public double Confidence { get; init; }
    public TemplateParseResult ParseResult { get; init; } = new();
}

#endregion
