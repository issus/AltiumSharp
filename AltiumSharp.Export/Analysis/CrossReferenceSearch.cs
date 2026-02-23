using System.Text;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Searches for all occurrences of a specific value across an entire Altium file.
/// Helps understand relationships, duplication, and value usage patterns.
/// </summary>
public sealed class CrossReferenceSearch : IDisposable
{
    private CompoundFile? _compoundFile;
    private const double InternalUnits = 10000.0;

    /// <summary>
    /// Search for an integer value (including as coordinate).
    /// </summary>
    public CrossReferenceResult SearchInt32(string filePath, int value)
    {
        _compoundFile = new CompoundFile(filePath);
        var result = new CrossReferenceResult
        {
            FileName = Path.GetFileName(filePath),
            SearchType = "Int32",
            SearchValue = value.ToString()
        };

        var bytes = BitConverter.GetBytes(value);
        SearchStorage(_compoundFile.RootStorage, "/", bytes, result, ValueType.Int32);

        // Also interpret as coordinate
        var mils = value / InternalUnits;
        result.InterpretedAs.Add($"Coordinate: {mils:F2}mil ({mils / 39.37007874:F4}mm)");

        // Check if it could be a color
        if (value >= 0 && value <= 0xFFFFFF)
        {
            var r = value & 0xFF;
            var g = (value >> 8) & 0xFF;
            var b = (value >> 16) & 0xFF;
            result.InterpretedAs.Add($"Color: RGB({r}, {g}, {b}) #{r:X2}{g:X2}{b:X2}");
        }

        return result;
    }

    /// <summary>
    /// Search for a coordinate value in mils.
    /// </summary>
    public CrossReferenceResult SearchCoordinate(string filePath, double mils)
    {
        var internalValue = (int)(mils * InternalUnits);
        var result = SearchInt32(filePath, internalValue);
        result.SearchType = "Coordinate";
        result.SearchValue = $"{mils}mil";
        return result;
    }

    /// <summary>
    /// Search for a string value.
    /// </summary>
    public CrossReferenceResult SearchString(string filePath, string value, bool caseSensitive = false)
    {
        _compoundFile = new CompoundFile(filePath);
        var result = new CrossReferenceResult
        {
            FileName = Path.GetFileName(filePath),
            SearchType = "String",
            SearchValue = value
        };

        var encoding = Encoding.GetEncoding(1252);
        var bytes = encoding.GetBytes(value);

        SearchStorage(_compoundFile.RootStorage, "/", bytes, result, ValueType.String, caseSensitive);

        // Also search for UTF-8 variant
        var utf8Bytes = Encoding.UTF8.GetBytes(value);
        if (!bytes.SequenceEqual(utf8Bytes))
        {
            SearchStorage(_compoundFile.RootStorage, "/", utf8Bytes, result, ValueType.String, caseSensitive, "UTF-8");
        }

        return result;
    }

    /// <summary>
    /// Search for a byte pattern.
    /// </summary>
    public CrossReferenceResult SearchBytes(string filePath, byte[] pattern)
    {
        _compoundFile = new CompoundFile(filePath);
        var result = new CrossReferenceResult
        {
            FileName = Path.GetFileName(filePath),
            SearchType = "Bytes",
            SearchValue = BitConverter.ToString(pattern).Replace("-", " ")
        };

        SearchStorage(_compoundFile.RootStorage, "/", pattern, result, ValueType.Bytes);

        return result;
    }

    /// <summary>
    /// Search for a color value.
    /// </summary>
    public CrossReferenceResult SearchColor(string filePath, int r, int g, int b)
    {
        var colorValue = r | (g << 8) | (b << 16);
        var result = SearchInt32(filePath, colorValue);
        result.SearchType = "Color";
        result.SearchValue = $"RGB({r}, {g}, {b})";
        return result;
    }

