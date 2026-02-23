using System.IO.Compression;
using System.Text;
using OpenMcdf;
using Xunit;
using Xunit.Abstractions;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

public class ChecksumAnalysisTest
{
    private readonly ITestOutputHelper _output;

    public ChecksumAnalysisTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public void FocusedCompressedDataAnalysis()
    {
        var testDataDir = GetDataPath("TestData");
        if (!Directory.Exists(testDataDir)) { Skip.If(true, "Test data not available"); return; }

        var allFiles = Directory.GetFiles(testDataDir, "*.PcbLib", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(testDataDir, "*.PcbDoc", SearchOption.AllDirectories))
            .ToArray();

        // Collect unique models (by ID) to avoid duplicates
        var seenIds = new HashSet<string>();
        var models = new List<(string file, string name, string id, int checksum, byte[] compressed, byte[] decompressed)>();

        foreach (var file in allFiles)
        {
            try
            {
                using var fs = File.OpenRead(file);
                using var cf = new CompoundFile(fs);

                CFStorage? modelsStorage = null;
                CFStorage? libStorage = null;
                cf.RootStorage.VisitEntries(e => { if (e.Name == "Library" && e is CFStorage s) libStorage = s; }, false);
                if (libStorage != null)
                    libStorage.VisitEntries(e => { if (e.Name == "Models" && e is CFStorage s) modelsStorage = s; }, false);
                if (modelsStorage == null)
                    cf.RootStorage.VisitEntries(e => { if (e.Name == "Models" && e is CFStorage s) modelsStorage = s; }, false);
                if (modelsStorage == null) continue;

                if (!modelsStorage.TryGetStream("Data", out var dataStream)) continue;
                var dataBytes = dataStream.GetData();
                if (dataBytes.Length < 4) continue;

                var offset = 0;
                var modelIdx = 0;
                while (offset + 4 <= dataBytes.Length)
                {
                    var paramLen = BitConverter.ToInt32(dataBytes, offset);
                    if (paramLen <= 0 || offset + 4 + paramLen > dataBytes.Length) break;
                    var paramStr = Encoding.ASCII.GetString(dataBytes, offset + 4, paramLen).TrimEnd('\0');
                    offset += 4 + paramLen;

                    string? id = null, name = null, checksumStr = null;
                    foreach (var part in paramStr.Split('|'))
                    {
                        if (part.StartsWith("ID=")) id = part[3..];
                        if (part.StartsWith("NAME=")) name = part[5..];
                        if (part.StartsWith("CHECKSUM=")) checksumStr = part[9..];
                    }

                    if (id != null && checksumStr != null && int.TryParse(checksumStr, out var cs) && !seenIds.Contains(id))
                    {
                        if (modelsStorage.TryGetStream(modelIdx.ToString(), out var modelStream))
                        {
                            var compressed = modelStream.GetData();
                            if (compressed.Length > 0)
                            {
                                try
                                {
                                    using var ms = new MemoryStream(compressed);
                                    using var zs = new ZLibStream(ms, CompressionMode.Decompress);
                                    using var outMs = new MemoryStream();
                                    zs.CopyTo(outMs);
                                    models.Add((Path.GetFileName(file), name ?? "?", id, cs, compressed, outMs.ToArray()));
                                    seenIds.Add(id);
                                }
                                catch { }
                            }
                        }
                    }
                    modelIdx++;
                }
            }
            catch { }
        }

        _output.WriteLine($"Unique models: {models.Count}\n");

        // For each model, compute various checksums and look for relationships
        _output.WriteLine("NAME | STORED | CRC32(comp) | CRC32(step) | Adler32(zlib) | Adler32(step) | compLen | stepLen");
        _output.WriteLine("---|---|---|---|---|---|---|---");

        foreach (var (file, name, id, stored, compressed, decompressed) in models)
        {
            var storedU = unchecked((uint)stored);
            var crc32comp = CRC32(compressed);
            var crc32step = CRC32(decompressed);

            // Extract Adler32 from zlib trailer (last 4 bytes, big-endian)
            uint zlibAdler = 0;
            if (compressed.Length >= 6)
                zlibAdler = (uint)((compressed[^4] << 24) | (compressed[^3] << 16) | (compressed[^2] << 8) | compressed[^1]);

            var adler32step = Adler32(decompressed);

            _output.WriteLine($"{name} | {stored} (0x{storedU:X8}) | 0x{crc32comp:X8} | 0x{crc32step:X8} | 0x{zlibAdler:X8} | 0x{adler32step:X8} | {compressed.Length} | {decompressed.Length}");

            // Check relationships
            CheckMatch($"  {name}: CRC32(comp)", crc32comp, storedU);
            CheckMatch($"  {name}: CRC32(step)", crc32step, storedU);
            CheckMatch($"  {name}: Adler32(zlib)", zlibAdler, storedU);
            CheckMatch($"  {name}: Adler32(step)", adler32step, storedU);

            // XOR relationships
            CheckMatch($"  {name}: CRC32(comp) XOR CRC32(step)", crc32comp ^ crc32step, storedU);
            CheckMatch($"  {name}: CRC32(comp) XOR Adler32", crc32comp ^ zlibAdler, storedU);
            CheckMatch($"  {name}: CRC32(step) XOR Adler32", crc32step ^ zlibAdler, storedU);

            // Addition relationships
            CheckMatch($"  {name}: CRC32(comp) + CRC32(step)", crc32comp + crc32step, storedU);

            // Maybe it's CRC32 of compressed WITHOUT the zlib header (2 bytes) and trailer (4 bytes)
            if (compressed.Length > 6)
            {
                var deflateOnly = compressed[2..^4];
                CheckMatch($"  {name}: CRC32(deflate-body)", CRC32(deflateOnly), storedU);
            }

            // Maybe CRC32 of compressed WITHOUT just the header (2 bytes)
            if (compressed.Length > 2)
            {
                var noHeader = compressed[2..];
                CheckMatch($"  {name}: CRC32(comp-noheader)", CRC32(noHeader), storedU);
            }

            // CRC32 of compressed WITH the length prefix (like Altium stores it)
            var withLen = new byte[4 + compressed.Length];
            BitConverter.GetBytes(compressed.Length).CopyTo(withLen, 0);
            compressed.CopyTo(withLen, 4);
            CheckMatch($"  {name}: CRC32(len+comp)", CRC32(withLen), storedU);

            // Maybe the decompressed length is incorporated somehow
            var stepLenBytes = BitConverter.GetBytes(decompressed.Length);
            var compPlusStepLen = compressed.Concat(stepLenBytes).ToArray();
            CheckMatch($"  {name}: CRC32(comp+steplen)", CRC32(compPlusStepLen), storedU);

            // CRC32 variants with different init/final
            CheckMatch($"  {name}: CRC32C(comp)", CRC32C(compressed), storedU);
            CheckMatch($"  {name}: CRC32C(step)", CRC32C(decompressed), storedU);

            // CRC32 init=0
            CheckMatch($"  {name}: CRC32i0(comp)", CRC32WithInit(compressed, 0), storedU);
            CheckMatch($"  {name}: CRC32i0(step)", CRC32WithInit(decompressed, 0), storedU);

            // CRC32 no final XOR
            CheckMatch($"  {name}: CRC32nf(comp)", CRC32NoFinal(compressed), storedU);
            CheckMatch($"  {name}: CRC32nf(step)", CRC32NoFinal(decompressed), storedU);

            // CRC32 init=0 no final XOR
            CheckMatch($"  {name}: CRC32i0nf(comp)", CRC32WithInitNoFinal(compressed, 0), storedU);
            CheckMatch($"  {name}: CRC32i0nf(step)", CRC32WithInitNoFinal(decompressed, 0), storedU);

            // Maybe it's a zlib crc32 called with initial = compressed length or something weird
            CheckMatch($"  {name}: CRC32i=compLen(comp)", CRC32WithInit(compressed, (uint)compressed.Length), storedU);
            CheckMatch($"  {name}: CRC32i=stepLen(step)", CRC32WithInit(decompressed, (uint)decompressed.Length), storedU);

            // POSIX cksum (CRC32 with length appended)
            CheckMatch($"  {name}: POSIX-cksum(comp)", POSIXCksum(compressed), storedU);
            CheckMatch($"  {name}: POSIX-cksum(step)", POSIXCksum(decompressed), storedU);

            // Maybe it's Adler32 with different modulus or init
            CheckMatch($"  {name}: Adler32(comp)", Adler32(compressed), storedU);

            // Signed interpretation - maybe it's computed with signed arithmetic
            // stored as int32, maybe negative values indicate something

            // Try: hash = sum of all uint16 pairs in compressed data
            uint sum16 = 0;
            for (var i = 0; i + 1 < compressed.Length; i += 2)
                sum16 += (uint)(compressed[i] | (compressed[i + 1] << 8));
            CheckMatch($"  {name}: Sum16(comp)", sum16, storedU);

            // Try: hash = sum of all uint32 groups in compressed data
            uint sum32 = 0;
            for (var i = 0; i + 3 < compressed.Length; i += 4)
                sum32 += BitConverter.ToUInt32(compressed, i);
            CheckMatch($"  {name}: Sum32(comp)", sum32, storedU);

            // Fletcher-32
            CheckMatch($"  {name}: Fletcher32(comp)", Fletcher32(compressed), storedU);
            CheckMatch($"  {name}: Fletcher32(step)", Fletcher32(decompressed), storedU);

            // Maybe it's CRC32 of the decompressed data but only up to a certain alignment
            // Round down to multiple of 4
            var aligned4 = decompressed.Length & ~3;
            if (aligned4 > 0 && aligned4 < decompressed.Length)
            {
                CheckMatch($"  {name}: CRC32(step[..align4])", CRC32(decompressed[..aligned4]), storedU);
            }

            // Maybe it's CRC32 of the decompressed data with trailing whitespace stripped
            var trimEnd = decompressed.Length;
            while (trimEnd > 0 && (decompressed[trimEnd - 1] == 0x0A || decompressed[trimEnd - 1] == 0x0D || decompressed[trimEnd - 1] == 0x20))
                trimEnd--;
            if (trimEnd != decompressed.Length)
            {
                CheckMatch($"  {name}: CRC32(step-trimmed)", CRC32(decompressed[..trimEnd]), storedU);
            }
        }

        // Also output summary of compressed vs stored for correlation analysis
        _output.WriteLine("\n\n=== Correlation analysis ===");
        _output.WriteLine("compLen, stepLen, stored (hex), CRC32(comp) (hex)");
        foreach (var (_, name, _, stored, compressed, decompressed) in models)
        {
            _output.WriteLine($"{compressed.Length}, {decompressed.Length}, 0x{unchecked((uint)stored):X8}, 0x{CRC32(compressed):X8}");
        }
    }

