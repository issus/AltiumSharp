using OpenMcdf;

namespace OriginalCircuit.Altium.Serialization.Compound;

/// <summary>
/// Provides async-friendly access to COM Structured Storage (compound) files.
/// </summary>
/// <remarks>
/// This is a thin wrapper around OpenMcdf 3.x. It fully encapsulates the OpenMcdf API surface
/// (<see cref="RootStorage"/>/<see cref="Storage"/>/<see cref="CfbStream"/>/<see cref="EntryInfo"/>)
/// behind the library-neutral <see cref="Compound.CompoundStorage"/>/<see cref="CompoundStream"/>
/// types so readers and writers never reference OpenMcdf directly.
///
/// OpenMcdf 3.x replaced the 2.x <c>CompoundFile</c>/<c>CFStorage</c>/<c>CFStream</c> model with a
/// root-storage that streams directly to its backing store. For reads we open the source stream
/// (which now hardens against the cyclic directory-entry DoS the 2.3.0 advisories described). For
/// writes we build in an in-memory root storage and copy the finished image out in <see cref="Save"/>,
/// preserving the previous "build, then Save(stream)" usage shape.
/// </remarks>
internal sealed class CompoundFileAccessor : IAsyncDisposable, IDisposable
{
    private readonly OpenMcdf.RootStorage _root;
    private readonly Stream? _stream;
    private readonly bool _leaveStreamOpen;
    private CompoundStorage? _rootStorage;
    private bool _disposed;

    /// <summary>
    /// Gets the root storage of the compound file.
    /// </summary>
    public CompoundStorage RootStorage => _rootStorage ??= new CompoundStorage(_root);

