using System.IO.Compression;
using System.Text;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp.Export.Analysis;

/// <summary>
/// Handles detection and decompression of compressed data in Altium files.
/// Supports zlib (common in Altium), as well as detection of other compression formats.
/// </summary>
public sealed class CompressionHandler
{
    /// <summary>
    /// Detect compression type in binary data.
    /// </summary>
    public CompressionDetectionResult DetectCompression(byte[] data)
    {
        var result = new CompressionDetectionResult
        {
            OriginalSize = data.Length
        };

        if (data.Length < 2) return result;

        // Check for zlib header (common in Altium)
        // zlib: first byte = CMF (usually 0x78), second byte = FLG
        // Common combinations: 78 01, 78 5E, 78 9C, 78 DA
        if (data[0] == 0x78)
        {
            var flg = data[1];
            if (flg == 0x01 || flg == 0x5E || flg == 0x9C || flg == 0xDA)
            {
                result.CompressionType = CompressionType.Zlib;
                result.IsCompressed = true;
                result.HeaderOffset = 0;

                // Determine compression level from header
                result.CompressionLevel = flg switch
                {
                    0x01 => "No compression/low",
                    0x5E => "Fast",
                    0x9C => "Default",
                    0xDA => "Best",
                    _ => "Unknown"
                };

                return result;
            }
        }

        // Check for gzip header: 1F 8B
        if (data[0] == 0x1F && data[1] == 0x8B)
        {
            result.CompressionType = CompressionType.Gzip;
            result.IsCompressed = true;
            result.HeaderOffset = 0;
            return result;
        }

        // Check for deflate raw (no header) - harder to detect
        // Try to decompress and see if it works
        if (TryDeflateRaw(data, out _))
        {
            result.CompressionType = CompressionType.DeflateRaw;
            result.IsCompressed = true;
            result.HeaderOffset = 0;
            return result;
        }

        // Check for compression embedded after a size prefix (common Altium pattern)
        if (data.Length >= 6)
        {
            var sizeField = BitConverter.ToInt32(data, 0);
            var size = sizeField & 0x00FFFFFF;

            if (size > 0 && size < data.Length - 4 && data[4] == 0x78)
            {
                var flg = data[5];
                if (flg == 0x01 || flg == 0x5E || flg == 0x9C || flg == 0xDA)
                {
                    result.CompressionType = CompressionType.ZlibWithSizePrefix;
                    result.IsCompressed = true;
                    result.HeaderOffset = 4;
                    result.CompressedSize = size;
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Decompress data if it's compressed.
    /// </summary>
    public DecompressionResult Decompress(byte[] data)
    {
        var detection = DetectCompression(data);
        if (!detection.IsCompressed)
        {
            return new DecompressionResult
            {
                Success = true,
                WasCompressed = false,
                Data = data
            };
        }

        return DecompressWithType(data, detection);
    }

    /// <summary>
    /// Decompress with known compression type.
    /// </summary>
    public DecompressionResult DecompressWithType(byte[] data, CompressionDetectionResult detection)
    {
        var result = new DecompressionResult
        {
            WasCompressed = true,
            CompressionType = detection.CompressionType,
            CompressedSize = data.Length
        };

        try
        {
            byte[] compressedData;
            if (detection.HeaderOffset > 0)
            {
                compressedData = new byte[data.Length - detection.HeaderOffset];
                Array.Copy(data, detection.HeaderOffset, compressedData, 0, compressedData.Length);
            }
            else
            {
                compressedData = data;
            }

            byte[]? decompressed = detection.CompressionType switch
            {
                CompressionType.Zlib => DecompressZlib(compressedData),
                CompressionType.ZlibWithSizePrefix => DecompressZlib(compressedData),
                CompressionType.Gzip => DecompressGzip(compressedData),
                CompressionType.DeflateRaw => DecompressDeflate(compressedData),
                _ => null
            };

            if (decompressed != null)
            {
                result.Success = true;
                result.Data = decompressed;
                result.DecompressedSize = decompressed.Length;
                result.CompressionRatio = (double)data.Length / decompressed.Length;
            }
            else
            {
                result.Success = false;
                result.Error = "Decompression failed";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Scan a file for all compressed sections.
    /// </summary>
    public CompressedSectionScan ScanFile(string filePath)
    {
        var scan = new CompressedSectionScan
        {
            FileName = Path.GetFileName(filePath)
        };

        using var cf = new CompoundFile(filePath);
        ScanStorage(cf.RootStorage, "/", scan);

        scan.TotalCompressedBytes = scan.Sections.Sum(s => s.CompressedSize);
        scan.TotalDecompressedBytes = scan.Sections.Sum(s => s.DecompressedSize);

        return scan;
    }

    /// <summary>
    /// Compress data using zlib.
    /// </summary>
    public byte[] CompressZlib(byte[] data, CompressionLevel level = CompressionLevel.Optimal)
    {
        using var output = new MemoryStream();

        // Write zlib header
        byte cmf = 0x78; // deflate, 32K window
        byte flg = level switch
        {
            CompressionLevel.NoCompression => 0x01,
            CompressionLevel.Fastest => 0x5E,
            CompressionLevel.Optimal => 0x9C,
            CompressionLevel.SmallestSize => 0xDA,
            _ => 0x9C
        };

        // Adjust FLG for checksum
        int check = (cmf * 256 + flg) % 31;
        if (check != 0) flg += (byte)(31 - check);

        output.WriteByte(cmf);
        output.WriteByte(flg);

        // Compress with deflate
        using (var deflate = new DeflateStream(output, level, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }

        // Calculate and write Adler-32 checksum
        var adler = CalculateAdler32(data);
        output.WriteByte((byte)(adler >> 24));
        output.WriteByte((byte)(adler >> 16));
        output.WriteByte((byte)(adler >> 8));
        output.WriteByte((byte)adler);

        return output.ToArray();
    }

    private void ScanStorage(CFStorage storage, string path, CompressedSectionScan scan)
    {
        storage.VisitEntries(entry =>
        {
            var entryPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            if (entry.IsStorage)
            {
                ScanStorage(storage.GetStorage(entry.Name), entryPath, scan);
            }
            else if (entry.IsStream)
            {
                var stream = storage.GetStream(entry.Name);
                var data = stream.GetData();

                // Check for compression in the stream
                var detection = DetectCompression(data);
                if (detection.IsCompressed)
                {
                    var section = new CompressedSection
                    {
                        Path = entryPath,
                        Offset = detection.HeaderOffset,
                        CompressedSize = data.Length - detection.HeaderOffset,
                        CompressionType = detection.CompressionType
                    };

                    // Try to decompress to get decompressed size
                    var decompResult = DecompressWithType(data, detection);
                    if (decompResult.Success)
                    {
                        section.DecompressedSize = decompResult.DecompressedSize;
                        section.CompressionRatio = decompResult.CompressionRatio;
                        section.IsValid = true;
                    }

                    scan.Sections.Add(section);
                }

                // Also scan within the stream for embedded compressed blocks
                ScanForEmbeddedCompression(data, entryPath, scan);
            }
        }, recursive: false);
    }

    private void ScanForEmbeddedCompression(byte[] data, string streamPath, CompressedSectionScan scan)
    {
        // Look for zlib headers within the data
        for (int i = 0; i < data.Length - 6; i++)
        {
            if (data[i] == 0x78)
            {
                var flg = data[i + 1];
                if (flg == 0x01 || flg == 0x5E || flg == 0x9C || flg == 0xDA)
                {
                    // Found potential zlib header, try to decompress
                    var remaining = data.Length - i;
                    var testData = new byte[remaining];
                    Array.Copy(data, i, testData, 0, remaining);

                    try
                    {
                        var decompressed = DecompressZlib(testData);
                        if (decompressed != null && decompressed.Length > 0)
                        {
                            // Don't add if this is the start of the stream (already detected)
                            if (i > 0)
                            {
                                scan.Sections.Add(new CompressedSection
                                {
                                    Path = $"{streamPath}@{i:X}",
                                    Offset = i,
                                    CompressedSize = remaining,
                                    DecompressedSize = decompressed.Length,
                                    CompressionType = CompressionType.Zlib,
                                    CompressionRatio = (double)remaining / decompressed.Length,
                                    IsEmbedded = true,
                                    IsValid = true
                                });
                            }

                            // Skip past this compressed block
                            i += remaining - 1;
                        }
                    }
                    catch
                    {
                        // Not valid compressed data, continue scanning
                    }
                }
            }
        }
    }

    private byte[]? DecompressZlib(byte[] data)
    {
        if (data.Length < 6) return null;

        try
        {
            // Skip 2-byte zlib header
            using var input = new MemoryStream(data, 2, data.Length - 6); // Also skip 4-byte Adler-32 footer
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            deflate.CopyTo(output);
            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private byte[]? DecompressGzip(byte[] data)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            gzip.CopyTo(output);
            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private byte[]? DecompressDeflate(byte[] data)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            deflate.CopyTo(output);
            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private bool TryDeflateRaw(byte[] data, out byte[]? decompressed)
    {
        decompressed = null;
        if (data.Length < 2) return false;

        try
        {
            using var input = new MemoryStream(data);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            deflate.CopyTo(output);
            decompressed = output.ToArray();
            return decompressed.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static uint CalculateAdler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (byte c in data)
        {
            a = (a + c) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }
}

#region Result Types

public sealed class CompressionDetectionResult
{
    public bool IsCompressed { get; set; }
    public CompressionType CompressionType { get; set; }
    public int HeaderOffset { get; set; }
    public int CompressedSize { get; set; }
    public int OriginalSize { get; set; }
    public string? CompressionLevel { get; set; }
}

public enum CompressionType
{
    None,
    Zlib,
    ZlibWithSizePrefix,
    Gzip,
    DeflateRaw
}

public sealed class DecompressionResult
{
    public bool Success { get; set; }
    public bool WasCompressed { get; set; }
    public CompressionType CompressionType { get; set; }
    public string? Error { get; set; }
    public byte[]? Data { get; set; }
    public int CompressedSize { get; set; }
    public int DecompressedSize { get; set; }
    public double CompressionRatio { get; set; }
}

public sealed class CompressedSectionScan
{
    public string FileName { get; init; } = "";
    public List<CompressedSection> Sections { get; init; } = [];
    public long TotalCompressedBytes { get; set; }
    public long TotalDecompressedBytes { get; set; }
}

public sealed class CompressedSection
{
    public string Path { get; init; } = "";
    public int Offset { get; init; }
    public int CompressedSize { get; set; }
    public int DecompressedSize { get; set; }
    public CompressionType CompressionType { get; init; }
    public double CompressionRatio { get; set; }
    public bool IsEmbedded { get; init; }
    public bool IsValid { get; set; }
}

#endregion
