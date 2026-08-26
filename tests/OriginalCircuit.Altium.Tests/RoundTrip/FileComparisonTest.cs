using System.IO.Compression;
using System.Text;
using OpenMcdf;
using OriginalCircuit.Altium.Serialization.Readers;
using Xunit;
using Xunit.Abstractions;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

public class FileComparisonTest
{
    private readonly ITestOutputHelper _output;

    public FileComparisonTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public async Task CompareOriginalAndWrittenPcbLib()
    {
        var origPath = GetDataPath("TestData", "Generated", "Individual", "PCB", "BODY_3D_STEP.PcbLib");
        if (!File.Exists(origPath)) { Skip.If(true, "Test data not available"); return; }

        // Regenerate the written variant in memory (zeroed model checksums, as
        // RawDataPreservationTest saves it) instead of depending on a file on disk.
        await using var origRead = File.OpenRead(origPath);
        var library = new PcbLibReader().Read(origRead);
        foreach (var model in library.Models)
            model.Checksum = 0;
        using var writtenMs = new MemoryStream();
        await library.SaveAsync(writtenMs);

        _output.WriteLine("=== ORIGINAL ===");
        DumpCompoundFile(origPath);

        _output.WriteLine("\n\n=== WRITTEN ===");
        _output.WriteLine($"In-memory zero-checksum variant ({writtenMs.Length} bytes)");
        writtenMs.Position = 0;
        using (var writtenDump = RootStorage.Open(writtenMs, StorageModeFlags.LeaveOpen))
            DumpStorage(writtenDump, "");

        // Now do a detailed byte comparison of each stream
        _output.WriteLine("\n\n=== STREAM-BY-STREAM COMPARISON ===");
        using var origFs = File.OpenRead(origPath);
        using var origCf = RootStorage.Open(origFs, StorageModeFlags.LeaveOpen);
        writtenMs.Position = 0;
        using var writtenCf = RootStorage.Open(writtenMs, StorageModeFlags.LeaveOpen);

        CompareStorage(origCf, writtenCf, "");
    }

    private void DumpCompoundFile(string path)
    {
        _output.WriteLine($"File: {Path.GetFileName(path)} ({new FileInfo(path).Length} bytes)");
        using var cf = RootStorage.OpenRead(path);
        DumpStorage(cf, "");
    }

    private void DumpStorage(Storage storage, string indent)
    {
        foreach (var entry in storage.EnumerateEntries().ToList())
        {
            if (entry.Type == EntryType.Stream)
            {
                var data = storage.ReadStreamData(entry.Name);
                _output.WriteLine($"{indent}[Stream] {entry.Name}: {data.Length} bytes");
                // Show first 100 bytes as hex + ascii
                if (data.Length > 0)
                {
                    var len = Math.Min(100, data.Length);
                    _output.WriteLine($"{indent}  Hex: {BitConverter.ToString(data, 0, len)}");
                    var ascii = Encoding.ASCII.GetString(data, 0, len);
                    var printable = new string(ascii.Select(c => c >= 32 && c < 127 ? c : '.').ToArray());
                    _output.WriteLine($"{indent}  ASCII: {printable}");
                }
            }
            else
            {
                _output.WriteLine($"{indent}[Storage] {entry.Name}/");
                DumpStorage(storage.OpenStorage(entry.Name), indent + "  ");
            }
        }
    }

