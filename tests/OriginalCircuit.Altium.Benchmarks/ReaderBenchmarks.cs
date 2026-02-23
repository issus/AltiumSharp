using BenchmarkDotNet.Attributes;

using V1SchLibReader = OriginalCircuit.AltiumSharp.SchLibReader;
using V2SchLibReader = OriginalCircuit.Altium.Serialization.Readers.SchLibReader;
using V1PcbLibReader = OriginalCircuit.AltiumSharp.PcbLibReader;
using V2PcbLibReader = OriginalCircuit.Altium.Serialization.Readers.PcbLibReader;

namespace OriginalCircuit.Altium.Benchmarks;

[MemoryDiagnoser]

public class SchLibReaderBenchmarks
{
    private byte[][] _schLibFiles = [];

    [GlobalSetup]
    public void Setup()
    {
        var files = GetSchLibFiles().ToArray();
        _schLibFiles = files.Select(File.ReadAllBytes).ToArray();

        if (_schLibFiles.Length == 0)
        {
            throw new InvalidOperationException(
                "No SchLib test files found. Run tests from the repository root.");
        }
    }

    [Benchmark(Baseline = true, Description = "v1 SchLibReader")]
    public int V1_ReadSchLib()
    {
        int totalComponents = 0;
        foreach (var fileBytes in _schLibFiles)
        {
            using var reader = new V1SchLibReader();
            using var ms = new MemoryStream(fileBytes);
            var lib = reader.Read(ms);
            totalComponents += lib.Items.Count;
        }
        return totalComponents;
    }

    [Benchmark(Description = "v2 SchLibReader")]
    public int V2_ReadSchLib()
    {
        int totalComponents = 0;
        foreach (var fileBytes in _schLibFiles)
        {
            using var ms = new MemoryStream(fileBytes);
            var lib = new V2SchLibReader().Read(ms);
            totalComponents += lib.Components.Count;
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

public class PcbLibReaderBenchmarks
{
    private byte[][] _pcbLibFiles = [];

    [GlobalSetup]
    public void Setup()
    {
        var files = GetPcbLibFiles().ToArray();
        _pcbLibFiles = files.Select(File.ReadAllBytes).ToArray();

        if (_pcbLibFiles.Length == 0)
        {
            throw new InvalidOperationException(
                "No PcbLib test files found. Run tests from the repository root.");
        }
    }

    [Benchmark(Baseline = true, Description = "v1 PcbLibReader")]
    public int V1_ReadPcbLib()
    {
        int totalComponents = 0;
        foreach (var fileBytes in _pcbLibFiles)
        {
            using var reader = new V1PcbLibReader();
            using var ms = new MemoryStream(fileBytes);
            var lib = reader.Read(ms);
            totalComponents += lib.Items.Count;
        }
        return totalComponents;
    }

    [Benchmark(Description = "v2 PcbLibReader")]
    public int V2_ReadPcbLib()
    {
        int totalComponents = 0;
        foreach (var fileBytes in _pcbLibFiles)
        {
            using var ms = new MemoryStream(fileBytes);
            var lib = new V2PcbLibReader().Read(ms);
            totalComponents += lib.Components.Count;
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
