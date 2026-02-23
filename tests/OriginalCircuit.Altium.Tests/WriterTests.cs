using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests for library writers.
/// </summary>
public class WriterTests
{
    [Fact]
    public async Task PcbLibWriter_CanWriteEmptyLibrary()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task PcbLibWriter_CanWriteLibraryWithComponent()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("R0402")
            .WithDescription("0402 Resistor")
            .WithHeight(Coord.FromMils(20))
            .AddPad(pad => pad
                .At(Coord.FromMils(-25), Coord.Zero)
                .Size(Coord.FromMils(30), Coord.FromMils(35))
                .WithDesignator("1")
                .Smd())
            .AddPad(pad => pad
                .At(Coord.FromMils(25), Coord.Zero)
                .Size(Coord.FromMils(30), Coord.FromMils(35))
                .WithDesignator("2")
                .Smd())
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task PcbLibWriter_RoundTrip_PreservesComponentCount()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        var component1 = PcbComponent.Create("R0402")
            .WithDescription("0402 Resistor")
            .AddPad(pad => pad
                .At(Coord.FromMils(-25), Coord.Zero)
                .Size(Coord.FromMils(30), Coord.FromMils(35))
                .WithDesignator("1"))
            .Build();

        var component2 = PcbComponent.Create("C0402")
            .WithDescription("0402 Capacitor")
            .AddPad(pad => pad
                .At(Coord.FromMils(-25), Coord.Zero)
                .Size(Coord.FromMils(30), Coord.FromMils(35))
                .WithDesignator("1"))
            .Build();

        library.Add(component1);
        library.Add(component2);

        // Act - Write to stream
        using var stream = new MemoryStream();
        await library.SaveAsync(stream);

        // Read back
        stream.Position = 0;
        var rereadLibrary = await AltiumLibrary.OpenPcbLibAsync(stream);

