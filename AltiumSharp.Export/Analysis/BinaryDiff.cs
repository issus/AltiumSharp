using System.Text;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Performs byte-level binary comparison between two Altium files.
/// </summary>
public sealed class BinaryDiffTool : IDisposable
{
    private CompoundFile? _file1;
    private CompoundFile? _file2;
    private string? _file1Path;
    private string? _file2Path;

    /// <summary>
    /// Compare two Altium files at the binary level.
    /// </summary>
    public BinaryDiffResult Compare(string file1Path, string file2Path)
    {
        _file1Path = file1Path;
        _file2Path = file2Path;
        _file1 = new CompoundFile(file1Path);
        _file2 = new CompoundFile(file2Path);

        var result = new BinaryDiffResult
        {
            File1 = Path.GetFileName(file1Path),
            File2 = Path.GetFileName(file2Path),
            File1Size = new FileInfo(file1Path).Length,
            File2Size = new FileInfo(file2Path).Length
        };

        // Compare storage structure
        CompareStorage(_file1.RootStorage, _file2.RootStorage, "/", result);

        // Calculate summary
        result.TotalBytesChanged = result.StreamDiffs.Sum(d => d.ChangedBytes);
        result.TotalStreamsChanged = result.StreamDiffs.Count(d => d.HasChanges);

        return result;
    }

    private void CompareStorage(CFStorage storage1, CFStorage storage2, string path, BinaryDiffResult result)
    {
        var entries1 = GetEntries(storage1);
        var entries2 = GetEntries(storage2);

        var allNames = entries1.Keys.Union(entries2.Keys).ToHashSet();

        foreach (var name in allNames)
        {
            var entryPath = path == "/" ? "/" + name : path + "/" + name;
            var in1 = entries1.TryGetValue(name, out var entry1);
            var in2 = entries2.TryGetValue(name, out var entry2);

            if (in1 && in2)
            {
                // Both exist - compare
                if (entry1!.IsStorage && entry2!.IsStorage)
                {
                    CompareStorage(
                        storage1.GetStorage(name),
                        storage2.GetStorage(name),
                        entryPath,
                        result);
                }
                else if (entry1!.IsStream && entry2!.IsStream)
                {
                    var stream1 = storage1.GetStream(name);
                    var stream2 = storage2.GetStream(name);
                    var streamDiff = CompareStreams(stream1, stream2, entryPath);
                    if (streamDiff.HasChanges)
                    {
                        result.StreamDiffs.Add(streamDiff);
                    }
                }
                else
                {
                    // Type mismatch (storage vs stream)
                    result.StructuralChanges.Add(new StructuralChange
                    {
                        Path = entryPath,
                        Type = StructuralChangeType.TypeChanged,
                        Description = $"Changed from {(entry1!.IsStorage ? "storage" : "stream")} to {(entry2!.IsStorage ? "storage" : "stream")}"
                    });
                }
            }
            else if (in1 && !in2)
            {
                // Removed in file2
                result.StructuralChanges.Add(new StructuralChange
                {
                    Path = entryPath,
                    Type = StructuralChangeType.Removed,
                    Description = $"Removed {(entry1!.IsStorage ? "storage" : "stream")}"
                });
            }
            else if (!in1 && in2)
            {
                // Added in file2
                result.StructuralChanges.Add(new StructuralChange
                {
                    Path = entryPath,
                    Type = StructuralChangeType.Added,
                    Description = $"Added {(entry2!.IsStorage ? "storage" : "stream")}"
                });

                if (entry2!.IsStream)
                {
                    var stream2 = storage2.GetStream(name);
                    result.StreamDiffs.Add(new StreamDiff
                    {
                        Path = entryPath,
                        Size1 = 0,
                        Size2 = (int)stream2.Size,
                        IsNew = true,
                        ChangedBytes = (int)stream2.Size
                    });
                }
            }
        }
    }

    private Dictionary<string, CFItem> GetEntries(CFStorage storage)
    {
        var entries = new Dictionary<string, CFItem>();
        storage.VisitEntries(entry =>
        {
            entries[entry.Name] = entry;
        }, recursive: false);
        return entries;
    }

