using System.Text;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Learns binary patterns from multiple Altium files to identify
/// repeating structures and suggest field boundaries.
/// </summary>
public sealed class PatternLearner
{
    private readonly List<LearnedPattern> _patterns = [];
    private readonly Dictionary<string, StreamStatistics> _streamStats = [];
    private readonly Dictionary<string, List<byte[]>> _streamSamples = [];
    private const double InternalUnits = 10000.0;

    /// <summary>
    /// Add a file to the learning set.
    /// </summary>
    public void AddFile(string filePath)
    {
        using var cf = new CompoundFile(filePath);
        LearnFromStorage(cf.RootStorage, "/");
    }

    /// <summary>
    /// Analyze learned data and generate pattern suggestions.
    /// </summary>
    public PatternLearningResult Analyze()
    {
        var result = new PatternLearningResult
        {
            TotalStreamsAnalyzed = _streamStats.Count,
            TotalSamples = _streamSamples.Values.Sum(s => s.Count)
        };

        // Analyze each stream type
        foreach (var (streamPath, samples) in _streamSamples)
        {
            if (samples.Count < 2) continue;

            var analysis = AnalyzeStreamSamples(streamPath, samples);
            if (analysis.IdentifiedFields.Count > 0)
            {
                result.StreamAnalyses.Add(analysis);
            }
        }

        // Find common patterns across streams
        result.CommonPatterns = FindCommonPatterns();

        // Generate template suggestions
        result.SuggestedTemplates = GenerateTemplateSuggestions(result.StreamAnalyses);

        return result;
    }

    /// <summary>
    /// Get statistics for a specific stream path.
    /// </summary>
    public StreamStatistics? GetStreamStats(string streamPath)
    {
        return _streamStats.GetValueOrDefault(streamPath);
    }

    private void LearnFromStorage(CFStorage storage, string path)
    {
        storage.VisitEntries(entry =>
        {
            var entryPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            if (entry.IsStorage)
            {
                LearnFromStorage(storage.GetStorage(entry.Name), entryPath);
            }
            else if (entry.IsStream)
            {
                var stream = storage.GetStream(entry.Name);
                var data = stream.GetData();
                LearnFromStream(entryPath, data);
            }
        }, recursive: false);
    }

    private void LearnFromStream(string streamPath, byte[] data)
    {
        // Update statistics
        if (!_streamStats.TryGetValue(streamPath, out var stats))
        {
            stats = new StreamStatistics { Path = streamPath };
            _streamStats[streamPath] = stats;
        }

        stats.SampleCount++;
        stats.MinSize = Math.Min(stats.MinSize == 0 ? int.MaxValue : stats.MinSize, data.Length);
        stats.MaxSize = Math.Max(stats.MaxSize, data.Length);
        stats.TotalSize += data.Length;

        // Store sample (limit to first 10)
        if (!_streamSamples.TryGetValue(streamPath, out var samples))
        {
            samples = [];
            _streamSamples[streamPath] = samples;
        }

        if (samples.Count < 10)
        {
            samples.Add(data);
        }
    }

    private StreamAnalysis AnalyzeStreamSamples(string streamPath, List<byte[]> samples)
    {
        var analysis = new StreamAnalysis
        {
            StreamPath = streamPath,
            SampleCount = samples.Count
        };

        if (samples.Count == 0) return analysis;

        // Find minimum common length
        var minLen = samples.Min(s => s.Length);
        analysis.CommonPrefixLength = minLen;

        // Analyze byte-by-byte for patterns
        var byteAnalyses = new List<BytePositionAnalysis>();

        for (int i = 0; i < minLen; i++)
        {
            var values = samples.Select(s => s[i]).ToList();
            var byteAnalysis = AnalyzeBytePosition(i, values);
            byteAnalyses.Add(byteAnalysis);
        }

        // Identify fields based on patterns
        var fields = IdentifyFieldsFromBytes(byteAnalyses, samples, streamPath);
        analysis.IdentifiedFields = fields;

        // Calculate confidence
        analysis.OverallConfidence = fields.Count > 0
            ? fields.Average(f => f.Confidence)
            : 0;

        return analysis;
    }

