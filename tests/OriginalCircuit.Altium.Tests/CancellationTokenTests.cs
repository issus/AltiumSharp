using OriginalCircuit.Altium;
using OriginalCircuit.Altium.Models;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.Altium.Tests;

/// <summary>
/// Tests that verify CancellationToken behavior on async read and write methods.
/// </summary>
public class CancellationTokenTests
{
    private static readonly CancellationToken CancelledToken = new(canceled: true);

    // --- Save tests (writer) ---

    [Fact]
    public async Task PcbLib_SaveAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var library = AltiumLibrary.CreatePcbLib();
        using var stream = new MemoryStream();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => library.SaveAsync(stream, ct: CancelledToken).AsTask());
    }

    [Fact]
    public async Task SchLib_SaveAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var library = AltiumLibrary.CreateSchLib();
        using var stream = new MemoryStream();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => library.SaveAsync(stream, ct: CancelledToken).AsTask());
    }

    // --- Read tests (reader) ---
    // These create a valid library file on disk, then attempt to read it
    // with a pre-cancelled token. The reader checks the token while iterating
    // components, so the library must contain at least one component.

    [Fact]
    public async Task OpenPcbLibAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange: create a valid PcbLib file with one component
        var library = AltiumLibrary.CreatePcbLib();
        var component = PcbComponent.Create("TestPad")
            .AddPad(pad => pad
                .At(Coord.FromMils(0), Coord.FromMils(0))
                .Size(Coord.FromMils(50), Coord.FromMils(50))
                .WithDesignator("1")
                .Smd())
            .Build();
        library.Add(component);

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".PcbLib");
        try
        {
            await library.SaveAsync(tempPath, new OriginalCircuit.Eda.Models.SaveOptions());

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => AltiumLibrary.OpenPcbLibAsync(tempPath, CancelledToken).AsTask());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task OpenSchLibAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange: create a valid SchLib file with one component
        var library = AltiumLibrary.CreateSchLib();
        var component = SchComponent.Create("RESISTOR").Build();
        library.Add(component);

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".SchLib");
        try
        {
            await library.SaveAsync(tempPath, new OriginalCircuit.Eda.Models.SaveOptions());

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => AltiumLibrary.OpenSchLibAsync(tempPath, CancelledToken).AsTask());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