    private CompoundFileAccessor(OpenMcdf.RootStorage root, Stream? stream = null, bool leaveOpen = false)
    {
        _root = root;
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
        cancellationToken.ThrowIfCancellationRequested();

        const FileMode mode = FileMode.Open; // Always open an existing file
        var access = writable ? FileAccess.ReadWrite : FileAccess.Read;
        var share = writable ? FileShare.None : FileShare.Read;

        var stream = new FileStream(path, mode, access, share, 4096, useAsync: true);

        try
        {
            // OpenMcdf reads synchronously from the seekable stream. LeaveOpen lets this accessor
            // own the FileStream's lifetime.
            var root = OpenMcdf.RootStorage.Open(stream, StorageModeFlags.LeaveOpen);
            return new CompoundFileAccessor(root, stream, leaveOpen: false);
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Opens a compound file from a stream.
    /// </summary>
    public static CompoundFileAccessor Open(Stream stream, bool leaveOpen = false)
    {
        // A compound file's length must be a whole number of sectors. A board whose final sector is
        // truncated — a "faulty"/not-fully-written file — makes OpenMcdf throw "...beyond the end of
        // the stream" when it can address the backing buffer directly, e.g. the growable MemoryStream
        // an HTTP upload produces, even though a FileStream of the same bytes is read leniently (a
        // partial read past EOF). Pad the truncated tail with zeros so the file opens consistently from
        // any stream source. Only misaligned (truncated) streams are copied; well-formed ones open as-is.
        var aligned = EnsureSectorAligned(stream);
        if (!ReferenceEquals(aligned, stream))
        {
            // The padded branch takes ownership of the source stream (consumed into the copy), so on
            // failure dispose both the copy and — when the caller didn't ask to keep it — the source,
            // mirroring the dispose-on-throw guarantee the path-based OpenAsync gives.
            try
            {
                var paddedRoot = OpenMcdf.RootStorage.Open(aligned, StorageModeFlags.LeaveOpen);
                if (!leaveOpen) stream.Dispose(); // the caller's stream has been fully consumed into the copy
                return new CompoundFileAccessor(paddedRoot, aligned, leaveOpen: false);
            }
            catch
            {
                aligned.Dispose();
                if (!leaveOpen) stream.Dispose();
                throw;
            }
        }

        var root = OpenMcdf.RootStorage.Open(stream, StorageModeFlags.LeaveOpen);
        return new CompoundFileAccessor(root, stream, leaveOpen);
    }

    // Bytes of the CFB header read to validate the magic and reach the sector-shift field (at offset 30).
    private const int MinHeaderBytes = 32;

    // If the stream's length is not a whole number of sectors (a truncated final sector), returns a
    // zero-padded read-only copy rounded up to the sector boundary; otherwise returns the stream
    // unchanged. The copy completes the partial last sector so OpenMcdf's strict in-buffer bounds
    // check (used when it can access a MemoryStream's buffer) does not trip on the missing tail bytes.
    private static Stream EnsureSectorAligned(Stream stream)
    {
        if (!stream.CanSeek) return stream; // not measurable; let OpenMcdf read it directly

        long length = stream.Length;
        int sectorSize = ReadSectorSize(stream);
        if (sectorSize <= 0 || length == 0 || length % sectorSize == 0) return stream;
        if (length > int.MaxValue - sectorSize) return stream; // don't buffer pathologically large files

        long padded = ((length / sectorSize) + 1) * sectorSize;
        var buffer = new byte[padded];
        long origin = stream.Position;
        stream.Position = 0;
        stream.ReadExactly(buffer.AsSpan(0, (int)length));
        stream.Position = origin;
        // publiclyVisible:false — OpenMcdf reads via Stream.Read; the buffer is whole-sector regardless.
        return new MemoryStream(buffer, 0, buffer.Length, writable: false, publiclyVisible: false);
    }

    // Reads the sector size from the compound-file header (sector shift at byte offset 30), validating
    // the CFB magic first. Returns 0 when the stream is too short or not a compound file, in which case
    // the caller leaves the stream untouched and lets OpenMcdf surface the real error.
    private static int ReadSectorSize(Stream stream)
    {
        if (stream.Length < 512) return 0;

        Span<byte> header = stackalloc byte[MinHeaderBytes];
        long origin = stream.Position;
        stream.Position = 0;
        int read = stream.ReadAtLeast(header, MinHeaderBytes, throwOnEndOfStream: false);
        stream.Position = origin;
        if (read < MinHeaderBytes) return 0;

        // CFB magic: D0 CF 11 E0 A1 B1 1A E1
        if (header[0] != 0xD0 || header[1] != 0xCF || header[2] != 0x11 || header[3] != 0xE0 ||
            header[4] != 0xA1 || header[5] != 0xB1 || header[6] != 0x1A || header[7] != 0xE1)
            return 0;

        int shift = header[30] | (header[31] << 8); // V3 = 9 (512 bytes), V4 = 12 (4096 bytes)
        if (shift is < 7 or > 20) return 0;          // sanity: 128 B .. 1 MB sectors
        return 1 << shift;
    }

    /// <summary>
    /// Creates a new (in-memory) compound file for writing. Call <see cref="Save"/> to emit it.
    /// </summary>
    public static CompoundFileAccessor Create()
    {
        var root = OpenMcdf.RootStorage.CreateInMemory(OpenMcdf.Version.V3);
        return new CompoundFileAccessor(root);
    }

    /// <summary>
    /// Gets a storage (directory) by path.
    /// </summary>
    /// <param name="path">Path using / as separator.</param>
    public CompoundStorage? TryGetStorage(string path)
    {
        var (storage, stream) = TryNavigate(path);
        return stream is null ? storage : null;
    }

    /// <summary>
    /// Gets a stream by path.
    /// </summary>
    /// <param name="path">Path using / as separator.</param>
    public CompoundStream? TryGetStream(string path) => TryNavigate(path).Stream;

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
    public ReadOnlyMemory<byte> GetStreamDataAsMemory(string path) => GetStreamData(path);

    /// <summary>
    /// Enumerates all child items in a storage.
    /// </summary>
    public IEnumerable<CompoundEntry> EnumerateChildren(CompoundStorage storage)
        => storage.EnumerateEntries();

    /// <summary>
    /// Enumerates all child storages in a storage.
    /// </summary>
    public IEnumerable<CompoundStorage> EnumerateStorages(CompoundStorage storage)
        => storage.EnumerateStorages();

    /// <summary>
    /// Enumerates all child streams in a storage.
    /// </summary>
    public IEnumerable<CompoundStream> EnumerateStreams(CompoundStorage storage)
        => storage.EnumerateStreams();

    /// <summary>
    /// Saves the compound file to a new path.
    /// </summary>
    public async ValueTask SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        _root.Flush();
        var image = _root.BaseStream;
        image.Position = 0;

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await image.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves the compound file to a stream.
    /// </summary>
    public void Save(Stream stream)
    {
        _root.Flush();
        var image = _root.BaseStream;
        image.Position = 0;
        image.CopyTo(stream);
    }

    private (CompoundStorage? Storage, CompoundStream? Stream) TryNavigate(string path)
    {
        if (string.IsNullOrEmpty(path))
            return (RootStorage, null);

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = RootStorage;

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var isLast = i == parts.Length - 1;

            if (isLast && current.TryGetStream(part, out var stream))
                return (null, stream);

            if (!current.TryGetStorage(part, out var child))
                return (null, null);

            current = child;
        }

        return (current, null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _root.Dispose();

        if (_stream != null && !_leaveStreamOpen)
        {
            _stream.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _root.Dispose();

        if (_stream != null && !_leaveStreamOpen)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