    private BytePositionAnalysis AnalyzeBytePosition(int offset, List<byte> values)
    {
        var analysis = new BytePositionAnalysis { Offset = offset };

        var distinctValues = values.Distinct().ToList();
        analysis.UniqueValues = distinctValues.Count;
        analysis.IsConstant = distinctValues.Count == 1;

        if (analysis.IsConstant)
        {
            analysis.ConstantValue = distinctValues[0];
        }

        // Calculate variance
        var avg = values.Average(v => v);
        analysis.Variance = values.Average(v => Math.Pow(v - avg, 2));

        // Check for boolean pattern (0 or 1 only, or 0 or 0xFF)
        if (distinctValues.All(v => v == 0 || v == 1))
        {
            analysis.LikelyType = "Boolean";
        }
        else if (distinctValues.All(v => v == 0 || v == 0xFF))
        {
            analysis.LikelyType = "Boolean (0xFF)";
        }

        return analysis;
    }

    private List<IdentifiedField> IdentifyFieldsFromBytes(List<BytePositionAnalysis> byteAnalyses, List<byte[]> samples, string streamPath)
    {
        var fields = new List<IdentifiedField>();
        var i = 0;

        while (i < byteAnalyses.Count)
        {
            var field = TryIdentifyFieldAt(i, byteAnalyses, samples);
            if (field != null)
            {
                fields.Add(field);
                i += field.Size;
            }
            else
            {
                i++;
            }
        }

        return fields;
    }

    private IdentifiedField? TryIdentifyFieldAt(int offset, List<BytePositionAnalysis> byteAnalyses, List<byte[]> samples)
    {
        var remaining = byteAnalyses.Count - offset;

        // Try 4-byte coordinate
        if (remaining >= 4)
        {
            var coordField = TryIdentifyCoordinate(offset, samples);
            if (coordField != null) return coordField;
        }

        // Try 4-byte integer
        if (remaining >= 4)
        {
            var intField = TryIdentifyInt32(offset, samples);
            if (intField != null) return intField;
        }

        // Try 2-byte short
        if (remaining >= 2)
        {
            var shortField = TryIdentifyInt16(offset, samples);
            if (shortField != null) return shortField;
        }

        // Try boolean
        var boolField = TryIdentifyBoolean(offset, byteAnalyses);
        if (boolField != null) return boolField;

        // Try constant byte
        if (byteAnalyses[offset].IsConstant)
        {
            return new IdentifiedField
            {
                Offset = offset,
                Size = 1,
                Type = "Constant",
                Confidence = 0.9,
                Notes = $"Always 0x{byteAnalyses[offset].ConstantValue:X2}"
            };
        }

        return null;
    }

    private IdentifiedField? TryIdentifyCoordinate(int offset, List<byte[]> samples)
    {
        var values = samples
            .Where(s => s.Length >= offset + 4)
            .Select(s => BitConverter.ToInt32(s, offset))
            .ToList();

        if (values.Count < 2) return null;

        // Check if values look like coordinates
        var mils = values.Select(v => v / InternalUnits).ToList();
        var allReasonable = mils.All(m => Math.Abs(m) < 100000);

        if (!allReasonable) return null;

        // Check for "round" values (multiples of common sizes)
        var roundCount = mils.Count(m => m % 1 == 0 || m % 0.5 == 0);
        var roundRatio = (double)roundCount / mils.Count;

        if (roundRatio < 0.5) return null;

        // Check if there's variation (not just constant)
        var distinctCount = values.Distinct().Count();
        if (distinctCount == 1) return null;

        return new IdentifiedField
        {
            Offset = offset,
            Size = 4,
            Type = "Coord",
            Confidence = 0.6 + (roundRatio * 0.3),
            SampleValues = mils.Distinct().Take(5).Select(m => $"{m:F2}mil").ToList(),
            Notes = $"{distinctCount} distinct values"
        };
    }

