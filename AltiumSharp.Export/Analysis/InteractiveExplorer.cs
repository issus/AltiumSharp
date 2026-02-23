using System.Text;
using OpenMcdf;
using OriginalCircuit.AltiumSharp.Export.Models;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Interactive REPL-style explorer for Altium files.
/// Allows navigation and inspection of file structure.
/// </summary>
public sealed class InteractiveExplorer : IDisposable
{
    private CompoundFile? _compoundFile;
    private string? _currentPath;
    private readonly Stack<string> _pathHistory = new();
    #pragma warning disable CS0414 // Assigned but never used — reserved for future caching
    private ExportResult? _cachedExport;
    #pragma warning restore CS0414
    private readonly BinaryFieldAnalyzer _binaryAnalyzer = new();
    private readonly UnknownFieldTracker _unknownTracker = new();

    public string? LoadedFile { get; private set; }
    public string CurrentPath => _currentPath ?? "/";
    public bool IsFileLoaded => _compoundFile != null;

    /// <summary>
    /// Load an Altium file for exploration.
    /// </summary>
    public ExplorerResult Load(string filePath)
    {
        try
        {
            Unload();
            _compoundFile = new CompoundFile(filePath);
            LoadedFile = filePath;
            _currentPath = "/";
            _cachedExport = null;

            return new ExplorerResult
            {
                Success = true,
                Message = $"Loaded: {Path.GetFileName(filePath)}",
                CurrentPath = "/",
                AvailableItems = GetCurrentItems()
            };
        }
        catch (Exception ex)
        {
            return new ExplorerResult
            {
                Success = false,
                Message = $"Failed to load file: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Unload the current file.
    /// </summary>
    public void Unload()
    {
        _compoundFile?.Close();
        _compoundFile = null;
        LoadedFile = null;
        _currentPath = null;
        _pathHistory.Clear();
        _cachedExport = null;
    }

    /// <summary>
    /// Navigate to a storage or stream.
    /// </summary>
    public ExplorerResult Navigate(string path)
    {
        if (_compoundFile == null)
        {
            return new ExplorerResult { Success = false, Message = "No file loaded" };
        }

        try
        {
            // Handle special paths
            if (path == "..")
            {
                return NavigateUp();
            }

            if (path == "/")
            {
                _pathHistory.Push(_currentPath ?? "/");
                _currentPath = "/";
                return new ExplorerResult
                {
                    Success = true,
                    CurrentPath = "/",
                    AvailableItems = GetCurrentItems()
                };
            }

            // Resolve relative path
            var targetPath = path.StartsWith("/")
                ? path
                : (_currentPath == "/" ? "/" + path : _currentPath + "/" + path);

            // Try to find the target
            var storage = TryGetStorage(targetPath);
            if (storage != null)
            {
                _pathHistory.Push(_currentPath ?? "/");
                _currentPath = targetPath;
                return new ExplorerResult
                {
                    Success = true,
                    CurrentPath = _currentPath,
                    AvailableItems = GetCurrentItems()
                };
            }

            var stream = TryGetStream(targetPath);
            if (stream != null)
            {
                return new ExplorerResult
                {
                    Success = true,
                    Message = $"Stream: {path} ({stream.Size} bytes)",
                    CurrentPath = _currentPath ?? "/",
                    StreamInfo = GetStreamInfo(stream, targetPath)
                };
            }

            return new ExplorerResult
            {
                Success = false,
                Message = $"Path not found: {path}"
            };
        }
        catch (Exception ex)
        {
            return new ExplorerResult
            {
                Success = false,
                Message = $"Navigation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Navigate up one level.
    /// </summary>
    public ExplorerResult NavigateUp()
    {
        if (_currentPath == "/" || string.IsNullOrEmpty(_currentPath))
        {
            return new ExplorerResult
            {
                Success = true,
                Message = "Already at root",
                CurrentPath = "/",
                AvailableItems = GetCurrentItems()
            };
        }

        var lastSlash = _currentPath.LastIndexOf('/');
        var newPath = lastSlash <= 0 ? "/" : _currentPath[..lastSlash];

        _pathHistory.Push(_currentPath);
        _currentPath = newPath;

        return new ExplorerResult
        {
            Success = true,
            CurrentPath = _currentPath,
            AvailableItems = GetCurrentItems()
        };
    }

    /// <summary>
    /// Go back in navigation history.
    /// </summary>
    public ExplorerResult Back()
    {
        if (_pathHistory.Count == 0)
        {
            return new ExplorerResult
            {
                Success = false,
                Message = "No history to go back to"
            };
        }

        _currentPath = _pathHistory.Pop();
        return new ExplorerResult
        {
            Success = true,
            CurrentPath = _currentPath,
            AvailableItems = GetCurrentItems()
        };
    }

    /// <summary>
    /// List contents at current path.
    /// </summary>
    public ExplorerResult List()
    {
        if (_compoundFile == null)
        {
            return new ExplorerResult { Success = false, Message = "No file loaded" };
        }

        return new ExplorerResult
        {
            Success = true,
            CurrentPath = _currentPath ?? "/",
            AvailableItems = GetCurrentItems()
        };
    }

    /// <summary>
    /// Read a stream's content with optional analysis.
    /// </summary>
    public ExplorerResult Read(string? streamName = null, bool analyze = false)
    {
        if (_compoundFile == null)
        {
            return new ExplorerResult { Success = false, Message = "No file loaded" };
        }

        try
        {
            string targetPath;
            if (streamName != null)
            {
                targetPath = streamName.StartsWith("/")
                    ? streamName
                    : (_currentPath == "/" ? "/" + streamName : _currentPath + "/" + streamName);
            }
            else
            {
                return new ExplorerResult { Success = false, Message = "Stream name required" };
            }

            var stream = TryGetStream(targetPath);
            if (stream == null)
            {
                return new ExplorerResult { Success = false, Message = $"Stream not found: {targetPath}" };
            }

            var info = GetStreamInfo(stream, targetPath);

            if (analyze && info.RawData != null)
            {
                info.BinaryAnalysis = _binaryAnalyzer.Analyze(info.RawData, targetPath);
            }

            return new ExplorerResult
            {
                Success = true,
                CurrentPath = _currentPath ?? "/",
                StreamInfo = info
            };
        }
        catch (Exception ex)
        {
            return new ExplorerResult
            {
                Success = false,
                Message = $"Read error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Search for a pattern in all streams.
    /// </summary>
    public ExplorerResult Search(string pattern, bool caseSensitive = false)
    {
        if (_compoundFile == null)
        {
            return new ExplorerResult { Success = false, Message = "No file loaded" };
        }

        var results = new List<SearchResult>();
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        SearchStorage(_compoundFile.RootStorage, "/", pattern, comparison, results);

        return new ExplorerResult
        {
            Success = true,
            Message = $"Found {results.Count} matches for '{pattern}'",
            CurrentPath = _currentPath ?? "/",
            SearchResults = results
        };
    }

    /// <summary>
    /// Get unknown fields for current location.
    /// </summary>
    public ExplorerResult GetUnknownFields(string? streamName = null)
    {
        if (_compoundFile == null)
        {
            return new ExplorerResult { Success = false, Message = "No file loaded" };
        }

        try
        {
            var readResult = Read(streamName);
            if (!readResult.Success || readResult.StreamInfo?.Parameters == null)
            {
                return new ExplorerResult
                {
                    Success = false,
                    Message = "Could not read stream parameters"
                };
            }

            var isPcb = LoadedFile?.EndsWith(".PcbLib", StringComparison.OrdinalIgnoreCase) == true ||
                        LoadedFile?.EndsWith(".PcbDoc", StringComparison.OrdinalIgnoreCase) == true;

            var report = _unknownTracker.AnalyzeParameters(
                readResult.StreamInfo.Parameters,
                readResult.StreamInfo.Path,
                isPcb);

            return new ExplorerResult
            {
                Success = true,
                Message = $"Found {report.UnknownParameters.Count} unknown fields ({report.UnknownPercentage:F1}%)",
                CurrentPath = _currentPath ?? "/",
                UnknownFieldReport = report
            };
        }
        catch (Exception ex)
        {
            return new ExplorerResult
            {
                Success = false,
                Message = $"Analysis error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Dump hex content of a stream.
    /// </summary>
    public ExplorerResult HexDump(string streamName, int offset = 0, int length = 256)
    {
        var readResult = Read(streamName, analyze: false);
        if (!readResult.Success || readResult.StreamInfo?.RawData == null)
        {
            return readResult;
        }

        var data = readResult.StreamInfo.RawData;
        var actualOffset = Math.Min(offset, data.Length);
        var actualLength = Math.Min(length, data.Length - actualOffset);

        var sb = new StringBuilder();
        for (int i = actualOffset; i < actualOffset + actualLength; i += 16)
        {
            sb.Append($"{i:X8}  ");

            // Hex part
            for (int j = 0; j < 16; j++)
            {
                if (i + j < data.Length)
                {
                    sb.Append($"{data[i + j]:X2} ");
                }
                else
                {
                    sb.Append("   ");
                }
                if (j == 7) sb.Append(' ');
            }

            sb.Append(" |");

            // ASCII part
            for (int j = 0; j < 16 && i + j < data.Length; j++)
            {
                var b = data[i + j];
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }

            sb.AppendLine("|");
        }

        return new ExplorerResult
        {
            Success = true,
            Message = sb.ToString(),
            CurrentPath = _currentPath ?? "/"
        };
    }

    /// <summary>
    /// Get file statistics and summary.
    /// </summary>
    public ExplorerResult GetStats()
    {
        if (_compoundFile == null)
        {
            return new ExplorerResult { Success = false, Message = "No file loaded" };
        }

        var stats = new FileStats
        {
            FileName = Path.GetFileName(LoadedFile ?? ""),
            FileSize = new FileInfo(LoadedFile!).Length
        };

        CountStorageContents(_compoundFile.RootStorage, stats);

        return new ExplorerResult
        {
            Success = true,
            CurrentPath = _currentPath ?? "/",
            Stats = stats
        };
    }

    private List<ExplorerItem> GetCurrentItems()
    {
        var items = new List<ExplorerItem>();
        if (_compoundFile == null) return items;

        var storage = _currentPath == "/" || string.IsNullOrEmpty(_currentPath)
            ? _compoundFile.RootStorage
            : TryGetStorage(_currentPath);

        if (storage == null) return items;

        // Add parent navigation if not at root
        if (_currentPath != "/" && !string.IsNullOrEmpty(_currentPath))
        {
            items.Add(new ExplorerItem { Name = "..", Type = ItemType.Parent });
        }

        // Add storages
        storage.VisitEntries(entry =>
        {
            if (entry.IsStorage)
            {
                items.Add(new ExplorerItem
                {
                    Name = entry.Name,
                    Type = ItemType.Storage
                });
            }
        }, recursive: false);

        // Add streams
        storage.VisitEntries(entry =>
        {
            if (entry.IsStream)
            {
                items.Add(new ExplorerItem
                {
                    Name = entry.Name,
                    Type = ItemType.Stream,
                    Size = (int)entry.Size
                });
            }
        }, recursive: false);

        return items;
    }

    private CFStorage? TryGetStorage(string path)
    {
        if (_compoundFile == null) return null;
        if (path == "/" || string.IsNullOrEmpty(path)) return _compoundFile.RootStorage;

        var parts = path.TrimStart('/').Split('/');
        var current = _compoundFile.RootStorage;

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            try
            {
                current = current.GetStorage(part);
            }
            catch
            {
                return null;
            }
        }

        return current;
    }

    private CFStream? TryGetStream(string path)
    {
        if (_compoundFile == null) return null;

        var lastSlash = path.LastIndexOf('/');
        var storagePath = lastSlash <= 0 ? "/" : path[..lastSlash];
        var streamName = path[(lastSlash + 1)..];

        var storage = TryGetStorage(storagePath);
        if (storage == null) return null;

        try
        {
            return storage.GetStream(streamName);
        }
        catch
        {
            return null;
        }
    }

    private StreamInfo GetStreamInfo(CFStream stream, string path)
    {
        var data = stream.GetData();
        var info = new StreamInfo
        {
            Path = path,
            Name = stream.Name,
            Size = (int)stream.Size,
            RawData = data
        };

        // Try to parse as parameters
        if (data.Length > 0)
        {
            try
            {
                var text = Encoding.GetEncoding(1252).GetString(data);
                if (text.Contains('|') && text.Contains('='))
                {
                    info.ContentType = "Parameters";
                    info.Parameters = ParseParameters(text);
                }
                else if (IsPrintableText(data))
                {
                    info.ContentType = "Text";
                    info.TextContent = text.TrimEnd('\0');
                }
                else
                {
                    info.ContentType = "Binary";
                }
            }
            catch
            {
                info.ContentType = "Binary";
            }
        }

        return info;
    }

    private Dictionary<string, string> ParseParameters(string text)
    {
        var result = new Dictionary<string, string>();
        var entries = text.Split('|', StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            var eqIndex = entry.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = entry[..eqIndex].TrimEnd('\0', '\r', '\n');
                var value = entry[(eqIndex + 1)..].TrimEnd('\0', '\r', '\n');

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

        return result;
    }

    private void SearchStorage(CFStorage storage, string path, string pattern, StringComparison comparison, List<SearchResult> results)
    {
        storage.VisitEntries(entry =>
        {
            var entryPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            if (entry.IsStorage)
            {
                if (entry.Name.Contains(pattern, comparison))
                {
                    results.Add(new SearchResult { Path = entryPath, Type = "Storage", Match = entry.Name });
                }
                SearchStorage(storage.GetStorage(entry.Name), entryPath, pattern, comparison, results);
            }
            else if (entry.IsStream)
            {
                if (entry.Name.Contains(pattern, comparison))
                {
                    results.Add(new SearchResult { Path = entryPath, Type = "Stream (name)", Match = entry.Name });
                }

                // Search in stream content
                try
                {
                    var stream = storage.GetStream(entry.Name);
                    var data = stream.GetData();
                    var text = Encoding.GetEncoding(1252).GetString(data);

                    if (text.Contains(pattern, comparison))
                    {
                        var matchIndex = text.IndexOf(pattern, comparison);
                        var start = Math.Max(0, matchIndex - 20);
                        var end = Math.Min(text.Length, matchIndex + pattern.Length + 20);
                        var context = text[start..end].Replace('\0', ' ').Replace('\n', ' ');

                        results.Add(new SearchResult
                        {
                            Path = entryPath,
                            Type = "Stream (content)",
                            Match = context
                        });
                    }
                }
                catch { }
            }
        }, recursive: false);
    }

    private void CountStorageContents(CFStorage storage, FileStats stats)
    {
        storage.VisitEntries(entry =>
        {
            if (entry.IsStorage)
            {
                stats.StorageCount++;
                CountStorageContents(storage.GetStorage(entry.Name), stats);
            }
            else if (entry.IsStream)
            {
                stats.StreamCount++;
                stats.TotalStreamSize += entry.Size;
            }
        }, recursive: false);
    }

    private static bool IsPrintableText(byte[] data)
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

    public void Dispose()
    {
        Unload();
    }
}

#region Explorer Types

public sealed class ExplorerResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? CurrentPath { get; init; }
    public List<ExplorerItem>? AvailableItems { get; init; }
    public StreamInfo? StreamInfo { get; init; }
    public List<SearchResult>? SearchResults { get; init; }
    public UnknownFieldReport? UnknownFieldReport { get; init; }
    public FileStats? Stats { get; init; }
}

public sealed class ExplorerItem
{
    public string Name { get; init; } = "";
    public ItemType Type { get; init; }
    public int Size { get; init; }
}

public enum ItemType
{
    Parent,
    Storage,
    Stream
}

public sealed class StreamInfo
{
    public string Path { get; init; } = "";
    public string Name { get; init; } = "";
    public int Size { get; init; }
    public string ContentType { get; set; } = "Unknown";
    public byte[]? RawData { get; init; }
    public Dictionary<string, string>? Parameters { get; set; }
    public string? TextContent { get; set; }
    public BinaryAnalysisResult? BinaryAnalysis { get; set; }
}

public sealed class SearchResult
{
    public string Path { get; init; } = "";
    public string Type { get; init; } = "";
    public string Match { get; init; } = "";
}

public sealed class FileStats
{
    public string FileName { get; init; } = "";
    public long FileSize { get; init; }
    public int StorageCount { get; set; }
    public int StreamCount { get; set; }
    public long TotalStreamSize { get; set; }
}

#endregion
