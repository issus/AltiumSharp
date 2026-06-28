using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using OriginalCircuit.Altium.Serialization.Compound;
using System.Buffers;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using PadShape = OriginalCircuit.Altium.Models.Pcb.PadShape;
using PadHoleType = OriginalCircuit.Altium.Models.Pcb.PadHoleType;

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
            await using var accessor = await CompoundFileAccessor.OpenAsync(path, writable: false, cancellationToken).ConfigureAwait(false);
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

        // PcbLib FileHeader is a fixed 53-byte, two-block layout (docs/decompile/fileheaders.md §1):
        //   [u32 len][b len][versionText "PCB 6.0 Binary Library File"]   (Reserved constant)
        //   [double 5.01]                                                 (Reserved constant, no prefix)
        //   [u32 len][b len][uniqueId 8×A-Z]                              (Identity — the only per-library datum)
        // Everything but the 8-char UniqueId is constant, so we model the whole header from UniqueId.
        var versionBlockLen = reader.ReadInt32();
        if (versionBlockLen <= 0)
            return;
        var versionLen = reader.ReadByte();
        reader.Skip(versionLen);            // versionText — Reserved constant, not retained
        if (!reader.HasMore)
            return;
        reader.Skip(8);                     // 5.01 format-version double — Reserved constant

        if (!reader.HasMore)
            return;
        var idBlockLen = reader.ReadInt32();
        if (idBlockLen <= 0)
            return;
        var idLen = reader.ReadByte();
        if (idLen > 0)
        {
            var idBytes = new byte[idLen];
            reader.ReadExact(idBytes);
            library.UniqueId = AltiumEncoding.Windows1252.GetString(idBytes);
        }
    }

    private static void PreserveAdditionalRootStreams(CompoundFileAccessor accessor, PcbLibrary library)
    {
        library.AdditionalRootStreams = new Dictionary<string, byte[]>();

        // FileVersionInfo is now modeled as a typed record.
        var fviStorage = accessor.TryGetStorage("FileVersionInfo");
        if (fviStorage != null && fviStorage.TryGetStream("Data", out var fviData))
            library.FileVersionInfo = ParseFileVersionInfo(fviData.GetData());
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

        // Read library parameters (header info). The header is an ordered parameter list that
        // contains duplicate keys (repeated RECORD=Board markers), so capture the ordered form
        // for faithful round-trip in addition to the flattened dictionary view.
        var libraryParams = ReadParameterBlock(reader, out var rawLibraryParams);
        library.LibraryParameters = libraryParams;
        library.LibraryParametersOrdered = ParseParametersOrdered(rawLibraryParams);

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

        // Parse the modeled library metadata storages into typed records (so they round-trip and can be
        // authored from scratch) instead of capturing them as opaque bytes.
        ReadLayerKindMapping(libraryStorage, library);
        ReadPadViaLibrary(libraryStorage, library);
        ReadComponentParamsToc(libraryStorage, library);

        // Preserve any remaining additional library-level streams for round-trip fidelity
        library.AdditionalLibraryStreams = new Dictionary<string, byte[]>();
        var knownLibraryChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Header", "Data", "Models", "LayerKindMapping", "PadViaLibrary", "ComponentParamsTOC" };
        foreach (var entry in libraryStorage.EnumerateEntries())
        {
            if (knownLibraryChildren.Contains(entry.Name))
                continue;

            // Textures / ModelsNoEmbed are reproduced as explicit empty storages (see
            // PcbLibrary.EmptyLibrarySubStorages). Skip capturing them while empty; if a file ever has
            // non-empty content, drop the name so it falls back to verbatim catch-all capture below.
            if (library.EmptyLibrarySubStorages.Contains(entry.Name))
            {
                if (entry.IsStorage && IsEmptyLibrarySubStorage(entry.AsStorage()))
                    continue;
                library.EmptyLibrarySubStorages.Remove(entry.Name);
            }

            if (entry.IsStream)
            {
                library.AdditionalLibraryStreams[entry.Name] = entry.AsStream().GetData();
            }
            else
            {
                // Preserve sub-storage streams (e.g., ComponentParamsTOC/Data)
                var subStorage = entry.AsStorage();
                foreach (var subEntry in subStorage.EnumerateEntries())
                {
                    if (subEntry.IsStream)
                    {
                        library.AdditionalLibraryStreams[$"{entry.Name}/{subEntry.Name}"] = subEntry.AsStream().GetData();
                    }
                }
            }
        }

        // Parse 3D model streams (STEP data + metadata)
        if (libraryStorage.TryGetStorage("Models", out var modelsStorage))
        {
            ReadModels(modelsStorage, library);
        }
    }

    private static bool IsEmptyLibrarySubStorage(CompoundStorage storage)
        => storage.TryGetStream("Data") is not { } data || data.GetData().Length == 0;

    private static void ReadModels(CompoundStorage modelsStorage, PcbLibrary library)
    {
        // Data stream: parameter blocks with model metadata (EMBED, MODELSOURCE, ID, ROTX, ROTY,
        // ROTZ, DZ, CHECKSUM, NAME). STEP payloads live in numbered streams (0, 1, ...), zlib
        // compressed. The parse is shared with PcbDoc's root Models storage (PcbModel.ParseModels).
        var dataBytes = modelsStorage.TryGetStream("Data", out var dataStream)
            ? dataStream.GetData()
            : null;

        var models = PcbModel.ParseModels(dataBytes,
            i => modelsStorage.TryGetStream(i.ToString(CultureInfo.InvariantCulture), out var modelStream)
                ? modelStream.GetData()
                : null);

        library.Models.AddRange(models);
    }

    private static void ReadLayerKindMapping(CompoundStorage libraryStorage, PcbLibrary library)
    {
        // Library/LayerKindMapping/Data = [u32 text byte-count][UTF-16LE FormatVersion + NUL][8 reserved].
        if (!libraryStorage.TryGetStorage("LayerKindMapping", out var storage)) return;
        if (!storage.TryGetStream("Data", out var stream)) return;
        var data = stream.GetData();
        if (data.Length < 4) return;
        var textLen = BitConverter.ToInt32(data, 0);
        if (textLen < 0 || 4 + textLen > data.Length) return;
        library.LayerKindMapping = ParseLayerKindMapping(data);
    }

    // LayerKindMapping/Data = [u32 text-byte-count][UTF-16LE FormatVersion + NUL]
    //                         [u32 signature][u32 count][count × (u32 layerId, u32 kind)].
    // In PcbLib the tail is signature=0,count=0 (the 8 zero bytes); PcbDoc carries a real table.
    internal static PcbLayerKindMapping ParseLayerKindMapping(byte[] data)
    {
        var lkm = new PcbLayerKindMapping();
        if (data.Length < 4) return lkm;
        var textLen = BitConverter.ToInt32(data, 0);
        if (textLen < 0 || 4 + textLen > data.Length) return lkm;
        lkm.FormatVersion = System.Text.Encoding.Unicode.GetString(data, 4, textLen).TrimEnd('\0');
        var pos = 4 + textLen;
        if (pos + 8 <= data.Length)
        {
            lkm.Signature = BitConverter.ToUInt32(data, pos);
            var count = BitConverter.ToUInt32(data, pos + 4);
            pos += 8;
            for (var i = 0; i < count && pos + 8 <= data.Length; i++, pos += 8)
                lkm.Entries.Add(new PcbLayerKindEntry(
                    BitConverter.ToUInt32(data, pos), BitConverter.ToUInt32(data, pos + 4)));
        }
        return lkm;
    }

    private static void ReadPadViaLibrary(CompoundStorage libraryStorage, PcbLibrary library)
    {
        var pvl = ParsePadViaLibrary(libraryStorage, "PadViaLibrary");
        if (pvl != null) library.PadViaLibrary = pvl;
    }

    // PadViaLibrary[Cache]/Data = [u32 byte-count][CP1252 |KEY=VALUE| block + NUL].
    internal static PcbPadViaLibrary? ParsePadViaLibrary(CompoundStorage parentStorage, string storageName)
    {
        if (!parentStorage.TryGetStorage(storageName, out var storage)) return null;
        if (!storage.TryGetStream("Data", out var stream)) return null;
        var data = stream.GetData();
        if (data.Length < 4) return null;
        var len = BitConverter.ToInt32(data, 0);
        if (len <= 0 || 4 + len > data.Length) return null;
        var text = AltiumEncoding.Windows1252.GetString(data, 4, len).TrimEnd('\0');
        var ordered = ParseParametersOrdered(text);
        var pvl = new PcbPadViaLibrary { OrderedParameters = ordered };
        if (storage.TryGetStream("Header", out var hdr))
        {
            var hb = hdr.GetData();
            if (hb.Length >= 4) pvl.HeaderCount = BitConverter.ToInt32(hb, 0);
        }
        foreach (var kvp in ordered)
        {
            if (kvp.Key.Equals("PADVIALIBRARY.LIBRARYID", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(kvp.Value, out var g)) pvl.LibraryId = g;
            else if (kvp.Key.Equals("PADVIALIBRARY.LIBRARYNAME", StringComparison.OrdinalIgnoreCase)) pvl.LibraryName = kvp.Value;
            else if (kvp.Key.Equals("PADVIALIBRARY.DISPLAYUNITS", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(kvp.Value, out var u)) pvl.DisplayUnits = u;
        }
        // Any bytes after the parameter block are an opaque binary template cache (PadViaLibraryCache).
        var tailStart = 4 + len;
        if (data.Length > tailStart) pvl.TemplateCache = data.AsSpan(tailStart).ToArray();
        return pvl;
    }

    internal static void ReadPrimitiveGuids(CompoundStorage parentStorage, PcbComponent component)
    {
        // PrimitiveGuids/Data = N x [u32 type_id][u32 index][16-byte GUID bytes_le]; Header = [u32 count].
        if (!parentStorage.TryGetStorage("PrimitiveGuids", out var storage)) return;
        if (!storage.TryGetStream("Data", out var stream)) return;
        var data = stream.GetData();
        for (var pos = 0; pos + 24 <= data.Length; pos += 24)
        {
            component.PrimitiveGuids.Add(new PcbPrimitiveGuid
            {
                TypeId = BitConverter.ToUInt32(data, pos),
                Index = BitConverter.ToUInt32(data, pos + 4),
                Guid = new Guid(data.AsSpan(pos + 8, 16)),
            });
        }
    }

    // Parses a FileVersionInfo/Data stream ([u32 len][|KEY=VALUE| block]) into a typed record.
    internal static PcbFileVersionInfo ParseFileVersionInfo(byte[]? data)
    {
        var info = new PcbFileVersionInfo();
        if (data == null || data.Length < 4) return info;
        var len = BitConverter.ToInt32(data, 0);
        if (len <= 0 || 4 + len > data.Length) return info;
        var text = AltiumEncoding.Windows1252.GetString(data, 4, len).TrimEnd('\0');
        info.OrderedParameters = ParseParametersOrdered(text);
        foreach (var kvp in info.OrderedParameters) info.Parameters[kvp.Key] = kvp.Value;
        info.Present = true;
        return info;
    }

    internal static void ReadPrimitiveUniqueIds(CompoundStorage parentStorage, PcbComponent component)
    {
        if (!parentStorage.TryGetStorage("UniqueIDPrimitiveInformation", out var storage)) return;
        if (!storage.TryGetStream("Data", out var stream)) return;
        component.PrimitiveUniqueIds.AddRange(ParsePrimitiveUniqueIdRecords(stream.GetData()));
    }

    // Parses the length-prefixed |KEY=VALUE| records of a UniqueIDPrimitiveInformation/Data stream.
    internal static IEnumerable<PcbPrimitiveUniqueId> ParsePrimitiveUniqueIdRecords(byte[] data)
    {
        var pos = 0;
        while (pos + 4 <= data.Length)
        {
            var len = BitConverter.ToInt32(data, pos); pos += 4;
            if (len <= 0 || pos + len > data.Length) break;
            var text = AltiumEncoding.Windows1252.GetString(data, pos, len).TrimEnd('\0');
            pos += len;
            var ordered = ParseParametersOrdered(text);
            var rec = new PcbPrimitiveUniqueId { OrderedParameters = ordered };
            foreach (var kvp in ordered)
            {
                if (kvp.Key.Equals("PRIMITIVEINDEX", StringComparison.OrdinalIgnoreCase) && int.TryParse(kvp.Value, out var pi)) rec.PrimitiveIndex = pi;
                else if (kvp.Key.Equals("PRIMITIVEOBJECTID", StringComparison.OrdinalIgnoreCase)) rec.ObjectId = kvp.Value;
                else if (kvp.Key.Equals("UNIQUEID", StringComparison.OrdinalIgnoreCase)) rec.UniqueId = kvp.Value;
            }
            yield return rec;
        }
    }

    private static void ReadComponentParamsToc(CompoundStorage libraryStorage, PcbLibrary library)
    {
        // Library/ComponentParamsTOC/Data = [u32 byte-count][CP1252 "Name=..|Pad Count=..|Height=..|
        // Description=..\r\n" per footprint, concatenated + NUL].
        if (!libraryStorage.TryGetStorage("ComponentParamsTOC", out var storage)) return;
        if (!storage.TryGetStream("Data", out var stream)) return;
        var data = stream.GetData();
        if (data.Length < 4) return;
        var len = BitConverter.ToInt32(data, 0);
        if (len <= 0 || 4 + len > data.Length) return;
        var text = AltiumEncoding.Windows1252.GetString(data, 4, len).TrimEnd('\0');
        foreach (var line in text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var entry = new PcbComponentParamsTocEntry();
            foreach (var part in line.Split('|'))
            {
                var eq = part.IndexOf('=');
                if (eq < 0) continue;
                var key = part[..eq];
                var val = part[(eq + 1)..];
                if (key.Equals("Name", StringComparison.OrdinalIgnoreCase)) entry.Name = val;
                else if (key.Equals("Pad Count", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out var pc)) entry.PadCount = pc;
                else if (key.Equals("Height", StringComparison.OrdinalIgnoreCase)) entry.Height = val;
                else if (key.Equals("Description", StringComparison.OrdinalIgnoreCase)) entry.Description = val;
            }
            library.ComponentParamsToc.Add(entry);
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

        // Parse the modeled identity tables into typed records.
        ReadPrimitiveGuids(storage, component);
        ReadPrimitiveUniqueIds(storage, component);

        // Preserve any remaining component-level streams.
        component.AdditionalStreams = new Dictionary<string, byte[]>();
        var knownChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Header", "Parameters", "WideStrings", "Data", "PrimitiveGuids", "UniqueIDPrimitiveInformation" };
        foreach (var entry in storage.EnumerateEntries())
        {
            if (knownChildren.Contains(entry.Name))
                continue;
            if (entry.IsStream)
            {
                component.AdditionalStreams[entry.Name] = entry.AsStream().GetData();
            }
            else
            {
                var subStorage = entry.AsStorage();
                foreach (var subEntry in subStorage.EnumerateEntries())
                {
                    if (subEntry.IsStream)
                    {
                        component.AdditionalStreams[$"{entry.Name}/{subEntry.Name}"] = subEntry.AsStream().GetData();
                    }
                }
            }
        }

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
                        // Unknown primitive: skip one size-prefixed block and record it. For
                        // multi-block primitives this may misalign the stream, so later primitives
                        // in this footprint could be affected - surface it rather than failing silently.
                        _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning,
                            $"Unknown PCB primitive object id {(int)objectId} in footprint '{component.Name}'; skipped one block.",
                            sectionKey));
                        reader.SkipBlock();
                        break;
                }
            }
        }

        return component;
    }

    internal static CompoundStream? GetChildStream(CompoundStorage storage, string name)
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
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
        {
            rawString = string.Empty;
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // PCB parameter blocks are C-strings (null-terminated, no length prefix). Decode directly
        // from the read buffer (no intermediate copy); the common small case stays on the stack and
        // larger blocks use a pooled buffer rather than a fresh allocation per record.
        string paramString;
        if (sanitizedSize <= 512)
        {
            Span<byte> stackBuffer = stackalloc byte[sanitizedSize];
            reader.ReadExact(stackBuffer);
            var nullIndex = stackBuffer.IndexOf((byte)0);
            var length = nullIndex >= 0 ? nullIndex : sanitizedSize;
            paramString = AltiumEncoding.Windows1252.GetString(stackBuffer[..length]);
        }
        else
        {
            var rented = ArrayPool<byte>.Shared.Rent(sanitizedSize);
            try
            {
                var span = rented.AsSpan(0, sanitizedSize);
                reader.ReadExact(span);
                var nullIndex = span.IndexOf((byte)0);
                var length = nullIndex >= 0 ? nullIndex : sanitizedSize;
                paramString = AltiumEncoding.Windows1252.GetString(span[..length]);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

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

    /// <summary>
    /// Parses a pipe-delimited parameter string into an ordered list, preserving key order
    /// and duplicate keys (unlike <see cref="ParseParameters"/> which flattens into a map).
    /// </summary>
    internal static List<KeyValuePair<string, string>> ParseParametersOrdered(string paramString)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrEmpty(paramString))
            return result;

        var span = paramString.AsSpan();
        var start = 0;
        while (start < span.Length)
        {
            if (span[start] == '|')
                start++;
            if (start >= span.Length)
                break;

            var equalsIndex = span.Slice(start).IndexOf('=');
            if (equalsIndex < 0)
                break;

            var key = span.Slice(start, equalsIndex).ToString();
            start += equalsIndex + 1;

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

            result.Add(new KeyValuePair<string, string>(key, value));
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
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            result = Coord.FromRaw(intValue);
            return true;
        }

        // Try parse with unit suffix
        var span = value.AsSpan();
        if (span.EndsWith("mil", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(span.Slice(0, span.Length - 3), NumberStyles.Float, CultureInfo.InvariantCulture, out var mils))
            {
                result = Coord.FromMils(mils);
                return true;
            }
        }
        else if (span.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(span.Slice(0, span.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var mm))
            {
                result = Coord.FromMm(mm);
                return true;
            }
        }

        return false;
    }

    internal static List<string> ReadWideStrings(CompoundStorage storage)
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
            if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var codePoint))
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
        => ReadCommonPrimitiveData(reader, out layer, out flags, out componentIndex, out _);

    internal static void ReadCommonPrimitiveData(BinaryFormatReader reader, out byte layer, out ushort flags, out int componentIndex, out ushort netIndex)
        => ReadCommonPrimitiveData(reader, out layer, out flags, out componentIndex, out netIndex, out _);

    internal static void ReadCommonPrimitiveData(BinaryFormatReader reader, out byte layer, out ushort flags, out int componentIndex, out ushort netIndex, out ushort polygonIndex)
    {
        layer = reader.ReadByte();
        flags = reader.ReadUInt16();

        // 10 bytes: uint16 netIndex, uint16 polygonIndex, uint16 componentIndex, uint32 reserved
        netIndex = reader.ReadUInt16(); // net index (0xFFFF = no net)
        polygonIndex = reader.ReadUInt16(); // polygon index (0xFFFF = none, 0 for regions)
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
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
            return null;

        var sr = reader.ReadBytes(sanitizedSize);
        bool Has(int off, int width) => off >= 0 && off + width <= sr.Length;
        byte B(int off) => Has(off, 1) ? sr[off] : (byte)0;
        int I32(int off) => Has(off, 4) ? BitConverter.ToInt32(sr, off) : 0;
        double Dbl(int off) => Has(off, 8) ? BitConverter.ToDouble(sr, off) : 0.0;
        ushort U16(int off) => Has(off, 2) ? BitConverter.ToUInt16(sr, off) : (ushort)0xFFFF;

        var layer = B(0);
        var flags = (ushort)(B(1) | (B(2) << 8));

        var arc = PcbArc.Create()
            .At(Coord.FromRaw(I32(13)), Coord.FromRaw(I32(17)))
            .Radius(Coord.FromRaw(I32(21)))
            .Angles(Dbl(25), Dbl(33))
            .Width(Coord.FromRaw(I32(41)))
            .Layer(layer)
            .Build();

        arc.NetIndex = U16(3);                                  // 3-4 net index
        var arcComp = U16(7);                                   // 7-8 component index
        arc.ComponentIndex = arcComp == 0xFFFF ? -1 : arcComp;
        arc.SolderMaskExpansion = Coord.FromRaw(I32(47)); // 47-50
        arc.KeepoutRestrictions = B(56);                  // 56

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

        // SubRecords 2 and 3 (Pascal strings, usually empty and "|&|0") and SubRecord 4 (empty).
        // Captured so non-default values round-trip; defaults applied for pads built from scratch.
        var subrecord2 = reader.ReadStringBlock();
        var netString = reader.ReadStringBlock();
        reader.SkipBlock();

        var size = reader.ReadInt32();
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;
        ReadCommonPrimitiveData(reader, out var layer, out var flags, out var componentIndex, out var netIndex);

        // Read main block fields
        var location = ReadCoordPoint(reader);
        var sizeTop = ReadCoordPoint(reader);
        var sizeMiddle = ReadCoordPoint(reader);
        var sizeBottom = ReadCoordPoint(reader);
        var holeSize = Coord.FromRaw(reader.ReadInt32());
        var shapeTop = reader.ReadByte();
        var shapeMiddle = reader.ReadByte();
        var shapeBottom = reader.ReadByte();
        // Capture the original main-block shape bytes before ReadPadSizeShapeBlock may override
        // shapeTop/Middle/Bottom from the per-layer shapes (for rounded-rect/oblong pads).
        var originalMainShapes = new[] { shapeTop, shapeMiddle, shapeBottom };
        var rotation = reader.ReadDouble();
        var isPlated = reader.ReadByte() != 0;

        // Read extended main block fields (power plane, masks, mask modes, jumper, tolerances)
        ReadPadExtendedFields(reader, startPos, sanitizedSize,
            out var stackMode, out var powerPlaneConnectStyle,
            out var reliefAirGap, out var reliefConductorWidth, out var reliefEntries,
            out var powerPlaneClearance, out var powerPlaneReliefExpansion,
            out var pasteMaskExpansion, out var solderMaskExpansion,
            out var pasteMaskMode, out var solderMaskMode,
            out var jumperId, out var holePositiveTolerance, out var holeNegativeTolerance,
            out var rawExtendedTail);

        // Read size/shape block (596 bytes for extended pad data)
        ReadPadSizeShapeBlock(reader, stackMode, ref shapeTop, ref shapeMiddle, ref shapeBottom,
            out var layerXSizes, out var layerYSizes, out var internalLayerShapes,
            out var holeShapeByte, out var holeSlotLength, out var holeRotation,
            out var offsetX, out var offsetY, out var hasRoundedRectByte,
            out var perLayerShapes, out var perLayerCornerRadii, out var hasSizeShapeBlock,
            out var fullStackEntries);

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
        pad.NetIndex = netIndex;
        pad.PadSubrecord2 = subrecord2;
        pad.PadNetString = netString;
        pad.Sr5Length = sanitizedSize;
        pad.SizeMiddle = sizeMiddle;
        pad.SizeBottom = sizeBottom;
        pad.ShapeMiddle = (PadShape)shapeMiddle;
        pad.ShapeBottom = (PadShape)shapeBottom;
        pad.PasteMaskExpansion = Coord.FromRaw(pasteMaskExpansion);
        pad.SolderMaskExpansion = Coord.FromRaw(solderMaskExpansion);
        pad.PasteMaskExpansionMode = pasteMaskMode;
        pad.SolderMaskExpansionMode = solderMaskMode;
        pad.HolePositiveTolerance = Coord.FromRaw(holePositiveTolerance);
        pad.HoleNegativeTolerance = Coord.FromRaw(holeNegativeTolerance);
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

        Array.Copy(layerXSizes, pad.LayerXSizes, Math.Min(layerXSizes.Length, pad.LayerXSizes.Length));
        Array.Copy(layerYSizes, pad.LayerYSizes, Math.Min(layerYSizes.Length, pad.LayerYSizes.Length));
        Array.Copy(internalLayerShapes, pad.InternalLayerShapes, Math.Min(internalLayerShapes.Length, pad.InternalLayerShapes.Length));
        pad.HoleType = (PadHoleType)holeShapeByte;
        pad.HoleSlotLength = holeSlotLength;
        pad.HoleRotation = holeRotation;
        Array.Copy(offsetX, pad.OffsetXFromHoleCenter, Math.Min(offsetX.Length, pad.OffsetXFromHoleCenter.Length));
        Array.Copy(offsetY, pad.OffsetYFromHoleCenter, Math.Min(offsetY.Length, pad.OffsetYFromHoleCenter.Length));
        pad.HasRoundedRectByte = hasRoundedRectByte;
        Array.Copy(perLayerShapes, pad.PerLayerShapes, Math.Min(perLayerShapes.Length, pad.PerLayerShapes.Length));
        Array.Copy(perLayerCornerRadii, pad.PerLayerCornerRadii, Math.Min(perLayerCornerRadii.Length, pad.PerLayerCornerRadii.Length));
        // The real rounded-rectangle corner radius lives per-layer in the size/shape block; the legacy
        // single CornerRadiusPercentage is not stored separately. When the per-layer overrides are
        // authoritative, surface the top-copper entry (index 0) as CornerRadiusPercentage — mirroring
        // how ShapeTop is taken from PerLayerShapes[0]. Without this the property always reports its 50%
        // default regardless of the pad's actual radius.
        if (hasRoundedRectByte != 0)
            pad.CornerRadiusPercentage = perLayerCornerRadii[0];
        pad.HasSizeShapeBlock = hasSizeShapeBlock;
        pad.FullStackEntries.AddRange(fullStackEntries);
        if (rawExtendedTail.Length > 0)
        {
            // Fully model the extended tail (no raw replay): the two identity GUIDs, the thermal/mask
            // cache-validity bytes, and the reserved marker. Everything else is a modeled field
            // (overlaid above) or a constant/derived value reproduced by BuildPadExtendedTail.
            byte TB(int sr5) => sr5 - PadExtendedStart < rawExtendedTail.Length ? rawExtendedTail[sr5 - PadExtendedStart] : (byte)0;
            int TI32(int sr5) => sr5 - PadExtendedStart + 4 <= rawExtendedTail.Length
                ? BitConverter.ToInt32(rawExtendedTail, sr5 - PadExtendedStart) : 0;
            Guid TG(int sr5) => (sr5 - PadExtendedStart + 16 <= rawExtendedTail.Length)
                ? new Guid(rawExtendedTail.AsSpan(sr5 - PadExtendedStart, 16)) : Guid.Empty;
            pad.IdentityGuid = TG(126);                                  // GUID-A (per-pad)
            pad.IdentityGuidB = TG(142);                                 // GUID-B (footprint/stack)
            pad.CachePlaneConnectionValid = TB(96);
            pad.CacheReliefConductorWidthValid = TB(97);
            pad.CacheReliefEntriesValid = TB(98);
            pad.CacheReliefAirGapValid = TB(99);
            pad.CachePowerPlaneReliefExpansionValid = TB(100);
            pad.CachePasteMaskExpansionValid = TB(103);
            pad.CacheSolderMaskExpansionValid = TB(104);
            pad.SolderMaskCache = TI32(121);
            // Offset 172 is derived from the SubRecord-5 length (pad-record format), not captured.
            pad.ReservedMarker185 = TB(185);
        }
        // When per-layer overrides replaced the typed shapes, keep the source's base main-block shapes.
        if (hasRoundedRectByte != 0)
            pad.MainBlockBaseShapes = (originalMainShapes[0], originalMainShapes[1], originalMainShapes[2]);
        return pad;
    }

    // Pad SubRecord-5 extended tail (offsets relative to the start of the SubRecord).
    // Layout verified against the Altium binary format: the thermal-relief / mask /
    // tolerance fields live at fixed offsets, interleaved with reserved and pad-cache bytes.
    private const int PadExtendedStart = 61;             // first byte after the basic geometry
    private const int PadHolePositiveToleranceOffset = 162;
    private const int PadHoleNegativeToleranceOffset = 166;

    private static void ReadPadExtendedFields(BinaryFormatReader reader, long startPos, long blockSize,
        out int stackMode, out byte powerPlaneConnectStyle,
        out int reliefAirGap, out int reliefConductorWidth, out short reliefEntries,
        out int powerPlaneClearance, out int powerPlaneReliefExpansion,
        out int pasteMaskExpansion, out int solderMaskExpansion,
        out byte pasteMaskMode, out byte solderMaskMode,
        out short jumperId,
        out int holePositiveTolerance, out int holeNegativeTolerance,
        out byte[] rawExtendedTail)
    {
        stackMode = 0;
        powerPlaneConnectStyle = 0;
        reliefAirGap = 0; reliefConductorWidth = 0; reliefEntries = 4;
        powerPlaneClearance = 0; powerPlaneReliefExpansion = 0;
        pasteMaskExpansion = 0; solderMaskExpansion = 0;
        pasteMaskMode = 1; solderMaskMode = 1; // 1 = From rule
        jumperId = 0;
        holePositiveTolerance = int.MaxValue; // 0x7FFFFFFF = unset
        holeNegativeTolerance = int.MaxValue;
        rawExtendedTail = Array.Empty<byte>();

        // We are positioned at offset 61 (start of the extended tail). Read the rest of the
        // SubRecord into a buffer and index it by absolute offset; this also consumes the
        // remainder so the SubRecord-6 read that follows stays aligned.
        var consumed = (int)(reader.Position - startPos);
        var tailLength = (int)(blockSize - consumed);
        if (tailLength <= 0)
            return;
        var tail = reader.ReadBytes(tailLength);
        rawExtendedTail = tail;

        bool Has(int offset, int width) => offset >= PadExtendedStart
            && offset - PadExtendedStart + width <= tail.Length;
        byte B(int offset, byte fallback) => Has(offset, 1) ? tail[offset - PadExtendedStart] : fallback;
        short I16(int offset, short fallback) => Has(offset, 2)
            ? (short)(tail[offset - PadExtendedStart] | (tail[offset - PadExtendedStart + 1] << 8)) : fallback;
        int I32(int offset, int fallback) => Has(offset, 4)
            ? BitConverter.ToInt32(tail, offset - PadExtendedStart) : fallback;

        stackMode = B(62, 0);                              // 62: pad stack mode
        // 63-66: reserved
        powerPlaneConnectStyle = B(67, 0);                 // 67: plane connection style
        reliefConductorWidth = I32(68, 0);                 // 68-71
        reliefEntries = I16(72, 4);                        // 72-73
        reliefAirGap = I32(74, 0);                         // 74-77
        powerPlaneReliefExpansion = I32(78, 0);            // 78-81
        powerPlaneClearance = I32(82, 0);                  // 82-85
        pasteMaskExpansion = I32(86, 0);                   // 86-89 (manual paste expansion)
        solderMaskExpansion = I32(90, 0);                  // 90-93 (manual solder expansion)
        // 94-100: pad-cache validity bytes (regenerated from template on write)
        pasteMaskMode = B(101, 1);                         // 101: 0=None,1=Rule,2=Manual
        solderMaskMode = B(102, 1);                        // 102
        // 103-105: cache validity + gap; 106-109: user union; 110-111: jumper; 114-117: save id
        jumperId = I16(110, 0);                            // 110-111
        holePositiveTolerance = I32(PadHolePositiveToleranceOffset, int.MaxValue); // 162-165
        holeNegativeTolerance = I32(PadHoleNegativeToleranceOffset, int.MaxValue); // 166-169
    }

    private static void ReadPadSizeShapeBlock(BinaryFormatReader reader, int stackMode,
        ref byte shapeTop, ref byte shapeMiddle, ref byte shapeBottom,
        out int[] layerXSizes, out int[] layerYSizes, out byte[] internalLayerShapes,
        out byte holeShapeByte, out int holeSlotLength, out double holeRotation,
        out int[] offsetX, out int[] offsetY, out byte hasRoundedRectByte,
        out byte[] perLayerShapes, out byte[] perLayerCornerRadii, out bool hasSizeShapeBlock,
        out List<PadFullStackEntry> fullStackEntries)
    {
        fullStackEntries = new List<PadFullStackEntry>();
        var sizeShapeBlockSize = reader.ReadInt32();
        var sanitizedSize = sizeShapeBlockSize & BinaryFormatReader.BlockSizeMask;
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
            // Full-stack tail: [32 reserved][u32 count][u32 stride][count x stride-byte entries].
            if (ssRemaining >= 40)
            {
                reader.Skip(32); // reserved (zeros)
                var count = reader.ReadInt32();
                var stride = reader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    if (stride < 15 || reader.Position - ssStartPos + stride > sanitizedSize)
                        break;
                    fullStackEntries.Add(new PadFullStackEntry
                    {
                        LayerCode = reader.ReadByte(),
                        Flag1 = reader.ReadByte(),
                        Flag2 = reader.ReadByte(),
                        Flag3 = reader.ReadByte(),
                        Flag4 = reader.ReadByte(),
                        SizeX = reader.ReadInt32(),
                        SizeY = reader.ReadInt32(),
                        CornerPercent = reader.ReadByte(),
                        Trailing = reader.ReadByte(),
                    });
                    if (stride > 15)
                        reader.Skip(stride - 15);
                }
                var ssRemaining2 = sanitizedSize - (reader.Position - ssStartPos);
                if (ssRemaining2 > 0)
                    reader.Skip((int)ssRemaining2);
            }
            else if (ssRemaining > 0)
            {
                reader.Skip((int)ssRemaining);
            }
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
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
            return null;

        // Read the entire SubRecord 1 into a buffer and parse by absolute offset.
        var sr1 = reader.ReadBytes(sanitizedSize);

        bool Has(int off, int width) => off >= 0 && off + width <= sr1.Length;
        byte B(int off) => Has(off, 1) ? sr1[off] : (byte)0;
        int I32(int off, int dflt = 0) => Has(off, 4) ? BitConverter.ToInt32(sr1, off) : dflt;
        ushort U16(int off) => Has(off, 2) ? BitConverter.ToUInt16(sr1, off) : (ushort)0xFFFF;

        var layer = B(0);
        var flags = (ushort)(B(1) | (B(2) << 8));

        var via = PcbVia.Create()
            .At(Coord.FromRaw(I32(13)), Coord.FromRaw(I32(17)))
            .Diameter(Coord.FromRaw(I32(21)))
            .HoleSize(Coord.FromRaw(I32(25)))
            .Layers(B(29), B(30))
            .Build();

        via.NetIndex = U16(3);                                  // 3-4 net index
        var viaComp = U16(7);                                   // 7-8 component index
        via.ComponentIndex = viaComp == 0xFFFF ? -1 : viaComp;
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        via.IsLocked = isLocked;
        via.Layer = layer;
        via.IsKeepout = isKeepout;
        via.IsTentingTop = isTentingTop;
        via.IsTentingBottom = isTentingBottom;

        via.PowerPlaneConnectStyle = B(31);              // 31
        via.ThermalReliefAirGap = Coord.FromRaw(I32(32)); // 32-35
        via.ThermalReliefConductors = B(36);             // 36
        via.ThermalReliefConductorsWidth = Coord.FromRaw(I32(38)); // 38-41
        via.PowerPlaneReliefExpansion = Coord.FromRaw(I32(42));    // 42-45
        via.PowerPlaneClearance = Coord.FromRaw(I32(46));          // 46-49
        via.PasteMaskExpansion = Coord.FromRaw(I32(50));           // 50-53
        via.SolderMaskExpansion = Coord.FromRaw(I32(54));          // 54-57 (front)
        var solderMaskMode = B(66);                                // 66
        via.SolderMaskExpansionMode = solderMaskMode;
        via.SolderMaskExpansionManual = solderMaskMode == 2;
        via.Mode = B(74);                                          // 74
        for (var i = 0; i < 32; i++)
            via.Diameters[i] = Coord.FromRaw(I32(75 + i * 4));     // 75-202
        via.SolderMaskExpansionFromHoleEdge = B(258) != 0;        // 258
        via.HolePositiveTolerance = Coord.FromRaw(I32(291, int.MaxValue)); // 291-294
        via.HoleNegativeTolerance = Coord.FromRaw(I32(295, int.MaxValue)); // 295-298
        via.DrillLayerPairType = B(312);                          // 312

        // Fully model the cache/reserved/identity bytes (no raw replay).
        via.CacheValid61 = I32(61);                                // 61-64 cache-validity word
        via.CacheValid67 = B(67);                                  // 67 cache-validity byte
        via.ReservedByte70 = B(70);                                // 70
        via.ReservedByte72 = B(72);                                // 72
        via.SolderMaskBackRaw = I32(242);                          // 242-245 back-side mask
        if (sr1.Length >= 259 + 16) via.IdentityGuid = new Guid(sr1.AsSpan(259, 16));   // uid
        if (sr1.Length >= 275 + 16) via.IdentityGuidB = new Guid(sr1.AsSpan(275, 16));  // sig
        return via;
    }

    internal static PcbTrack? ReadTrack(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
            return null;

        var sr = reader.ReadBytes(sanitizedSize);
        bool Has(int off, int width) => off >= 0 && off + width <= sr.Length;
        byte B(int off) => Has(off, 1) ? sr[off] : (byte)0;
        int I32(int off) => Has(off, 4) ? BitConverter.ToInt32(sr, off) : 0;
        ushort U16(int off) => Has(off, 2) ? BitConverter.ToUInt16(sr, off) : (ushort)0xFFFF;

        var layer = B(0);
        var flags = (ushort)(B(1) | (B(2) << 8));

        var track = PcbTrack.Create()
            .From(Coord.FromRaw(I32(13)), Coord.FromRaw(I32(17)))
            .To(Coord.FromRaw(I32(21)), Coord.FromRaw(I32(25)))
            .Width(Coord.FromRaw(I32(29)))
            .Layer(layer)
            .Build();

        track.NetIndex = U16(3);                                  // 3-4 net index
        var trackComp = U16(7);                                   // 7-8 component index
        track.ComponentIndex = trackComp == 0xFFFF ? -1 : trackComp;

        track.SolderMaskExpansion = Coord.FromRaw(I32(35)); // 35-38
        track.KeepoutRestrictions = B(45);                  // 45
        track.RawFlags = flags;

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        track.IsLocked = isLocked;
        track.IsTentingTop = isTentingTop;
        track.IsTentingBottom = isTentingBottom;
        track.IsKeepout = isKeepout;

        return track;
    }

    internal static PcbText? ReadText(BinaryFormatReader reader, List<string> wideStrings)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
            return null;

        // Read the entire SubRecord 1 into a buffer and parse by absolute offset
        // (the Altium text layout is fixed: geometry, font, text-box, barcode block, tail).
        var sr1 = reader.ReadBytes(sanitizedSize);

        bool Has(int off, int width) => off >= 0 && off + width <= sr1.Length;
        byte B(int off) => Has(off, 1) ? sr1[off] : (byte)0;
        short I16(int off) => Has(off, 2) ? (short)(sr1[off] | (sr1[off + 1] << 8)) : (short)0;
        ushort U16(int off) => Has(off, 2) ? (ushort)(sr1[off] | (sr1[off + 1] << 8)) : (ushort)0xFFFF;
        int I32(int off) => Has(off, 4) ? BitConverter.ToInt32(sr1, off) : 0;
        double Dbl(int off) => Has(off, 8) ? BitConverter.ToDouble(sr1, off) : 0.0;
        string Utf16(int off, int byteLen)
        {
            if (!Has(off, byteLen)) return string.Empty;
            var s = System.Text.Encoding.Unicode.GetString(sr1, off, byteLen);
            var nul = s.IndexOf('\0');
            return nul >= 0 ? s.Substring(0, nul) : s;
        }

        var layer = B(0);
        var flags = (ushort)(B(1) | (B(2) << 8));
        var x = I32(13);
        var y = I32(17);
        var height = I32(21);
        var fontId = I16(25);                                       // font-table index (Altium fontID)
        var rotation = Dbl(27);
        var mirrored = B(35) != 0;
        var strokeWidth = I32(36);
        var isComment = B(40) != 0;
        var isDesignator = B(41) != 0;
        var charSet = B(42);
        var fontTypeAt43 = B(43);
        var fontBold = B(44) != 0;
        var fontItalic = B(45) != 0;
        var fontName = Utf16(46, 64);
        var isInverted = B(110) != 0;
        var marginBorderWidth = I32(111);
        var wideStringIndex = I32(115);
        var textUnionIndex = I32(119);
        var useInvertedRect = B(123) != 0;
        var textboxWidth = I32(124);
        var textboxHeight = I32(128);
        var textboxJustification = B(132);
        var textOffsetWidth = I32(133);
        // Barcode block (offsets 137-229)
        var bcFullWidth = I32(137);
        var bcFullHeight = I32(141);
        var bcXMargin = I32(145);
        var bcYMargin = I32(149);
        var bcMinWidth = I32(153);
        var bcKind = B(157);
        var bcRenderMode = B(158);
        var bcInverted = B(159) != 0;
        var bcFontType = B(160); // bc[23]: authoritative text kind when barcode block present
        var bcFontName = Utf16(161, 64);
        var bcShowText = B(225) != 0;
        // Frame tail (offsets 230-251)
        var isFrame = B(230) != 0;
        var isOffsetBorder = B(231) != 0;
        var isJustificationValid = B(240) != 0;
        var advanceSnapping = B(241) != 0;
        var snapPointX = I32(244);
        var snapPointY = I32(248);

        // Read ASCII text (SubRecord 2)
        var asciiText = reader.ReadStringBlock();
        var text = asciiText;
        if (wideStringIndex >= 0 && wideStringIndex < wideStrings.Count)
            text = wideStrings[wideStringIndex];
        if (string.IsNullOrEmpty(text))
            return null;

        // The barcode block's font-type byte (bc[23]) overrides the offset-43 value when present.
        var textKind = (PcbTextKind)(Has(160, 1) ? bcFontType : fontTypeAt43);

        var result = PcbText.Create(text)
            .At(Coord.FromRaw(x), Coord.FromRaw(y))
            .Height(Coord.FromRaw(height))
            .StrokeWidth(Coord.FromRaw(strokeWidth))
            .Rotation(rotation)
            .Mirrored(mirrored)
            .Layer(layer)
            .Build();

        result.FontId = fontId;                                     // 25-26 font-table index (Altium fontID)
        result.TextKind = textKind;
        result.NetIndex = U16(3);                                   // 3-4 net index
        var textComp = U16(7);                                      // 7-8 component index
        result.ComponentIndex = textComp == 0xFFFF ? -1 : textComp;
        result.IsTrueType = textKind == PcbTextKind.TrueType;
        result.IsComment = isComment;
        result.IsDesignator = isDesignator;
        result.CharSet = charSet;
        result.FontBold = fontBold;
        result.FontItalic = fontItalic;
        result.FontName = fontName;
        result.IsInverted = isInverted;
        result.InvertedBorder = Coord.FromRaw(marginBorderWidth);
        result.WideStringIndex = wideStringIndex;
        result.UnionIndex = textUnionIndex;
        result.UseInvertedRectangle = useInvertedRect;
        result.InvertedRectWidth = Coord.FromRaw(textboxWidth);
        result.InvertedRectHeight = Coord.FromRaw(textboxHeight);
        result.InvertedRectJustification = (PcbTextJustification)textboxJustification;
        result.InvertedRectTextOffset = Coord.FromRaw(textOffsetWidth);
        result.BarCodeFullWidth = Coord.FromRaw(bcFullWidth);
        result.BarCodeFullHeight = Coord.FromRaw(bcFullHeight);
        result.BarCodeXMargin = Coord.FromRaw(bcXMargin);
        result.BarCodeYMargin = Coord.FromRaw(bcYMargin);
        result.BarCodeMinWidth = Coord.FromRaw(bcMinWidth);
        result.BarCodeKind = bcKind;
        result.BarCodeRenderMode = bcRenderMode;
        result.BarCodeInverted = bcInverted;
        result.BarCodeFontName = bcFontName;
        result.BarCodeShowText = bcShowText;
        result.IsFrame = isFrame;
        result.IsOffsetBorder = isOffsetBorder;
        result.IsJustificationValid = isJustificationValid;
        result.AdvanceSnapping = advanceSnapping;
        result.SnapPointX = Coord.FromRaw(snapPointX);
        result.SnapPointY = Coord.FromRaw(snapPointY);

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        result.IsLocked = isLocked;
        result.IsTentingTop = isTentingTop;
        result.IsTentingBottom = isTentingBottom;
        result.IsKeepout = isKeepout;

        // Store offsets 43/160 only when they disagree with the value derived from TextKind/IsTrueType,
        // so the writer can derive them for from-scratch text while round-tripping exact source bytes.
        var isTrue = textKind == PcbTextKind.TrueType;
        var baseDerived = (byte)(textKind == PcbTextKind.BarCode ? (isTrue ? 1 : 0) : (int)textKind);
        result.BaseFontType = fontTypeAt43 != baseDerived ? fontTypeAt43 : null;
        result.TextKindByte = bcFontType != (byte)textKind ? bcFontType : null;
        // Capture the 64-byte font-name fields only when their trailing padding differs from a clean
        // (name + zero-fill) emit, so the handful of texts with dirty padding round-trip exactly while
        // everything else stays fully modeled.
        result.FontFieldRaw = DirtyFontField(sr1, 46, fontName);
        result.BarCodeFontFieldRaw = DirtyFontField(sr1, 161, bcFontName);
        result.RawFlags = flags;
        return result;
    }

    // Returns the exact 64-byte font field at <paramref name="off"/> when it differs from the modeled
    // "UTF-16 name (max 62 bytes) + zero fill" form the writer would emit; otherwise null.
    private static byte[]? DirtyFontField(byte[] sr1, int off, string name)
    {
        if (off + 64 > sr1.Length) return null;
        var modeled = new byte[64];
        var nameBytes = System.Text.Encoding.Unicode.GetBytes(name ?? string.Empty);
        Array.Copy(nameBytes, 0, modeled, 0, Math.Min(nameBytes.Length, 62));
        return sr1.AsSpan(off, 64).SequenceEqual(modeled) ? null : sr1.AsSpan(off, 64).ToArray();
    }

    internal static PcbFill? ReadFill(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
            return null;

        var sr = reader.ReadBytes(sanitizedSize);
        bool Has(int off, int width) => off >= 0 && off + width <= sr.Length;
        byte B(int off) => Has(off, 1) ? sr[off] : (byte)0;
        int I32(int off) => Has(off, 4) ? BitConverter.ToInt32(sr, off) : 0;
        double Dbl(int off) => Has(off, 8) ? BitConverter.ToDouble(sr, off) : 0.0;
        ushort U16(int off) => Has(off, 2) ? BitConverter.ToUInt16(sr, off) : (ushort)0xFFFF;

        var layer = B(0);
        var flags = (ushort)(B(1) | (B(2) << 8));

        var fill = PcbFill.Create()
            .From(Coord.FromRaw(I32(13)), Coord.FromRaw(I32(17)))
            .To(Coord.FromRaw(I32(21)), Coord.FromRaw(I32(25)))
            .Rotation(Dbl(29))
            .OnLayer(layer)
            .Build();

        fill.NetIndex = U16(3);                                  // 3-4 net index
        var fillComp = U16(7);                                   // 7-8 component index
        fill.ComponentIndex = fillComp == 0xFFFF ? -1 : fillComp;
        fill.SolderMaskExpansion = Coord.FromRaw(I32(37)); // 37-40
        fill.KeepoutRestrictions = B(46);                  // 46

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
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags, out var componentIndex, out var netIndex, out var polygonIndex);

        // Header: reserved byte @13 + hole_count uint16 @14-15 + 2 reserved bytes @16-17,
        // then the nested parameter block and the geometry.
        reader.Skip(1);
        var holeCount = reader.ReadUInt16();
        reader.Skip(2);

        // Read nested C-string parameter block, parsed into the region's typed fields.
        var parameters = ReadParameterBlock(reader, out _);

        // Read outline vertices (stored as 16-byte x,y doubles in Altium format). The exact doubles are
        // preserved alongside the integer Outline so the fractional sub-coord precision round-trips.
        var vertexCount = reader.ReadUInt32();
        var kind = 0;
        if (parameters.TryGetValue("KIND", out var kindStr))
            int.TryParse(kindStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out kind);

        var region = PcbRegion.Create()
            .OnLayer(layer)
            .Kind(kind);

        var outlineExact = new List<(double X, double Y)>((int)vertexCount);
        for (var i = 0; i < vertexCount; i++)
        {
            var dx = reader.ReadDouble();
            var dy = reader.ReadDouble();
            outlineExact.Add((dx, dy));
            region.AddPoint(Coord.FromRaw((int)dx), Coord.FromRaw((int)dy));
        }

        // Read hole / cutout contours: [uint32 count][count x,y doubles] per hole.
        var holes = new List<List<CoordPoint>>(holeCount);
        var holesExact = new List<List<(double X, double Y)>>(holeCount);
        for (var h = 0; h < holeCount; h++)
        {
            if (reader.Position - startPos + 4 > sanitizedSize)
                break;
            var holeVertexCount = reader.ReadUInt32();
            if (reader.Position - startPos + (long)holeVertexCount * 16 > sanitizedSize)
                break;
            var hole = new List<CoordPoint>((int)holeVertexCount);
            var holeExact = new List<(double X, double Y)>((int)holeVertexCount);
            for (var i = 0; i < holeVertexCount; i++)
            {
                var hx = reader.ReadDouble();
                var hy = reader.ReadDouble();
                holeExact.Add((hx, hy));
                hole.Add(new CoordPoint(Coord.FromRaw((int)hx), Coord.FromRaw((int)hy)));
            }
            holes.Add(hole);
            holesExact.Add(holeExact);
        }

        // Skip trailing data
        var consumed = reader.Position - startPos;
        var remaining = sanitizedSize - consumed;
        if (remaining > 0)
            reader.Skip((int)remaining);

        var result = region.Build();
        result.OutlineExact = outlineExact;
        result.HolesExact = holesExact;
        result.RawFlags = flags;
        result.ComponentIndex = componentIndex;
        result.NetIndex = netIndex;
        result.PolygonIndex = polygonIndex;
        result.Holes = holes;

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        result.IsLocked = isLocked;
        result.IsTentingTop = isTentingTop;
        result.IsTentingBottom = isTentingBottom;
        result.IsKeepout = isKeepout;

        // Extract typed properties from the nested parameter block.
        bool ParamBool(string k) => parameters.TryGetValue(k, out var v) && v.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        if (parameters.TryGetValue("V7_LAYER", out var v7layer))
            result.V7LayerName = v7layer;
        if (parameters.TryGetValue("NAME", out var name))
            result.Name = name;
        if (parameters.TryGetValue("LAYER", out var blayer))
            result.BoardRegionLayer = blayer;
        if (parameters.ContainsKey("KEEPOUT"))
            result.BoardRegionKeepout = ParamBool("KEEPOUT");
        if (parameters.ContainsKey("ISBOARDCUTOUT"))
            result.IsBoardCutout = ParamBool("ISBOARDCUTOUT");
        if (parameters.TryGetValue("SUBPOLYINDEX", out var spi)
            && int.TryParse(spi, NumberStyles.Integer, CultureInfo.InvariantCulture, out var spiVal))
            result.SubPolyIndex = spiVal;
        if (parameters.TryGetValue("UNIONINDEX", out var uix)
            && int.TryParse(uix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uixVal))
            result.UnionIndex = uixVal;
        if (parameters.TryGetValue("ARCRESOLUTION", out var arc) && TryParseCoord(arc, out var arcCoord))
            result.ArcApproximation = arcCoord;
        if (parameters.TryGetValue("ISSHAPEBASED", out var isb))
            result.IsShapeBased = isb.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        if (parameters.TryGetValue("CAVITYHEIGHT", out var cav) && TryParseCoord(cav, out var cavCoord))
            result.CavityHeight = cavCoord;
        if (parameters.TryGetValue("KEEPOUTRESTRICTIONS", out var kor)
            && int.TryParse(kor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var korVal))
            result.KeepoutRestrictions = korVal;
        if (parameters.TryGetValue("PADINDEX", out var pix)
            && int.TryParse(pix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pixVal))
            result.PadIndex = pixVal;
        if (parameters.TryGetValue("OBJECTKIND", out var objk))
            result.ObjectKind = objk;
        if (parameters.TryGetValue("BENDINGLINECOUNT", out var blc)
            && int.TryParse(blc, NumberStyles.Integer, CultureInfo.InvariantCulture, out var blcVal))
            result.BendingLineCount = blcVal;
        if (parameters.ContainsKey("LOCKED3D"))
            result.Locked3D = ParamBool("LOCKED3D");
        if (parameters.TryGetValue("LAYERSTACKID", out var lsid))
            result.LayerStackId = lsid;
        // NET/UNIQUEID are not part of the corpus region block; populate the convenience fields if
        // present and let the catch-all round-trip them.
        if (parameters.TryGetValue("NET", out var net))
            result.Net = net;
        if (parameters.TryGetValue("UNIQUEID", out var uid))
            result.UniqueId = uid;

        // Preserve any additional parameters not modeled as typed properties (kept for forward-compat).
        result.AdditionalParameters = ExtractAdditionalParameters(parameters,
            ["V7_LAYER", "NAME", "LAYER", "KEEPOUT", "ISBOARDCUTOUT", "KIND", "SUBPOLYINDEX",
             "UNIONINDEX", "ARCRESOLUTION", "ISSHAPEBASED", "CAVITYHEIGHT", "KEEPOUTRESTRICTIONS",
             "PADINDEX", "OBJECTKIND", "BENDINGLINECOUNT", "LOCKED3D", "LAYERSTACKID"]);

        return result;
    }

    internal static PcbComponentBody? ReadComponentBody(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & BinaryFormatReader.BlockSizeMask;

        if (sanitizedSize <= 0)
            return null;

        var startPos = reader.Position;

        ReadCommonPrimitiveData(reader, out var layer, out var flags, out var componentIndex, out var netIndex);

        // Structure: uint32 prefix + byte prefix + nested parameter block + outline vertices
        reader.Skip(4); // reserved uint32 prefix
        reader.Skip(1); // reserved byte prefix

        // Read nested C-string parameter block (3D model references etc.). It has a duplicate
        // ARCRESOLUTION key (region-prefix + body), so we keep the ordered form to recover both.
        var parameters = ReadParameterBlock(reader, out var rawBodyParams);
        var orderedBodyParams = ParseParametersOrdered(rawBodyParams);

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
        result.ComponentIndex = componentIndex;
        result.NetIndex = netIndex;

        // Decode flags
        PcbBinaryConstants.DecodeFlags(flags, out var isLocked, out var isTentingTop, out var isTentingBottom, out var isKeepout);
        result.IsLocked = isLocked;
        result.IsTentingTop = isTentingTop;
        result.IsTentingBottom = isTentingBottom;
        result.IsKeepout = isKeepout;

        // Extract typed properties from the nested parameter block (byte-exact formats). Mil values
        // are rounded (not truncated like Coord.FromMils) so fractional mils round-trip exactly.
        Coord Mil(string k)
        {
            if (!parameters.TryGetValue(k, out var v) || string.IsNullOrEmpty(v)) return default;
            var span = v.AsSpan();
            if (span.EndsWith("mil", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(span[..^3], NumberStyles.Float, CultureInfo.InvariantCulture, out var mils))
                return Coord.FromRaw((int)Math.Round(mils * 10000.0, MidpointRounding.AwayFromZero));
            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var raw))
                return Coord.FromRaw(raw);
            return default;
        }
        double Dbl(string k) => parameters.TryGetValue(k, out var v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
        int Int(string k) => parameters.TryGetValue(k, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
        bool Bool(string k) => parameters.TryGetValue(k, out var v) && v.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

        // Propagate the numeric layer id from the CommonPrimitiveData byte (e.g. 69 = Mechanical 13).
        // This is the inverse of the writer's LayerNameToByte(LayerName), so it stays in sync with the
        // V7_LAYER name below. Without it, Layer was left at its model default of 57 (Mechanical 1)
        // regardless of the body's real layer, while only LayerName reflected the truth.
        result.Layer = layer;
        if (parameters.TryGetValue("V7_LAYER", out var v7Layer)) result.LayerName = v7Layer;
        if (parameters.TryGetValue("NAME", out var name)) result.Name = name;
        result.Kind = Int("KIND");
        if (parameters.ContainsKey("SUBPOLYINDEX")) result.SubPolyIndex = Int("SUBPOLYINDEX");
        result.UnionIndex = Int("UNIONINDEX");

        // The two duplicate ARCRESOLUTION keys (region-prefix, then body) from the ordered list.
        var arcOccurrences = orderedBodyParams.Where(kv => kv.Key.Equals("ARCRESOLUTION", StringComparison.OrdinalIgnoreCase)).ToList();
        if (arcOccurrences.Count > 0 && TryParseCoord(arcOccurrences[0].Value, out var arc0)) result.ArcResolutionPrefix = arc0;
        if (arcOccurrences.Count > 1 && TryParseCoord(arcOccurrences[1].Value, out var arc1)) result.ArcResolutionBody = arc1;
        else result.ArcResolutionBody = result.ArcResolutionPrefix;

        result.IsShapeBased = Bool("ISSHAPEBASED");
        result.CavityHeight = Mil("CAVITYHEIGHT");
        result.StandoffHeight = Mil("STANDOFFHEIGHT");
        result.OverallHeight = Mil("OVERALLHEIGHT");
        result.BodyProjection = Int("BODYPROJECTION");
        result.BodyColor3D = Int("BODYCOLOR3D");
        result.BodyOpacity3D = Dbl("BODYOPACITY3D");
        if (parameters.TryGetValue("IDENTIFIER", out var identifier)) result.Identifier = DecodeBodyAsciiCodes(identifier);
        if (parameters.TryGetValue("TEXTURE", out var texture)) result.Texture = texture;
        result.TextureCenterX = Mil("TEXTURECENTERX");
        result.TextureCenterY = Mil("TEXTURECENTERY");
        result.TextureSizeX = Mil("TEXTURESIZEX");
        result.TextureSizeY = Mil("TEXTURESIZEY");
        result.TextureRotation = Dbl("TEXTUREROTATION");
        if (parameters.TryGetValue("MODELID", out var modelId)) result.ModelId = modelId;
        if (parameters.TryGetValue("MODEL.CHECKSUM", out var modelCs)
            && long.TryParse(modelCs, NumberStyles.Integer, CultureInfo.InvariantCulture, out var modelCsVal))
            result.ModelChecksum = modelCsVal;
        result.ModelEmbed = Bool("MODEL.EMBED");
        if (parameters.TryGetValue("MODEL.NAME", out var modelName)) result.ModelName = modelName;
        result.Model2DLocation = new CoordPoint(Mil("MODEL.2D.X"), Mil("MODEL.2D.Y"));
        result.Model2DRotation = Dbl("MODEL.2D.ROTATION");
        result.Model3DRotX = Dbl("MODEL.3D.ROTX");
        result.Model3DRotY = Dbl("MODEL.3D.ROTY");
        result.Model3DRotZ = Dbl("MODEL.3D.ROTZ");
        result.Model3DDz = Mil("MODEL.3D.DZ");
        result.ModelType = Int("MODEL.MODELTYPE");
        if (parameters.TryGetValue("MODEL.MODELSOURCE", out var modelSource)) result.ModelSource = modelSource;
        if (parameters.ContainsKey("MODEL.EXTRUDED.MINZ")) result.ModelExtrudedMinZ = Mil("MODEL.EXTRUDED.MINZ");
        if (parameters.ContainsKey("MODEL.EXTRUDED.MAXZ")) result.ModelExtrudedMaxZ = Mil("MODEL.EXTRUDED.MAXZ");

        var bodyKnownKeys = new List<string>
        {
            "V7_LAYER", "NAME", "KIND", "SUBPOLYINDEX", "UNIONINDEX", "ARCRESOLUTION",
            "ISSHAPEBASED", "CAVITYHEIGHT", "STANDOFFHEIGHT", "OVERALLHEIGHT",
            "BODYPROJECTION", "BODYCOLOR3D", "BODYOPACITY3D", "IDENTIFIER", "TEXTURE",
            "TEXTURECENTERX", "TEXTURECENTERY", "TEXTURESIZEX", "TEXTURESIZEY", "TEXTUREROTATION",
            "MODELID", "MODEL.CHECKSUM", "MODEL.EMBED", "MODEL.NAME",
            "MODEL.2D.X", "MODEL.2D.Y", "MODEL.2D.ROTATION",
            "MODEL.3D.ROTX", "MODEL.3D.ROTY", "MODEL.3D.ROTZ", "MODEL.3D.DZ",
            "MODEL.MODELTYPE", "MODEL.MODELSOURCE", "MODEL.EXTRUDED.MINZ", "MODEL.EXTRUDED.MAXZ"
        };

        // Snap points: MODEL.SNAPCOUNT followed by MODEL.S{i}X/Y/Z raw coordinate triples.
        if (parameters.TryGetValue("MODEL.SNAPCOUNT", out var snapCountStr)
            && int.TryParse(snapCountStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var snapCount))
        {
            result.SnapCount = snapCount;
            bodyKnownKeys.Add("MODEL.SNAPCOUNT");
            int RawInt(string key) => parameters.TryGetValue(key, out var v)
                && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
            for (var i = 0; i < snapCount; i++)
            {
                result.SnapPoints.Add((RawInt($"MODEL.S{i}X"), RawInt($"MODEL.S{i}Y"), RawInt($"MODEL.S{i}Z")));
                bodyKnownKeys.Add($"MODEL.S{i}X");
                bodyKnownKeys.Add($"MODEL.S{i}Y");
                bodyKnownKeys.Add($"MODEL.S{i}Z");
            }
        }

        // Preserve any additional parameters not modeled as typed properties (trailing indexed keys).
        result.AdditionalParameters = ExtractAdditionalParameters(parameters, bodyKnownKeys);

        return result;
    }

    // Decodes a comma-separated codepoint list (e.g. "48,54,48,51") to its string; passes through
    // values that aren't a codepoint list unchanged (older files store IDENTIFIER as plain text).
    private static string DecodeBodyAsciiCodes(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var parts = s.Split(',');
        var sb = new System.Text.StringBuilder(parts.Length);
        foreach (var part in parts)
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
                return s; // not an ascii-code list
            sb.Append((char)code);
        }
        return sb.ToString();
    }
}