    private IdentifiedField? TryIdentifyInt32(int offset, List<byte[]> samples)
    {
        var values = samples
            .Where(s => s.Length >= offset + 4)
            .Select(s => BitConverter.ToInt32(s, offset))
            .ToList();

        if (values.Count < 2) return null;

        var distinctCount = values.Distinct().Count();

        // Check for small integers (likely enum or index)
        if (values.All(v => v >= 0 && v < 256))
        {
            return new IdentifiedField
            {
                Offset = offset,
                Size = 4,
                Type = "Int32 (small)",
                Confidence = 0.5,
                SampleValues = values.Distinct().Take(5).Select(v => v.ToString()).ToList(),
                Notes = distinctCount == 1 ? "Constant" : $"{distinctCount} distinct values, possibly enum"
            };
        }

        // Check for color values
        if (values.All(v => v >= 0 && v <= 0xFFFFFF))
        {
            return new IdentifiedField
            {
                Offset = offset,
                Size = 4,
                Type = "Color/Int32",
                Confidence = 0.4,
                SampleValues = values.Distinct().Take(5).Select(v => $"0x{v:X6}").ToList()
            };
        }

        return null;
    }

    private IdentifiedField? TryIdentifyInt16(int offset, List<byte[]> samples)
    {
        var values = samples
            .Where(s => s.Length >= offset + 2)
            .Select(s => BitConverter.ToInt16(s, offset))
            .ToList();

        if (values.Count < 2) return null;

        var distinctCount = values.Distinct().Count();

        // Small positive values are likely
        if (values.All(v => v >= 0 && v < 1000))
        {
            return new IdentifiedField
            {
                Offset = offset,
                Size = 2,
                Type = "Int16",
                Confidence = 0.5,
                SampleValues = values.Distinct().Take(5).Select(v => v.ToString()).ToList(),
                Notes = distinctCount == 1 ? "Constant" : $"{distinctCount} distinct values"
            };
        }

        return null;
    }

    private IdentifiedField? TryIdentifyBoolean(int offset, List<BytePositionAnalysis> byteAnalyses)
    {
        var analysis = byteAnalyses[offset];

        if (analysis.LikelyType?.StartsWith("Boolean") == true)
        {
            return new IdentifiedField
            {
                Offset = offset,
                Size = 1,
                Type = analysis.LikelyType,
                Confidence = 0.8
            };
        }

        return null;
    }

    private List<LearnedPattern> FindCommonPatterns()
    {
        var patterns = new List<LearnedPattern>();

        // Find size-prefixed block pattern
        var sizePrefix = FindSizePrefixPattern();
        if (sizePrefix != null) patterns.Add(sizePrefix);

        // Find coordinate pair pattern
        var coordPair = FindCoordPairPattern();
        if (coordPair != null) patterns.Add(coordPair);

        return patterns;
    }

    private LearnedPattern? FindSizePrefixPattern()
    {
        var matches = 0;
        var total = 0;

        foreach (var (path, samples) in _streamSamples)
        {
            foreach (var sample in samples)
            {
                if (sample.Length >= 4)
                {
                    total++;
                    var sizeField = BitConverter.ToInt32(sample, 0);
                    var size = sizeField & 0x00FFFFFF;

                    if (size > 0 && size == sample.Length - 4)
                    {
                        matches++;
                    }
                }
            }
        }

        if (matches > 0 && (double)matches / total > 0.1)
        {
            return new LearnedPattern
            {
                Name = "SizePrefixedBlock",
                Description = "4-byte size field (lower 24 bits) followed by content",
                Occurrences = matches,
                Confidence = (double)matches / total,
                Structure = "int32 Size | byte[Size] Content"
            };
        }

        return null;
    }

    private LearnedPattern? FindCoordPairPattern()
    {
        // Look for consecutive 4-byte values that both look like coordinates
        var matches = 0;

        foreach (var samples in _streamSamples.Values)
        {
            foreach (var sample in samples)
            {
                for (int i = 0; i <= sample.Length - 8; i += 4)
                {
                    var v1 = BitConverter.ToInt32(sample, i);
                    var v2 = BitConverter.ToInt32(sample, i + 4);

                    var m1 = v1 / InternalUnits;
                    var m2 = v2 / InternalUnits;

                    if (Math.Abs(m1) < 50000 && Math.Abs(m2) < 50000 &&
                        (m1 % 1 == 0 || m1 % 0.5 == 0) &&
                        (m2 % 1 == 0 || m2 % 0.5 == 0))
                    {
                        matches++;
                    }
                }
            }
        }

        if (matches > 10)
        {
            return new LearnedPattern
            {
                Name = "CoordPair",
                Description = "Two consecutive 4-byte coordinate values (X, Y)",
                Occurrences = matches,
                Confidence = 0.7,
                Structure = "Coord X | Coord Y"
            };
        }

        return null;
    }