        // Assert
        Assert.Equal(2, rereadLibrary.Components.Count);
    }

    [Fact]
    public async Task PcbLibWriter_RoundTrip_PreservesComponentNames()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("TestComponent123")
            .WithDescription("Test Description")
            .Build();

        library.Add(component);

        // Act - Write to stream
        using var stream = new MemoryStream();
        await library.SaveAsync(stream);

        // Read back
        stream.Position = 0;
        var rereadLibrary = await AltiumLibrary.OpenPcbLibAsync(stream);

        // Assert
        Assert.Single(rereadLibrary.Components);
        Assert.Equal("TestComponent123", rereadLibrary.Components[0].Name);
    }

    [Fact]
    public async Task PcbLibWriter_CanWriteLibraryWithTracks()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("TrackTest")
            .AddTrack(track => track
                .From(Coord.Zero, Coord.Zero)
                .To(Coord.FromMils(100), Coord.FromMils(100))
                .Width(Coord.FromMils(10))
                .OnLayer(1))
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");

        // Read back and verify
        stream.Position = 0;
        var rereadLibrary = await AltiumLibrary.OpenPcbLibAsync(stream);
        Assert.Single(rereadLibrary.Components);
        Assert.Single(rereadLibrary.Components[0].Tracks);
    }

    [Fact]
    public async Task PcbLibWriter_CanWriteLibraryWithArcs()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("ArcTest")
            .AddArc(arc => arc
                .Center(Coord.Zero, Coord.Zero)
                .Radius(Coord.FromMils(50))
                .Angles(0, 360)
                .Width(Coord.FromMils(10)))
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task PcbLibWriter_CanWriteLibraryWithVias()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("ViaTest")
            .AddVia(via => via
                .At(Coord.FromMils(50), Coord.FromMils(50))
                .Diameter(Coord.FromMils(30))
                .HoleSize(Coord.FromMils(15))
                .ThroughHole())
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task PcbLibWriter_CanWriteLibraryWithText()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("TextTest")
            .AddText("REF**", text => text
                .At(Coord.Zero, Coord.FromMils(50))
                .Height(Coord.FromMils(40))
                .OnLayer(21))  // Top Overlay
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task PcbLibWriter_CanWriteToFile()
    {
        // Arrange
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("R0402")
            .WithDescription("0402 Resistor")
            .AddPad(pad => pad
                .At(Coord.FromMils(-25), Coord.Zero)
                .Size(Coord.FromMils(30), Coord.FromMils(35))
                .WithDesignator("1"))
            .Build();

        library.Add(component);
        var tempPath = Path.GetTempFileName() + ".PcbLib";

        try
        {
            // Act
            await library.SaveAsync(tempPath, new OriginalCircuit.Eda.Models.SaveOptions());

            // Assert
            Assert.True(File.Exists(tempPath));
            var fileInfo = new FileInfo(tempPath);
            Assert.True(fileInfo.Length > 0);

            // Read back to verify
            var rereadLibrary = await AltiumLibrary.OpenPcbLibAsync(tempPath);
            Assert.Single(rereadLibrary.Components);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    // ========== SchLib Writer Tests ==========

    [Fact]
    public async Task SchLibWriter_CanWriteEmptyLibrary()
    {
        // Arrange
        var library = AltiumLibrary.CreateSchLib();
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task SchLibWriter_CanWriteLibraryWithComponent()
    {
        // Arrange
        var library = AltiumLibrary.CreateSchLib();
        var component = SchComponent.Create("RES")
            .WithDescription("Resistor Symbol")
            .WithDesignatorPrefix("R")
            .AddRectangle(rect => rect
                .From(Coord.FromMils(-50), Coord.FromMils(-100))
                .To(Coord.FromMils(50), Coord.FromMils(100))
                .LineWidth(Coord.FromMils(10)))
            .AddPin(pin => pin
                .WithName("1")
                .At(Coord.FromMils(-150), Coord.Zero)
                .Length(Coord.FromMils(100))
                .Orient(PinOrientation.Right))
            .AddPin(pin => pin
                .WithName("2")
                .At(Coord.FromMils(150), Coord.Zero)
                .Length(Coord.FromMils(100))
                .Orient(PinOrientation.Left))
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task SchLibWriter_RoundTrip_PreservesComponentCount()
    {
        // Arrange
        var library = AltiumLibrary.CreateSchLib();
        var component1 = SchComponent.Create("RES")
            .WithDescription("Resistor")
            .AddPin(pin => pin
                .WithName("1")
                .At(Coord.FromMils(-100), Coord.Zero)
                .Length(Coord.FromMils(50)))
            .Build();

        var component2 = SchComponent.Create("CAP")
            .WithDescription("Capacitor")
            .AddPin(pin => pin
                .WithName("1")
                .At(Coord.FromMils(-100), Coord.Zero)
                .Length(Coord.FromMils(50)))
            .Build();

        library.Add(component1);
        library.Add(component2);

        // Act - Write to stream
        using var stream = new MemoryStream();
        await library.SaveAsync(stream);

        // Read back
        stream.Position = 0;
        var rereadLibrary = await AltiumLibrary.OpenSchLibAsync(stream);

        // Assert
        Assert.Equal(2, rereadLibrary.Components.Count);
    }

    [Fact]
    public async Task SchLibWriter_RoundTrip_PreservesComponentNames()
    {
        // Arrange
        var library = AltiumLibrary.CreateSchLib();
        var component = SchComponent.Create("TestSymbol123")
            .WithDescription("Test Description")
            .Build();

        library.Add(component);

        // Act - Write to stream
        using var stream = new MemoryStream();
        await library.SaveAsync(stream);

        // Read back
        stream.Position = 0;
        var rereadLibrary = await AltiumLibrary.OpenSchLibAsync(stream);

        // Assert
        Assert.Single(rereadLibrary.Components);
        Assert.Equal("TestSymbol123", rereadLibrary.Components[0].Name);
    }

    [Fact]
    public async Task SchLibWriter_CanWriteLibraryWithLines()
    {
        // Arrange
        var library = AltiumLibrary.CreateSchLib();
        var component = SchComponent.Create("LineTest")
            .AddLine(line => line
                .From(Coord.Zero, Coord.Zero)
                .To(Coord.FromMils(100), Coord.FromMils(100))
                .Width(Coord.FromMils(10)))
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task SchLibWriter_CanWriteLibraryWithRectangles()
    {
        // Arrange
        var library = AltiumLibrary.CreateSchLib();
        var component = SchComponent.Create("RectTest")
            .AddRectangle(rect => rect
                .From(Coord.FromMils(-50), Coord.FromMils(-50))
                .To(Coord.FromMils(50), Coord.FromMils(50))
                .Filled(true)
                .FillColor(0x00FF00))
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task SchLibWriter_CanWriteLibraryWithLabels()
    {
        // Arrange
        var library = AltiumLibrary.CreateSchLib();
        var component = SchComponent.Create("LabelTest")
            .AddLabel("Test Label", label => label
                .At(Coord.Zero, Coord.FromMils(50))
                .Font(1)
                .Justify(SchTextJustification.MiddleCenter))
            .Build();

        library.Add(component);
        using var stream = new MemoryStream();

        // Act
        await library.SaveAsync(stream);

        // Assert
        Assert.True(stream.Length > 0, "Written file should have content");
    }

    [Fact]
    public async Task SchLibWriter_CanWriteToFile()
    {
        // Arrange
        var library = AltiumLibrary.CreateSchLib();
        var component = SchComponent.Create("RES")
            .WithDescription("Resistor Symbol")
            .AddPin(pin => pin
                .WithName("1")
                .At(Coord.FromMils(-100), Coord.Zero)
                .Length(Coord.FromMils(50)))
            .Build();

        library.Add(component);
        var tempPath = Path.GetTempFileName() + ".SchLib";

        try
        {
            // Act
            await library.SaveAsync(tempPath, new OriginalCircuit.Eda.Models.SaveOptions());

            // Assert
            Assert.True(File.Exists(tempPath));
            var fileInfo = new FileInfo(tempPath);
            Assert.True(fileInfo.Length > 0);

            // Read back to verify
            var rereadLibrary = await AltiumLibrary.OpenSchLibAsync(tempPath);
            Assert.Single(rereadLibrary.Components);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
