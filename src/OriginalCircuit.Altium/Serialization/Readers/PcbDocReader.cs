using OpenMcdf;
using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Pcb;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using OriginalCircuit.Altium.Serialization.Compound;
using OriginalCircuit.Altium.Serialization.Writers;

namespace OriginalCircuit.Altium.Serialization.Readers;

/// <summary>
/// Reads PCB document (.PcbDoc) files.
/// PcbDoc files store primitives in separate storages by type
/// (e.g., [Arcs6], [Pads6], [Tracks6], etc.), each with Header and Data streams.
/// </summary>
public sealed class PcbDocReader
{
    private List<AltiumDiagnostic> _diagnostics = new();
    /// <summary>
    /// Reads a PcbDoc file from the specified path.
    /// </summary>
    /// <param name="path">Path to the .PcbDoc file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed PCB document.</returns>
    /// <exception cref="AltiumCorruptFileException">Thrown when the file cannot be parsed.</exception>
    /// <remarks>This method is not thread-safe. Create a new reader instance per thread.</remarks>
    public async ValueTask<PcbDocument> ReadAsync(string path, CancellationToken cancellationToken = default)
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
            throw new AltiumCorruptFileException($"Failed to read PcbDoc file: {ex.Message}", filePath: path, innerException: ex);
        }
    }

    /// <summary>
    /// Reads a PcbDoc file from a stream.
    /// </summary>
    /// <param name="stream">A readable stream containing compound file data. The stream is not closed.</param>
    /// <returns>The parsed PCB document.</returns>
    /// <exception cref="AltiumCorruptFileException">Thrown when the stream cannot be parsed.</exception>
    /// <remarks>This method is not thread-safe. Create a new reader instance per thread.</remarks>
    public PcbDocument Read(Stream stream)
    {
        try
        {
            using var accessor = CompoundFileAccessor.Open(stream, leaveOpen: true);
            return Read(accessor);
        }
        catch (Exception ex) when (ex is not AltiumFileException and not OutOfMemoryException)
        {
            throw new AltiumCorruptFileException($"Failed to read PcbDoc file: {ex.Message}", innerException: ex);
        }
    }

    private static readonly HashSet<string> KnownStorageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "FileHeader", "Board6", "Nets6", "Arcs6", "Pads6", "Vias6", "Tracks6",
        "Texts6", "Fills6", "Regions6", "ComponentBodies6", "Polygons6",
        "Components6", "WideStrings6", "EmbeddedBoards6",
        "Rules6", "Classes6", "DifferentialPairs6", "Rooms6"
    };

    private PcbDocument Read(CompoundFileAccessor accessor, CancellationToken cancellationToken = default)
    {
        _diagnostics = new List<AltiumDiagnostic>();
        var document = new PcbDocument();

        // Read wide strings (used by text primitives)
        var wideStrings = ReadWideStrings(accessor);

        // Read board-level parameters
        ReadBoard(accessor, document);
        ReadNets(accessor, document);

        cancellationToken.ThrowIfCancellationRequested();

        // Read each primitive type from its dedicated storage
        ReadArcs(accessor, document, cancellationToken);
        ReadPads(accessor, document, cancellationToken);
        ReadVias(accessor, document, cancellationToken);
        ReadTracks(accessor, document, cancellationToken);
        ReadTexts(accessor, document, wideStrings, cancellationToken);
        ReadFills(accessor, document, cancellationToken);
        ReadRegions(accessor, document, cancellationToken);
        ReadComponentBodies(accessor, document, cancellationToken);
        ReadPolygons(accessor, document, cancellationToken);
        ReadComponents(accessor, document, cancellationToken);
        ReadEmbeddedBoards(accessor, document, cancellationToken);

        // Assign document-level pads to their parent components using the binary component index
        AssignPadsToComponents(document);

        // Read parameter-block storages
        ReadRules(accessor, document);
        ReadClasses(accessor, document);
        ReadDifferentialPairs(accessor, document);
        ReadRooms(accessor, document);

        // Capture unknown storages for round-trip fidelity
        ReadAdditionalStreams(accessor, document);

        document.Diagnostics = _diagnostics;
        return document;
    }

    private void ReadAdditionalStreams(CompoundFileAccessor accessor, PcbDocument document)
    {
        var additional = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in accessor.EnumerateChildren(accessor.RootStorage))
        {
            if (KnownStorageNames.Contains(item.Name))
                continue;

            if (item is OpenMcdf.CFStorage childStorage)
            {
                foreach (var stream in accessor.EnumerateStreams(childStorage))
                {
                    try
                    {
                        additional[$"{item.Name}/{stream.Name}"] = stream.GetData();
                    }
                    catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or IOException)
                    {
                        _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, $"Failed to read stream '{item.Name}/{stream.Name}': {ex.Message}"));
                    }
                }
            }
            else if (item is OpenMcdf.CFStream rootStream)
            {
                try
                {
                    additional[item.Name] = rootStream.GetData();
                }
                catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or IOException)
                {
                    _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning, $"Failed to read stream '{item.Name}': {ex.Message}"));
                }
            }
        }

        if (additional.Count > 0)
            document.AdditionalStreams = additional;
    }

    private static List<string> ReadWideStrings(CompoundFileAccessor accessor)
    {
        var result = new List<string>();
        var storage = accessor.TryGetStorage("WideStrings6");
        if (storage == null)
            return result;

        var dataStream = PcbLibReader.GetChildStream(storage, "Data");
        if (dataStream == null)
            return result;

        var data = dataStream.GetData();
        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        var parameters = PcbLibReader.ReadParameterBlock(reader);

        for (var i = 0; i < parameters.Count; i++)
        {
            var key = $"ENCODEDTEXT{i}";
            if (!parameters.TryGetValue(key, out var encodedText))
                break;

            var text = PcbLibReader.DecodeWideString(encodedText);
            result.Add(text);
        }

        return result;
    }

    private static void ReadBoard(CompoundFileAccessor accessor, PcbDocument document)
    {
        var storage = accessor.TryGetStorage("Board6");
        if (storage == null)
            return;

        var dataStream = PcbLibReader.GetChildStream(storage, "Data");
        if (dataStream == null)
            return;

        var data = dataStream.GetData();
        if (data.Length == 0)
            return;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        var parameters = PcbLibReader.ReadParameterBlock(reader);
        if (parameters.Count > 0)
            document.BoardParameters = parameters;
    }

    private static void ReadNets(CompoundFileAccessor accessor, PcbDocument document)
    {
        var storage = accessor.TryGetStorage("Nets6");
        if (storage == null)
            return;

        var dataStream = PcbLibReader.GetChildStream(storage, "Data");
        if (dataStream == null)
            return;

        var data = dataStream.GetData();
        if (data.Length == 0)
            return;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        while (reader.HasMore)
        {
            var parameters = PcbLibReader.ReadParameterBlock(reader);
            if (parameters.Count == 0)
                continue;

            var net = new PcbNet();
            if (parameters.TryGetValue("NAME", out var name))
                net.Name = name;

            document.AddNet(net);
        }
    }

    private void ReadParameterBlockStorage(CompoundFileAccessor accessor, string storageName, Action<Dictionary<string, string>> addItem)
    {
        var storage = accessor.TryGetStorage(storageName);
        if (storage == null)
            return;

        var dataStream = PcbLibReader.GetChildStream(storage, "Data");
        if (dataStream == null)
            return;

        var data = dataStream.GetData();
        if (data.Length == 0)
            return;

        try
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryFormatReader(ms, leaveOpen: true);

            while (reader.HasMore)
            {
                var parameters = PcbLibReader.ReadParameterBlock(reader);
                if (parameters.Count == 0)
                    continue;

                addItem(parameters);
            }
        }
        catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or FormatException or OverflowException)
        {
            _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning,
                $"Failed to fully parse {storageName}: {ex.Message}", storageName));
        }
    }

    private void ReadRules(CompoundFileAccessor accessor, PcbDocument document)
    {
        ReadParameterBlockStorage(accessor, "Rules6", parameters =>
        {
            var rule = new PcbRule { Parameters = parameters };
            if (parameters.TryGetValue("NAME", out var name))
                rule.Name = name;
            if (parameters.TryGetValue("RULEKIND", out var ruleKind))
                rule.RuleKind = ruleKind;
            if (parameters.TryGetValue("COMMENT", out var comment))
                rule.Comment = comment;
            if (parameters.TryGetValue("UNIQUEID", out var uniqueId))
                rule.UniqueId = uniqueId;
            if (parameters.TryGetValue("ENABLED", out var enabled))
                rule.Enabled = enabled.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            if (parameters.TryGetValue("PRIORITY", out var priority) && int.TryParse(priority, out var p))
                rule.Priority = p;
            if (parameters.TryGetValue("SCOPE1EXPRESSION", out var scope1))
                rule.Scope1Expression = scope1;
            if (parameters.TryGetValue("SCOPE2EXPRESSION", out var scope2))
                rule.Scope2Expression = scope2;

            document.AddRule(rule);
        });
    }

    private void ReadClasses(CompoundFileAccessor accessor, PcbDocument document)
    {
        ReadParameterBlockStorage(accessor, "Classes6", parameters =>
        {
            var objectClass = new PcbObjectClass { Parameters = parameters };
            if (parameters.TryGetValue("NAME", out var name))
                objectClass.Name = name;
            if (parameters.TryGetValue("SUPERCLASS", out var superClass))
                objectClass.SuperClass = superClass;
            if (parameters.TryGetValue("SUBCLASS", out var subClass))
                objectClass.SubClass = subClass;
            if (parameters.TryGetValue("UNIQUEID", out var uniqueId))
                objectClass.UniqueId = uniqueId;
            if (parameters.TryGetValue("KIND", out var kind))
                objectClass.Kind = kind;
            if (parameters.TryGetValue("ENABLED", out var enabled))
                objectClass.Enabled = enabled.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            // Extract indexed members (MEMBER0, MEMBER1, ...)
            for (var i = 0; ; i++)
            {
                if (!parameters.TryGetValue($"MEMBER{i}", out var member))
                    break;
                objectClass.Members.Add(member);
            }

            document.AddClass(objectClass);
        });
    }

    private void ReadDifferentialPairs(CompoundFileAccessor accessor, PcbDocument document)
    {
        ReadParameterBlockStorage(accessor, "DifferentialPairs6", parameters =>
        {
            var pair = new PcbDifferentialPair { Parameters = parameters };
            if (parameters.TryGetValue("NAME", out var name))
                pair.Name = name;
            if (parameters.TryGetValue("POSITIVENETNAME", out var posNet))
                pair.PositiveNetName = posNet;
            if (parameters.TryGetValue("NEGATIVENETNAME", out var negNet))
                pair.NegativeNetName = negNet;
            if (parameters.TryGetValue("UNIQUEID", out var uniqueId))
                pair.UniqueId = uniqueId;
            if (parameters.TryGetValue("ENABLED", out var enabled))
                pair.Enabled = enabled.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

            document.AddDifferentialPair(pair);
        });
    }

    private void ReadRooms(CompoundFileAccessor accessor, PcbDocument document)
    {
        ReadParameterBlockStorage(accessor, "Rooms6", parameters =>
        {
            var room = new PcbRoom { Parameters = parameters };
            if (parameters.TryGetValue("NAME", out var name))
                room.Name = name;
            if (parameters.TryGetValue("UNIQUEID", out var uniqueId))
                room.UniqueId = uniqueId;

            document.AddRoom(room);
        });
    }

    private static void ReadArcs(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        ReadPrimitiveStorage(accessor, "Arcs6", reader =>
        {
            var arc = PcbLibReader.ReadArc(reader);
            if (arc != null)
                document.AddArc(arc);
        }, cancellationToken);
    }

    private static void ReadPads(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        ReadPrimitiveStorage(accessor, "Pads6", reader =>
        {
            var pad = PcbLibReader.ReadPad(reader);
            if (pad != null)
                document.AddPad(pad);
        }, cancellationToken);
    }

    /// <summary>
    /// Assigns document-level pads to their parent component using the component index
    /// stored in the binary pad record. This populates each component's Pads collection
    /// so consumers can access pads through the component rather than searching by proximity.
    /// </summary>
    private static void AssignPadsToComponents(PcbDocument document)
    {
        var components = document.Components.OfType<PcbComponent>().ToList();
        if (components.Count == 0)
            return;

        foreach (var pad in document.Pads.OfType<PcbPad>())
        {
            var idx = pad.ComponentIndex;
            if (idx >= 0 && idx < components.Count)
            {
                components[idx].AddPad(pad);
            }
        }
    }

    private static void ReadVias(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        ReadPrimitiveStorage(accessor, "Vias6", reader =>
        {
            var via = PcbLibReader.ReadVia(reader);
            if (via != null)
                document.AddVia(via);
        }, cancellationToken);
    }

    private static void ReadTracks(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        ReadPrimitiveStorage(accessor, "Tracks6", reader =>
        {
            var track = PcbLibReader.ReadTrack(reader);
            if (track != null)
                document.AddTrack(track);
        }, cancellationToken);
    }

    private static void ReadTexts(CompoundFileAccessor accessor, PcbDocument document, List<string> wideStrings, CancellationToken cancellationToken)
    {
        ReadPrimitiveStorage(accessor, "Texts6", reader =>
        {
            var text = PcbLibReader.ReadText(reader, wideStrings);
            if (text != null)
                document.AddText(text);
        }, cancellationToken);
    }

    private static void ReadFills(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        ReadPrimitiveStorage(accessor, "Fills6", reader =>
        {
            var fill = PcbLibReader.ReadFill(reader);
            if (fill != null)
                document.AddFill(fill);
        }, cancellationToken);
    }

    private static void ReadRegions(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        ReadPrimitiveStorage(accessor, "Regions6", reader =>
        {
            var region = PcbLibReader.ReadRegion(reader);
            if (region != null)
                document.AddRegion(region);
        }, cancellationToken);
    }

    private static void ReadComponentBodies(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        ReadPrimitiveStorage(accessor, "ComponentBodies6", reader =>
        {
            var body = PcbLibReader.ReadComponentBody(reader);
            if (body != null)
                document.AddComponentBody(body);
        }, cancellationToken);
    }

    private static void ReadPolygons(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        var storage = accessor.TryGetStorage("Polygons6");
        if (storage == null)
            return;

        var dataStream = PcbLibReader.GetChildStream(storage, "Data");
        if (dataStream == null)
            return;

        var data = dataStream.GetData();
        if (data.Length == 0)
            return;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        while (reader.HasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = PcbLibReader.ReadParameterBlock(reader);
            if (parameters.Count == 0)
                continue;

            var polygon = new PcbPolygon();
            ApplyPolygonParameters(polygon, parameters);
            document.AddPolygon(polygon);
        }
    }

    /// <summary>
    /// Parses a layer value that can be either a numeric ID (e.g. "1") or a name (e.g. "TOP").
    /// </summary>
    private static int ParseLayerValue(string layerStr)
    {
        if (int.TryParse(layerStr, out var numeric))
            return numeric;

        // Fall back to layer name lookup
        return PcbLibWriter.LayerNameToByte(layerStr);
    }

    private static bool TryGetBool(Dictionary<string, string> parameters, string key, out bool value)
    {
        value = false;
        if (!parameters.TryGetValue(key, out var str))
            return false;
        value = string.Equals(str, "TRUE", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static bool TryGetBoolAny(Dictionary<string, string> parameters, out bool value, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (TryGetBool(parameters, key, out value))
                return true;
        }
        value = false;
        return false;
    }

    private static bool TryGetIntAny(Dictionary<string, string> parameters, out int value, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (parameters.TryGetValue(key, out var str) && int.TryParse(str, out value))
                return true;
        }
        value = 0;
        return false;
    }

    private static bool TryGetCoordAny(Dictionary<string, string> parameters, out Coord value, params string[] keys)
    {
        if (TryGetIntAny(parameters, out var raw, keys))
        {
            value = Coord.FromRaw(raw);
            return true;
        }
        value = default;
        return false;
    }

    private static void ApplyPolygonParameters(PcbPolygon polygon, Dictionary<string, string> parameters)
    {
        // Track known keys for AdditionalParameters capture
        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Track(params string[] keys) { foreach (var k in keys) knownKeys.Add(k); }

        // Basic identity
        Track("LAYER", "NET", "NAME", "UNIQUEID", "POLYGONTYPE");
        if (parameters.TryGetValue("LAYER", out var layerStr))
            polygon.Layer = ParseLayerValue(layerStr);
        if (parameters.TryGetValue("NET", out var net))
            polygon.Net = net;
        if (parameters.TryGetValue("NAME", out var name))
            polygon.Name = name;
        if (parameters.TryGetValue("UNIQUEID", out var uid))
            polygon.UniqueId = uid;
        if (parameters.TryGetValue("POLYGONTYPE", out var pt) && int.TryParse(pt, out var polygonType))
            polygon.PolygonType = polygonType;

        // Hatch/pour settings - read both old and DTO keys
        Track("HATCHSTYLE", "POLYHATCHSTYLE", "POURMODE", "POUROVER");
        if (TryGetIntAny(parameters, out var hatchStyle, "HATCHSTYLE", "POLYHATCHSTYLE"))
            polygon.PolyHatchStyle = hatchStyle;
        if (TryGetIntAny(parameters, out var pourOver, "POURMODE", "POUROVER"))
            polygon.PourOver = pourOver;

        // Boolean flags - read both old and DTO keys
        Track("REMOVEISLANDSBYAREA", "ISLANDAREATHRESHOLD", "REMOVEDEAD",
              "REMOVENARROWNECKS", "REMOVENECKS", "USEOCTAGONS",
              "AVOIDOBST", "AVOIDOBSTICLES");
        if (TryGetBool(parameters, "REMOVEISLANDSBYAREA", out var ria))
            polygon.RemoveIslandsByArea = ria;
        if (parameters.TryGetValue("ISLANDAREATHRESHOLD", out var iat) && int.TryParse(iat, out var islandAreaThreshold))
            polygon.IslandAreaThreshold = islandAreaThreshold;
        if (TryGetBool(parameters, "REMOVEDEAD", out var rd))
            polygon.RemoveDead = rd;
        if (TryGetBoolAny(parameters, out var rnn, "REMOVENECKS", "REMOVENARROWNECKS"))
            polygon.RemoveNarrowNecks = rnn;
        if (TryGetBool(parameters, "USEOCTAGONS", out var uo))
            polygon.UseOctagons = uo;
        if (TryGetBoolAny(parameters, out var ao, "AVOIDOBST", "AVOIDOBSTICLES"))
        {
            polygon.AvoidObstacles = ao;
            polygon.AvoidObsticles = ao;
        }

        // Coord properties
        Track("GRIDSIZE", "TRACKWIDTH", "MINPRIMLENGTH", "NECKWIDTH", "ARCAPPROXIMATION",
              "BORDERWIDTH", "SOLDERMASKEXPANSION", "PASTEMASKEXPANSION",
              "RELIEFAIRGAP", "RELIEFCONDUCTORWIDTH", "POWERPLANECLEARANCE",
              "POWERPLANERELIEFEXPANSION");
        if (TryGetCoordAny(parameters, out var grid, "GRIDSIZE"))
            polygon.Grid = grid;
        if (TryGetCoordAny(parameters, out var trackWidth, "TRACKWIDTH"))
            polygon.TrackSize = trackWidth;
        if (TryGetCoordAny(parameters, out var minTrack, "MINPRIMLENGTH"))
            polygon.MinTrack = minTrack;
        if (TryGetCoordAny(parameters, out var neckWidth, "NECKWIDTH"))
            polygon.NeckWidthThreshold = neckWidth;
        if (TryGetCoordAny(parameters, out var arcApprox, "ARCAPPROXIMATION"))
            polygon.ArcApproximation = arcApprox;
        if (TryGetCoordAny(parameters, out var borderWidth, "BORDERWIDTH"))
            polygon.BorderWidth = borderWidth;
        if (TryGetCoordAny(parameters, out var smExp, "SOLDERMASKEXPANSION"))
            polygon.SolderMaskExpansion = smExp;
        if (TryGetCoordAny(parameters, out var pmExp, "PASTEMASKEXPANSION"))
            polygon.PasteMaskExpansion = pmExp;
        if (TryGetCoordAny(parameters, out var reliefGap, "RELIEFAIRGAP"))
            polygon.ReliefAirGap = reliefGap;
        if (TryGetCoordAny(parameters, out var reliefWidth, "RELIEFCONDUCTORWIDTH"))
            polygon.ReliefConductorWidth = reliefWidth;
        if (TryGetCoordAny(parameters, out var ppClear, "POWERPLANECLEARANCE"))
            polygon.PowerPlaneClearance = ppClear;
        if (TryGetCoordAny(parameters, out var ppRelief, "POWERPLANERELIEFEXPANSION"))
            polygon.PowerPlaneReliefExpansion = ppRelief;

        // Integer properties
        Track("POURORDER", "RELIEFENTRIES", "POWERPLANECONNECTSTYLE", "FLAGS");
        if (parameters.TryGetValue("POURORDER", out var pourOrderStr) && int.TryParse(pourOrderStr, out var pourOrder))
            polygon.PourIndex = pourOrder;
        if (parameters.TryGetValue("RELIEFENTRIES", out var reliefEntriesStr) && int.TryParse(reliefEntriesStr, out var reliefEntries))
            polygon.ReliefEntries = reliefEntries;
        if (parameters.TryGetValue("POWERPLANECONNECTSTYLE", out var ppcsStr) && int.TryParse(ppcsStr, out var ppcs))
            polygon.PowerPlaneConnectStyle = ppcs;

        // Long properties
        Track("REPOURAREA");
        if (parameters.TryGetValue("REPOURAREA", out var areaStr) && long.TryParse(areaStr, out var areaSize))
            polygon.AreaSize = areaSize;

        // More boolean flags
        Track("LOCKED", "PRIMITIVELOCK", "SHELVED", "POUROVERSAMENETPOLYGONS",
              "ENABLED", "KEEPOUT", "POLYGONOUTLINE", "POURED",
              "AUTOGENERATENAME", "CLIPACUTECORNERS", "DRAWDEADCOPPER",
              "DRAWREMOVEDISLANDS", "DRAWREMOVEDNECKS", "EXPANDOUTLINE",
              "IGNOREVIOLATIONS", "MITRECORNERS", "OBEYPOLYGONCUTOUT",
              "OPTIMALVOIDROTATION", "ALLOWGLOBALEDIT", "MOVEABLE", "ARCPOURMODE");
        if (TryGetBoolAny(parameters, out var primLock, "PRIMITIVELOCK", "LOCKED"))
            polygon.PrimitiveLock = primLock;
        if (TryGetBool(parameters, "SHELVED", out var shelved))
            polygon.IsHidden = shelved;
        if (TryGetBool(parameters, "POUROVERSAMENETPOLYGONS", out var posnp))
            polygon.PourOverSameNetPolygons = posnp;
        if (TryGetBool(parameters, "ENABLED", out var enabled))
            polygon.Enabled = enabled;
        if (TryGetBool(parameters, "KEEPOUT", out var keepout))
            polygon.IsKeepout = keepout;
        if (TryGetBool(parameters, "POLYGONOUTLINE", out var pgOutline))
            polygon.PolygonOutline = pgOutline;
        if (TryGetBool(parameters, "POURED", out var poured))
            polygon.Poured = poured;
        if (TryGetBool(parameters, "AUTOGENERATENAME", out var autoName))
            polygon.AutoGenerateName = autoName;
        if (TryGetBool(parameters, "CLIPACUTECORNERS", out var clipCorners))
            polygon.ClipAcuteCorners = clipCorners;
        if (TryGetBool(parameters, "DRAWDEADCOPPER", out var drawDead))
            polygon.DrawDeadCopper = drawDead;
        if (TryGetBool(parameters, "DRAWREMOVEDISLANDS", out var drawIslands))
            polygon.DrawRemovedIslands = drawIslands;
        if (TryGetBool(parameters, "DRAWREMOVEDNECKS", out var drawNecks))
            polygon.DrawRemovedNecks = drawNecks;
        if (TryGetBool(parameters, "EXPANDOUTLINE", out var expandOutline))
            polygon.ExpandOutline = expandOutline;
        if (TryGetBool(parameters, "IGNOREVIOLATIONS", out var ignoreViol))
            polygon.IgnoreViolations = ignoreViol;
        if (TryGetBool(parameters, "MITRECORNERS", out var mitre))
            polygon.MitreCorners = mitre;
        if (TryGetBool(parameters, "OBEYPOLYGONCUTOUT", out var obeyCutout))
            polygon.ObeyPolygonCutout = obeyCutout;
        if (TryGetBool(parameters, "OPTIMALVOIDROTATION", out var optVoid))
            polygon.OptimalVoidRotation = optVoid;
        if (TryGetBool(parameters, "ALLOWGLOBALEDIT", out var allowGlobal))
            polygon.AllowGlobalEdit = allowGlobal;
        if (TryGetBool(parameters, "MOVEABLE", out var moveable))
            polygon.Moveable = moveable;
        if (TryGetBool(parameters, "ARCPOURMODE", out var arcPour))
            polygon.ArcPourMode = arcPour;

        // Read vertices
        Track("POINTCOUNT");
        if (parameters.TryGetValue("POINTCOUNT", out var pcStr) && int.TryParse(pcStr, out var pointCount))
        {
            for (var i = 0; i < pointCount; i++)
            {
                var prefix = $"SA{i}";
                Track($"{prefix}.X", $"{prefix}.Y");
                Coord x = default, y = default;
                if (parameters.TryGetValue($"{prefix}.X", out var xStr) && int.TryParse(xStr, out var xVal))
                    x = Coord.FromRaw(xVal);
                if (parameters.TryGetValue($"{prefix}.Y", out var yStr) && int.TryParse(yStr, out var yVal))
                    y = Coord.FromRaw(yVal);
                polygon.AddVertex(new CoordPoint(x, y));
            }
        }

        // Capture unknown parameters for round-trip fidelity
        var additional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in parameters)
        {
            if (!knownKeys.Contains(kvp.Key))
                additional[kvp.Key] = kvp.Value;
        }
        if (additional.Count > 0)
            polygon.AdditionalParameters = additional;
    }

    private static void ReadEmbeddedBoards(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        var storage = accessor.TryGetStorage("EmbeddedBoards6");
        if (storage == null)
            return;

        var dataStream = PcbLibReader.GetChildStream(storage, "Data");
        if (dataStream == null)
            return;

        var data = dataStream.GetData();
        if (data.Length == 0)
            return;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        while (reader.HasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = PcbLibReader.ReadParameterBlock(reader);
            if (parameters.Count == 0)
                continue;

            var board = new PcbEmbeddedBoard();
            ApplyEmbeddedBoardParameters(board, parameters);
            document.AddEmbeddedBoard(board);
        }
    }

    private static void ApplyEmbeddedBoardParameters(PcbEmbeddedBoard board, Dictionary<string, string> parameters)
    {
        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Track(params string[] keys) { foreach (var k in keys) knownKeys.Add(k); }

        // String properties
        Track("DOCUMENTPATH", "VIEWPORTTITLE", "FONTNAME", "VISIBLELAYERS");
        if (parameters.TryGetValue("DOCUMENTPATH", out var docPath))
            board.DocumentPath = docPath;
        if (parameters.TryGetValue("VIEWPORTTITLE", out var vpTitle))
            board.ViewportTitle = vpTitle;
        if (parameters.TryGetValue("FONTNAME", out var fontName))
            board.TitleFontName = fontName;

        // Layer (can be name like "TOP" or number like "1")
        Track("LAYER");
        if (parameters.TryGetValue("LAYER", out var layerStr))
            board.Layer = ParseLayerValue(layerStr);

        // Double properties
        Track("ROTATION", "VIEWPORTSCALE");
        if (parameters.TryGetValue("ROTATION", out var rotStr))
        {
            var trimmed = rotStr.Trim();
            if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var rotation))
                board.Rotation = rotation;
        }
        if (parameters.TryGetValue("VIEWPORTSCALE", out var scaleStr))
        {
            if (double.TryParse(scaleStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var sc))
                board.Scale = sc;
        }

        // Boolean properties (actual keys from real Altium files)
        Track("SELECTION", "LOCKED", "MIRROR", "KEEPOUT", "POLYGONOUTLINE",
              "USERROUTED", "ISVIEWPORT", "VIEWPORTVISIBLE");
        if (TryGetBool(parameters, "MIRROR", out var mirror))
            board.MirrorFlag = mirror;
        if (TryGetBool(parameters, "KEEPOUT", out var keepout))
            board.IsKeepout = keepout;
        if (TryGetBool(parameters, "POLYGONOUTLINE", out var polyOutline))
            board.PolygonOutline = polyOutline;
        if (TryGetBool(parameters, "USERROUTED", out var userRouted))
            board.UserRouted = userRouted;
        if (TryGetBool(parameters, "ISVIEWPORT", out var viewport))
            board.IsViewport = viewport;
        if (TryGetBool(parameters, "VIEWPORTVISIBLE", out var vpVis))
            board.ViewportVisible = vpVis;

        // Integer properties
        Track("ORIGINMODE", "COLCOUNT", "ROWCOUNT", "UNIONINDEX",
              "FONTSIZE", "FONTCOLOR");
        if (parameters.TryGetValue("ORIGINMODE", out var omStr) && int.TryParse(omStr, out var om))
            board.OriginMode = om;
        if (parameters.TryGetValue("COLCOUNT", out var ccStr) && int.TryParse(ccStr, out var cc))
            board.ColCount = cc;
        if (parameters.TryGetValue("ROWCOUNT", out var rcStr) && int.TryParse(rcStr, out var rc))
            board.RowCount = rc;
        if (parameters.TryGetValue("UNIONINDEX", out var uiStr) && int.TryParse(uiStr, out var ui))
            board.UnionIndex = ui;
        if (parameters.TryGetValue("FONTSIZE", out var fsStr) && int.TryParse(fsStr, out var fs))
            board.TitleFontSize = fs;
        if (parameters.TryGetValue("FONTCOLOR", out var fcStr) && int.TryParse(fcStr, out var fc))
            board.TitleFontColor = fc;

        // Coord properties (stored as "1338.5827mil" format)
        Track("X1", "Y1", "X2", "Y2", "X", "Y",
              "COLSPACING", "ROWSPACING",
              "VIEWPORTX1", "VIEWPORTY1", "VIEWPORTX2", "VIEWPORTY2");
        if (TryParseMilCoord(parameters, "X1", out var x1))
            board.X1Location = x1;
        if (TryParseMilCoord(parameters, "Y1", out var y1))
            board.Y1Location = y1;
        if (TryParseMilCoord(parameters, "X2", out var x2))
            board.X2Location = x2;
        if (TryParseMilCoord(parameters, "Y2", out var y2))
            board.Y2Location = y2;
        if (TryParseMilCoord(parameters, "COLSPACING", out var colSp))
            board.ColSpacing = colSp;
        if (TryParseMilCoord(parameters, "ROWSPACING", out var rowSp))
            board.RowSpacing = rowSp;
    }

    /// <summary>
    /// Parses a coordinate value in "NNNNmil" format (e.g., "1338.5827mil") to a Coord.
    /// Also handles raw integer values for compatibility with programmatically saved files.
    /// </summary>
    private static bool TryParseMilCoord(Dictionary<string, string> parameters, string key, out Coord value)
    {
        value = default;
        if (!parameters.TryGetValue(key, out var str))
            return false;

        var trimmed = str.AsSpan().Trim();

        // If the string has a "mil" suffix, parse as mils
        if (trimmed.EndsWith("mil", StringComparison.OrdinalIgnoreCase))
        {
            var numPart = trimmed[..^3];
            if (double.TryParse(numPart, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var mils))
            {
                value = Coord.FromMils(mils);
                return true;
            }
            return false;
        }

        // No "mil" suffix — treat as raw integer (internal coordinate units)
        if (int.TryParse(trimmed, out var raw))
        {
            value = Coord.FromRaw(raw);
            return true;
        }

        return false;
    }

    private static void ReadComponents(CompoundFileAccessor accessor, PcbDocument document, CancellationToken cancellationToken)
    {
        var storage = accessor.TryGetStorage("Components6");
        if (storage == null)
            return;

        var dataStream = PcbLibReader.GetChildStream(storage, "Data");
        if (dataStream == null)
            return;

        var data = dataStream.GetData();
        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        // Components are stored as parameter blocks (not binary primitives)
        while (reader.HasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = PcbLibReader.ReadParameterBlock(reader);
            if (parameters.Count == 0)
                continue;

            var component = new PcbComponent();
            ApplyComponentParameters(component, parameters);
            document.AddComponent(component);
        }
    }

    private static void ApplyComponentParameters(PcbComponent component, Dictionary<string, string> parameters)
    {
        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Track(params string[] keys) { foreach (var k in keys) knownKeys.Add(k); }

        // Basic identity
        Track("PATTERN", "DESCRIPTION", "HEIGHT", "COMMENT", "X", "Y", "ROTATION", "LAYER");
        if (parameters.TryGetValue("PATTERN", out var pattern))
            component.Name = pattern;
        if (parameters.TryGetValue("DESCRIPTION", out var description))
            component.Description = description;
        if (TryParseMilCoord(parameters, "HEIGHT", out var height))
            component.Height = height;
        if (parameters.TryGetValue("COMMENT", out var comment))
            component.Comment = comment;
        if (TryParseMilCoord(parameters, "X", out var x))
            component.X = x;
        if (TryParseMilCoord(parameters, "Y", out var y))
            component.Y = y;
        if (parameters.TryGetValue("ROTATION", out var rotStr) && double.TryParse(rotStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rotation))
            component.Rotation = rotation;
        if (parameters.TryGetValue("LAYER", out var layerStr))
            component.Layer = ParseLayerValue(layerStr);

        // Display
        Track("COMMENTON", "COMMENTAUTOPOSITION", "NAMEON", "NAMEAUTOPOSITION", "LOCKSTRINGS");
        if (TryGetBool(parameters, "COMMENTON", out var commentOn))
            component.CommentOn = commentOn;
        if (parameters.TryGetValue("COMMENTAUTOPOSITION", out var capStr) && int.TryParse(capStr, out var cap))
            component.CommentAutoPosition = cap;
        if (TryGetBool(parameters, "NAMEON", out var nameOn))
            component.NameOn = nameOn;
        if (parameters.TryGetValue("NAMEAUTOPOSITION", out var napStr) && int.TryParse(napStr, out var nap))
            component.NameAutoPosition = nap;
        if (TryGetBool(parameters, "LOCKSTRINGS", out var lockStrings))
            component.LockStrings = lockStrings;

        // Component state
        Track("COMPONENTKIND", "ENABLED", "FLIPPEDONLAYER", "GROUPNUM", "ISBGA", "CHANNELOFFSET");
        if (parameters.TryGetValue("COMPONENTKIND", out var ckStr) && int.TryParse(ckStr, out var ck))
            component.ComponentKind = ck;
        if (TryGetBool(parameters, "ENABLED", out var enabled))
            component.Enabled = enabled;
        if (TryGetBool(parameters, "FLIPPEDONLAYER", out var flipped))
            component.FlippedOnLayer = flipped;
        if (parameters.TryGetValue("GROUPNUM", out var gnStr) && int.TryParse(gnStr, out var gn))
            component.GroupNum = gn;
        if (TryGetBool(parameters, "ISBGA", out var isBga))
            component.IsBGA = isBga;
        if (parameters.TryGetValue("CHANNELOFFSET", out var coStr) && int.TryParse(coStr, out var co))
            component.ChannelOffset = co;

        // Source info
        Track("SOURCEDESIGNATOR", "SOURCELIBREFRENCE", "SOURCECOMPONENTLIBRARY",
              "SOURCEDESCRIPTION", "SOURCEFOOTPRINTLIBRARY", "SOURCEUNIQUEID",
              "SOURCEHIERARCHICALPATH", "SOURCECOMPDESIGNITEMID",
              "FOOTPRINTDESCRIPTION");
        if (parameters.TryGetValue("FOOTPRINTDESCRIPTION", out var fpDesc))
            component.FootprintDescription = fpDesc;
        if (parameters.TryGetValue("SOURCEDESIGNATOR", out var srcDesg))
            component.SourceDesignator = srcDesg;
        if (parameters.TryGetValue("SOURCELIBREFRENCE", out var srcLibRef))
            component.SourceLibReference = srcLibRef;
        if (parameters.TryGetValue("SOURCECOMPONENTLIBRARY", out var srcCompLib))
            component.SourceComponentLibrary = srcCompLib;
        if (parameters.TryGetValue("SOURCEDESCRIPTION", out var srcDesc))
            component.SourceDescription = srcDesc;
        if (parameters.TryGetValue("SOURCEFOOTPRINTLIBRARY", out var srcFpLib))
            component.SourceFootprintLibrary = srcFpLib;
        if (parameters.TryGetValue("SOURCEUNIQUEID", out var srcUid))
            component.SourceUniqueId = srcUid;
        if (parameters.TryGetValue("SOURCEHIERARCHICALPATH", out var srcHierPath))
            component.SourceHierarchicalPath = srcHierPath;
        if (parameters.TryGetValue("SOURCECOMPDESIGNITEMID", out var srcCompId))
            component.SourceCompDesignItemID = srcCompId;

        // Vault/GUID
        Track("ITEMGUID", "REVISIONGUID", "VAULTGUID", "UNIQUEID");
        if (parameters.TryGetValue("ITEMGUID", out var itemGuid))
            component.ItemGUID = itemGuid;
        if (parameters.TryGetValue("REVISIONGUID", out var revGuid))
            component.ItemRevisionGUID = revGuid;
        if (parameters.TryGetValue("VAULTGUID", out var vaultGuid))
            component.VaultGUID = vaultGuid;
        if (parameters.TryGetValue("UNIQUEID", out var uniqueId))
            component.UniqueId = uniqueId;

        // Hash/model
        Track("MODELHASH", "PACKAGESPECIFICHASH", "DEFAULTPCB3DMODEL");
        if (parameters.TryGetValue("MODELHASH", out var modelHash))
            component.ModelHash = modelHash;
        if (parameters.TryGetValue("PACKAGESPECIFICHASH", out var pkgHash))
            component.PackageSpecificHash = pkgHash;
        if (parameters.TryGetValue("DEFAULTPCB3DMODEL", out var def3d))
            component.DefaultPCB3DModel = def3d;

        // Capture unknown parameters for round-trip fidelity
        var additional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in parameters)
        {
            if (!knownKeys.Contains(kvp.Key))
                additional[kvp.Key] = kvp.Value;
        }
        if (additional.Count > 0)
            component.AdditionalParameters = additional;
    }

    /// <summary>
    /// <summary>
    /// Reads a primitive storage section. Each storage has a Header stream (record count)
    /// and a Data stream. Each record is prefixed with an object ID byte (same as PcbLib),
    /// followed by the type-specific binary data.
    /// </summary>
    private static void ReadPrimitiveStorage(
        CompoundFileAccessor accessor,
        string storageName,
        Action<BinaryFormatReader> readPrimitive,
        CancellationToken cancellationToken = default)
    {
        var storage = accessor.TryGetStorage(storageName);
        if (storage == null)
            return;

        // Read primitives from Data stream
        var dataStream = PcbLibReader.GetChildStream(storage, "Data");
        if (dataStream == null)
            return;

        var data = dataStream.GetData();
        if (data.Length == 0)
            return;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        while (reader.HasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // Each record is prefixed with an object ID byte
                reader.ReadByte();
                readPrimitive(reader);
            }
            catch (EndOfStreamException)
            {
                break;
            }
        }
    }
}
