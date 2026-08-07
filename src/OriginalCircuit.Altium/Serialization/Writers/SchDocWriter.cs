using System.Globalization;
using System.Linq;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Serialization.Compound;
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
        await WriteAsync(document, stream, cancellationToken).ConfigureAwait(false);
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
        await ms.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a SchDoc file to a stream synchronously.
    /// </summary>
    /// <param name="document">The schematic document to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public void Write(SchDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        using var cf = CompoundFileAccessor.Create();

        WriteFileHeader(cf, document, cancellationToken);
        WriteStorage(cf, document);
        WriteAdditionalStreams(cf, document);

        cf.Save(stream);
    }

    private static void WriteTemplateRecord(BinaryFormatWriter writer, SchTemplate template, ref int index)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "39",
        };
        if (template.IsNotAccessible) parameters["IsNotAccesible"] = "T";
        parameters["OwnerPartId"] = template.OwnerPartId.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(template.FileName)) parameters["FileName"] = template.FileName;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    /// <summary>
    /// Writes a single primitive owned by a sheet template, dispatching on its concrete type and pointing
    /// its <c>OWNERINDEX</c> at the template record. Covers the graphic/text types a title block uses
    /// (lines, rectangles, labels, parameters, images, arcs, curves). Types a template never contains are
    /// ignored so an unexpected primitive can't corrupt the record stream.
    /// </summary>
    private static void WriteOwnedPrimitive(BinaryFormatWriter writer, object primitive, int ownerIndex, ref int index)
    {
        switch (primitive)
        {
            case SchLine line: SchLibWriter.WriteLineRecord(writer, line, ref index, ownerIndex); break;
            case SchRectangle rect: SchLibWriter.WriteRectangleRecord(writer, rect, ref index, ownerIndex); break;
            case SchLabel label: SchLibWriter.WriteLabelRecord(writer, label, ref index, ownerIndex); break;
            case SchArc arc: SchLibWriter.WriteArcRecord(writer, arc, ref index, ownerIndex); break;
            case SchPolygon polygon: SchLibWriter.WritePolygonRecord(writer, polygon, ref index, ownerIndex, SchLibWriter.CoordToDxpUnits); break;
            case SchPolyline polyline: SchLibWriter.WritePolylineRecord(writer, polyline, ref index, ownerIndex, SchLibWriter.CoordToDxpUnits); break;
            case SchBezier bezier: SchLibWriter.WriteBezierRecord(writer, bezier, ref index, ownerIndex, SchLibWriter.CoordToDxpUnits); break;
            case SchEllipse ellipse: SchLibWriter.WriteEllipseRecord(writer, ellipse, ref index, ownerIndex); break;
            case SchRoundedRectangle roundedRect: SchLibWriter.WriteRoundedRectangleRecord(writer, roundedRect, ref index, ownerIndex); break;
            case SchPie pie: SchLibWriter.WritePieRecord(writer, pie, ref index, ownerIndex); break;
            case SchEllipticalArc ellipticalArc: SchLibWriter.WriteEllipticalArcRecord(writer, ellipticalArc, ref index, ownerIndex); break;
            case SchParameter param: SchLibWriter.WriteParameterRecord(writer, param, ref index, ownerIndex); break;
            case SchNetLabel netLabel: SchLibWriter.WriteNetLabelRecord(writer, netLabel, ref index, ownerIndex); break;
            case SchJunction junction: SchLibWriter.WriteJunctionRecord(writer, junction, ref index, ownerIndex); break;
            case SchTextFrame textFrame: SchLibWriter.WriteTextFrameRecord(writer, textFrame, ref index, ownerIndex); break;
            case SchImage image: SchLibWriter.WriteImageRecord(writer, image, ref index, ownerIndex); break;
            case SchSymbol symbol: SchLibWriter.WriteSymbolRecord(writer, symbol, ref index, ownerIndex); break;
            case SchPowerObject powerObj: SchLibWriter.WritePowerObjectRecord(writer, powerObj, ref index, ownerIndex); break;
        }
    }

    private static void WriteSignalHarnessRecord(BinaryFormatWriter writer, SchSignalHarness harness, ref int index)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "218",
            ["LOCATIONCOUNT"] = harness.Vertices.Count.ToString(CultureInfo.InvariantCulture),
        };
        if (harness.OwnerIndex >= 0) parameters["OWNERINDEX"] = harness.OwnerIndex.ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < harness.Vertices.Count; i++)
        {
            parameters[$"X{i + 1}"] = SchLibWriter.CoordToDxpUnits(harness.Vertices[i].X);
            parameters[$"Y{i + 1}"] = SchLibWriter.CoordToDxpUnits(harness.Vertices[i].Y);
        }
        if (harness.Color != 0) parameters["Color"] = harness.Color.ToString(CultureInfo.InvariantCulture);
        if (harness.LineWidth != 0) parameters["LineWidth"] = harness.LineWidth.ToString(CultureInfo.InvariantCulture);
        if (harness.IsNotAccessible) parameters["IsNotAccesible"] = "T";
        if (harness.IndexInSheet != 0) parameters["IndexInSheet"] = harness.IndexInSheet.ToString(CultureInfo.InvariantCulture);
        if (harness.OwnerPartId != 0) parameters["OwnerPartId"] = harness.OwnerPartId.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(harness.UniqueId)) parameters["UniqueID"] = harness.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static void WriteHarnessConnectorRecord(BinaryFormatWriter writer, SchHarnessConnector c, ref int index)
    {
        var parameters = new Dictionary<string, string> { ["RECORD"] = "215" };
        if (c.OwnerIndex >= 0) parameters["OWNERINDEX"] = c.OwnerIndex.ToString(CultureInfo.InvariantCulture);
        if (c.IsNotAccessible) parameters["IsNotAccesible"] = "T";
        if (c.IndexInSheet != 0) parameters["IndexInSheet"] = c.IndexInSheet.ToString(CultureInfo.InvariantCulture);
        if (c.OwnerPartId != 0) parameters["OwnerPartId"] = c.OwnerPartId.ToString(CultureInfo.InvariantCulture);
        SchLibWriter.AddCoordParam(parameters, "Location.X", c.Corner1.X);
        SchLibWriter.AddCoordParam(parameters, "Location.Y", c.Corner1.Y);
        SchLibWriter.AddCoordParam(parameters, "Corner.X", c.Corner2.X);
        SchLibWriter.AddCoordParam(parameters, "Corner.Y", c.Corner2.Y);
        if (c.Color != 0) parameters["Color"] = c.Color.ToString(CultureInfo.InvariantCulture);
        if (c.AreaColor != 0) parameters["AreaColor"] = c.AreaColor.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(c.UniqueId)) parameters["UniqueID"] = c.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static void WriteHarnessEntryRecord(BinaryFormatWriter writer, SchHarnessEntry e, ref int index)
    {
        var parameters = new Dictionary<string, string> { ["RECORD"] = "216" };
        if (e.OwnerIndex >= 0) parameters["OWNERINDEX"] = e.OwnerIndex.ToString(CultureInfo.InvariantCulture);
        if (e.IsNotAccessible) parameters["IsNotAccesible"] = "T";
        if (e.IndexInSheet != 0) parameters["IndexInSheet"] = e.IndexInSheet.ToString(CultureInfo.InvariantCulture);
        if (e.OwnerPartId != 0) parameters["OwnerPartId"] = e.OwnerPartId.ToString(CultureInfo.InvariantCulture);
        if (e.Side != 0) parameters["Side"] = e.Side.ToString(CultureInfo.InvariantCulture);
        var dftRaw = e.DistanceFromTop.ToRaw();
        if (dftRaw / 100_000 != 0) parameters["DistanceFromTop"] = (dftRaw / 100_000).ToString(CultureInfo.InvariantCulture);
        if (dftRaw % 100_000 != 0) parameters["DistanceFromTop_Frac1"] = (dftRaw % 100_000).ToString(CultureInfo.InvariantCulture);
        parameters["Text"] = e.Text;
        if (e.Color != 0) parameters["Color"] = e.Color.ToString(CultureInfo.InvariantCulture);
        if (e.AreaColor != 0) parameters["AreaColor"] = e.AreaColor.ToString(CultureInfo.InvariantCulture);
        if (e.TextColor != 0) parameters["TextColor"] = e.TextColor.ToString(CultureInfo.InvariantCulture);
        if (e.TextFontId != 0) parameters["TextFontID"] = e.TextFontId.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(e.UniqueId)) parameters["UniqueID"] = e.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static void WriteHarnessTypeRecord(BinaryFormatWriter writer, SchHarnessType t, ref int index)
    {
        var parameters = new Dictionary<string, string> { ["RECORD"] = "217" };
        if (t.OwnerIndex >= 0) parameters["OWNERINDEX"] = t.OwnerIndex.ToString(CultureInfo.InvariantCulture);
        if (t.IsNotAccessible) parameters["IsNotAccesible"] = "T";
        if (t.IndexInSheet != 0) parameters["IndexInSheet"] = t.IndexInSheet.ToString(CultureInfo.InvariantCulture);
        if (t.OwnerPartId != 0) parameters["OwnerPartId"] = t.OwnerPartId.ToString(CultureInfo.InvariantCulture);
        SchLibWriter.AddCoordParam(parameters, "Location.X", t.Location.X);
        SchLibWriter.AddCoordParam(parameters, "Location.Y", t.Location.Y);
        parameters["Text"] = t.Text;
        if (t.Color != 0) parameters["Color"] = t.Color.ToString(CultureInfo.InvariantCulture);
        if (t.TextColor != 0) parameters["TextColor"] = t.TextColor.ToString(CultureInfo.InvariantCulture);
        parameters["FontID"] = t.FontId.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(t.UniqueId)) parameters["UniqueID"] = t.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static void WriteCompileMaskRecord(BinaryFormatWriter writer, SchCompileMask mask, ref int index)
    {
        var parameters = new Dictionary<string, string> { ["RECORD"] = "211" };
        if (mask.IsNotAccessible) parameters["IsNotAccesible"] = "T";
        if (mask.IndexInSheet != 0) parameters["IndexInSheet"] = mask.IndexInSheet.ToString(CultureInfo.InvariantCulture);
        if (mask.OwnerPartId != 0) parameters["OwnerPartId"] = mask.OwnerPartId.ToString(CultureInfo.InvariantCulture);
        SchLibWriter.AddCoordParam(parameters, "Location.X", mask.Corner1.X);
        SchLibWriter.AddCoordParam(parameters, "Location.Y", mask.Corner1.Y);
        SchLibWriter.AddCoordParam(parameters, "Corner.X", mask.Corner2.X);
        SchLibWriter.AddCoordParam(parameters, "Corner.Y", mask.Corner2.Y);
        if (mask.Color != 0) parameters["Color"] = mask.Color.ToString(CultureInfo.InvariantCulture);
        if (mask.AreaColor != 0) parameters["AreaColor"] = mask.AreaColor.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(mask.UniqueId)) parameters["UniqueID"] = mask.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static void WriteNoteRecord(BinaryFormatWriter writer, SchNote note, ref int index)
    {
        var parameters = new Dictionary<string, string> { ["RECORD"] = "209" };
        if (note.IsNotAccessible) parameters["IsNotAccesible"] = "T";
        if (note.IndexInSheet != 0) parameters["IndexInSheet"] = note.IndexInSheet.ToString(CultureInfo.InvariantCulture);
        if (note.OwnerPartId != 0) parameters["OwnerPartId"] = note.OwnerPartId.ToString(CultureInfo.InvariantCulture);
        SchLibWriter.AddCoordParam(parameters, "Location.X", note.Corner1.X);
        SchLibWriter.AddCoordParam(parameters, "Location.Y", note.Corner1.Y);
        SchLibWriter.AddCoordParam(parameters, "Corner.X", note.Corner2.X);
        SchLibWriter.AddCoordParam(parameters, "Corner.Y", note.Corner2.Y);
        if (note.Color != 0) parameters["Color"] = note.Color.ToString(CultureInfo.InvariantCulture);
        if (note.AreaColor != 0) parameters["AreaColor"] = note.AreaColor.ToString(CultureInfo.InvariantCulture);
        if (note.TextColor != 0) parameters["TextColor"] = note.TextColor.ToString(CultureInfo.InvariantCulture);
        parameters["Text"] = note.Text;
        parameters["FontID"] = note.FontId.ToString(CultureInfo.InvariantCulture);
        if (note.Alignment != 0) parameters["Alignment"] = note.Alignment.ToString(CultureInfo.InvariantCulture);
        if (note.WordWrap) parameters["WordWrap"] = "T";
        if (note.ClipToRect) parameters["ClipToRect"] = "T";
        if (!string.IsNullOrEmpty(note.Author)) parameters["Author"] = note.Author;
        if (!string.IsNullOrEmpty(note.UniqueId)) parameters["UniqueID"] = note.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static void WriteHyperlinkRecord(BinaryFormatWriter writer, SchHyperlink hyperlink, ref int index)
    {
        var parameters = new Dictionary<string, string> { ["RECORD"] = "226" };
        if (hyperlink.IsNotAccessible) parameters["IsNotAccesible"] = "T";
        if (hyperlink.IndexInSheet != 0) parameters["IndexInSheet"] = hyperlink.IndexInSheet.ToString(CultureInfo.InvariantCulture);
        if (hyperlink.OwnerPartId != 0) parameters["OwnerPartId"] = hyperlink.OwnerPartId.ToString(CultureInfo.InvariantCulture);
        SchLibWriter.AddCoordParam(parameters, "Location.X", hyperlink.Location.X);
        SchLibWriter.AddCoordParam(parameters, "Location.Y", hyperlink.Location.Y);
        if (hyperlink.Color != 0) parameters["Color"] = hyperlink.Color.ToString(CultureInfo.InvariantCulture);
        parameters["Text"] = hyperlink.Text;
        if (!string.IsNullOrEmpty(hyperlink.Url)) parameters["URL"] = hyperlink.Url;
        parameters["FontID"] = hyperlink.FontId.ToString(CultureInfo.InvariantCulture);
        if (hyperlink.Orientation != 0) parameters["Orientation"] = hyperlink.Orientation.ToString(CultureInfo.InvariantCulture);
        if (hyperlink.Justification != 0) parameters["Justification"] = hyperlink.Justification.ToString(CultureInfo.InvariantCulture);
        if (hyperlink.AreaColor != 0) parameters["AreaColor"] = hyperlink.AreaColor.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(hyperlink.UniqueId)) parameters["UniqueID"] = hyperlink.UniqueId;

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    private static string BuildOrderedParamString(List<KeyValuePair<string, string>> ordered)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in ordered)
            sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
        return sb.ToString();
    }

    private static void WriteFileHeader(CompoundFileAccessor cf, SchDocument document, CancellationToken cancellationToken = default)
    {
        var headerStream = cf.RootStorage.AddStream("FileHeader");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Push SchComponent.Comment into its backing "Comment" parameter before deciding how to
        // serialize: the byte-faithful path below replays captured bytes rather than live model state,
        // so a Comment edit must be detected here to force the typed path, or it would be silently lost.
        var commentEdited = false;
        foreach (var component in document.Components.Cast<SchComponent>())
            commentEdited |= SchLibWriter.SyncComponentComment(component);

        // Byte-faithful path: when the document was read from a file and is unedited, walk the captured
        // record order (each entry linked to its model object) and re-emit each record's parameters
        // verbatim — preserving order, duplicate keys and unmodeled parameters. Files built from scratch,
        // edited (primitive added/removed or Comment changed), or containing binary-pin records fall
        // through to the typed serialization below.
        if (!commentEdited &&
            document.ReadOrderedRecords is { Count: > 0 } orderedRecords &&
            document.HeaderParametersOrdered is { Count: > 0 } headerOrdered &&
            document.LoadedPrimitiveCount == document.CountModeledPrimitives())
        {
            writer.WriteCStringParameterBlockRaw(BuildOrderedParamString(headerOrdered));
            foreach (var rec in orderedRecords)
                writer.WriteCStringParameterBlockRaw(BuildOrderedParamString(rec.OrderedParams));
            writer.Flush();
            headerStream.SetData(ms.ToArray());
            return;
        }

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

        // Write Record 31 (Sheet Settings) if present — occupies a primitive index slot
        if (document.SheetSettings != null && document.SheetSettings.Count > 0)
        {
            writer.WriteCStringParameterBlock(document.SheetSettings);
            index++;
        }

        foreach (var component in document.Components.Cast<SchComponent>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var componentIndex = index;
            SchLibWriter.WriteComponentRecord(writer, component, ref index, writeLocation: true);

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
            SchLibWriter.WritePolygonRecord(writer, (SchPolygon)polygon, ref index, ownerIndex, SchLibWriter.CoordToDxpUnits);

        foreach (var polyline in component.Polylines)
            SchLibWriter.WritePolylineRecord(writer, (SchPolyline)polyline, ref index, ownerIndex, SchLibWriter.CoordToDxpUnits);

        foreach (var bezier in component.Beziers)
            SchLibWriter.WriteBezierRecord(writer, (SchBezier)bezier, ref index, ownerIndex, SchLibWriter.CoordToDxpUnits);

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
                ["OWNERINDEX"] = componentIndex.ToString(CultureInfo.InvariantCulture)
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
            ["OWNERINDEX"] = componentIndex.ToString(CultureInfo.InvariantCulture)
        };
        writer.WriteCStringParameterBlock(implListParams);
        index++;

        foreach (var impl in component.Implementations.Cast<SchImplementation>())
        {
            // Record 45: Implementation, owned by ImplementationList
            var implIndex = index;
            SchLibWriter.WriteImplementationRecord(writer, impl, ref index, implListIndex);

            // Record 46: MapDefinerList container, owned by Implementation
            // Always write even when empty — Altium expects this container
            var mdlIndex = index;
            var mdlParams = new Dictionary<string, string>
            {
                ["RECORD"] = "46",
                ["OWNERINDEX"] = implIndex.ToString(CultureInfo.InvariantCulture)
            };
            writer.WriteCStringParameterBlock(mdlParams);
            index++;

            foreach (var mapDefiner in impl.MapDefiners.Cast<SchMapDefiner>())
            {
                // Record 47: MapDefiner, owned by MapDefinerList
                SchLibWriter.WriteMapDefinerRecord(writer, mapDefiner, ref index, mdlIndex);
            }

            // Record 48: ImplementationParameters, owned by Implementation
            var ipParams = new Dictionary<string, string>
            {
                ["RECORD"] = "48",
                ["OWNERINDEX"] = implIndex.ToString(CultureInfo.InvariantCulture)
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
            SchLibWriter.WritePolygonRecord(writer, (SchPolygon)polygon, ref index, vertexUnits: SchLibWriter.CoordToDxpUnits);

        foreach (var polyline in document.Polylines)
            SchLibWriter.WritePolylineRecord(writer, (SchPolyline)polyline, ref index, vertexUnits: SchLibWriter.CoordToDxpUnits);

        foreach (var arc in document.Arcs)
            SchLibWriter.WriteArcRecord(writer, (SchArc)arc, ref index);

        foreach (var bezier in document.Beziers)
            SchLibWriter.WriteBezierRecord(writer, (SchBezier)bezier, ref index, vertexUnits: SchLibWriter.CoordToDxpUnits);

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
            SchLibWriter.WriteBusRecord(writer, bus, ref index, vertexUnits: SchLibWriter.CoordToDxpUnits);

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
            SchLibWriter.WriteBlanketRecord(writer, blanket, ref index, vertexUnits: SchLibWriter.CoordToDxpUnits);
            foreach (var param in blanket.Parameters)
                SchLibWriter.WriteParameterRecord(writer, param, ref index, blanketIndex);
        }

        foreach (var template in document.Templates)
        {
            var templateIndex = index;
            WriteTemplateRecord(writer, template, ref index);
            // Emit the template's border/title-block graphics as child records owning back to it, so a
            // re-read re-attaches them to the template (matching the reader) instead of the document.
            foreach (var owned in template.OwnedPrimitives)
                WriteOwnedPrimitive(writer, owned, templateIndex, ref index);
        }

        foreach (var note in document.Notes)
            WriteNoteRecord(writer, note, ref index);

        foreach (var hyperlink in document.Hyperlinks)
            WriteHyperlinkRecord(writer, hyperlink, ref index);

        foreach (var compileMask in document.CompileMasks)
            WriteCompileMaskRecord(writer, compileMask, ref index);

        // Harnesses read from the "Additional" stream are preserved verbatim there — don't also emit
        // them here, or a round-trip duplicates every harness object.
        if (!document.HarnessesInAdditionalStream)
        {
            foreach (var harnessConnector in document.HarnessConnectors)
                WriteHarnessConnectorRecord(writer, harnessConnector, ref index);

            foreach (var harnessEntry in document.HarnessEntries)
                WriteHarnessEntryRecord(writer, harnessEntry, ref index);

            foreach (var harnessType in document.HarnessTypes)
                WriteHarnessTypeRecord(writer, harnessType, ref index);

            foreach (var signalHarness in document.SignalHarnesses)
                WriteSignalHarnessRecord(writer, signalHarness, ref index);
        }

        // Write opaque (unmodeled) records for round-trip fidelity
        foreach (var record in document.OpaqueRecords)
        {
            writer.WriteCStringParameterBlock(record);
            index++;
        }
    }

    private static void WritePinAsParameterRecord(BinaryFormatWriter writer, SchPin pin, int ownerIndex, ref int index)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "2",
            ["OWNERINDEX"] = ownerIndex.ToString(CultureInfo.InvariantCulture),
            ["OWNERPARTID"] = pin.OwnerPartId != 0 ? pin.OwnerPartId.ToString(CultureInfo.InvariantCulture) : "1",
            ["OWNERPARTDISPLAYMODE"] = pin.OwnerPartDisplayMode.ToString(CultureInfo.InvariantCulture),
            ["NAME"] = pin.Name ?? string.Empty,
            ["DESIGNATOR"] = pin.Designator ?? string.Empty,
            ["ELECTRICAL"] = ((int)pin.ElectricalType).ToString(CultureInfo.InvariantCulture),
            ["PINCONGLOMERATE"] = SchLibWriter.GetPinConglomerate(pin).ToString(CultureInfo.InvariantCulture),
            ["COLOR"] = pin.Color.ToString(CultureInfo.InvariantCulture)
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

        // Propagation delay — always write, using scientific notation format to match Altium's "0.000000E+000"
        parameters["PINPROPAGATIONDELAY"] = ((double)pin.PinPropagationDelay).ToString("0.000000E+000", CultureInfo.InvariantCulture);

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
            ["COLOR"] = wire.Color.ToString(CultureInfo.InvariantCulture),
            ["LOCATIONCOUNT"] = wire.Vertices.Count.ToString(CultureInfo.InvariantCulture)
        };

        if (ownerIndex >= 0) parameters["OWNERINDEX"] = ownerIndex.ToString(CultureInfo.InvariantCulture);

        for (var i = 0; i < wire.Vertices.Count; i++)
        {
            parameters[$"X{i + 1}"] = SchLibWriter.CoordToDxpUnits(wire.Vertices[i].X);
            parameters[$"Y{i + 1}"] = SchLibWriter.CoordToDxpUnits(wire.Vertices[i].Y);
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

    private static void WriteStorage(CompoundFileAccessor cf, SchDocument document)
    {
        // Collect all embedded images from template-, document- and component-level, in the SAME order the
        // reader collects them (templates first) so blobs pair with the right image on the next read.
        var embeddedImages = new List<byte[]>();

        foreach (var template in document.Templates)
        {
            foreach (var primitive in template.OwnedPrimitives)
            {
                if (primitive is SchImage timg && timg.EmbedImage && timg.ImageData != null)
                    embeddedImages.Add(timg.ImageData);
            }
        }

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

    private static void WriteAdditionalStreams(CompoundFileAccessor cf, SchDocument document)
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
                var rootStream = cf.RootStorage.AddStream(kvp.Key);
                rootStream.SetData(kvp.Value);
            }
        }

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

    private static void AddNonZero(Dictionary<string, string> parameters, string key, int value)
    {
        if (value != 0) parameters[key] = value.ToString(CultureInfo.InvariantCulture);
    }

    private static void AddBool(Dictionary<string, string> parameters, string key, bool value)
    {
        if (value) parameters[key] = "T";
    }
}
