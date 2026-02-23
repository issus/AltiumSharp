using OpenMcdf;

namespace OriginalCircuit.Altium.Serialization.Compound;

/// <summary>
/// Provides async-friendly access to COM Structured Storage (compound) files.
/// </summary>
/// <remarks>
/// This is a wrapper around OpenMcdf that provides a cleaner async API for the v2 library.
///
/// Evaluation notes on OpenMcdf vs custom implementation:
/// - OpenMcdf is a mature, well-tested library for COM compound file access
/// - The compound file format (OLE/COM structured storage) is complex
/// - Building a custom implementation would require significant effort with minimal benefit
/// - Performance bottlenecks are in parsing logic, not file I/O
/// - OpenMcdf handles edge cases and format variations well
///
/// Conclusion: Use OpenMcdf with a thin wrapper for async/modern API patterns.
/// </remarks>
internal sealed class CompoundFileAccessor : IAsyncDisposable, IDisposable
{
    private readonly CompoundFile _compoundFile;
    private readonly Stream? _stream;
    private readonly bool _leaveStreamOpen;
    private bool _disposed;

    /// <summary>
    /// Gets the root storage of the compound file.
    /// </summary>
    public CFStorage RootStorage => _compoundFile.RootStorage;

    private CompoundFileAccessor(CompoundFile cf, Stream? stream = null, bool leaveOpen = false)
    {
        _compoundFile = cf;
        _stream = stream;
        _leaveStreamOpen = leaveOpen;
    }

    /// <summary>
    /// Opens a compound file from a path.
    /// </summary>
    public static async ValueTask<CompoundFileAccessor> OpenAsync(
        string path,
        bool writable = false,
        CancellationToken cancellationToken = default)
    {
        // OpenMcdf doesn't have native async support, so we load the stream asynchronously
        // then pass it to OpenMcdf. For large files, consider memory-mapped approach.
        const FileMode mode = FileMode.Open; // Always open existing file
        var access = writable ? FileAccess.ReadWrite : FileAccess.Read;
        var share = writable ? FileShare.None : FileShare.Read;

        var stream = new FileStream(path, mode, access, share, 4096, useAsync: true);

        try
        {
            // Load file content for OpenMcdf (it needs seekable stream)
            var updateMode = writable ? CFSUpdateMode.Update : CFSUpdateMode.ReadOnly;
            var cf = new CompoundFile(stream, updateMode, CFSConfiguration.Default);
            return new CompoundFileAccessor(cf, stream, leaveOpen: false);
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Opens a compound file from a stream.
    /// </summary>
    public static CompoundFileAccessor Open(Stream stream, bool leaveOpen = false)
    {
        var cf = new CompoundFile(stream, CFSUpdateMode.ReadOnly, CFSConfiguration.Default);
        return new CompoundFileAccessor(cf, stream, leaveOpen);
    }

    /// <summary>
    /// Creates a new compound file.
    /// </summary>
    public static CompoundFileAccessor Create()
    {
        var cf = new CompoundFile(CFSVersion.Ver_3, CFSConfiguration.Default);
        return new CompoundFileAccessor(cf);
    }

    /// <summary>
    /// Gets a storage (directory) by path.
    /// </summary>
    /// <param name="path">Path using / as separator.</param>
    public CFStorage? TryGetStorage(string path)
    {
        return TryNavigate(path) as CFStorage;
    }

    /// <summary>
    /// Gets a stream by path.
    /// </summary>
    /// <param name="path">Path using / as separator.</param>
    public CFStream? TryGetStream(string path)
    {
        return TryNavigate(path) as CFStream;
    }

    /// <summary>
    /// Gets the data from a stream.
    /// </summary>
    public byte[] GetStreamData(string path)
    {
        var stream = TryGetStream(path)
            ?? throw new ArgumentException($"Stream '{path}' not found");
        return stream.GetData();
    }

    /// <summary>
    /// Gets stream data as a Memory for async-friendly access.
    /// </summary>
    public ReadOnlyMemory<byte> GetStreamDataAsMemory(string path)
    {
        return GetStreamData(path);
    }

    /// <summary>
    /// Enumerates all child items in a storage.
    /// </summary>
    public IEnumerable<CFItem> EnumerateChildren(CFStorage storage)
    {
        var items = new List<CFItem>();
        storage.VisitEntries(item => items.Add(item), recursive: false);
        return items;
    }

    /// <summary>
    /// Enumerates all child storages in a storage.
    /// </summary>
    public IEnumerable<CFStorage> EnumerateStorages(CFStorage storage)
    {
        return EnumerateChildren(storage).OfType<CFStorage>();
    }

    /// <summary>
    /// Enumerates all child streams in a storage.
    /// </summary>
    public IEnumerable<CFStream> EnumerateStreams(CFStorage storage)
    {
        return EnumerateChildren(storage).OfType<CFStream>();
    }

    /// <summary>
    /// Saves the compound file to a new path.
    /// </summary>
    public async ValueTask SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        // OpenMcdf Save is synchronous, but we can still use async file operations
        using var memStream = new MemoryStream();
        _compoundFile.Save(memStream);

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        memStream.Position = 0;
        await memStream.CopyToAsync(fileStream, cancellationToken);
    }

    /// <summary>
    /// Saves the compound file to a stream.
    /// </summary>
    public void Save(Stream stream)
    {
        _compoundFile.Save(stream);
    }

    private CFItem? TryNavigate(string path)
    {
        if (string.IsNullOrEmpty(path))
            return RootStorage;

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        CFItem? current = RootStorage;

        foreach (var part in parts)
        {
            if (current is not CFStorage storage)
                return null;

            current = TryGetChild(storage, part);
            if (current == null)
                return null;
        }

        return current;
    }

    private static CFItem? TryGetChild(CFStorage storage, string name)
    {
        if (storage.TryGetStorage(name, out var childStorage))
            return childStorage;
        if (storage.TryGetStream(name, out var childStream))
            return childStream;
        return null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _compoundFile.Close();

        if (_stream != null && !_leaveStreamOpen)
        {
            _stream.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _compoundFile.Close();

        if (_stream != null && !_leaveStreamOpen)
        {
            await _stream.DisposeAsync();
        }
    }
}
