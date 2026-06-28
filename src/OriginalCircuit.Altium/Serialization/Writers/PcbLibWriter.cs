using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using OriginalCircuit.Altium.Serialization.Compound;

namespace OriginalCircuit.Altium.Serialization.Writers;

/// <summary>
/// Writes PCB footprint library (.PcbLib) files.
/// </summary>
public sealed class PcbLibWriter
{
    /// <summary>
    /// Writes a PcbLib file to the specified path.
    /// </summary>
    /// <param name="library">The PCB footprint library to write.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="overwrite">If true, overwrites an existing file; otherwise throws if the file exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public async ValueTask WriteAsync(PcbLibrary library, string path, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        await using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await WriteAsync(library, stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a PcbLib file to a stream.
    /// </summary>
    /// <param name="library">The PCB footprint library to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public async ValueTask WriteAsync(PcbLibrary library, Stream stream, CancellationToken cancellationToken = default)
    {
        // Write synchronously to memory, then copy to output stream
        using var ms = new MemoryStream();
        Write(library, ms, cancellationToken);
        ms.Position = 0;
        await ms.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a PcbLib file to a stream synchronously.
    /// </summary>
    /// <param name="library">The PCB footprint library to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public void Write(PcbLibrary library, Stream stream, CancellationToken cancellationToken = default)
    {
        using var cf = CompoundFileAccessor.Create();
        var sectionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        WriteFileHeader(cf, library);
        WriteSectionKeys(cf, library, sectionKeys);
        WriteLibrary(cf, library, sectionKeys, cancellationToken);
        WriteFileVersionInfo(cf.RootStorage, library.FileVersionInfo);
        WriteAdditionalRootStreams(cf, library);

        cf.Save(stream);
    }

    private static void WriteFileHeader(CompoundFileAccessor cf, PcbLibrary library)
    {
        var headerStream = cf.RootStorage.AddStream("FileHeader");

        // Fully modeled — no replay. The 53-byte FileHeader is two pascal-string blocks with the
        // 5.01 version double between them (docs/decompile/fileheaders.md §1). Everything except the
        // 8-char UniqueId is a fixed constant; UniqueId is the library's Identity (round-tripped on
        // read, freshly generated for a new library by PcbLibrary.UniqueId's default).
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        const string versionText = "PCB 6.0 Binary Library File";   // Reserved constant
        writer.Write(versionText.Length);
        writer.WritePascalShortString(versionText);

        writer.Write(5.01d);                                        // Reserved constant (no length prefix)

        // 8-character unique library identifier as a string block: [4-byte char count][pascal-string].
        var uniqueId = string.IsNullOrEmpty(library.UniqueId) ? PcbLibrary.GenerateUniqueId() : library.UniqueId;
        writer.Write(uniqueId.Length);
        writer.WritePascalShortString(uniqueId);

        writer.Flush();
        headerStream.SetData(ms.ToArray());
    }

    private static void WriteSectionKeys(CompoundFileAccessor cf, PcbLibrary library, Dictionary<string, string> sectionKeys)
    {
        // Use preserved section keys if available, otherwise generate new ones
        if (library.SectionKeys != null && library.SectionKeys.Count > 0)
        {
            foreach (var kvp in library.SectionKeys)
                sectionKeys[kvp.Key] = kvp.Value;
        }

        // Build section keys for components that need them
        var componentsNeedingKeys = new List<IPcbComponent>();
        foreach (var component in library.Components)
        {
            if (sectionKeys.ContainsKey(component.Name))
            {
                componentsNeedingKeys.Add(component);
            }
            else
            {
                var sectionKey = GetSectionKeyFromName(component.Name);
                if (sectionKey != component.Name)
                {
                    sectionKeys[component.Name] = sectionKey;
                    componentsNeedingKeys.Add(component);
                }
            }
        }

        if (componentsNeedingKeys.Count == 0)
            return;

        var sectionKeysStream = cf.RootStorage.AddStream("SectionKeys");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        writer.Write(componentsNeedingKeys.Count);
        foreach (var component in componentsNeedingKeys)
        {
            // Name and key are both string blocks with no trailing null (WritePascalString would
            // append one, growing the stream by one byte per entry).
            writer.WriteStringBlock(component.Name);
            writer.WriteStringBlock(sectionKeys[component.Name]);
        }

        writer.Flush();
        sectionKeysStream.SetData(ms.ToArray());
    }

    private static void WriteLibrary(CompoundFileAccessor cf, PcbLibrary library, Dictionary<string, string> sectionKeys, CancellationToken cancellationToken = default)
    {
        var libraryStorage = cf.RootStorage.AddStorage("Library");

        // Write header (record count)
        WriteStorageHeader(libraryStorage, 1);

        // Write library data
        WriteLibraryData(cf, libraryStorage, library, sectionKeys, cancellationToken);

        // Write models
        WriteLibraryModels(libraryStorage, library);

        // Write the modeled library metadata storages (emitted for from-scratch libraries too).
        WriteLayerKindMapping(libraryStorage, library);
        WritePadViaLibrary(libraryStorage, library);
        WriteComponentParamsToc(libraryStorage, library);

        // Reproduce the always-empty library sub-storages (Textures, ModelsNoEmbed) explicitly rather
        // than replaying them from AdditionalLibraryStreams, so from-scratch libraries carry them.
        foreach (var name in library.EmptyLibrarySubStorages)
        {
            var emptyStorage = libraryStorage.AddStorage(name);
            WriteStorageHeader(emptyStorage, 0);
            emptyStorage.AddStream("Data").SetData([]);
        }

        // Write additional library-level streams (ComponentParamsTOC, EmbeddedFonts, etc.)
        WriteAdditionalLibraryStreams(libraryStorage, library);
    }

    private static void WriteLayerKindMapping(CompoundStorage libraryStorage, PcbLibrary library)
        => WriteLayerKindMapping(libraryStorage, library.LayerKindMapping);

    internal static void WriteLayerKindMapping(CompoundStorage parentStorage, PcbLayerKindMapping lkm)
    {
        var storage = parentStorage.AddStorage("LayerKindMapping");
        WriteStorageHeader(storage, 1);
        using var ms = new MemoryStream();
        var textBytes = System.Text.Encoding.Unicode.GetBytes((lkm.FormatVersion ?? "1.0") + '\0');
        ms.Write(BitConverter.GetBytes(textBytes.Length));
        ms.Write(textBytes);
        // [u32 signature][u32 count][count × (u32 layerId, u32 kind)]. PcbLib emits 0/0 (the 8 zero
        // bytes); PcbDoc emits the typed mapping table. Preserve a captured (non-zero) signature
        // verbatim for byte-exact round-trip; for a from-scratch table compute it (MurmurHash2).
        var signature = lkm.Signature != 0 ? lkm.Signature : lkm.ComputeSignature();
        ms.Write(BitConverter.GetBytes(signature));
        ms.Write(BitConverter.GetBytes(lkm.Entries.Count));
        foreach (var e in lkm.Entries)
        {
            ms.Write(BitConverter.GetBytes(e.LayerId));
            ms.Write(BitConverter.GetBytes(e.Kind));
        }
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    private static void WriteComponentParamsToc(CompoundStorage libraryStorage, PcbLibrary library)
    {
        // Rebuild from the captured TOC entries (byte-exact); from-scratch libraries derive entries
        // from their components. Each footprint is "Name=..|Pad Count=..|Height=..|Description=..\r\n",
        // all concatenated into one NUL-terminated chunk.
        var entries = library.ComponentParamsToc;
        if (entries.Count == 0)
        {
            foreach (var c in library.Components)
                entries.Add(new PcbComponentParamsTocEntry
                {
                    Name = c.Name,
                    PadCount = c.Pads.Count,
                    Height = "0",
                    Description = c.Description ?? string.Empty,
                });
        }
        if (entries.Count == 0) return;

        var sb = new System.Text.StringBuilder();
        foreach (var e in entries)
            sb.Append("Name=").Append(e.Name).Append("|Pad Count=").Append(e.PadCount.ToString(System.Globalization.CultureInfo.InvariantCulture))
              .Append("|Height=").Append(e.Height).Append("|Description=").Append(e.Description).Append("\r\n");
        var payload = AltiumEncoding.Windows1252.GetBytes(sb.ToString() + '\0');

        var storage = libraryStorage.AddStorage("ComponentParamsTOC");
        WriteStorageHeader(storage, 1);
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(payload.Length));
        ms.Write(payload);
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    private static void WritePadViaLibrary(CompoundStorage libraryStorage, PcbLibrary library)
        => WritePadViaLibrary(libraryStorage, library.PadViaLibrary, "PadViaLibrary");

    internal static void WritePadViaLibrary(CompoundStorage parentStorage, PcbPadViaLibrary pvl, string storageName)
    {
        var storage = parentStorage.AddStorage(storageName);
        WriteStorageHeader(storage, pvl.HeaderCount);   // 0 for PadViaLibrary; template count for a populated cache

        string text;
        if (pvl.OrderedParameters is { Count: > 0 } ordered)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in ordered) sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
            text = sb.ToString();
        }
        else
        {
            text = $"|PADVIALIBRARY.LIBRARYID={{{pvl.LibraryId.ToString("D").ToUpperInvariant()}}}" +
                   $"|PADVIALIBRARY.LIBRARYNAME={pvl.LibraryName}" +
                   $"|PADVIALIBRARY.DISPLAYUNITS={pvl.DisplayUnits.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }
        var payload = AltiumEncoding.Windows1252.GetBytes(text + '\0');
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(payload.Length));
        ms.Write(payload);
        if (pvl.TemplateCache is { Length: > 0 } cache) ms.Write(cache);   // opaque binary cache tail
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    internal static void WriteStorageHeader(CompoundStorage storage, int recordCount)
    {
        var headerStream = storage.AddStream("Header");
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(recordCount);
        writer.Flush();
        headerStream.SetData(ms.ToArray());
    }

    private static void WriteLibraryData(CompoundFileAccessor cf, CompoundStorage libraryStorage, PcbLibrary library, Dictionary<string, string> sectionKeys, CancellationToken cancellationToken = default)
    {
        var dataStream = libraryStorage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Generate library parameters from dictionary or defaults
        if (library.LibraryParametersOrdered is { Count: > 0 } ordered)
        {
            // The header is an ordered parameter list with duplicate RECORD=Board markers;
            // serialize it verbatim from the ordered model (no WEIGHT key is injected — the
            // PcbLib library header does not carry one; the footprint count follows separately).
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in ordered)
                sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
            writer.WriteCStringParameterBlockRaw(sb.ToString());
        }
        else if (library.LibraryParameters != null && library.LibraryParameters.Count > 0)
        {
            var headerParams = new Dictionary<string, string>(library.LibraryParameters, StringComparer.OrdinalIgnoreCase);
            headerParams["WEIGHT"] = library.Components.Count.ToString();
            writer.WriteCStringParameterBlock(headerParams);
        }
        else
        {
            var headerParams = new Dictionary<string, string>
            {
                ["HEADER"] = "PCB 6.0 Binary Library File",
                ["WEIGHT"] = library.Components.Count.ToString()
            };
            writer.WriteCStringParameterBlock(headerParams);
        }

        // Write component count and names
        writer.Write((uint)library.Components.Count);
        foreach (var component in library.Components.Cast<PcbComponent>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.WriteStringBlock(component.Name);
            WriteFootprint(cf, component, sectionKeys);
        }

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    private static void WriteFootprint(CompoundFileAccessor cf, PcbComponent component, Dictionary<string, string> sectionKeys)
    {
        var sectionKey = sectionKeys.TryGetValue(component.Name, out var key)
            ? key
            : GetSectionKeyFromName(component.Name);

        var footprintStorage = cf.RootStorage.AddStorage(sectionKey);

        // Write header (primitive count)
        var primitiveCount = component.Pads.Count + component.Tracks.Count +
                            component.Vias.Count + component.Arcs.Count +
                            component.Texts.Count + component.Fills.Count +
                            component.Regions.Count + component.ComponentBodies.Count;
        WriteStorageHeader(footprintStorage, primitiveCount);

        WriteFootprintParameters(footprintStorage, component);
        WriteWideStrings(footprintStorage, component);
        WriteFootprintData(footprintStorage, component);
        WriteUniqueIdPrimitiveInformation(footprintStorage, component);
        WritePrimitiveGuids(footprintStorage, component);
        WriteAdditionalComponentStreams(footprintStorage, component);
    }

    internal static void WritePrimitiveGuids(CompoundStorage parentStorage, PcbComponent component)
    {
        if (component.PrimitiveGuids.Count == 0) return;   // from-scratch footprints omit the cache
        var storage = parentStorage.AddStorage("PrimitiveGuids");
        WriteStorageHeader(storage, component.PrimitiveGuids.Count);
        using var ms = new MemoryStream();
        foreach (var g in component.PrimitiveGuids)
        {
            ms.Write(BitConverter.GetBytes(g.TypeId));
            ms.Write(BitConverter.GetBytes(g.Index));
            ms.Write(g.Guid.ToByteArray());
        }
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    private static void WriteFootprintParameters(CompoundStorage storage, PcbComponent component)
    {
        var paramsStream = storage.AddStream("Parameters");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Generate parameters from typed properties
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Merge any additional parameters first (typed properties override)
        if (component is PcbComponent { AdditionalParameters: not null } pcbComp)
        {
            foreach (var kvp in pcbComp.AdditionalParameters)
                parameters[kvp.Key] = kvp.Value;
        }
        parameters["PATTERN"] = component.Name;
        // Altium serializes HEIGHT as a mil-suffixed coordinate string (e.g. "0mil"),
        // not a raw integer, with trailing zeros trimmed.
        parameters["HEIGHT"] = FormatMilCoord(component.Height);
        // Emit DESCRIPTION whenever it was present — Altium keeps an empty "DESCRIPTION=" entry, and
        // the reader leaves Description null only when the key was absent (empty string = present but
        // blank). Writing only on non-empty would drop a blank DESCRIPTION and break byte fidelity.
        if (component.Description != null)
            parameters["DESCRIPTION"] = component.Description;
        // Altium always emits ITEMGUID and REVISIONGUID, even when empty.
        parameters["ITEMGUID"] = component.ItemGUID ?? "";
        parameters["REVISIONGUID"] = component.ItemRevisionGUID ?? "";
        writer.WriteCStringParameterBlock(parameters);

        writer.Flush();
        paramsStream.SetData(ms.ToArray());
    }

    /// <summary>
    /// Formats a coordinate as an Altium mil-suffixed string with trailing zeros trimmed
    /// (e.g. "0mil", "19.685mil"), matching how Altium serializes PcbLib component HEIGHT.
    /// </summary>
    private static string FormatMilCoord(Coord coord)
        => coord.ToMils().ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) + "mil";

    // Reassembles a region's nested parameter block in Altium's canonical key order (no leading '|'),
    // from the region's typed fields. Optional keys (KEEPOUTRESTRICTIONS, PADINDEX) are emitted only
    // when present; any forward-compat AdditionalParameters follow.
    private static string BuildRegionParamText(PcbRegion region)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var pairs = new List<KeyValuePair<string, string>>();
        void Add(string k, string v) => pairs.Add(new KeyValuePair<string, string>(k, v));

        string B(bool x) => x ? "TRUE" : "FALSE";
        Add("V7_LAYER", region.V7LayerName ?? PcbDocWriter.LayerByteToName(region.Layer));
        Add("NAME", region.Name ?? string.Empty);
        if (region.BoardRegionLayer is { } brl) Add("LAYER", brl);
        if (region.BoardRegionKeepout is { } brk) Add("KEEPOUT", B(brk));
        if (region.IsBoardCutout is { } ibc) Add("ISBOARDCUTOUT", B(ibc));
        Add("KIND", region.Kind.ToString(ci));
        Add("SUBPOLYINDEX", region.SubPolyIndex.ToString(ci));
        Add("UNIONINDEX", region.UnionIndex.ToString(ci));
        Add("ARCRESOLUTION", FormatMilCoord(region.ArcApproximation));
        Add("ISSHAPEBASED", region.IsShapeBased ? "TRUE" : "FALSE");
        Add("CAVITYHEIGHT", FormatMilCoord(region.CavityHeight));
        if (region.KeepoutRestrictions is { } kor) Add("KEEPOUTRESTRICTIONS", kor.ToString(ci));
        if (region.PadIndex is { } pix) Add("PADINDEX", pix.ToString(ci));
        if (region.ObjectKind is { } ok) Add("OBJECTKIND", ok);
        if (region.BendingLineCount is { } blc) Add("BENDINGLINECOUNT", blc.ToString(ci));
        if (region.Locked3D is { } l3d) Add("LOCKED3D", B(l3d));
        if (region.LayerStackId is { } lsid) Add("LAYERSTACKID", lsid);
        if (region.AdditionalParameters != null)
            foreach (var kvp in region.AdditionalParameters) Add(kvp.Key, kvp.Value);

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < pairs.Count; i++)
        {
            if (i > 0) sb.Append('|');
            sb.Append(pairs[i].Key).Append('=').Append(pairs[i].Value);
        }
        return sb.ToString();
    }

    // Reassembles a component-body nested parameter block in Altium's canonical key order (no leading
    // '|'), from typed fields. Reproduces the duplicate ARCRESOLUTION key, the MODEL.* placement block,
    // and the model-type-dependent tail (MODELSOURCE for linked models, EXTRUDED.MIN/MAXZ for extruded).
    private static string BuildComponentBodyParamText(PcbComponentBody body)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var pairs = new List<KeyValuePair<string, string>>();
        void Add(string k, string v) => pairs.Add(new KeyValuePair<string, string>(k, v));
        string B(bool x) => x ? "TRUE" : "FALSE";
        string F3(double v) => v.ToString("0.000", ci);

        Add("V7_LAYER", body.LayerName ?? "MECHANICAL1");
        Add("NAME", body.Name ?? string.Empty);
        Add("KIND", body.Kind.ToString(ci));
        Add("SUBPOLYINDEX", body.SubPolyIndex.ToString(ci));
        Add("UNIONINDEX", body.UnionIndex.ToString(ci));
        Add("ARCRESOLUTION", FormatMilCoord(body.ArcResolutionPrefix));
        Add("ISSHAPEBASED", B(body.IsShapeBased));
        Add("CAVITYHEIGHT", FormatMilCoord(body.CavityHeight));
        Add("STANDOFFHEIGHT", FormatMilCoord(body.StandoffHeight));
        Add("OVERALLHEIGHT", FormatMilCoord(body.OverallHeight));
        Add("BODYPROJECTION", body.BodyProjection.ToString(ci));
        Add("ARCRESOLUTION", FormatMilCoord(body.ArcResolutionBody));
        Add("BODYCOLOR3D", body.BodyColor3D.ToString(ci));
        Add("BODYOPACITY3D", F3(body.BodyOpacity3D));
        Add("IDENTIFIER", EncodeBodyAsciiCodes(body.Identifier));
        Add("TEXTURE", body.Texture ?? string.Empty);
        Add("TEXTURECENTERX", FormatMilCoord(body.TextureCenterX));
        Add("TEXTURECENTERY", FormatMilCoord(body.TextureCenterY));
        Add("TEXTURESIZEX", FormatMilCoord(body.TextureSizeX));
        Add("TEXTURESIZEY", FormatMilCoord(body.TextureSizeY));
        Add("TEXTUREROTATION", PcbDocWriter.DelphiExp(body.TextureRotation));
        Add("MODELID", body.ModelId ?? string.Empty);
        Add("MODEL.CHECKSUM", body.ModelChecksum.ToString(ci));
        Add("MODEL.EMBED", B(body.ModelEmbed));
        Add("MODEL.NAME", body.ModelName ?? string.Empty);
        Add("MODEL.2D.X", FormatMilCoord(body.Model2DLocation.X));
        Add("MODEL.2D.Y", FormatMilCoord(body.Model2DLocation.Y));
        Add("MODEL.2D.ROTATION", F3(body.Model2DRotation));
        Add("MODEL.3D.ROTX", F3(body.Model3DRotX));
        Add("MODEL.3D.ROTY", F3(body.Model3DRotY));
        Add("MODEL.3D.ROTZ", F3(body.Model3DRotZ));
        Add("MODEL.3D.DZ", FormatMilCoord(body.Model3DDz));
        if (body.SnapCount is { } snapCount)
        {
            Add("MODEL.SNAPCOUNT", snapCount.ToString(ci));
            for (var i = 0; i < body.SnapPoints.Count; i++)
            {
                var sp = body.SnapPoints[i];
                Add($"MODEL.S{i}X", sp.X.ToString(ci));
                Add($"MODEL.S{i}Y", sp.Y.ToString(ci));
                Add($"MODEL.S{i}Z", sp.Z.ToString(ci));
            }
        }
        Add("MODEL.MODELTYPE", body.ModelType.ToString(ci));
        if (body.ModelSource is { } ms) Add("MODEL.MODELSOURCE", ms);
        if (body.ModelExtrudedMinZ is { } minZ) Add("MODEL.EXTRUDED.MINZ", FormatMilCoord(minZ));
        if (body.ModelExtrudedMaxZ is { } maxZ) Add("MODEL.EXTRUDED.MAXZ", FormatMilCoord(maxZ));
        if (body.AdditionalParameters != null)
            foreach (var kvp in body.AdditionalParameters) Add(kvp.Key, kvp.Value);

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < pairs.Count; i++)
        {
            if (i > 0) sb.Append('|');
            sb.Append(pairs[i].Key).Append('=').Append(pairs[i].Value);
        }
        return sb.ToString();
    }

    // Encodes a string as the comma-separated codepoint list Altium stores for IDENTIFIER (empty -> "").
    private static string EncodeBodyAsciiCodes(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(((int)s[i]).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static void WriteWideStrings(CompoundStorage storage, PcbComponent component)
    {
        var wideStringsStream = storage.AddStream("WideStrings");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        var parameters = new Dictionary<string, string>();
        var textIndex = 0;

        foreach (var text in component.Texts)
        {
            var encoded = string.Join(",", text.Text.Select(c => ((int)c).ToString()));
            parameters[$"ENCODEDTEXT{textIndex}"] = encoded;
            textIndex++;
        }

        writer.WriteCStringParameterBlock(parameters);

        writer.Flush();
        wideStringsStream.SetData(ms.ToArray());
    }

    private static void WriteFootprintData(CompoundStorage storage, PcbComponent component)
    {
        var dataStream = storage.AddStream("Data");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Write pattern name
        writer.WriteStringBlock(component.Name);

        // Write primitives
        foreach (var arc in component.Arcs)
        {
            writer.Write((byte)1); // Arc object ID
            WriteArc(writer, (PcbArc)arc);
        }

        foreach (var pad in component.Pads)
        {
            writer.Write((byte)2); // Pad object ID
            WritePad(writer, (PcbPad)pad);
        }

        foreach (var via in component.Vias)
        {
            writer.Write((byte)3); // Via object ID
            WriteVia(writer, (PcbVia)via);
        }

        foreach (var track in component.Tracks)
        {
            writer.Write((byte)4); // Track object ID
            WriteTrack(writer, (PcbTrack)track);
        }

        var textIndex = 0;
        foreach (var text in component.Texts)
        {
            writer.Write((byte)5); // Text object ID
            WriteText(writer, (PcbText)text, textIndex++);
        }

        foreach (var fill in component.Fills)
        {
            writer.Write((byte)6); // Fill object ID
            WriteFill(writer, (PcbFill)fill);
        }

        foreach (var region in component.Regions)
        {
            writer.Write((byte)11); // Region object ID
            WriteRegion(writer, (PcbRegion)region);
        }

        foreach (var body in component.ComponentBodies)
        {
            writer.Write((byte)12); // ComponentBody object ID
            WriteComponentBody(writer, (PcbComponentBody)body);
        }

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    internal static void WriteCommonPrimitiveData(BinaryFormatWriter writer, int layer, ushort flags = 0,
        ushort netIndex = 0xFFFF, int componentIndex = -1, ushort polygonIndex = 0xFFFF)
    {
        writer.Write((byte)layer);
        writer.Write(flags);
        writer.Write(netIndex);                                                       // net index (0xFFFF = none)
        writer.Write(polygonIndex);                                                    // polygon index (0xFFFF = none, 0 for regions)
        writer.Write(componentIndex < 0 ? (ushort)0xFFFF : (ushort)componentIndex);    // component index (0xFFFF = none)
        writer.Write(0xFFFFFFFFu);                                                     // reserved
    }

    /// <summary>
    /// Encodes boolean properties into the flags word. All bits are computed from properties.
    /// </summary>
    internal static ushort EncodeFlags(bool isLocked, bool isTentingTop,
        bool isTentingBottom, bool isKeepout)
    {
        ushort flags = PcbBinaryConstants.FlagSaved; // bit 3: always set on saved primitives
        if (!isLocked)
            flags |= PcbBinaryConstants.FlagUnlocked;
        if (isTentingTop)
            flags |= PcbBinaryConstants.FlagTentingTop;
        if (isTentingBottom)
            flags |= PcbBinaryConstants.FlagTentingBottom;
        if (isKeepout)
            flags |= PcbBinaryConstants.FlagKeepout;
        return flags;
    }

    /// <summary>
    /// Derives the "v7 saved layer id" (uint32) from the Altium layer number. This redundant field
    /// is written into primitive tails and must match the primitive's layer. Encoding (verified
    /// against the corpus and altium_monkey's PcbV7LayerPartition):
    /// 0x0100_0000+layer (signal 1-32), 0x0101_0000+(layer-38) (internal plane 39-54),
    /// 0x0102_0000+(layer-56) (mechanical 57-72), 0x0103_0000+partition (special layers).
    /// </summary>
    internal static uint V7LayerId(int layer)
    {
        if (layer == 32) return 0x0100FFFFu;                                      // bottom signal layer (sentinel)
        if (layer >= 1 && layer <= 31) return 0x01000000u + (uint)layer;          // signal (top/mid)
        if (layer >= 39 && layer <= 54) return 0x01010000u + (uint)(layer - 38);  // internal plane 1-16
        if (layer >= 57 && layer <= 72) return 0x01020000u + (uint)(layer - 56);  // mechanical 1-16
        return layer switch                                                       // special (0x0103 partition)
        {
            33 => 0x01030006u, // top overlay
            34 => 0x01030007u, // bottom overlay
            35 => 0x01030008u, // top paste
            36 => 0x01030009u, // bottom paste
            37 => 0x0103000Au, // top solder
            38 => 0x0103000Bu, // bottom solder
            55 => 0x0103000Cu, // drill guide
            56 => 0x0103000Du, // keepout
            73 => 0x0103000Eu, // drill drawing
            74 => 0x0103000Fu, // multi-layer
            _ => 0x0103000Fu,  // fallback: multi-layer
        };
    }

    internal static void WriteArc(BinaryFormatWriter writer, PcbArc arc)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(arc.IsLocked, arc.IsTentingTop,
                arc.IsTentingBottom, arc.IsKeepout);
            WriteCommonPrimitiveData(w, arc.Layer, flags, arc.NetIndex, arc.ComponentIndex); // 0-12
            w.WriteCoordPoint(arc.Center);                 // 13-20
            w.WriteCoord(arc.Radius);                      // 21-24
            w.Write(arc.StartAngle);                       // 25-32
            w.Write(arc.EndAngle);                         // 33-40
            w.WriteCoord(arc.Width);                       // 41-44
            // Extended tail (offsets 45-59)
            w.Write((short)0);                              // 45-46 subpoly index
            w.WriteCoord(arc.SolderMaskExpansion);          // 47-50 solder mask expansion
            w.Write((byte)0);                               // 51 paste mask expansion
            w.Write(V7LayerId(arc.Layer));                  // 52-55 v7 layer id (derived from layer)
            w.Write((byte)arc.KeepoutRestrictions);         // 56 keepout restrictions
            w.Write(new byte[3]);                           // 57-59 reserved
        });
    }

    internal static void WritePad(BinaryFormatWriter writer, PcbPad pad)
    {
        // Pad has a complex multi-block structure
        writer.WriteStringBlock(pad.Designator ?? string.Empty);
        writer.WriteStringBlock(pad.PadSubrecord2 ?? string.Empty); // SubRecord 2 (usually empty)
        writer.WriteStringBlock(pad.PadNetString ?? "|&|0"); // SubRecord 3 (usually "|&|0")
        writer.WriteBlock(new byte[] { 0 }); // SubRecord 4 (always empty)

        // Main pad data block (114 bytes standard)
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(pad.IsLocked, pad.IsTentingTop,
                pad.IsTentingBottom, pad.IsKeepout);
            WriteCommonPrimitiveData(w, pad.Layer, flags, pad.NetIndex, pad.ComponentIndex);
            w.WriteCoordPoint(pad.Location);
            w.WriteCoordPoint(pad.SizeTop);
            w.WriteCoordPoint(pad.SizeMiddle);
            w.WriteCoordPoint(pad.SizeBottom);
            w.WriteCoord(pad.HoleSize);
            // Main-block base shapes: when per-layer overrides were active the typed Shape* were
            // overridden for rendering, so emit the modeled base shapes; otherwise the typed shapes.
            if (pad.MainBlockBaseShapes is { } baseShapes)
            {
                w.Write(baseShapes.Top);
                w.Write(baseShapes.Middle);
                w.Write(baseShapes.Bottom);
            }
            else
            {
                w.Write((byte)pad.ShapeTop);
                w.Write((byte)pad.ShapeMiddle);
                w.Write((byte)pad.ShapeBottom);
            }
            w.Write(pad.Rotation);
            w.Write(pad.IsPlated);
            // Offsets 61-201: extended tail (thermal relief, mask expansion + modes,
            // jumper, hole tolerances) built from a fixed template with the typed/semantic
            // fields overlaid at their exact offsets.
            w.Write(BuildPadExtendedTail(pad));
        });

        // Size/shape block (596 bytes standard, or empty if not present in original)
        if (!pad.HasSizeShapeBlock)
        {
            writer.WriteBlock(Array.Empty<byte>());
            return;
        }

        writer.WriteBlock(w =>
        {
            // 29 X sizes for internal copper layers (offset 0-115)
            for (var i = 0; i < 29; i++) w.Write(pad.LayerXSizes[i]);
            // 29 Y sizes for internal copper layers (offset 116-231)
            for (var i = 0; i < 29; i++) w.Write(pad.LayerYSizes[i]);
            // 29 shapes for internal copper layers (offset 232-260)
            for (var i = 0; i < 29; i++) w.Write(pad.InternalLayerShapes[i]);
            // Reserved byte (offset 261)
            w.Write((byte)0);
            // Hole shape (offset 262)
            w.Write((byte)pad.HoleType);
            // Hole slot length (offset 263-266)
            w.Write(pad.HoleSlotLength);
            // Hole rotation (offset 267-274)
            w.Write(pad.HoleRotation);
            // 32 X offsets from hole center (offset 275-402)
            for (var i = 0; i < 32; i++) w.Write(pad.OffsetXFromHoleCenter[i]);
            // 32 Y offsets from hole center (offset 403-530)
            for (var i = 0; i < 32; i++) w.Write(pad.OffsetYFromHoleCenter[i]);
            // HasRoundedRect flag (offset 531)
            w.Write(pad.HasRoundedRectByte);
            // 32 per-layer shapes (offset 532-563)
            for (var i = 0; i < 32; i++) w.Write(pad.PerLayerShapes[i]);
            // 32 corner radius percentages (offset 564-595)
            for (var i = 0; i < 32; i++) w.Write(pad.PerLayerCornerRadii[i]);

            // Full-stack tail: [32 reserved][u32 count][u32 stride=15][count x 15-byte entries].
            if (pad.FullStackEntries.Count > 0)
            {
                w.Write(new byte[32]);
                w.Write(pad.FullStackEntries.Count);
                w.Write(15);
                foreach (var e in pad.FullStackEntries)
                {
                    w.Write(e.LayerCode);
                    w.Write(e.Flag1);
                    w.Write(e.Flag2);
                    w.Write(e.Flag3);
                    w.Write(e.Flag4);
                    w.Write(e.SizeX);
                    w.Write(e.SizeY);
                    w.Write(e.CornerPercent);
                    w.Write(e.Trailing);
                }
            }
        });
    }

    // First offset of the pad SubRecord-5 extended tail (after the basic geometry).
    private const int PadExtendedStart = 61;

    // Canonical 141-byte pad SubRecord-5 extended tail (offsets 61-201), captured from a
    // standard Altium pad. The typed/semantic fields are overlaid at their exact offsets in
    // BuildPadExtendedTail; the remaining bytes are reserved / pad-cache / footprint-identity
    // values reproduced verbatim so the record matches Altium's 202-byte layout.
    private static readonly byte[] PadExtendedTailTemplate =
    {
        // 61-76
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA0, 0x86, 0x01, 0x00, 0x04, 0x00, 0xA0, 0x86, 0x01,
        // 77-92
        0x00, 0x40, 0x0D, 0x03, 0x00, 0x40, 0x0D, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x9C, 0x00,
        // 93-108
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        // 109-124
        0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x03, 0x01, 0x00, 0x00, 0x00, 0x40, 0x9C, 0x00, 0x00,
        // 125-140
        0x00, 0x64, 0x9A, 0x92, 0x26, 0x10, 0xC7, 0xE4, 0x41, 0xA3, 0x2B, 0x29, 0x17, 0xA5, 0x35, 0x2E,
        // 141-156
        0x67, 0x7F, 0xAB, 0x21, 0x20, 0xC3, 0x0B, 0x32, 0x47, 0xAD, 0xCE, 0x6C, 0xB7, 0xB8, 0xC9, 0x7E,
        // 157-172
        0x68, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0x7F, 0x00, 0x01, 0x1A,
        // 173-188
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00,
        // 189-201
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    };

    /// <summary>
    /// Builds the pad SubRecord-5 extended tail (offsets 61-201) by overlaying the typed
    /// semantic fields onto the canonical template.
    /// </summary>
    private static byte[] BuildPadExtendedTail(PcbPad pad)
    {
        // Fully modeled — no raw replay. Start from the reserved-constant template and overlay every
        // varying byte from a typed field (modeled geometry, the two identity GUIDs, the cache-validity
        // bytes and reserved marker) or a derived value (the v7 layer id and the solder-mask cache copy).
        var ext = (byte[])PadExtendedTailTemplate.Clone();
        void PutI32(int offset, int value) => BitConverter.GetBytes(value).CopyTo(ext, offset - PadExtendedStart);
        void PutI16(int offset, short value) => BitConverter.GetBytes(value).CopyTo(ext, offset - PadExtendedStart);
        void PutGuid(int offset, Guid g) => g.ToByteArray().CopyTo(ext, offset - PadExtendedStart);

        ext[62 - PadExtendedStart] = (byte)pad.Mode;                      // 62: pad stack mode
        ext[67 - PadExtendedStart] = (byte)pad.PowerPlaneConnectStyle;    // 67: plane connection style
        PutI32(68, pad.ReliefConductorWidth.ToRaw());                    // 68-71
        PutI16(72, (short)pad.ReliefEntries);                            // 72-73
        PutI32(74, pad.ReliefAirGap.ToRaw());                            // 74-77
        PutI32(78, pad.PowerPlaneReliefExpansion.ToRaw());               // 78-81
        PutI32(82, pad.PowerPlaneClearance.ToRaw());                     // 82-85
        PutI32(86, pad.PasteMaskExpansion.ToRaw());                      // 86-89 (manual paste)
        PutI32(90, pad.SolderMaskExpansion.ToRaw());                     // 90-93 (manual solder)
        ext[96 - PadExtendedStart] = pad.CachePlaneConnectionValid;       // 96-100, 103-104: cache validity
        ext[97 - PadExtendedStart] = pad.CacheReliefConductorWidthValid;
        ext[98 - PadExtendedStart] = pad.CacheReliefEntriesValid;
        ext[99 - PadExtendedStart] = pad.CacheReliefAirGapValid;
        ext[100 - PadExtendedStart] = pad.CachePowerPlaneReliefExpansionValid;
        ext[101 - PadExtendedStart] = (byte)pad.PasteMaskExpansionMode;  // 101
        ext[102 - PadExtendedStart] = (byte)pad.SolderMaskExpansionMode; // 102
        ext[103 - PadExtendedStart] = pad.CachePasteMaskExpansionValid;
        ext[104 - PadExtendedStart] = pad.CacheSolderMaskExpansionValid;
        PutI16(110, (short)pad.JumperID);                                // 110-111
        PutI32(114, unchecked((int)V7LayerId(pad.Layer)));               // 114-117 v7 layer id (derived)
        PutI32(121, pad.SolderMaskCache);                                // 121-124 solder-mask cache word (modeled)
        PutGuid(126, pad.IdentityGuid);                                  // 126-141 GUID-A (per-pad identity)
        PutGuid(142, pad.IdentityGuidB);                                 // 142-157 GUID-B (footprint/stack identity)
        PutI32(162, pad.HolePositiveTolerance.ToRaw());                  // 162-165
        PutI32(166, pad.HoleNegativeTolerance.ToRaw());                  // 166-169
        ext[172 - PadExtendedStart] = (byte)(pad.Sr5Length == 202 ? 0x1A : 0x12); // 172: derived from pad-record format (RE: tracks Sr5Length, not editable)
        ext[185 - PadExtendedStart] = pad.ReservedMarker185;             // 185 reserved marker

        // Reproduce the original SubRecord-5 length (PcbLib pads are 202 bytes, PcbDoc pads 194).
        var targetTailLength = pad.Sr5Length - PadExtendedStart;
        if (targetTailLength > 0 && targetTailLength != ext.Length)
        {
            var resized = new byte[targetTailLength];
            Array.Copy(ext, resized, Math.Min(ext.Length, targetTailLength));
            return resized;
        }
        return ext;
    }

    internal static void WriteVia(BinaryFormatWriter writer, PcbVia via)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(via.IsLocked, via.IsTentingTop,
                via.IsTentingBottom, via.IsKeepout);
            WriteCommonPrimitiveData(w, via.Layer, flags, via.NetIndex, via.ComponentIndex); // offsets 0-12
            // Offsets 13-320: full 321-byte via record built from a template with the typed
            // fields overlaid (geometry, layers, thermal relief, mask expansions, per-layer
            // diameters, drill tolerances). Reserved/cache/identity regions come from template.
            w.Write(BuildViaExtended(via));
        });
    }

    // Canonical 321-byte via SubRecord-1 (offsets 0-320), captured from a standard Altium via.
    private static readonly byte[] ViaSr1Template =
    {
        0x4A, 0x0C, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, // 0
        0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xF0, 0x49, 0x02, 0x00, 0x02, 0x20, 0x00, // 16
        0xA0, 0x86, 0x01, 0x00, 0x04, 0x00, 0xA0, 0x86, 0x01, 0x00, 0x40, 0x0D, 0x03, 0x00, 0x40, 0x0D, // 32
        0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x9C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 48
        0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, // 64
        0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, // 80
        0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, // 96
        0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, // 112
        0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, // 128
        0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, // 144
        0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, // 160
        0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, // 176
        0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0xE0, 0x93, 0x04, 0x00, 0x0F, 0x00, 0x03, 0x01, 0x00, // 192
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 208
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 224
        0x00, 0x00, 0x40, 0x9C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2A, 0x00, // 240
        0x00, 0x00, 0x00, 0x80, 0x63, 0xD4, 0xE4, 0x65, 0xC4, 0xF4, 0x4E, 0x8B, 0xAD, 0xA7, 0xCE, 0x97, // 256
        0xDC, 0x40, 0xDA, 0xA5, 0xB1, 0xE3, 0xB2, 0x84, 0x25, 0x11, 0x43, 0x83, 0xDB, 0x2B, 0x6A, 0x87, // 272
        0x7C, 0xB1, 0x74, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00, // 288
        0x1E, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 304
        0x01,                                                                                           // 320
    };

    /// <summary>
    /// Builds the via SubRecord-1 body (offsets 13-320) by overlaying the typed fields onto
    /// the canonical template.
    /// </summary>
    private static byte[] BuildViaExtended(PcbVia via)
    {
        // Fully modeled — no raw replay. Start from the reserved-constant template and overlay every
        // varying byte from a typed field (geometry, thermal/mask, per-layer diameters, the two identity
        // GUIDs, the cache-validity bytes) or a derived value (the symmetric back-side mask).
        var b = (byte[])ViaSr1Template.Clone();
        void PutI32(int off, int v) => BitConverter.GetBytes(v).CopyTo(b, off);
        void PutGuid(int off, Guid g) => g.ToByteArray().CopyTo(b, off);

        PutI32(13, via.Location.X.ToRaw());
        PutI32(17, via.Location.Y.ToRaw());
        PutI32(21, via.Diameter.ToRaw());
        PutI32(25, via.HoleSize.ToRaw());
        b[29] = (byte)via.StartLayer;
        b[30] = (byte)via.EndLayer;
        b[31] = (byte)via.PowerPlaneConnectStyle;
        PutI32(32, via.ThermalReliefAirGap.ToRaw());
        b[36] = (byte)via.ThermalReliefConductors;
        PutI32(38, via.ThermalReliefConductorsWidth.ToRaw());
        PutI32(42, via.PowerPlaneReliefExpansion.ToRaw());
        PutI32(46, via.PowerPlaneClearance.ToRaw());
        PutI32(50, via.PasteMaskExpansion.ToRaw());
        PutI32(54, via.SolderMaskExpansion.ToRaw());
        PutI32(61, via.CacheValid61);                     // 61-64 cache-validity word (modeled)
        b[66] = (byte)via.SolderMaskExpansionMode;
        b[67] = via.CacheValid67;                         // 67 cache-validity byte (modeled)
        b[70] = via.ReservedByte70;                       // 70 (modeled)
        b[72] = via.ReservedByte72;                       // 72 (modeled)
        b[74] = (byte)via.Mode;
        // Per-layer diameters: write the populated array verbatim (so a genuine zero on one layer of a
        // full stack round-trips); fall back to the via diameter only for a from-scratch via whose array
        // is entirely zero (else a simple via would get 0 diameter on every layer).
        var hasPerLayerDiameters = via.Diameters.Any(d => d.ToRaw() != 0);
        var defaultDiameter = via.Diameter.ToRaw();
        for (var i = 0; i < 32; i++)
            PutI32(75 + i * 4, hasPerLayerDiameters ? via.Diameters[i].ToRaw() : defaultDiameter);
        PutI32(242, via.SolderMaskBackRaw);               // 242-245 back-side mask (modeled)
        b[258] = (byte)(via.SolderMaskExpansionFromHoleEdge ? 1 : 0);
        PutGuid(259, via.IdentityGuid);                   // 259-274 uid (per-via identity)
        PutGuid(275, via.IdentityGuidB);                  // 275-290 sig (footprint/stack identity)
        PutI32(291, via.HolePositiveTolerance.ToRaw());
        PutI32(295, via.HoleNegativeTolerance.ToRaw());
        b[312] = (byte)via.DrillLayerPairType;
        return b[13..];
    }

    internal static void WriteTrack(BinaryFormatWriter writer, PcbTrack track)
    {
        writer.WriteBlock(w =>
        {
            var flags = PcbBinaryConstants.MergeFlags(track.RawFlags, EncodeFlags(track.IsLocked, track.IsTentingTop,
                track.IsTentingBottom, track.IsKeepout));
            WriteCommonPrimitiveData(w, track.Layer, flags, track.NetIndex, track.ComponentIndex); // 0-12
            w.WriteCoordPoint(track.Start);                  // 13-20
            w.WriteCoordPoint(track.End);                    // 21-28
            w.WriteCoord(track.Width);                       // 29-32
            // Extended tail (offsets 33-48)
            w.Write((short)0);                              // 33-34 subpoly index
            w.WriteCoord(track.SolderMaskExpansion);        // 35-38 solder mask expansion
            w.Write((short)0);                              // 39-40 paste mask expansion
            w.Write(V7LayerId(track.Layer));                // 41-44 v7 layer id (derived from layer)
            w.Write((byte)track.KeepoutRestrictions);       // 45 keepout restrictions
            w.Write(new byte[3]);                           // 46-48 reserved
        });
    }

    internal static void WriteText(BinaryFormatWriter writer, PcbText text, int wideStringIndex)
    {
        writer.WriteBlock(w =>
        {
            var flags = PcbBinaryConstants.MergeFlags(text.RawFlags, EncodeFlags(text.IsLocked, text.IsTentingTop,
                text.IsTentingBottom, text.IsKeepout));
            WriteCommonPrimitiveData(w, text.Layer, flags, text.NetIndex, text.ComponentIndex); // offsets 0-12
            // Offsets 13-251: geometry, font, text-box, barcode block and frame tail built
            // from a fixed template with the typed/semantic fields overlaid at their offsets.
            w.Write(BuildTextExtended(text, wideStringIndex));
        });

        writer.WriteStringBlock(text.Text);
    }

    // Canonical 252-byte text SubRecord-1 (offsets 0-251), captured from a standard Altium
    // text record. BuildTextExtended overlays the typed fields and returns offsets 13-251
    // (the common header 0-12 is written separately by WriteCommonPrimitiveData).
    private static readonly byte[] TextSr1Template =
    {
        0x21, 0x0C, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, // 0
        0x00, 0x50, 0x8E, 0xF4, 0xFF, 0x80, 0x1A, 0x06, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 16
        0x80, 0x46, 0x40, 0x00, 0x40, 0x9C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x41, 0x00, // 32
        0x72, 0x00, 0x69, 0x00, 0x61, 0x00, 0x6C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 48
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 64
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 80
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 96
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCE, 0xE5, 0x29, 0x00, // 112
        0x7F, 0x52, 0x07, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0xA0, 0x37, 0xA0, 0x00, 0x20, 0x0B, 0x20, // 128
        0x00, 0x40, 0x0D, 0x03, 0x00, 0x40, 0x0D, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, // 144
        0x00, 0x41, 0x00, 0x72, 0x00, 0x69, 0x00, 0x61, 0x00, 0x6C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 160
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 176
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 192
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 208
        0x00, 0x01, 0x06, 0x00, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x80, // 224
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x50, 0x8E, 0xF4, 0xFF,                         // 240
    };

    /// <summary>
    /// Builds the text SubRecord-1 extended body (offsets 13-251) by overlaying the typed
    /// fields onto the canonical template.
    /// </summary>
    private static byte[] BuildTextExtended(PcbText text, int wideStringIndex)
    {
        // Fully modeled — no raw replay. Start from the reserved-constant template and overlay every
        // varying byte from a typed field; the v7 layer id is derived.
        var b = (byte[])TextSr1Template.Clone();
        void PutI32(int off, int v) => BitConverter.GetBytes(v).CopyTo(b, off);
        void PutI16(int off, short v) => BitConverter.GetBytes(v).CopyTo(b, off);
        void PutDbl(int off, double v) => BitConverter.GetBytes(v).CopyTo(b, off);
        void PutFont(int off, string? s, byte[]? raw)
        {
            if (raw is { Length: 64 }) { raw.CopyTo(b, off); return; }   // dirty-padding texts: exact field
            Array.Clear(b, off, 64);
            if (string.IsNullOrEmpty(s)) return;
            var bytes = System.Text.Encoding.Unicode.GetBytes(s);
            Array.Copy(bytes, 0, b, off, Math.Min(bytes.Length, 62)); // leave a UTF-16 null terminator
        }

        PutI32(13, text.Location.X.ToRaw());
        PutI32(17, text.Location.Y.ToRaw());
        PutI32(21, text.Height.ToRaw());
        PutI16(25, (short)text.FontId);     // 25-26 font-table index (Altium fontID)
        PutDbl(27, text.Rotation);
        b[35] = (byte)(text.IsMirrored ? 1 : 0);
        PutI32(36, text.StrokeWidth.ToRaw());
        if (b.Length >= 230) PutI32(226, unchecked((int)V7LayerId(text.Layer))); // 226-229 v7 layer id (derived)
        b[40] = (byte)(text.IsComment ? 1 : 0);
        b[41] = (byte)(text.IsDesignator ? 1 : 0);
        b[42] = (byte)text.CharSet;
        // 43 base font type: derived from the kind, with an override for sources that disagree.
        b[43] = text.BaseFontType ?? (byte)(text.TextKind == PcbTextKind.BarCode
            ? (text.IsTrueType ? 1 : 0)
            : (int)text.TextKind);
        b[44] = (byte)(text.FontBold ? 1 : 0);
        b[45] = (byte)(text.FontItalic ? 1 : 0);
        PutFont(46, text.FontName, text.FontFieldRaw);   // 46-109 primary font field
        b[110] = (byte)(text.IsInverted ? 1 : 0);
        PutI32(111, text.InvertedBorder.ToRaw());
        PutI32(115, wideStringIndex);
        PutI32(119, text.UnionIndex);
        b[123] = (byte)(text.UseInvertedRectangle ? 1 : 0);
        PutI32(124, text.InvertedRectWidth.ToRaw());
        PutI32(128, text.InvertedRectHeight.ToRaw());
        b[132] = (byte)text.InvertedRectJustification;
        PutI32(133, text.InvertedRectTextOffset.ToRaw());
        PutI32(137, text.BarCodeFullWidth.ToRaw());
        PutI32(141, text.BarCodeFullHeight.ToRaw());
        PutI32(145, text.BarCodeXMargin.ToRaw());
        PutI32(149, text.BarCodeYMargin.ToRaw());
        PutI32(153, text.BarCodeMinWidth.ToRaw());
        b[157] = (byte)text.BarCodeKind;
        b[158] = (byte)text.BarCodeRenderMode;
        b[159] = (byte)(text.BarCodeInverted ? 1 : 0);
        b[160] = text.TextKindByte ?? (byte)text.TextKind;   // 160 authoritative text kind (derived + override)
        PutFont(161, text.BarCodeFontName, text.BarCodeFontFieldRaw);   // 161-224 barcode font field
        b[225] = (byte)(text.BarCodeShowText ? 1 : 0);
        b[230] = (byte)(text.IsFrame ? 1 : 0);
        b[231] = (byte)(text.IsOffsetBorder ? 1 : 0);
        b[240] = (byte)(text.IsJustificationValid ? 1 : 0);
        b[241] = (byte)(text.AdvanceSnapping ? 1 : 0);
        PutI32(244, text.SnapPointX.ToRaw());
        PutI32(248, text.SnapPointY.ToRaw());

        return b[13..];
    }

    internal static void WriteFill(BinaryFormatWriter writer, PcbFill fill)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(fill.IsLocked, fill.IsTentingTop,
                fill.IsTentingBottom, fill.IsKeepout);
            WriteCommonPrimitiveData(w, fill.Layer, flags, fill.NetIndex, fill.ComponentIndex); // 0-12
            w.WriteCoordPoint(fill.Corner1);                // 13-20
            w.WriteCoordPoint(fill.Corner2);                // 21-28
            w.Write(fill.Rotation);                         // 29-36
            // Extended tail (offsets 37-49)
            w.WriteCoord(fill.SolderMaskExpansion);         // 37-40 solder mask expansion
            w.Write((byte)0);                               // 41 paste mask expansion
            w.Write(V7LayerId(fill.Layer));                 // 42-45 v7 layer id (derived from layer)
            w.Write((byte)fill.KeepoutRestrictions);        // 46 keepout restrictions
            w.Write(new byte[3]);                           // 47-49 reserved
        });
    }

    internal static void WriteRegion(BinaryFormatWriter writer, PcbRegion region)
    {
        writer.WriteBlock(w =>
        {
            var flags = PcbBinaryConstants.MergeFlags(region.RawFlags, EncodeFlags(region.IsLocked, region.IsTentingTop,
                region.IsTentingBottom, region.IsKeepout));
            WriteCommonPrimitiveData(w, region.Layer, flags, region.NetIndex, region.ComponentIndex, region.PolygonIndex);

            // Header: reserved byte @13 + hole_count uint16 @14-15 + 2 reserved bytes @16-17.
            var holes = region.Holes;
            w.Write((byte)0);
            w.Write((ushort)(holes?.Count ?? 0));
            w.Write((byte)0);
            w.Write((byte)0);

            // Nested parameter block, regenerated in Altium's canonical key order from typed fields
            // (no leading '|'); the byte-exact mil/bool formatting is reproduced exactly.
            w.WriteCStringParameterBlockRaw(BuildRegionParamText(region));

            // Geometry section, fully modeled (no raw replay). Loaded regions carry exact IEEE-double
            // vertices (sub-coord precision); from-scratch regions encode the integer vertices as doubles.
            var outlineExact = region.OutlineExact;
            if (outlineExact is { } oe && oe.Count == region.Outline.Count)
            {
                w.Write((uint)oe.Count);
                foreach (var (x, y) in oe) { w.Write(x); w.Write(y); }
            }
            else
            {
                w.Write((uint)region.Outline.Count);
                foreach (var point in region.Outline)
                {
                    w.Write((double)point.X.ToRaw());
                    w.Write((double)point.Y.ToRaw());
                }
            }

            // Hole / cutout vertex arrays: [uint32 count][count x,y doubles] per hole.
            if (holes != null)
            {
                var holesExact = region.HolesExact;
                for (var h = 0; h < holes.Count; h++)
                {
                    var hole = holes[h];
                    var hExact = holesExact is not null && h < holesExact.Count && holesExact[h].Count == hole.Count
                        ? holesExact[h] : null;
                    w.Write((uint)hole.Count);
                    if (hExact is not null)
                        foreach (var (x, y) in hExact) { w.Write(x); w.Write(y); }
                    else
                        foreach (var point in hole) { w.Write((double)point.X.ToRaw()); w.Write((double)point.Y.ToRaw()); }
                }
            }
        });
    }

    internal static void WriteComponentBody(BinaryFormatWriter writer, PcbComponentBody body)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(body.IsLocked, body.IsTentingTop,
                body.IsTentingBottom, body.IsKeepout);
            var binaryLayer = LayerNameToByte(body.LayerName);
            WriteCommonPrimitiveData(w, binaryLayer, flags, body.NetIndex, body.ComponentIndex);

            // Structure: uint32 prefix + byte prefix + nested parameter block + outline vertices (doubles)
            w.Write((uint)0); // reserved prefix 1
            w.Write((byte)0); // reserved prefix 2

            // Nested parameter block, regenerated in Altium's canonical key order from typed fields
            // (no leading '|'), reproducing the duplicate ARCRESOLUTION and exact mil/F3/Delphi formats.
            w.WriteCStringParameterBlockRaw(BuildComponentBodyParamText(body));

            // Write outline vertices as doubles (Altium PCB format)
            w.Write((uint)body.Outline.Count);
            foreach (var point in body.Outline)
            {
                w.Write((double)point.X.ToRaw());
                w.Write((double)point.Y.ToRaw());
            }
        });
    }

    private static void WriteUniqueIdPrimitiveInformation(CompoundStorage parentStorage, PcbComponent component)
    {
        if (component.PrimitiveUniqueIds.Count == 0) return;   // optional; from-scratch footprints omit it
        var storage = parentStorage.AddStorage("UniqueIDPrimitiveInformation");
        WriteStorageHeader(storage, component.PrimitiveUniqueIds.Count);
        storage.AddStream("Data").SetData(BuildPrimitiveUniqueIdData(component.PrimitiveUniqueIds));
    }

    // Emits a FileVersionInfo/Data stream ([u32 len][|KEY=VALUE| block]) from the typed record.
    internal static byte[] BuildFileVersionInfoData(PcbFileVersionInfo info)
    {
        string text;
        if (info.OrderedParameters is { Count: > 0 } ordered)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in ordered) sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
            text = sb.ToString();
        }
        else
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in info.Parameters) sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
            text = sb.ToString();
        }
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);
        writer.WriteCStringParameterBlockRaw(text);
        writer.Flush();
        return ms.ToArray();
    }

    // Emits the length-prefixed |KEY=VALUE| records of a UniqueIDPrimitiveInformation/Data stream.
    internal static byte[] BuildPrimitiveUniqueIdData(IReadOnlyList<PcbPrimitiveUniqueId> records)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);
        foreach (var rec in records)
        {
            string text;
            if (rec.OrderedParameters is { Count: > 0 } ordered)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var kvp in ordered) sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
                text = sb.ToString();
            }
            else text = rec.ToText();
            writer.WriteCStringParameterBlockRaw(text);
        }
        writer.Flush();
        return ms.ToArray();
    }

    private static void WriteLibraryModels(CompoundStorage libraryStorage, PcbLibrary library)
        => WriteModelsStorage(libraryStorage, library.Models);

    /// <summary>
    /// Writes a <c>Models</c> child storage (Header + metadata Data + numbered zlib STEP streams) under
    /// <paramref name="parentStorage"/>. Shared by the PcbLib library writer (<c>Library/Models</c>) and
    /// the PcbDoc writer (root <c>Models</c>). The metadata Data is reconstructed byte-exactly from the
    /// typed fields; the numbered STEP payloads are re-compressed (zlib bytes may differ, content preserved).
    /// </summary>
    internal static void WriteModelsStorage(CompoundStorage parentStorage, IReadOnlyList<PcbModel> models)
    {
        var modelsStorage = parentStorage.AddStorage("Models");
        WriteStorageHeader(modelsStorage, models.Count);

        // Write Data stream: model metadata as length-prefixed C-string parameter blocks
        using var dataMs = new MemoryStream();
        foreach (var model in models)
        {
            var paramStr = string.Join("|",
                $"EMBED={( model.IsEmbedded ? "TRUE" : "FALSE" )}",
                $"MODELSOURCE={model.ModelSource}",
                $"ID={model.Id}",
                $"ROTX={model.RotationX.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
                $"ROTY={model.RotationY.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
                $"ROTZ={model.RotationZ.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
                $"DZ={model.Dz}",
                $"CHECKSUM={model.Checksum}",
                $"NAME={model.Name}");
            var paramBytes = System.Text.Encoding.ASCII.GetBytes(paramStr + '\0');
            var lenBytes = BitConverter.GetBytes(paramBytes.Length);
            dataMs.Write(lenBytes);
            dataMs.Write(paramBytes);
        }
        var dataStream = modelsStorage.AddStream("Data");
        dataStream.SetData(dataMs.ToArray());

        // Write numbered model streams: zlib-compressed STEP text
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            byte[] compressedData;
            if (!string.IsNullOrEmpty(model.StepData))
            {
                var stepBytes = System.Text.Encoding.UTF8.GetBytes(model.StepData);
                using var outMs = new MemoryStream();
                using (var zs = new System.IO.Compression.ZLibStream(outMs, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
                {
                    zs.Write(stepBytes);
                }
                compressedData = outMs.ToArray();
            }
            else
            {
                compressedData = Array.Empty<byte>();
            }

            var modelStream = modelsStorage.AddStream(i.ToString());
            modelStream.SetData(compressedData);
        }
    }

    private static void WriteAdditionalComponentStreams(CompoundStorage storage, PcbComponent component)
    {
        if (component is PcbComponent { AdditionalStreams: not null } pcbComp)
            WriteAdditionalStreams(storage, pcbComp.AdditionalStreams);
    }

    private static void WriteAdditionalLibraryStreams(CompoundStorage libraryStorage, PcbLibrary library)
    {
        if (library.AdditionalLibraryStreams != null)
            WriteAdditionalStreams(libraryStorage, library.AdditionalLibraryStreams);
    }

    private static void WriteAdditionalRootStreams(CompoundFileAccessor cf, PcbLibrary library)
    {
        if (library.AdditionalRootStreams != null)
            WriteAdditionalStreams(cf.RootStorage, library.AdditionalRootStreams);
    }

    internal static void WriteFileVersionInfo(CompoundStorage rootStorage, PcbFileVersionInfo info)
    {
        if (!info.Present && info.Parameters.Count == 0 && info.OrderedParameters is null) return;
        var storage = rootStorage.AddStorage("FileVersionInfo");
        WriteStorageHeader(storage, 1);
        storage.AddStream("Data").SetData(BuildFileVersionInfoData(info));
    }

    internal static void WriteAdditionalStreams(CompoundStorage storage, Dictionary<string, byte[]> streams)
    {
        foreach (var kvp in streams)
        {
            if (kvp.Key.Contains('/'))
            {
                var parts = kvp.Key.Split('/', 2);
                if (!storage.TryGetStorage(parts[0], out var subStorage))
                    subStorage = storage.AddStorage(parts[0]);
                var stream = subStorage.AddStream(parts[1]);
                stream.SetData(kvp.Value);
            }
            else
            {
                var stream = storage.AddStream(kvp.Key);
                stream.SetData(kvp.Value);
            }
        }
    }

    /// <summary>
    /// Maps a V7_LAYER string (e.g., "MECHANICAL1", "TOP", "MULTILAYER") to the binary layer byte.
    /// </summary>
    internal static byte LayerNameToByte(string? layerName)
    {
        if (string.IsNullOrEmpty(layerName))
            return 0;

        // Normalize to uppercase for case-insensitive matching
        var name = layerName.ToUpperInvariant().Replace(" ", "").Replace("-", "");

        // Check common mechanical layers first (most common for ComponentBody)
        if (name.StartsWith("MECHANICAL") && int.TryParse(name.AsSpan("MECHANICAL".Length), out var mechNum) && mechNum >= 1 && mechNum <= 16)
            return (byte)(56 + mechNum); // Mechanical1=57, Mechanical16=72

        return name switch
        {
            "TOPLAYER" or "TOP" => 1,
            "BOTTOMLAYER" or "BOTTOM" => 32,
            "TOPOVERLAY" => 33,
            "BOTTOMOVERLAY" => 34,
            "TOPPASTE" => 35,
            "BOTTOMPASTE" => 36,
            "TOPSOLDER" => 37,
            "BOTTOMSOLDER" => 38,
            "DRILLGUIDE" => 55,
            "KEEPOUTLAYER" or "KEEPOUT" => 56,
            "DRILLDRAWING" => 73,
            "MULTILAYER" => 74,
            _ when name.StartsWith("MIDLAYER") && int.TryParse(name.AsSpan("MIDLAYER".Length), out var midNum) && midNum >= 1 && midNum <= 30
                => (byte)(1 + midNum), // MidLayer1=2, MidLayer30=31
            _ when name.StartsWith("MID") && int.TryParse(name.AsSpan("MID".Length), out var mid2Num) && mid2Num >= 1 && mid2Num <= 30
                => (byte)(1 + mid2Num),
            _ when name.StartsWith("INTERNALPLANE") && int.TryParse(name.AsSpan("INTERNALPLANE".Length), out var planeNum) && planeNum >= 1 && planeNum <= 16
                => (byte)(38 + planeNum), // InternalPlane1=39, InternalPlane16=54
            _ => 0 // NoLayer
        };
    }

    private static string GetSectionKeyFromName(string name) =>
        WriterUtilities.GetSectionKeyFromName(name);
}
