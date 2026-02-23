using System.Text.Json.Serialization;

namespace OriginalCircuit.AltiumSharp.Export.Comparison;

/// <summary>
/// Type of difference detected between two values.
/// </summary>
public enum DifferenceType
{
    /// <summary>Field was added in the new version.</summary>
    Added,
    /// <summary>Field was removed in the new version.</summary>
    Removed,
    /// <summary>Field value changed between versions.</summary>
    ValueChanged,
    /// <summary>Field type changed between versions.</summary>
    TypeChanged,
    /// <summary>Array item was added.</summary>
    ArrayItemAdded,
    /// <summary>Array item was removed.</summary>
    ArrayItemRemoved,
    /// <summary>Array item was modified.</summary>
    ArrayItemChanged
}

/// <summary>
/// A single difference between two JSON values.
/// </summary>
public sealed record Difference
{
    /// <summary>
    /// JSON path to the difference (e.g., "$.parsedModel.pcbLib.components[0].pattern").
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Type of difference.
    /// </summary>
    public DifferenceType Type { get; init; }

    /// <summary>
    /// Value in the old/first file (null if added).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OldValue { get; init; }

    /// <summary>
    /// Value in the new/second file (null if removed).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NewValue { get; init; }

    /// <summary>
    /// Human-readable context (e.g., "Component: RESISTOR_0603").
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Context { get; init; }

    /// <summary>
    /// Additional notes about this difference.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Notes { get; init; }
}

/// <summary>
/// Result of comparing two JSON exports.
/// </summary>
public sealed class DiffResult
{
    /// <summary>
    /// Path to the first (old) file.
    /// </summary>
    public string File1 { get; init; } = string.Empty;

    /// <summary>
    /// Path to the second (new) file.
    /// </summary>
    public string File2 { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp of the comparison.
    /// </summary>
    public DateTime ComparedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Summary statistics of the comparison.
    /// </summary>
    public DiffSummary Summary { get; init; } = new();

    /// <summary>
    /// All differences found.
    /// </summary>
    public List<Difference> Differences { get; init; } = [];

    /// <summary>
    /// New parameters discovered (potential new Altium fields).
    /// </summary>
    public List<NewParameter> NewParameters { get; init; } = [];

    /// <summary>
    /// Check if there are any differences.
    /// </summary>
    public bool HasDifferences => Differences.Count > 0 || NewParameters.Count > 0;
}

/// <summary>
/// Summary statistics for a comparison.
/// </summary>
public sealed record DiffSummary
{
    /// <summary>Total number of differences.</summary>
    public int TotalDifferences { get; init; }

    /// <summary>Number of added fields.</summary>
    public int AddedFields { get; init; }

    /// <summary>Number of removed fields.</summary>
    public int RemovedFields { get; init; }

    /// <summary>Number of changed values.</summary>
    public int ChangedValues { get; init; }

    /// <summary>Number of type changes.</summary>
    public int TypeChanges { get; init; }

    /// <summary>Differences grouped by section.</summary>
    public Dictionary<string, int> BySection { get; init; } = [];
}

/// <summary>
/// A new parameter discovered in the comparison.
/// </summary>
public sealed record NewParameter
{
    /// <summary>
    /// JSON path where the parameter was found.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Name of the parameter.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Value of the parameter.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Context where found (e.g., stream name, component name).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Context { get; init; }

    /// <summary>
    /// Likely purpose of the parameter (if can be inferred).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LikelyPurpose { get; init; }
}
