using System.Text;
using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Rendering.Gltf;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Integration tests for the glTF board renderer: rendering a real board produces a valid GLB whose
/// scene carries the expected named, toggleable feature nodes, and the output format follows the
/// requested settings / file extension.
/// </summary>
public class GltfRenderingTests
{
    private static readonly byte[] GlbMagic = [0x67, 0x6C, 0x54, 0x46]; // "glTF"

    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }

    private static string ExtractGlbJson(byte[] glb)
    {
        int jsonLength = BitConverter.ToInt32(glb, 12); // chunk-0 length follows the 12-byte header
        return Encoding.UTF8.GetString(glb, 20, jsonLength);
    }

    [SkippableTheory]
    [InlineData("SPI Isolator.PcbDoc")]
    [InlineData("MAX5719 Breakout.PcbDoc")]
    public async Task RenderAsync_Glb_ProducesValidToggleableScene(string file)
    {
        var path = Path.Combine(GetTestDataPath(), file);
        Skip.IfNot(File.Exists(path), "Test data not available");
        var doc = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(path);

        using var ms = new MemoryStream();
        await new GltfRenderer().RenderAsync(doc, ms); // GLB by default for a stream

        var bytes = ms.ToArray();
        Assert.True(bytes.Length > 1000, "GLB should be non-trivial");
        Assert.Equal(GlbMagic, bytes[..4]);

        string json = ExtractGlbJson(bytes);
        Assert.Contains("\"Substrate\"", json);
        Assert.Contains("Copper.", json);
        Assert.Contains("SolderMask.", json);    // the inverse mask layer (openings reveal the copper beneath)
        Assert.Contains("Silkscreen.", json);    // overlay tracks/arcs/text
        Assert.Contains("\"Drills\"", json);
        Assert.Contains("\"Components\"", json); // these boards have embedded component models
    }

    [SkippableFact]
    public async Task RenderAsync_BareBoard_OmitsComponents()
    {
        var path = Path.Combine(GetTestDataPath(), "SPI Isolator.PcbDoc");
        Skip.IfNot(File.Exists(path), "Test data not available");
        var doc = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(path);

        using var ms = new MemoryStream();
        await new GltfRenderer().RenderAsync(doc, ms, new GltfRenderSettings { IncludeComponents = false });

        string json = ExtractGlbJson(ms.ToArray());
        Assert.Contains("\"Substrate\"", json);
        Assert.DoesNotContain("\"Components\"", json);
    }

    [SkippableFact]
    public async Task RenderAsync_CopperFilter_RestrictsCopperLayers()
    {
        var path = Path.Combine(GetTestDataPath(), "MAX5719 Breakout.PcbDoc"); // a 4-layer board
        Skip.IfNot(File.Exists(path), "Test data not available");
        var doc = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(path);

        using var ms = new MemoryStream();
        await new GltfRenderer().RenderAsync(doc, ms, new GltfRenderSettings
        {
            IncludeComponents = false,
            CopperLayerFilter = new[] { 1, 32 }, // top + bottom only
        });

        string json = ExtractGlbJson(ms.ToArray());
        Assert.Contains("Copper.", json);
        Assert.DoesNotContain("Copper.2 GND", json); // inner layer excluded
    }

    [SkippableTheory]
    [InlineData(".glb", (byte)0x67)] // "glTF" magic
    [InlineData(".gltf", (byte)0x7B)] // '{'  JSON
    public async Task RenderAsync_File_InfersFormatFromExtension(string ext, byte firstByte)
    {
        var path = Path.Combine(GetTestDataPath(), "SPI Isolator.PcbDoc");
        Skip.IfNot(File.Exists(path), "Test data not available");
        var doc = (PcbDocument)await AltiumLibrary.OpenPcbDocAsync(path);

        var outPath = Path.Combine(Path.GetTempPath(), $"gltf_test_{Guid.NewGuid():N}{ext}");
        try
        {
            await new GltfRenderer().RenderAsync(doc, outPath, new GltfRenderSettings { IncludeComponents = false });
            Assert.True(File.Exists(outPath));
            using var fs = File.OpenRead(outPath);
            Assert.Equal(firstByte, (byte)fs.ReadByte());
        }
        finally
        {
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }
}