    private StreamDiff CompareStreams(CFStream stream1, CFStream stream2, string path)
    {
        var data1 = stream1.GetData();
        var data2 = stream2.GetData();

        var diff = new StreamDiff
        {
            Path = path,
            Size1 = data1.Length,
            Size2 = data2.Length
        };

        if (data1.Length == 0 && data2.Length == 0)
        {
            return diff;
        }

        // Find all differences
        var maxLen = Math.Max(data1.Length, data2.Length);
        var minLen = Math.Min(data1.Length, data2.Length);

        int? rangeStart = null;
        byte[]? rangeOldBytes = null;
        byte[]? rangeNewBytes = null;

        for (int i = 0; i < maxLen; i++)
        {
            var b1 = i < data1.Length ? data1[i] : (byte?)null;
            var b2 = i < data2.Length ? data2[i] : (byte?)null;

            bool isDifferent = b1 != b2;

            if (isDifferent)
            {
                if (rangeStart == null)
                {
                    rangeStart = i;
                    rangeOldBytes = [];
                    rangeNewBytes = [];
                }

                rangeOldBytes = [.. rangeOldBytes!, b1 ?? 0];
                rangeNewBytes = [.. rangeNewBytes!, b2 ?? 0];
            }
            else if (rangeStart != null)
            {
                // End of a changed range
                AddByteRange(diff, rangeStart.Value, rangeOldBytes!, rangeNewBytes!, data1, data2);
                rangeStart = null;
                rangeOldBytes = null;
                rangeNewBytes = null;
            }
        }

        // Handle final range
        if (rangeStart != null)
        {
            AddByteRange(diff, rangeStart.Value, rangeOldBytes!, rangeNewBytes!, data1, data2);
        }

        diff.ChangedBytes = diff.Changes.Sum(c => c.Length);

        return diff;
    }

    private void AddByteRange(StreamDiff diff, int start, byte[] oldBytes, byte[] newBytes, byte[] fullOld, byte[] fullNew)
    {
        // Get context (bytes before and after)
        const int contextSize = 8;
        var contextStart = Math.Max(0, start - contextSize);
        var contextEnd = Math.Min(Math.Max(fullOld.Length, fullNew.Length), start + oldBytes.Length + contextSize);

        var change = new ByteChange
        {
            Offset = start,
            Length = Math.Max(oldBytes.Length, newBytes.Length),
            OldBytes = oldBytes,
            NewBytes = newBytes,
            ContextBefore = contextStart < start
                ? fullOld.Skip(contextStart).Take(start - contextStart).ToArray()
                : [],
            ContextAfter = start + oldBytes.Length < fullOld.Length
                ? fullOld.Skip(start + oldBytes.Length).Take(Math.Min(contextSize, fullOld.Length - start - oldBytes.Length)).ToArray()
                : []
        };

        // Try to interpret the change
        change.Interpretation = InterpretChange(change, fullOld, fullNew);

        diff.Changes.Add(change);
    }

