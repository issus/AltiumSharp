using System.Globalization;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using OriginalCircuit.Altium.Serialization.Compound;

namespace OriginalCircuit.Altium.Serialization.Writers;

/// <summary>
/// Writes PCB document (.PcbDoc) files.
/// PcbDoc files store primitives in separate storages per type
/// (e.g., [Arcs6], [Pads6], [Tracks6]).
/// </summary>
public sealed class PcbDocWriter
{
    /// <summary>
    /// The fixed 24-byte legacy <c>FileHeader</c> stamp every PcbDoc carries: <c>uint32</c> char-count
    /// 19 followed by the truncated UTF-16LE text <c>"PCB 5.0 Bi"</c> (docs/decompile/fileheaders.md §3).
    /// Entirely constant — no per-document data.
    /// </summary>
    private static readonly byte[] LegacyFileHeaderStamp = BuildLegacyFileHeaderStamp();

    private static byte[] BuildLegacyFileHeaderStamp()
    {
        var payload = System.Text.Encoding.Unicode.GetBytes("PCB 5.0 Bi"); // 20 bytes UTF-16LE
        var stamp = new byte[4 + payload.Length];
        BitConverter.TryWriteBytes(stamp, 19);                              // char-count = 19 (constant)
        payload.CopyTo(stamp, 4);
        return stamp;
    }

    /// <summary>
    /// Writes a PcbDoc file to the specified path.
    /// </summary>
    /// <param name="document">The PCB document to write.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="overwrite">If true, overwrites an existing file; otherwise throws if the file exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public async ValueTask WriteAsync(PcbDocument document, string path, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        await using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await WriteAsync(document, stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a PcbDoc file to a stream.
    /// </summary>
    /// <param name="document">The PCB document to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public async ValueTask WriteAsync(PcbDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        Write(document, ms, cancellationToken);
        ms.Position = 0;
        await ms.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a PcbDoc file to a stream synchronously.
    /// </summary>
    /// <param name="document">The PCB document to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public void Write(PcbDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        using var cf = CompoundFileAccessor.Create();

        WriteFileHeader(cf, document);
        WriteFileHeaderSix(cf, document);
        WriteBoard(cf, document);
        WriteNets(cf, document);
        cancellationToken.ThrowIfCancellationRequested();
        WriteArcs(cf, document);
        WritePads(cf, document);
        WriteVias(cf, document);
        WriteTracks(cf, document);
        cancellationToken.ThrowIfCancellationRequested();
        WriteTexts(cf, document);
        WriteFills(cf, document);
        WriteRegions(cf, document);
        WriteBoardRegions(cf, document);
        WriteShapeBased(cf, document, "ShapeBasedRegions6", document.ShapeBasedRegions);
        WriteShapeBased(cf, document, "ShapeBasedComponentBodies6", document.ShapeBasedComponentBodies);
        WritePrimitiveParameters(cf, document);
        WriteExtendedPrimitiveInformation(cf, document);
        WriteNamedParameterStorages(cf, document);
        WriteEmbeddedModels(cf, document);
        WriteComponentBodies(cf, document);
        cancellationToken.ThrowIfCancellationRequested();
        WritePolygons(cf, document);
        WriteComponents(cf, document);
        WriteEmbeddedBoards(cf, document);
        WriteRules(cf, document);
        WriteClasses(cf, document);
        WriteSignalClasses(cf, document);
        WriteSmartUnions(cf, document);
        WriteUnionNames(cf, document);
        WriteDifferentialPairs(cf, document);
        WriteRooms(cf, document);
        WriteWideStrings(cf, document);
        // Empty optional feature storages (no instances in the corpus) reproduced exactly: a
        // [u32 count=0] Header + empty Data. Removing them from the AdditionalStreams catch-all.
        WriteEmptyStorageIfPresent(cf, document, "Dimensions6");
        WriteEmptyStorageIfPresent(cf, document, "Coordinates6");
        WriteEmptyStorageIfPresent(cf, document, "FromTos6");
        WriteEmptyStorageIfPresent(cf, document, "Embeddeds6");
        WriteEmptyStorageIfPresent(cf, document, "Textures");
        WriteEmptyStorageIfPresent(cf, document, "ModelsNoEmbed");
        WriteDocumentPrimitiveGuids(cf, document);
        WriteDocumentPrimitiveUniqueIds(cf, document);
        PcbLibWriter.WriteFileVersionInfo(cf.RootStorage, document.FileVersionInfo);
        if (document.LayerKindMapping is { } lkm) PcbLibWriter.WriteLayerKindMapping(cf.RootStorage, lkm);
        if (document.PadViaLibrary is { } pvl) PcbLibWriter.WritePadViaLibrary(cf.RootStorage, pvl, "PadViaLibrary");
        if (document.PadViaLibraryCache is { } pvlc) PcbLibWriter.WritePadViaLibrary(cf.RootStorage, pvlc, "PadViaLibraryCache");
        WriteEmptyStorageIfPresent(cf, document, "PadViaLibraryLinks");
        WriteAdditionalStreams(cf, document);

