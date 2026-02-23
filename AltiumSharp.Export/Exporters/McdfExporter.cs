using System.Security.Cryptography;
using System.Text;
using OpenMcdf;
using OriginalCircuit.AltiumSharp.Export.Models;

namespace OriginalCircuit.AltiumSharp.Export.Exporters;

/// <summary>
/// Exports the raw MCDF (COM/OLE Structured Storage) structure of an Altium file.
/// </summary>
public sealed class McdfExporter : IDisposable
{
    private readonly ExportOptions _options;
    private readonly List<string> _warnings = [];
    private CompoundFile? _compoundFile;

    /// <summary>
    /// Warnings generated during export.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    public McdfExporter(ExportOptions? options = null)
    {
        _options = options ?? new ExportOptions();
    }

    /// <summary>
    /// Export an Altium file to an ExportResult.
    /// </summary>
    public ExportResult Export(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var metadata = CreateMetadata(fileInfo);
        var result = new ExportResult { Metadata = metadata };

        try
        {
            _compoundFile = new CompoundFile(filePath);

            if (_options.IncludeRawMcdfStructure)
            {
                result.RawMcdf = ExportMcdfStructure();
            }

            // Detect file version from FileHeader
            DetectFileVersion(metadata);
        }
        finally
        {
            _compoundFile?.Close();
            _compoundFile = null;
        }

        metadata.Warnings.AddRange(_warnings);
        return result;
    }

    /// <summary>
    /// Export an Altium file from a stream.
    /// </summary>
    public ExportResult Export(Stream stream, string fileName = "unknown")
    {
        var metadata = new ExportMetadata
        {
            SourceFileName = fileName,
            SourceFileSize = stream.Length,
            SourceFileHash = ComputeHash(stream)
        };
        stream.Position = 0;

        var result = new ExportResult { Metadata = metadata };

        try
        {
            _compoundFile = new CompoundFile(stream);

            if (_options.IncludeRawMcdfStructure)
            {
                result.RawMcdf = ExportMcdfStructure();
            }

            DetectFileVersion(metadata);
        }
        finally
        {
            _compoundFile?.Close();
            _compoundFile = null;
        }

        metadata.Warnings.AddRange(_warnings);
        return result;
    }

    private ExportMetadata CreateMetadata(FileInfo fileInfo)
    {
        using var stream = fileInfo.OpenRead();
        var hash = ComputeHash(stream);

        return new ExportMetadata
        {
            SourceFileName = fileInfo.Name,
            SourceFileSize = fileInfo.Length,
            SourceFileHash = hash
        };
    }