    /// <summary>
    /// Search for a GUID.
    /// </summary>
    public CrossReferenceResult SearchGuid(string filePath, Guid guid)
    {
        var bytes = guid.ToByteArray();
        var result = SearchBytes(filePath, bytes);
        result.SearchType = "GUID";
        result.SearchValue = guid.ToString();

        // Also search for string representation
        var stringResult = SearchString(filePath, guid.ToString(), caseSensitive: false);
        foreach (var match in stringResult.Matches)
        {
            match.Notes = "String representation";
            result.Matches.Add(match);
        }

        return result;
    }

    /// <summary>
    /// Search for a floating-point value.
    /// </summary>
    public CrossReferenceResult SearchDouble(string filePath, double value, double tolerance = 0.0001)
    {
        _compoundFile = new CompoundFile(filePath);
        var result = new CrossReferenceResult
        {
            FileName = Path.GetFileName(filePath),
            SearchType = "Double",
            SearchValue = value.ToString("G")
        };

        var bytes = BitConverter.GetBytes(value);
        SearchStorage(_compoundFile.RootStorage, "/", bytes, result, ValueType.Double);

        // Also search for float representation
        var floatValue = (float)value;
        var floatBytes = BitConverter.GetBytes(floatValue);
        SearchStorage(_compoundFile.RootStorage, "/", floatBytes, result, ValueType.Float, note: "as Float32");

        return result;
    }

    /// <summary>
    /// Search for all occurrences of a parameter key.
    /// </summary>
    public ParameterSearchResult SearchParameter(string filePath, string parameterKey)
    {
        _compoundFile = new CompoundFile(filePath);
        var result = new ParameterSearchResult
        {
            FileName = Path.GetFileName(filePath),
            ParameterKey = parameterKey
        };

        SearchParameterInStorage(_compoundFile.RootStorage, "/", parameterKey, result);

        return result;
    }

    /// <summary>
    /// Find all values that appear multiple times (potential relationships).
    /// </summary>
    public DuplicateValueReport FindDuplicateValues(string filePath, int minOccurrences = 2)
    {
        _compoundFile = new CompoundFile(filePath);
        var report = new DuplicateValueReport
        {
            FileName = Path.GetFileName(filePath),
            MinOccurrences = minOccurrences
        };

        var int32Counts = new Dictionary<int, List<ValueLocation>>();
        var stringCounts = new Dictionary<string, List<ValueLocation>>();

        CollectValuesFromStorage(_compoundFile.RootStorage, "/", int32Counts, stringCounts);

        // Filter to duplicates
        foreach (var (value, locations) in int32Counts.Where(kv => kv.Value.Count >= minOccurrences))
        {
            var mils = value / InternalUnits;
            var interpretation = Math.Abs(mils) < 100000
                ? $"{mils:F2}mil"
                : value.ToString();

            report.DuplicateInt32s.Add(new DuplicateValue<int>
            {
                Value = value,
                Interpretation = interpretation,
                Occurrences = locations.Count,
                Locations = locations
            });
        }

        foreach (var (value, locations) in stringCounts.Where(kv => kv.Value.Count >= minOccurrences))
        {
            report.DuplicateStrings.Add(new DuplicateValue<string>
            {
                Value = value,
                Occurrences = locations.Count,
                Locations = locations
            });
        }

        // Sort by occurrence count
        report.DuplicateInt32s = report.DuplicateInt32s.OrderByDescending(d => d.Occurrences).ToList();
        report.DuplicateStrings = report.DuplicateStrings.OrderByDescending(d => d.Occurrences).ToList();

        return report;
    }

    private void SearchStorage(CFStorage storage, string path, byte[] pattern, CrossReferenceResult result, ValueType valueType, bool caseSensitive = false, string? note = null)
    {
        storage.VisitEntries(entry =>
        {
            var entryPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            if (entry.IsStorage)
            {
                SearchStorage(storage.GetStorage(entry.Name), entryPath, pattern, result, valueType, caseSensitive, note);
            }
            else if (entry.IsStream)
            {
                var stream = storage.GetStream(entry.Name);
                var data = stream.GetData();
                SearchInData(data, entryPath, pattern, result, valueType, caseSensitive, note);
            }
        }, recursive: false);
    }