    private List<SuggestedTemplate> GenerateTemplateSuggestions(List<StreamAnalysis> analyses)
    {
        var suggestions = new List<SuggestedTemplate>();

        foreach (var analysis in analyses.Where(a => a.IdentifiedFields.Count >= 3))
        {
            var template = new SuggestedTemplate
            {
                Name = GenerateTemplateName(analysis.StreamPath),
                BasedOnStream = analysis.StreamPath,
                Confidence = analysis.OverallConfidence
            };

            foreach (var field in analysis.IdentifiedFields)
            {
                template.Fields.Add(new SuggestedField
                {
                    Offset = field.Offset,
                    Size = field.Size,
                    Type = field.Type,
                    Name = GenerateFieldName(field),
                    Confidence = field.Confidence
                });
            }

            if (template.Fields.Count >= 3)
            {
                suggestions.Add(template);
            }
        }

        return suggestions.OrderByDescending(s => s.Confidence).Take(10).ToList();
    }

    private string GenerateTemplateName(string streamPath)
    {
        var parts = streamPath.Split('/');
        var lastPart = parts.LastOrDefault(p => !string.IsNullOrEmpty(p)) ?? "Unknown";
        return $"Template_{lastPart.Replace(" ", "")}";
    }

    private string GenerateFieldName(IdentifiedField field)
    {
        return field.Type switch
        {
            "Coord" => $"Coord_{field.Offset:X}",
            "Boolean" or "Boolean (0xFF)" => $"Flag_{field.Offset:X}",
            "Int32 (small)" => $"Index_{field.Offset:X}",
            "Int16" => $"Short_{field.Offset:X}",
            "Color/Int32" => $"Value_{field.Offset:X}",
            "Constant" => $"Magic_{field.Offset:X}",
            _ => $"Field_{field.Offset:X}"
        };
    }
}

#region Result Types

public sealed class PatternLearningResult
{
    public int TotalStreamsAnalyzed { get; set; }
    public int TotalSamples { get; set; }
    public List<StreamAnalysis> StreamAnalyses { get; init; } = [];
    public List<LearnedPattern> CommonPatterns { get; set; } = [];
    public List<SuggestedTemplate> SuggestedTemplates { get; set; } = [];
}

public sealed class StreamStatistics
{
    public string Path { get; init; } = "";
    public int SampleCount { get; set; }
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
    public long TotalSize { get; set; }
    public double AverageSize => SampleCount > 0 ? (double)TotalSize / SampleCount : 0;
}

public sealed class StreamAnalysis
{
    public string StreamPath { get; init; } = "";
    public int SampleCount { get; init; }
    public int CommonPrefixLength { get; set; }
    public double OverallConfidence { get; set; }
    public List<IdentifiedField> IdentifiedFields { get; set; } = [];
}

public sealed class BytePositionAnalysis
{
    public int Offset { get; init; }
    public int UniqueValues { get; set; }
    public bool IsConstant { get; set; }
    public byte ConstantValue { get; set; }
    public double Variance { get; set; }
    public string? LikelyType { get; set; }
}

public sealed class IdentifiedField
{
    public int Offset { get; init; }
    public int Size { get; init; }
    public string Type { get; init; } = "";
    public double Confidence { get; init; }
    public List<string> SampleValues { get; init; } = [];
    public string? Notes { get; init; }
}

public sealed class LearnedPattern
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public int Occurrences { get; init; }
    public double Confidence { get; init; }
    public string Structure { get; init; } = "";
}

public sealed class SuggestedTemplate
{
    public string Name { get; init; } = "";
    public string BasedOnStream { get; init; } = "";
    public double Confidence { get; init; }
    public List<SuggestedField> Fields { get; init; } = [];
}

public sealed class SuggestedField
{
    public int Offset { get; init; }
    public int Size { get; init; }
    public string Type { get; init; } = "";
    public string Name { get; init; } = "";
    public double Confidence { get; init; }
}

#endregion
