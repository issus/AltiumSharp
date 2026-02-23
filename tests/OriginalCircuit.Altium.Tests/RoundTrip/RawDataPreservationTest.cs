using OriginalCircuit.Altium.Serialization.Readers;
using Xunit;
using Xunit.Abstractions;

namespace OriginalCircuit.Altium.Tests.RoundTrip;

public class RawDataPreservationTest
{
    private readonly ITestOutputHelper _output;

    public RawDataPreservationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public async Task PcbLib_SaveWithZeroChecksum()
    {
        var testFile = GetDataPath("TestData", "Generated", "Individual", "PCB", "BODY_3D_STEP.PcbLib");
        if (!File.Exists(testFile)) { Skip.If(true, "Test data not available"); return; }

        var reader = new PcbLibReader();
        await using var fs = File.OpenRead(testFile);
        var library = reader.Read(fs);

        // Zero out the checksum on all models
        foreach (var model in library.Models)
        {
            _output.WriteLine($"Model '{model.Name}' checksum {model.Checksum} -> 0");
            model.Checksum = 0;
        }

        // Save to a new file next to the original
        var outPath = GetDataPath("TestData", "Generated", "Individual", "PCB", "BODY_3D_STEP_CHECKSUM0.PcbLib");
        await library.SaveAsync(outPath, new OriginalCircuit.Eda.Models.SaveOptions());
        _output.WriteLine($"Saved to: {outPath}");
    }

    [SkippableFact]
    public async Task PcbLib_ModelRoundTrip()
    {
        var testFile = GetDataPath("TestData", "Generated", "Individual", "PCB", "BODY_3D_STEP.PcbLib");
        if (!File.Exists(testFile)) { Skip.If(true, "Test data not available"); return; }

        // Read the PcbLib
        var reader = new PcbLibReader();
        await using var fs = File.OpenRead(testFile);
        var library = reader.Read(fs);

        // Verify models were parsed
        Assert.Single(library.Models);
        var model = library.Models[0];
        Assert.Equal("{CD5F2285-DAA3-4129-A6AA-C28EC220260E}", model.Id);
        Assert.Equal("PSEMI QFN-24 4x4.step", model.Name);
        Assert.True(model.IsEmbedded);
        Assert.Equal("Undefined", model.ModelSource);
        Assert.Equal(0.0, model.RotationX);
        Assert.Equal(0.0, model.RotationY);
        Assert.Equal(0.0, model.RotationZ);
        Assert.Equal(0, model.Dz);
        Assert.Equal(1468567647, model.Checksum);
        Assert.StartsWith("ISO-10303-21;", model.StepData);
        Assert.Contains("ENDSEC;", model.StepData);
        _output.WriteLine($"STEP data length: {model.StepData.Length}");

        // Write to a new stream
        using var outMs = new MemoryStream();
        await library.SaveAsync(outMs);

        // Read back and verify
        outMs.Position = 0;
        var library2 = new PcbLibReader().Read(outMs);
        Assert.Single(library2.Models);
        var model2 = library2.Models[0];
        Assert.Equal(model.Id, model2.Id);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(model.IsEmbedded, model2.IsEmbedded);
        Assert.Equal(model.Checksum, model2.Checksum);
        Assert.Equal(model.StepData, model2.StepData);
        _output.WriteLine("Round-trip STEP model data matches!");
    }

    private static string GetDataPath(params string[] parts)
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
