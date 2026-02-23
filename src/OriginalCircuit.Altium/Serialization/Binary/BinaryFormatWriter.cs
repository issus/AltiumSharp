using OriginalCircuit.Altium.Primitives;
using System.Buffers;
using System.Text;

namespace OriginalCircuit.Altium.Serialization.Binary;

/// <summary>
/// High-performance binary format writer for Altium file formats.
/// </summary>
internal sealed class BinaryFormatWriter : IDisposable
{
    private readonly BinaryWriter _writer;
    private readonly bool _leaveOpen;
    private bool _disposed;

    /// <summary>
    /// Creates a new binary format writer.
    /// </summary>
    public BinaryFormatWriter(Stream stream, bool leaveOpen = false)
    {
        _writer = new BinaryWriter(stream, AltiumEncoding.Windows1252, leaveOpen);
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Gets the current position in the stream.
    /// </summary>
    public long Position => _writer.BaseStream.Position;

    /// <summary>
    /// Gets the underlying stream.
    /// </summary>
    public Stream BaseStream => _writer.BaseStream;

    /// <summary>
    /// Writes a byte.
    /// </summary>
    public void Write(byte value) => _writer.Write(value);

    /// <summary>
    /// Writes a signed 16-bit integer.
    /// </summary>
    public void Write(short value) => _writer.Write(value);

    /// <summary>
    /// Writes an unsigned 16-bit integer.
    /// </summary>
    public void Write(ushort value) => _writer.Write(value);

    /// <summary>
    /// Writes a signed 32-bit integer.
    /// </summary>
    public void Write(int value) => _writer.Write(value);

    /// <summary>
    /// Writes an unsigned 32-bit integer.
    /// </summary>
    public void Write(uint value) => _writer.Write(value);

    /// <summary>
    /// Writes a 64-bit floating point number.
    /// </summary>
    public void Write(double value) => _writer.Write(value);

    /// <summary>
    /// Writes a boolean as a single byte.
    /// </summary>
    public void Write(bool value) => _writer.Write(value);

    /// <summary>
    /// Writes a byte array.
    /// </summary>
    public void Write(byte[] data) => _writer.Write(data);

    /// <summary>
    /// Writes a span of bytes.
    /// </summary>
    public void Write(ReadOnlySpan<byte> data)
    {
        _writer.Write(data);
    }

    /// <summary>
    /// Writes a coordinate value.
    /// </summary>
    public void WriteCoord(Coord coord) => _writer.Write(coord.ToRaw());

    /// <summary>
    /// Writes a coordinate point.
    /// </summary>
    public void WriteCoordPoint(CoordPoint point)
    {
        WriteCoord(point.X);
        WriteCoord(point.Y);
    }

    /// <summary>
    /// Writes a block with size prefix.
    /// </summary>
    public void WriteBlock(byte[] data, byte flags = 0)
    {
        var size = (flags << 24) | data.Length;
        _writer.Write(size);
        if (data.Length > 0)
            _writer.Write(data);
    }

    /// <summary>
    /// Writes a block with size prefix using an action.
    /// </summary>
    public void WriteBlock(Action<BinaryFormatWriter> serializer, byte flags = 0)
    {
        var startPos = _writer.BaseStream.Position;

        _writer.Write(0); // placeholder for size
        serializer?.Invoke(this);

        var endPos = _writer.BaseStream.Position;
        _writer.BaseStream.Position = startPos;

        var length = (int)(endPos - startPos - sizeof(int));
        var size = (flags << 24) | length;
        _writer.Write(size);

        _writer.BaseStream.Position = endPos;
    }

    /// <summary>
    /// Writes a Pascal-style short string (length byte prefix).
    /// </summary>
    public void WritePascalShortString(string data)
    {
        data ??= string.Empty;
        var bytes = AltiumEncoding.Windows1252.GetBytes(data);
        _writer.Write((byte)Math.Min(bytes.Length, 255));
        _writer.Write(bytes);
    }

    /// <summary>
    /// Writes a string block (size prefix + length byte + string).
    /// </summary>
    public void WriteStringBlock(string data)
    {
        WriteBlock(w => w.WritePascalShortString(data));
    }

    /// <summary>
    /// Writes a Pascal string (size prefix + length byte + string + null terminator).
    /// </summary>
    public void WritePascalString(string data)
    {
        WriteBlock(w =>
        {
            w.WritePascalShortString(data);
            w.Write((byte)0); // null terminator (included in the short string write)
        });
    }

    /// <summary>
    /// Writes a null-terminated C string.
    /// </summary>
    public void WriteCString(string data)
    {
        data ??= string.Empty;
        var bytes = AltiumEncoding.Windows1252.GetBytes(data);
        _writer.Write(bytes);
        _writer.Write((byte)0);
    }

    /// <summary>
    /// Writes a parameter collection as a block.
    /// </summary>
    public void WriteParameterBlock(Dictionary<string, string> parameters)
    {
        WriteBlock(w =>
        {
            var paramString = ParametersToString(parameters);
            w.WritePascalShortString(paramString);
        });
    }

    /// <summary>
    /// Writes a parameter block as a C-string (size prefix + null-terminated string).
    /// Used by PCB binary format where parameters are C-strings, not Pascal strings.
    /// </summary>
    public void WriteCStringParameterBlock(Dictionary<string, string> parameters)
    {
        WriteBlock(w =>
        {
            var paramString = ParametersToString(parameters);
            w.WriteCString(paramString);
        });
    }

    /// <summary>
    /// Writes a raw parameter string as a C-string block (size prefix + null-terminated string).
    /// Preserves the original key ordering.
    /// </summary>
    public void WriteCStringParameterBlockRaw(string paramString)
    {
        WriteBlock(w =>
        {
            w.WriteCString(paramString);
        });
    }

    /// <summary>
    /// Writes parameters as a C string.
    /// </summary>
    public void WriteParameters(Dictionary<string, string> parameters)
    {
        var paramString = ParametersToString(parameters);
        WriteCString(paramString);
    }

    /// <summary>
    /// Writes a parameter collection as a Unicode block (for schematic extended data).
    /// </summary>
    public void WriteUnicodeParameterBlock(Dictionary<string, string> parameters)
    {
        WriteBlock(w =>
        {
            var paramString = ParametersToString(parameters);
            var bytes = Encoding.Unicode.GetBytes(paramString);
            w.Write((byte)Math.Min(bytes.Length / 2, 255)); // Length in characters
            w.Write(bytes);
        });
    }

    /// <summary>
    /// Writes a fixed-length font name (64 bytes = 32 UTF-16 characters, null-terminated).
    /// </summary>
    public void WriteFontName(string fontName)
    {
        fontName ??= string.Empty;
        var bytes = Encoding.Unicode.GetBytes(fontName);
        var len = Math.Min(bytes.Length, 62); // Max 31 chars to leave room for null terminator
        _writer.Write(bytes.AsSpan(0, len));

        // Pad to 64 bytes with zeros
        Span<byte> padding = stackalloc byte[64 - len];
        padding.Clear();
        _writer.Write(padding);
    }

    /// <summary>
    /// Writes n bytes with the specified value.
    /// </summary>
    public void WriteFill(byte value, int count)
    {
        if (count <= 0) return;

        if (count <= 64)
        {
            Span<byte> buffer = stackalloc byte[count];
            buffer.Fill(value);
            _writer.Write(buffer);
        }
        else
        {
            var buffer = new byte[count];
            Array.Fill(buffer, value);
            _writer.Write(buffer);
        }
    }

    private static string ParametersToString(Dictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var kvp in parameters)
        {
            sb.Append('|');
            sb.Append(kvp.Key);
            sb.Append('=');
            sb.Append(kvp.Value ?? string.Empty);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Flushes the underlying stream.
    /// </summary>
    public void Flush() => _writer.Flush();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _writer.Flush();
        if (!_leaveOpen)
            _writer.Dispose();
    }
}
