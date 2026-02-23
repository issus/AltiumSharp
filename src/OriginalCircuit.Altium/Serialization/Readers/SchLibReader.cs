using OpenMcdf;
using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using OriginalCircuit.Altium.Serialization.Compound;
using OriginalCircuit.Altium.Serialization.Dto.Sch;
using System.IO.Compression;
using System.Text;
using PinElectricalType = OriginalCircuit.Altium.Models.Sch.PinElectricalType;

namespace OriginalCircuit.Altium.Serialization.Readers;

/// <summary>
/// Record types used in schematic binary format.
/// </summary>
internal enum SchRecordType
{
    Component = 1,
    Pin = 2,
    Symbol = 3,
    Label = 4,
    Bezier = 5,
    Polyline = 6,
    Polygon = 7,
    Ellipse = 8,
    Pie = 9,
    RoundedRectangle = 10,
    EllipticalArc = 11,
    Arc = 12,
    Line = 13,
    Rectangle = 14,
    SheetSymbol = 15,
    SheetEntry = 16,
    PowerObject = 17,
    Port = 18,
    NoErc = 22,
    NetLabel = 25,
    Bus = 26,
    Wire = 27,
    TextFrame = 28,
    Junction = 29,
    Image = 30,
    Designator = 34,
    BusEntry = 37,
    Parameter = 41,
    ParameterSet = 43,
    ImplementationList = 44,
    Implementation = 45,
    MapDefinerList = 46,
    MapDefiner = 47,
    ImplementationParameters = 48,
    Blanket = 225
}

/// <summary>
/// Reads schematic symbol library (.SchLib) files.
/// </summary>
public sealed class SchLibReader
{
    private readonly Dictionary<string, string> _sectionKeys = new(StringComparer.OrdinalIgnoreCase);
    private List<AltiumDiagnostic> _diagnostics = new();

