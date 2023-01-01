using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp
{
    /// <summary>
    /// Base class for implementing writers for COM/OLE Structured Storage based formats.
    /// </summary>
    public abstract class CompoundFileWriter<TData> : IDisposable
    {
        /// <summary>
        /// Instance of the compound storage file class.
        /// </summary>
        internal CompoundFile Cf { get; private set; }

        /// <summary>
        /// Data to be written to the file.
        /// </summary>
        protected TData Data { get; private set; }

        /// <summary>
        /// Creates a CompoundFileWriter instance for accessing the provided file name.
        /// </summary>
        public CompoundFileWriter()
        {

        }

        /// <summary>
        /// Framework hook method to be implemented by derived classes in order to perform the actual
        /// writing of the file contents.
        /// </summary>
        protected abstract void DoWrite(string fileName);

        /// <summary>
        /// Writes the content of the compound file.
        /// </summary>
        /// <param name="data">Data to be saved to file.</param>
        /// <param name="fileName">Name of the compound storage file to be written to.</param>
        public void Write(TData data, string fileName, bool overwrite = false)
        {
            Data = data;

            using (Cf = new CompoundFile())
            {
                DoWrite(fileName);

                using (var stream = new FileStream(fileName, overwrite ? FileMode.Create : FileMode.CreateNew))
                {
                    Cf.Save(stream);
                }
            }

            Cf = null;
        }

        public void WriteStream(TData data, Stream stream)
        {
            Data = data;

            using (Cf = new CompoundFile())
            {
                DoWrite("");

                Cf.Save(stream);
            }

            Cf = null;
        }

        /// <summary>
        /// Writes the header record containing the size of the data.
        /// </summary>
        /// <param name="storage">
        /// Storage key where to write for the Header stream.
        /// </param>
        /// <param name="recordCount">
        /// Size of the Data section.
        /// </param>
        internal static void WriteHeader(CFStorage storage, int recordCount)
        {
            storage.GetOrAddStream("Header").Write(writer => writer.Write(recordCount));
        }

        /// <summary>
        /// Writes a block of bytes that is prefixed with its size as an int32.
        /// </summary>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="data">Block data.</param>
        /// <param name="flags">Block flags.</param>
        /// <param name="emptySize">Size considered as an empty block that will not have content data written.</param>
        internal static void WriteBlock(BinaryWriter writer, byte[] data, byte flags = 0, int emptySize = 0)
        {
            writer.Write((flags << 24) | data.Length); // flags + size
            if (data.Length > emptySize)
            {
                writer.Write(data ?? Array.Empty<byte>()); // data
            }
        }

        /// <summary>
        /// Writes a block of bytes that is prefixed with its size as an int32.
        /// </summary>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="serializer">Serializer that will generate the block data.</param>
        /// <param name="flags">Block flags.</param>
        /// <param name="emptySize">Size considered as an empty block that will not have content data written.</param>
        internal static void WriteBlock(BinaryWriter writer, Action<BinaryWriter> serializer, byte flags = 0, int emptySize = 0)
        {
            var posStart = writer.BaseStream.Position;

            writer.Write(0); // dummy length header
            serializer?.Invoke(writer);

            var posEnd = writer.BaseStream.Position;
            writer.BaseStream.Position = posStart;
            
            int length = (int)(posEnd - posStart - sizeof(int));
            writer.Write(((flags << 24) | length)); // write length header

            if (length > emptySize)
            {
                writer.BaseStream.Position = posEnd;
            }
        }

        /// <summary>
        /// Compresses data into zlib format.
        /// </summary>
        /// <param name="data">Byte data to be compressed.</param>
        internal static byte[] CompressZlibData(byte[] data)
        {
            int Adler32(byte[] buf)
            {
                // Source: https://tools.ietf.org/html/rfc1950#page-6
                const int mod = 65521;
                int s1 = 1;
                int s2 = 0;
                foreach (var b in buf)
                {
                    s1 = (s1 + b) % mod;
                    s2 = (s2 + s1) % mod;
                }
                return IPAddress.HostToNetworkOrder((s2 << 16) + s1);
            }

            using (var compressedData = new MemoryStream())
            using (var decompressedData = new MemoryStream(data))
            using (var deflater = new DeflateStream(compressedData, CompressionMode.Compress, true))
            {
                // zlib 2 byte header: CMF + FLG
                compressedData.WriteByte(0x78); // CMF = deflate + 32k window
                compressedData.WriteByte(0x9c); // FLG = ? obtained from the reader

                decompressedData.CopyTo(deflater);
                deflater.Close(); // DeflateStream needs to be closed in order to write the compressed data

                // write adler32 checksum
                using (var binaryWriter = new BinaryWriter(compressedData))
                {
                    binaryWriter.Write(Adler32(data));
                }

                return compressedData.ToArray();
            }
        }

        /// <summary>
        /// Write compressed storage (id, data) pair, where data is compressed in zlib format.
        /// </summary>
        /// <param name="writer"> Binary data writer.</param>
        /// <param name="id">Identifier for the data to be stored.</param>
        /// <param name="data">Data to be stored.</param>
        internal static void WriteCompressedStorage(BinaryWriter writer, string id, byte[] data)
        {
            WriteCompressedStorage(writer, id, w => w.Write(data ?? Array.Empty<byte>()));
        }

        /// <summary>
        /// Write compressed storage (id, data) pair, where data is compressed in zlib format.
        /// </summary>
        /// <param name="writer"> Binary data writer.</param>
        /// <param name="id">Identifier for the data to be stored.</param>
        /// <param name="serializer">Serializer that will generate the data to be stored.</param>
        internal static void WriteCompressedStorage(BinaryWriter writer, string id, Action<BinaryWriter> serializer)
        {
            WriteBlock(writer, w =>
            {
                w.Write((byte)0xD0); // required 0xD0 tag 
                WritePascalShortString(w, id);

                using (var memoryStream = new MemoryStream())
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    serializer?.Invoke(binaryWriter);
                    var data = memoryStream.ToArray();
                    var zlibData = CompressZlibData(data); // Images and other Storage items are compressed with zlib format including a two byte header
                    WriteBlock(w, zlibData);
                }
            }, 0x01); // compressed blocks have a 0x01 flag for unknown reason
        }

        /// <summary>
        /// Serializes a raw string as an array of bytes.
        /// </summary>
        /// <param name="data">String to be converted to bytes.</param>
        /// <param name="encoding">
        /// Encoding to be used when serializing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding
        /// </param>
        /// <returns>Serialized bytes of the string according to the specified encoding.</returns>
        internal static byte[] SerializeRawString(string data, Encoding encoding = null)
        {
            data = data ?? string.Empty;
            encoding = encoding ?? Utils.Win1252Encoding;
            return encoding.GetBytes(data);
        }

        /// <summary>
        /// Writes to <paramref name="writer"/> a raw string, that is, a string <em>without</em>
        /// leading length and <em>without</em> a <c>0x00</c> terminator.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="data">String to be serialized.</param>
        /// <param name="encoding">
        /// Encoding to be used when serializing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding
        /// </param>
        internal static void WriteRawString(BinaryWriter writer, string data, Encoding encoding = null)
        {
            var rawData = SerializeRawString(data, encoding);
            writer.Write(rawData);
        }

        /// <summary>
        /// Writes to <paramref name="writer"/> a C-like string that is 0x00 terminated.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="data">String to be serialized.</param>
        /// <param name="encoding">
        /// Encoding to be used when serializing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding
        /// </param>
        internal static void WriteCString(BinaryWriter writer, string data, Encoding encoding = null)
        {
            WriteRawString(writer, data, encoding);
            writer.Write((byte)0x00); // NUL terminator
        }

        /// <summary>
        /// Writes a fixed length type of string, which is used for encoding font names.
        /// <para>
        /// The string has the following properties:
        /// <list type="bullet">
        /// <item><description>a sequence of UTF-16 code-points;</description></item>
        /// <item><description>a fixed length of 32 bytes;</description></item>
        /// <item><description>the actual string data ends with a U+0000 UTF-16 code-point.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// This is equivalent to a Pascal <c>WideString[32]</c>.
        /// </para>
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="data">String to be serialized.</param>
        internal static void WriteStringFontName(BinaryWriter writer, string data)
        {
            var rawData = SerializeRawString(data, Encoding.Unicode).Take(30).ToArray(); // Unicode means UTF-16
            writer.Write(rawData);
            
            // pad the remaning space with 0x00
            for (int i = rawData.Length; i < 32; ++i)
            {
                writer.Write((byte)0);
            }
        }

        /// <summary>
        /// Writes to <paramref name="writer"/> a Pascal-like string, that is, a string which is
        /// prefixed with its size as an int32.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="data">String to be serialized.</param>
        /// <param name="encoding">
        /// Encoding to be used when serializing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding
        /// </param>
        internal static void WritePascalString(BinaryWriter writer, string data, Encoding encoding = null)
        {
            WriteBlock(writer, w => WriteCString(w, data, encoding));
        }

        /// <summary>
        /// Writes to <paramref name="writer"/> a string which is prefixed with its size
        /// as a byte.
        /// <para>
        /// This is roughly equivalent to Pascal <c>ShortString</c> with the exception that
        /// we can support different encodings.
        /// </para>
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="data">String to be serialized.</param>
        /// <param name="encoding">
        /// Encoding to be used when serializing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding.
        /// </param>
        internal static void WritePascalShortString(BinaryWriter writer, string data, Encoding encoding = null)
        {
            data = data ?? string.Empty;
            encoding = encoding ?? Utils.Win1252Encoding;
            writer.Write((byte)encoding.GetByteCount(data)); // size
            WriteRawString(writer, data, encoding: encoding);
        }

        /// <summary>
        /// Writes to <paramref name="writer"/> a block which contains a single Pascal's ShortString-like string.
        /// <para>
        /// </para>
        /// <seealso cref="WriteBlock(BinaryWriter, Action{BinaryWriter})"/>
        /// <seealso cref="WritePascalShortString(BinaryWriter, string, Encoding)"/>
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="data">String to be serialized.</param>
        /// <param name="encoding">
        /// Encoding to be used when serializing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding.
        /// </param>
        internal static void WriteStringBlock(BinaryWriter writer, string data, Encoding encoding = null)
        {
            WriteBlock(writer, w => WritePascalShortString(w, data, encoding));
        }

        /// <summary>
        /// Writes to <paramref name="writer"/> a <see cref="ParameterCollection"/> as a C-like string
        /// encoded using Windows-1252.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="parameters">Parameters to be serialized.</param>
        /// <param name="raw">If false the parameters are 0x00 terminated.</param>
        /// <param name="encoding">
        /// Encoding to be used when serializing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding.
        /// </param>
        internal static void WriteParameters(BinaryWriter writer, ParameterCollection parameters, bool raw = false, Encoding encoding = null, bool outputUtfKeys = true)
        {
            var data = outputUtfKeys ? parameters.ToString() : parameters.ToUnicodeString();
            if (raw)
            {
                WriteRawString(writer, data, encoding);
            }
            else
            {
                WriteCString(writer, data, encoding);
            }
        }

        /// <summary>
        /// Writes a pair of (x, y) from a <see cref="CoordPoint"/>.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="data">Point to serialize.</param>
        internal static void WriteCoordPoint(BinaryWriter writer, CoordPoint data)
        {
            writer.Write(data.X);
            writer.Write(data.Y);
        }

        /// <summary>
        /// Writes a list of strings from the <paramref name="component"/> to the "WideStrings"
        /// stream inside of the specified <paramref name="storage"/> key.
        /// <para>
        /// Each string entry is encoded inside a parameter string value with a comma separated
        /// list of integers that represent UTF-16 code-points.
        /// </para>
        /// <para>
        /// These strings are used as Unicode variants of the texts existing in text string
        /// binary records.
        /// </para>
        /// </summary>
        /// <param name="storage">
        /// Storage key where to write to "WideStrings" stream.
        /// </param>
        /// <param name="component">
        /// Component to have its list of strings serialized.
        /// </param>
        internal static void WriteWideStrings(CFStorage storage, PcbComponent component)
        {
            var texts = component.Primitives.OfType<PcbText>().ToList();
            storage.GetOrAddStream("WideStrings").Write(writer =>
            {
                var parameters = new ParameterCollection();
                for (var i = 0; i < texts.Count; ++i)
                {
                    var text = texts[i];
                    text.WideStringsIndex = i;

                    var data = text.Text ?? "";
                    var codepoints = data.Select(c => Convert.ToInt32(c));
                    var intList = string.Join(",", codepoints);
                    parameters.Add($"ENCODEDTEXT{i}", intList);
                }
                WriteBlock(writer, w => WriteParameters(w, parameters));
            });
        }

        /// <summary>
        /// Get the name of the section storage key to be used to write a component "ref name".
        /// <para>
        /// This allows for giving an alias for accessing a component that has a name that is
        /// not supported by the compound file storage format because of its limitations.
        /// </para>
        /// </summary>
        /// <param name="refName">Component reference name.</param>
        /// <returns>
        /// The generated name for a storage section key where write the data for the given <paramref name="refName"/>.
        /// </returns>
        protected string GetSectionKeyFromComponentPattern(string refName)
        {
            return refName?.Substring(0, refName.Length > 31 ? 31 : refName.Length).Replace('/', '_');
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Cf?.Close();
                    Cf = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
