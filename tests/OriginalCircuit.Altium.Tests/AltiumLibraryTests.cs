using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;

namespace OriginalCircuit.Altium.Tests;

public class AltiumLibraryTests
{
    [Fact]
    public void CreatePcbLib_ReturnsEmptyLibrary()
    {
        var lib = AltiumLibrary.CreatePcbLib();
        Assert.NotNull(lib);
        Assert.IsAssignableFrom<IPcbLibrary>(lib);
        Assert.Equal(0, lib.Count);
    }

    [Fact]
    public void CreateSchLib_ReturnsEmptyLibrary()
    {
        var lib = AltiumLibrary.CreateSchLib();
        Assert.NotNull(lib);
        Assert.IsAssignableFrom<ISchLibrary>(lib);
        Assert.Equal(0, lib.Count);
    }

    [Fact]
    public void CreateSchDoc_ReturnsEmptyDocument()
    {
        var doc = AltiumLibrary.CreateSchDoc();
        Assert.NotNull(doc);
        Assert.IsAssignableFrom<ISchDocument>(doc);
    }

    [Fact]
    public void CreatePcbDoc_ReturnsEmptyDocument()
    {
        var doc = AltiumLibrary.CreatePcbDoc();
        Assert.NotNull(doc);
        Assert.IsAssignableFrom<IPcbDocument>(doc);
    }

    [Fact]
    public async Task OpenAsync_UnsupportedExtension_ThrowsNotSupportedException()
    {
        var tempFile = Path.GetTempFileName() + ".xyz";
        try
        {
            File.WriteAllBytes(tempFile, [0]);
            await Assert.ThrowsAsync<NotSupportedException>(
                () => AltiumLibrary.OpenAsync(tempFile).AsTask());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void PcbLibrary_AddRemove_Components()
    {
        var lib = AltiumLibrary.CreatePcbLib();
        var comp = PcbComponent.Create("TestPad").Build();
        lib.Add(comp);

        Assert.Equal(1, lib.Count);
        Assert.True(lib.Contains("TestPad"));

        lib.Remove("TestPad");
        Assert.Equal(0, lib.Count);
        Assert.False(lib.Contains("TestPad"));
    }

    [Fact]
    public void SchLibrary_AddRemove_Components()
    {
        var lib = AltiumLibrary.CreateSchLib();
        var comp = SchComponent.Create("RESISTOR").Build();
        lib.Add(comp);

        Assert.Equal(1, lib.Count);
        Assert.True(lib.Contains("RESISTOR"));

        lib.Remove("RESISTOR");
        Assert.Equal(0, lib.Count);
    }
}
