using System.IO.Compression;
using System.Text;
using OpenMcdf;
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
    public void CompareOriginalAndWrittenPcbLib()
    {
        var origPath = GetDataPath("TestData", "Generated", "Individual", "PCB", "BODY_3D_STEP.PcbLib");
        var writtenPath = GetDataPath("TestData", "Generated", "Individual", "PCB", "BODY_3D_STEP_CHECKSUM0.PcbLib");
        if (!File.Exists(origPath) || !File.Exists(writtenPath)) { Skip.If(true, "Test data not available"); return; }

        _output.WriteLine("=== ORIGINAL ===");
        DumpCompoundFile(origPath);

        _output.WriteLine("\n\n=== WRITTEN ===");
        DumpCompoundFile(writtenPath);

        // Now do a detailed byte comparison of each stream
        _output.WriteLine("\n\n=== STREAM-BY-STREAM COMPARISON ===");
        using var origFs = File.OpenRead(origPath);
        using var origCf = new CompoundFile(origFs);
        using var writtenFs = File.OpenRead(writtenPath);
        using var writtenCf = new CompoundFile(writtenFs);

        CompareStorage(origCf.RootStorage, writtenCf.RootStorage, "");
    }

    private void DumpCompoundFile(string path)
    {
        _output.WriteLine($"File: {Path.GetFileName(path)} ({new FileInfo(path).Length} bytes)");
        using var fs = File.OpenRead(path);
        using var cf = new CompoundFile(fs);
        DumpStorage(cf.RootStorage, "");
    }

    private void DumpStorage(CFStorage storage, string indent)
    {
        storage.VisitEntries(entry =>
        {
            if (entry is CFStream stream)
            {
                var data = stream.GetData();
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
            else if (entry is CFStorage subStorage)
            {
                _output.WriteLine($"{indent}[Storage] {entry.Name}/");
                DumpStorage(subStorage, indent + "  ");
            }
        }, false);
    }

    private void CompareStorage(CFStorage orig, CFStorage written, string path)
    {
        var origEntries = new Dictionary<string, CFItem>();
        var writtenEntries = new Dictionary<string, CFItem>();

        orig.VisitEntries(e => origEntries[e.Name] = e, false);
        written.VisitEntries(e => writtenEntries[e.Name] = e, false);

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

            if (origEntry is CFStream origStream && writtenEntry is CFStream writtenStream)
            {
                var origData = origStream.GetData();
                var writtenData = writtenStream.GetData();

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
            else if (origEntry is CFStorage origSub && writtenEntry is CFStorage writtenSub)
            {
                _output.WriteLine($"Comparing storage {path}/{name}/");
                CompareStorage(origSub, writtenSub, $"{path}/{name}");
            }
            else
            {
                _output.WriteLine($"TYPE MISMATCH {path}/{name}: orig={origEntry.GetType().Name}, written={writtenEntry.GetType().Name}");
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