    private ChangeInterpretation? InterpretChange(ByteChange change, byte[] fullOld, byte[] fullNew)
    {
        // Try to interpret as various types
        if (change.Length == 4)
        {
            var oldInt = BitConverter.ToInt32(change.OldBytes, 0);
            var newInt = BitConverter.ToInt32(change.NewBytes, 0);

            // Check if it's a coordinate
            var oldMils = oldInt / 10000.0;
            var newMils = newInt / 10000.0;

            if (Math.Abs(oldMils) < 100000 && Math.Abs(newMils) < 100000)
            {
                return new ChangeInterpretation
                {
                    Type = "Coordinate",
                    OldValue = $"{oldMils:F2}mil ({oldMils / 39.37:F4}mm)",
                    NewValue = $"{newMils:F2}mil ({newMils / 39.37:F4}mm)",
                    Delta = $"{newMils - oldMils:+0.##;-0.##;0}mil"
                };
            }

            // Check if it's a color
            if ((oldInt & 0xFF000000) == 0 && (newInt & 0xFF000000) == 0)
            {
                return new ChangeInterpretation
                {
                    Type = "Color",
                    OldValue = $"#{oldInt:X6}",
                    NewValue = $"#{newInt:X6}"
                };
            }

            // Plain integer
            return new ChangeInterpretation
            {
                Type = "Int32",
                OldValue = oldInt.ToString(),
                NewValue = newInt.ToString(),
                Delta = $"{newInt - oldInt:+0;-0;0}"
            };
        }

        if (change.Length == 2)
        {
            var oldShort = BitConverter.ToInt16(change.OldBytes, 0);
            var newShort = BitConverter.ToInt16(change.NewBytes, 0);

            return new ChangeInterpretation
            {
                Type = "Int16",
                OldValue = oldShort.ToString(),
                NewValue = newShort.ToString(),
                Delta = $"{newShort - oldShort:+0;-0;0}"
            };
        }

        if (change.Length == 1)
        {
            var oldByte = change.OldBytes[0];
            var newByte = change.NewBytes[0];

            // Check for boolean
            if ((oldByte == 0 || oldByte == 1) && (newByte == 0 || newByte == 1))
            {
                return new ChangeInterpretation
                {
                    Type = "Boolean",
                    OldValue = oldByte == 1 ? "true" : "false",
                    NewValue = newByte == 1 ? "true" : "false"
                };
            }

            return new ChangeInterpretation
            {
                Type = "Byte",
                OldValue = $"0x{oldByte:X2} ({oldByte})",
                NewValue = $"0x{newByte:X2} ({newByte})"
            };
        }

        if (change.Length == 8)
        {
            var oldLong = BitConverter.ToInt64(change.OldBytes, 0);
            var newLong = BitConverter.ToInt64(change.NewBytes, 0);

            // Check for coordinate pair
            var oldX = BitConverter.ToInt32(change.OldBytes, 0) / 10000.0;
            var oldY = BitConverter.ToInt32(change.OldBytes, 4) / 10000.0;
            var newX = BitConverter.ToInt32(change.NewBytes, 0) / 10000.0;
            var newY = BitConverter.ToInt32(change.NewBytes, 4) / 10000.0;

            if (Math.Abs(oldX) < 100000 && Math.Abs(oldY) < 100000 &&
                Math.Abs(newX) < 100000 && Math.Abs(newY) < 100000)
            {
                return new ChangeInterpretation
                {
                    Type = "CoordPair",
                    OldValue = $"({oldX:F2}, {oldY:F2})mil",
                    NewValue = $"({newX:F2}, {newY:F2})mil",
                    Delta = $"Δ({newX - oldX:+0.##;-0.##;0}, {newY - oldY:+0.##;-0.##;0})mil"
                };
            }

            // Check for double
            var oldDouble = BitConverter.ToDouble(change.OldBytes, 0);
            var newDouble = BitConverter.ToDouble(change.NewBytes, 0);

            if (!double.IsNaN(oldDouble) && !double.IsInfinity(oldDouble) &&
                !double.IsNaN(newDouble) && !double.IsInfinity(newDouble) &&
                Math.Abs(oldDouble) < 1e10 && Math.Abs(newDouble) < 1e10)
            {
                return new ChangeInterpretation
                {
                    Type = "Double",
                    OldValue = oldDouble.ToString("G"),
                    NewValue = newDouble.ToString("G")
                };
            }
        }

        // Check if it's a string change
        if (change.Length >= 3 && IsPrintableAscii(change.OldBytes) && IsPrintableAscii(change.NewBytes))
        {
            return new ChangeInterpretation
            {
                Type = "String",
                OldValue = $"\"{Encoding.ASCII.GetString(change.OldBytes).TrimEnd('\0')}\"",
                NewValue = $"\"{Encoding.ASCII.GetString(change.NewBytes).TrimEnd('\0')}\""
            };
        }

        return null;
    }

    private static bool IsPrintableAscii(byte[] bytes)
    {
        return bytes.All(b => (b >= 0x20 && b < 0x7F) || b == 0);
    }

    public void Dispose()
    {
        _file1?.Close();
        _file2?.Close();
    }
}

#region Result Types

public sealed class BinaryDiffResult
{
    public string File1 { get; init; } = "";
    public string File2 { get; init; } = "";
    public long File1Size { get; init; }
    public long File2Size { get; init; }
    public List<StructuralChange> StructuralChanges { get; init; } = [];
    public List<StreamDiff> StreamDiffs { get; init; } = [];
    public int TotalBytesChanged { get; set; }
    public int TotalStreamsChanged { get; set; }
}

public sealed class StructuralChange
{
    public string Path { get; init; } = "";
    public StructuralChangeType Type { get; init; }
    public string Description { get; init; } = "";
}

public enum StructuralChangeType
{
    Added,
    Removed,
    TypeChanged
}

public sealed class StreamDiff
{
    public string Path { get; init; } = "";
    public int Size1 { get; init; }
    public int Size2 { get; init; }
    public bool IsNew { get; init; }
    public bool IsRemoved { get; init; }
    public int ChangedBytes { get; set; }
    public List<ByteChange> Changes { get; init; } = [];

    public bool HasChanges => IsNew || IsRemoved || Changes.Count > 0;
    public int SizeDelta => Size2 - Size1;
}

public sealed class ByteChange
{
    public int Offset { get; init; }
    public int Length { get; init; }
    public byte[] OldBytes { get; init; } = [];
    public byte[] NewBytes { get; init; } = [];
    public byte[] ContextBefore { get; init; } = [];
    public byte[] ContextAfter { get; init; } = [];
    public ChangeInterpretation? Interpretation { get; set; }
}

public sealed class ChangeInterpretation
{
    public string Type { get; init; } = "";
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
    public string? Delta { get; init; }
}

#endregion
