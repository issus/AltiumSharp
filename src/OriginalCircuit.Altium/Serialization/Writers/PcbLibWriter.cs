using OpenMcdf;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;

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
        await WriteAsync(library, stream, cancellationToken);
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
        await ms.CopyToAsync(stream, cancellationToken);
    }

    /// <summary>
    /// Writes a PcbLib file to a stream synchronously.
    /// </summary>
    /// <param name="library">The PCB footprint library to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public void Write(PcbLibrary library, Stream stream, CancellationToken cancellationToken = default)
    {
        using var cf = new CompoundFile();
        var sectionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        WriteFileHeader(cf, library);
        WriteSectionKeys(cf, library, sectionKeys);
        WriteLibrary(cf, library, sectionKeys, cancellationToken);
        WriteAdditionalRootStreams(cf, library);

        cf.Save(stream);
    }

    private static void WriteFileHeader(CompoundFile cf, PcbLibrary library)
    {
        var headerStream = cf.RootStorage.AddStream("FileHeader");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        var versionText = "PCB 6.0 Binary Library File";
        writer.Write(versionText.Length);
        writer.WritePascalShortString(versionText);

        // String 1: Format version double (5.01) + 2 padding bytes
        writer.Write((byte)10); // length prefix
        writer.Write(5.01d);    // version double (8 bytes)
        writer.Write((short)0); // 2 padding bytes

        // String 2: Empty placeholder
        writer.Write((byte)0);

        // String 3: 8-character unique library identifier
        var uniqueId = library.UniqueId ?? "AAAAAAAA";
        var idBytes = System.Text.Encoding.ASCII.GetBytes(uniqueId);
        writer.Write((byte)idBytes.Length);
        writer.Write(idBytes);

        writer.Flush();
        headerStream.SetData(ms.ToArray());
    }

    private static void WriteSectionKeys(CompoundFile cf, PcbLibrary library, Dictionary<string, string> sectionKeys)
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
            writer.WritePascalString(component.Name);
            writer.WriteStringBlock(sectionKeys[component.Name]);
        }

        writer.Flush();
        sectionKeysStream.SetData(ms.ToArray());
    }

    private static void WriteLibrary(CompoundFile cf, PcbLibrary library, Dictionary<string, string> sectionKeys, CancellationToken cancellationToken = default)
    {
        var libraryStorage = cf.RootStorage.AddStorage("Library");

        // Write header (record count)
        WriteStorageHeader(libraryStorage, 1);

        // Write library data
        WriteLibraryData(cf, libraryStorage, library, sectionKeys, cancellationToken);

        // Write models
        WriteLibraryModels(libraryStorage, library);

        // Write additional library-level streams (ComponentParamsTOC, EmbeddedFonts, etc.)
        WriteAdditionalLibraryStreams(libraryStorage, library);
    }

    internal static void WriteStorageHeader(CFStorage storage, int recordCount)
    {
        var headerStream = storage.AddStream("Header");
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(recordCount);
        writer.Flush();
        headerStream.SetData(ms.ToArray());
    }

    private static void WriteLibraryData(CompoundFile cf, CFStorage libraryStorage, PcbLibrary library, Dictionary<string, string> sectionKeys, CancellationToken cancellationToken = default)
    {
        var dataStream = libraryStorage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Generate library parameters from dictionary or defaults
        if (library.LibraryParameters != null && library.LibraryParameters.Count > 0)
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
        foreach (var component in library.Components)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.WriteStringBlock(component.Name);
            WriteFootprint(cf, component, sectionKeys);
        }

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    private static void WriteFootprint(CompoundFile cf, IPcbComponent component, Dictionary<string, string> sectionKeys)
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
        WriteAdditionalComponentStreams(footprintStorage, component);
    }

    private static void WriteFootprintParameters(CFStorage storage, IPcbComponent component)
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
        parameters["HEIGHT"] = component.Height.ToRaw().ToString();
        if (!string.IsNullOrEmpty(component.Description))
            parameters["DESCRIPTION"] = component.Description;
        if (component is PcbComponent pcbComp2)
        {
            if (!string.IsNullOrEmpty(pcbComp2.ItemGUID))
                parameters["ITEMGUID"] = pcbComp2.ItemGUID;
            if (!string.IsNullOrEmpty(pcbComp2.ItemRevisionGUID))
                parameters["REVISIONGUID"] = pcbComp2.ItemRevisionGUID;
        }
        writer.WriteCStringParameterBlock(parameters);

        writer.Flush();
        paramsStream.SetData(ms.ToArray());
    }

    private static void WriteWideStrings(CFStorage storage, IPcbComponent component)
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

    private static void WriteFootprintData(CFStorage storage, IPcbComponent component)
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

    internal static void WriteCommonPrimitiveData(BinaryFormatWriter writer, int layer, ushort flags = 0)
    {
        writer.Write((byte)layer);
        writer.Write(flags);
        writer.WriteFill(0xFF, 10); // 10 bytes of 0xFF
    }

    /// <summary>
    /// Encodes boolean properties into the flags word. All bits are computed from properties.
    /// </summary>
    internal static ushort EncodeFlags(bool isLocked, bool isTentingTop,
        bool isTentingBottom, bool isKeepout)
    {
        ushort flags = 0;
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

    internal static void WriteArc(BinaryFormatWriter writer, PcbArc arc)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(arc.IsLocked, arc.IsTentingTop,
                arc.IsTentingBottom, arc.IsKeepout);
            WriteCommonPrimitiveData(w, arc.Layer, flags);
            w.WriteCoordPoint(arc.Center);
            w.WriteCoord(arc.Radius);
            w.Write(arc.StartAngle);
            w.Write(arc.EndAngle);
            w.WriteCoord(arc.Width);
        });
    }

    internal static void WritePad(BinaryFormatWriter writer, PcbPad pad)
    {
        // Pad has a complex multi-block structure
        writer.WriteStringBlock(pad.Designator ?? string.Empty);
        writer.WriteBlock(new byte[] { 0 }); // reserved block 1
        writer.WriteStringBlock("|&|0"); // net string (always this in footprint libraries)
        writer.WriteBlock(new byte[] { 0 }); // reserved block 2

        // Main pad data block (114 bytes standard)
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(pad.IsLocked, pad.IsTentingTop,
                pad.IsTentingBottom, pad.IsKeepout);
            WriteCommonPrimitiveData(w, pad.Layer, flags);
            w.WriteCoordPoint(pad.Location);
            w.WriteCoordPoint(pad.SizeTop);
            w.WriteCoordPoint(pad.SizeMiddle);
            w.WriteCoordPoint(pad.SizeBottom);
            w.WriteCoord(pad.HoleSize);
            w.Write((byte)pad.ShapeTop);
            w.Write((byte)pad.ShapeMiddle);
            w.Write((byte)pad.ShapeBottom);
            w.Write(pad.Rotation);
            w.Write(pad.IsPlated);
            // Offset 61-85
            w.Write((byte)0); // offset 61: constant 0
            w.Write((byte)pad.Mode); // offset 62: StackMode
            w.Write((byte)pad.PowerPlaneConnectStyle); // offset 63: PowerPlaneConnectStyle
            w.WriteCoord(pad.ReliefAirGap); // offset 64: ReliefAirGap
            w.WriteCoord(pad.ReliefConductorWidth); // offset 68: ReliefConductorWidth
            w.Write((short)pad.ReliefEntries); // offset 72: ReliefEntries (typically 4)
            w.WriteCoord(pad.PowerPlaneClearance); // offset 74: PowerPlaneClearance
            w.WriteCoord(pad.PowerPlaneReliefExpansion); // offset 78: PowerPlaneReliefExpansion
            w.Write(0); // offset 82: reserved (always 0)
            // Offset 86-93: paste/solder mask expansions
            w.WriteCoord(pad.PasteMaskExpansion);
            w.WriteCoord(pad.SolderMaskExpansion);
            // Offset 94-100: 7 zero bytes
            w.Write(new byte[7]);
            // Offset 101-102: manual mask flags (encoded as 0 or 2)
            w.Write((byte)(pad.PasteMaskExpansion.ToRaw() != 0 ? 2 : 0));
            w.Write((byte)(pad.SolderMaskExpansion.ToRaw() != 0 ? 2 : 0));
            // Offset 103: DrillType
            w.Write((byte)pad.DrillType);
            // Offset 104-105: reserved
            w.Write((short)0);
            // Offset 106-109: reserved
            w.Write(0);
            // Offset 110-111: JumperID
            w.Write((short)pad.JumperID);
            // Offset 112-113: reserved
            w.Write((short)0);
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
        });
    }

    internal static void WriteVia(BinaryFormatWriter writer, PcbVia via)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(via.IsLocked, via.IsTentingTop,
                via.IsTentingBottom, via.IsKeepout);
            WriteCommonPrimitiveData(w, via.Layer, flags);
            w.WriteCoordPoint(via.Location);
            w.WriteCoord(via.Diameter);
            w.WriteCoord(via.HoleSize);
            w.Write((byte)via.StartLayer);
            w.Write((byte)via.EndLayer);
            w.Write((byte)0); // reserved padding byte
            w.WriteCoord(via.ThermalReliefAirGap);
            w.Write((byte)via.ThermalReliefConductors);
            w.Write((byte)0); // reserved padding byte
            w.WriteCoord(via.ThermalReliefConductorsWidth);
            w.WriteCoord(via.PowerPlaneClearance);
            w.WriteCoord(via.PowerPlaneReliefExpansion);
            w.Write(0); // reserved int
            w.WriteCoord(via.SolderMaskExpansion);
            // 8 bytes: post-solder-mask flags (default pattern)
            w.Write(new byte[] { 0, 0, 0, 1, 1, 1, 1, 0 });
            w.Write((byte)(via.SolderMaskExpansionManual ? 2 : 0));
            w.Write((byte)1); // reserved (usually 1)
            w.Write((short)0); // reserved
            w.Write(0); // reserved
            w.Write((byte)via.Mode); // diameter stack mode

            // Write 32 diameter values
            for (var i = 0; i < 32; i++)
                w.WriteCoord(via.Diameters[i]);

            w.Write((short)15); // reserved constant
            w.Write(259); // reserved constant
        });
    }

    internal static void WriteTrack(BinaryFormatWriter writer, PcbTrack track)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(track.IsLocked, track.IsTentingTop,
                track.IsTentingBottom, track.IsKeepout);
            WriteCommonPrimitiveData(w, track.Layer, flags);
            w.WriteCoordPoint(track.Start);
            w.WriteCoordPoint(track.End);
            w.WriteCoord(track.Width);
            w.Write(track.NetIndex);
            w.Write(track.ComponentIndex);
        });
    }

    internal static void WriteText(BinaryFormatWriter writer, PcbText text, int wideStringIndex)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(text.IsLocked, text.IsTentingTop,
                text.IsTentingBottom, text.IsKeepout);
            WriteCommonPrimitiveData(w, text.Layer, flags);
            w.WriteCoordPoint(text.Location);
            w.WriteCoord(text.Height);
            w.Write((short)text.StrokeFont);
            w.Write(text.Rotation);
            w.Write(text.IsMirrored);
            w.WriteCoord(text.StrokeWidth);

            // Extended data
            w.Write((short)0); // ReservedExt1
            w.Write((byte)0); // ReservedExt2
            w.Write((byte)text.TextKind);
            w.Write(text.FontBold);
            w.Write(text.FontItalic);
            w.WriteFontName(text.FontName ?? "Arial");
            w.WriteCoord(text.BarcodeLRMargin);
            w.WriteCoord(text.BarcodeTBMargin);
            w.Write(0); // ReservedExt3
            w.Write(0); // ReservedExt4
            w.Write((byte)0); // ReservedExt5
            w.Write((byte)0); // ReservedExt6
            w.Write(0); // ReservedExt7
            w.Write((short)0); // ReservedExt8
            w.Write(1); // ReservedExt9 (usually 1)
            w.Write(0); // ReservedExt10
            w.Write(text.IsInverted);
            w.WriteCoord(text.InvertedBorder);
            w.Write(wideStringIndex); // wide strings index
            w.Write(0); // ReservedExt11
            w.Write(text.UseInvertedRectangle);
            w.WriteCoord(text.InvertedRectWidth);
            w.WriteCoord(text.InvertedRectHeight);
            w.Write((byte)text.InvertedRectJustification);
            w.WriteCoord(text.InvertedRectTextOffset);
        });

        writer.WriteStringBlock(text.Text);
    }

    internal static void WriteFill(BinaryFormatWriter writer, PcbFill fill)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(fill.IsLocked, fill.IsTentingTop,
                fill.IsTentingBottom, fill.IsKeepout);
            WriteCommonPrimitiveData(w, fill.Layer, flags);
            w.WriteCoordPoint(fill.Corner1);
            w.WriteCoordPoint(fill.Corner2);
            w.Write(fill.Rotation);
        });
    }

    internal static void WriteRegion(BinaryFormatWriter writer, PcbRegion region)
    {
        writer.WriteBlock(w =>
        {
            var flags = EncodeFlags(region.IsLocked, region.IsTentingTop,
                region.IsTentingBottom, region.IsKeepout);
            WriteCommonPrimitiveData(w, region.Layer, flags);

            // Structure: uint32 prefix + byte prefix + nested parameter block + outline vertices (doubles)
            w.Write((uint)0); // reserved prefix 1
            w.Write((byte)0); // reserved prefix 2

            // Generate parameters from typed properties
            var regionParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Merge any additional parameters first (typed properties override)
            if (region.AdditionalParameters != null)
            {
                foreach (var kvp in region.AdditionalParameters)
                    regionParams[kvp.Key] = kvp.Value;
            }
            if (region.Kind != 0)
                regionParams["KIND"] = region.Kind.ToString();
            if (!string.IsNullOrEmpty(region.Net))
                regionParams["NET"] = region.Net;
            if (!string.IsNullOrEmpty(region.UniqueId))
                regionParams["UNIQUEID"] = region.UniqueId;
            if (!string.IsNullOrEmpty(region.Name))
                regionParams["NAME"] = region.Name;
            w.WriteCStringParameterBlock(regionParams);

            // Write outline vertices as doubles (Altium PCB format)
            w.Write((uint)region.Outline.Count);
            foreach (var point in region.Outline)
            {
                w.Write((double)point.X.ToRaw());
                w.Write((double)point.Y.ToRaw());
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
            WriteCommonPrimitiveData(w, binaryLayer, flags);

            // Structure: uint32 prefix + byte prefix + nested parameter block + outline vertices (doubles)
            w.Write((uint)0); // reserved prefix 1
            w.Write((byte)0); // reserved prefix 2

            // Generate ALL parameters from typed properties
            var bodyParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Merge any additional parameters first (typed properties override)
            if (body.AdditionalParameters != null)
            {
                foreach (var kvp in body.AdditionalParameters)
                    bodyParams[kvp.Key] = kvp.Value;
            }
            bodyParams["V7_LAYER"] = body.LayerName ?? "MECHANICAL1";
            bodyParams["NAME"] = body.Name ?? string.Empty;
            bodyParams["KIND"] = body.Kind.ToString();
            bodyParams["SUBPOLYINDEX"] = body.SubPolyIndex.ToString();
            bodyParams["UNIONINDEX"] = body.UnionIndex.ToString();
            bodyParams["ARCRESOLUTION"] = body.ArcResolution.ToString(System.Globalization.CultureInfo.InvariantCulture);
            bodyParams["ISSHAPEBASED"] = body.IsShapeBased ? "TRUE" : "FALSE";
            bodyParams["CAVITYHEIGHT"] = body.CavityHeight.ToRaw().ToString();
            bodyParams["STANDOFFHEIGHT"] = body.StandoffHeight.ToRaw().ToString();
            bodyParams["OVERALLHEIGHT"] = body.OverallHeight.ToRaw().ToString();
            bodyParams["BODYCOLOR3D"] = body.BodyColor3D.ToString();
            bodyParams["BODYOPACITY3D"] = body.BodyOpacity3D.ToString(System.Globalization.CultureInfo.InvariantCulture);
            bodyParams["BODYPROJECTION"] = body.BodyProjection.ToString();
            bodyParams["MODELID"] = body.ModelId ?? string.Empty;
            bodyParams["MODEL.EMBED"] = body.ModelEmbed ? "TRUE" : "FALSE";
            bodyParams["MODEL.2D.X"] = body.Model2DLocation.X.ToRaw().ToString();
            bodyParams["MODEL.2D.Y"] = body.Model2DLocation.Y.ToRaw().ToString();
            bodyParams["MODEL.2D.ROTATION"] = body.Model2DRotation.ToString(System.Globalization.CultureInfo.InvariantCulture);
            bodyParams["MODEL.3D.ROTX"] = body.Model3DRotX.ToString(System.Globalization.CultureInfo.InvariantCulture);
            bodyParams["MODEL.3D.ROTY"] = body.Model3DRotY.ToString(System.Globalization.CultureInfo.InvariantCulture);
            bodyParams["MODEL.3D.ROTZ"] = body.Model3DRotZ.ToString(System.Globalization.CultureInfo.InvariantCulture);
            bodyParams["MODEL.3D.DZ"] = body.Model3DDz.ToRaw().ToString();
            bodyParams["MODEL.CHECKSUM"] = body.ModelChecksum.ToString();
            bodyParams["MODEL.NAME"] = body.ModelName ?? string.Empty;
            bodyParams["MODEL.MODELTYPE"] = body.ModelType.ToString();
            bodyParams["MODEL.MODELSOURCE"] = body.ModelSource ?? string.Empty;
            if (!string.IsNullOrEmpty(body.Identifier))
                bodyParams["IDENTIFIER"] = body.Identifier;
            if (!string.IsNullOrEmpty(body.Texture))
                bodyParams["TEXTURE"] = body.Texture;
            w.WriteCStringParameterBlock(bodyParams);

            // Write outline vertices as doubles (Altium PCB format)
            w.Write((uint)body.Outline.Count);
            foreach (var point in body.Outline)
            {
                w.Write((double)point.X.ToRaw());
                w.Write((double)point.Y.ToRaw());
            }
        });
    }

    private static void WriteUniqueIdPrimitiveInformation(CFStorage storage, IPcbComponent component)
    {
        // UniqueIdPrimitiveInformation is optional - skip for new files
    }

    private static void WriteLibraryModels(CFStorage libraryStorage, PcbLibrary library)
    {
        var modelsStorage = libraryStorage.AddStorage("Models");
        var modelCount = library.Models.Count;

        WriteStorageHeader(modelsStorage, modelCount);

        // Write Data stream: model metadata as length-prefixed C-string parameter blocks
        using var dataMs = new MemoryStream();
        foreach (var model in library.Models)
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
        for (var i = 0; i < library.Models.Count; i++)
        {
            var model = library.Models[i];
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

    private static void WriteAdditionalComponentStreams(CFStorage storage, IPcbComponent component)
    {
        if (component is PcbComponent { AdditionalStreams: not null } pcbComp)
            WriteAdditionalStreams(storage, pcbComp.AdditionalStreams);
    }

    private static void WriteAdditionalLibraryStreams(CFStorage libraryStorage, PcbLibrary library)
    {
        if (library.AdditionalLibraryStreams != null)
            WriteAdditionalStreams(libraryStorage, library.AdditionalLibraryStreams);
    }

    private static void WriteAdditionalRootStreams(CompoundFile cf, PcbLibrary library)
    {
        if (library.AdditionalRootStreams != null)
            WriteAdditionalStreams(cf.RootStorage, library.AdditionalRootStreams);
    }

    internal static void WriteAdditionalStreams(CFStorage storage, Dictionary<string, byte[]> streams)
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