    private void SearchInData(byte[] data, string streamPath, byte[] pattern, CrossReferenceResult result, ValueType valueType, bool caseSensitive = false, string? note = null)
    {
        if (data.Length < pattern.Length) return;

        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool match;
            if (caseSensitive || valueType != ValueType.String)
            {
                match = true;
                for (int j = 0; j < pattern.Length && match; j++)
                {
                    match = data[i + j] == pattern[j];
                }
            }
            else
            {
                // Case-insensitive string search
                match = true;
                for (int j = 0; j < pattern.Length && match; j++)
                {
                    var d = (char)data[i + j];
                    var p = (char)pattern[j];
                    match = char.ToUpperInvariant(d) == char.ToUpperInvariant(p);
                }
            }

            if (match)
            {
                var location = new ValueMatch
                {
                    StreamPath = streamPath,
                    Offset = i,
                    ValueType = valueType,
                    Notes = note
                };

                // Get context
                var contextStart = Math.Max(0, i - 16);
                var contextEnd = Math.Min(data.Length, i + pattern.Length + 16);
                var context = new byte[contextEnd - contextStart];
                Array.Copy(data, contextStart, context, 0, context.Length);
                location.ContextHex = BitConverter.ToString(context).Replace("-", " ");

                // Try to determine field context from parameters
                location.FieldContext = TryDetermineFieldContext(data, i, streamPath);

                result.Matches.Add(location);
            }
        }
    }

    private string? TryDetermineFieldContext(byte[] data, int offset, string streamPath)
    {
        // Try to find a nearby parameter name
        try
        {
            var text = Encoding.GetEncoding(1252).GetString(data);

            // Look backwards for '|' and '='
            var searchStart = Math.Max(0, offset - 100);
            var searchText = text[searchStart..Math.Min(text.Length, offset + 50)];

            var lastPipe = searchText.LastIndexOf('|');
            if (lastPipe >= 0)
            {
                var afterPipe = searchText[(lastPipe + 1)..];
                var eqIndex = afterPipe.IndexOf('=');
                if (eqIndex > 0)
                {
                    var paramName = afterPipe[..eqIndex].Trim();
                    if (paramName.Length > 0 && paramName.Length < 50)
                    {
                        return $"Near parameter: {paramName}";
                    }
                }
            }
        }
        catch { }

        return null;
    }

    private void SearchParameterInStorage(CFStorage storage, string path, string paramKey, ParameterSearchResult result)
    {
        var upperKey = paramKey.ToUpperInvariant();

        storage.VisitEntries(entry =>
        {
            var entryPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            if (entry.IsStorage)
            {
                SearchParameterInStorage(storage.GetStorage(entry.Name), entryPath, paramKey, result);
            }
            else if (entry.IsStream)
            {
                var stream = storage.GetStream(entry.Name);
                var data = stream.GetData();

                try
                {
                    var text = Encoding.GetEncoding(1252).GetString(data);
                    if (text.Contains('|') && text.Contains('='))
                    {
                        var entries = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var paramEntry in entries)
                        {
                            var eqIndex = paramEntry.IndexOf('=');
                            if (eqIndex > 0)
                            {
                                var key = paramEntry[..eqIndex].Trim();
                                if (key.Equals(paramKey, StringComparison.OrdinalIgnoreCase) ||
                                    key.EndsWith(paramKey, StringComparison.OrdinalIgnoreCase))
                                {
                                    var value = paramEntry[(eqIndex + 1)..].TrimEnd('\0', '\r', '\n');
                                    result.Occurrences.Add(new ParameterOccurrence
                                    {
                                        StreamPath = entryPath,
                                        FullKey = key,
                                        Value = value,
                                        ValueType = InferParameterType(value)
                                    });
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }, recursive: false);
    }

    private void CollectValuesFromStorage(CFStorage storage, string path, Dictionary<int, List<ValueLocation>> int32s, Dictionary<string, List<ValueLocation>> strings)
    {
        storage.VisitEntries(entry =>
        {
            var entryPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            if (entry.IsStorage)
            {
                CollectValuesFromStorage(storage.GetStorage(entry.Name), entryPath, int32s, strings);
            }
            else if (entry.IsStream)
            {
                var stream = storage.GetStream(entry.Name);
                var data = stream.GetData();

                // Collect int32 values at 4-byte boundaries
                for (int i = 0; i <= data.Length - 4; i += 4)
                {
                    var value = BitConverter.ToInt32(data, i);
                    // Filter to reasonable values
                    if (value != 0 && value != -1 && Math.Abs(value) < 100000000)
                    {
                        if (!int32s.TryGetValue(value, out var list))
                        {
                            list = [];
                            int32s[value] = list;
                        }
                        list.Add(new ValueLocation { StreamPath = entryPath, Offset = i });
                    }
                }

                // Collect strings from parameters
                try
                {
                    var text = Encoding.GetEncoding(1252).GetString(data);
                    if (text.Contains('|') && text.Contains('='))
                    {
                        var entries = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var paramEntry in entries)
                        {
                            var eqIndex = paramEntry.IndexOf('=');
                            if (eqIndex > 0)
                            {
                                var value = paramEntry[(eqIndex + 1)..].TrimEnd('\0', '\r', '\n');
                                if (value.Length >= 2 && value.Length <= 100 && !int.TryParse(value, out _))
                                {
                                    if (!strings.TryGetValue(value, out var list))
                                    {
                                        list = [];
                                        strings[value] = list;
                                    }
                                    list.Add(new ValueLocation { StreamPath = entryPath, Offset = -1 });
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }, recursive: false);
    }

    private string InferParameterType(string value)
    {
        if (value == "T" || value == "F") return "Boolean";
        if (int.TryParse(value, out var intVal))
        {
            var mils = intVal / InternalUnits;
            if (Math.Abs(mils) < 100000) return "Coordinate/Int";
            return "Integer";
        }
        if (double.TryParse(value, out _)) return "Number";
        if (Guid.TryParse(value, out _)) return "GUID";
        if (value.Length == 6 && value.All(c => char.IsAsciiHexDigit(c))) return "Color";
        return "String";
    }

    public void Dispose()
    {
        _compoundFile?.Close();
    }
}

#region Result Types

public enum ValueType
{
    Int32,
    Float,
    Double,
    String,
    Bytes,
    Coord,
    Color,
    Guid
}

public sealed class CrossReferenceResult
{
    public string FileName { get; init; } = "";
    public string SearchType { get; set; } = "";
    public string SearchValue { get; set; } = "";
    public List<string> InterpretedAs { get; init; } = [];
    public List<ValueMatch> Matches { get; init; } = [];
}

public sealed class ValueMatch
{
    public string StreamPath { get; init; } = "";
    public int Offset { get; init; }
    public ValueType ValueType { get; init; }
    public string? ContextHex { get; set; }
    public string? FieldContext { get; set; }
    public string? Notes { get; set; }
}

public sealed class ParameterSearchResult
{
    public string FileName { get; init; } = "";
    public string ParameterKey { get; init; } = "";
    public List<ParameterOccurrence> Occurrences { get; init; } = [];
}

public sealed class ParameterOccurrence
{
    public string StreamPath { get; init; } = "";
    public string FullKey { get; init; } = "";
    public string Value { get; init; } = "";
    public string ValueType { get; init; } = "";
}

public sealed class DuplicateValueReport
{
    public string FileName { get; init; } = "";
    public int MinOccurrences { get; init; }
    public List<DuplicateValue<int>> DuplicateInt32s { get; set; } = [];
    public List<DuplicateValue<string>> DuplicateStrings { get; set; } = [];
}

public sealed class DuplicateValue<T>
{
    public T Value { get; init; } = default!;
    public string? Interpretation { get; init; }
    public int Occurrences { get; init; }
    public List<ValueLocation> Locations { get; init; } = [];
}

public sealed class ValueLocation
{
    public string StreamPath { get; init; } = "";
    public int Offset { get; init; }
}

#endregion