    private void CompareStorage(Storage orig, Storage written, string path)
    {
        var origEntries = orig.EnumerateEntries().ToDictionary(e => e.Name);
        var writtenEntries = written.EnumerateEntries().ToDictionary(e => e.Name);

        // Check for missing/extra entries
        foreach (var name in origEntries.Keys.Except(writtenEntries.Keys))
            _output.WriteLine($"MISSING in written: {path}/{name}");
        foreach (var name in writtenEntries.Keys.Except(origEntries.Keys))
            _output.WriteLine($"EXTRA in written: {path}/{name}");

        // Compare common entries
        foreach (var name in origEntries.Keys.Intersect(writtenEntries.Keys))
        {
            var origEntry = origEntries[name];
            var writtenEntry = writtenEntries[name];

            if (origEntry.Type == EntryType.Stream && writtenEntry.Type == EntryType.Stream)
            {
                var origData = orig.ReadStreamData(name);
                var writtenData = written.ReadStreamData(name);

                if (origData.Length != writtenData.Length)
                {
                    _output.WriteLine($"SIZE DIFF {path}/{name}: orig={origData.Length}, written={writtenData.Length}");
                    // Show both
                    var showLen = Math.Min(200, Math.Max(origData.Length, writtenData.Length));
                    if (origData.Length > 0)
                        _output.WriteLine($"  Orig:    {BitConverter.ToString(origData, 0, Math.Min(showLen, origData.Length))}");
                    if (writtenData.Length > 0)
                        _output.WriteLine($"  Written: {BitConverter.ToString(writtenData, 0, Math.Min(showLen, writtenData.Length))}");

                    // Show as ASCII too
                    if (origData.Length > 0 && origData.Length < 1000)
                    {
                        var origAscii = Encoding.ASCII.GetString(origData);
                        var origPrintable = new string(origAscii.Select(c => c >= 32 && c < 127 ? c : '.').ToArray());
                        _output.WriteLine($"  Orig ASCII:    {origPrintable}");
                    }
                    if (writtenData.Length > 0 && writtenData.Length < 1000)
                    {
                        var writtenAscii = Encoding.ASCII.GetString(writtenData);
                        var writtenPrintable = new string(writtenAscii.Select(c => c >= 32 && c < 127 ? c : '.').ToArray());
                        _output.WriteLine($"  Written ASCII: {writtenPrintable}");
                    }
                }
                else if (!origData.SequenceEqual(writtenData))
                {
                    _output.WriteLine($"CONTENT DIFF {path}/{name}: {origData.Length} bytes");
                    // Find first difference
                    for (var i = 0; i < origData.Length; i++)
                    {
                        if (origData[i] != writtenData[i])
                        {
                            var contextStart = Math.Max(0, i - 8);
                            var contextEnd = Math.Min(origData.Length, i + 24);
                            _output.WriteLine($"  First diff at byte {i}:");
                            _output.WriteLine($"  Orig:    ...{BitConverter.ToString(origData, contextStart, contextEnd - contextStart)}");
                            _output.WriteLine($"  Written: ...{BitConverter.ToString(writtenData, contextStart, contextEnd - contextStart)}");
                            break;
                        }
                    }

                    // Count total different bytes
                    var diffCount = origData.Zip(writtenData).Count(pair => pair.First != pair.Second);
                    _output.WriteLine($"  Total different bytes: {diffCount}/{origData.Length}");

                    // Show as ASCII if small
                    if (origData.Length < 500)
                    {
                        var origAscii = new string(Encoding.ASCII.GetString(origData).Select(c => c >= 32 && c < 127 ? c : '.').ToArray());
                        var writtenAscii = new string(Encoding.ASCII.GetString(writtenData).Select(c => c >= 32 && c < 127 ? c : '.').ToArray());
                        _output.WriteLine($"  Orig ASCII:    {origAscii}");
                        _output.WriteLine($"  Written ASCII: {writtenAscii}");
                    }
                }
                else
                {
                    _output.WriteLine($"IDENTICAL {path}/{name}: {origData.Length} bytes");
                }
            }
            else if (origEntry.Type == EntryType.Storage && writtenEntry.Type == EntryType.Storage)
            {
                _output.WriteLine($"Comparing storage {path}/{name}/");
                CompareStorage(orig.OpenStorage(name), written.OpenStorage(name), $"{path}/{name}");
            }
            else
            {
                _output.WriteLine($"TYPE MISMATCH {path}/{name}: orig={origEntry.Type}, written={writtenEntry.Type}");
            }
        }
    }

    private static string GetDataPath(params string[] parts)
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
