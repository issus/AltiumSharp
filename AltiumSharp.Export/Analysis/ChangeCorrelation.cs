using System.Text.Json;
using System.Text.Json.Serialization;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Correlates changes between Altium file versions with specific actions,
/// building a knowledge base of "action → binary effect" mappings.
/// </summary>
public sealed class ChangeCorrelationTool
{
    private readonly BinaryDiffTool _diffTool = new();
    private readonly List<CorrelatedChange> _knowledgeBase = [];

    /// <summary>
    /// Record a change: compare before/after files and associate with an action.
    /// </summary>
    public CorrelatedChange RecordChange(string beforeFile, string afterFile, string actionDescription, string? category = null)
    {
        var diff = _diffTool.Compare(beforeFile, afterFile);

        var correlation = new CorrelatedChange
        {
            Action = actionDescription,
            Category = category ?? InferCategory(actionDescription),
            Timestamp = DateTime.UtcNow,
            BeforeFile = Path.GetFileName(beforeFile),
            AfterFile = Path.GetFileName(afterFile),
            AffectedStreams = diff.StreamDiffs.Select(d => new AffectedStream
            {
                Path = d.Path,
                BytesChanged = d.ChangedBytes,
                SizeDelta = d.SizeDelta,
                IsNew = d.IsNew,
                IsRemoved = d.IsRemoved,
                ChangePatterns = AnalyzeChangePatterns(d)
            }).ToList(),
            StructuralChanges = diff.StructuralChanges.Select(s => s.Path).ToList(),
            TotalBytesChanged = diff.TotalBytesChanged
        };

        _knowledgeBase.Add(correlation);
        return correlation;
    }

    /// <summary>
    /// Analyze a diff and try to match it against known patterns.
    /// </summary>
    public List<PatternMatch> MatchPatterns(BinaryDiffResult diff)
    {
        var matches = new List<PatternMatch>();

        foreach (var knownChange in _knowledgeBase)
        {
            var similarity = CalculateSimilarity(diff, knownChange);
            if (similarity > 0.3) // 30% threshold
            {
                matches.Add(new PatternMatch
                {
                    Action = knownChange.Action,
                    Category = knownChange.Category,
                    Confidence = similarity,
                    MatchingStreams = GetMatchingStreams(diff, knownChange)
                });
            }
        }

        return matches.OrderByDescending(m => m.Confidence).ToList();
    }

    /// <summary>
    /// Load knowledge base from file.
    /// </summary>
    public void LoadKnowledgeBase(string filePath)
    {
        if (!File.Exists(filePath)) return;

        var json = File.ReadAllText(filePath);
        var loaded = JsonSerializer.Deserialize<List<CorrelatedChange>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (loaded != null)
        {
            _knowledgeBase.Clear();
            _knowledgeBase.AddRange(loaded);
        }
    }

