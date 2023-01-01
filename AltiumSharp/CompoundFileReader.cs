using OpenMcdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp
{
    /// <summary>
    /// Base class for implementing parsers that read COM/OLE Structured Storage based formats.
    /// </summary>
    public abstract class CompoundFileReader<TData> : IDisposable
        where TData : new()
    {
        /// <summary>
        /// Debug helper information for keeping track of the current operation context
        /// when generating warnings and errors.
        /// </summary>
        private List<string> _context;

        /// <summary>
        /// Mapping of "ref lib" component names to sections in the structured storage file.
        /// </summary>
        protected Dictionary<string, string> SectionKeys { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Instance of the compound storage file reader class.
        /// </summary>
        internal CompoundFile Cf { get; private set; }

        /// <summary>
        /// Data read from the file.
        /// </summary>
        protected TData Data { get; private set; }

        /// <summary>
        /// List of warnings produced during the reading of the current file.
        /// </summary>
        public List<string> Warnings { get; }

        /// <summary>
        /// List of errors encountered during the reading of the current file.
        /// </summary>
        public List<string> Errors { get; }

        public string Context => string.Join(":", _context);

        /// <summary>
        /// Creates a CompoundFileReader instance for accessing the provided file name.
        /// </summary>
        /// <param name="fileName">Name of the compound storage file to be processed.</param>
        public CompoundFileReader()
        {
            _context = new List<string>();
            Warnings = new List<string>();
            Errors = new List<string>();
        }

        /// <summary>
        /// Framework hook method to be implemented by derived classes in order to perform the actual
        /// reading of the file contents.
        /// </summary>
        protected abstract void DoRead();

        /// <summary>
        /// Clears the previously read data, including warnings and errors, reseting the reader state.
        /// </summary>
        private void Clear()
        {
            Data = new TData();
            Warnings.Clear();
            Errors.Clear();
        }

        /// <summary>
        /// Reads the content of the compound file.
        /// </summary>
        /// <param name="fileName">Name of the compound storage file to be read.</param>
        public TData Read(string fileName)
        {
            Clear();

            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Cf = new CompoundFile(stream))
            {
                DoRead();
            }

            Cf = null;

            return Data;
        }

        /// <summary>
        /// Reads the content of the compound file.
        /// </summary>
        /// <param name="stream">Stream of the compound storage file to be read.</param>
        public TData Read(Stream stream)
        {
            Clear();

            using (Cf = new CompoundFile(stream))
            {
                DoRead();
            }

            Cf = null;

            return Data;
        }

        /// <summary>
        /// Get the name of the section storage key to be used to read a component "ref name".
        /// <para>
        /// This allows for giving an alias for reading a component that has a name that is
        /// not supported by the compound file storage format because of its limitations.
        /// </para>
        /// <para>
        /// In case the <see cref="DoReadSectionKeys(Dictionary{string, string})"/> didn't
        /// properly provide a relevant list of "ref name" to section keys, we attempt the
        /// to guess the possible way the component name can be mangled to fit the compound
        /// storage format naming limitations.
        /// </para>
        /// </summary>
        /// <param name="refName">Component reference name.</param>
        /// <returns>
        /// A storage section key where to access the data for the given <paramref name="refName"/>.
        /// </returns>
        protected string GetSectionKeyFromRefName(string refName)
        {
            if (SectionKeys.TryGetValue(refName, out var result))
            {
                return result;
            }
            else
            {
                return refName?.Substring(0, refName.Length > 31 ? 31 : refName.Length).Replace('/', '_');
            }
        }

        /// <summary>
        /// Debug helper to mark the start of an operation context, in order to keep track
        /// of where warnings and errors are encountered while reading the file.
        /// <para>
        /// This must be matched by a corresponding call to <see cref="EndContext"/>.
        /// </para>
        /// </summary>
        /// <param name="context">Description of the current context.
        /// <para>
        /// This can be the name of the component being currently read, name of the section
        /// being parsed, index of the current record, post or preprocessing stage, etc.,
        /// that is, anything that helps contextualize the current operation.
        /// </para>
        /// </param>
        protected void BeginContext(string context)
        {
            _context.Add(context);
        }

        /// <summary>
        /// Debug helper to mark the end of an operation context.
        /// <para>
        /// This must be matched by a corresponding call to <see cref="BeginContext"/>.
        /// </para>
        /// </summary>
        protected void EndContext()
        {
            _context.RemoveAt(_context.Count - 1);
        }

        /// <summary>
        /// Generates an error message and logs it to the <see cref="Errors"/> list.
        /// <para>
        /// This actually throws an <see cref="InvalidDataException"/> which, in case
        /// processing of the file should continue, such exception must be handled,
        /// otherwise file reading will fail.
        /// </para>
        /// <para>
        /// When debugging this will also log to the error to the console error output stream.
        /// </para>
        /// </summary>
        /// <param name="message">Description of the error found while reading the file.</param>
        protected void EmitError(string message)
        {
            Console.Error.WriteLine($"Error: {message}");
            Errors.Add($"{Context}\t{message}");
            throw new InvalidDataException(message);
        }

        /// <summary>
        /// Generates a warning and logs it to the <see cref="Warnings"/> list.
        /// <para>
        /// When debugging this will also log to the warning to the console error output stream.
        /// </para>
        /// </summary>
        /// <param name="message">Description of the possible issue found while reading the file.</param>
        protected void EmitWarning(string message)
        {
            Console.Error.WriteLine($"Warning: {message}");
            Warnings.Add($"{Context}\t{message}");
        }

        /// <summary>
        /// Checks that the <paramref name="actual"/> value passed matches any of the <paramref name="expected"/>
        /// values, and if it doesn't then an error will be generated using a relevant message.
        /// <para>
        /// <b>Warning:</b> This will throw an exception in case the assertion fails.
        /// </para>
        /// <para>
        /// Use <see cref="CheckValue{T}(string, T, T[])"/> if a "soft assertion", that is, which
        /// only checks the values but does <em>not</em> throw is needed.
        /// </para>
        /// </summary>
        /// <typeparam name="T">
        /// Type of the value to be tested.
        /// <para>Must implement <see cref="IEnumerable{T}"/></para>
        /// </typeparam>
        /// <param name="name">
        /// Reference to the variable, field, or general source of the <paramref name="actual"/> value.
        /// </param>
        /// <param name="actual">
        /// Actual value that is to be tested compared to the list of <paramref name="expected"/> values.
        /// </param>
        /// <param name="expected">
        /// List of expected possible values that are allowed for the <paramref name="actual"/> value.
        /// </param>
        protected void AssertValue<T>(string name, T actual, params T[] expected) where T : IEquatable<T>
        {
            if (!expected.Any(s => EqualityComparer<T>.Default.Equals(s, actual)))
            {
                EmitError($"Expected {name ?? "value"} to be {string.Join(", ", expected)}, actual value is {actual}");
            }
        }

        /// <summary>
        /// Checks that the <paramref name="actual"/> value passed matches any of the <paramref name="expected"/>
        /// values, and otherwise it will generate a warning using a relevant message.
        /// <para>
        /// This method will allow for unexpected values without generating an exception.
        /// </para>
        /// <para>
        /// Use <see cref="AssertValue{T}(string, T, T[])"/> if throwing when unexpected values
        /// are found is desired.
        /// </para>
        /// </summary>
        /// <typeparam name="T">
        /// Type of the value to be tested.
        /// <para>Must implement <see cref="IEnumerable{T}"/></para>
        /// </typeparam>
        /// <param name="name">
        /// Reference to the variable, field, or general source of the <paramref name="actual"/> value.
        /// </param>
        /// <param name="actual">
        /// Actual value that is to be tested compared to the list of <paramref name="expected"/> values.
        /// </param>
        /// <param name="expected">
        /// List of expected possible values that are allowed for the <paramref name="actual"/> value.
        /// </param>
        protected bool CheckValue<T>(string name, T actual, params T[] expected) where T : IEquatable<T>
        {
            if (!expected.Any(s => EqualityComparer<T>.Default.Equals(s, actual)))
            {
                EmitWarning($"Expected {name ?? "value"} to be {string.Join(", ", expected)}, actual value is {actual}");
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Reads the header record containing the size of the data.
        /// </summary>
        /// <param name="storage">
        /// Storage key where to look for the Header stream.
        /// </param>
        /// <returns>Size of the Data section.</returns>
        internal static uint ReadHeader(CFStorage storage)
        {
            using (var header = storage.GetStream("Header").GetBinaryReader())
            {
                return header.ReadUInt32();
            }
        }

        /// <summary>
        /// Reads a block of bytes that is prefixed with its size as an int32.
        /// </summary>
        /// <param name="reader">Binary data reader.</param>
        /// <param name="emptySize">Size considered as an empty block that should have its content skipped.</param>
        /// <returns>Bytes contained in the block.</returns>
        internal static byte[] ReadBlock(BinaryReader reader, int emptySize = 0)
        {
            return ReadBlock(reader, reader.ReadBytes, emptySize: emptySize);
        }

        /// <summary>
        /// Reads and interprets the contents of a block of data that is prefixed with
        /// its size as an int32.
        /// <seealso cref="ReadBlock{T}(BinaryReader, Func{int, T}, Func{T})"/>
        /// </summary>
        /// <param name="reader">Binary data reader.</param>
        /// <param name="interpreter">
        /// Interpreter callback that receives as parameter the size header of the block.
        /// <para>
        /// It is possible that the block contains a flag as the last byte of the int32 size prefix,
        /// in which case the unmasked value is passed to the interpreter callback so it can handle
        /// the flag as needed.
        /// </para>
        /// </param>
        /// <param name="onEmpty">
        /// Callback to signal that the block was empty.
        /// </param>
        /// <param name="emptySize">Size considered as an empty block that should have its content skipped.</param>
        internal static void ReadBlock(BinaryReader reader, Action<int> interpreter, Action onEmpty = null, int emptySize = 0)
        {
            ReadBlock<object>(reader, size =>
            {
                interpreter(size);
                return null;
            }, () =>
            {
                onEmpty?.Invoke();
                return null;
            }, emptySize);
        }

        /// <summary>
        /// Reads and interprets a block that is prefixed with its size as an int32.
        /// <para>
        /// This also makes sure that if any error happens we can recover by skipping the block,
        /// by guaranteeing that regardless of the amount of data read, the reader stream is
        /// left at the appropriate position at the end of the block.
        /// </para>
        /// </summary>
        /// <typeparam name="T">
        /// Type of the interpreted result of the block contents to be returned.
        /// </typeparam>
        /// <param name="reader">Binary data reader.</param>
        /// <param name="interpreter">
        /// Interpreter callback that receives as parameter the size header of the block, and
        /// returns an instance of <typeparamref name="T"/> with interpreted results.
        /// <para>
        /// It is possible that the block contains a flag as the last byte of the int32 size prefix,
        /// in which case the unmasked value is passed to the interpreter callback so it can handle
        /// the flag as needed.
        /// </para>
        /// </param>
        /// <param name="onEmpty">
        /// Callback to signal that the block was empty.
        /// </param>
        /// <param name="emptySize">Size considered as an empty block that should have its content skipped.</param>
        internal static T ReadBlock<T>(BinaryReader reader, Func<int, T> interpreter, Func<T> onEmpty = null, int emptySize = 0)
        {
            var size = reader.ReadInt32();
            var sanitizedSize = size & 0x00ffffff; // mask out last byte which may include a flag
            if (size > emptySize)
            {
                var position = reader.BaseStream.Position;
                try
                {
                    var result = interpreter(size);
                    if (reader.BaseStream.Position > position + sanitizedSize)
                    {
                        // oops... if this happened we read past the block size
                        throw new IndexOutOfRangeException("Read past the end of the block");
                    }
                    return result;
                }
                finally
                {
                    // make sure we end up right after the block even if the interpreter
                    // reads less data than available in the block
                    reader.BaseStream.Position = position + sanitizedSize;
                }
            }
            else if (onEmpty != null)
            {
                return onEmpty();
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Decompresses zlib compressed data and returns the interpreted value.
        /// </summary>
        /// <typeparam name="T">
        /// Type of the interpreted result of the block contents to be returned.
        /// </typeparam>
        /// <param name="data">Byte data to be decompressed and interpreted.</param>
        /// <param name="interpreter">
        /// Interpreter callback that receives as parameter the size header of the block, and
        /// returns an instance of <typeparamref name="T"/> with interpreted results.
        /// </param>
        /// <returns>Value of type <typeparamref name="T"/> with the interpreted results.</returns>
        internal static T ParseCompressedZlibData<T>(byte[] data, Func<MemoryStream, T> interpreter)
        {
            const int ZlibHeaderSize = 2; // skip zlib two byte header
            using (var compressedData = new MemoryStream(data, ZlibHeaderSize, data.Length - ZlibHeaderSize))
            using (var decompressedData = new MemoryStream())
            using (var deflater = new DeflateStream(compressedData, CompressionMode.Decompress))
            {
                deflater.CopyTo(decompressedData);

                decompressedData.Position = 0;
                return interpreter.Invoke(decompressedData);
            }
        }

        /// <summary>
        /// Reads compressed storage (id, data) pair, where data is compressed in zlib format.
        /// </summary>
        /// <typeparam name="T">Type of the interpreted data to be returned.</typeparam>
        /// <param name = "reader" > Binary data reader.</param>
        /// <param name="interpreter">
        /// Interpreter callback that receives as parameter the size header of the block, and
        /// returns an instance of <typeparamref name="T"/> with interpreted results.
        /// </param>
        /// <returns></returns>
        internal (string id, T data) ReadCompressedStorage<T>(BinaryReader reader, Func<MemoryStream, T> interpreter)
        {
            return ReadBlock(reader, size =>
            {
                if (reader.ReadByte() != 0xD0) EmitError("Expected 0xD0 tag");
                var id = ReadPascalShortString(reader);

                // Images are compressed with zlib format including a two byte header
                var zlibData = ReadBlock(reader);
                return ParseCompressedZlibData(zlibData, stream => (id, interpreter(stream)));
            });
        }

        internal (string id, byte[] data) ReadCompressedStorage(BinaryReader reader)
        {
            return ReadCompressedStorage(reader, s => s.ToArray());
        }

        /// <summary>
        /// Parses an array of bytes as a raw string, that is, a string of known <paramref name="size"/>
        /// valued length, <em>without</em> leading length and <em>without</em> a <c>0x00</c> terminator.
        /// </summary>
        /// <param name="data">Byte data to be converted to string.</param>
        /// <param name="index">
        /// Starting index of the string in the <paramref name="data"/> array. This defaults to <c>0</c>.
        /// </param>
        /// <param name="size">
        /// Length of the string to be parsed. This defaults to <c>-1</c> which means reading from the
        /// <paramref name="index"/> position until the end of the <paramref name="data"/> byte array.
        /// </param>
        /// <param name="encoding">
        /// Encoding to be used when parsing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding
        /// </param>
        /// <returns>Parsed string interpreted with the specified encoding.</returns>
        internal static string ParseRawString(byte[] data, int index = 0, int size = -1, Encoding encoding = null)
        {
            size = size == -1 ? data.Length : size;
            if (size != 0)
            {
                encoding = encoding ?? Utils.Win1252Encoding;
                return encoding.GetString(data, index, size);
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Reads from <paramref name="reader"/> a raw string, that is, a string of known <paramref name="size"/>
        /// valued length, <em>without</em> leading length and <em>without</em> a <c>0x00</c> terminator.
        /// </summary>
        /// <param name="reader">Binary reader from where to read the raw string.</param>
        /// <param name="size">Length of the string to be read.</param>
        /// <param name="encoding">
        /// Encoding to be used when parsing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding
        /// </param>
        /// <returns>Read string interpreted with the specified encoding.</returns>
        internal static string ReadRawString(BinaryReader reader, int size, Encoding encoding = null)
        {
            var data = reader.ReadBytes(size);
            return ParseRawString(data, encoding: encoding);
        }

        /// <summary>
        /// Parses an array of bytes as a C-like string, that is, a string that is <c>0x00</c> terminated.
        /// </summary>
        /// <param name="data">Byte data to be converted to string.</param>
        /// <param name="encoding">
        /// Encoding to be used when parsing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding
        /// </param>
        /// <returns>Parsed string interpreted with the specified encoding.</returns>
        internal static string ParseCString(byte[] data, Encoding encoding = null)
        {
            return ParseRawString(data, 0, data.Length - 1, encoding);
        }

        /// <summary>
        /// Reads from <paramref name="reader"/> a string that is 0x00 terminated.
        /// </summary>
        /// <param name="reader">Binary reader from where to read the C-like string.</param>
        /// <param name="size">Length of the string to be read.</param>
        /// <param name="encoding">
        /// Encoding to be used when parsing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding.
        /// </param>
        /// <returns>Read string interpreted with the specified encoding.</returns>
        internal static string ReadCString(BinaryReader reader, int size, Encoding encoding = null)
        {
            var data = reader.ReadBytes(size);
            return ParseCString(data, encoding);
        }

        /// <summary>
        /// Reads a fixed length type of string, which is used for encoding font names.
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
        /// <para>
        /// No hint is given of the actual string data except the maximum length and <c>0x00</c> byte, so
        /// the stream is manually read until that is found. And if the string is shorter than the length
        /// of then skip the remaining bytes.
        /// </para>
        /// </summary>
        /// <param name="reader">Binary reader from where to read the font name.</param>
        /// <returns>Read string.</returns>
        internal static string ReadStringFontName(BinaryReader reader)
        {
            var pos = reader.BaseStream.Position;
            List<byte> data = new List<byte>();
            ushort unicodeChar;
            while (data.Count < 32 && (unicodeChar = reader.ReadUInt16()) != 0)
            {
                data.AddRange(BitConverter.GetBytes(unicodeChar));
            }
            reader.BaseStream.Position = pos + 32; // needs to read 32 bytes, so skip remaining ones
            return ParseRawString(data.ToArray(), encoding: Encoding.Unicode);
        }

        /// <summary>
        /// Reads from <paramref name="reader"/> a Pascal-like string, that is, a string which is
        /// prefixed with its size as an int32.
        /// </summary>
        /// <param name="reader">Binary reader from where to read the string.</param>
        /// <param name="encoding">
        /// Encoding to be used when parsing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding.
        /// </param>
        /// <returns>Read string interpreted with the specified encoding.</returns>
        internal static string ReadPascalString(BinaryReader reader, Encoding encoding = null)
        {
            return ReadBlock(reader, size => ReadCString(reader, size, encoding));
        }

        /// <summary>
        /// Reads from <paramref name="reader"/> a string which is prefixed with its size
        /// as a byte.
        /// <para>
        /// This is roughly equivalent to Pascal <c>ShortString</c> with the exception that
        /// we can support different encodings.
        /// </para>
        /// </summary>
        /// <param name="reader">Binary reader from where to read the string.</param>
        /// <param name="encoding">
        /// Encoding to be used when parsing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding.
        /// </param>
        /// <returns>Read string interpreted with the specified encoding.</returns>
        internal static string ReadPascalShortString(BinaryReader reader, Encoding encoding = null)
        {
            return ReadRawString(reader, reader.ReadByte(), encoding: encoding);
        }

        /// <summary>
        /// Reads from <paramref name="reader"/> a block, as read by <see cref="ReadBlock{T}(BinaryReader, Func{int, T}, Func{T})"/>,
        /// which contains a single Pascal's ShortString-like string.
        /// <para>
        /// </para>
        /// <seealso cref="ReadBlock{T}(BinaryReader, Func{int, T}, Func{T})"/>
        /// <seealso cref="ReadPascalShortString(BinaryReader, Encoding)"/>
        /// </summary>
        /// <param name="reader">Binary reader from where to read the string.</param>
        /// <param name="encoding">
        /// Encoding to be used when parsing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding.
        /// </param>
        /// <returns>Read string contained in the block as interpreted with the specified encoding.</returns>
        internal static string ReadStringBlock(BinaryReader reader, Encoding encoding = null)
        {
            return ReadBlock(reader, size => ReadPascalShortString(reader, encoding));
        }

        /// <summary>
        /// Reads from <paramref name="reader"/> a C-like string encoded using Windows-1252 and
        /// interprets it as a <see cref="ParameterCollection"/>.
        /// </summary>
        /// <param name="reader">
        /// Binary reader from where to read the data used to populate the <see cref="ParameterCollection"/>.
        /// </param>
        /// <param name="size">Length of the string in bytes.</param>
        /// <param name="raw">If false the parameter are 0x00 terminated.</param>
        /// <param name="encoding">
        /// Encoding to be used when parsing the string. When <c>null</c> this defaults to Windows-1252
        /// code page encoding.
        /// </param>
        /// <returns>New instance of <see cref="ParameterCollection"/> containing the read data.</returns>
        internal static ParameterCollection ReadParameters(BinaryReader reader, int size, bool raw = false, Encoding encoding = null)
        {
            var data = raw ? ReadRawString(reader, size, encoding) : ReadCString(reader, size, encoding);
            return ParameterCollection.FromString(data);
        }

        /// <summary>
        /// Reads a pair of (x, y) <see cref="Coord"/>s as a <see cref="CoordPoint"/>.
        /// </summary>
        /// <param name="reader">Binary reader from where to read the point.</param>
        /// <returns>Read point.</returns>
        internal static CoordPoint ReadCoordPoint(BinaryReader reader)
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            return new CoordPoint(x, y);
        }

        /// <summary>
        /// Reads a list of strings  from the "WideStrings" stream inside
        /// of the specified <paramref name="storage"/> key.
        /// <para>
        /// Each string entry is encoded inside a parameter string value with a comma separated
        /// list of integers. Those values are interpreted as UTF-16 code-points.
        /// </para>
        /// <para>
        /// These strings are used as Unicode variants of the texts existing in text string
        /// binary records.
        /// </para>
        /// </summary>
        /// <param name="storage">
        /// Storage key where to look for the "WideStrings" stream.
        /// </param>
        /// <returns>
        /// List of strings in the same order as they will be used by the components later.
        /// </returns>
        internal List<string> ReadWideStrings(CFStorage storage)
        {
            BeginContext("WideStrings");

            var result = new List<string>();
            using (var reader = storage.GetStream("WideStrings").GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                for (var i = 0; i < parameters.Count; ++i)
                {
                    var chars = parameters[$"ENCODEDTEXT{i}"].AsIntList()
                        .Select(codepoint => Convert.ToChar(codepoint));
                    var text = string.Concat(chars);
                    result.Add(text);
                }
            }

            EndContext();

            return result;
        }

        /// <summary>
        /// Utility function used to extract data from a binary reader without changing its
        /// underlying stream position.
        /// </summary>
        /// <param name="reader">Binary reader to be used.</param>
        /// <param name="startPosition">Position where to start reading the data.</param>
        /// <param name="endPosition">Ending position including the last byte to read.</param>
        /// <returns>
        /// Array of bytes with the data read from the <paramref name="reader"/>'s stream.
        /// </returns>
        protected static byte[] ExtractStreamData(BinaryReader reader, long startPosition, long endPosition)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (!reader.BaseStream.CanSeek) throw new InvalidOperationException("Reader base stream is not seekable");

            var currentPosition = reader.BaseStream.Position;
            try
            {
                var count = endPosition - startPosition;
                reader.BaseStream.Position = startPosition;
                return reader.ReadBytes((int)count);
            }
            finally
            {
                reader.BaseStream.Position = currentPosition;
            }
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