    private void CheckMatch(string label, uint computed, uint stored)
    {
        if (computed == stored)
            _output.WriteLine($"  *** MATCH *** {label}");
    }

    private static uint CRC32(byte[] data) => CRC32WithInit(data, 0xFFFFFFFF) ^ 0xFFFFFFFF;

    private static uint CRC32WithInit(byte[] data, uint init)
    {
        uint crc = init;
        foreach (var b in data)
        {
            crc ^= b;
            for (var j = 0; j < 8; j++)
                crc = (crc >> 1) ^ (0xEDB88320 & ~((crc & 1) - 1));
        }
        return crc;
    }

    private static uint CRC32NoFinal(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var j = 0; j < 8; j++)
                crc = (crc >> 1) ^ (0xEDB88320 & ~((crc & 1) - 1));
        }
        return crc; // no final XOR
    }

    private static uint CRC32WithInitNoFinal(byte[] data, uint init)
    {
        uint crc = init;
        foreach (var b in data)
        {
            crc ^= b;
            for (var j = 0; j < 8; j++)
                crc = (crc >> 1) ^ (0xEDB88320 & ~((crc & 1) - 1));
        }
        return crc;
    }

    private static uint CRC32C(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var j = 0; j < 8; j++)
                crc = (crc >> 1) ^ (0x82F63B78 & ~((crc & 1) - 1));
        }
        return ~crc;
    }

    private static uint Adler32(byte[] data)
    {
        uint s1 = 1, s2 = 0;
        foreach (var b in data)
        {
            s1 = (s1 + b) % 65521;
            s2 = (s2 + s1) % 65521;
        }
        return (s2 << 16) | s1;
    }

    private static uint Fletcher32(byte[] data)
    {
        uint sum1 = 0xFFFF, sum2 = 0xFFFF;
        var i = 0;
        while (i < data.Length)
        {
            var tlen = Math.Min(360, data.Length - i);
            for (var j = 0; j < tlen; j++)
            {
                sum1 += data[i + j];
                sum2 += sum1;
            }
            sum1 = (sum1 & 0xFFFF) + (sum1 >> 16);
            sum2 = (sum2 & 0xFFFF) + (sum2 >> 16);
            i += tlen;
        }
        sum1 = (sum1 & 0xFFFF) + (sum1 >> 16);
        sum2 = (sum2 & 0xFFFF) + (sum2 >> 16);
        return (sum2 << 16) | sum1;
    }

    private static uint POSIXCksum(byte[] data)
    {
        // POSIX cksum: CRC32 with length octets appended
        uint crc = 0;
        foreach (var b in data)
        {
            for (var i = 7; i >= 0; i--)
            {
                var bit = ((crc >> 31) ^ ((uint)b >> i)) & 1;
                crc <<= 1;
                if (bit != 0) crc ^= 0x04C11DB7;
            }
        }
        // Process length
        var len = (uint)data.Length;
        while (len > 0)
        {
            var lb = (byte)(len & 0xFF);
            len >>= 8;
            for (var i = 7; i >= 0; i--)
            {
                var bit = ((crc >> 31) ^ ((uint)lb >> i)) & 1;
                crc <<= 1;
                if (bit != 0) crc ^= 0x04C11DB7;
            }
        }
        return ~crc;
    }

    private static string GetDataPath(params string[] parts)
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
