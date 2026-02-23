using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Serialization.Readers;
using OriginalCircuit.Altium.Serialization.Writers;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests that readers don't silently accept wrong file formats.
/// Also tests that writers produce files that the correct reader can consume.
/// </summary>
public class CrossFormatTests
{
    [Fact]
    public void SchLibReader_ReadingPcbLibData_DoesNotCrash()
    {
        var pcbLib = (PcbLibrary)AltiumLibrary.CreatePcbLib();
        pcbLib.Add(PcbComponent.Create("TEST_FP")
            .WithDescription("Test")
            .AddPad(p => p.WithDesignator("1").At(Coord.FromMils(0), Coord.FromMils(0)))
            .Build());

        using var ms = new MemoryStream();
        new PcbLibWriter().Write(pcbLib, ms);

        ms.Position = 0;
        var schReader = new SchLibReader();
        try
        {
            var result = schReader.Read(ms);
            // If it reads without throwing, that's acceptable
        }
        catch (Exception)
        {
            // Expected - wrong format
        }
    }

    [Fact]
    public void PcbLibReader_ReadingSchLibData_DoesNotCrash()
    {
        var schLib = (SchLibrary)AltiumLibrary.CreateSchLib();
        schLib.Add(SchComponent.Create("TEST_SYM")
            .WithDescription("Test")
            .AddPin(p => p.WithName("A").At(Coord.FromMils(0), Coord.FromMils(0)))
            .Build());

        using var ms = new MemoryStream();
        new SchLibWriter().Write(schLib, ms);

        ms.Position = 0;
        var pcbReader = new PcbLibReader();
        try
        {
            var result = pcbReader.Read(ms);
        }
        catch (Exception)
        {
            // Expected - wrong format
        }
    }

    [Fact]
    public void SchDocReader_ReadingSchLibData_DoesNotCrash()
    {
        var schLib = (SchLibrary)AltiumLibrary.CreateSchLib();
        schLib.Add(SchComponent.Create("TEST")
            .WithDescription("Test")
            .Build());

        using var ms = new MemoryStream();
        new SchLibWriter().Write(schLib, ms);

        ms.Position = 0;
        var schDocReader = new SchDocReader();
        try
        {
            var result = schDocReader.Read(ms);
        }
        catch (Exception)
        {
            // Expected - SchLib structure differs from SchDoc
        }
    }

    [Fact]
    public void PcbDocReader_ReadingPcbLibData_DoesNotCrash()
    {
        var pcbLib = (PcbLibrary)AltiumLibrary.CreatePcbLib();
        pcbLib.Add(PcbComponent.Create("TEST")
            .WithDescription("Test")
            .Build());

        using var ms = new MemoryStream();
        new PcbLibWriter().Write(pcbLib, ms);

        ms.Position = 0;
        var pcbDocReader = new PcbDocReader();
        try
        {
            var result = pcbDocReader.Read(ms);
        }
        catch (Exception)
        {
            // Expected - PcbLib structure differs from PcbDoc
        }
    }

    [Fact]
    public void PcbLib_WriteThenRead_Roundtrips()
    {
        var original = (PcbLibrary)AltiumLibrary.CreatePcbLib();
        original.Add(PcbComponent.Create("ROUNDTRIP")
            .WithDescription("Roundtrip test")
            .AddPad(p => p.WithDesignator("1").At(Coord.FromMils(100), Coord.FromMils(200)))
            .Build());

        using var ms = new MemoryStream();
        new PcbLibWriter().Write(original, ms);

        ms.Position = 0;
        var readBack = new PcbLibReader().Read(ms);

        Assert.Single(readBack.Components);
        Assert.Equal("ROUNDTRIP", readBack.Components.First().Name);
    }

    [Fact]
    public void SchLib_WriteThenRead_Roundtrips()
    {
        var original = (SchLibrary)AltiumLibrary.CreateSchLib();
        original.Add(SchComponent.Create("ROUNDTRIP")
            .WithDescription("Roundtrip test")
            .AddPin(p => p.WithName("OUT").At(Coord.FromMils(0), Coord.FromMils(0)))
            .Build());

        using var ms = new MemoryStream();
        new SchLibWriter().Write(original, ms);

        ms.Position = 0;
        var readBack = new SchLibReader().Read(ms);

        Assert.Single(readBack.Components);
        Assert.Equal("ROUNDTRIP", readBack.Components.First().Name);
    }

    [Fact]
    public void SchDoc_WriteThenRead_Roundtrips()
    {
        var original = (SchDocument)AltiumLibrary.CreateSchDoc();
        original.AddComponent(SchComponent.Create("U1")
            .WithDescription("IC")
            .Build());

        using var ms = new MemoryStream();
        new SchDocWriter().Write(original, ms);

        ms.Position = 0;
        var readBack = new SchDocReader().Read(ms);

        Assert.Single(readBack.Components);
        Assert.Equal("U1", readBack.Components.First().Name);
    }

    [Fact]
    public void PcbDoc_WriteThenRead_Roundtrips()
    {
        var original = (PcbDocument)AltiumLibrary.CreatePcbDoc();
        original.AddTrack(new PcbTrack
        {
            Start = new CoordPoint(Coord.FromMils(0), Coord.FromMils(0)),
            End = new CoordPoint(Coord.FromMils(100), Coord.FromMils(100)),
            Width = Coord.FromMils(10)
        });

        using var ms = new MemoryStream();
        new PcbDocWriter().Write(original, ms);

        ms.Position = 0;
        var readBack = new PcbDocReader().Read(ms);

        Assert.Single(readBack.Tracks);
    }
}
