using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Serialization.Readers;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests for error handling: malformed files, wrong formats, empty files, etc.
/// </summary>
public class NegativeTests
{
    [Fact]
    public void PcbLibReader_EmptyStream_ThrowsCorruptFileException()
    {
        using var ms = new MemoryStream();
        var reader = new PcbLibReader();

        Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
    }

    [Fact]
    public void SchLibReader_EmptyStream_ThrowsCorruptFileException()
    {
        using var ms = new MemoryStream();
        var reader = new SchLibReader();

        Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
    }

    [Fact]
    public void SchDocReader_EmptyStream_ThrowsCorruptFileException()
    {
        using var ms = new MemoryStream();
        var reader = new SchDocReader();

        Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
    }

    [Fact]
    public void PcbDocReader_EmptyStream_ThrowsCorruptFileException()
    {
        using var ms = new MemoryStream();
        var reader = new PcbDocReader();

        Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
    }

    [Fact]
    public void PcbLibReader_RandomBytes_ThrowsCorruptFileException()
    {
        var random = new Random(42);
        var bytes = new byte[1024];
        random.NextBytes(bytes);

        using var ms = new MemoryStream(bytes);
        var reader = new PcbLibReader();

        Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
    }

    [Fact]
    public void SchLibReader_RandomBytes_ThrowsCorruptFileException()
    {
        var random = new Random(42);
        var bytes = new byte[1024];
        random.NextBytes(bytes);

        using var ms = new MemoryStream(bytes);
        var reader = new SchLibReader();

        Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
    }

    [Fact]
    public void SchDocReader_RandomBytes_ThrowsCorruptFileException()
    {
        var random = new Random(42);
        var bytes = new byte[1024];
        random.NextBytes(bytes);

        using var ms = new MemoryStream(bytes);
        var reader = new SchDocReader();

        Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
    }

    [Fact]
    public void PcbDocReader_RandomBytes_ThrowsCorruptFileException()
    {
        var random = new Random(42);
        var bytes = new byte[1024];
        random.NextBytes(bytes);

        using var ms = new MemoryStream(bytes);
        var reader = new PcbDocReader();

        Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
    }

    [Fact]
    public void CorruptFileException_PreservesInnerException()
    {
        var random = new Random(42);
        var bytes = new byte[1024];
        random.NextBytes(bytes);

        using var ms = new MemoryStream(bytes);
        var reader = new PcbLibReader();

        var ex = Assert.Throws<AltiumCorruptFileException>(() => reader.Read(ms));
        Assert.NotNull(ex.InnerException);
        Assert.Contains("PcbLib", ex.Message);
    }

    [Fact]
    public async Task CorruptFileException_PreservesFilePath()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02, 0x03 });
            var reader = new PcbLibReader();

            var ex = await Assert.ThrowsAsync<AltiumCorruptFileException>(
                () => reader.ReadAsync(tempFile).AsTask());
            Assert.Equal(tempFile, ex.FilePath);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AltiumLibrary_OpenAsync_UnsupportedExtension_Throws()
    {
        var tempFile = Path.GetTempFileName() + ".xyz";
        try
        {
            File.WriteAllBytes(tempFile, [0xD0, 0xCF, 0x11, 0xE0]); // Partial OLE header
            await Assert.ThrowsAsync<NotSupportedException>(
                () => AltiumLibrary.OpenAsync(tempFile).AsTask());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AltiumLibrary_OpenAsync_FileNotFound_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(
            () => AltiumLibrary.OpenAsync("/nonexistent/file.PcbLib").AsTask());
    }

    [Fact]
    public async Task AltiumLibrary_OpenPcbDocAsync_FileNotFound_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(
            () => AltiumLibrary.OpenPcbDocAsync("/nonexistent/file.PcbDoc").AsTask());
    }

    [Fact]
    public async Task AltiumLibrary_OpenSchDocAsync_FileNotFound_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(
            () => AltiumLibrary.OpenSchDocAsync("/nonexistent/file.SchDoc").AsTask());
    }

    [Fact]
    public async Task PcbLibReader_NonExistentPath_ThrowsIOException()
    {
        var path = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid().ToString("N") + ".PcbLib");
        var reader = new PcbLibReader();
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => reader.ReadAsync(path).AsTask());
    }

    [Fact]
    public async Task SchLibReader_NonExistentPath_ThrowsIOException()
    {
        var path = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid().ToString("N") + ".SchLib");
        var reader = new SchLibReader();
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => reader.ReadAsync(path).AsTask());
    }

    [Fact]
    public async Task SchDocReader_NonExistentPath_ThrowsIOException()
    {
        var path = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid().ToString("N") + ".SchDoc");
        var reader = new SchDocReader();
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => reader.ReadAsync(path).AsTask());
    }

    [Fact]
    public async Task PcbDocReader_NonExistentPath_ThrowsIOException()
    {
        var path = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid().ToString("N") + ".PcbDoc");
        var reader = new PcbDocReader();
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => reader.ReadAsync(path).AsTask());
    }

    [Fact]
    public void CreateEmptyLibraries_AreUsable()
    {
        var pcbLib = AltiumLibrary.CreatePcbLib();
        Assert.Empty(pcbLib.Components);
        Assert.Equal(0, pcbLib.Count);

        var schLib = AltiumLibrary.CreateSchLib();
        Assert.Empty(schLib.Components);
        Assert.Equal(0, schLib.Count);

        var schDoc = AltiumLibrary.CreateSchDoc();
        Assert.Empty(schDoc.Components);
        Assert.Empty(schDoc.Wires);

        var pcbDoc = AltiumLibrary.CreatePcbDoc();
        Assert.Empty(pcbDoc.Components);
        Assert.Empty(pcbDoc.Tracks);
        Assert.Empty(pcbDoc.Pads);
    }

    [Fact]
    public void NewDocuments_HaveEmptyDiagnostics()
    {
        var pcbLib = new OriginalCircuit.Altium.Models.Pcb.PcbLibrary();
        Assert.Empty(pcbLib.Diagnostics);

        var schLib = new OriginalCircuit.Altium.Models.Sch.SchLibrary();
        Assert.Empty(schLib.Diagnostics);

        var schDoc = new OriginalCircuit.Altium.Models.Sch.SchDocument();
        Assert.Empty(schDoc.Diagnostics);

        var pcbDoc = new OriginalCircuit.Altium.Models.Pcb.PcbDocument();
        Assert.Empty(pcbDoc.Diagnostics);
    }

    [Fact]
    public void ExceptionHierarchy_IsCorrect()
    {
        var baseEx = new AltiumFileException("test", filePath: "/test.PcbLib");
        Assert.IsAssignableFrom<Exception>(baseEx);
        Assert.Equal("/test.PcbLib", baseEx.FilePath);

        var corruptEx = new AltiumCorruptFileException("corrupt", streamName: "Data", filePath: "/test.PcbLib");
        Assert.IsAssignableFrom<AltiumFileException>(corruptEx);
        Assert.Equal("Data", corruptEx.StreamName);

        var unsupportedEx = new AltiumUnsupportedFeatureException("unsupported", recordType: 99);
        Assert.IsAssignableFrom<AltiumFileException>(unsupportedEx);
        Assert.Equal(99, unsupportedEx.RecordType);
    }

    [Fact]
    public void DiagnosticRecord_Properties()
    {
        var diag = new AltiumDiagnostic(DiagnosticSeverity.Warning, "Test message", "TestStream", 42);
        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        Assert.Equal("Test message", diag.Message);
        Assert.Equal("TestStream", diag.StreamName);
        Assert.Equal(42, diag.RecordIndex);
    }
}
