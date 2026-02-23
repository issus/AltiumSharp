using System.Text;
using System.Text.Json.Nodes;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Tracks indices and references between records to understand
/// the object graph and relationships in Altium files.
/// </summary>
public sealed class ReferenceTracker : IDisposable
{
    private CompoundFile? _compoundFile;
    private readonly List<TrackedRecord> _records = [];
    private readonly Dictionary<int, List<TrackedRecord>> _byOwnerIndex = [];
    private readonly Dictionary<string, List<TrackedRecord>> _byStream = [];
    private readonly List<ReferenceEdge> _edges = [];

    /// <summary>
    /// Analyze references in an Altium file.
    /// </summary>
    public ReferenceAnalysisResult Analyze(string filePath)
    {
        _compoundFile = new CompoundFile(filePath);
        _records.Clear();
        _byOwnerIndex.Clear();
        _byStream.Clear();
        _edges.Clear();

        var result = new ReferenceAnalysisResult
        {
            FileName = Path.GetFileName(filePath)
        };

        // Scan all streams for records
        ScanStorage(_compoundFile.RootStorage, "/");

        // Build reference graph
        BuildReferenceGraph();

        // Populate result
        result.TotalRecords = _records.Count;
        result.TotalReferences = _edges.Count;
        result.Records = _records;
        result.References = _edges;
        result.RootRecords = _records.Where(r => r.OwnerIndex == -1 || !_records.Any(o => o.Index == r.OwnerIndex)).ToList();
        result.OrphanRecords = FindOrphans();
        result.CircularReferences = FindCircularReferences();

        // Group by stream
        result.RecordsByStream = _byStream.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Count);

        // Build hierarchy summary
        result.HierarchyDepth = CalculateMaxDepth();

