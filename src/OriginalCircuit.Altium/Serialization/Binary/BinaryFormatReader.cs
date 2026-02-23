using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace OriginalCircuit.Altium.Serialization.Binary;

/// <summary>
/// High-performance binary format reader with Span support and buffer pooling.
/// </summary>
/// <remarks>
/// This reader is designed for zero-allocation parsing where possible,
/// using ArrayPool for any necessary buffer allocations.
/// </remarks>
internal sealed class BinaryFormatReader : IDisposable
{
    /// <summary>
    /// Mask to extract the actual size from a block header (high byte contains flags).
    /// </summary>
    private const int BlockSizeMask = 0x00FFFFFF;

    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly ArrayPool<byte> _pool;
    private byte[]? _rentedBuffer;
    private bool _disposed;

    // Small buffer for reading primitive values
    private readonly byte[] _primitiveBuffer = new byte[8];

    /// <summary>
    /// Gets the current position in the stream.
    /// </summary>
    public long Position => _stream.Position;

    /// <summary>
    /// Gets the length of the stream.
    /// </summary>
    public long Length => _stream.Length;

    /// <summary>
    /// Gets whether there is more data to read.
    /// </summary>
    public bool HasMore => Position < Length;

    /// <summary>
    /// Creates a new reader for the given stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open when disposed.</param>
    /// <param name="pool">Optional custom array pool.</param>
    public BinaryFormatReader(Stream stream, bool leaveOpen = false, ArrayPool<byte>? pool = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _leaveOpen = leaveOpen;
        _pool = pool ?? ArrayPool<byte>.Shared;
    }