    /// <summary>
    /// Save knowledge base to file.
    /// </summary>
    public async Task SaveKnowledgeBaseAsync(string filePath)
    {
        var json = JsonSerializer.Serialize(_knowledgeBase, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Get summary statistics from the knowledge base.
    /// </summary>
    public KnowledgeBaseSummary GetSummary()
    {
        return new KnowledgeBaseSummary
        {
            TotalChanges = _knowledgeBase.Count,
            ByCategory = _knowledgeBase
                .GroupBy(c => c.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            CommonPatterns = ExtractCommonPatterns(),
            StreamFrequency = _knowledgeBase
                .SelectMany(c => c.AffectedStreams)
                .GroupBy(s => s.Path)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(kv => kv.Value)
                .Take(20)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };
    }

    /// <summary>
    /// Find similar changes in the knowledge base.
    /// </summary>
    public List<CorrelatedChange> FindSimilarChanges(CorrelatedChange change, int maxResults = 10)
    {
        return _knowledgeBase
            .Where(c => c != change)
            .Select(c => (Change: c, Similarity: CalculateChangeSimilarity(change, c)))
            .Where(x => x.Similarity > 0.3)
            .OrderByDescending(x => x.Similarity)
            .Take(maxResults)
            .Select(x => x.Change)
            .ToList();
    }

    private List<ChangePattern> AnalyzeChangePatterns(StreamDiff diff)
    {
        var patterns = new List<ChangePattern>();

        foreach (var change in diff.Changes)
        {
            var pattern = new ChangePattern
            {
                Offset = change.Offset,
                Size = change.Length,
                Type = change.Interpretation?.Type ?? "Unknown",
                OldValue = change.Interpretation?.OldValue,
                NewValue = change.Interpretation?.NewValue
            };

            patterns.Add(pattern);
        }

        return patterns;
    }

    private double CalculateSimilarity(BinaryDiffResult diff, CorrelatedChange known)
    {
        // Compare affected streams
        var diffStreams = diff.StreamDiffs.Select(d => d.Path).ToHashSet();
        var knownStreams = known.AffectedStreams.Select(s => s.Path).ToHashSet();

        if (diffStreams.Count == 0 && knownStreams.Count == 0) return 0;

        var commonStreams = diffStreams.Intersect(knownStreams).Count();
        var totalStreams = diffStreams.Union(knownStreams).Count();

        var streamSimilarity = (double)commonStreams / totalStreams;

        // Compare change sizes
        var diffTotalBytes = diff.TotalBytesChanged;
        var knownTotalBytes = known.TotalBytesChanged;

        var sizeSimilarity = diffTotalBytes > 0 && knownTotalBytes > 0
            ? 1.0 - Math.Abs(diffTotalBytes - knownTotalBytes) / (double)Math.Max(diffTotalBytes, knownTotalBytes)
            : 0;

        // Compare change patterns
        var patternSimilarity = CalculatePatternSimilarity(diff, known);

        return (streamSimilarity * 0.4) + (sizeSimilarity * 0.2) + (patternSimilarity * 0.4);
    }

    private double CalculatePatternSimilarity(BinaryDiffResult diff, CorrelatedChange known)
    {
        var diffPatterns = diff.StreamDiffs
            .SelectMany(d => d.Changes)
            .Select(c => c.Interpretation?.Type ?? "Unknown")
            .ToList();

        var knownPatterns = known.AffectedStreams
            .SelectMany(s => s.ChangePatterns)
            .Select(p => p.Type)
            .ToList();

        if (diffPatterns.Count == 0 && knownPatterns.Count == 0) return 1;
        if (diffPatterns.Count == 0 || knownPatterns.Count == 0) return 0;

        var common = diffPatterns.Intersect(knownPatterns).Count();
        var total = diffPatterns.Union(knownPatterns).Count();

        return (double)common / total;
    }

    private double CalculateChangeSimilarity(CorrelatedChange a, CorrelatedChange b)
    {
        var streamsA = a.AffectedStreams.Select(s => s.Path).ToHashSet();
        var streamsB = b.AffectedStreams.Select(s => s.Path).ToHashSet();

        var commonStreams = streamsA.Intersect(streamsB).Count();
        var totalStreams = streamsA.Union(streamsB).Count();

        if (totalStreams == 0) return 0;

        var streamSimilarity = (double)commonStreams / totalStreams;
        var categorySimilarity = a.Category == b.Category ? 0.3 : 0;

        return streamSimilarity * 0.7 + categorySimilarity;
    }

    private List<string> GetMatchingStreams(BinaryDiffResult diff, CorrelatedChange known)
    {
        var diffStreams = diff.StreamDiffs.Select(d => d.Path).ToHashSet();
        var knownStreams = known.AffectedStreams.Select(s => s.Path).ToHashSet();

        return diffStreams.Intersect(knownStreams).ToList();
    }

    private List<CommonPattern> ExtractCommonPatterns()
    {
        var patterns = new Dictionary<string, CommonPattern>();

        foreach (var change in _knowledgeBase)
        {
            foreach (var stream in change.AffectedStreams)
            {
                foreach (var pattern in stream.ChangePatterns)
                {
                    var key = $"{stream.Path}:{pattern.Type}";
                    if (!patterns.TryGetValue(key, out var common))
                    {
                        common = new CommonPattern
                        {
                            StreamPath = stream.Path,
                            DataType = pattern.Type
                        };
                        patterns[key] = common;
                    }

                    common.Occurrences++;
                    if (!common.AssociatedActions.Contains(change.Action))
                    {
                        common.AssociatedActions.Add(change.Action);
                    }
                }
            }
        }

        return patterns.Values
            .Where(p => p.Occurrences > 1)
            .OrderByDescending(p => p.Occurrences)
            .Take(20)
            .ToList();
    }

    private static string InferCategory(string action)
    {
        var lowerAction = action.ToLowerInvariant();

        if (lowerAction.Contains("move") || lowerAction.Contains("position") || lowerAction.Contains("location"))
            return "Position";
        if (lowerAction.Contains("resize") || lowerAction.Contains("size") || lowerAction.Contains("width") || lowerAction.Contains("height"))
            return "Size";
        if (lowerAction.Contains("rotate") || lowerAction.Contains("rotation") || lowerAction.Contains("angle"))
            return "Rotation";
        if (lowerAction.Contains("color") || lowerAction.Contains("colour"))
            return "Appearance";
        if (lowerAction.Contains("add") || lowerAction.Contains("create") || lowerAction.Contains("new"))
            return "Add";
        if (lowerAction.Contains("delete") || lowerAction.Contains("remove"))
            return "Delete";
        if (lowerAction.Contains("rename") || lowerAction.Contains("name"))
            return "Naming";
        if (lowerAction.Contains("layer"))
            return "Layer";
        if (lowerAction.Contains("property") || lowerAction.Contains("parameter"))
            return "Property";

        return "Other";
    }
}

#region Result Types

public sealed class CorrelatedChange
{
    public string Action { get; init; } = "";
    public string Category { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string BeforeFile { get; init; } = "";
    public string AfterFile { get; init; } = "";
    public List<AffectedStream> AffectedStreams { get; init; } = [];
    public List<string> StructuralChanges { get; init; } = [];
    public int TotalBytesChanged { get; init; }
}

public sealed class AffectedStream
{
    public string Path { get; init; } = "";
    public int BytesChanged { get; init; }
    public int SizeDelta { get; init; }
    public bool IsNew { get; init; }
    public bool IsRemoved { get; init; }
    public List<ChangePattern> ChangePatterns { get; init; } = [];
}

public sealed class ChangePattern
{
    public int Offset { get; init; }
    public int Size { get; init; }
    public string Type { get; init; } = "";
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

public sealed class PatternMatch
{
    public string Action { get; init; } = "";
    public string Category { get; init; } = "";
    public double Confidence { get; init; }
    public List<string> MatchingStreams { get; init; } = [];
}

public sealed class KnowledgeBaseSummary
{
    public int TotalChanges { get; init; }
    public Dictionary<string, int> ByCategory { get; init; } = [];
    public List<CommonPattern> CommonPatterns { get; init; } = [];
    public Dictionary<string, int> StreamFrequency { get; init; } = [];
}

public sealed class CommonPattern
{
    public string StreamPath { get; init; } = "";
    public string DataType { get; init; } = "";
    public int Occurrences { get; set; }
    public List<string> AssociatedActions { get; init; } = [];
}

#endregion
