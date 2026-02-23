using BenchmarkDotNet.Attributes;

using V2SchLibReader = OriginalCircuit.Altium.Serialization.Readers.SchLibReader;
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

    [Benchmark(Baseline = true, Description = "SchLibReader")]
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

    [Benchmark(Baseline = true, Description = "PcbLibReader")]
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
