using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Pcb;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests for the physical board stack-up model (<see cref="PcbStackup"/>): the default-stack factory,
/// the V9_STACK Board6 parser, copper layer mapping, and Z computation.
/// </summary>
public class PcbStackupTests
{
    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }

    [Fact]
    public void CreateDefault_TwoLayer_HasExpectedThicknessAndCopperMapping()
    {
        var stack = PcbStackup.CreateDefault(1.6, copperLayers: 2);

        Assert.True(stack.IsFallback);
        Assert.Equal(1.6, stack.TotalThicknessMm, 3);

        var copper = stack.CopperLayers.ToList();
        Assert.Equal(2, copper.Count);
        Assert.Equal(1, copper[0].Layer);    // Top
        Assert.Equal(32, copper[1].Layer);   // Bottom

        // Bottom face sits on Z = 0, top face equals the total thickness.
        Assert.Equal(0.0, stack.Layers[^1].Z0Mm, 4);
        Assert.Equal(stack.TotalThicknessMm, stack.Layers[0].Z1Mm, 4);
    }

    [Fact]
    public void CreateDefault_FourLayer_AssignsInnerCopperPositionally()
    {
        var stack = PcbStackup.CreateDefault(1.6, copperLayers: 4);

        var copper = stack.CopperLayers.ToList();
        Assert.Equal(4, copper.Count);
        Assert.Equal(new[] { 1, 2, 3, 32 }, copper.Select(c => c.Layer!.Value).ToArray());

        // Layers run strictly top -> bottom in Z.
        for (int i = 1; i < stack.Layers.Count; i++)
            Assert.True(stack.Layers[i].CenterZMm <= stack.Layers[i - 1].CenterZMm + 1e-9);
    }

    [Fact]
    public void FromBoardParameters_Null_ReturnsNull()
        => Assert.Null(PcbStackup.FromBoardParameters(null));

    [Fact]
    public void FromBoardParameters_NoV9Stack_ReturnsNull()
        => Assert.Null(PcbStackup.FromBoardParameters(new Dictionary<string, string> { ["FOO"] = "BAR" }));

    [Fact]
    public void FromBoardParameters_SyntheticTwoLayer_ParsesKindsThicknessAndMapping()
    {
        var bp = new Dictionary<string, string>
        {
            ["V9_STACK_LAYER1_NAME"] = "Top Overlay",
            ["V9_STACK_LAYER2_NAME"] = "Top Solder",
            ["V9_STACK_LAYER2_DIELTYPE"] = "3",
            ["V9_STACK_LAYER2_DIELHEIGHT"] = "0.4mil",
            ["V9_STACK_LAYER2_DIELMATERIAL"] = "Solder Resist",
            ["V9_STACK_LAYER3_NAME"] = "Top Layer",
            ["V9_STACK_LAYER3_COPTHICK"] = "1.4mil",
            ["V9_STACK_LAYER4_NAME"] = "Dielectric 1",
            ["V9_STACK_LAYER4_DIELTYPE"] = "0",
            ["V9_STACK_LAYER4_DIELHEIGHT"] = "59mil",
            ["V9_STACK_LAYER4_DIELMATERIAL"] = "FR-4",
            ["V9_STACK_LAYER5_NAME"] = "Bottom Layer",
            ["V9_STACK_LAYER5_COPTHICK"] = "1.4mil",
            ["V9_STACK_LAYER6_NAME"] = "Bottom Solder",
            ["V9_STACK_LAYER6_DIELTYPE"] = "3",
            ["V9_STACK_LAYER6_DIELHEIGHT"] = "0.4mil",
        };

        var stack = PcbStackup.FromBoardParameters(bp);
        Assert.NotNull(stack);
        Assert.False(stack!.IsFallback);

        Assert.Equal(PcbStackupLayerKind.Overlay, stack.Layers[0].Kind);
        Assert.Equal(PcbStackupLayerKind.SolderMask, stack.Layers[1].Kind);
        Assert.Equal(PcbStackupLayerKind.Copper, stack.Layers[2].Kind);
        Assert.Equal(PcbStackupLayerKind.Dielectric, stack.Layers[3].Kind);

        // 1.4mil ≈ 0.03556 mm copper; 59mil ≈ 1.4986 mm core.
        Assert.Equal(0.03556, stack.Layers[2].ThicknessMm, 4);
        Assert.Equal(1.4986, stack.Layers[3].ThicknessMm, 3);

        Assert.Equal(1, stack.ForLayer(1)!.Layer);     // Top copper
        Assert.Equal(32, stack.ForLayer(32)!.Layer);   // Bottom copper
        Assert.Equal(PcbStackupLayerKind.SolderMask, stack.ForLayer(37)!.Kind); // Top solder mask
    }

    [SkippableTheory]
    [InlineData("SPI Isolator.PcbDoc", 2, 1.55, 1.70)]
    [InlineData("MAX5719 Breakout.PcbDoc", 4, 1.55, 1.70)]
    public async Task GetStackup_RealBoard_HasExpectedCopperLayersAndThickness(
        string file, int expectedCopper, double minMm, double maxMm)
    {
        var filePath = Path.Combine(GetTestDataPath(), file);
        Skip.IfNot(File.Exists(filePath), "Test data not available");

        await using var idoc = await AltiumLibrary.OpenPcbDocAsync(filePath);
        var doc = (PcbDocument)idoc;

        var stack = doc.GetStackup();
        Assert.NotNull(stack);
        Assert.False(stack!.IsFallback);

        var copper = stack.CopperLayers.ToList();
        Assert.Equal(expectedCopper, copper.Count);
        Assert.Equal(1, copper.First().Layer);
        Assert.Equal(32, copper.Last().Layer);

        // Total board thickness in a believable range for a standard board.
        Assert.InRange(stack.TotalThicknessMm, minMm, maxMm);

        // Top copper sits near the top face, bottom copper near the bottom face.
        Assert.True(copper.First().CenterZMm > stack.TotalThicknessMm * 0.5);
        Assert.True(copper.Last().CenterZMm < stack.TotalThicknessMm * 0.5);

        // Layers are ordered top -> bottom and the stack lands on Z = 0.
        Assert.Equal(0.0, stack.Layers[^1].Z0Mm, 3);
    }
}
