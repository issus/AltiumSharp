using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests for the pad/via power-plane connection style and the derived "relief enabled" API
/// (<see cref="PlaneConnectStyle"/>, <see cref="PcbPad.PowerPlaneConnection"/>,
/// <see cref="PcbPad.IsReliefEnabled"/>).
/// </summary>
public class PlaneConnectStyleTests
{
    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }

    // --- Enum mapping + IsReliefEnabled (Pad) ---

    [Theory]
    [InlineData(0, PlaneConnectStyle.Relief, true)]
    [InlineData(1, PlaneConnectStyle.Direct, false)]
    [InlineData(2, PlaneConnectStyle.NoConnect, false)]
    public void Pad_PowerPlaneConnection_MapsRawStyleAndReliefFlag(
        int raw, PlaneConnectStyle expected, bool reliefEnabled)
    {
        var pad = new PcbPad { PowerPlaneConnectStyle = raw };

        Assert.Equal(expected, pad.PowerPlaneConnection);
        Assert.Equal(reliefEnabled, pad.IsReliefEnabled);
    }

    [Fact]
    public void Pad_SettingPowerPlaneConnection_UpdatesRawStyle()
    {
        var pad = new PcbPad();

        pad.PowerPlaneConnection = PlaneConnectStyle.NoConnect;
        Assert.Equal(2, pad.PowerPlaneConnectStyle);
        Assert.False(pad.IsReliefEnabled);

        pad.PowerPlaneConnection = PlaneConnectStyle.Relief;
        Assert.Equal(0, pad.PowerPlaneConnectStyle);
        Assert.True(pad.IsReliefEnabled);
    }

    [Fact]
    public void PadBuilder_PowerPlaneConnection_SetsConfiguredStyle()
    {
        var pad = PcbPad.Create("1")
            .PowerPlaneConnection(PlaneConnectStyle.Direct)
            .Build();

        Assert.Equal(PlaneConnectStyle.Direct, pad.PowerPlaneConnection);
        Assert.False(pad.IsReliefEnabled);
    }

    // --- Enum mapping + IsReliefEnabled (Via) ---

    [Theory]
    [InlineData(0, PlaneConnectStyle.Relief, true)]
    [InlineData(1, PlaneConnectStyle.Direct, false)]
    [InlineData(2, PlaneConnectStyle.NoConnect, false)]
    public void Via_PowerPlaneConnection_MapsRawStyleAndReliefFlag(
        int raw, PlaneConnectStyle expected, bool reliefEnabled)
    {
        var via = new PcbVia { PowerPlaneConnectStyle = raw };

        Assert.Equal(expected, via.PowerPlaneConnection);
        Assert.Equal(reliefEnabled, via.IsReliefEnabled);
    }

    // --- Real footprint: stored/configured relief style is read correctly ---

    [SkippableFact]
    public async Task RealFootprintPads_ReportConfiguredReliefConnection()
    {
        var filePath = Path.Combine(GetTestDataPath(),
            "Generated", "Individual", "PCB", "PAD_THERMAL_RELIEF.PcbLib");
        Skip.IfNot(File.Exists(filePath), "Test data not available");

        var library = await AltiumLibrary.OpenPcbLibAsync(filePath);
        var pads = library.Components.SelectMany(c => c.Pads).OfType<PcbPad>().ToList();

        Assert.NotEmpty(pads);
        foreach (var pad in pads)
        {
            // These footprint pads are stored as relief-connect with populated relief geometry.
            Assert.Equal(PlaneConnectStyle.Relief, pad.PowerPlaneConnection);
            Assert.True(pad.IsReliefEnabled);
            Assert.Equal(4, pad.ReliefEntries);
            Assert.True(pad.ReliefAirGap.ToMils() > 0);
            Assert.True(pad.ReliefConductorWidth.ToMils() > 0);
        }
    }

    // --- Round-trip through the writer preserves the configured style ---

    [Theory]
    [InlineData(PlaneConnectStyle.Relief)]
    [InlineData(PlaneConnectStyle.Direct)]
    [InlineData(PlaneConnectStyle.NoConnect)]
    public async Task PowerPlaneConnection_SurvivesWriteReadRoundTrip(PlaneConnectStyle style)
    {
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("TESTPAD")
            .AddPad(pad => pad
                .At(Coord.Zero, Coord.Zero)
                .Size(Coord.FromMils(60), Coord.FromMils(60))
                .WithDesignator("1")
                .ThroughHole(Coord.FromMils(30))
                .PowerPlaneConnection(style))
            .Build();
        library.Add(component);

        using var stream = new MemoryStream();
        await library.SaveAsync(stream);

        stream.Position = 0;
        var reread = await AltiumLibrary.OpenPcbLibAsync(stream);
        var pad = reread.Components.Single().Pads.OfType<PcbPad>().Single();

        Assert.Equal(style, pad.PowerPlaneConnection);
        Assert.Equal(style == PlaneConnectStyle.Relief, pad.IsReliefEnabled);
    }
}
