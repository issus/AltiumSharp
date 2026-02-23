using BenchmarkDotNet.Attributes;

using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Models.Pcb;
using V2SchLibReader = OriginalCircuit.Altium.Serialization.Readers.SchLibReader;
using V2SchLibWriter = OriginalCircuit.Altium.Serialization.Writers.SchLibWriter;
using V2PcbLibReader = OriginalCircuit.Altium.Serialization.Readers.PcbLibReader;
using V2PcbLibWriter = OriginalCircuit.Altium.Serialization.Writers.PcbLibWriter;

namespace OriginalCircuit.Altium.Benchmarks;

[MemoryDiagnoser]

public class SchLibWriterBenchmarks
{
    private SchLibrary[] _libraries = [];

    [GlobalSetup]
    public void Setup()
    {
        var files = GetSchLibFiles().ToArray();
        _libraries = files.Select(f =>
        {
            using var stream = File.OpenRead(f);
            return new V2SchLibReader().Read(stream);
        }).ToArray();

        if (_libraries.Length == 0)
        {
            throw new InvalidOperationException(
                "No SchLib test files found. Run tests from the repository root.");
        }
    }

    [Benchmark(Description = "v2 SchLibWriter")]
    public long V2_WriteSchLib()
    {
        long totalBytes = 0;
        foreach (var lib in _libraries)
        {
            using var ms = new MemoryStream();
            new V2SchLibWriter().Write(lib, ms);
            totalBytes += ms.Length;
        }
        return totalBytes;
    }

    [Benchmark(Description = "v2 SchLib Round-Trip")]
    public int V2_RoundTrip()
    {
        int totalComponents = 0;
        foreach (var lib in _libraries)
        {
            using var ms = new MemoryStream();
            new V2SchLibWriter().Write(lib, ms);
            ms.Position = 0;
            var roundTripped = new V2SchLibReader().Read(ms);
            totalComponents += roundTripped.Components.Count;
        }
        return totalComponents;
    }

    private static IEnumerable<string> GetSchLibFiles()
    {
        var dirs = new[]
        {
            GetDataPath("TestData", "Generated", "Individual", "SchLib"),
            GetDataPath("AltiumScriptExamples")
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.SchLib"))
                yield return file;
        }
    }

    private static string GetDataPath(params string[] parts)
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}

[MemoryDiagnoser]

public class PcbLibWriterBenchmarks
{
    private PcbLibrary[] _libraries = [];

    [GlobalSetup]
    public void Setup()
    {
        var files = GetPcbLibFiles().ToArray();
        _libraries = files.Select(f =>
        {
            using var stream = File.OpenRead(f);
            return new V2PcbLibReader().Read(stream);
        }).ToArray();

        if (_libraries.Length == 0)
        {
            throw new InvalidOperationException(
                "No PcbLib test files found. Run tests from the repository root.");
        }
    }

    [Benchmark(Description = "v2 PcbLibWriter")]
    public long V2_WritePcbLib()
    {
        long totalBytes = 0;
        foreach (var lib in _libraries)
        {
            using var ms = new MemoryStream();
            new V2PcbLibWriter().Write(lib, ms);
            totalBytes += ms.Length;
        }
        return totalBytes;
    }

    [Benchmark(Description = "v2 PcbLib Round-Trip")]
    public int V2_RoundTrip()
    {
        int totalComponents = 0;
        foreach (var lib in _libraries)
        {
            using var ms = new MemoryStream();
            new V2PcbLibWriter().Write(lib, ms);
            ms.Position = 0;
            var roundTripped = new V2PcbLibReader().Read(ms);
            totalComponents += roundTripped.Components.Count;
        }
        return totalComponents;
    }

    private static IEnumerable<string> GetPcbLibFiles()
    {
        var dirs = new[]
        {
            GetDataPath("TestData", "Generated", "Individual", "PCB"),
            GetDataPath("AltiumScriptExamples")
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.PcbLib"))
                yield return file;
        }
    }

    private static string GetDataPath(params string[] parts)
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