        cf.Save(stream);
    }

    private static void WriteFileHeader(CompoundFileAccessor cf, PcbDocument document)
    {
        var headerStream = cf.RootStorage.AddStream("FileHeader");

        // Fully modeled — no replay. The legacy PcbDoc FileHeader is a fixed 24-byte stamp
        // (docs/decompile/fileheaders.md §3): a uint32 char-count of 19 followed by the truncated
        // UTF-16LE text "PCB 5.0 Bi" (20 bytes). It is entirely constant — no per-document data — so
        // we emit the exact bytes. (The real 6.0 version stamp + per-document GUID live in
        // FileHeaderSix; see WriteFileHeaderSix.)
        headerStream.SetData(LegacyFileHeaderStamp);
    }

    private static void WriteFileHeaderSix(CompoundFileAccessor cf, PcbDocument document)
    {
        // The modern 6.0 version stamp + per-document GUID (docs/decompile/fileheaders.md §4). Fully
        // modeled — no replay. 75-byte two-block layout: version text + 5.01 double (both Reserved
        // constants) and the document GUID (Identity) as a brace-wrapped uppercase token. A loaded
        // document without a FileHeaderSix stream leaves FileGuid null and emits nothing.
        if (document.FileGuid is not Guid guid)
            return;

        var headerStream = cf.RootStorage.AddStream("FileHeaderSix");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        const string versionText = "PCB 6.0 Binary File";   // Reserved constant
        writer.Write(versionText.Length);
        writer.WritePascalShortString(versionText);

        writer.Write(5.01d);                                 // Reserved constant (no length prefix)

        var guidText = "{" + guid.ToString("D").ToUpperInvariant() + "}";   // Identity (38 chars)
        writer.Write(guidText.Length);
        writer.WritePascalShortString(guidText);

        writer.Flush();
        headerStream.SetData(ms.ToArray());
    }

    private static void WriteBoard(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.BoardParameters == null || document.BoardParameters.Count == 0)
            return;

        var storage = cf.RootStorage.AddStorage("Board6");
        PcbLibWriter.WriteStorageHeader(storage, 1); // Board6 always holds exactly one board record

        var dataStream = storage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        if (document.BoardParametersOrdered is { Count: > 0 } ordered)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in ordered)
                sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
            writer.WriteCStringParameterBlockRaw(sb.ToString());
        }
        else
        {
            writer.WriteCStringParameterBlock(document.BoardParameters);
        }

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    /// <summary>
    /// Writes an empty Header(count=0) + empty Data storage when the named storage existed in the
    /// source file but its collection is now empty, so a present-but-empty storage round-trips.
    /// </summary>
    private static void WriteEmptyStorageIfPresent(CompoundFileAccessor cf, PcbDocument document, string name)
    {
        if (!document.PresentStorages.Contains(name))
            return;
        var storage = cf.RootStorage.AddStorage(name);
        PcbLibWriter.WriteStorageHeader(storage, 0);
        // Emit an explicit empty Data stream (the lazy stream handle only materializes on SetData).
        storage.AddStream("Data").SetData([]);
    }

    private static void WriteNets(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.Nets.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "Nets6");
            return;
        }

        var storage = cf.RootStorage.AddStorage("Nets6");
        PcbLibWriter.WriteStorageHeader(storage, document.Nets.Count);

        var dataStream = storage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        foreach (var net in document.Nets)
            writer.WriteCStringParameterBlockRaw(BuildNetParamText(net));

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    // Serializes a PcbNet to its Nets6 parameter block in Altium's canonical key order/formatting,
    // so a fully typed net round-trips byte-for-byte without replaying the captured block.
    private static string BuildNetParamText(PcbNet net)
    {
        var sb = new System.Text.StringBuilder();
        void Add(string k, string v) => sb.Append('|').Append(k).Append('=').Append(v);
        static string B(bool b) => b ? "TRUE" : "FALSE";
        static string I(int v) => v.ToString(CultureInfo.InvariantCulture);

        Add("SELECTION", B(net.Selection));
        Add("LAYER", net.Layer);
        Add("LOCKED", B(net.Locked));
        Add("POLYGONOUTLINE", B(net.PolygonOutline));
        Add("USERROUTED", B(net.UserRouted));
        Add("KEEPOUT", B(net.Keepout));
        Add("UNIONINDEX", I(net.UnionIndex));
        Add("PRIMITIVELOCK", B(net.PrimitiveLock));
        Add("NAME", net.Name);
        Add("VISIBLE", B(net.Visible));
        Add("COLOR", I(net.Color));
        Add("LOOPREMOVAL", B(net.LoopRemoval));
        Add("OVERRIDECOLORFORDRAW", B(net.OverrideColorForDraw));
        if (net.TargetLengthUnits is { } tl)
            Add("TARGETLENGTH", FormatMilUnits(tl));
        if (net.LayerMinRoutedWidths is { } widths)
            foreach (var w in widths)
                Add(w.LayerKey + "_MRWIDTH", FormatMilUnits(w.WidthUnits));
        if (net.MinRoutedViaSizeUnits is { } mvs)
            Add("MRVIASIZE", FormatMilUnits(mvs));
        if (net.MinRoutedViaHoleUnits is { } mvh)
            Add("MRVIAHOLE", FormatMilUnits(mvh));
        Add("UNIQUEID", net.UniqueId);
        Add("JUMPERSVISIBLE", B(net.JumpersVisible));
        if (net.RoutedLength is { } rl)
            Add("ROUTEDLENGTH", I(rl));
        if (net.ManhattanLength is { } ml)
            Add("MANHATTANLENGTH", I(ml));
        if (net.DelayTotal is { } dt)
            Add("DELAYTOTAL", D(dt));
        if (net.SignalLength is { } sl)
            Add("SIGNALLENGTH", I(sl));
        if (net.SignalDelay is { } sd)
            Add("SIGNALDELAY", D(sd));
        if (net.CurrentTotal is { } ct)
            Add("CURRENTTOTAL", D(ct));
        if (net.ResistanceTotal is { } rt)
            Add("RESISTANCETOTAL", D(rt));
        return sb.ToString();
    }

    // 15 significant digits matches Delphi's FloatToStr (e.g. 1.8542496354265E-10, 0.819995004733263).
    // .NET pads the exponent to two digits (1.856…E-09); Delphi uses a minimal exponent (E-9), so strip
    // a single leading zero from a one-digit exponent.
    private static string D(double v) =>
        System.Text.RegularExpressions.Regex.Replace(
            v.ToString("G15", CultureInfo.InvariantCulture), @"E([+-])0(\d)$", "E$1$2");

    // Formats internal coordinate units (1 mil = 10000) as Altium's mil text, e.g. 118110 -> "11.811mil".
    private static string FormatMilUnits(int units) =>
        (units / 10000.0).ToString("0.#####", CultureInfo.InvariantCulture) + "mil";

    private static void WriteParameterBlockStorage(CompoundFileAccessor cf, string storageName, IReadOnlyList<Dictionary<string, string>> parameterSets)
    {
        if (parameterSets.Count == 0)
            return;

        var storage = cf.RootStorage.AddStorage(storageName);
        PcbLibWriter.WriteStorageHeader(storage, parameterSets.Count);

        var dataStream = storage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        foreach (var parameters in parameterSets)
        {
            writer.WriteCStringParameterBlock(parameters);
        }

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    private static void WriteRules(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.Rules.Count == 0)
            return;

        var storage = cf.RootStorage.AddStorage("Rules6");
        PcbLibWriter.WriteStorageHeader(storage, document.Rules.Count);
        var dataStream = storage.AddStream("Data");

        using var ms = new MemoryStream();
        foreach (var rule in document.Rules)
        {
            // Typed rule kinds serialize from named properties (common header + kind body) in canonical
            // order; not-yet-modeled kinds replay the captured ordered list. Framed as
            // [2-byte leader][4-byte length][text][null].
            string text;
            if (rule.IsModeled)
            {
                var pairs = new List<KeyValuePair<string, string>>();
                void Add(string k, string v) => pairs.Add(new KeyValuePair<string, string>(k, v));
                rule.WriteCommonHeader(Add);
                rule.WriteBody(Add);
                text = BuildUnicodeAwareParamString(pairs);
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                if (rule.RawParametersOrdered is { Count: > 0 } ordered)
                    foreach (var kvp in ordered) sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
                else
                    foreach (var kvp in rule.ToParameters()) sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
                text = sb.ToString();
            }

            var textBytes = AltiumEncoding.Windows1252.GetBytes(text);
            var length = textBytes.Length + 1; // include the trailing null
            ms.WriteByte((byte)(rule.RawLeader & 0xFF));
            ms.WriteByte((byte)((rule.RawLeader >> 8) & 0xFF));
            ms.WriteByte((byte)(length & 0xFF));
            ms.WriteByte((byte)((length >> 8) & 0xFF));
            ms.WriteByte((byte)((length >> 16) & 0xFF));
            ms.WriteByte((byte)((length >> 24) & 0xFF));
            ms.Write(textBytes, 0, textBytes.Length);
            ms.WriteByte(0);
        }

        dataStream.SetData(ms.ToArray());
    }

    private static void WriteClasses(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.Classes.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "Classes6");
            return;
        }

        var texts = new List<string>();
        foreach (var objectClass in document.Classes)
            texts.Add(BuildClassParamText(objectClass));
        WriteParameterStringStorage(cf, "Classes6", texts);
    }

    // Serializes a PcbObjectClass to its Classes6 parameter block in Altium's canonical key order.
    private static string BuildClassParamText(PcbObjectClass oc)
    {
        var sb = new System.Text.StringBuilder();
        void Add(string k, string v) => sb.Append('|').Append(k).Append('=').Append(v);
        static string B(bool b) => b ? "TRUE" : "FALSE";

        Add("SELECTION", B(oc.Selection));
        Add("LAYER", oc.Layer);
        Add("LOCKED", B(oc.Locked));
        Add("POLYGONOUTLINE", B(oc.PolygonOutline));
        Add("USERROUTED", B(oc.UserRouted));
        Add("KEEPOUT", B(oc.Keepout));
        Add("UNIONINDEX", oc.UnionIndex.ToString(CultureInfo.InvariantCulture));
        Add("NAME", oc.Name);
        Add("KIND", oc.Kind);
        Add("SUPERCLASS", B(oc.SuperClass));
        if (oc.AutoGeneratedClass is { } agc)
            Add("AUTOGENERATEDCLASS", B(agc));
        for (var i = 0; i < oc.Members.Count; i++)
            Add("M" + i.ToString(CultureInfo.InvariantCulture), oc.Members[i]);
        Add("SELECTED", B(oc.Selected));
        Add("SCHAUTOGENERATEDCLUSTER", B(oc.SchAutoGeneratedCluster));
        Add("UNIQUEID", oc.UniqueId);
        if (oc.AutoGeneratedClassKind is { } agck)
            Add("AUTOGENERATEDCLASSKIND", agck.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private static void WriteSignalClasses(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.SignalClasses.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "SignalClasses");
            return;
        }

        var texts = new List<string>();
        foreach (var sc in document.SignalClasses)
            texts.Add(BuildSignalClassParamText(sc));
        WriteParameterStringStorage(cf, "SignalClasses", texts);
    }

    // Serializes a PcbSignalClass to its SignalClasses parameter block in canonical key order.
    private static string BuildSignalClassParamText(PcbSignalClass sc)
    {
        var sb = new System.Text.StringBuilder();
        void Add(string k, string v) => sb.Append('|').Append(k).Append('=').Append(v);
        static string B(bool b) => b ? "TRUE" : "FALSE";

        Add("SELECTION", B(sc.Selection));
        Add("LAYER", sc.Layer);
        Add("LOCKED", B(sc.Locked));
        Add("POLYGONOUTLINE", B(sc.PolygonOutline));
        Add("USERROUTED", B(sc.UserRouted));
        Add("KEEPOUT", B(sc.Keepout));
        Add("UNIONINDEX", sc.UnionIndex.ToString(CultureInfo.InvariantCulture));
        Add("NAME", sc.Name);
        Add("KIND", sc.Kind.ToString(CultureInfo.InvariantCulture));
        Add("SUPERCLASS", B(sc.SuperClass));
        Add("SELECTED", B(sc.Selected));
        Add("SCHAUTOGENERATEDCLUSTER", B(sc.SchAutoGeneratedCluster));
        Add("UNIQUEID", sc.UniqueId ?? string.Empty);
        return sb.ToString();
    }

    private static void WriteSmartUnions(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.SmartUnions.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "SmartUnions");
            return;
        }
        var texts = new List<string>();
        foreach (var u in document.SmartUnions)
            texts.Add(BuildUnicodeAwareParamString(BuildUnionPairs(u)));
        WriteParameterStringStorage(cf, "SmartUnions", texts);
    }

    // Reassembles a SmartUnions record's ordered key list from its typed members: each member emits
    // the common-primitive prefix in canonical order followed by its member-specific parameters.
    private static List<KeyValuePair<string, string>> BuildUnionPairs(PcbSmartUnion u)
    {
        string B(bool x) => x ? "TRUE" : "FALSE";
        var pairs = new List<KeyValuePair<string, string>>();
        void Add(string k, string v) => pairs.Add(new KeyValuePair<string, string>(k, v));
        foreach (var m in u.Members)
        {
            Add("SELECTION", B(m.Selection));
            Add("LAYER", m.Layer);
            Add("LOCKED", B(m.Locked));
            Add("POLYGONOUTLINE", B(m.PolygonOutline));
            Add("USERROUTED", B(m.UserRouted));
            Add("KEEPOUT", B(m.Keepout));
            Add("UNIONINDEX", m.UnionIndex.ToString(CultureInfo.InvariantCulture));
            pairs.AddRange(m.Parameters);
        }
        return pairs;
    }

    private static void WriteUnionNames(CompoundFileAccessor cf, PcbDocument document)
    {
        // UnionNames/Header is the constant 1; Data is [u32 count][per record...] and is always present
        // (a [u32 0] for an empty list) whenever the storage existed.
        if (document.UnionNames.Count == 0 && !document.PresentStorages.Contains("UnionNames"))
            return;
        var storage = cf.RootStorage.AddStorage("UnionNames");
        PcbLibWriter.WriteStorageHeader(storage, 1);
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes((uint)document.UnionNames.Count));
        foreach (var n in document.UnionNames)
        {
            var nameBytes = System.Text.Encoding.Unicode.GetBytes(n.Name + '\0'); // UTF-16LE + 00 00
            ms.Write(BitConverter.GetBytes(n.UnionIndex));
            ms.Write(BitConverter.GetBytes((uint)nameBytes.Length));
            ms.Write(nameBytes);
        }
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    private static void WriteDifferentialPairs(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.DifferentialPairs.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "DifferentialPairs6");
            return;
        }

        var texts = new List<string>();
        foreach (var pair in document.DifferentialPairs)
            texts.Add(BuildDifferentialPairParamText(pair));
        WriteParameterStringStorage(cf, "DifferentialPairs6", texts);
    }

    // Serializes a PcbDifferentialPair to its DifferentialPairs6 parameter block in canonical key order.
    private static string BuildDifferentialPairParamText(PcbDifferentialPair pair)
    {
        var sb = new System.Text.StringBuilder();
        void Add(string k, string v) => sb.Append('|').Append(k).Append('=').Append(v);
        static string B(bool b) => b ? "TRUE" : "FALSE";

        Add("SELECTION", B(pair.Selection));
        Add("LAYER", pair.Layer);
        Add("LOCKED", B(pair.Locked));
        Add("POLYGONOUTLINE", B(pair.PolygonOutline));
        Add("USERROUTED", B(pair.UserRouted));
        Add("KEEPOUT", B(pair.Keepout));
        Add("UNIONINDEX", pair.UnionIndex.ToString(CultureInfo.InvariantCulture));
        Add("POSITIVENETNAME", pair.PositiveNetName);
        Add("NEGATIVENETNAME", pair.NegativeNetName);
        Add("NAME", pair.Name);
        Add("GATHERCONTROL", B(pair.GatherControl));
        Add("UNIQUEID", pair.UniqueId);
        return sb.ToString();
    }

    private static void WriteRooms(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.Rooms.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "Rooms6");
            return;
        }

        var texts = new List<string>();
        foreach (var room in document.Rooms)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("|NAME=").Append(room.Name);
            if (!string.IsNullOrEmpty(room.UniqueId))
                sb.Append("|UNIQUEID=").Append(room.UniqueId);
            texts.Add(sb.ToString());
        }
        WriteParameterStringStorage(cf, "Rooms6", texts);
    }

    /// <summary>
    /// Builds a pipe-delimited parameter string, preferring the ordered list (verbatim round-trip)
    /// and falling back to the typed dictionary for items created from scratch.
    /// </summary>
    private static string BuildParamText(List<KeyValuePair<string, string>>? ordered, Dictionary<string, string> fallback)
    {
        var sb = new System.Text.StringBuilder();
        if (ordered is { Count: > 0 })
        {
            foreach (var kvp in ordered)
                sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
        }
        else
        {
            foreach (var kvp in fallback)
                sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Writes a list of pre-formatted parameter strings as a Header + Data storage, each framed
    /// as a length-prefixed C-string block.
    /// </summary>
    private static void WriteParameterStringStorage(CompoundFileAccessor cf, string storageName, List<string> texts)
    {
        if (texts.Count == 0)
            return;

        var storage = cf.RootStorage.AddStorage(storageName);
        PcbLibWriter.WriteStorageHeader(storage, texts.Count);
        var dataStream = storage.AddStream("Data");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);
        foreach (var text in texts)
            writer.WriteCStringParameterBlockRaw(text);
        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    private static void WriteArcs(CompoundFileAccessor cf, PcbDocument document)
    {
        WritePrimitiveStorage(cf, "Arcs6", document.Arcs, (writer, arc) =>
        {
            writer.Write((byte)1);
            PcbLibWriter.WriteArc(writer, (PcbArc)arc);
        });
    }

    private static void WritePads(CompoundFileAccessor cf, PcbDocument document)
    {
        WritePrimitiveStorage(cf, "Pads6", document.Pads, (writer, pad) =>
        {
            writer.Write((byte)2);
            PcbLibWriter.WritePad(writer, (PcbPad)pad);
        });
    }

    private static void WriteVias(CompoundFileAccessor cf, PcbDocument document)
    {
        WritePrimitiveStorage(cf, "Vias6", document.Vias, (writer, via) =>
        {
            writer.Write((byte)3);
            PcbLibWriter.WriteVia(writer, (PcbVia)via);
        });
    }

    private static void WriteTracks(CompoundFileAccessor cf, PcbDocument document)
    {
        WritePrimitiveStorage(cf, "Tracks6", document.Tracks, (writer, track) =>
        {
            writer.Write((byte)4);
            PcbLibWriter.WriteTrack(writer, (PcbTrack)track);
        });
    }

    private static void WriteTexts(CompoundFileAccessor cf, PcbDocument document)
    {
        var textIndex = 0;
        WritePrimitiveStorage(cf, "Texts6", document.Texts, (writer, text) =>
        {
            writer.Write((byte)5);
            PcbLibWriter.WriteText(writer, (PcbText)text, textIndex++);
        });
    }

    private static void WriteFills(CompoundFileAccessor cf, PcbDocument document)
    {
        WritePrimitiveStorage(cf, "Fills6", document.Fills, (writer, fill) =>
        {
            writer.Write((byte)6);
            PcbLibWriter.WriteFill(writer, (PcbFill)fill);
        });
    }

    private static void WriteRegions(CompoundFileAccessor cf, PcbDocument document)
    {
        WritePrimitiveStorage(cf, "Regions6", document.Regions, (writer, region) =>
        {
            writer.Write((byte)11);
            PcbLibWriter.WriteRegion(writer, (PcbRegion)region);
        });
    }

    private static void WriteDocumentPrimitiveGuids(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.PrimitiveGuids.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "PrimitiveGuids");
            return;
        }
        var storage = cf.RootStorage.AddStorage("PrimitiveGuids");
        PcbLibWriter.WriteStorageHeader(storage, document.PrimitiveGuids.Count);
        using var ms = new MemoryStream();
        foreach (var g in document.PrimitiveGuids)
        {
            ms.Write(BitConverter.GetBytes(g.TypeId));
            ms.Write(BitConverter.GetBytes(g.Index));
            ms.Write(g.Guid.ToByteArray());
        }
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    private static void WriteDocumentPrimitiveUniqueIds(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.PrimitiveUniqueIds.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "UniqueIDPrimitiveInformation");
            return;
        }
        var storage = cf.RootStorage.AddStorage("UniqueIDPrimitiveInformation");
        PcbLibWriter.WriteStorageHeader(storage, document.PrimitiveUniqueIds.Count);
        storage.AddStream("Data").SetData(PcbLibWriter.BuildPrimitiveUniqueIdData(document.PrimitiveUniqueIds));
    }

    private static void WritePrimitiveParameters(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.PrimitiveParameters.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "PrimitiveParameters");
            return;
        }
        // The Header is the captured group-count metric (component count × 3), not the record count.
        var storage = cf.RootStorage.AddStorage("PrimitiveParameters");
        PcbLibWriter.WriteStorageHeader(storage, document.PrimitiveParametersHeader);
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);
        foreach (var rec in document.PrimitiveParameters)
            writer.WriteCStringParameterBlockRaw(BuildParamText(rec.OrderedParameters, rec.Parameters));
        writer.Flush();
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    private static void WriteExtendedPrimitiveInformation(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.ExtendedPrimitiveInfo.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "ExtendedPrimitiveInformation");
            return;
        }
        // Header is the record count; Data is one length-prefixed C-string parameter block per record.
        var storage = cf.RootStorage.AddStorage("ExtendedPrimitiveInformation");
        PcbLibWriter.WriteStorageHeader(storage, document.ExtendedPrimitiveInfo.Count);
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);
        foreach (var info in document.ExtendedPrimitiveInfo)
            writer.WriteCStringParameterBlockRaw(BuildParamText(info.OrderedParameters, info.ToParameters()));
        writer.Flush();
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    private static void WriteEmbeddedModels(CompoundFileAccessor cf, PcbDocument document)
    {
        // Reproduce the Models storage when the source had it (even empty) or the document carries models.
        if (document.Models.Count == 0 && !document.ModelsStoragePresent)
            return;
        PcbLibWriter.WriteModelsStorage(cf.RootStorage, document.Models);
    }

    private static void WriteNamedParameterStorages(CompoundFileAccessor cf, PcbDocument document)
    {
        foreach (var entry in document.NamedParameterStorages)
        {
            // Header is captured verbatim (a record count for some storages, a dirty/modified flag for
            // others). Data is one length-prefixed C-string parameter block per record (empty when none).
            var storage = cf.RootStorage.AddStorage(entry.Name);
            PcbLibWriter.WriteStorageHeader(storage, entry.Header);
            using var ms = new MemoryStream();
            using var writer = new BinaryFormatWriter(ms, leaveOpen: true);
            foreach (var rec in entry.Records)
                writer.WriteCStringParameterBlockRaw(BuildParamText(rec.OrderedParameters, rec.Parameters));
            writer.Flush();
            storage.AddStream("Data").SetData(ms.ToArray());
        }
    }

    private static void WriteShapeBased(CompoundFileAccessor cf, PcbDocument document, string storageName, List<PcbShapeBasedRegion> items)
    {
        if (items.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, storageName);
            return;
        }
        var storage = cf.RootStorage.AddStorage(storageName);
        PcbLibWriter.WriteStorageHeader(storage, items.Count);
        using var ms = new MemoryStream();
        foreach (var r in items)
        {
            // Build the SubRecord-1 body (everything after the length field), then prefix the type byte
            // and the computed SR1 length. The length, the post-component-index "FF FF FF FF 00"
            // (union-index=none + reserved) and the post-hole-count "00 00" are derived/constant — verified
            // across all 209 corpus shape-based records — so nothing here is captured/replayed.
            using var body = new MemoryStream();
            body.WriteByte(r.Layer);
            body.WriteByte(r.Flags1);
            body.WriteByte(r.Flags2);
            body.Write(BitConverter.GetBytes(r.NetIndex));
            body.Write(BitConverter.GetBytes(r.PolygonIndex));
            body.Write(BitConverter.GetBytes(r.ComponentIndex));
            body.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00 });
            body.Write(BitConverter.GetBytes((ushort)r.Holes.Count));
            body.Write(new byte[] { 0x00, 0x00 });
            // Property block: rejoin the ordered KEY=VALUE list byte-identically (no canonical reorder),
            // then re-append the inner terminating NUL(s); byte-exact for any captured or authored block.
            var propsText = string.Join("|", r.Properties.Select(p => p.Value is null ? p.Key : p.Key + "=" + p.Value))
                + new string('\0', r.PropsInnerNulls);
            var propBytes = System.Text.Encoding.UTF8.GetBytes(propsText);
            body.Write(BitConverter.GetBytes(propBytes.Length));
            body.Write(propBytes);
            if (r.PropsHasTrailingNull) body.WriteByte(0);
            body.Write(BitConverter.GetBytes((uint)Math.Max(0, r.Outline.Count - 1)));   // disk count = N-1
            foreach (var v in r.Outline)
            {
                body.WriteByte(v.IsRoundRaw);
                body.Write(BitConverter.GetBytes(v.X)); body.Write(BitConverter.GetBytes(v.Y));
                body.Write(BitConverter.GetBytes(v.CenterX)); body.Write(BitConverter.GetBytes(v.CenterY));
                body.Write(BitConverter.GetBytes(v.Radius));
                body.Write(BitConverter.GetBytes(v.StartAngle)); body.Write(BitConverter.GetBytes(v.EndAngle));
            }
            foreach (var hole in r.Holes)
            {
                body.Write(BitConverter.GetBytes((uint)hole.Count));
                foreach (var (x, y) in hole) { body.Write(BitConverter.GetBytes(x)); body.Write(BitConverter.GetBytes(y)); }
            }
            var bodyBytes = body.ToArray();
            ms.WriteByte(r.TypeByte);
            ms.Write(BitConverter.GetBytes((uint)bodyBytes.Length));    // SR1 length = body length (derived)
            ms.Write(bodyBytes);
        }
        storage.AddStream("Data").SetData(ms.ToArray());
    }

    private static void WriteBoardRegions(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.BoardRegions.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "BoardRegions");
            return;
        }
        WritePrimitiveStorage(cf, "BoardRegions", document.BoardRegions, (writer, region) =>
        {
            writer.Write((byte)11);
            PcbLibWriter.WriteRegion(writer, region);
        });
    }

    private static void WriteComponentBodies(CompoundFileAccessor cf, PcbDocument document)
    {
        WritePrimitiveStorage(cf, "ComponentBodies6", document.ComponentBodies, (writer, body) =>
        {
            writer.Write((byte)12);
            PcbLibWriter.WriteComponentBody(writer, (PcbComponentBody)body);
        });
    }

    private static void WritePolygons(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.Polygons.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "Polygons6");
            return;
        }

        var storage = cf.RootStorage.AddStorage("Polygons6");
        PcbLibWriter.WriteStorageHeader(storage, document.Polygons.Count);

        var dataStream = storage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        foreach (var polygon in document.Polygons)
        {
            WritePolygonParameters(writer, polygon);
        }

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    private static void WritePolygonParameters(BinaryFormatWriter writer, PcbPolygon polygon)
    {
        var pairs = new List<KeyValuePair<string, string>>();
        void Add(string k, string v) => pairs.Add(new KeyValuePair<string, string>(k, v));
        string B(bool x) => x ? "TRUE" : "FALSE";
        string M(Coord c) => FormatMilUnits(c.ToRaw());

        Add("SELECTION", B(polygon.Selection));
        Add("LAYER", LayerByteToName(polygon.Layer));
        Add("LOCKED", B(polygon.Locked));
        Add("POLYGONOUTLINE", B(polygon.PolygonOutline));
        Add("USERROUTED", B(polygon.UserRouted));
        Add("KEEPOUT", B(polygon.IsKeepout));
        Add("UNIONINDEX", polygon.UnionIndex.ToString(CultureInfo.InvariantCulture));
        Add("PRIMITIVELOCK", B(polygon.PrimitiveLock));
        Add("POLYGONTYPE", polygon.PolygonType);
        Add("POUROVER", B(polygon.PourOver));
        Add("REMOVEDEAD", B(polygon.RemoveDead));
        Add("GRIDSIZE", M(polygon.Grid));
        Add("TRACKWIDTH", M(polygon.TrackSize));
        Add("HATCHSTYLE", polygon.HatchStyle);
        Add("USEOCTAGONS", B(polygon.UseOctagons));
        Add("MINPRIMLENGTH", M(polygon.MinTrack));

        // Per-vertex block: use the full typed vertices when present, else synthesize line vertices
        // from the simple CoordPoint outline (from-scratch polygons).
        List<PcbPolygonVertex> verts;
        if (polygon.OutlineVertices.Count > 0)
            verts = polygon.OutlineVertices;
        else
        {
            verts = new List<PcbPolygonVertex>();
            foreach (var pt in polygon.Vertices) verts.Add(new PcbPolygonVertex { X = pt.X, Y = pt.Y });
        }
        for (var i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            Add($"KIND{i}", v.Kind.ToString(CultureInfo.InvariantCulture));
            Add($"VX{i}", M(v.X)); Add($"VY{i}", M(v.Y));
            Add($"CX{i}", M(v.CenterX)); Add($"CY{i}", M(v.CenterY));
            Add($"SA{i}", DelphiExp(v.StartAngle)); Add($"EA{i}", DelphiExp(v.EndAngle));
            Add($"R{i}", M(v.Radius));
        }

        Add("SHELVED", B(polygon.IsHidden));
        Add("RESTORELAYER", polygon.RestoreLayer);
        Add("RESTORENET", polygon.RestoreNet);
        Add("REMOVEISLANDSBYAREA", B(polygon.RemoveIslandsByArea));
        Add("REMOVENECKS", B(polygon.RemoveNarrowNecks));
        Add("AREATHRESHOLD", polygon.AreaThreshold.ToString("F6", CultureInfo.InvariantCulture));
        Add("ARCRESOLUTION", M(polygon.ArcApproximation));
        Add("NECKWIDTHTHRESHOLD", M(polygon.NeckWidthThreshold));
        if (polygon.NeckWidthFromRule is { } nwfr) Add("NECKWIDTHFROMRULE", B(nwfr));
        Add("POUROVERSTYLE", polygon.PourOverStyle.ToString(CultureInfo.InvariantCulture));
        Add("NAME", EncodeAsciiCodes(polygon.Name));
        Add("POURINDEX", polygon.PourIndex.ToString(CultureInfo.InvariantCulture));
        Add("IGNOREVIOLATIONS", B(polygon.IgnoreViolations));
        if (polygon.CopperInvalidate is { } copperInval) Add("COPPERINVALIDATE", B(copperInval));
        if (polygon.AutoGenerateName is { } autoName) Add("AUTONAME", B(autoName));
        if (polygon.OptimalVoidRotation is { } ovr) Add("OPTIMALVOIDROTATION", B(ovr));
        if (polygon.ObeyPolygonCutout is { } opc) Add("OBEYPOLYGONCUTOUT", B(opc));
        Add("NET", polygon.Net ?? string.Empty);
        if (polygon.AdditionalParameters != null)
            foreach (var kvp in polygon.AdditionalParameters) Add(kvp.Key, kvp.Value);

        writer.WriteCStringParameterBlockRaw(BuildUnicodeAwareParamString(pairs));
    }

    // Encodes a polygon NAME as the comma-separated ASCII/Unicode code-point list Altium stores.
    private static string EncodeAsciiCodes(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(((int)s[i]).ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static void WriteComponents(CompoundFileAccessor cf, PcbDocument document)
    {
        var storage = cf.RootStorage.AddStorage("Components6");
        PcbLibWriter.WriteStorageHeader(storage, document.Components.Count);

        var dataStream = storage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        foreach (var icomp in document.Components)
        {
            var comp = (PcbComponent)icomp;
            WriteComponentParameters(writer, comp);
        }

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    // Serializes a PcbComponent to its Components6 parameter block in Altium's canonical key order,
    // emitting optional keys only when present (nullable fields / non-default), so a fully typed
    // component round-trips byte-for-byte without replaying the captured block.
    private static void WriteComponentParameters(BinaryFormatWriter writer, PcbComponent c)
    {
        var pairs = new List<KeyValuePair<string, string>>();
        void Add(string k, string v) => pairs.Add(new KeyValuePair<string, string>(k, v));
        string B(bool x) => x ? "TRUE" : "FALSE";
        string I(int x) => x.ToString(CultureInfo.InvariantCulture);
        string M(Coord co) => FormatMilUnits(co.ToRaw());

        Add("SELECTION", B(c.Selection));
        Add("LAYER", LayerByteToName(c.Layer));
        Add("LOCKED", B(c.Locked));
        Add("POLYGONOUTLINE", B(c.PolygonOutline));
        Add("USERROUTED", B(c.UserRouted));
        Add("KEEPOUT", B(c.IsKeepout));
        Add("PRIMITIVELOCK", B(c.PrimitiveLock));
        Add("X", M(c.X));
        Add("Y", M(c.Y));
        Add("PATTERN", c.Name);
        if (c.Description != null) Add("DESCRIPTION", c.Description);
        Add("NAMEON", B(c.NameOn));
        Add("COMMENTON", B(c.CommentOn));
        if (c.Comment != null) Add("COMMENT", c.Comment);
        if (c.LockStrings) Add("LOCKSTRINGS", "TRUE");
        Add("GROUPNUM", I(c.GroupNum));
        Add("COUNT", I(c.Count));
        Add("ROTATION", DelphiExp(c.Rotation));
        if (c.Height.ToRaw() != 0) Add("HEIGHT", M(c.Height));
        if (c.NameAutoPosition is { } nap) Add("NAMEAUTOPOSITION", I(nap));
        if (c.CommentAutoPosition is { } cap) Add("COMMENTAUTOPOSITION", I(cap));
        Add("UNIONINDEX", I(c.UnionIndex));
        if (c.EnablePinSwapping is { } eps) Add("ENABLEPINSWAPPING", B(eps));
        if (c.EnablePartSwapping is { } eparts) Add("ENABLEPARTSWAPPING", B(eparts));
        if (!c.Enabled) Add("ENABLED", "FALSE");
        if (c.FlippedOnLayer) Add("FLIPPEDONLAYER", "TRUE");
        if (c.IsBGA) Add("ISBGA", "TRUE");
        if (c.ComponentKind is { } ck) Add("COMPONENTKIND", I(ck));
        if (c.ComponentKindVersion2 is { } ckv) Add("COMPONENTKINDVERSION2", I(ckv));
        if (c.ChannelOffset is { } cho) Add("CHANNELOFFSET", I(cho));
        if (c.SourceDesignator != null) Add("SOURCEDESIGNATOR", c.SourceDesignator);
        if (c.SourceUniqueId != null) Add("SOURCEUNIQUEID", c.SourceUniqueId);
        if (c.SourceHierarchicalPath != null) Add("SOURCEHIERARCHICALPATH", c.SourceHierarchicalPath);
        if (c.SourceFootprintLibrary != null) Add("SOURCEFOOTPRINTLIBRARY", c.SourceFootprintLibrary);
        if (c.SourceComponentLibrary != null) Add("SOURCECOMPONENTLIBRARY", c.SourceComponentLibrary);
        if (c.SourceLibReference != null) Add("SOURCELIBREFERENCE", c.SourceLibReference);
        if (c.SourceDescription != null) Add("SOURCEDESCRIPTION", c.SourceDescription);
        if (c.FootprintDescription != null) Add("FOOTPRINTDESCRIPTION", c.FootprintDescription);
        if (c.SourceCompDesignItemID != null) Add("SOURCECOMPDESIGNITEMID", c.SourceCompDesignItemID);
        if (c.SourceCompLibIdentifierKind is { } sclik) Add("SOURCECOMPLIBIDENTIFIERKIND", I(sclik));
        if (c.SourceCompLibraryIdentifier != null) Add("SOURCECOMPLIBRARYIDENTIFIER", c.SourceCompLibraryIdentifier);
        if (c.VaultGUID != null) Add("VAULTGUID", c.VaultGUID);
        if (c.ItemGUID != null) Add("ITEMGUID", c.ItemGUID);
        if (c.ItemRevisionGUID != null) Add("ITEMREVISIONGUID", c.ItemRevisionGUID);
        if (c.ModelHash != null) Add("MODELHASH", c.ModelHash);
        if (c.PackageSpecificHash != null) Add("PACKAGESPECIFICHASH", c.PackageSpecificHash);
        if (c.DefaultPCB3DModel != null) Add("DEFAULTPCB3DMODEL", c.DefaultPCB3DModel);
        if (c.UniqueId != null) Add("UNIQUEID", c.UniqueId);
        Add("JUMPERSVISIBLE", B(c.JumpersVisible));
        if (c.Area is { } area) Add("AREA", area.ToString("F6", CultureInfo.InvariantCulture));
        if (c.AdditionalParameters != null)
            foreach (var kvp in c.AdditionalParameters)
                Add(kvp.Key, kvp.Value);
        WriteUnicodeAwareParamBlock(writer, pairs);
    }

    // Emits a parameter block, applying Altium's UNICODE=EXISTS wrapping when any value contains a
    // codepoint that is not a single byte (>255): the inline values have those codepoints stripped,
    // and a UNICODE__<KEY>=<comma-decimal UTF-16 code units> companion carrying the full value is
    // appended for each affected key, bracketed by UNICODE=EXISTS markers.
    private static void WriteUnicodeAwareParamBlock(BinaryFormatWriter writer, List<KeyValuePair<string, string>> pairs)
        => writer.WriteCStringParameterBlockRaw(BuildUnicodeAwareParamString(pairs));

    // Builds the "|k=v|..." parameter string for a record, applying the UNICODE=EXISTS wrapping when
    // any value has non-ASCII content. Used both by WriteCStringParameterBlockRaw consumers and by the
    // Rules6 framing (which prefixes its own leader+length).
    private static string BuildUnicodeAwareParamString(List<KeyValuePair<string, string>> pairs)
    {
        static bool NeedsUnicode(string v)
        {
            foreach (var ch in v) if (ch > 127) return true;
            return false;
        }
        var hasUnicode = false;
        foreach (var kv in pairs) if (NeedsUnicode(kv.Value)) { hasUnicode = true; break; }

        var sb = new System.Text.StringBuilder();
        if (hasUnicode) sb.Append("|UNICODE=EXISTS");
        // Inline values are written as-is (the raw writer encodes them via Windows-1252); the
        // UNICODE__ companions below carry the full code units for any non-single-byte content.
        foreach (var (k, v) in pairs)
            sb.Append('|').Append(k).Append('=').Append(v);
        if (hasUnicode)
        {
            foreach (var (k, v) in pairs)
            {
                if (!NeedsUnicode(v)) continue;
                sb.Append("|UNICODE__").Append(k).Append('=');
                for (var i = 0; i < v.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(((int)v[i]).ToString(CultureInfo.InvariantCulture));
                }
            }
            sb.Append("|UNICODE=EXISTS");
        }
        return sb.ToString();
    }

    private static void WriteEmbeddedBoards(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.EmbeddedBoards.Count == 0)
        {
            WriteEmptyStorageIfPresent(cf, document, "EmbeddedBoards6");
            return;
        }

        var storage = cf.RootStorage.AddStorage("EmbeddedBoards6");
        PcbLibWriter.WriteStorageHeader(storage, document.EmbeddedBoards.Count);

        var dataStream = storage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        foreach (var board in document.EmbeddedBoards)
            writer.WriteCStringParameterBlockRaw(BuildEmbeddedBoardParamText(board));

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }

    // Serializes a PcbEmbeddedBoard to its EmbeddedBoards6 parameter block in Altium's canonical order.
    private static string BuildEmbeddedBoardParamText(PcbEmbeddedBoard b)
    {
        var sb = new System.Text.StringBuilder();
        void Add(string k, string v) => sb.Append('|').Append(k).Append('=').Append(v);
        static string B(bool x) => x ? "TRUE" : "FALSE";
        static string I(int x) => x.ToString(CultureInfo.InvariantCulture);
        static string M(Coord c) => FormatMilUnits(c.ToRaw());

        Add("SELECTION", B(b.Selection));
        Add("LAYER", b.Layer);
        Add("LOCKED", B(b.Locked));
        Add("POLYGONOUTLINE", B(b.PolygonOutline));
        Add("USERROUTED", B(b.UserRouted));
        Add("KEEPOUT", B(b.IsKeepout));
        Add("UNIONINDEX", I(b.UnionIndex));
        Add("X1", M(b.X1Location));
        Add("Y1", M(b.Y1Location));
        Add("X2", M(b.X2Location));
        Add("Y2", M(b.Y2Location));
        Add("ROTATION", DelphiExp(b.Rotation));
        Add("ISVIEWPORT", B(b.IsViewport));
        Add("VIEWPORTX1", M(b.ViewportX1));
        Add("VIEWPORTY1", M(b.ViewportY1));
        Add("VIEWPORTX2", M(b.ViewportX2));
        Add("VIEWPORTY2", M(b.ViewportY2));
        Add("VIEWPORTSCALE", b.ViewportScale.ToString("0.000", CultureInfo.InvariantCulture));
        Add("VIEWPORTVISIBLE", B(b.ViewportVisible));
        Add("VIEWPORTTITLE", b.ViewportTitle);
        Add("FONTNAME", b.TitleFontName);
        Add("FONTSIZE", I(b.TitleFontSize));
        Add("FONTCOLOR", I(b.TitleFontColor));
        Add("VISIBLELAYERS", b.VisibleLayers);
        Add("DOCUMENTPATH", b.DocumentPath);
        Add("X", M(b.X));
        Add("Y", M(b.Y));
        Add("ROWSPACING", M(b.RowSpacing));
        Add("COLSPACING", M(b.ColSpacing));
        Add("ROWCOUNT", I(b.RowCount));
        Add("COLCOUNT", I(b.ColCount));
        Add("MIRROR", B(b.MirrorFlag));
        Add("ORIGINMODE", I(b.OriginMode));
        return sb.ToString();
    }

    // Delphi FloatToStrF exponential form: 15 sig digits, 4-digit signed exponent, leading space for
    // non-negative values (the sign column), e.g. 0 -> " 0.00000000000000E+0000", 90 -> " 9.0...E+0001".
    internal static string DelphiExp(double v)
    {
        var mantissa = Math.Abs(v).ToString("0.00000000000000E+0000", CultureInfo.InvariantCulture);
        return (v < 0 ? "-" : " ") + mantissa;
    }

    internal static string LayerByteToName(int layer)
    {
        return layer switch
        {
            1 => "TOP",
            32 => "BOTTOM",
            33 => "TOPOVERLAY",
            34 => "BOTTOMOVERLAY",
            35 => "TOPPASTE",
            36 => "BOTTOMPASTE",
            37 => "TOPSOLDER",
            38 => "BOTTOMSOLDER",
            55 => "DRILLGUIDE",
            56 => "KEEPOUT",
            73 => "DRILLDRAWING",
            74 => "MULTILAYER",
            _ when layer >= 2 && layer <= 31 => $"MID{layer - 1}",
            _ when layer >= 39 && layer <= 54 => $"INTERNALPLANE{layer - 38}",
            _ when layer >= 57 && layer <= 72 => $"MECHANICAL{layer - 56}",
            _ => layer.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static void WriteWideStrings(CompoundFileAccessor cf, PcbDocument document)
    {
        // Collect all text strings that need wide encoding
        var hasWideStrings = false;
        foreach (var text in document.Texts)
        {
            if (text.Text.Any(c => c > 127))
            {
                hasWideStrings = true;
                break;
            }
        }

        _ = hasWideStrings;
        // Altium always writes WideStrings6 when the document has text.
        if (document.Texts.Count == 0 && !document.PresentStorages.Contains("WideStrings6"))
            return;

        var storage = cf.RootStorage.AddStorage("WideStrings6");
        PcbLibWriter.WriteStorageHeader(storage, document.Texts.Count);

        var dataStream = storage.AddStream("Data");
        using var ms = new MemoryStream();

        // Each text is stored as [u32 text_index][u32 byte_len][UTF-16LE string incl. null].
        var textIndex = 0;
        foreach (var text in document.Texts)
        {
            var utf16 = System.Text.Encoding.Unicode.GetBytes((text.Text ?? string.Empty) + "\0");
            ms.Write(BitConverter.GetBytes(textIndex), 0, 4);
            ms.Write(BitConverter.GetBytes(utf16.Length), 0, 4);
            ms.Write(utf16, 0, utf16.Length);
            textIndex++;
        }

        dataStream.SetData(ms.ToArray());
    }

    private static void WriteAdditionalStreams(CompoundFileAccessor cf, PcbDocument document)
    {
        if (document.AdditionalStreams == null || document.AdditionalStreams.Count == 0)
            return;

        // Group entries by storage name
        var storageGroups = new Dictionary<string, List<(string StreamName, byte[] Data)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in document.AdditionalStreams)
        {
            var slashIndex = kvp.Key.IndexOf('/');
            if (slashIndex > 0)
            {
                var storageName = kvp.Key[..slashIndex];
                var streamName = kvp.Key[(slashIndex + 1)..];
                if (!storageGroups.TryGetValue(storageName, out var list))
                {
                    list = new List<(string, byte[])>();
                    storageGroups[storageName] = list;
                }
                list.Add((streamName, kvp.Value));
            }
            else
            {
                // Root-level stream
                var stream = cf.RootStorage.AddStream(kvp.Key);
                stream.SetData(kvp.Value);
            }
        }

        // Create each storage and its streams
        foreach (var group in storageGroups)
        {
            var storage = cf.RootStorage.AddStorage(group.Key);
            foreach (var (streamName, data) in group.Value)
            {
                var stream = storage.AddStream(streamName);
                stream.SetData(data);
            }
        }
    }

    private static void WritePrimitiveStorage<T>(
        CompoundFileAccessor cf,
        string storageName,
        IReadOnlyList<T> primitives,
        Action<BinaryFormatWriter, T> writePrimitive)
    {
        var storage = cf.RootStorage.AddStorage(storageName);
        PcbLibWriter.WriteStorageHeader(storage, primitives.Count);

        var dataStream = storage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        foreach (var primitive in primitives)
        {
            writePrimitive(writer, primitive);
        }

        writer.Flush();
        dataStream.SetData(ms.ToArray());
    }
}