    private static string ComputeHash(Stream stream)
    {
        var hash = SHA256.HashData(stream);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private McdfStructure ExportMcdfStructure()
    {
        if (_compoundFile == null)
            throw new InvalidOperationException("Compound file not open");

        return new McdfStructure
        {
            RootStorage = ExportStorage(_compoundFile.RootStorage)
        };
    }

    private McdfStorage ExportStorage(CFStorage storage)
    {
        var result = new McdfStorage
        {
            Name = storage.Name
        };

        storage.VisitEntries(item =>
        {
            if (item is CFStorage childStorage)
            {
                result.Storages.Add(ExportStorage(childStorage));
            }
            else if (item is CFStream childStream)
            {
                result.Streams.Add(ExportStream(childStream));
            }
        }, recursive: false);

        return result;
    }

    private McdfStream ExportStream(CFStream stream)
    {
        byte[] data;
        try
        {
            data = stream.GetData();
        }
        catch (Exception ex)
        {
            _warnings.Add($"Failed to read stream '{stream.Name}': {ex.Message}");
            return new McdfStream
            {
                Name = stream.Name,
                Size = 0,
                Content = new McdfStreamContent
                {
                    InterpretedAs = "Error",
                    Notes = ex.Message
                }
            };
        }

        return new McdfStream
        {
            Name = stream.Name,
            Size = data.Length,
            Content = InterpretStreamContent(stream.Name, data)
        };
    }

    private McdfStreamContent InterpretStreamContent(string name, byte[] data)
    {
        if (data.Length == 0)
        {
            return new McdfStreamContent
            {
                InterpretedAs = "Empty"
            };
        }

        // Try to interpret as parameters (most common for Altium files)
        if (TryParseAsParameters(data, out var parameters))
        {
            return new McdfStreamContent
            {
                InterpretedAs = "Parameters",
                Parameters = parameters,
                RawDataBase64 = _options.IncludeRawData ? Convert.ToBase64String(data) : null
            };
        }

        // Check for known binary stream patterns
        if (name == "Header" && data.Length == 4)
        {
            // Typical header is just a record count
            int recordCount = BitConverter.ToInt32(data, 0);
            return new McdfStreamContent
            {
                InterpretedAs = "Binary",
                BinaryFields =
                [
                    new BinaryFieldInfo
                    {
                        Offset = 0,
                        Size = 4,
                        Type = "int32",
                        Value = recordCount,
                        FieldName = "recordCount"
                    }
                ],
                RawDataBase64 = _options.IncludeRawData ? Convert.ToBase64String(data) : null
            };
        }

        // Check if it's text data
        if (IsTextData(data, out var text))
        {
            return new McdfStreamContent
            {
                InterpretedAs = "Text",
                Text = text
            };
        }

        // Default to binary
        return new McdfStreamContent
        {
            InterpretedAs = "Binary",
            RawDataBase64 = _options.IncludeRawData ? Convert.ToBase64String(data) : null,
            Notes = $"Unrecognized binary data ({data.Length} bytes)"
        };
    }

    private static bool TryParseAsParameters(byte[] data, out Dictionary<string, string> parameters)
    {
        parameters = [];

        try
        {
            // Altium uses Windows-1252 encoding typically
            var encoding = Encoding.GetEncoding(1252);
            var text = encoding.GetString(data);

            // Check if it looks like parameter data (starts with | or contains |KEY=VALUE patterns)
            if (!text.Contains('|') || !text.Contains('='))
            {
                return false;
            }

            // Parse pipe-delimited entries
            var entries = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
            int validCount = 0;

            foreach (var entry in entries)
            {
                var parts = entry.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim('\r', '\n', '\0');
                    var value = parts[1].Trim('\r', '\n', '\0');

                    // Handle UTF8 prefixed keys
                    if (key.StartsWith("%UTF8%", StringComparison.OrdinalIgnoreCase))
                    {
                        key = key.Substring(6);
                        // Convert from Windows-1252 bytes that represent UTF-8
                        var bytes = encoding.GetBytes(value);
                        value = Encoding.UTF8.GetString(bytes);
                    }

                    // Handle duplicate keys by appending suffix
                    var finalKey = key;
                    int suffix = 2;
                    while (parameters.ContainsKey(finalKey))
                    {
                        finalKey = $"{key}_{suffix}";
                        suffix++;
                    }

                    parameters[finalKey] = value;
                    validCount++;
                }
            }

            // Require at least some valid entries
            return validCount > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTextData(byte[] data, out string text)
    {
        text = string.Empty;

        try
        {
            // Check if data is primarily printable ASCII/UTF-8
            int printableCount = 0;
            foreach (byte b in data)
            {
                if (b >= 32 && b < 127 || b == '\r' || b == '\n' || b == '\t')
                {
                    printableCount++;
                }
            }

            // If more than 90% printable, treat as text
            if (printableCount > data.Length * 0.9)
            {
                text = Encoding.UTF8.GetString(data).TrimEnd('\0');
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void DetectFileVersion(ExportMetadata metadata)
    {
        if (_compoundFile == null) return;

        try
        {
            // Try to read FileHeader stream for version info
            if (_compoundFile.RootStorage.TryGetStream("FileHeader", out var headerStream))
            {
                var data = headerStream.GetData();
                if (TryParseAsParameters(data, out var parameters))
                {
                    if (parameters.TryGetValue("VERSION", out var version))
                    {
                        metadata = metadata with { FileVersionString = version };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _warnings.Add($"Failed to detect file version: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _compoundFile?.Close();
        _compoundFile = null;
    }
}
