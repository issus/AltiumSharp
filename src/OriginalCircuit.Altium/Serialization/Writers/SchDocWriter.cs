using OpenMcdf;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Serialization.Binary;

namespace OriginalCircuit.Altium.Serialization.Writers;

/// <summary>
/// Writes schematic document (.SchDoc) files.
/// SchDoc files store all primitives in a flat list in the FileHeader stream.
/// Components own their children via OWNERINDEX.
/// </summary>
public sealed class SchDocWriter
{
    /// <summary>
    /// Writes a SchDoc file to the specified path.
    /// </summary>
    /// <param name="document">The schematic document to write.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="overwrite">If true, overwrites an existing file; otherwise throws if the file exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public async ValueTask WriteAsync(SchDocument document, string path, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        await using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await WriteAsync(document, stream, cancellationToken);
    }

    /// <summary>
    /// Writes a SchDoc file to a stream.
    /// </summary>
    /// <param name="document">The schematic document to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public async ValueTask WriteAsync(SchDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        Write(document, ms, cancellationToken);
        ms.Position = 0;
        await ms.CopyToAsync(stream, cancellationToken);
    }

    /// <summary>
    /// Writes a SchDoc file to a stream synchronously.
    /// </summary>
    /// <param name="document">The schematic document to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public void Write(SchDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        using var cf = new CompoundFile();

        WriteFileHeader(cf, document, cancellationToken);
        WriteStorage(cf, document);

        cf.Save(stream);
    }

    private static void WriteFileHeader(CompoundFile cf, SchDocument document, CancellationToken cancellationToken = default)
    {
        var headerStream = cf.RootStorage.AddStream("FileHeader");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Write document header record (C-string format, same as SchLib/PCB)
        // Use preserved header parameters for round-trip fidelity, or defaults for new files
        Dictionary<string, string> headerParams;
        if (document.HeaderParameters != null && document.HeaderParameters.Count > 0)
        {
            headerParams = new Dictionary<string, string>(document.HeaderParameters, StringComparer.OrdinalIgnoreCase);
            // Ensure required keys are present
            headerParams.TryAdd("HEADER", "Protel for Windows - Schematic Capture Binary File Version 5.0");
            headerParams.TryAdd("WEIGHT", "0");
        }
        else
        {
            headerParams = new Dictionary<string, string>
            {
                ["HEADER"] = "Protel for Windows - Schematic Capture Binary File Version 5.0",
                ["WEIGHT"] = "0"
            };
        }
        writer.WriteCStringParameterBlock(headerParams);

        // Track pin data
        var pinsFrac = new Dictionary<int, (int x, int y, int length)>();
        var pinsSymbolLineWidth = new Dictionary<int, Dictionary<string, string>>();

        // Write all primitives in flat order: components first, then their children
        var index = 0;
        var pinIndex = 0;

        foreach (var component in document.Components.Cast<SchComponent>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var componentIndex = index;
            SchLibWriter.WriteComponentRecord(writer, component, ref index);

            // Write component's children with OWNERINDEX pointing to the component
            WritePrimitives(writer, component, componentIndex, ref index, ref pinIndex, pinsFrac, pinsSymbolLineWidth);
        }

        // Write document-level primitives (not owned by any component)
        WriteDocumentPrimitives(writer, document, ref index, ref pinIndex, pinsFrac, pinsSymbolLineWidth);

        writer.Flush();
        headerStream.SetData(ms.ToArray());
    }

    private static void WritePrimitives(BinaryFormatWriter writer, SchComponent component,
        int ownerIndex, ref int index, ref int pinIndex,
        Dictionary<int, (int x, int y, int length)> pinsFrac,
        Dictionary<int, Dictionary<string, string>> pinsSymbolLineWidth)
    {
        // In SchDoc format, write pins as parameter-based records (not binary)
        // so that OWNERINDEX is preserved for parent assignment.
        foreach (var pin in component.Pins)
        {
            WritePinAsParameterRecord(writer, (SchPin)pin, ownerIndex, ref index);
            pinIndex++;
        }

        foreach (var line in component.Lines)
            SchLibWriter.WriteLineRecord(writer, (SchLine)line, ref index, ownerIndex);

        foreach (var rect in component.Rectangles)
            SchLibWriter.WriteRectangleRecord(writer, (SchRectangle)rect, ref index, ownerIndex);

        foreach (var label in component.Labels)
            SchLibWriter.WriteLabelRecord(writer, (SchLabel)label, ref index, ownerIndex);

        foreach (var arc in component.Arcs)
            SchLibWriter.WriteArcRecord(writer, (SchArc)arc, ref index, ownerIndex);

        foreach (var polygon in component.Polygons)
            SchLibWriter.WritePolygonRecord(writer, (SchPolygon)polygon, ref index, ownerIndex);

        foreach (var polyline in component.Polylines)
            SchLibWriter.WritePolylineRecord(writer, (SchPolyline)polyline, ref index, ownerIndex);

        foreach (var bezier in component.Beziers)
            SchLibWriter.WriteBezierRecord(writer, (SchBezier)bezier, ref index, ownerIndex);

        foreach (var ellipse in component.Ellipses)
            SchLibWriter.WriteEllipseRecord(writer, (SchEllipse)ellipse, ref index, ownerIndex);

        foreach (var roundedRect in component.RoundedRectangles)
            SchLibWriter.WriteRoundedRectangleRecord(writer, (SchRoundedRectangle)roundedRect, ref index, ownerIndex);

        foreach (var pie in component.Pies)
            SchLibWriter.WritePieRecord(writer, (SchPie)pie, ref index, ownerIndex);

        foreach (var ellipticalArc in component.EllipticalArcs)
            SchLibWriter.WriteEllipticalArcRecord(writer, (SchEllipticalArc)ellipticalArc, ref index, ownerIndex);

        foreach (var param in component.Parameters)
            SchLibWriter.WriteParameterRecord(writer, (SchParameter)param, ref index, ownerIndex);

        foreach (var netLabel in component.NetLabels)
            SchLibWriter.WriteNetLabelRecord(writer, (SchNetLabel)netLabel, ref index, ownerIndex);

        foreach (var junction in component.Junctions)
            SchLibWriter.WriteJunctionRecord(writer, (SchJunction)junction, ref index, ownerIndex);

        foreach (var textFrame in component.TextFrames)
            SchLibWriter.WriteTextFrameRecord(writer, (SchTextFrame)textFrame, ref index, ownerIndex);

        foreach (var image in component.Images)
            SchLibWriter.WriteImageRecord(writer, (SchImage)image, ref index, ownerIndex);

        foreach (var symbol in component.Symbols)
            SchLibWriter.WriteSymbolRecord(writer, (SchSymbol)symbol, ref index, ownerIndex);

        foreach (var powerObj in component.PowerObjects)
            SchLibWriter.WritePowerObjectRecord(writer, (SchPowerObject)powerObj, ref index, ownerIndex);

        // Write implementation records (records 44-48) with proper OWNERINDEX chain
        WriteImplementationRecords(writer, component, ownerIndex, ref index);
    }

    private static void WriteImplementationRecords(BinaryFormatWriter writer, SchComponent component,
        int componentIndex, ref int index)
    {
        if (component.Implementations.Count == 0)
        {
            // Write empty ImplementationList container (Altium always writes record 44)
            var emptyParams = new Dictionary<string, string>
            {
                ["RECORD"] = "44",
                ["OWNERINDEX"] = componentIndex.ToString()
            };
            writer.WriteCStringParameterBlock(emptyParams);
            index++;
            return;
        }

        // Record 44: ImplementationList container, owned by component
        var implListIndex = index;
        var implListParams = new Dictionary<string, string>
        {
            ["RECORD"] = "44",
            ["OWNERINDEX"] = componentIndex.ToString()
        };
        writer.WriteCStringParameterBlock(implListParams);
        index++;

        foreach (var impl in component.Implementations.Cast<SchImplementation>())
        {
            // Record 45: Implementation, owned by ImplementationList
            var implIndex = index;
            SchLibWriter.WriteImplementationRecord(writer, impl, ref index, implListIndex);

            if (impl.MapDefiners.Count > 0)
            {
                // Record 46: MapDefinerList container, owned by Implementation
                var mdlIndex = index;
                var mdlParams = new Dictionary<string, string>
                {
                    ["RECORD"] = "46",
                    ["OWNERINDEX"] = implIndex.ToString()
                };
                writer.WriteCStringParameterBlock(mdlParams);
                index++;

                foreach (var mapDefiner in impl.MapDefiners.Cast<SchMapDefiner>())
                {
                    // Record 47: MapDefiner, owned by MapDefinerList
                    SchLibWriter.WriteMapDefinerRecord(writer, mapDefiner, ref index, mdlIndex);
                }
            }

            // Record 48: ImplementationParameters, owned by Implementation
            var ipParams = new Dictionary<string, string>
            {
                ["RECORD"] = "48",
                ["OWNERINDEX"] = implIndex.ToString()
            };
            writer.WriteCStringParameterBlock(ipParams);
            index++;
        }
    }

    private static void WriteDocumentPrimitives(BinaryFormatWriter writer, SchDocument document,
        ref int index, ref int pinIndex,
        Dictionary<int, (int x, int y, int length)> pinsFrac,
        Dictionary<int, Dictionary<string, string>> pinsSymbolLineWidth)
    {
        foreach (var wire in document.Wires)
        {
            WriteWireRecord(writer, (SchWire)wire, -1, ref index);
        }

        foreach (var label in document.Labels)
            SchLibWriter.WriteLabelRecord(writer, (SchLabel)label, ref index);

        foreach (var param in document.Parameters)
            SchLibWriter.WriteParameterRecord(writer, (SchParameter)param, ref index);

        foreach (var line in document.Lines)
            SchLibWriter.WriteLineRecord(writer, (SchLine)line, ref index);

        foreach (var rect in document.Rectangles)
            SchLibWriter.WriteRectangleRecord(writer, (SchRectangle)rect, ref index);

        foreach (var netLabel in document.NetLabels)
            SchLibWriter.WriteNetLabelRecord(writer, (SchNetLabel)netLabel, ref index);

        foreach (var junction in document.Junctions)
            SchLibWriter.WriteJunctionRecord(writer, (SchJunction)junction, ref index);

        foreach (var powerObj in document.PowerObjects)
            SchLibWriter.WritePowerObjectRecord(writer, (SchPowerObject)powerObj, ref index);

        foreach (var polygon in document.Polygons)
            SchLibWriter.WritePolygonRecord(writer, (SchPolygon)polygon, ref index);

        foreach (var polyline in document.Polylines)
            SchLibWriter.WritePolylineRecord(writer, (SchPolyline)polyline, ref index);

        foreach (var arc in document.Arcs)
            SchLibWriter.WriteArcRecord(writer, (SchArc)arc, ref index);

        foreach (var bezier in document.Beziers)
            SchLibWriter.WriteBezierRecord(writer, (SchBezier)bezier, ref index);

        foreach (var ellipse in document.Ellipses)
            SchLibWriter.WriteEllipseRecord(writer, (SchEllipse)ellipse, ref index);

        foreach (var roundedRect in document.RoundedRectangles)
            SchLibWriter.WriteRoundedRectangleRecord(writer, (SchRoundedRectangle)roundedRect, ref index);

        foreach (var pie in document.Pies)
            SchLibWriter.WritePieRecord(writer, (SchPie)pie, ref index);

        foreach (var ellipticalArc in document.EllipticalArcs)
            SchLibWriter.WriteEllipticalArcRecord(writer, (SchEllipticalArc)ellipticalArc, ref index);

        foreach (var textFrame in document.TextFrames)
            SchLibWriter.WriteTextFrameRecord(writer, (SchTextFrame)textFrame, ref index);

        foreach (var image in document.Images)
            SchLibWriter.WriteImageRecord(writer, (SchImage)image, ref index);

        foreach (var symbol in document.Symbols)
            SchLibWriter.WriteSymbolRecord(writer, (SchSymbol)symbol, ref index);

        foreach (var noErc in document.NoErcs)
            SchLibWriter.WriteNoErcRecord(writer, noErc, ref index);

        foreach (var bus in document.Buses)
            SchLibWriter.WriteBusRecord(writer, bus, ref index);

        foreach (var busEntry in document.BusEntries)
            SchLibWriter.WriteBusEntryRecord(writer, busEntry, ref index);

        foreach (var port in document.Ports)
            SchLibWriter.WritePortRecord(writer, port, ref index);

        foreach (var sheetSymbol in document.SheetSymbols)
        {
            var sheetSymbolIndex = index;
            SchLibWriter.WriteSheetSymbolRecord(writer, sheetSymbol, ref index);
            foreach (var entry in sheetSymbol.Entries)
                SchLibWriter.WriteSheetEntryRecord(writer, entry, ref index, sheetSymbolIndex);
        }

        foreach (var sheetEntry in document.SheetEntries)
            SchLibWriter.WriteSheetEntryRecord(writer, sheetEntry, ref index);

        foreach (var parameterSet in document.ParameterSets)
        {
            var paramSetIndex = index;
            SchLibWriter.WriteParameterSetRecord(writer, parameterSet, ref index);
            foreach (var param in parameterSet.Parameters)
                SchLibWriter.WriteParameterRecord(writer, param, ref index, paramSetIndex);
        }

        foreach (var blanket in document.Blankets)
        {
            var blanketIndex = index;
            SchLibWriter.WriteBlanketRecord(writer, blanket, ref index);
            foreach (var param in blanket.Parameters)
                SchLibWriter.WriteParameterRecord(writer, param, ref index, blanketIndex);
        }
    }

    private static void WritePinAsParameterRecord(BinaryFormatWriter writer, SchPin pin, int ownerIndex, ref int index)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "2",
            ["OWNERINDEX"] = ownerIndex.ToString(),
            ["OWNERPARTID"] = pin.OwnerPartId != 0 ? pin.OwnerPartId.ToString() : "1",
            ["OWNERPARTDISPLAYMODE"] = pin.OwnerPartDisplayMode.ToString(),
            ["NAME"] = pin.Name ?? string.Empty,
            ["DESIGNATOR"] = pin.Designator ?? string.Empty,
            ["ELECTRICAL"] = ((int)pin.ElectricalType).ToString(),
            ["PINCONGLOMERATE"] = SchLibWriter.GetPinConglomerate(pin).ToString(),
            ["COLOR"] = pin.Color.ToString()
        };
        SchLibWriter.AddCoordParam(parameters, "PINLENGTH", pin.Length);
        SchLibWriter.AddCoordParam(parameters, "LOCATION.X", pin.Location.X);
        SchLibWriter.AddCoordParam(parameters, "LOCATION.Y", pin.Location.Y);

        if (!string.IsNullOrEmpty(pin.Description))
            parameters["DESCRIPTION"] = pin.Description;

        // Symbol properties
        AddNonZero(parameters, "SYMBOL_INNEREDGE", pin.SymbolInnerEdge);
        AddNonZero(parameters, "SYMBOL_OUTEREDGE", pin.SymbolOuterEdge);
        AddNonZero(parameters, "SYMBOL_INSIDE", pin.SymbolInside);
        AddNonZero(parameters, "SYMBOL_OUTSIDE", pin.SymbolOutside);
        AddNonZero(parameters, "SYMBOL_LINEWIDTH", pin.SymbolLineWidth);

        // Formal type
        AddNonZero(parameters, "FORMALTYPE", pin.FormalType);

        // Swap IDs
        if (!string.IsNullOrEmpty(pin.SwapIdPart))
            parameters["SWAPIDPART"] = pin.SwapIdPart;
        if (!string.IsNullOrEmpty(pin.SwapIdPair))
            parameters["SWAPID_PAIR"] = pin.SwapIdPair;
        if (!string.IsNullOrEmpty(pin.SwapIdPartPin))
            parameters["SWAPID_PARTPIN"] = pin.SwapIdPartPin;
        if (!string.IsNullOrEmpty(pin.SwapIdPin))
            parameters["SWAPID_PIN"] = pin.SwapIdPin;

        // Propagation delay
        AddNonZero(parameters, "PINPROPAGATIONDELAY", pin.PinPropagationDelay);

        // Font/position customization
        AddNonZero(parameters, "DESIGNATOR.CUSTOMFONTID", pin.DesignatorCustomFontId);
        AddNonZero(parameters, "DESIGNATOR.CUSTOMCOLOR", pin.DesignatorCustomColor);
        AddNonZero(parameters, "DESIGNATOR.CUSTOMPOSITION.MARGIN", pin.DesignatorCustomPositionMargin);
        AddNonZero(parameters, "DESIGNATOR.CUSTOMPOSITION.ROTATIONANCHOR", pin.DesignatorCustomPositionRotationAnchor);
        AddBool(parameters, "DESIGNATOR.CUSTOMPOSITION.ROTATIONRELATIVE", pin.DesignatorCustomPositionRotationRelative);
        AddNonZero(parameters, "DESIGNATOR.FONTMODE", pin.DesignatorFontMode);
        AddNonZero(parameters, "DESIGNATOR.POSITIONMODE", pin.DesignatorPositionMode);

        AddNonZero(parameters, "NAME.CUSTOMFONTID", pin.NameCustomFontId);
        AddNonZero(parameters, "NAME.CUSTOMCOLOR", pin.NameCustomColor);
        AddNonZero(parameters, "NAME.CUSTOMPOSITION.MARGIN", pin.NameCustomPositionMargin);
        AddNonZero(parameters, "NAME.CUSTOMPOSITION.ROTATIONANCHOR", pin.NameCustomPositionRotationAnchor);
        AddBool(parameters, "NAME.CUSTOMPOSITION.ROTATIONRELATIVE", pin.NameCustomPositionRotationRelative);
        AddNonZero(parameters, "NAME.FONTMODE", pin.NameFontMode);
        AddNonZero(parameters, "NAME.POSITIONMODE", pin.NamePositionMode);

        // Other pin properties
        AddNonZero(parameters, "WIDTH", pin.Width);
        AddNonZero(parameters, "AREACOLOR", pin.AreaColor);
        if (!string.IsNullOrEmpty(pin.DefaultValue))
            parameters["DEFAULTVALUE"] = pin.DefaultValue;
        AddBool(parameters, "ISHIDDEN", pin.IsHidden);
        if (!string.IsNullOrEmpty(pin.HiddenNetName))
            parameters["HIDDENNETNAME"] = pin.HiddenNetName;
        SchLibWriter.AddCoordParam(parameters, "PINPACKAGELENGTH", pin.PinPackageLength);

        // Common properties
        if (pin.IsNotAccessible) parameters["ISNOTACCESIBLE"] = "T";
        AddNonZero(parameters, "INDEXINSHEET", pin.IndexInSheet);
        AddBool(parameters, "GRAPHICALLYLOCKED", pin.GraphicallyLocked);
        AddBool(parameters, "DISABLED", pin.Disabled);
        AddBool(parameters, "DIMMED", pin.Dimmed);
        if (!string.IsNullOrEmpty(pin.UniqueId))
            parameters["UNIQUEID"] = pin.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static void WriteWireRecord(BinaryFormatWriter writer, SchWire wire, int ownerIndex, ref int index)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "27",
            ["COLOR"] = wire.Color.ToString(),
            ["LOCATIONCOUNT"] = wire.Vertices.Count.ToString()
        };

        if (ownerIndex >= 0) parameters["OWNERINDEX"] = ownerIndex.ToString();

        for (var i = 0; i < wire.Vertices.Count; i++)
        {
            parameters[$"X{i + 1}"] = SchLibWriter.CoordToSchematicUnits(wire.Vertices[i].X);
            parameters[$"Y{i + 1}"] = SchLibWriter.CoordToSchematicUnits(wire.Vertices[i].Y);
        }

        AddNonZero(parameters, "LINEWIDTH", wire.LineWidth);
        AddNonZero(parameters, "LINESTYLE", (int)wire.LineStyle);
        AddNonZero(parameters, "AREACOLOR", wire.AreaColor);
        AddBool(parameters, "ISSOLID", wire.IsSolid);
        AddBool(parameters, "TRANSPARENT", wire.IsTransparent);
        AddBool(parameters, "AUTOWIRE", wire.AutoWire);
        AddNonZero(parameters, "UNDERLINECOLOR", wire.UnderlineColor);

        // Common properties
        if (wire.IsNotAccessible) parameters["ISNOTACCESIBLE"] = "T";
        AddNonZero(parameters, "INDEXINSHEET", wire.IndexInSheet);
        AddNonZero(parameters, "OWNERPARTID", wire.OwnerPartId);
        AddNonZero(parameters, "OWNERPARTDISPLAYMODE", wire.OwnerPartDisplayMode);
        AddBool(parameters, "GRAPHICALLYLOCKED", wire.GraphicallyLocked);
        AddBool(parameters, "DISABLED", wire.Disabled);
        AddBool(parameters, "DIMMED", wire.Dimmed);
        if (!string.IsNullOrEmpty(wire.UniqueId))
            parameters["UNIQUEID"] = wire.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static void WriteStorage(CompoundFile cf, SchDocument document)
    {
        // Collect all embedded images from document-level and component-level
        var embeddedImages = new List<byte[]>();

        foreach (var image in document.Images)
        {
            if (image is SchImage img && img.EmbedImage && img.ImageData != null)
                embeddedImages.Add(img.ImageData);
        }

        foreach (var component in document.Components)
        {
            foreach (var image in component.Images)
            {
                if (image is SchImage img && img.EmbedImage && img.ImageData != null)
                    embeddedImages.Add(img.ImageData);
            }
        }

        SchLibWriter.WriteStorageStream(cf.RootStorage, embeddedImages);
    }

    private static void AddNonZero(Dictionary<string, string> parameters, string key, int value)
    {
        if (value != 0) parameters[key] = value.ToString();
    }

    private static void AddBool(Dictionary<string, string> parameters, string key, bool value)
    {
        if (value) parameters[key] = "T";
    }
}
