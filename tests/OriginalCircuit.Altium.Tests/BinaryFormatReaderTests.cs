using OriginalCircuit.Altium.Serialization.Binary;

namespace OriginalCircuit.Altium.Tests;

public class BinaryFormatReaderTests
{
    [Fact]
    public void ReadInt32_LittleEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678 in little-endian
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        var result = reader.ReadInt32();

        Assert.Equal(0x12345678, result);
    }

    [Fact]
    public void ReadUInt32_LittleEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }; // Max uint
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        var result = reader.ReadUInt32();

        Assert.Equal(uint.MaxValue, result);
    }

    [Fact]
    public void ReadInt16_LittleEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0x34, 0x12 }; // 0x1234 in little-endian
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        var result = reader.ReadInt16();

        Assert.Equal(0x1234, result);
    }

    [Fact]
    public void ReadDouble_ReadsCorrectly()
    {
        var expected = 3.14159265359;
        using var memStream = new MemoryStream();
        using (var writer = new BinaryWriter(memStream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(expected);
        }
        memStream.Position = 0;

        using var reader = new BinaryFormatReader(memStream);
        var result = reader.ReadDouble();

        Assert.Equal(expected, result, precision: 10);
    }

    [Fact]
    public void ReadBlock_ReadsCorrectly()
    {
        // Block format: 4-byte size followed by data
        var blockData = System.Text.Encoding.ASCII.GetBytes("Hello");
        using var memStream = new MemoryStream();
        using (var writer = new BinaryWriter(memStream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(blockData.Length); // Size
            writer.Write(blockData);        // Data
        }
        memStream.Position = 0;

        using var reader = new BinaryFormatReader(memStream);
        using var block = reader.ReadBlock();

        Assert.Equal(5, block.Length);
        Assert.Equal("Hello", System.Text.Encoding.ASCII.GetString(block.Span));
    }

    [Fact]
    public void ReadBlock_WithFlagInSize_MasksCorrectly()
    {
        // Size with flag in high byte: 0xFF000005 (size = 5 with flag)
        var data = new byte[]
        {
            0x05, 0x00, 0x00, 0xFF,  // Size with flag
            0x01, 0x02, 0x03, 0x04, 0x05  // Data
        };
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        using var block = reader.ReadBlock();

        Assert.Equal(5, block.Length);
    }

    [Fact]
    public void SkipBlock_AdvancesPosition()
    {
        var blockData = new byte[] { 1, 2, 3, 4, 5 };
        using var memStream = new MemoryStream();
        using (var writer = new BinaryWriter(memStream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(blockData.Length);
            writer.Write(blockData);
            writer.Write((int)42); // Value after the block
        }
        memStream.Position = 0;

        using var reader = new BinaryFormatReader(memStream);
        var skippedSize = reader.SkipBlock();
        var valueAfter = reader.ReadInt32();

        Assert.Equal(5, skippedSize);
        Assert.Equal(42, valueAfter);
    }

    [Fact]
    public void ReadPascalString_ReadsCorrectly()
    {
        // Pascal string: 1-byte length followed by chars
        var testString = "Hello";
        using var memStream = new MemoryStream();
        memStream.WriteByte((byte)testString.Length);
        memStream.Write(System.Text.Encoding.ASCII.GetBytes(testString));
        memStream.Position = 0;

        using var reader = new BinaryFormatReader(memStream);
        var result = reader.ReadPascalString(System.Text.Encoding.ASCII);

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ReadPascalString_EmptyString_ReturnsEmpty()
    {
        using var memStream = new MemoryStream(new byte[] { 0x00 }); // Length = 0
        using var reader = new BinaryFormatReader(memStream);

        var result = reader.ReadPascalString();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ReadStringBlock_ReadsCorrectly()
    {
        // String block: Int32 size, then Pascal string inside
        var testString = "Test";
        using var memStream = new MemoryStream();
        using (var writer = new BinaryWriter(memStream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            var blockSize = 1 + testString.Length; // Pascal length byte + string bytes
            writer.Write(blockSize);
            writer.Write((byte)testString.Length);
            writer.Write(System.Text.Encoding.ASCII.GetBytes(testString));
        }
        memStream.Position = 0;

        using var reader = new BinaryFormatReader(memStream);
        var result = reader.ReadStringBlock(System.Text.Encoding.ASCII);

        Assert.Equal("Test", result);
    }

    [Fact]
    public void HasMore_ReturnsCorrectValue()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        Assert.True(reader.HasMore);
        reader.ReadInt32();
        Assert.False(reader.HasMore);
    }

    [Fact]
    public void Position_TracksCorrectly()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        Assert.Equal(0, reader.Position);
        reader.ReadInt32();
        Assert.Equal(4, reader.Position);
        reader.ReadInt16();
        Assert.Equal(6, reader.Position);
    }

    [Fact]
    public void Skip_AdvancesPosition()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        reader.Skip(4);

        Assert.Equal(4, reader.Position);
        Assert.Equal(5, reader.ReadByte());
    }

    [Fact]
    public void Seek_MovesToPosition()
    {
        var data = new byte[] { 0, 0, 0, 0, 42, 0, 0, 0 };
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        reader.Seek(4);
        var value = reader.ReadInt32();

        Assert.Equal(42, value);
    }

    [Fact]
    public void RentedBuffer_DisposesCorrectly()
    {
        var blockData = new byte[] { 1, 2, 3 };
        using var memStream = new MemoryStream();
        using (var writer = new BinaryWriter(memStream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(blockData.Length);
            writer.Write(blockData);
        }
        memStream.Position = 0;

        using var reader = new BinaryFormatReader(memStream);

        // Block should be usable within using scope
        using (var block = reader.ReadBlock())
        {
            Assert.Equal(3, block.Length);
            Assert.False(block.IsEmpty);
            Assert.Equal(1, block.Span[0]);
            Assert.Equal(2, block.Span[1]);
            Assert.Equal(3, block.Span[2]);
        }
        // After dispose, buffer is returned to pool (no exception)
    }

    [Fact]
    public void ReadExact_ThrowsOnEndOfStream()
    {
        var data = new byte[] { 1, 2 };
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        var buffer = new byte[10];
        Assert.Throws<EndOfStreamException>(() => reader.ReadExact(buffer));
    }

    // --- Round-trip tests (Writer → Reader) ---

    [Fact]
    public void RoundTrip_Int32_PreservesValue()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.Write(42);
            writer.Write(-1);
            writer.Write(int.MaxValue);
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        Assert.Equal(42, reader.ReadInt32());
        Assert.Equal(-1, reader.ReadInt32());
        Assert.Equal(int.MaxValue, reader.ReadInt32());
    }

    [Fact]
    public void RoundTrip_Double_PreservesValue()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.Write(3.14159);
            writer.Write(-1.5e10);
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        Assert.Equal(3.14159, reader.ReadDouble(), precision: 10);
        Assert.Equal(-1.5e10, reader.ReadDouble(), precision: 0);
    }

    [Fact]
    public void RoundTrip_Block_PreservesData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteBlock(data);
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        using var block = reader.ReadBlock();
        Assert.Equal(data, block.Span.ToArray());
    }

    [Fact]
    public void RoundTrip_PascalShortString_PreservesValue()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WritePascalShortString("Hello World");
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        var result = reader.ReadPascalString();
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void RoundTrip_StringBlock_PreservesValue()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteStringBlock("Test String");
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        var result = reader.ReadStringBlock();
        Assert.Equal("Test String", result);
    }

    [Fact]
    public void RoundTrip_FontName_PreservesValue()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteFontName("Arial");
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        var result = reader.ReadFontName();
        Assert.Equal("Arial", result);
    }

    [Fact]
    public void RoundTrip_FontName_LongName_Truncates()
    {
        var longName = new string('A', 50); // Longer than 31 chars
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteFontName(longName);
            writer.Flush();
        }
        Assert.Equal(64, ms.Length); // Always 64 bytes

        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        var result = reader.ReadFontName();
        Assert.Equal(31, result.Length); // Truncated to 31 chars (62 bytes / 2)
    }

    [Fact]
    public void RoundTrip_ParameterBlock_PreservesParameters()
    {
        var parameters = new Dictionary<string, string>
        {
            ["PATTERN"] = "RESISTOR_0402",
            ["HEIGHT"] = "50000"
        };

        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteParameterBlock(parameters);
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        var paramBlock = reader.ReadParameterBlock();

        Assert.Contains("PATTERN=RESISTOR_0402", paramBlock);
        Assert.Contains("HEIGHT=50000", paramBlock);
    }

    [Fact]
    public void RoundTrip_Coord_PreservesValue()
    {
        var coord = OriginalCircuit.Altium.Primitives.Coord.FromMils(100);
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteCoord(coord);
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        var rawValue = reader.ReadInt32();
        var result = OriginalCircuit.Altium.Primitives.Coord.FromRaw(rawValue);
        Assert.Equal(coord, result);
    }

    [Fact]
    public void RoundTrip_WriteFill_WritesCorrectBytes()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteFill(0xFF, 10);
            writer.Flush();
        }
        Assert.Equal(10, ms.Length);
        Assert.All(ms.ToArray(), b => Assert.Equal(0xFF, b));
    }

    [Fact]
    public void RoundTrip_WriteFill_LargeCount_Works()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteFill(0xAB, 100);
            writer.Flush();
        }
        Assert.Equal(100, ms.Length);
        Assert.All(ms.ToArray(), b => Assert.Equal(0xAB, b));
    }

    [Fact]
    public void RoundTrip_WriteFill_ZeroCount_WritesNothing()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteFill(0xFF, 0);
            writer.Flush();
        }
        Assert.Equal(0, ms.Length);
    }

    [Fact]
    public void ReadFontName_ReadsFixed64Bytes()
    {
        // Write "Arial" as UTF-16 followed by zeros to fill 64 bytes
        var data = new byte[64];
        var nameBytes = System.Text.Encoding.Unicode.GetBytes("Arial");
        nameBytes.CopyTo(data, 0);
        using var stream = new MemoryStream(data);
        using var reader = new BinaryFormatReader(stream);

        var result = reader.ReadFontName();

        Assert.Equal("Arial", result);
        Assert.Equal(64, stream.Position); // Always consumes exactly 64 bytes
    }

    [Fact]
    public void RoundTrip_EmptyBlock_PreservesEmpty()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteBlock(Array.Empty<byte>());
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        using var block = reader.ReadBlock();
        Assert.Equal(0, block.Length);
        Assert.True(block.IsEmpty);
    }

    [Fact]
    public void RoundTrip_BlockWithAction_PreservesData()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryFormatWriter(ms, leaveOpen: true))
        {
            writer.WriteBlock(w =>
            {
                w.Write(42);
                w.Write(3.14);
            });
            writer.Flush();
        }
        ms.Position = 0;
        using var reader = new BinaryFormatReader(ms);
        using var block = reader.ReadBlock();
        Assert.Equal(12, block.Length); // 4 (int) + 8 (double)
    }
}
