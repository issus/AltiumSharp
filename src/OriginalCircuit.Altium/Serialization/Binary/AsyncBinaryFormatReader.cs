using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace OriginalCircuit.Altium.Serialization.Binary;

/// <summary>
/// Asynchronous binary format reader using PipeReader for efficient streaming.
/// </summary>
/// <remarks>
/// This reader is optimized for async I/O scenarios and large files where
/// buffered streaming is more efficient than random access.
/// </remarks>
internal sealed class AsyncBinaryFormatReader : IAsyncDisposable
{
    private readonly PipeReader _pipeReader;
    private readonly bool _ownsReader;
    private long _consumed;
    private bool _disposed;

    /// <summary>
    /// Creates a reader from a PipeReader.
    /// </summary>
    public AsyncBinaryFormatReader(PipeReader reader, bool ownsReader = true)
    {
        _pipeReader = reader ?? throw new ArgumentNullException(nameof(reader));
        _ownsReader = ownsReader;
    }

    /// <summary>
    /// Creates a reader from a Stream.
    /// </summary>
    public static AsyncBinaryFormatReader FromStream(Stream stream, int minimumBufferSize = 4096)
    {
        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(
            bufferSize: minimumBufferSize,
            leaveOpen: false));
        return new AsyncBinaryFormatReader(reader, ownsReader: true);
    }

    /// <summary>
    /// Gets the total number of bytes consumed so far.
    /// </summary>
    public long BytesConsumed => _consumed;

    /// <summary>
    /// Reads exactly the specified number of bytes.
    /// </summary>
    public async ValueTask<ReadOnlyMemory<byte>> ReadExactAsync(int count, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var result = await _pipeReader.ReadAsync(cancellationToken);
            var buffer = result.Buffer;

            if (buffer.Length >= count)
            {
                var data = buffer.Slice(0, count);
                var array = data.ToArray();
                _pipeReader.AdvanceTo(data.End);
                _consumed += count;
                return array;
            }

            if (result.IsCompleted)
            {
                throw new EndOfStreamException($"Expected {count} bytes but stream ended after {buffer.Length}");
            }

            _pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    /// <summary>
    /// Reads a single byte.
    /// </summary>
    public async ValueTask<byte> ReadByteAsync(CancellationToken cancellationToken = default)
    {
        var data = await ReadExactAsync(1, cancellationToken);
        return data.Span[0];
    }

    /// <summary>
    /// Reads a signed 32-bit integer (little-endian).
    /// </summary>
    public async ValueTask<int> ReadInt32Async(CancellationToken cancellationToken = default)
    {
        var data = await ReadExactAsync(4, cancellationToken);
        var span = data.Span;
        return span[0] | (span[1] << 8) | (span[2] << 16) | (span[3] << 24);
    }

    /// <summary>
    /// Reads an unsigned 32-bit integer (little-endian).
    /// </summary>
    public async ValueTask<uint> ReadUInt32Async(CancellationToken cancellationToken = default)
    {
        var data = await ReadExactAsync(4, cancellationToken);
        var span = data.Span;
        return (uint)(span[0] | (span[1] << 8) | (span[2] << 16) | (span[3] << 24));
    }

    /// <summary>
    /// Reads a block prefixed with an Int32 size.
    /// </summary>
    public async ValueTask<ReadOnlyMemory<byte>> ReadBlockAsync(CancellationToken cancellationToken = default)
    {
        var size = await ReadInt32Async(cancellationToken);
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return ReadOnlyMemory<byte>.Empty;

        return await ReadExactAsync(sanitizedSize, cancellationToken);
    }

    /// <summary>
    /// Skips a block by reading its size and discarding the data.
    /// </summary>
    public async ValueTask<int> SkipBlockAsync(CancellationToken cancellationToken = default)
    {
        var size = await ReadInt32Async(cancellationToken);
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize > 0)
        {
            await SkipAsync(sanitizedSize, cancellationToken);
        }

        return sanitizedSize;
    }

    /// <summary>
    /// Skips the specified number of bytes.
    /// </summary>
    public async ValueTask SkipAsync(int count, CancellationToken cancellationToken = default)
    {
        var remaining = count;

        while (remaining > 0)
        {
            var result = await _pipeReader.ReadAsync(cancellationToken);
            var buffer = result.Buffer;

            var toConsume = (int)Math.Min(buffer.Length, remaining);
            if (toConsume > 0)
            {
                _pipeReader.AdvanceTo(buffer.GetPosition(toConsume));
                _consumed += toConsume;
                remaining -= toConsume;
            }

            if (result.IsCompleted && remaining > 0)
            {
                throw new EndOfStreamException($"Expected to skip {count} bytes but stream ended");
            }
        }
    }

    /// <summary>
    /// Reads a Pascal-style string (length-prefixed with a single byte).
    /// </summary>
    public async ValueTask<string> ReadPascalStringAsync(Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        encoding ??= AltiumEncoding.Windows1252;

        var length = await ReadByteAsync(cancellationToken);
        if (length == 0)
            return string.Empty;

        var data = await ReadExactAsync(length, cancellationToken);
        return encoding.GetString(data.Span);
    }

    /// <summary>
    /// Reads a string block (block with Int32 size, containing Pascal-style string).
    /// </summary>
    public async ValueTask<string> ReadStringBlockAsync(Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        var size = await ReadInt32Async(cancellationToken);
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return string.Empty;

        encoding ??= AltiumEncoding.Windows1252;

        // Read the Pascal string (1 byte length + chars)
        var length = await ReadByteAsync(cancellationToken);
        var consumed = 1;

        string result;
        if (length > 0)
        {
            var data = await ReadExactAsync(length, cancellationToken);
            result = encoding.GetString(data.Span);
            consumed += length;
        }
        else
        {
            result = string.Empty;
        }

        // Skip any remaining bytes in the block
        var remaining = sanitizedSize - consumed;
        if (remaining > 0)
        {
            await SkipAsync(remaining, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Reads a parameter block as a string.
    /// </summary>
    public ValueTask<string> ReadParameterBlockAsync(Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        return ReadStringBlockAsync(encoding, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_ownsReader)
        {
            await _pipeReader.CompleteAsync();
        }
    }
}
