using OpenMcdf;
using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using OriginalCircuit.Altium.Serialization.Compound;
using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace OriginalCircuit.Altium.Serialization.Readers;

/// <summary>
/// Primitive object IDs used in PCB binary format.
/// </summary>
internal enum PcbPrimitiveObjectId : byte
{
    Arc = 1,
    Pad = 2,
    Via = 3,
    Track = 4,
    Text = 5,
    Fill = 6,
    Region = 11,
    ComponentBody = 12
}

/// <summary>
/// Reads PCB footprint library (.PcbLib) files.
/// </summary>
public sealed class PcbLibReader
{
    private readonly Dictionary<string, string> _sectionKeys = new(StringComparer.OrdinalIgnoreCase);
    private List<AltiumDiagnostic> _diagnostics = new();

    /// <summary>
    /// Reads a PcbLib file from the specified path.
    /// </summary>
    /// <param name="path">Path to the .PcbLib file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed PCB footprint library.</returns>
    /// <exception cref="AltiumCorruptFileException">Thrown when the file cannot be parsed.</exception>
    /// <remarks>This method is not thread-safe. Create a new reader instance per thread.</remarks>
    public async ValueTask<PcbLibrary> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var accessor = await CompoundFileAccessor.OpenAsync(path, writable: false, cancellationToken);
            return Read(accessor, cancellationToken);
        }
        catch (Exception ex) when (ex is not AltiumFileException and not OperationCanceledException
            and not OutOfMemoryException and not FileNotFoundException and not UnauthorizedAccessException
            and not DirectoryNotFoundException)
        {
            throw new AltiumCorruptFileException($"Failed to read PcbLib file: {ex.Message}", filePath: path, innerException: ex);
        }
    }

    /// <summary>
    /// Reads a PcbLib file from a stream.
    /// </summary>
    /// <param name="stream">A readable stream containing compound file data. The stream is not closed.</param>
    /// <returns>The parsed PCB footprint library.</returns>
    /// <exception cref="AltiumCorruptFileException">Thrown when the stream cannot be parsed.</exception>
    /// <remarks>This method is not thread-safe. Create a new reader instance per thread.</remarks>
    public PcbLibrary Read(Stream stream)
    {
        try
        {
            using var accessor = CompoundFileAccessor.Open(stream, leaveOpen: true);
            return Read(accessor);
        }
        catch (Exception ex) when (ex is not AltiumFileException and not OutOfMemoryException)
        {
            throw new AltiumCorruptFileException($"Failed to read PcbLib file: {ex.Message}", innerException: ex);
        }
    }

    private PcbLibrary Read(CompoundFileAccessor accessor, CancellationToken cancellationToken = default)
    {
        _diagnostics = new List<AltiumDiagnostic>();
        var library = new PcbLibrary();

        // Read and preserve FileHeader trailing data (after version string)
        ReadFileHeader(accessor, library);

        // Preserve additional root-level streams/storages for round-trip fidelity
        // (FileVersionInfo, etc.)
        PreserveAdditionalRootStreams(accessor, library);

        // Read section keys mapping
        ReadSectionKeys(accessor, library);

        // Read library data (components list and their primitives)
        ReadLibrary(accessor, library, cancellationToken);

        library.Diagnostics = _diagnostics;
        return library;
    }

    private static void ReadFileHeader(CompoundFileAccessor accessor, PcbLibrary library)
    {
        var stream = accessor.TryGetStream("FileHeader");
        if (stream == null)
            return;

        var data = stream.GetData();
        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        // Read the version string: int32 length + pascal short string
        var blockLen = reader.ReadInt32();
        if (blockLen <= 0)
            return;
        var stringLen = reader.ReadByte();
        reader.Skip(stringLen);
        var consumed = 4 + 1 + stringLen;
        // Skip any padding within the block
        if (consumed < 4 + blockLen)
            reader.Skip(4 + blockLen - consumed);

        // After the version string block, there are 3 pascal short strings:
        // 1) Format version double (5.01) + 2 padding bytes
        // 2) Empty string (placeholder)
        // 3) 8-character unique library identifier
        if (reader.HasMore)
        {
            var versionLen = reader.ReadByte();
            if (versionLen > 0)
                reader.Skip(versionLen); // skip version double + padding
        }
        if (reader.HasMore)
        {
            var emptyLen = reader.ReadByte();
            if (emptyLen > 0)
                reader.Skip(emptyLen); // skip empty string
        }
        if (reader.HasMore)
        {
            var idLen = reader.ReadByte();
            if (idLen > 0)
            {
                var idBytes = new byte[idLen];
                reader.ReadExact(idBytes);
                library.UniqueId = AltiumEncoding.Windows1252.GetString(idBytes);
            }
        }
    }

    private static void PreserveAdditionalRootStreams(CompoundFileAccessor accessor, PcbLibrary library)
    {
        library.AdditionalRootStreams = new Dictionary<string, byte[]>();

        // Known additional storages that Altium writes
        var additionalStorages = new[] { "FileVersionInfo" };
        foreach (var storageName in additionalStorages)
        {
            var storage = accessor.TryGetStorage(storageName);
            if (storage != null)
            {
                storage.VisitEntries(entry =>
                {
                    if (entry is OpenMcdf.CFStream stream)
                    {
                        library.AdditionalRootStreams[$"{storageName}/{entry.Name}"] = stream.GetData();
                    }
                }, false);
            }
        }
    }

    private void ReadSectionKeys(CompoundFileAccessor accessor, PcbLibrary library)
    {
        _sectionKeys.Clear();

        var stream = accessor.TryGetStream("SectionKeys");
        if (stream == null)
            return;

        var data = stream.GetData();
        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        var keyCount = reader.ReadInt32();
        for (var i = 0; i < keyCount; i++)
        {
            var libRef = ReadPascalString(reader);
            var sectionKey = reader.ReadStringBlock();
            _sectionKeys[libRef] = sectionKey;
        }

        // Preserve section keys for round-trip fidelity
        library.SectionKeys = new Dictionary<string, string>(_sectionKeys, StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadPascalString(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        if (size <= 0)
            return string.Empty;

        // Read null-terminated string
        var length = reader.ReadByte();
        if (length == 0)
        {
            reader.Skip(size - 1);
            return string.Empty;
        }

        // Use stack for small strings, rent for larger ones
        var encoding = AltiumEncoding.Windows1252;
        int stringLength = length; // byte fits in int, avoid Math.Min ambiguity
        Span<byte> buffer = stackalloc byte[Math.Min(stringLength, 256)];

        if (stringLength <= 256)
        {
            reader.ReadExact(buffer.Slice(0, length));
            var consumed = 1 + length;
            if (consumed < size)
                reader.Skip(size - consumed);
            return encoding.GetString(buffer.Slice(0, length));
        }
        else
        {
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                reader.ReadExact(rentedBuffer.AsSpan(0, length));
                var consumed = 1 + length;
                if (consumed < size)
                    reader.Skip(size - consumed);
                return encoding.GetString(rentedBuffer, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    private void ReadLibrary(CompoundFileAccessor accessor, PcbLibrary library, CancellationToken cancellationToken)
    {
        var libraryStorage = accessor.TryGetStorage("Library")
            ?? throw new InvalidDataException("PcbLib file missing 'Library' storage");

        // Read header (record count - not currently used)
        GetChildStream(libraryStorage, "Header");

        // Read library data
        var dataStream = GetChildStream(libraryStorage, "Data")
            ?? throw new InvalidDataException("Library storage missing 'Data' stream");

        var data = dataStream.GetData();
        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        // Read library parameters (header info) into dictionary
        var libraryParams = ReadParameterBlock(reader);
        library.LibraryParameters = libraryParams;

        // Read footprint count
        var footprintCount = reader.ReadUInt32();

        for (var i = 0; i < footprintCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var refName = reader.ReadStringBlock();
            var sectionKey = GetSectionKeyFromRefName(refName);

            var component = ReadFootprint(accessor, sectionKey, cancellationToken);
            if (component != null)
            {
                library.Add(component);
            }
        }

        // Preserve additional library-level streams for round-trip fidelity
        library.AdditionalLibraryStreams = new Dictionary<string, byte[]>();
        var knownLibraryChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Header", "Data", "Models" };
        libraryStorage.VisitEntries(entry =>
        {
            if (knownLibraryChildren.Contains(entry.Name))
                return;
            if (entry is OpenMcdf.CFStream stream)
            {
                library.AdditionalLibraryStreams[entry.Name] = stream.GetData();
            }
            else if (entry is OpenMcdf.CFStorage subStorage)
            {
                // Preserve sub-storage streams (e.g., ComponentParamsTOC/Data)
                subStorage.VisitEntries(subEntry =>
                {
                    if (subEntry is OpenMcdf.CFStream subStream)
                    {
                        library.AdditionalLibraryStreams[$"{entry.Name}/{subEntry.Name}"] = subStream.GetData();
                    }
                }, false);
            }
        }, false);

        // Parse 3D model streams (STEP data + metadata)
        if (libraryStorage.TryGetStorage("Models", out var modelsStorage))
        {
            ReadModels(modelsStorage, library);
        }
    }

    private static void ReadModels(CFStorage modelsStorage, PcbLibrary library)
    {
        // Read Data stream: contains parameter blocks with model metadata
        // Format: int32 length + C-string (null-terminated pipe-delimited params)
        // One entry per model with: EMBED, MODELSOURCE, ID, ROTX, ROTY, ROTZ, DZ, CHECKSUM, NAME
        var modelMetadata = new List<Dictionary<string, string>>();
        if (modelsStorage.TryGetStream("Data", out var dataStream))
        {
            var dataBytes = dataStream.GetData();
            using var ms = new MemoryStream(dataBytes);
            using var br = new BinaryReader(ms, Encoding.ASCII);

            while (ms.Position + 4 <= ms.Length)
            {
                var paramLen = br.ReadInt32();
                if (paramLen <= 0 || ms.Position + paramLen > ms.Length)
                    break;

                var paramStr = Encoding.ASCII.GetString(br.ReadBytes(paramLen)).TrimEnd('\0');
                var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var part in paramStr.Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    var eqIdx = part.IndexOf('=');
                    if (eqIdx > 0)
                        meta[part[..eqIdx]] = part[(eqIdx + 1)..];
                }
                modelMetadata.Add(meta);
            }
        }

        // Read numbered STEP streams (0, 1, 2, ...) - zlib compressed STEP text
        for (var i = 0; ; i++)
        {
            if (!modelsStorage.TryGetStream(i.ToString(), out var modelStream))
                break;

            var compressedData = modelStream.GetData();
            var model = new PcbModel();

            // Apply metadata from Data stream if available
            if (i < modelMetadata.Count)
            {
                var meta = modelMetadata[i];
                if (meta.TryGetValue("ID", out var id)) model.Id = id;
                if (meta.TryGetValue("NAME", out var name)) model.Name = name;
                if (meta.TryGetValue("EMBED", out var embed)) model.IsEmbedded = string.Equals(embed, "TRUE", StringComparison.OrdinalIgnoreCase);
                if (meta.TryGetValue("MODELSOURCE", out var source)) model.ModelSource = source;
                if (meta.TryGetValue("ROTX", out var rotx) && double.TryParse(rotx, System.Globalization.CultureInfo.InvariantCulture, out var rx)) model.RotationX = rx;
                if (meta.TryGetValue("ROTY", out var roty) && double.TryParse(roty, System.Globalization.CultureInfo.InvariantCulture, out var ry)) model.RotationY = ry;
                if (meta.TryGetValue("ROTZ", out var rotz) && double.TryParse(rotz, System.Globalization.CultureInfo.InvariantCulture, out var rz)) model.RotationZ = rz;
                if (meta.TryGetValue("DZ", out var dz) && int.TryParse(dz, out var dzVal)) model.Dz = dzVal;
                if (meta.TryGetValue("CHECKSUM", out var cs) && int.TryParse(cs, out var csVal)) model.Checksum = csVal;
            }

            // Decompress STEP data
            if (compressedData.Length > 0)
            {
                using var ms = new MemoryStream(compressedData);
                using var zs = new ZLibStream(ms, CompressionMode.Decompress);
                using var outMs = new MemoryStream();
                zs.CopyTo(outMs);
                model.StepData = Encoding.UTF8.GetString(outMs.ToArray());
            }

            library.Models.Add(model);
        }
    }

    private string GetSectionKeyFromRefName(string refName)
    {
        if (_sectionKeys.TryGetValue(refName, out var sectionKey))
            return sectionKey;

        // Fallback: mangle name to fit compound storage limitations
        var maxLength = Math.Min(refName.Length, 31);
        return refName.Substring(0, maxLength).Replace('/', '_');
    }

    private PcbComponent? ReadFootprint(CompoundFileAccessor accessor, string sectionKey, CancellationToken cancellationToken = default)
    {
        var storage = accessor.TryGetStorage(sectionKey);
        if (storage == null)
            return null;

        var component = new PcbComponent();

        // Read header (record count - not currently used beyond validation)
        GetChildStream(storage, "Header");

        // Read parameters
        var paramStream = GetChildStream(storage, "Parameters");
        if (paramStream != null)
        {
            var paramData = paramStream.GetData();
            using var paramMs = new MemoryStream(paramData);
            using var paramReader = new BinaryFormatReader(paramMs, leaveOpen: true);

            var parameters = ReadParameterBlock(paramReader);
            ApplyComponentParameters(component, parameters);
        }

        // Read wide strings (Unicode text for Text primitives)
        var wideStrings = ReadWideStrings(storage);

        // Preserve additional component-level streams (PrimitiveGuids, UniqueIdPrimitiveInformation, etc.)
        component.AdditionalStreams = new Dictionary<string, byte[]>();
        var knownChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Header", "Parameters", "WideStrings", "Data" };
        storage.VisitEntries(entry =>
        {
            if (knownChildren.Contains(entry.Name))
                return;
            if (entry is OpenMcdf.CFStream stream)
            {
                component.AdditionalStreams[entry.Name] = stream.GetData();
            }
            else if (entry is OpenMcdf.CFStorage subStorage)
            {
                subStorage.VisitEntries(subEntry =>
                {
                    if (subEntry is OpenMcdf.CFStream subStream)
                    {
                        component.AdditionalStreams[$"{entry.Name}/{subEntry.Name}"] = subStream.GetData();
                    }
                }, false);
            }
        }, false);

        // Read primitive data
        var dataStream = GetChildStream(storage, "Data");
        if (dataStream != null)
        {
            var data = dataStream.GetData();
            using var ms = new MemoryStream(data);
            using var reader = new BinaryFormatReader(ms, leaveOpen: true);

            // First comes the pattern name
            var pattern = reader.ReadStringBlock();
            if (string.IsNullOrEmpty(component.Name))
                component.Name = pattern;

            // Then the primitives
            while (reader.HasMore)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var objectId = (PcbPrimitiveObjectId)reader.ReadByte();

                switch (objectId)
                {
                    case PcbPrimitiveObjectId.Arc:
                        var arc = ReadArc(reader);
                        if (arc != null)
                            component.AddArc(arc);
                        break;

                    case PcbPrimitiveObjectId.Pad:
                        var pad = ReadPad(reader);
                        if (pad != null)
                            component.AddPad(pad);
                        break;

                    case PcbPrimitiveObjectId.Via:
                        var via = ReadVia(reader);
                        if (via != null)
                            component.AddVia(via);
                        break;

                    case PcbPrimitiveObjectId.Track:
                        var track = ReadTrack(reader);
                        if (track != null)
                            component.AddTrack(track);
                        break;

                    case PcbPrimitiveObjectId.Text:
                        var text = ReadText(reader, wideStrings);
                        if (text != null)
                            component.AddText(text);
                        break;

                    case PcbPrimitiveObjectId.Fill:
                        var fill = ReadFill(reader);
                        if (fill != null)
                            component.AddFill(fill);
                        break;

                    case PcbPrimitiveObjectId.Region:
                        var region = ReadRegion(reader);
                        if (region != null)
                            component.AddRegion(region);
                        break;

                    case PcbPrimitiveObjectId.ComponentBody:
                        var body = ReadComponentBody(reader);
                        if (body != null)
                            component.AddComponentBody(body);
                        break;

                    default:
                        // Unknown primitive - try to skip
                        reader.SkipBlock();
                        break;
                }
            }
        }

        return component;
    }

    internal static CFStream? GetChildStream(CFStorage storage, string name)
    {
        return storage.TryGetStream(name, out var stream) ? stream : null;
    }

    internal static Dictionary<string, string> ReadParameterBlock(BinaryFormatReader reader)
    {
        return ReadParameterBlock(reader, out _);
    }

    internal static Dictionary<string, string> ReadParameterBlock(BinaryFormatReader reader, out string rawString)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
        {
            rawString = string.Empty;
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // PCB parameter blocks are C-strings (null-terminated, no length prefix).
        // Read the entire block as raw bytes and decode as a string.
        byte[] buffer;
        if (sanitizedSize <= 512)
        {
            Span<byte> stackBuffer = stackalloc byte[sanitizedSize];
            reader.ReadExact(stackBuffer);
            buffer = stackBuffer.ToArray();
        }
        else
        {
            buffer = new byte[sanitizedSize];
            reader.ReadExact(buffer);
        }

        // Find the null terminator (if present) and decode the string
        var nullIndex = Array.IndexOf(buffer, (byte)0);
        var length = nullIndex >= 0 ? nullIndex : sanitizedSize;
        var paramString = AltiumEncoding.Windows1252.GetString(buffer, 0, length);

        rawString = paramString;
        return ParseParameters(paramString);
    }

    internal static Dictionary<string, string> ParseParameters(string paramString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(paramString))
            return result;

        // Parameters are in format: |KEY1=VALUE1|KEY2=VALUE2|...
        var span = paramString.AsSpan();

        var start = 0;
        while (start < span.Length)
        {
            // Skip leading pipe
            if (span[start] == '|')
                start++;

            if (start >= span.Length)
                break;

            // Find the equals sign
            var equalsIndex = span.Slice(start).IndexOf('=');
            if (equalsIndex < 0)
                break;

            var key = span.Slice(start, equalsIndex).ToString();
            start += equalsIndex + 1;

            // Find the next pipe or end
            var pipeIndex = span.Slice(start).IndexOf('|');
            string value;
            if (pipeIndex < 0)
            {
                value = span.Slice(start).ToString();
                start = span.Length;
            }
            else
            {
                value = span.Slice(start, pipeIndex).ToString();
                start += pipeIndex;
            }

            result[key] = value;
        }

        return result;
    }

    private static void ApplyComponentParameters(PcbComponent component, Dictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("PATTERN", out var pattern))
            component.Name = pattern;

        if (parameters.TryGetValue("DESCRIPTION", out var description))
            component.Description = description;

        if (parameters.TryGetValue("HEIGHT", out var heightStr) && TryParseCoord(heightStr, out var height))
            component.Height = height;

        if (parameters.TryGetValue("ITEMGUID", out var itemGuid))
            component.ItemGUID = itemGuid;

        if (parameters.TryGetValue("REVISIONGUID", out var revisionGuid))
            component.ItemRevisionGUID = revisionGuid;

        // Preserve any additional parameters not modeled as typed properties
        component.AdditionalParameters = ExtractAdditionalParameters(parameters,
            ["PATTERN", "DESCRIPTION", "HEIGHT", "ITEMGUID", "REVISIONGUID"]);
    }

    private static Dictionary<string, string>? ExtractAdditionalParameters(
        Dictionary<string, string> parameters, IEnumerable<string> knownKeys)
    {
        var known = new HashSet<string>(knownKeys, StringComparer.OrdinalIgnoreCase);
        var additional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in parameters)
        {
            if (!known.Contains(kvp.Key))
                additional[kvp.Key] = kvp.Value;
        }
        return additional.Count > 0 ? additional : null;
    }

    private static bool TryParseCoord(string value, out Coord result)
    {
        result = default;

        // Altium stores coords as strings like "10mil" or raw integers
        if (string.IsNullOrEmpty(value))
            return false;

        // Try parse as integer (internal units)
        if (int.TryParse(value, out var intValue))
        {
            result = Coord.FromRaw(intValue);
            return true;
        }

        // Try parse with unit suffix
        var span = value.AsSpan();
        if (span.EndsWith("mil", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(span.Slice(0, span.Length - 3), out var mils))
            {
                result = Coord.FromMils(mils);
                return true;
            }
        }
        else if (span.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(span.Slice(0, span.Length - 2), out var mm))
            {
                result = Coord.FromMm(mm);
                return true;
            }
        }

        return false;
    }

    internal static List<string> ReadWideStrings(CFStorage storage)
    {
        var result = new List<string>();

        if (!storage.TryGetStream("WideStrings", out var stream))
            return result;

        var data = stream.GetData();
        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        var parameters = ReadParameterBlock(reader);

        for (var i = 0; i < parameters.Count; i++)
        {
            var key = $"ENCODEDTEXT{i}";
            if (!parameters.TryGetValue(key, out var encodedText))
                break;

            // Encoded text is comma-separated UTF-16 code points
            var text = DecodeWideString(encodedText);
            result.Add(text);
        }

        return result;
    }

    internal static string DecodeWideString(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return string.Empty;

        var parts = encoded.Split(',');
        var chars = new char[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out var codePoint))
            {
                chars[i] = (char)codePoint;
            }
        }

        return new string(chars);
    }

    internal static void ReadCommonPrimitiveData(BinaryFormatReader reader, out byte layer, out ushort flags)
    {
        ReadCommonPrimitiveData(reader, out layer, out flags, out _);
    }

    internal static void ReadCommonPrimitiveData(BinaryFormatReader reader, out byte layer, out ushort flags, out int componentIndex)
    {
        layer = reader.ReadByte();
        flags = reader.ReadUInt16();

        // 10 bytes: uint16 netIndex, uint16 reserved, uint16 componentIndex, uint32 reserved
        reader.Skip(4); // net index + reserved
        componentIndex = reader.ReadUInt16(); // component index (0xFFFF = free primitive)
        if (componentIndex == 0xFFFF)
            componentIndex = -1;
        reader.Skip(4); // remaining reserved
    }

    internal static CoordPoint ReadCoordPoint(BinaryFormatReader reader)
    {
        var x = reader.ReadInt32();
        var y = reader.ReadInt32();
        return new CoordPoint(Coord.FromRaw(x), Coord.FromRaw(y));
    }

    internal static PcbArc? ReadArc(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags);

        var center = ReadCoordPoint(reader);
        var radius = Coord.FromRaw(reader.ReadInt32());
        var startAngle = reader.ReadDouble();
        var endAngle = reader.ReadDouble();
        var width = Coord.FromRaw(reader.ReadInt32());

        // Skip any trailing data (record size 56 has 11 extra bytes)
        var consumed = reader.Position - startPos;
        var remaining = sanitizedSize - consumed;
        if (remaining > 0)
            reader.Skip((int)remaining);

        var arc = PcbArc.Create()
            .At(center.X, center.Y)
            .Radius(radius)
            .Angles(startAngle, endAngle)
            .Width(width)
            .Layer(layer)
            .Build();

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        arc.IsLocked = isLocked;
        arc.IsTentingTop = isTentingTop;
        arc.IsTentingBottom = isTentingBottom;
        arc.IsKeepout = isKeepout;

        return arc;
    }

    internal static PcbPad? ReadPad(BinaryFormatReader reader)
    {
        // Pad has a complex multi-block structure
        var designator = reader.ReadStringBlock();

        // Skip reserved blocks (always single zero byte) and net string (always "|&|0")
        reader.SkipBlock();
        reader.ReadStringBlock(); // Usually "|&|0" - discard
        reader.SkipBlock();

        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;
        ReadCommonPrimitiveData(reader, out var layer, out var flags, out var componentIndex);

        // Read main block fields
        var location = ReadCoordPoint(reader);
        var sizeTop = ReadCoordPoint(reader);
        var sizeMiddle = ReadCoordPoint(reader);
        var sizeBottom = ReadCoordPoint(reader);
        var holeSize = Coord.FromRaw(reader.ReadInt32());
        var shapeTop = reader.ReadByte();
        var shapeMiddle = reader.ReadByte();
        var shapeBottom = reader.ReadByte();
        var rotation = reader.ReadDouble();
        var isPlated = reader.ReadByte() != 0;

        // Read extended main block fields (power plane, masks, drill, jumper)
        ReadPadExtendedFields(reader, startPos, sanitizedSize,
            out var stackMode, out var powerPlaneConnectStyle,
            out var reliefAirGap, out var reliefConductorWidth, out var reliefEntries,
            out var powerPlaneClearance, out var powerPlaneReliefExpansion,
            out var pasteMaskExpansion, out var solderMaskExpansion,
            out var drillType, out var jumperId);

        // Read size/shape block (596 bytes for extended pad data)
        ReadPadSizeShapeBlock(reader, stackMode, ref shapeTop, ref shapeMiddle, ref shapeBottom,
            out var layerXSizes, out var layerYSizes, out var internalLayerShapes,
            out var holeShapeByte, out var holeSlotLength, out var holeRotation,
            out var offsetX, out var offsetY, out var hasRoundedRectByte,
            out var perLayerShapes, out var perLayerCornerRadii, out var hasSizeShapeBlock);

        // Build the pad model
        var pad = PcbPad.Create()
            .At(location.X, location.Y)
            .Size(sizeTop.X, sizeTop.Y)
            .Shape((PadShape)shapeTop)
            .HoleSize(holeSize)
            .Plated(isPlated)
            .Rotation(rotation)
            .WithDesignator(designator)
            .Layer(layer)
            .Build();

        pad.ComponentIndex = componentIndex;
        pad.SizeMiddle = sizeMiddle;
        pad.SizeBottom = sizeBottom;
        pad.ShapeMiddle = (PadShape)shapeMiddle;
        pad.ShapeBottom = (PadShape)shapeBottom;
        pad.PasteMaskExpansion = Coord.FromRaw(pasteMaskExpansion);
        pad.SolderMaskExpansion = Coord.FromRaw(solderMaskExpansion);
        pad.Mode = stackMode;
        pad.JumperID = jumperId;

        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        pad.IsLocked = isLocked;
        pad.IsTentingTop = isTentingTop;
        pad.IsTentingBottom = isTentingBottom;
        pad.IsKeepout = isKeepout;

        pad.PowerPlaneConnectStyle = powerPlaneConnectStyle;
        pad.ReliefAirGap = Coord.FromRaw(reliefAirGap);
        pad.ReliefConductorWidth = Coord.FromRaw(reliefConductorWidth);
        pad.ReliefEntries = reliefEntries;
        pad.PowerPlaneClearance = Coord.FromRaw(powerPlaneClearance);
        pad.PowerPlaneReliefExpansion = Coord.FromRaw(powerPlaneReliefExpansion);
        pad.DrillType = drillType;

        pad.LayerXSizes = layerXSizes;
        pad.LayerYSizes = layerYSizes;
        pad.InternalLayerShapes = internalLayerShapes;
        pad.HoleType = (PadHoleType)holeShapeByte;
        pad.HoleSlotLength = holeSlotLength;
        pad.HoleRotation = holeRotation;
        pad.OffsetXFromHoleCenter = offsetX;
        pad.OffsetYFromHoleCenter = offsetY;
        pad.HasRoundedRectByte = hasRoundedRectByte;
        pad.PerLayerShapes = perLayerShapes;
        pad.PerLayerCornerRadii = perLayerCornerRadii;
        pad.HasSizeShapeBlock = hasSizeShapeBlock;
        return pad;
    }

    private static void ReadPadExtendedFields(BinaryFormatReader reader, long startPos, long blockSize,
        out int stackMode, out byte powerPlaneConnectStyle,
        out int reliefAirGap, out int reliefConductorWidth, out short reliefEntries,
        out int powerPlaneClearance, out int powerPlaneReliefExpansion,
        out int pasteMaskExpansion, out int solderMaskExpansion,
        out byte drillType, out short jumperId)
    {
        stackMode = 0;
        pasteMaskExpansion = 0; solderMaskExpansion = 0;
        jumperId = 0;
        powerPlaneConnectStyle = 0;
        reliefAirGap = 0; reliefConductorWidth = 0;
        reliefEntries = 4;
        powerPlaneClearance = 0; powerPlaneReliefExpansion = 0;
        drillType = 0;

        long Remaining() => blockSize - (reader.Position - startPos);

        if (Remaining() >= 25) // Fields from offset 61-85
        {
            reader.Skip(1); // offset 61: constant 0
            stackMode = reader.ReadByte(); // offset 62: StackMode
            powerPlaneConnectStyle = reader.ReadByte(); // offset 63
            reliefAirGap = reader.ReadInt32(); // offset 64
            reliefConductorWidth = reader.ReadInt32(); // offset 68
            reliefEntries = reader.ReadInt16(); // offset 72
            powerPlaneClearance = reader.ReadInt32(); // offset 74
            powerPlaneReliefExpansion = reader.ReadInt32(); // offset 78
            reader.Skip(4); // offset 82: reserved
        }
        if (Remaining() >= 8) // PasteMask + SolderMask (offset 86-93)
        {
            pasteMaskExpansion = reader.ReadInt32();
            solderMaskExpansion = reader.ReadInt32();
        }
        if (Remaining() >= 16) // Unk bytes + flags + DrillType + reserved (offset 94-109)
        {
            reader.Skip(9); // offset 94-102: unknown + manual mask flags
            drillType = reader.ReadByte(); // offset 103: DrillType
            reader.Skip(6); // offset 104-109: reserved
        }
        if (Remaining() >= 4) // JumperID + reserved (offset 110-113)
        {
            jumperId = reader.ReadInt16();
            reader.Skip(2); // offset 112: reserved
        }

        // Skip any trailing data beyond the standard 114-byte main block
        if (Remaining() > 0)
            reader.Skip((int)Remaining());
    }

    private static void ReadPadSizeShapeBlock(BinaryFormatReader reader, int stackMode,
        ref byte shapeTop, ref byte shapeMiddle, ref byte shapeBottom,
        out int[] layerXSizes, out int[] layerYSizes, out byte[] internalLayerShapes,
        out byte holeShapeByte, out int holeSlotLength, out double holeRotation,
        out int[] offsetX, out int[] offsetY, out byte hasRoundedRectByte,
        out byte[] perLayerShapes, out byte[] perLayerCornerRadii, out bool hasSizeShapeBlock)
    {
        var sizeShapeBlockSize = reader.ReadInt32();
        var sanitizedSize = sizeShapeBlockSize & 0x00FFFFFF;
        hasSizeShapeBlock = sanitizedSize > 0;

        layerXSizes = new int[29];
        layerYSizes = new int[29];
        internalLayerShapes = new byte[29];
        holeShapeByte = 0;
        holeSlotLength = 0;
        holeRotation = 0;
        offsetX = new int[32];
        offsetY = new int[32];
        hasRoundedRectByte = 0;
        perLayerShapes = new byte[32];
        perLayerCornerRadii = new byte[32];

        if (sanitizedSize >= 596)
        {
            var ssStartPos = reader.Position;

            for (var i = 0; i < 29; i++) layerXSizes[i] = reader.ReadInt32();
            for (var i = 0; i < 29; i++) layerYSizes[i] = reader.ReadInt32();
            for (var i = 0; i < 29; i++) internalLayerShapes[i] = reader.ReadByte();
            reader.Skip(1); // reserved byte
            holeShapeByte = reader.ReadByte();
            holeSlotLength = reader.ReadInt32();
            holeRotation = reader.ReadDouble();
            for (var i = 0; i < 32; i++) offsetX[i] = reader.ReadInt32();
            for (var i = 0; i < 32; i++) offsetY[i] = reader.ReadInt32();
            hasRoundedRectByte = reader.ReadByte();
            for (var i = 0; i < 32; i++) perLayerShapes[i] = reader.ReadByte();
            for (var i = 0; i < 32; i++) perLayerCornerRadii[i] = reader.ReadByte();

            var ssRemaining = sanitizedSize - (reader.Position - ssStartPos);
            if (ssRemaining > 0)
                reader.Skip((int)ssRemaining);
        }
        else if (sanitizedSize > 0)
        {
            reader.Skip(sanitizedSize);
        }

        // Apply per-layer shape overrides when hasRoundedRect is set
        if (hasRoundedRectByte != 0)
        {
            shapeTop = perLayerShapes[0];
            shapeMiddle = perLayerShapes[1];
            shapeBottom = stackMode == 0 ? shapeTop : perLayerShapes[31];
        }
    }

    internal static PcbVia? ReadVia(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags);

        var location = ReadCoordPoint(reader);
        var diameter = Coord.FromRaw(reader.ReadInt32());
        var holeSize = Coord.FromRaw(reader.ReadInt32());
        var fromLayer = reader.ReadByte();
        var toLayer = reader.ReadByte();

        var via = PcbVia.Create()
            .At(location.X, location.Y)
            .Diameter(diameter)
            .HoleSize(holeSize)
            .Layers(fromLayer, toLayer)
            .Build();

        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        via.IsLocked = isLocked;
        via.Layer = layer;
        via.IsKeepout = isKeepout;
        via.IsTentingTop = isTentingTop;
        via.IsTentingBottom = isTentingBottom;

        // Read remaining via fields
        var consumed = reader.Position - startPos;
        if (consumed < sanitizedSize)
        {
            reader.Skip(1); // reserved padding byte (0)
            via.ThermalReliefAirGap = Coord.FromRaw(reader.ReadInt32());
            via.ThermalReliefConductors = reader.ReadByte();
            reader.Skip(1); // reserved padding byte (0)
            via.ThermalReliefConductorsWidth = Coord.FromRaw(reader.ReadInt32());
            via.PowerPlaneClearance = Coord.FromRaw(reader.ReadInt32());
            via.PowerPlaneReliefExpansion = Coord.FromRaw(reader.ReadInt32());
            reader.Skip(4); // reserved int (0)
            via.SolderMaskExpansion = Coord.FromRaw(reader.ReadInt32());

            // 8 bytes of post-solder-mask flags (skip)
            reader.Skip(8);
            var solderMaskManualByte = reader.ReadByte();
            via.SolderMaskExpansionManual = solderMaskManualByte == 2;
            reader.Skip(1); // reserved byte (usually 1)
            reader.Skip(2); // reserved short (0)
            reader.Skip(4); // reserved int (0)
            via.Mode = reader.ReadByte(); // diameter stack mode

            // 32 diameter values
            for (var i = 0; i < 32; i++)
                via.Diameters[i] = Coord.FromRaw(reader.ReadInt32());

            reader.Skip(2); // reserved short (15)
            reader.Skip(4); // reserved int (259)

            // Skip any extra bytes beyond what we understand
            consumed = reader.Position - startPos;
            var remaining = sanitizedSize - consumed;
            if (remaining > 0)
                reader.Skip((int)remaining);
        }

        return via;
    }

    internal static PcbTrack? ReadTrack(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags);

        var startX = reader.ReadInt32();
        var startY = reader.ReadInt32();
        var endX = reader.ReadInt32();
        var endY = reader.ReadInt32();
        var width = Coord.FromRaw(reader.ReadInt32());

        // Read the 3 post-core bytes (always present in 36-byte minimum record)
        ushort netIndex = 0;
        byte componentIndex = 0;
        var consumed = reader.Position - startPos;
        if (consumed + 3 <= sanitizedSize)
        {
            netIndex = reader.ReadUInt16(); // SubNet/Net index
            componentIndex = reader.ReadByte(); // Component index
        }

        // Skip optional trailing data (record sizes 41, 45)
        consumed = reader.Position - startPos;
        var remaining = sanitizedSize - consumed;
        if (remaining > 0)
            reader.Skip((int)remaining);

        var track = PcbTrack.Create()
            .From(Coord.FromRaw(startX), Coord.FromRaw(startY))
            .To(Coord.FromRaw(endX), Coord.FromRaw(endY))
            .Width(width)
            .Layer(layer)
            .Build();

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        track.IsLocked = isLocked;
        track.IsTentingTop = isTentingTop;
        track.IsTentingBottom = isTentingBottom;
        track.IsKeepout = isKeepout;

        track.NetIndex = netIndex;
        track.ComponentIndex = componentIndex;

        return track;
    }

    internal static PcbText? ReadText(BinaryFormatReader reader, List<string> wideStrings)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags);

        var corner1 = ReadCoordPoint(reader);
        var height = reader.ReadInt32();
        var strokeFont = reader.ReadInt16();
        var rotation = reader.ReadDouble();
        var mirrored = reader.ReadByte() != 0;
        var strokeWidth = reader.ReadInt32();

        // Extended text fields (record size >= 123)
        var textKind = PcbTextKind.Stroke;
        var fontBold = false;
        var fontItalic = false;
        string? fontName = null;
        int barcodeLRMargin = 0;
        int barcodeTBMargin = 0;
        var fontInverted = false;
        int fontInvertedBorder = 0;
        int wideStringIndex = -1;
        var fontInvertedRect = false;
        int fontInvertedRectWidth = 0;
        int fontInvertedRectHeight = 0;
        byte fontInvertedRectJustification = 0;
        int fontInvertedRectTextOffset = 0;
        short reservedExt1 = 0;
        byte reservedExt2 = 0;
        int reservedExt3 = 0;
        int reservedExt4 = 0;
        byte reservedExt5 = 0;
        byte reservedExt6 = 0;
        int reservedExt7 = 0;
        short reservedExt8 = 0;
        int reservedExt9 = 1;
        int reservedExt10 = 0;
        int reservedExt11 = 0;

        if (sanitizedSize >= 123)
        {
            reservedExt1 = reader.ReadInt16();
            reservedExt2 = reader.ReadByte();
            textKind = (PcbTextKind)reader.ReadByte();
            fontBold = reader.ReadByte() != 0;
            fontItalic = reader.ReadByte() != 0;
            fontName = reader.ReadFontName(); // 64 bytes fixed
            barcodeLRMargin = reader.ReadInt32();
            barcodeTBMargin = reader.ReadInt32();
            reservedExt3 = reader.ReadInt32();
            reservedExt4 = reader.ReadInt32();
            reservedExt5 = reader.ReadByte();
            reservedExt6 = reader.ReadByte();
            reservedExt7 = reader.ReadInt32();
            reservedExt8 = reader.ReadInt16();
            reservedExt9 = reader.ReadInt32();
            reservedExt10 = reader.ReadInt32();
            fontInverted = reader.ReadByte() != 0;
            fontInvertedBorder = reader.ReadInt32();
            wideStringIndex = reader.ReadInt32();
            reservedExt11 = reader.ReadInt32();
            fontInvertedRect = reader.ReadByte() != 0;
            fontInvertedRectWidth = reader.ReadInt32();
            fontInvertedRectHeight = reader.ReadInt32();
            fontInvertedRectJustification = reader.ReadByte();
            fontInvertedRectTextOffset = reader.ReadInt32();
        }

        // Skip any trailing data beyond what we understand
        var consumed = reader.Position - startPos;
        var remaining = sanitizedSize - consumed;
        if (remaining > 0)
            reader.Skip((int)remaining);

        // Read ASCII text
        var asciiText = reader.ReadStringBlock();

        var text = asciiText;
        if (wideStringIndex >= 0 && wideStringIndex < wideStrings.Count)
            text = wideStrings[wideStringIndex];

        if (string.IsNullOrEmpty(text))
            return null;

        var result = PcbText.Create(text)
            .At(corner1.X, corner1.Y)
            .Height(Coord.FromRaw(height))
            .StrokeWidth(Coord.FromRaw(strokeWidth))
            .Rotation(rotation)
            .Mirrored(mirrored)
            .Layer(layer)
            .Build();

        result.StrokeFont = (PcbStrokeFont)strokeFont;
        result.TextKind = textKind;
        result.IsTrueType = textKind == PcbTextKind.TrueType;
        result.FontBold = fontBold;
        result.FontItalic = fontItalic;
        result.FontName = fontName;
        result.IsInverted = fontInverted;
        result.InvertedBorder = Coord.FromRaw(fontInvertedBorder);
        result.UseInvertedRectangle = fontInvertedRect;
        result.InvertedRectWidth = Coord.FromRaw(fontInvertedRectWidth);
        result.InvertedRectHeight = Coord.FromRaw(fontInvertedRectHeight);
        result.InvertedRectJustification = (TextJustification)fontInvertedRectJustification;
        result.InvertedRectTextOffset = Coord.FromRaw(fontInvertedRectTextOffset);
        result.BarcodeLRMargin = Coord.FromRaw(barcodeLRMargin);
        result.BarcodeTBMargin = Coord.FromRaw(barcodeTBMargin);
        result.WideStringIndex = wideStringIndex;

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        result.IsLocked = isLocked;
        result.IsTentingTop = isTentingTop;
        result.IsTentingBottom = isTentingBottom;
        result.IsKeepout = isKeepout;

        return result;
    }

    internal static PcbFill? ReadFill(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags);

        var corner1 = ReadCoordPoint(reader);
        var corner2 = ReadCoordPoint(reader);
        var rotation = reader.ReadDouble();

        // Skip any trailing data (record sizes 41, 46)
        var consumed = reader.Position - startPos;
        var remaining = sanitizedSize - consumed;
        if (remaining > 0)
            reader.Skip((int)remaining);

        var fill = PcbFill.Create()
            .From(corner1.X, corner1.Y)
            .To(corner2.X, corner2.Y)
            .Rotation(rotation)
            .OnLayer(layer)
            .Build();

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        fill.IsLocked = isLocked;
        fill.IsTentingTop = isTentingTop;
        fill.IsTentingBottom = isTentingBottom;
        fill.IsKeepout = isKeepout;

        return fill;
    }

    internal static PcbRegion? ReadRegion(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags);

        // Structure: uint32 prefix + byte prefix + nested parameter block + outline vertices
        reader.Skip(4); // reserved uint32 prefix
        reader.Skip(1); // reserved byte prefix

        // Read nested C-string parameter block
        var parameters = ReadParameterBlock(reader);

        // Read outline vertices (stored as doubles in Altium format)
        var vertexCount = reader.ReadUInt32();
        var kind = 0;
        if (parameters.TryGetValue("KIND", out var kindStr))
            int.TryParse(kindStr, out kind);

        var region = PcbRegion.Create()
            .OnLayer(layer)
            .Kind(kind);

        for (var i = 0; i < vertexCount; i++)
        {
            var x = Coord.FromRaw((int)reader.ReadDouble());
            var y = Coord.FromRaw((int)reader.ReadDouble());
            region.AddPoint(x, y);
        }

        // Skip trailing data
        var consumed = reader.Position - startPos;
        var remaining = sanitizedSize - consumed;
        if (remaining > 0)
            reader.Skip((int)remaining);

        var result = region.Build();

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        result.IsLocked = isLocked;
        result.IsTentingTop = isTentingTop;
        result.IsTentingBottom = isTentingBottom;
        result.IsKeepout = isKeepout;

        // Extract typed properties from parameter block
        if (parameters.TryGetValue("NET", out var net))
            result.Net = net;
        if (parameters.TryGetValue("UNIQUEID", out var uid))
            result.UniqueId = uid;
        if (parameters.TryGetValue("NAME", out var name))
            result.Name = name;

        // Preserve any additional parameters not modeled as typed properties
        result.AdditionalParameters = ExtractAdditionalParameters(parameters,
            ["KIND", "NET", "UNIQUEID", "NAME"]);

        return result;
    }

    internal static PcbComponentBody? ReadComponentBody(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags);

        // Structure: uint32 prefix + byte prefix + nested parameter block + outline vertices
        reader.Skip(4); // reserved uint32 prefix
        reader.Skip(1); // reserved byte prefix

        // Read nested C-string parameter block (contains 3D model references etc.)
        var parameters = ReadParameterBlock(reader);

        // Read outline vertices (stored as doubles in Altium format)
        var vertexCount = reader.ReadUInt32();
        var body = PcbComponentBody.Create();

        for (var i = 0; i < vertexCount; i++)
        {
            var x = Coord.FromRaw((int)reader.ReadDouble());
            var y = Coord.FromRaw((int)reader.ReadDouble());
            body.AddPoint(x, y);
        }

        // Skip trailing data
        var consumed = reader.Position - startPos;
        var remaining = sanitizedSize - consumed;
        if (remaining > 0)
            reader.Skip((int)remaining);

        var result = body.Build();

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        result.IsLocked = isLocked;
        result.IsTentingTop = isTentingTop;
        result.IsTentingBottom = isTentingBottom;
        result.IsKeepout = isKeepout;

        // Extract typed properties from parameter block
        if (parameters.TryGetValue("V7_LAYER", out var v7Layer))
            result.LayerName = v7Layer;
        if (parameters.TryGetValue("NAME", out var name))
            result.Name = name;
        if (parameters.TryGetValue("KIND", out var kindStr) && int.TryParse(kindStr, out var kind))
            result.Kind = kind;
        if (parameters.TryGetValue("SUBPOLYINDEX", out var subPoly) && int.TryParse(subPoly, out var subPolyVal))
            result.SubPolyIndex = subPolyVal;
        if (parameters.TryGetValue("UNIONINDEX", out var unionIdx) && int.TryParse(unionIdx, out var unionIdxVal))
            result.UnionIndex = unionIdxVal;
        if (parameters.TryGetValue("ARCRESOLUTION", out var arcRes) && double.TryParse(arcRes, System.Globalization.CultureInfo.InvariantCulture, out var arcResVal))
            result.ArcResolution = arcResVal;
        if (parameters.TryGetValue("ISSHAPEBASED", out var isShapeBased))
            result.IsShapeBased = string.Equals(isShapeBased, "TRUE", StringComparison.OrdinalIgnoreCase);
        if (parameters.TryGetValue("CAVITYHEIGHT", out var cavHeight) && int.TryParse(cavHeight, out var cavHeightVal))
            result.CavityHeight = Coord.FromRaw(cavHeightVal);
        if (parameters.TryGetValue("STANDOFFHEIGHT", out var standoff) && int.TryParse(standoff, out var standoffVal))
            result.StandoffHeight = Coord.FromRaw(standoffVal);
        if (parameters.TryGetValue("OVERALLHEIGHT", out var overall) && int.TryParse(overall, out var overallVal))
            result.OverallHeight = Coord.FromRaw(overallVal);
        if (parameters.TryGetValue("BODYCOLOR3D", out var bodyColor) && int.TryParse(bodyColor, out var bodyColorVal))
            result.BodyColor3D = bodyColorVal;
        if (parameters.TryGetValue("BODYOPACITY3D", out var opacity) && double.TryParse(opacity, System.Globalization.CultureInfo.InvariantCulture, out var opacityVal))
            result.BodyOpacity3D = opacityVal;
        if (parameters.TryGetValue("MODELID", out var modelId))
            result.ModelId = modelId;
        if (parameters.TryGetValue("MODEL.EMBED", out var modelEmbed))
            result.ModelEmbed = string.Equals(modelEmbed, "TRUE", StringComparison.OrdinalIgnoreCase);
        if (parameters.TryGetValue("MODEL.2D.X", out var m2dx) && int.TryParse(m2dx, out var m2dxVal))
            result.Model2DLocation = new CoordPoint(Coord.FromRaw(m2dxVal),
                parameters.TryGetValue("MODEL.2D.Y", out var m2dy) && int.TryParse(m2dy, out var m2dyVal)
                    ? Coord.FromRaw(m2dyVal) : Coord.FromRaw(0));
        if (parameters.TryGetValue("MODEL.2D.ROTATION", out var m2dRot) && double.TryParse(m2dRot, System.Globalization.CultureInfo.InvariantCulture, out var m2dRotVal))
            result.Model2DRotation = m2dRotVal;
        if (parameters.TryGetValue("MODEL.3D.ROTX", out var m3dRotX) && double.TryParse(m3dRotX, System.Globalization.CultureInfo.InvariantCulture, out var m3dRotXVal))
            result.Model3DRotX = m3dRotXVal;
        if (parameters.TryGetValue("MODEL.3D.ROTY", out var m3dRotY) && double.TryParse(m3dRotY, System.Globalization.CultureInfo.InvariantCulture, out var m3dRotYVal))
            result.Model3DRotY = m3dRotYVal;
        if (parameters.TryGetValue("MODEL.3D.ROTZ", out var m3dRotZ) && double.TryParse(m3dRotZ, System.Globalization.CultureInfo.InvariantCulture, out var m3dRotZVal))
            result.Model3DRotZ = m3dRotZVal;
        if (parameters.TryGetValue("MODEL.3D.DZ", out var m3dDz) && int.TryParse(m3dDz, out var m3dDzVal))
            result.Model3DDz = Coord.FromRaw(m3dDzVal);
        if (parameters.TryGetValue("MODEL.CHECKSUM", out var modelCs) && int.TryParse(modelCs, out var modelCsVal))
            result.ModelChecksum = modelCsVal;
        if (parameters.TryGetValue("MODEL.NAME", out var modelName))
            result.ModelName = modelName;
        if (parameters.TryGetValue("MODEL.MODELTYPE", out var modelType) && int.TryParse(modelType, out var modelTypeVal))
            result.ModelType = modelTypeVal;
        if (parameters.TryGetValue("MODEL.MODELSOURCE", out var modelSource))
            result.ModelSource = modelSource;
        if (parameters.TryGetValue("BODYPROJECTION", out var bodyProj) && int.TryParse(bodyProj, out var bodyProjVal))
            result.BodyProjection = bodyProjVal;
        if (parameters.TryGetValue("IDENTIFIER", out var identifier))
            result.Identifier = identifier;
        if (parameters.TryGetValue("TEXTURE", out var texture))
            result.Texture = texture;

        // Preserve any additional parameters not modeled as typed properties
        result.AdditionalParameters = ExtractAdditionalParameters(parameters,
        [
            "V7_LAYER", "NAME", "KIND", "SUBPOLYINDEX", "UNIONINDEX", "ARCRESOLUTION",
            "ISSHAPEBASED", "CAVITYHEIGHT", "STANDOFFHEIGHT", "OVERALLHEIGHT",
            "BODYCOLOR3D", "BODYOPACITY3D", "BODYPROJECTION",
            "MODELID", "MODEL.EMBED", "MODEL.2D.X", "MODEL.2D.Y", "MODEL.2D.ROTATION",
            "MODEL.3D.ROTX", "MODEL.3D.ROTY", "MODEL.3D.ROTZ", "MODEL.3D.DZ",
            "MODEL.CHECKSUM", "MODEL.NAME", "MODEL.MODELTYPE", "MODEL.MODELSOURCE",
            "IDENTIFIER", "TEXTURE"
        ]);

        return result;
    }
}
