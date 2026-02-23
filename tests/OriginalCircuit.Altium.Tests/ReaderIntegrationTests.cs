using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models.Sch;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Integration tests that read real Altium library files.
/// </summary>
public class ReaderIntegrationTests
{
    private static string GetExamplesPath()
    {
        // Navigate from test output to Examples folder
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "Examples");
    }

    private static string GetTestDataPath()
    {
        var current = Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "TestData");
    }

    [SkippableFact]
    public async Task SchLibReader_CanReadSinglePinGndFile()
    {
        // Arrange
        var examplesPath = GetExamplesPath();
        var filePath = Path.Combine(examplesPath, "SinglePinGND.SchLib");

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        // Act
        var library = await AltiumLibrary.OpenSchLibAsync(filePath);

        // Assert
        Assert.NotNull(library);
        Assert.True(library.Count >= 0, "Library should have zero or more components");
    }

    [SkippableFact]
    public async Task SchLibReader_CanReadStm32File()
    {
        // Arrange
        var examplesPath = GetExamplesPath();
        var filePath = Path.Combine(examplesPath, "SCH - MCU - STM32 - ST MICROELECTRONICS STM32U535NEYXQ WLCSP56.SchLib");

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        // Act
        var library = await AltiumLibrary.OpenSchLibAsync(filePath);

        // Assert
        Assert.NotNull(library);
        Assert.True(library.Count >= 0, "Library should have zero or more components");

        // If library has components, check that they have valid data
        foreach (var component in library.Components)
        {
            Assert.NotNull(component.Name);
        }
    }

    [SkippableFact]
    public async Task AltiumLibrary_OpenAsync_DetectsSchLibType()
    {
        // Arrange
        var examplesPath = GetExamplesPath();
        var filePath = Path.Combine(examplesPath, "SinglePinGND.SchLib");

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        // Act
        var library = await AltiumLibrary.OpenAsync(filePath);

        // Assert
        Assert.NotNull(library);
        Assert.IsAssignableFrom<ISchLibrary>(library);
    }

    [Fact]
    public void AltiumLibrary_CreateSchLib_ReturnsEmptyLibrary()
    {
        // Act
        var library = AltiumLibrary.CreateSchLib();

        // Assert
        Assert.NotNull(library);
        Assert.Equal(0, library.Count);
        Assert.Empty(library.Components);
    }

    [Fact]
    public void AltiumLibrary_CreatePcbLib_ReturnsEmptyLibrary()
    {
        // Act
        var library = AltiumLibrary.CreatePcbLib();

        // Assert
        Assert.NotNull(library);
        Assert.Equal(0, library.Count);
        Assert.Empty(library.Components);
    }

    [Fact]
    public async Task AltiumLibrary_OpenAsync_ThrowsForUnsupportedType()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".unsupported";

        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0, 1, 2, 3 });

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(
                () => AltiumLibrary.OpenAsync(tempFile).AsTask());
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [SkippableTheory]
    [InlineData("DAC.SchDoc")]
    [InlineData("Fanout Buffer.SchDoc")]
    [InlineData("Level Shifter.SchDoc")]
    [InlineData("Overview.SchDoc")]
    [InlineData("Power Supplies.SchDoc")]
    [InlineData("Power Supply.SchDoc")]
    [InlineData("SPI Isolator.SchDoc")]
    [InlineData("USB Power.SchDoc")]
    public async Task SchDocReader_CanReadTestFile(string fileName)
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, fileName);

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        var document = await AltiumLibrary.OpenSchDocAsync(filePath);

        Assert.NotNull(document);
    }

    [SkippableFact]
    public async Task SchDocReader_DacFile_HasExpectedPrimitives()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "DAC.SchDoc");

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        var document = await AltiumLibrary.OpenSchDocAsync(filePath);

        Assert.NotNull(document);
        // DAC schematic should have components, wires, and net labels
        Assert.True(document.Components.Count > 0, "Should have components");
        Assert.True(document.Wires.Count > 0, "Should have wires");
    }

    [SkippableTheory]
    [InlineData("MAX5719 Breakout.PcbDoc")]
    [InlineData("Power Adapter Panel.PcbDoc")]
    [InlineData("SPI Isolator Panel.PcbDoc")]
    [InlineData("SPI Isolator.PcbDoc")]
    [InlineData("USB Power Adapter.PcbDoc")]
    [InlineData("VCOCXO Breakout.PcbDoc")]
    public async Task PcbDocReader_CanReadTestFile(string fileName)
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, fileName);

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        var document = await AltiumLibrary.OpenPcbDocAsync(filePath);

        Assert.NotNull(document);
    }

    [SkippableFact]
    public async Task PcbDocReader_PowerAdapterPanel_HasEmbeddedBoards()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "Power Adapter Panel.PcbDoc");

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        var document = (OriginalCircuit.Altium.Models.Pcb.PcbDocument)await AltiumLibrary.OpenPcbDocAsync(filePath);

        Assert.NotNull(document);
        Assert.True(document.EmbeddedBoards.Count > 0,
            $"Expected >0 embedded boards but got {document.EmbeddedBoards.Count}");
        var eb = document.EmbeddedBoards[0];

        // Identity & path
        Assert.Contains("USB Power Adapter.PcbDoc", eb.DocumentPath, StringComparison.OrdinalIgnoreCase);

        // Layer & orientation
        Assert.Equal(1, eb.Layer);
        Assert.Equal(0.0, eb.Rotation);
        Assert.False(eb.MirrorFlag);
        Assert.Equal(1, eb.OriginMode);

        // Array layout
        Assert.Equal(4, eb.RowCount);
        Assert.Equal(3, eb.ColCount);
        // Coords are stored as "mil" values so use mil comparison (tolerance for float conversion)
        Assert.InRange(eb.RowSpacing.ToMils(), 964.5, 964.6);
        Assert.InRange(eb.ColSpacing.ToMils(), 1456.6, 1456.7);

        // Bounding box
        Assert.InRange(eb.X1Location.ToMils(), 1338.5, 1338.6);
        Assert.InRange(eb.Y1Location.ToMils(), 1751.9, 1752.0);
        Assert.InRange(eb.X2Location.ToMils(), 5629.9, 5630.0);
        Assert.InRange(eb.Y2Location.ToMils(), 5531.4, 5531.5);

        // Viewport
        Assert.True(eb.ViewportVisible);

        // State flags
        Assert.True(eb.UserRouted);
        Assert.False(eb.IsKeepout);
        Assert.False(eb.PolygonOutline);
    }

    [SkippableFact]
    public async Task PcbDocReader_SpiIsolatorPanel_HasEmbeddedBoards()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "SPI Isolator Panel.PcbDoc");

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        var document = (OriginalCircuit.Altium.Models.Pcb.PcbDocument)await AltiumLibrary.OpenPcbDocAsync(filePath);

        Assert.NotNull(document);
        Assert.True(document.EmbeddedBoards.Count > 0);
        var eb = document.EmbeddedBoards[0];

        // Different file, different values — cross-validates the reader
        Assert.Contains("SPI Isolator.PcbDoc", eb.DocumentPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, eb.Layer);
        Assert.Equal(3, eb.RowCount);
        Assert.Equal(3, eb.ColCount);
        Assert.InRange(eb.RowSpacing.ToMils(), 1555.1, 1555.2);
        Assert.InRange(eb.ColSpacing.ToMils(), 2047.2, 2047.3);
        Assert.InRange(eb.X1Location.ToMils(), 275.5, 275.6);
        Assert.InRange(eb.Y1Location.ToMils(), 472.4, 472.5);
    }

    [SkippableFact]
    public async Task PcbDocReader_SpiIsolator_HasExpectedPrimitives()
    {
        var testDataPath = GetTestDataPath();
        var filePath = Path.Combine(testDataPath, "SPI Isolator.PcbDoc");

        if (!File.Exists(filePath))
        {
            Skip.If(true, "Test data not available");
            return;
        }

        var document = await AltiumLibrary.OpenPcbDocAsync(filePath);

        Assert.NotNull(document);
        // A real PCB should have tracks, pads, and vias
        Assert.True(document.Tracks.Count > 0, "Should have tracks");
        Assert.True(document.Pads.Count > 0, "Should have pads");
        Assert.True(document.Vias.Count > 0, "Should have vias");
    }
}
