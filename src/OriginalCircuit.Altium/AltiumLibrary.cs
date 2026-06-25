using OriginalCircuit.Altium.Models;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Models.Project;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Serialization.Readers;

namespace OriginalCircuit.Altium;

/// <summary>
/// Main entry point for reading and creating Altium library files.
/// </summary>
public static class AltiumLibrary
{
    /// <summary>
    /// Opens an Altium library file asynchronously, automatically detecting the file type.
    /// </summary>
    /// <param name="path">Path to the library file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The opened library.</returns>
    /// <exception cref="NotSupportedException">If the file type is not recognized.</exception>
    public static async ValueTask<ILibrary> OpenAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".pcblib" => await OpenPcbLibAsync(path, cancellationToken).ConfigureAwait(false),
            ".schlib" => await OpenSchLibAsync(path, cancellationToken).ConfigureAwait(false),
            ".prjpcb" => throw new NotSupportedException(
                "A .PrjPcb project is not a library; use OpenProjectAsync()."),
            _ => throw new NotSupportedException(
                $"Unsupported file type: {extension}. " +
                $"For .SchDoc use OpenSchDocAsync(), for .PcbDoc use OpenPcbDocAsync(), " +
                $"for .PrjPcb use OpenProjectAsync().")
        };
    }

    /// <summary>
    /// Opens a PCB footprint library file.
    /// </summary>
    public static async ValueTask<IPcbLibrary> OpenPcbLibAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var reader = new PcbLibReader();
        return await reader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a PCB footprint library from a stream.
    /// </summary>
    public static ValueTask<IPcbLibrary> OpenPcbLibAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var reader = new PcbLibReader();
        return new ValueTask<IPcbLibrary>(reader.Read(stream));
    }

    /// <summary>
    /// Opens a schematic symbol library file.
    /// </summary>
    public static async ValueTask<ISchLibrary> OpenSchLibAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var reader = new SchLibReader();
        return await reader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a schematic symbol library from a stream.
    /// </summary>
    public static ValueTask<ISchLibrary> OpenSchLibAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var reader = new SchLibReader();
        return new ValueTask<ISchLibrary>(reader.Read(stream));
    }

    /// <summary>
    /// Opens a schematic document file.
    /// </summary>
    public static async ValueTask<ISchDocument> OpenSchDocAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var reader = new SchDocReader();
        return await reader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a schematic document from a stream.
    /// </summary>
    public static ValueTask<ISchDocument> OpenSchDocAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var reader = new SchDocReader();
        return new ValueTask<ISchDocument>(reader.Read(stream));
    }

    /// <summary>
    /// Opens a PCB document file.
    /// </summary>
    public static async ValueTask<IPcbDocument> OpenPcbDocAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var reader = new PcbDocReader();
        var document = await reader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (document is Models.Pcb.PcbDocument pcb)
            pcb.SourcePath = System.IO.Path.GetFullPath(path);
        return document;
    }

    /// <summary>
    /// Opens a PCB document from a stream.
    /// </summary>
    public static ValueTask<IPcbDocument> OpenPcbDocAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var reader = new PcbDocReader();
        return new ValueTask<IPcbDocument>(reader.Read(stream));
    }

    /// <summary>
    /// Opens an Altium project (.PrjPcb) file, also loading its sibling .PrjPcbStructure
    /// (the compiled logical sheet hierarchy) when present.
    /// </summary>
    public static async ValueTask<AltiumProject> OpenProjectAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var reader = new PrjPcbReader();
        return await reader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens an Altium project from a stream of .PrjPcb text. The sibling .PrjPcbStructure
    /// hierarchy file cannot be located from a stream, so it is not loaded.
    /// </summary>
    public static ValueTask<AltiumProject> OpenProjectAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var reader = new PrjPcbReader();
        return new ValueTask<AltiumProject>(reader.Read(stream));
    }

    /// <summary>
    /// Creates a new empty PCB project with a default <c>[Design]</c> section.
    /// </summary>
    public static AltiumProject CreateProject()
    {
        var project = new AltiumProject();
        project.Design.Version = "1.0";
        return project;
    }

    /// <summary>
    /// Creates a new empty PCB footprint library.
    /// </summary>
    public static IPcbLibrary CreatePcbLib() => new PcbLibrary();

    /// <summary>
    /// Creates a new empty schematic symbol library.
    /// </summary>
    public static ISchLibrary CreateSchLib() => new SchLibrary();

    /// <summary>
    /// Creates a new empty schematic document.
    /// </summary>
    public static ISchDocument CreateSchDoc() => new SchDocument();

    /// <summary>
    /// Creates a new empty PCB document.
    /// </summary>
    public static IPcbDocument CreatePcbDoc() => new PcbDocument();
}
