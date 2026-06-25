using System;
using System.IO;
using OpenMcdf;
using OriginalCircuit.Altium.Serialization.Compound;
using Xunit;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Regression tests for opening a "faulty"/not-fully-written compound file whose final sector is
/// truncated. OpenMcdf reads such a file leniently from a <see cref="FileStream"/> (a partial read past
/// EOF) but throws "...beyond the end of the stream" when it can address the backing buffer directly —
/// e.g. the growable <see cref="MemoryStream"/> an HTTP upload produces. The library pads the truncated
/// final sector so a board opens consistently from any stream source (issue: BoardViewer upload failing
/// to load a truncated board). See <see cref="CompoundFileAccessor.Open(Stream, bool)"/>.
/// </summary>
public class TruncatedCompoundFileTests
{
    // A valid V3 compound file (512-byte sectors) carrying one multi-sector stream, mirroring how the
    // library itself builds an in-memory root and copies the image out.
    private static byte[] BuildCompoundFile(int payloadBytes)
    {
        using var root = RootStorage.CreateInMemory(OpenMcdf.Version.V3);
        using (var stream = root.CreateStream("Payload"))
        {
            var data = new byte[payloadBytes];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)((i * 31 + 7) & 0xFF);
            stream.Write(data, 0, data.Length);
        }
        root.Flush();

        var image = root.BaseStream;
        image.Position = 0;
        using var outMs = new MemoryStream();
        image.CopyTo(outMs);
        return outMs.ToArray();
    }

    // The upload path: a growable MemoryStream (its internal buffer is over-allocated and publicly
    // visible), which is exactly what triggers OpenMcdf's strict in-buffer bounds check.
    private static MemoryStream ToGrowableStream(byte[] data)
    {
        var ms = new MemoryStream();
        ms.Write(data, 0, data.Length);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void RawOpenMcdf_TruncatedFinalSector_GrowableStream_Throws()
    {
        // Establishes the underlying behaviour the library works around: the same bytes that read fine
        // from a sized buffer throw from a growable MemoryStream once the final sector is truncated.
        var bytes = BuildCompoundFile(200_000);
        var truncated = bytes[..(bytes.Length - 100)];
        Assert.NotEqual(0, truncated.Length % 512); // a genuinely partial final sector

        Assert.ThrowsAny<Exception>(() =>
        {
            using var growable = ToGrowableStream(truncated);
            using var root = RootStorage.Open(growable, StorageModeFlags.LeaveOpen);
            // Touch the FAT/directory so a lazily-validating implementation still surfaces the error.
            foreach (var _ in root.EnumerateEntries()) { }
        });
    }

    [Fact]
    public void CompoundFileAccessor_Open_TruncatedFinalSector_GrowableStream_Succeeds()
    {
        var bytes = BuildCompoundFile(200_000);

        // The full, well-formed file opens unchanged (no copy) and exposes its stream.
        using (var full = ToGrowableStream(bytes))
        using (var accessor = CompoundFileAccessor.Open(full, leaveOpen: true))
            Assert.True(accessor.RootStorage.TryGetStream("Payload", out _));

        // Truncating the final sector must not make the file unreadable: the accessor pads the missing
        // tail and still opens, enumerates and finds the stream.
        var truncated = bytes[..(bytes.Length - 100)];
        using var growable = ToGrowableStream(truncated);
        using var truncatedAccessor = CompoundFileAccessor.Open(growable, leaveOpen: true);

        Assert.NotNull(truncatedAccessor.RootStorage);
        Assert.True(truncatedAccessor.RootStorage.TryGetStream("Payload", out _));
    }

    [Fact]
    public void CompoundFileAccessor_Open_WellFormedFile_OpensNormally()
    {
        // A sector-aligned file must not be copied/padded — it opens directly.
        var bytes = BuildCompoundFile(50_000);
        Assert.Equal(0, bytes.Length % 512);

        using var growable = ToGrowableStream(bytes);
        using var accessor = CompoundFileAccessor.Open(growable, leaveOpen: true);
        Assert.True(accessor.RootStorage.TryGetStream("Payload", out _));
    }
}