    /// <summary>
    /// Reads a SchLib file from the specified path.
    /// </summary>
    /// <param name="path">Path to the .SchLib file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed schematic library.</returns>
    /// <exception cref="AltiumCorruptFileException">Thrown when the file cannot be parsed.</exception>
    /// <remarks>This method is not thread-safe. Create a new reader instance per thread.</remarks>
    public async ValueTask<SchLibrary> ReadAsync(string path, CancellationToken cancellationToken = default)
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
            throw new AltiumCorruptFileException($"Failed to read SchLib file: {ex.Message}", filePath: path, innerException: ex);
        }
    }

    /// <summary>
    /// Reads a SchLib file from a stream.
    /// </summary>
    /// <param name="stream">A readable stream containing compound file data. The stream is not closed.</param>
    /// <returns>The parsed schematic library.</returns>
    /// <exception cref="AltiumCorruptFileException">Thrown when the stream cannot be parsed.</exception>
    /// <remarks>This method is not thread-safe. Create a new reader instance per thread.</remarks>
    public SchLibrary Read(Stream stream)
    {
        try
        {
            using var accessor = CompoundFileAccessor.Open(stream, leaveOpen: true);
            return Read(accessor);
        }
        catch (Exception ex) when (ex is not AltiumFileException and not OutOfMemoryException)
        {
            throw new AltiumCorruptFileException($"Failed to read SchLib file: {ex.Message}", innerException: ex);
        }
    }

    private SchLibrary Read(CompoundFileAccessor accessor, CancellationToken cancellationToken = default)
    {
        _diagnostics = new List<AltiumDiagnostic>();

        var library = new SchLibrary();

        // Read section keys mapping
        ReadSectionKeys(accessor, library);

        // Read file header to get component list
        var componentNames = ReadFileHeader(accessor);

        // Read each component
        foreach (var componentName in componentNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sectionKey = GetSectionKeyFromRefName(componentName);
            var component = ReadComponent(accessor, sectionKey, cancellationToken);
            if (component != null)
            {
                library.Add(component);
            }
        }

        // Read embedded image data from Storage stream
        ReadStorageImageData(accessor, library);

        library.Diagnostics = _diagnostics;
        return library;
    }

    private void ReadStorageImageData(CompoundFileAccessor accessor, SchLibrary library)
    {
        var stream = accessor.TryGetStream("Storage");
        if (stream == null)
            return;

        var data = stream.GetData();
        if (data.Length == 0)
            return;

        var imageDataList = ParseStorageImageData(data, _diagnostics);
        if (imageDataList == null || imageDataList.Count == 0)
            return;

        // Match embedded image data to SchImage objects across all components in order
        var embeddedImages = new List<SchImage>();
        foreach (var component in library.Components)
        {
            foreach (var image in component.Images)
            {
                if (image is SchImage img && img.EmbedImage)
                    embeddedImages.Add(img);
            }
        }

        for (var i = 0; i < Math.Min(imageDataList.Count, embeddedImages.Count); i++)
        {
            embeddedImages[i].ImageData = imageDataList[i];
        }
    }

    private void ReadSectionKeys(CompoundFileAccessor accessor, SchLibrary library)
    {
        _sectionKeys.Clear();

        var stream = accessor.TryGetStream("SectionKeys");
        if (stream == null)
            return;

        var data = stream.GetData();
        if (data.Length == 0)
            return;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        try
        {
            var parameters = ReadParameterBlock(reader);

            if (parameters.TryGetValue("KEYCOUNT", out var keyCountStr) &&
                int.TryParse(keyCountStr, out var keyCount))
            {
                for (var i = 0; i < keyCount; i++)
                {
                    if (parameters.TryGetValue($"LIBREF{i}", out var libRef) &&
                        parameters.TryGetValue($"SECTIONKEY{i}", out var sectionKey))
                    {
                        _sectionKeys[libRef] = sectionKey;
                    }
                }
            }
        }
        catch (EndOfStreamException)
        {
            // Section keys may be empty or malformed, continue without them
            _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, "Unexpected end of stream while reading section keys", "SectionKeys"));
        }

        // Preserve section keys for round-trip fidelity
        if (_sectionKeys.Count > 0)
            library.SectionKeys = new Dictionary<string, string>(_sectionKeys, StringComparer.OrdinalIgnoreCase);
    }

    private List<string> ReadFileHeader(CompoundFileAccessor accessor)
    {
        var componentNames = new List<string>();

        var stream = accessor.TryGetStream("FileHeader");
        if (stream == null)
            return componentNames;

        var data = stream.GetData();
        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        var parameters = ReadParameterBlock(reader);

        // Check if there's more data after parameters (binary component list)
        if (reader.HasMore)
        {
            var count = reader.ReadUInt32();
            for (var i = 0; i < count; i++)
            {
                var name = reader.ReadStringBlock();
                if (!string.IsNullOrEmpty(name))
                    componentNames.Add(name);
            }
        }
        else
        {
            // Read from parameters
            if (parameters.TryGetValue("COMPCOUNT", out var countStr) &&
                int.TryParse(countStr, out var count))
            {
                for (var i = 0; i < count; i++)
                {
                    if (parameters.TryGetValue($"LIBREF{i}", out var name))
                        componentNames.Add(name);
                }
            }
        }

        return componentNames;
    }

    private string GetSectionKeyFromRefName(string refName)
    {
        if (_sectionKeys.TryGetValue(refName, out var sectionKey))
            return sectionKey;

        // Fallback: mangle name to fit compound storage limitations
        var maxLength = Math.Min(refName.Length, 31);
        return refName.Substring(0, maxLength).Replace('/', '_');
    }

    private SchComponent? ReadComponent(CompoundFileAccessor accessor, string sectionKey, CancellationToken cancellationToken = default)
    {
        var storage = accessor.TryGetStorage(sectionKey);
        if (storage == null)
            return null;

        var component = new SchComponent();

        // Read data stream containing primitives
        if (!storage.TryGetStream("Data", out var dataStream))
            return null;

        var data = dataStream.GetData();

        // Read auxiliary streams for pin data
        byte[]? pinFracRawData = null;
        byte[]? pinSymLineWidthRawData = null;
        if (storage.TryGetStream("PinFrac", out var pinFracStream))
            pinFracRawData = pinFracStream.GetData();
        if (storage.TryGetStream("PinSymbolLineWidth", out var pinSymLineWidthStream))
            pinSymLineWidthRawData = pinSymLineWidthStream.GetData();

        // Parse PinFrac data to get fractional coordinate values for binary pin records
        var pinFracData = ParsePinFracData(pinFracRawData);

        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        var isFirstRecord = true;
        var pinIndex = 0;
        SchImplementation? currentImplementation = null;

        while (reader.HasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = ReadRecordParameters(reader);
            if (parameters == null || parameters.Count == 0)
                continue;

            if (!parameters.TryGetValue("RECORD", out var recordTypeStr) ||
                !int.TryParse(recordTypeStr, out var recordType))
                continue;

            if (isFirstRecord)
            {
                // First record should be the component definition
                if (recordType == (int)SchRecordType.Component)
                {
                    ApplyComponentParameters(component, parameters);
                }
                isFirstRecord = false;
            }
            else
            {
                // Apply PinFrac data to binary pin records
                if (recordType == (int)SchRecordType.Pin && pinFracData != null)
                {
                    if (pinFracData.TryGetValue(pinIndex, out var frac))
                    {
                        // Only set frac values if they're non-zero (binary records don't include frac)
                        if (frac.x != 0)
                            parameters["LOCATION.X_FRAC"] = frac.x.ToString();
                        if (frac.y != 0)
                            parameters["LOCATION.Y_FRAC"] = frac.y.ToString();
                        if (frac.length != 0)
                            parameters["PINLENGTH_FRAC"] = frac.length.ToString();
                    }
                    pinIndex++;
                }

                // Subsequent records are primitives
                var primitive = CreatePrimitive((SchRecordType)recordType, parameters);
                if (primitive is SchImplementation impl)
                {
                    currentImplementation = impl;
                    component.AddImplementation(impl);
                }
                else if (primitive is SchMapDefiner mapDefiner)
                {
                    currentImplementation?.AddMapDefiner(mapDefiner);
                }
                else if (primitive != null && primitive is not string)
                {
                    // Skip string markers (ImplementationList, MapDefinerList, ImplementationParameters)
                    AddPrimitiveToComponent(component, primitive);
                }
            }
        }

        // Extract Designator and Comment from child SchParameter records.
        // In Altium, these are stored as RECORD=41 child parameters with NAME=Designator/Comment.
        foreach (var param in component.Parameters)
        {
            if (string.Equals(param.Name, "Designator", StringComparison.OrdinalIgnoreCase))
                component.DesignatorPrefix = param.Value;
            else if (string.Equals(param.Name, "Comment", StringComparison.OrdinalIgnoreCase))
                component.Comment = param.Value;
        }

        // Parse PinSymbolLineWidth auxiliary stream to set per-pin SymbolLineWidth values.
        if (pinSymLineWidthRawData is { Length: > 0 })
            ApplyPinSymbolLineWidths(component, pinSymLineWidthRawData);

        return component;
    }

    /// <summary>
    /// Reads a single record from the data stream and returns it as a parameter dictionary.
    /// Handles both parameter-based (ASCII) records and binary pin records.
    /// </summary>
    internal static Dictionary<string, string>? ReadRecordParameters(BinaryFormatReader reader)
    {
        if (!reader.HasMore)
            return null;

        try
        {
            var sizeHeader = reader.ReadInt32();
            var flags = (byte)((sizeHeader >> 24) & 0xFF);
            var dataSize = sizeHeader & 0x00FFFFFF;

            if (dataSize <= 0 || dataSize > 1_000_000)
                return null;

            if (flags == 0x01)
            {
                // Binary pin record
                return ReadBinaryPinRecord(reader, dataSize);
            }
            else
            {
                // Parameter-based (ASCII) record
                return ReadParameterRecord(reader, dataSize);
            }
        }
        catch (EndOfStreamException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads a binary pin record and converts it to a parameter dictionary
    /// matching the SchPinDto expected keys.
    /// </summary>
    private static Dictionary<string, string>? ReadBinaryPinRecord(BinaryFormatReader reader, int dataSize)
    {
        var startPos = reader.Position;

        var recordType = reader.ReadInt32(); // Should be 2 for pins
        if (recordType != 2)
        {
            // Not a pin, skip remaining data
            var remaining = dataSize - 4;
            if (remaining > 0) reader.Skip(remaining);
            return null;
        }

        var unknown1 = reader.ReadByte();
        var ownerPartId = reader.ReadInt16();
        var ownerPartDisplayMode = reader.ReadByte();
        var symbolInnerEdge = reader.ReadByte();
        var symbolOuterEdge = reader.ReadByte();
        var symbolInside = reader.ReadByte();
        var symbolOutside = reader.ReadByte();
        var description = reader.ReadPascalShortString();
        var formalType = reader.ReadByte();
        var electrical = reader.ReadByte();
        var pinConglomerate = reader.ReadByte();
        var pinLength = reader.ReadInt16();
        var locationX = reader.ReadInt16();
        var locationY = reader.ReadInt16();
        var color = reader.ReadInt32();
        var name = reader.ReadPascalShortString();
        var designator = reader.ReadPascalShortString();
        var swapIdGroup = reader.ReadPascalShortString();
        var partAndSequence = reader.ReadPascalShortString();
        var defaultValue = reader.ReadPascalShortString();

        // Consume any remaining bytes in the block
        var consumed = (int)(reader.Position - startPos);
        if (consumed < dataSize)
            reader.Skip(dataSize - consumed);

        // Store values in DXP units (same as parameter-based records).
        // The DXP-to-raw conversion happens later in the Create* methods.
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RECORD"] = "2",
            ["OWNERPARTID"] = ownerPartId.ToString(),
            ["OWNERPARTDISPLAYMODE"] = ownerPartDisplayMode.ToString(),
            ["SYMBOL_INNEREDGE"] = symbolInnerEdge.ToString(),
            ["SYMBOL_OUTEREDGE"] = symbolOuterEdge.ToString(),
            ["SYMBOL_INSIDE"] = symbolInside.ToString(),
            ["SYMBOL_OUTSIDE"] = symbolOutside.ToString(),
            ["DESCRIPTION"] = description,
            ["FORMALTYPE"] = formalType.ToString(),
            ["ELECTRICAL"] = electrical.ToString(),
            ["PINCONGLOMERATE"] = pinConglomerate.ToString(),
            ["PINLENGTH"] = pinLength.ToString(),
            ["LOCATION.X"] = locationX.ToString(),
            ["LOCATION.Y"] = locationY.ToString(),
            ["COLOR"] = color.ToString(),
            ["NAME"] = name,
            ["DESIGNATOR"] = designator,
            ["SWAPIDGROUP"] = swapIdGroup,
            ["DEFAULTVALUE"] = defaultValue
        };

        return parameters;
    }

    /// <summary>
    /// Reads a parameter-based (ASCII) record from the data stream.
    /// Records are C-strings (null-terminated, no length prefix).
    /// </summary>
    private static Dictionary<string, string>? ReadParameterRecord(BinaryFormatReader reader, int dataSize)
    {
        if (dataSize <= 0)
            return null;

        byte[] buffer;
        if (dataSize <= 512)
        {
            Span<byte> stackBuffer = stackalloc byte[dataSize];
            reader.ReadExact(stackBuffer);
            buffer = stackBuffer.ToArray();
        }
        else
        {
            buffer = new byte[dataSize];
            reader.ReadExact(buffer);
        }

        // Find null terminator (if present) and decode the string
        var nullIndex = Array.IndexOf(buffer, (byte)0);
        var length = nullIndex >= 0 ? nullIndex : dataSize;
        if (length == 0)
            return null;

        var paramString = AltiumEncoding.Windows1252.GetString(buffer, 0, length);
        return ParseParameters(paramString);
    }

    private static Dictionary<string, string> ReadParameterBlock(BinaryFormatReader reader)
    {
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // SchLib parameter blocks (FileHeader, SectionKeys) are C-strings
        // (null-terminated, no length prefix) — same as PCB format.
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

        return ParseParameters(paramString);
    }

    internal static Dictionary<string, string> ParseParameters(string paramString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

            result[key] = value;
        }

        return result;
    }

    private static void ApplyComponentParameters(SchComponent component, Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchComponentDto.FromParameters(paramCollection);

        component.Name = dto.LibReference ?? dto.DesignItemId ?? string.Empty;
        component.Description = dto.ComponentDescription;
        component.DesignatorPrefix = dto.DesignatorPrefix;
        // PARTCOUNT in binary includes part 0 (common part); user-facing count is PARTCOUNT - 1
        component.PartCount = Math.Max(0, dto.PartCount - 1);
        component.UniqueId = dto.UniqueId;
        component.Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac));
        component.Orientation = dto.Orientation;
        component.CurrentPartId = dto.CurrentPartId;
        component.DisplayModeCount = dto.DisplayModeCount;
        component.DisplayMode = dto.DisplayMode;
        component.ShowHiddenPins = dto.ShowHiddenPins;
        component.ShowHiddenFields = dto.ShowHiddenFields;
        component.LibraryPath = dto.LibraryPath;
        component.SourceLibraryName = dto.SourceLibraryName;
        component.LibReference = dto.LibReference;
        component.DesignItemId = dto.DesignItemId;
        component.ComponentKind = dto.ComponentKind;
        component.OverrideColors = dto.OverrideColors;
        component.Color = dto.Color;
        component.AreaColor = dto.AreaColor;
        component.DesignatorLocked = dto.DesignatorLocked;
        component.PartIdLocked = dto.PartIdLocked;
        component.SymbolReference = dto.SymbolReference;
        component.SheetPartFileName = dto.SheetPartFileName;
        component.TargetFileName = dto.TargetFileName;
        component.AliasList = dto.AliasList;
        component.AllPinCount = dto.AllPinCount;
        component.GraphicallyLocked = dto.GraphicallyLocked;
        component.PinsMoveable = dto.PinsMoveable;
        component.PinColor = dto.PinColor;
        component.DatabaseLibraryName = dto.DatabaseLibraryName;
        component.DatabaseTableName = dto.DatabaseTableName;
        component.LibraryIdentifier = dto.LibraryIdentifier;
        component.VaultGuid = dto.VaultGuid;
        component.ItemGuid = dto.ItemGuid;
        component.RevisionGuid = dto.RevisionGuid;
        component.Disabled = dto.Disabled;
        component.Dimmed = dto.Dimmed;
        component.ConfigurationParameters = dto.ConfigurationParameters;
        component.ConfiguratorName = dto.ConfiguratorName;
        component.NotUsedBTableName = dto.NotUsedBTableName;
        component.OwnerPartId = dto.OwnerPartId;
        component.DisplayFieldNames = dto.DisplayFieldNames;
        component.IsMirrored = dto.IsMirrored;
        component.IsUnmanaged = dto.IsUnmanaged;
        component.IsUserConfigurable = dto.IsUserConfigurable;
        component.LibIdentifierKind = dto.LibIdentifierKind;
        component.OwnerPartDisplayMode = dto.OwnerPartDisplayMode;
        component.RevisionDetails = dto.RevisionDetails;
        component.RevisionHrid = dto.RevisionHrid;
        component.RevisionState = dto.RevisionState;
        component.RevisionStatus = dto.RevisionStatus;
        component.SymbolItemGuid = dto.SymbolItemGuid;
        component.SymbolItemsGuid = dto.SymbolItemsGuid;
        component.SymbolRevisionGuid = dto.SymbolRevisionGuid;
        component.SymbolVaultGuid = dto.SymbolVaultGuid;
        component.UseDbTableName = dto.UseDbTableName;
        component.UseLibraryName = dto.UseLibraryName;
        component.VariantOption = dto.VariantOption;
        component.VaultHrid = dto.VaultHrid;
        component.GenericComponentTemplateGuid = dto.GenericComponentTemplateGuid;
    }

    /// <summary>
    /// Parses the PinSymbolLineWidth auxiliary stream and applies SYMBOL_LINEWIDTH
    /// values to the corresponding pins in the component.
    /// Format: header block + per-pin compressed storage entries.
    /// Each entry: int32 size header (low 24 bits = size, high byte = flags),
    ///   0xD0 tag, Pascal string (pin index), int32 compressed size, zlib data.
    /// </summary>
    /// <summary>
    /// Parses the PinFrac auxiliary stream data to extract fractional coordinate values.
    /// Returns a dictionary keyed by pin index with (x, y, length) fractional values.
    /// </summary>
    private Dictionary<int, (int x, int y, int length)>? ParsePinFracData(byte[]? rawData)
    {
        if (rawData == null || rawData.Length == 0)
            return null;

        try
        {
            var result = new Dictionary<int, (int x, int y, int length)>();
            using var ms = new MemoryStream(rawData);
            using var reader = new BinaryFormatReader(ms, leaveOpen: true);

            // Skip header block (C-string parameter block with HEADER=PinFrac and WEIGHT=N)
            var headerSize = reader.ReadInt32();
            if (headerSize > 0) reader.Skip(headerSize);

            // Read per-pin compressed storage entries
            while (reader.HasMore)
            {
                var sizeHeader = reader.ReadInt32();
                var blockSize = sizeHeader & 0x00FFFFFF;
                if (blockSize <= 0) break;

                var blockStart = reader.Position;

                // 0xD0 tag
                var tag = reader.ReadByte();
                if (tag != 0xD0) break;

                // Pascal string: pin index
                var nameLen = reader.ReadByte();
                var nameBytes = new byte[nameLen];
                reader.ReadExact(nameBytes);
                var pinIndexStr = AltiumEncoding.Windows1252.GetString(nameBytes);

                // Compressed data block: int32 size + zlib data
                var compressedSize = reader.ReadInt32();
                if (compressedSize <= 0 || compressedSize > 1_000_000)
                {
                    reader.Skip((int)(blockSize - (reader.Position - blockStart)));
                    continue;
                }

                var compressedData = new byte[compressedSize];
                reader.ReadExact(compressedData);

                // Decompress zlib data → 12 bytes: 3 × Int32 (fracX, fracY, fracLength)
                if (compressedData.Length >= 2)
                {
                    try
                    {
                        using var compressedMs = new MemoryStream(compressedData);
                        using var zlibStream = new ZLibStream(compressedMs, CompressionMode.Decompress);
                        using var decompressedMs = new MemoryStream();
                        zlibStream.CopyTo(decompressedMs);
                        var decompressed = decompressedMs.ToArray();

                        if (decompressed.Length >= 12 && int.TryParse(pinIndexStr, out var idx))
                        {
                            var fracX = BitConverter.ToInt32(decompressed, 0);
                            var fracY = BitConverter.ToInt32(decompressed, 4);
                            var fracLength = BitConverter.ToInt32(decompressed, 8);
                            result[idx] = (fracX, fracY, fracLength);
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // Corrupted compressed data; skip this entry
                        _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, $"Corrupted compressed data at entry {pinIndexStr}", "PinFrac"));
                    }
                }

                // Ensure we advance to end of block
                var consumed = reader.Position - blockStart;
                if (consumed < blockSize)
                    reader.Skip((int)(blockSize - consumed));
            }

            return result.Count > 0 ? result : null;
        }
        catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or IOException
            or FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            // Non-critical: if parsing fails, pins use integer-only coordinates
            _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, "Failed to parse pin fractional coordinates", "PinFrac"));
            return null;
        }
    }

    private void ApplyPinSymbolLineWidths(SchComponent component, byte[] data)
    {
        if (data.Length == 0) return;

        try
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryFormatReader(ms, leaveOpen: true);

            // Skip header block (C-string parameter block with HEADER and WEIGHT)
            var headerSize = reader.ReadInt32();
            if (headerSize > 0) reader.Skip(headerSize);

            // Read per-pin compressed storage entries
            while (reader.HasMore)
            {
                // Read block size header (same format as SchLib record: low 24 bits = size)
                var sizeHeader = reader.ReadInt32();
                var blockSize = sizeHeader & 0x00FFFFFF;
                if (blockSize <= 0) break;

                var blockStart = reader.Position;

                // 0xD0 tag
                var tag = reader.ReadByte();
                if (tag != 0xD0) break;

                // Pascal string: pin index (1 byte length + chars)
                var nameLen = reader.ReadByte();
                var nameBytes = new byte[nameLen];
                reader.ReadExact(nameBytes);
                var pinIndexStr = AltiumEncoding.Windows1252.GetString(nameBytes);

                // Compressed data block: int32 size + zlib data
                var compressedSize = reader.ReadInt32();
                if (compressedSize <= 0 || compressedSize > 1_000_000)
                {
                    reader.Skip((int)(blockSize - (reader.Position - blockStart)));
                    continue;
                }

                var compressedData = new byte[compressedSize];
                reader.ReadExact(compressedData);

                // Decompress zlib data (skip 2-byte zlib header, use DeflateStream)
                if (compressedData.Length >= 2)
                {
                    try
                    {
                        using var compressedMs = new MemoryStream(compressedData);
                        using var zlibStream = new ZLibStream(compressedMs, CompressionMode.Decompress);
                        using var decompressedMs = new MemoryStream();
                        zlibStream.CopyTo(decompressedMs);
                        var decompressed = decompressedMs.ToArray();

                        // Parse decompressed data as Unicode parameter block:
                        // int32 innerSize + UTF-16LE string data
                        if (decompressed.Length >= 4)
                        {
                            var innerSize = BitConverter.ToInt32(decompressed, 0);
                            if (innerSize > 0 && decompressed.Length >= 4 + innerSize)
                            {
                                var paramString = Encoding.Unicode.GetString(decompressed, 4, innerSize);
                                var parameters = ParseParameters(paramString);
                                if (parameters.TryGetValue("SYMBOL_LINEWIDTH", out var lwStr) &&
                                    int.TryParse(lwStr, out var lineWidth) &&
                                    int.TryParse(pinIndexStr, out var idx) &&
                                    idx >= 0 && idx < component.Pins.Count)
                                {
                                    ((SchPin)component.Pins[idx]).SymbolLineWidth = lineWidth;
                                }
                            }
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // Corrupted compressed data; skip this entry
                        _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, $"Corrupted compressed data at entry {pinIndexStr}", "PinSymbolLineWidth"));
                    }
                }

                // Ensure we advance to end of block
                var consumed = reader.Position - blockStart;
                if (consumed < blockSize)
                    reader.Skip((int)(blockSize - consumed));
            }
        }
        catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or IOException
            or FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            // Non-critical: if parsing fails, pins retain default SymbolLineWidth
            _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, "Failed to parse pin symbol line widths", "PinSymbolLineWidth"));
        }
    }

    private static object? CreatePrimitive(SchRecordType recordType, Dictionary<string, string> parameters)
    {
        return recordType switch
        {
            SchRecordType.Pin => CreatePin(parameters),
            SchRecordType.Line => CreateLine(parameters),
            SchRecordType.Rectangle => CreateRectangle(parameters),
            SchRecordType.Label => CreateLabel(parameters),
            SchRecordType.Wire => CreateWire(parameters),
            SchRecordType.Polyline => CreatePolyline(parameters),
            SchRecordType.Polygon => CreatePolygon(parameters),
            SchRecordType.Arc => CreateArc(parameters),
            SchRecordType.Bezier => CreateBezier(parameters),
            SchRecordType.Ellipse => CreateEllipse(parameters),
            SchRecordType.RoundedRectangle => CreateRoundedRectangle(parameters),
            SchRecordType.Pie => CreatePie(parameters),
            SchRecordType.NetLabel => CreateNetLabel(parameters),
            SchRecordType.Junction => CreateJunction(parameters),
            SchRecordType.Designator => CreateParameter(parameters),
            SchRecordType.Parameter => CreateParameter(parameters),
            SchRecordType.TextFrame => CreateTextFrame(parameters),
            SchRecordType.Image => CreateImage(parameters),
            SchRecordType.Symbol => CreateSymbol(parameters),
            SchRecordType.EllipticalArc => CreateEllipticalArc(parameters),
            SchRecordType.PowerObject => CreatePowerObject(parameters),
            SchRecordType.NoErc => CreateNoErc(parameters),
            SchRecordType.BusEntry => CreateBusEntry(parameters),
            SchRecordType.Bus => CreateBus(parameters),
            SchRecordType.Port => CreatePort(parameters),
            SchRecordType.SheetSymbol => CreateSheetSymbol(parameters),
            SchRecordType.SheetEntry => CreateSheetEntry(parameters),
            SchRecordType.Blanket => CreateBlanket(parameters),
            SchRecordType.ParameterSet => CreateParameterSet(parameters),
            SchRecordType.ImplementationList => "ImplementationList", // Container marker
            SchRecordType.Implementation => CreateImplementation(parameters),
            SchRecordType.MapDefinerList => "MapDefinerList", // Container marker
            SchRecordType.MapDefiner => CreateMapDefiner(parameters),
            SchRecordType.ImplementationParameters => "ImplementationParameters", // Empty container
            _ => null // Other primitives not yet implemented
        };
    }

    /// <summary>
    /// Parses the Storage stream to extract embedded image data.
    /// The Storage stream uses the Altium compressed storage format:
    /// header block (C-string params) + entries (0xD0 tag, name, zlib data).
    /// Returns a list of raw image byte arrays in order.
    /// </summary>
    internal static List<byte[]>? ParseStorageImageData(byte[] rawData, List<AltiumDiagnostic>? diagnostics = null)
    {
        if (rawData.Length == 0)
            return null;

        try
        {
            var result = new List<byte[]>();
            using var ms = new MemoryStream(rawData);
            using var reader = new BinaryFormatReader(ms, leaveOpen: true);

            // Skip header block (C-string parameter block with HEADER=Icon storage)
            var headerSize = reader.ReadInt32();
            if (headerSize > 0) reader.Skip(headerSize);

            // Read compressed storage entries
            var entryIndex = 0;
            while (reader.HasMore)
            {
                var sizeHeader = reader.ReadInt32();
                var blockSize = sizeHeader & 0x00FFFFFF;
                if (blockSize <= 0) break;

                var blockStart = reader.Position;

                // 0xD0 tag
                var tag = reader.ReadByte();
                if (tag != 0xD0) break;

                // Pascal string: entry name/index
                var nameLen = reader.ReadByte();
                var nameBytes = new byte[nameLen];
                reader.ReadExact(nameBytes);

                // Compressed data block: int32 size + zlib data
                var compressedSize = reader.ReadInt32();
                if (compressedSize <= 0 || compressedSize > 100_000_000)
                {
                    reader.Skip((int)(blockSize - (reader.Position - blockStart)));
                    entryIndex++;
                    continue;
                }

                var compressedData = new byte[compressedSize];
                reader.ReadExact(compressedData);

                // Decompress zlib data → raw image bytes
                try
                {
                    using var compressedMs = new MemoryStream(compressedData);
                    using var zlibStream = new ZLibStream(compressedMs, CompressionMode.Decompress);
                    using var decompressedMs = new MemoryStream();
                    zlibStream.CopyTo(decompressedMs);
                    result.Add(decompressedMs.ToArray());
                }
                catch (InvalidDataException)
                {
                    // Corrupted compressed data; skip this entry
                    diagnostics?.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, $"Corrupted compressed data at entry {entryIndex}", "Storage"));
                }

                // Ensure we advance to end of block
                var consumed = reader.Position - blockStart;
                if (consumed < blockSize)
                    reader.Skip((int)(blockSize - consumed));

                entryIndex++;
            }

            return result.Count > 0 ? result : null;
        }
        catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or IOException
            or FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            diagnostics?.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, "Failed to parse pin image data", "PinTextData"));
            return null;
        }
    }

    /// <summary>
    /// Converts a Dictionary to a ParameterCollection for DTO deserialization.
    /// </summary>
    internal static ParameterCollection ToParameterCollection(Dictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        foreach (var kvp in parameters)
        {
            sb.Append('|');
            sb.Append(kvp.Key);
            sb.Append('=');
            sb.Append(kvp.Value);
        }
        return ParameterCollection.Parse(sb.ToString());
    }

    private static SchPin CreatePin(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchPinDto.FromParameters(paramCollection);

        var pin = new SchPin
        {
            Name = dto.Name,
            Designator = dto.Designator,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Length = CoordFromDxp(dto.PinLength, dto.PinLengthFrac),
            ElectricalType = (PinElectricalType)dto.Electrical
        };

        // Decode PinConglomerate flags
        var cong = dto.PinConglomerate;
        pin.Orientation = (PinOrientation)(cong & 0x03);
        pin.ShowName = (cong & 0x08) != 0;
        pin.ShowDesignator = (cong & 0x10) != 0;
        pin.Description = dto.Description;
        pin.FormalType = dto.FormalType;
        pin.SymbolInnerEdge = dto.SymbolInnerEdge;
        pin.SymbolOuterEdge = dto.SymbolOuterEdge;
        pin.SymbolInside = dto.SymbolInside;
        pin.SymbolOutside = dto.SymbolOutside;
        pin.SymbolLineWidth = dto.SymbolLineWidth;
        pin.SwapIdPart = dto.SwapIdPart.ToString();
        pin.PinPropagationDelay = (int)dto.PinPropagationDelay;
        pin.DesignatorCustomFontId = dto.DesignatorCustomFontId;
        pin.NameCustomFontId = dto.NameCustomFontId;
        pin.Width = dto.Width;
        pin.Color = dto.Color;
        pin.AreaColor = dto.AreaColor;
        pin.DefaultValue = dto.DefaultValue;
        pin.IsHidden = dto.IsHidden;
        pin.DesignatorCustomColor = dto.DesignatorCustomColor;
        pin.DesignatorCustomPositionMargin = dto.DesignatorCustomPositionMargin;
        pin.DesignatorCustomPositionRotationAnchor = dto.DesignatorCustomPositionRotationAnchor;
        pin.DesignatorCustomPositionRotationRelative = dto.DesignatorCustomPositionRotationRelative;
        pin.DesignatorFontMode = dto.DesignatorFontMode;
        pin.DesignatorPositionMode = dto.DesignatorPositionMode;
        pin.NameCustomColor = dto.NameCustomColor;
        pin.NameCustomPositionMargin = dto.NameCustomPositionMargin;
        pin.NameCustomPositionRotationAnchor = dto.NameCustomPositionRotationAnchor;
        pin.NameCustomPositionRotationRelative = dto.NameCustomPositionRotationRelative;
        pin.NameFontMode = dto.NameFontMode;
        pin.NamePositionMode = dto.NamePositionMode;
        pin.SwapIdPair = dto.SwapIdPair;
        pin.SwapIdPartPin = dto.SwapIdPartPin;
        pin.SwapIdPin = dto.SwapIdPin;
        pin.HiddenNetName = dto.HiddenNetName;
        pin.PinPackageLength = CoordFromDxp(dto.PinPackageLength, 0);
        pin.OwnerIndex = dto.OwnerIndex;
        pin.IsNotAccessible = dto.IsNotAccessible;
        pin.IndexInSheet = dto.IndexInSheet;
        pin.OwnerPartId = dto.OwnerPartId;
        pin.OwnerPartDisplayMode = dto.OwnerPartDisplayMode;
        pin.GraphicallyLocked = dto.GraphicallyLocked;
        pin.Disabled = dto.Disabled;
        pin.Dimmed = dto.Dimmed;
        pin.UniqueId = dto.UniqueId;

        return pin;
    }

    private static SchLine CreateLine(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchLineDto.FromParameters(paramCollection);

        return new SchLine
        {
            Start = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            End = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            Width = LineWidthFromIndex(dto.LineWidth),
            Color = dto.Color,
            LineStyle = dto.LineStyle,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchRectangle CreateRectangle(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchRectangleDto.FromParameters(paramCollection);

        return new SchRectangle
        {
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            LineWidth = LineWidthFromIndex(dto.LineWidth),
            IsFilled = dto.IsSolid,
            IsTransparent = dto.Transparent,
            LineStyle = dto.LineStyle,
            Color = dto.Color,
            FillColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchLabel CreateLabel(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchLabelDto.FromParameters(paramCollection);

        return new SchLabel
        {
            Text = dto.Text ?? string.Empty,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            FontId = dto.FontId,
            Justification = (TextJustification)dto.Justification,
            Rotation = dto.Orientation * 90.0,
            Color = dto.Color,
            IsMirrored = dto.IsMirrored,
            IsHidden = dto.IsHidden,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchWire CreateWire(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchWireDto.FromParameters(paramCollection);

        var wire = SchWire.Create()
            .LineWidth(dto.LineWidth)
            .Color(dto.Color)
            .Style((SchLineStyle)dto.LineStyle);

        // Read vertices from raw parameters. Altium omits coordinates with value 0.
        var vertexCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(vertexCount, 10); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= vertexCount)
            {
                if (i == 1)
                    wire.From(hasX ? x : default, hasY ? y : default);
                else
                    wire.To(hasX ? x : default, hasY ? y : default);
            }
            else
            {
                break;
            }
        }

        var result = wire.Build();
        result.AreaColor = dto.AreaColor;
        result.IsSolid = dto.IsSolid;
        result.IsTransparent = dto.Transparent;
        result.AutoWire = dto.AutoWire;
        result.UnderlineColor = dto.UnderlineColor;
        result.OwnerIndex = dto.OwnerIndex;
        result.IsNotAccessible = dto.IsNotAccessible;
        result.IndexInSheet = dto.IndexInSheet;
        result.OwnerPartId = dto.OwnerPartId;
        result.OwnerPartDisplayMode = dto.OwnerPartDisplayMode;
        result.GraphicallyLocked = dto.GraphicallyLocked;
        result.Disabled = dto.Disabled;
        result.Dimmed = dto.Dimmed;
        result.UniqueId = dto.UniqueId;
        return result;
    }

    private static SchPolyline CreatePolyline(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchPolylineDto.FromParameters(paramCollection);

        var polyline = SchPolyline.Create()
            .LineWidth(dto.LineWidth)
            .Color(dto.Color)
            .Style((SchLineStyle)dto.LineStyle);

        // Read vertices from raw parameters. Altium omits coordinates with value 0.
        var vertexCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(vertexCount, 50); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY)
            {
                if (i == 1)
                    polyline.From(hasX ? x : default, hasY ? y : default);
                else
                    polyline.To(hasX ? x : default, hasY ? y : default);
            }
            else
            {
                break;
            }
        }

        var polylineResult = polyline.Build();
        polylineResult.StartLineShape = dto.StartLineShape;
        polylineResult.EndLineShape = dto.EndLineShape;
        polylineResult.LineShapeSize = dto.LineShapeSize;
        polylineResult.AreaColor = dto.AreaColor;
        polylineResult.IsTransparent = dto.Transparent;
        polylineResult.IsSolid = dto.IsSolid;
        polylineResult.OwnerIndex = dto.OwnerIndex;
        polylineResult.IsNotAccessible = dto.IsNotAccessible;
        polylineResult.IndexInSheet = dto.IndexInSheet;
        polylineResult.OwnerPartId = dto.OwnerPartId;
        polylineResult.OwnerPartDisplayMode = dto.OwnerPartDisplayMode;
        polylineResult.GraphicallyLocked = dto.GraphicallyLocked;
        polylineResult.Disabled = dto.Disabled;
        polylineResult.Dimmed = dto.Dimmed;
        polylineResult.UniqueId = dto.UniqueId;
        return polylineResult;
    }

    private static SchPolygon CreatePolygon(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchPolygonDto.FromParameters(paramCollection);

        var polygon = SchPolygon.Create()
            .LineWidth(dto.LineWidth)
            .Color(dto.Color)
            .FillColor(dto.AreaColor)
            .Filled(dto.IsSolid)
            .Transparent(dto.Transparent);

        // Read vertices from raw parameters. Altium omits coordinates with value 0,
        // so we treat missing X or Y as 0. Within declared LocationCount, always add vertex.
        // Beyond LocationCount, discover extra vertices only if at least one coord is present.
        var vertexCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(vertexCount, 50); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= vertexCount)
            {
                polygon.AddVertex(hasX ? x : default, hasY ? y : default);
            }
            else
            {
                break;
            }
        }

        var polygonResult = polygon.Build();
        polygonResult.OwnerIndex = dto.OwnerIndex;
        polygonResult.IsNotAccessible = dto.IsNotAccessible;
        polygonResult.IndexInSheet = dto.IndexInSheet;
        polygonResult.OwnerPartId = dto.OwnerPartId;
        polygonResult.OwnerPartDisplayMode = dto.OwnerPartDisplayMode;
        polygonResult.GraphicallyLocked = dto.GraphicallyLocked;
        polygonResult.Disabled = dto.Disabled;
        polygonResult.Dimmed = dto.Dimmed;
        polygonResult.UniqueId = dto.UniqueId;
        return polygonResult;
    }

    private static SchArc CreateArc(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchArcDto.FromParameters(paramCollection);

        return new SchArc
        {
            Center = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Radius = CoordFromDxp(dto.Radius, dto.RadiusFrac),
            StartAngle = dto.StartAngle,
            EndAngle = dto.EndAngle,
            LineWidth = dto.LineWidth,
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchBezier CreateBezier(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchBezierDto.FromParameters(paramCollection);

        var bezier = SchBezier.Create()
            .LineWidth(dto.LineWidth)
            .Color(dto.Color);

        // Read control points from raw parameters
        var pointCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(pointCount, 50); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= pointCount)
            {
                bezier.AddPoint(hasX ? x : default, hasY ? y : default);
            }
            else
            {
                break;
            }
        }

        var bezierResult = bezier.Build();
        bezierResult.AreaColor = dto.AreaColor;
        bezierResult.OwnerIndex = dto.OwnerIndex;
        bezierResult.IsNotAccessible = dto.IsNotAccessible;
        bezierResult.IndexInSheet = dto.IndexInSheet;
        bezierResult.OwnerPartId = dto.OwnerPartId;
        bezierResult.OwnerPartDisplayMode = dto.OwnerPartDisplayMode;
        bezierResult.GraphicallyLocked = dto.GraphicallyLocked;
        bezierResult.Disabled = dto.Disabled;
        bezierResult.Dimmed = dto.Dimmed;
        bezierResult.UniqueId = dto.UniqueId;
        return bezierResult;
    }

    private static SchEllipse CreateEllipse(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchEllipseDto.FromParameters(paramCollection);

        return new SchEllipse
        {
            Center = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            RadiusX = CoordFromDxp(dto.Radius, dto.RadiusFrac),
            RadiusY = dto.SecondaryRadius != 0 ? CoordFromDxp(dto.SecondaryRadius, dto.SecondaryRadiusFrac) : CoordFromDxp(dto.Radius, dto.RadiusFrac),
            LineWidth = dto.LineWidth,
            Color = dto.Color,
            FillColor = dto.AreaColor,
            IsFilled = dto.IsSolid,
            IsTransparent = dto.Transparent,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchRoundedRectangle CreateRoundedRectangle(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchRoundedRectangleDto.FromParameters(paramCollection);

        return new SchRoundedRectangle
        {
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            CornerRadiusX = CoordFromDxp(dto.CornerXRadius),
            CornerRadiusY = CoordFromDxp(dto.CornerYRadius),
            LineWidth = dto.LineWidth,
            LineStyle = dto.LineStyle,
            Color = dto.Color,
            FillColor = dto.AreaColor,
            IsFilled = dto.IsSolid,
            IsTransparent = dto.Transparent,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchPie CreatePie(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchPieDto.FromParameters(paramCollection);

        return new SchPie
        {
            Center = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Radius = CoordFromDxp(dto.Radius, dto.RadiusFrac),
            StartAngle = dto.StartAngle,
            EndAngle = dto.EndAngle,
            LineWidth = dto.LineWidth,
            Color = dto.Color,
            FillColor = dto.AreaColor,
            IsFilled = dto.IsSolid,
            IsTransparent = dto.Transparent,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchNetLabel CreateNetLabel(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchNetLabelDto.FromParameters(paramCollection);

        return new SchNetLabel
        {
            Text = dto.Text ?? string.Empty,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Orientation = dto.Orientation,
            Justification = (TextJustification)dto.Justification,
            FontId = dto.FontId,
            Color = dto.Color,
            IsMirrored = dto.IsMirrored,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchJunction CreateJunction(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchJunctionDto.FromParameters(paramCollection);

        return new SchJunction
        {
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Color = dto.Color,
            Locked = dto.Locked,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchParameter CreateParameter(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchParameterDto.FromParameters(paramCollection);

        return new SchParameter
        {
            Name = dto.Name ?? string.Empty,
            Value = dto.Text ?? string.Empty,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Orientation = dto.Orientation,
            Justification = (TextJustification)dto.Justification,
            FontId = dto.FontId,
            Color = dto.Color,
            IsVisible = !dto.IsHidden,
            ParamType = dto.ParamType,
            ShowName = dto.ShowName,
            IsMirrored = dto.IsMirrored,
            IsReadOnly = dto.ReadOnlyState != 0,
            Description = dto.Description,
            AreaColor = dto.AreaColor,
            AutoPosition = dto.AutoPosition,
            IsConfigurable = dto.IsConfigurable,
            IsRule = dto.IsRule,
            IsSystemParameter = dto.IsSystemParameter,
            TextHorzAnchor = dto.TextHorzAnchor,
            TextVertAnchor = dto.TextVertAnchor,
            HideName = dto.HideName,
            AllowDatabaseSynchronize = dto.AllowDatabaseSynchronize,
            AllowLibrarySynchronize = dto.AllowLibrarySynchronize,
            NameIsReadOnly = dto.NameIsReadOnly,
            PhysicalDesignator = dto.PhysicalDesignator,
            ValueIsReadOnly = dto.ValueIsReadOnly,
            VariantOption = dto.VariantOption,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchTextFrame CreateTextFrame(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchTextFrameDto.FromParameters(paramCollection);

        return new SchTextFrame
        {
            Text = dto.Text ?? string.Empty,
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            Orientation = dto.Orientation,
            Alignment = (TextJustification)dto.Alignment,
            FontId = dto.FontId,
            TextColor = dto.TextColor,
            BorderColor = dto.Color,
            FillColor = dto.AreaColor,
            LineWidth = dto.LineWidth,
            LineStyle = dto.LineStyle,
            TextMargin = dto.TextMargin,
            IsFilled = dto.IsSolid,
            IsTransparent = dto.Transparent,
            ShowBorder = dto.ShowBorder,
            WordWrap = dto.WordWrap,
            ClipToRect = dto.ClipToRect,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchImage CreateImage(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchImageDto.FromParameters(paramCollection);

        return new SchImage
        {
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            KeepAspect = dto.KeepAspect,
            EmbedImage = dto.EmbedImage,
            Filename = dto.FileName,
            BorderColor = dto.Color,
            LineWidth = dto.LineWidth,
            AreaColor = dto.AreaColor,
            IsSolid = dto.IsSolid,
            LineStyle = dto.LineStyle,
            IsTransparent = dto.Transparent,
            ShowBorder = dto.ShowBorder,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchSymbol CreateSymbol(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchSymbolDto.FromParameters(paramCollection);

        return new SchSymbol
        {
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            SymbolType = dto.Symbol,
            IsMirrored = dto.IsMirrored,
            Orientation = dto.Orientation,
            LineWidth = dto.LineWidth,
            ScaleFactor = dto.ScaleFactor != 0 ? dto.ScaleFactor : 1,
            Color = dto.Color,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchEllipticalArc CreateEllipticalArc(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchEllipticalArcDto.FromParameters(paramCollection);

        return new SchEllipticalArc
        {
            Center = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            PrimaryRadius = CoordFromDxp(dto.Radius, dto.RadiusFrac),
            SecondaryRadius = CoordFromDxp(dto.SecondaryRadius, dto.SecondaryRadiusFrac),
            StartAngle = dto.StartAngle,
            EndAngle = dto.EndAngle,
            LineWidth = CoordFromDxp(dto.LineWidth, dto.LineWidthFrac),
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchPowerObject CreatePowerObject(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchPowerObjectDto.FromParameters(paramCollection);

        return new SchPowerObject
        {
            Text = dto.Text ?? string.Empty,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Style = (PowerPortStyle)dto.Style,
            Rotation = dto.Orientation * 90.0,
            ShowNetName = dto.ShowNetName,
            IsCrossSheetConnector = dto.IsCrossSheetConnector,
            Color = dto.Color,
            FontId = dto.FontId,
            AreaColor = dto.AreaColor,
            IsCustomStyle = dto.IsCustomStyle,
            IsMirrored = dto.IsMirrored,
            Justification = dto.Justification,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchNoErc CreateNoErc(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchNoErcDto.FromParameters(paramCollection);

        return new SchNoErc
        {
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Orientation = dto.Orientation,
            Color = dto.Color,
            IsActive = dto.IsActive,
            Symbol = dto.Symbol,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchBusEntry CreateBusEntry(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchBusEntryDto.FromParameters(paramCollection);

        return new SchBusEntry
        {
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            LineWidth = dto.LineWidth,
            Color = dto.Color,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchBus CreateBus(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchBusDto.FromParameters(paramCollection);

        var bus = new SchBus
        {
            LineWidth = dto.LineWidth,
            LineStyle = dto.LineStyle,
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };

        // Read vertices (same pattern as Wire)
        var vertexCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(vertexCount, 10); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= vertexCount)
            {
                bus.AddVertex(new CoordPoint(hasX ? x : default, hasY ? y : default));
            }
            else break;
        }

        return bus;
    }

    private static SchPort CreatePort(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchPortDto.FromParameters(paramCollection);

        return new SchPort
        {
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Name = dto.Name ?? string.Empty,
            IoType = dto.IoType,
            Style = dto.Style,
            Alignment = dto.Alignment,
            Width = CoordFromDxp(dto.Width),
            Height = CoordFromDxp(dto.Height),
            BorderWidth = dto.BorderWidth,
            AutoSize = dto.AutoSize,
            ConnectedEnd = dto.ConnectedEnd,
            CrossReference = dto.CrossReference,
            ShowNetName = dto.ShowNetName,
            HarnessType = dto.HarnessType,
            HarnessColor = dto.HarnessColor,
            IsCustomStyle = dto.IsCustomStyle,
            FontId = dto.FontId,
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            TextColor = dto.TextColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchSheetSymbol CreateSheetSymbol(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchSheetSymbolDto.FromParameters(paramCollection);

        return new SchSheetSymbol
        {
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            XSize = CoordFromDxp(dto.XSize),
            YSize = CoordFromDxp(dto.YSize),
            IsMirrored = dto.IsMirrored,
            FileName = dto.FileName,
            SheetName = dto.SheetName,
            LineWidth = dto.LineWidth,
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            IsSolid = dto.IsSolid,
            ShowHiddenFields = dto.ShowHiddenFields,
            SymbolType = dto.SymbolType,
            DesignItemId = dto.DesignItemId,
            ItemGuid = dto.ItemGuid,
            LibIdentifierKind = dto.LibIdentifierKind,
            LibraryIdentifier = dto.LibraryIdentifier,
            RevisionGuid = dto.RevisionGuid,
            SourceLibraryName = dto.SourceLibraryName,
            VaultGuid = dto.VaultGuid,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchSheetEntry CreateSheetEntry(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchSheetEntryDto.FromParameters(paramCollection);

        return new SchSheetEntry
        {
            Side = dto.Side,
            DistanceFromTop = CoordFromDxp(dto.DistanceFromTop),
            Name = dto.Name ?? string.Empty,
            IoType = dto.IoType,
            Style = dto.Style,
            ArrowKind = dto.ArrowKind,
            HarnessType = dto.HarnessType,
            HarnessColor = dto.HarnessColor,
            FontId = dto.FontId,
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            TextColor = dto.TextColor,
            TextStyle = dto.TextStyle,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchBlanket CreateBlanket(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchBlanketDto.FromParameters(paramCollection);

        var blanket = new SchBlanket
        {
            IsCollapsed = dto.IsCollapsed,
            LineWidth = dto.LineWidth,
            LineStyle = dto.LineStyle,
            IsSolid = dto.IsSolid,
            IsTransparent = dto.Transparent,
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };

        var vertexCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(vertexCount, 50); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= vertexCount)
            {
                blanket.AddVertex(new CoordPoint(hasX ? x : default, hasY ? y : default));
            }
            else break;
        }

        return blanket;
    }

    private static SchParameterSet CreateParameterSet(Dictionary<string, string> parameters)
    {
        var paramCollection = ToParameterCollection(parameters);
        var dto = SchParameterSetDto.FromParameters(paramCollection);

        return new SchParameterSet
        {
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Orientation = dto.Orientation,
            Style = dto.Style,
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            Name = dto.Name,
            ShowHiddenFields = dto.ShowHiddenFields,
            BorderWidth = dto.BorderWidth,
            IsSolid = dto.IsSolid,
            OwnerIndex = dto.OwnerIndex,
            IsNotAccessible = dto.IsNotAccessible,
            IndexInSheet = dto.IndexInSheet,
            OwnerPartId = dto.OwnerPartId,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            GraphicallyLocked = dto.GraphicallyLocked,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            UniqueId = dto.UniqueId
        };
    }

    private static SchImplementation CreateImplementation(Dictionary<string, string> parameters)
    {
        var impl = new SchImplementation
        {
            Description = TryGetString(parameters, "DESCRIPTION"),
            ModelName = TryGetString(parameters, "MODELNAME"),
            ModelType = TryGetString(parameters, "MODELTYPE"),
            IsCurrent = TryGetBool(parameters, "ISCURRENT"),
            OwnerIndex = TryGetInt(parameters, "OWNERINDEX"),
            IsNotAccessible = TryGetBool(parameters, "ISNOTACCESIBLE"),
            IndexInSheet = TryGetInt(parameters, "INDEXINSHEET"),
            OwnerPartId = TryGetInt(parameters, "OWNERPARTID"),
            OwnerPartDisplayMode = TryGetInt(parameters, "OWNERPARTDISPLAYMODE"),
            GraphicallyLocked = TryGetBool(parameters, "GRAPHICALLYLOCKED"),
            Disabled = TryGetBool(parameters, "DISABLED"),
            Dimmed = TryGetBool(parameters, "DIMMED"),
            UniqueId = TryGetString(parameters, "UNIQUEID")
        };

        var dataFileCount = TryGetInt(parameters, "DATAFILECOUNT");
        for (var i = 1; i <= dataFileCount; i++)
        {
            var kind = TryGetString(parameters, $"MODELDATAFILEKIND{i}");
            if (kind != null)
                impl.DataFileKinds.Add(kind);
        }

        return impl;
    }

    private static SchMapDefiner CreateMapDefiner(Dictionary<string, string> parameters)
    {
        var mapDefiner = new SchMapDefiner
        {
            DesignatorInterface = TryGetString(parameters, "DESINTF"),
            IsTrivial = TryGetBool(parameters, "ISTRIVIAL"),
            OwnerIndex = TryGetInt(parameters, "OWNERINDEX"),
            IsNotAccessible = TryGetBool(parameters, "ISNOTACCESIBLE"),
            IndexInSheet = TryGetInt(parameters, "INDEXINSHEET"),
            OwnerPartId = TryGetInt(parameters, "OWNERPARTID"),
            OwnerPartDisplayMode = TryGetInt(parameters, "OWNERPARTDISPLAYMODE"),
            GraphicallyLocked = TryGetBool(parameters, "GRAPHICALLYLOCKED"),
            Disabled = TryGetBool(parameters, "DISABLED"),
            Dimmed = TryGetBool(parameters, "DIMMED"),
            UniqueId = TryGetString(parameters, "UNIQUEID")
        };

        var desImpCount = TryGetInt(parameters, "DESIMPCOUNT");
        for (var i = 0; i < desImpCount; i++)
        {
            var imp = TryGetString(parameters, $"DESIMP{i}");
            if (imp != null)
                mapDefiner.DesignatorImplementations.Add(imp);
        }

        return mapDefiner;
    }

    private static string? TryGetString(Dictionary<string, string> parameters, string key) =>
        parameters.TryGetValue(key, out var value) ? value : null;

    private static int TryGetInt(Dictionary<string, string> parameters, string key) =>
        parameters.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : 0;

    private static bool TryGetBool(Dictionary<string, string> parameters, string key) =>
        parameters.TryGetValue(key, out var value) && string.Equals(value, "T", StringComparison.OrdinalIgnoreCase);

    private static void AddPrimitiveToComponent(SchComponent component, object primitive)
    {
        switch (primitive)
        {
            case SchPin pin:
                component.AddPin(pin);
                break;
            case SchLine line:
                component.AddLine(line);
                break;
            case SchRectangle rect:
                component.AddRectangle(rect);
                break;
            case SchLabel label:
                component.AddLabel(label);
                break;
            case SchWire wire:
                component.AddWire(wire);
                break;
            case SchPolyline polyline:
                component.AddPolyline(polyline);
                break;
            case SchPolygon polygon:
                component.AddPolygon(polygon);
                break;
            case SchArc arc:
                component.AddArc(arc);
                break;
            case SchBezier bezier:
                component.AddBezier(bezier);
                break;
            case SchEllipse ellipse:
                component.AddEllipse(ellipse);
                break;
            case SchRoundedRectangle roundedRect:
                component.AddRoundedRectangle(roundedRect);
                break;
            case SchPie pie:
                component.AddPie(pie);
                break;
            case SchNetLabel netLabel:
                component.AddNetLabel(netLabel);
                break;
            case SchJunction junction:
                component.AddJunction(junction);
                break;
            case SchParameter param:
                component.AddParameter(param);
                break;
            case SchTextFrame textFrame:
                component.AddTextFrame(textFrame);
                break;
            case SchImage image:
                component.AddImage(image);
                break;
            case SchSymbol symbol:
                component.AddSymbol(symbol);
                break;
            case SchEllipticalArc ellipticalArc:
                component.AddEllipticalArc(ellipticalArc);
                break;
            case SchPowerObject powerObject:
                component.AddPowerObject(powerObject);
                break;
            case SchImplementation implementation:
                component.AddImplementation(implementation);
                break;
        }
    }

    /// <summary>
    /// Converts a DXP coordinate value to raw internal units.
    /// DXP units are 10-mil increments; 1 mil = 10,000 raw units.
    /// So 1 DXP = 100,000 raw. The optional frac parameter adds sub-DXP precision.
    /// </summary>
    private static Coord CoordFromDxp(int dxpValue, int frac = 0) => Coord.FromRaw(dxpValue * 100_000 + frac);

    // Line width values in internal units
    // These correspond to Small (0), Medium (1), Large (2) line styles
    private static readonly Coord[] LineWidths =
    {
        Coord.FromMils(1),   // Small
        Coord.FromMils(2),   // Medium
        Coord.FromMils(4)    // Large
    };

    private static Coord LineWidthFromIndex(int index)
    {
        return index >= 0 && index < LineWidths.Length
            ? LineWidths[index]
            : LineWidths[0];
    }

    private static bool TryParseCoord(string value, out Coord result)
    {
        result = default;

        if (string.IsNullOrEmpty(value))
            return false;

        // Schematic uses 10 units per mil (different from PCB's 10000)
        if (int.TryParse(value, out var intValue))
        {
            // Convert from schematic units to internal units
            result = Coord.FromRaw(intValue * 1000); // Scale up
            return true;
        }

        return false;
    }
}