        return result;
    }

    /// <summary>
    /// Get the object graph starting from a specific record.
    /// </summary>
    public ObjectGraph GetObjectGraph(int recordIndex)
    {
        var record = _records.FirstOrDefault(r => r.Index == recordIndex);
        if (record == null)
        {
            return new ObjectGraph { Error = $"Record {recordIndex} not found" };
        }

        return BuildObjectGraph(record, new HashSet<int>());
    }

    /// <summary>
    /// Find all records that reference a specific index.
    /// </summary>
    public List<TrackedRecord> FindReferencesTo(int targetIndex)
    {
        return _edges
            .Where(e => e.TargetIndex == targetIndex)
            .Select(e => _records.FirstOrDefault(r => r.Index == e.SourceIndex))
            .Where(r => r != null)
            .Cast<TrackedRecord>()
            .ToList();
    }

    /// <summary>
    /// Find all records owned by a specific record.
    /// </summary>
    public List<TrackedRecord> FindChildren(int ownerIndex)
    {
        return _byOwnerIndex.TryGetValue(ownerIndex, out var children)
            ? children
            : [];
    }

    /// <summary>
    /// Get the ownership chain for a record (ancestors).
    /// </summary>
    public List<TrackedRecord> GetOwnershipChain(int recordIndex)
    {
        var chain = new List<TrackedRecord>();
        var current = _records.FirstOrDefault(r => r.Index == recordIndex);

        while (current != null && current.OwnerIndex >= 0)
        {
            var owner = _records.FirstOrDefault(r => r.Index == current.OwnerIndex);
            if (owner != null)
            {
                chain.Add(owner);
                current = owner;
            }
            else
            {
                break;
            }
        }

        return chain;
    }

    private void ScanStorage(CFStorage storage, string path)
    {
        storage.VisitEntries(entry =>
        {
            var entryPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            if (entry.IsStorage)
            {
                ScanStorage(storage.GetStorage(entry.Name), entryPath);
            }
            else if (entry.IsStream)
            {
                var stream = storage.GetStream(entry.Name);
                var data = stream.GetData();
                ScanStreamForRecords(data, entryPath);
            }
        }, recursive: false);
    }

    private void ScanStreamForRecords(byte[] data, string streamPath)
    {
        if (data.Length == 0) return;

        // Try to parse as parameters (common Altium format)
        try
        {
            var text = Encoding.GetEncoding(1252).GetString(data);
            if (text.Contains('|') && text.Contains('='))
            {
                var records = ParseParameterRecords(text, streamPath);
                foreach (var record in records)
                {
                    AddRecord(record);
                }
                return;
            }
        }
        catch { }

        // Try to parse as binary records (size-prefixed blocks)
        TryParseBinaryRecords(data, streamPath);
    }

    private List<TrackedRecord> ParseParameterRecords(string text, string streamPath)
    {
        var records = new List<TrackedRecord>();

        // Split by record marker if present
        var sections = text.Split(new[] { "|RECORD=" }, StringSplitOptions.RemoveEmptyEntries);

        var index = 0;
        foreach (var section in sections)
        {
            var record = new TrackedRecord
            {
                Index = _records.Count + records.Count,
                StreamPath = streamPath,
                RecordIndex = index++
            };

            // Parse parameters
            var entries = ("|RECORD=" + section).Split('|', StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var eqIndex = entry.IndexOf('=');
                if (eqIndex <= 0) continue;

                var key = entry[..eqIndex].Trim();
                var value = entry[(eqIndex + 1)..].TrimEnd('\0', '\r', '\n');

                record.Parameters[key] = value;

                // Track special reference fields
                if (key.Equals("RECORD", StringComparison.OrdinalIgnoreCase))
                {
                    record.RecordType = value;
                }
                else if (key.Equals("OWNERINDEX", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var ownerIdx))
                {
                    record.OwnerIndex = ownerIdx;
                }
                else if (key.Equals("INDEXINSHEET", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var sheetIdx))
                {
                    record.IndexInSheet = sheetIdx;
                }
                else if (key.Equals("OWNERPARTID", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var partId))
                {
                    record.OwnerPartId = partId;
                }
                else if (key.Equals("UNIQUEID", StringComparison.OrdinalIgnoreCase))
                {
                    record.UniqueId = value;
                }
                else if (key.Contains("INDEX", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var refIdx))
                {
                    record.OtherReferences[key] = refIdx;
                }
            }

            if (record.Parameters.Count > 0)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private void TryParseBinaryRecords(byte[] data, string streamPath)
    {
        int offset = 0;
        int recordIndex = 0;

        while (offset + 4 <= data.Length)
        {
            var sizeField = BitConverter.ToInt32(data, offset);
            var size = sizeField & 0x00FFFFFF;
            var flags = (sizeField >> 24) & 0xFF;

            if (size > 0 && size < 0x100000 && offset + 4 + size <= data.Length)
            {
                var record = new TrackedRecord
                {
                    Index = _records.Count,
                    StreamPath = streamPath,
                    RecordIndex = recordIndex++,
                    BinaryOffset = offset,
                    BinarySize = size + 4
                };

                // Try to extract references from the binary data
                var recordData = new byte[size];
                Array.Copy(data, offset + 4, recordData, 0, size);

                // Check if it's parameter data
                try
                {
                    var text = Encoding.GetEncoding(1252).GetString(recordData);
                    if (text.Contains('|') && text.Contains('='))
                    {
                        var entries = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var entry in entries)
                        {
                            var eqIndex = entry.IndexOf('=');
                            if (eqIndex <= 0) continue;

                            var key = entry[..eqIndex].Trim();
                            var value = entry[(eqIndex + 1)..].TrimEnd('\0');

                            record.Parameters[key] = value;

                            if (key.Equals("OWNERINDEX", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var ownerIdx))
                            {
                                record.OwnerIndex = ownerIdx;
                            }
                            else if (key.Equals("RECORD", StringComparison.OrdinalIgnoreCase))
                            {
                                record.RecordType = value;
                            }
                        }
                    }
                }
                catch { }

                AddRecord(record);
                offset += 4 + size;
            }
            else
            {
                break;
            }
        }
    }

    private void AddRecord(TrackedRecord record)
    {
        _records.Add(record);

        // Index by owner
        if (record.OwnerIndex >= 0)
        {
            if (!_byOwnerIndex.TryGetValue(record.OwnerIndex, out var list))
            {
                list = [];
                _byOwnerIndex[record.OwnerIndex] = list;
            }
            list.Add(record);
        }

        // Index by stream
        if (!_byStream.TryGetValue(record.StreamPath, out var streamList))
        {
            streamList = [];
            _byStream[record.StreamPath] = streamList;
        }
        streamList.Add(record);
    }

    private void BuildReferenceGraph()
    {
        foreach (var record in _records)
        {
            // Owner reference
            if (record.OwnerIndex >= 0)
            {
                _edges.Add(new ReferenceEdge
                {
                    SourceIndex = record.Index,
                    TargetIndex = record.OwnerIndex,
                    ReferenceType = "OwnerIndex",
                    SourceStream = record.StreamPath
                });
            }

            // Other index references
            foreach (var (key, targetIdx) in record.OtherReferences)
            {
                _edges.Add(new ReferenceEdge
                {
                    SourceIndex = record.Index,
                    TargetIndex = targetIdx,
                    ReferenceType = key,
                    SourceStream = record.StreamPath
                });
            }
        }
    }

    private List<TrackedRecord> FindOrphans()
    {
        // Records with OwnerIndex pointing to non-existent records
        var existingIndices = _records.Select(r => r.Index).ToHashSet();
        return _records
            .Where(r => r.OwnerIndex >= 0 && !existingIndices.Contains(r.OwnerIndex))
            .ToList();
    }

    private List<List<int>> FindCircularReferences()
    {
        var cycles = new List<List<int>>();
        var visited = new HashSet<int>();
        var recursionStack = new HashSet<int>();

        foreach (var record in _records)
        {
            if (!visited.Contains(record.Index))
            {
                FindCycles(record.Index, visited, recursionStack, [], cycles);
            }
        }

        return cycles;
    }

    private void FindCycles(int index, HashSet<int> visited, HashSet<int> recursionStack, List<int> path, List<List<int>> cycles)
    {
        visited.Add(index);
        recursionStack.Add(index);
        path.Add(index);

        var outgoing = _edges.Where(e => e.SourceIndex == index);
        foreach (var edge in outgoing)
        {
            if (!visited.Contains(edge.TargetIndex))
            {
                FindCycles(edge.TargetIndex, visited, recursionStack, new List<int>(path), cycles);
            }
            else if (recursionStack.Contains(edge.TargetIndex))
            {
                // Found a cycle
                var cycleStart = path.IndexOf(edge.TargetIndex);
                if (cycleStart >= 0)
                {
                    var cycle = path.Skip(cycleStart).ToList();
                    cycle.Add(edge.TargetIndex);
                    cycles.Add(cycle);
                }
            }
        }

        recursionStack.Remove(index);
    }

    private ObjectGraph BuildObjectGraph(TrackedRecord record, HashSet<int> visited)
    {
        if (visited.Contains(record.Index))
        {
            return new ObjectGraph
            {
                RecordIndex = record.Index,
                RecordType = record.RecordType,
                IsCyclic = true
            };
        }

        visited.Add(record.Index);

        var graph = new ObjectGraph
        {
            RecordIndex = record.Index,
            RecordType = record.RecordType,
            StreamPath = record.StreamPath,
            UniqueId = record.UniqueId,
            Properties = record.Parameters.ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        // Add children
        if (_byOwnerIndex.TryGetValue(record.Index, out var children))
        {
            foreach (var child in children)
            {
                graph.Children.Add(BuildObjectGraph(child, new HashSet<int>(visited)));
            }
        }

        return graph;
    }

    private int CalculateMaxDepth()
    {
        int maxDepth = 0;

        foreach (var root in _records.Where(r => r.OwnerIndex < 0))
        {
            var depth = CalculateDepth(root.Index, new HashSet<int>());
            maxDepth = Math.Max(maxDepth, depth);
        }

        return maxDepth;
    }

    private int CalculateDepth(int index, HashSet<int> visited)
    {
        if (visited.Contains(index)) return 0;
        visited.Add(index);

        if (!_byOwnerIndex.TryGetValue(index, out var children) || children.Count == 0)
        {
            return 1;
        }

        return 1 + children.Max(c => CalculateDepth(c.Index, new HashSet<int>(visited)));
    }

    public void Dispose()
    {
        _compoundFile?.Close();
    }
}

#region Result Types

public sealed class ReferenceAnalysisResult
{
    public string FileName { get; init; } = "";
    public int TotalRecords { get; set; }
    public int TotalReferences { get; set; }
    public int HierarchyDepth { get; set; }
    public List<TrackedRecord> Records { get; set; } = [];
    public List<ReferenceEdge> References { get; set; } = [];
    public List<TrackedRecord> RootRecords { get; set; } = [];
    public List<TrackedRecord> OrphanRecords { get; set; } = [];
    public List<List<int>> CircularReferences { get; set; } = [];
    public Dictionary<string, int> RecordsByStream { get; set; } = [];
}

public sealed class TrackedRecord
{
    public int Index { get; init; }
    public string StreamPath { get; init; } = "";
    public int RecordIndex { get; init; }
    public string? RecordType { get; set; }
    public int OwnerIndex { get; set; } = -1;
    public int? IndexInSheet { get; set; }
    public int? OwnerPartId { get; set; }
    public string? UniqueId { get; set; }
    public int BinaryOffset { get; init; }
    public int BinarySize { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = [];
    public Dictionary<string, int> OtherReferences { get; init; } = [];
}

public sealed class ReferenceEdge
{
    public int SourceIndex { get; init; }
    public int TargetIndex { get; init; }
    public string ReferenceType { get; init; } = "";
    public string SourceStream { get; init; } = "";
}

public sealed class ObjectGraph
{
    public int RecordIndex { get; init; }
    public string? RecordType { get; init; }
    public string? StreamPath { get; init; }
    public string? UniqueId { get; init; }
    public bool IsCyclic { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, string> Properties { get; init; } = [];
    public List<ObjectGraph> Children { get; init; } = [];
}

#endregion