    /// <summary>
    /// Reads a single byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        var result = _stream.ReadByte();
        if (result < 0)
            throw new EndOfStreamException();
        return (byte)result;
    }

    /// <summary>
    /// Reads a signed 16-bit integer (little-endian).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16()
    {
        FillBuffer(2);
        return (short)(_primitiveBuffer[0] | (_primitiveBuffer[1] << 8));
    }

    /// <summary>
    /// Reads an unsigned 16-bit integer (little-endian).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        FillBuffer(2);
        return (ushort)(_primitiveBuffer[0] | (_primitiveBuffer[1] << 8));
    }

    /// <summary>
    /// Reads a signed 32-bit integer (little-endian).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        FillBuffer(4);
        return _primitiveBuffer[0] |
               (_primitiveBuffer[1] << 8) |
               (_primitiveBuffer[2] << 16) |
               (_primitiveBuffer[3] << 24);
    }

    /// <summary>
    /// Reads an unsigned 32-bit integer (little-endian).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        FillBuffer(4);
        return (uint)(_primitiveBuffer[0] |
                      (_primitiveBuffer[1] << 8) |
                      (_primitiveBuffer[2] << 16) |
                      (_primitiveBuffer[3] << 24));
    }

    /// <summary>
    /// Reads a signed 64-bit integer (little-endian).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        FillBuffer(8);
        var lo = (uint)(_primitiveBuffer[0] |
                        (_primitiveBuffer[1] << 8) |
                        (_primitiveBuffer[2] << 16) |
                        (_primitiveBuffer[3] << 24));
        var hi = (uint)(_primitiveBuffer[4] |
                        (_primitiveBuffer[5] << 8) |
                        (_primitiveBuffer[6] << 16) |
                        (_primitiveBuffer[7] << 24));
        return (long)((ulong)hi << 32 | lo);
    }

    /// <summary>
    /// Reads a single-precision floating-point number.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle()
    {
        FillBuffer(4);
        return BitConverter.ToSingle(_primitiveBuffer, 0);
    }

    /// <summary>
    /// Reads a double-precision floating-point number.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble()
    {
        FillBuffer(8);
        return BitConverter.ToDouble(_primitiveBuffer, 0);
    }

    /// <summary>
    /// Reads a block prefixed with an Int32 size.
    /// </summary>
    /// <returns>A rented buffer containing the block data. Caller must return to pool or use ReadBlockInto.</returns>
    public RentedBuffer ReadBlock()
    {
        var size = ReadInt32();
        var sanitizedSize = size & BlockSizeMask; // Mask out flags in high byte

        if (sanitizedSize <= 0)
            return new RentedBuffer(Array.Empty<byte>(), 0, null);

        var buffer = _pool.Rent(sanitizedSize);
        var bytesRead = _stream.Read(buffer, 0, sanitizedSize);

        if (bytesRead < sanitizedSize)
            throw new EndOfStreamException($"Expected {sanitizedSize} bytes, but only {bytesRead} available");

        return new RentedBuffer(buffer, sanitizedSize, _pool);
    }

    /// <summary>
    /// Reads a block into the provided span.
    /// </summary>
    /// <param name="destination">Destination buffer.</param>
    /// <returns>The actual number of bytes read (the block size).</returns>
    public int ReadBlockInto(Span<byte> destination)
    {
        var size = ReadInt32();
        var sanitizedSize = size & BlockSizeMask;

        if (sanitizedSize <= 0)
            return 0;

        if (sanitizedSize > destination.Length)
            throw new ArgumentException($"Destination buffer too small: need {sanitizedSize}, have {destination.Length}");

        var bytesRead = _stream.Read(destination.Slice(0, sanitizedSize));

        if (bytesRead < sanitizedSize)
            throw new EndOfStreamException($"Expected {sanitizedSize} bytes, but only {bytesRead} available");

        return sanitizedSize;
    }

    /// <summary>
    /// Skips a block by reading its size and advancing past the data.
    /// </summary>
    /// <returns>The size of the skipped block.</returns>
    public int SkipBlock()
    {
        var size = ReadInt32();
        var sanitizedSize = size & BlockSizeMask;

        if (sanitizedSize > 0)
        {
            if (_stream.CanSeek)
            {
                _stream.Position += sanitizedSize;
            }
            else
            {
                // Non-seekable stream - must read through
                var buffer = _pool.Rent(Math.Min(sanitizedSize, 4096));
                try
                {
                    var remaining = sanitizedSize;
                    while (remaining > 0)
                    {
                        var toRead = Math.Min(remaining, buffer.Length);
                        var read = _stream.Read(buffer, 0, toRead);
                        if (read == 0)
                            throw new EndOfStreamException();
                        remaining -= read;
                    }
                }
                finally
                {
                    _pool.Return(buffer);
                }
            }
        }

        return sanitizedSize;
    }

    /// <summary>
    /// Reads a Pascal-style string (length-prefixed with a single byte).
    /// </summary>
    /// <param name="encoding">Optional encoding (defaults to Windows-1252).</param>
    public string ReadPascalString(Encoding? encoding = null)
    {
        encoding ??= AltiumEncoding.Windows1252;

        var length = ReadByte();
        if (length == 0)
            return string.Empty;

        Span<byte> buffer = stackalloc byte[length];
        var bytesRead = _stream.Read(buffer);

        if (bytesRead < length)
            throw new EndOfStreamException();

        return encoding.GetString(buffer);
    }

    /// <summary>
    /// Reads a Pascal-style short string (length-prefixed with Int32, but only up to 255 chars used).
    /// </summary>
    /// <param name="encoding">Optional encoding (defaults to Windows-1252).</param>
    public string ReadPascalShortString(Encoding? encoding = null)
    {
        encoding ??= AltiumEncoding.Windows1252;

        var length = ReadByte();
        if (length == 0)
            return string.Empty;

        Span<byte> buffer = stackalloc byte[length];
        var bytesRead = _stream.Read(buffer);

        if (bytesRead < length)
            throw new EndOfStreamException();

        return encoding.GetString(buffer);
    }

    /// <summary>
    /// Reads a string block (block with Int32 size, containing Pascal-style string).
    /// </summary>
    /// <param name="encoding">Optional encoding (defaults to Windows-1252).</param>
    public string ReadStringBlock(Encoding? encoding = null)
    {
        var size = ReadInt32();
        var sanitizedSize = size & BlockSizeMask;

        if (sanitizedSize <= 0)
            return string.Empty;

        var startPosition = _stream.Position;
        var result = ReadPascalShortString(encoding);

        // Ensure we consume exactly the block size
        var consumed = _stream.Position - startPosition;
        if (consumed < sanitizedSize)
        {
            if (_stream.CanSeek)
            {
                _stream.Position = startPosition + sanitizedSize;
            }
            else
            {
                var remaining = (int)(sanitizedSize - consumed);
                Span<byte> skip = stackalloc byte[Math.Min(remaining, 256)];
                while (remaining > 0)
                {
                    var toRead = Math.Min(remaining, skip.Length);
                    var read = _stream.Read(skip.Slice(0, toRead));
                    if (read == 0) break;
                    remaining -= read;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Reads a parameter block as a string for parsing.
    /// </summary>
    public string ReadParameterBlock(Encoding? encoding = null)
    {
        return ReadStringBlock(encoding);
    }

    /// <summary>
    /// Reads a fixed-length font name (64 bytes = 32 UTF-16 chars, null-terminated).
    /// </summary>
    public string ReadFontName()
    {
        Span<byte> buffer = stackalloc byte[64];
        ReadExact(buffer);

        // Find null terminator
        var charCount = 0;
        for (var i = 0; i < 64; i += 2)
        {
            if (buffer[i] == 0 && buffer[i + 1] == 0)
                break;
            charCount++;
        }

        return charCount == 0
            ? string.Empty
            : Encoding.Unicode.GetString(buffer.Slice(0, charCount * 2));
    }

    /// <summary>
    /// Reads bytes into the provided span.
    /// </summary>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Number of bytes read.</returns>
    public int Read(Span<byte> destination)
    {
        return _stream.Read(destination);
    }

    /// <summary>
    /// Reads exactly the specified number of bytes.
    /// </summary>
    /// <param name="destination">The destination buffer.</param>
    /// <exception cref="EndOfStreamException">If not enough bytes are available.</exception>
    public void ReadExact(Span<byte> destination)
    {
        var totalRead = 0;
        while (totalRead < destination.Length)
        {
            var read = _stream.Read(destination.Slice(totalRead));
            if (read == 0)
                throw new EndOfStreamException();
            totalRead += read;
        }
    }

    /// <summary>
    /// Seeks to a position in the stream.
    /// </summary>
    public void Seek(long position)
    {
        _stream.Position = position;
    }

    /// <summary>
    /// Skips the specified number of bytes.
    /// </summary>
    public void Skip(int count)
    {
        if (_stream.CanSeek)
        {
            _stream.Position += count;
        }
        else
        {
            Span<byte> buffer = stackalloc byte[Math.Min(count, 256)];
            var remaining = count;
            while (remaining > 0)
            {
                var toRead = Math.Min(remaining, buffer.Length);
                var read = _stream.Read(buffer.Slice(0, toRead));
                if (read == 0)
                    throw new EndOfStreamException();
                remaining -= read;
            }
        }
    }

    /// <summary>
    /// Captures raw bytes from a previously read range by seeking back.
    /// The stream position must be at or past startPos + length.
    /// After capture, the stream position is restored to where it was.
    /// </summary>
    public byte[] CaptureRawBytes(long startPos, int length)
    {
        var savedPos = _stream.Position;
        _stream.Position = startPos;
        var data = new byte[length];
        ReadExact(data);
        _stream.Position = savedPos;
        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FillBuffer(int count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = _stream.Read(_primitiveBuffer, totalRead, count - totalRead);
            if (read == 0)
                throw new EndOfStreamException();
            totalRead += read;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_rentedBuffer != null)
        {
            _pool.Return(_rentedBuffer);
            _rentedBuffer = null;
        }

        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}

/// <summary>
/// Represents a rented buffer from ArrayPool that must be returned.
/// </summary>
public readonly struct RentedBuffer : IDisposable
{
    private readonly byte[] _buffer;
    private readonly int _length;
    private readonly ArrayPool<byte>? _pool;

    internal RentedBuffer(byte[] buffer, int length, ArrayPool<byte>? pool)
    {
        _buffer = buffer;
        _length = length;
        _pool = pool;
    }

    /// <summary>
    /// Gets the data as a span.
    /// </summary>
    public ReadOnlySpan<byte> Span => _buffer.AsSpan(0, _length);

    /// <summary>
    /// Gets the length of the data.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Gets whether this buffer is empty.
    /// </summary>
    public bool IsEmpty => _length == 0;

    /// <summary>
    /// Returns the buffer to the pool.
    /// </summary>
    public void Dispose()
    {
        if (_pool != null && _buffer.Length > 0)
        {
            _pool.Return(_buffer);
        }
    }
}
