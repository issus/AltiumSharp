using OpenMcdf;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using System.IO.Compression;
using System.Text;

namespace OriginalCircuit.Altium.Serialization.Writers;

/// <summary>
/// Writes schematic symbol library (.SchLib) files.
/// </summary>
public sealed class SchLibWriter
{
    /// <summary>
    /// Writes a SchLib file to the specified path.
    /// </summary>
    /// <param name="library">The schematic library to write.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="overwrite">If true, overwrites an existing file; otherwise throws if the file exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public async ValueTask WriteAsync(SchLibrary library, string path, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        await using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await WriteAsync(library, stream, cancellationToken);
    }

    /// <summary>
    /// Writes a SchLib file to a stream.
    /// </summary>
    /// <param name="library">The schematic library to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public async ValueTask WriteAsync(SchLibrary library, Stream stream, CancellationToken cancellationToken = default)
    {
        // Write synchronously to memory, then copy to output stream
        using var ms = new MemoryStream();
        Write(library, ms, cancellationToken);
        ms.Position = 0;
        await ms.CopyToAsync(stream, cancellationToken);
    }

    /// <summary>
    /// Writes a SchLib file to a stream synchronously.
    /// </summary>
    /// <param name="library">The schematic library to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public void Write(SchLibrary library, Stream stream, CancellationToken cancellationToken = default)
    {
        using var cf = new CompoundFile();
        var sectionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        WriteFileHeader(cf, library);
        WriteSectionKeys(cf, library, sectionKeys);

        foreach (var component in library.Components.Cast<SchComponent>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteComponent(cf, component, sectionKeys);
        }

        // Write empty Storage section (no embedded images for now)
        WriteStorage(cf, library);

        cf.Save(stream);
    }

    private static void WriteFileHeader(CompoundFile cf, SchLibrary library)
    {
        var headerStream = cf.RootStorage.AddStream("FileHeader");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Write header parameters - Altium uses mixed case keys
        var headerParams = new Dictionary<string, string>
        {
            ["HEADER"] = "Protel for Windows - Schematic Library Editor Binary File Version 5.0",
            ["Weight"] = library.Components.Count.ToString()
        };
        writer.WriteCStringParameterBlock(headerParams);

        // Write component count and names
        writer.Write(library.Components.Count);
        foreach (var component in library.Components)
        {
            writer.WriteStringBlock(component.Name);
        }

        writer.Flush();
        headerStream.SetData(ms.ToArray());
    }

    private static void WriteSectionKeys(CompoundFile cf, SchLibrary library, Dictionary<string, string> sectionKeys)
    {
        // Use preserved section keys if available
        if (library.SectionKeys != null && library.SectionKeys.Count > 0)
        {
            foreach (var kvp in library.SectionKeys)
                sectionKeys[kvp.Key] = kvp.Value;
        }

        // Build section keys for components that need them
        var componentsNeedingKeys = new List<ISchComponent>();
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

        // Write as parameter block format - Altium uses mixed case keys
        var parameters = new Dictionary<string, string>
        {
            ["KeyCount"] = componentsNeedingKeys.Count.ToString()
        };

        for (var i = 0; i < componentsNeedingKeys.Count; i++)
        {
            var component = componentsNeedingKeys[i];
            parameters[$"LibRef{i}"] = component.Name;
            parameters[$"SectionKey{i}"] = sectionKeys[component.Name];
        }

        writer.WriteCStringParameterBlock(parameters);

        writer.Flush();
        sectionKeysStream.SetData(ms.ToArray());
    }

    private static void WriteComponent(CompoundFile cf, SchComponent component, Dictionary<string, string> sectionKeys)
    {
        var sectionKey = sectionKeys.TryGetValue(component.Name, out var key)
            ? key
            : GetSectionKeyFromName(component.Name);

        var componentStorage = cf.RootStorage.AddStorage(sectionKey);

        // Write component data
        var dataStream2 = componentStorage.AddStream("Data");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Track pin data for separate streams
        var pinsFrac = new Dictionary<int, (int x, int y, int length)>();
        var pinsSymbolLineWidth = new Dictionary<int, Dictionary<string, string>>();

        var index = 0;
        var pinIndex = 0;

        // Write component record (the root primitive)
        WriteComponentRecord(writer, component, ref index);

        // Write pins
        foreach (var pin in component.Pins)
        {
            WritePinRecord(writer, (SchPin)pin, pinIndex, pinsFrac, pinsSymbolLineWidth);
            pinIndex++;
            index++;
        }

        // Write lines
        foreach (var line in component.Lines)
        {
            WriteLineRecord(writer, (SchLine)line, ref index);
        }

        // Write rectangles
        foreach (var rect in component.Rectangles)
        {
            WriteRectangleRecord(writer, (SchRectangle)rect, ref index);
        }

        // Write labels
        foreach (var label in component.Labels)
        {
            WriteLabelRecord(writer, (SchLabel)label, ref index);
        }

        // Write arcs
        foreach (var arc in component.Arcs)
        {
            WriteArcRecord(writer, (SchArc)arc, ref index);
        }

        // Write polygons
        foreach (var polygon in component.Polygons)
        {
            WritePolygonRecord(writer, (SchPolygon)polygon, ref index);
        }

        // Write polylines
        foreach (var polyline in component.Polylines)
        {
            WritePolylineRecord(writer, (SchPolyline)polyline, ref index);
        }

        // Write wires
        foreach (var wire in component.Wires)
        {
            WriteWireRecord(writer, (SchWire)wire, ref index);
        }

        // Write beziers
        foreach (var bezier in component.Beziers)
        {
            WriteBezierRecord(writer, (SchBezier)bezier, ref index);
        }

        // Write ellipses
        foreach (var ellipse in component.Ellipses)
        {
            WriteEllipseRecord(writer, (SchEllipse)ellipse, ref index);
        }

        // Write rounded rectangles
        foreach (var roundedRect in component.RoundedRectangles)
        {
            WriteRoundedRectangleRecord(writer, (SchRoundedRectangle)roundedRect, ref index);
        }

        // Write pies
        foreach (var pie in component.Pies)
        {
            WritePieRecord(writer, (SchPie)pie, ref index);
        }

        // Write elliptical arcs
        foreach (var ellipticalArc in component.EllipticalArcs)
        {
            WriteEllipticalArcRecord(writer, (SchEllipticalArc)ellipticalArc, ref index);
        }

        // Write parameters
        foreach (var param in component.Parameters)
        {
            WriteParameterRecord(writer, (SchParameter)param, ref index);
        }

        // Write net labels
        foreach (var netLabel in component.NetLabels)
        {
            WriteNetLabelRecord(writer, (SchNetLabel)netLabel, ref index);
        }

        // Write junctions
        foreach (var junction in component.Junctions)
        {
            WriteJunctionRecord(writer, (SchJunction)junction, ref index);
        }

        // Write text frames
        foreach (var textFrame in component.TextFrames)
        {
            WriteTextFrameRecord(writer, (SchTextFrame)textFrame, ref index);
        }

        // Write images
        foreach (var image in component.Images)
        {
            WriteImageRecord(writer, (SchImage)image, ref index);
        }

        // Write symbols
        foreach (var symbol in component.Symbols)
        {
            WriteSymbolRecord(writer, (SchSymbol)symbol, ref index);
        }

        // Write power objects
        foreach (var powerObj in component.PowerObjects)
        {
            WritePowerObjectRecord(writer, (SchPowerObject)powerObj, ref index);
        }

        // Write implementation records (records 44-48) — Altium writes these at the end
        WriteImplementationRecords(writer, component, ref index);

        writer.Flush();
        dataStream2.SetData(ms.ToArray());

        // Write optional pin-related streams
        WritePinFrac(componentStorage, pinsFrac);
        WritePinSymbolLineWidth(componentStorage, pinsSymbolLineWidth);
    }

    internal static void WriteComponentRecord(BinaryFormatWriter writer, SchComponent component, ref int index)
    {
        // Parameter keys match Altium's exact mixed-case convention
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "1",
            ["LibReference"] = component.Name,
            ["ComponentDescription"] = component.Description ?? string.Empty,
            ["PartCount"] = (component.PartCount + 1).ToString(),
            ["DisplayModeCount"] = "1",
            ["IndexInSheet"] = "-1",
            ["OwnerPartId"] = "-1",
            ["CurrentPartId"] = "1",
            ["LibraryPath"] = "*",
            ["SourceLibraryName"] = "*",
            ["SheetPartFileName"] = "*",
            ["TargetFileName"] = "*"
        };

        if (component is SchComponent schComp)
        {
            if (!string.IsNullOrEmpty(schComp.UniqueId))
                parameters["UniqueID"] = schComp.UniqueId;

            if (schComp.DisplayModeCount > 1)
                parameters["DisplayModeCount"] = schComp.DisplayModeCount.ToString();
            AddNonZero(parameters, "DisplayMode", schComp.DisplayMode);
            AddNonZero(parameters, "Orientation", schComp.Orientation);
            if (schComp.CurrentPartId > 1)
                parameters["CurrentPartId"] = schComp.CurrentPartId.ToString();
            AddBool(parameters, "ShowHiddenPins", schComp.ShowHiddenPins);
            AddBool(parameters, "ShowHiddenFields", schComp.ShowHiddenFields);
            if (!string.IsNullOrEmpty(schComp.LibraryPath) && schComp.LibraryPath != "*")
                parameters["LibraryPath"] = schComp.LibraryPath;
            if (!string.IsNullOrEmpty(schComp.SourceLibraryName) && schComp.SourceLibraryName != "*")
                parameters["SourceLibraryName"] = schComp.SourceLibraryName;
            if (!string.IsNullOrEmpty(schComp.LibReference))
                parameters["LibReference"] = schComp.LibReference;
            if (!string.IsNullOrEmpty(schComp.DesignItemId))
                parameters["DesignItemId"] = schComp.DesignItemId;
            // Note: DesignatorPrefix is NOT written to RECORD=1 (Altium derives it from the child Designator parameter text)
            AddNonZero(parameters, "ComponentKind", schComp.ComponentKind);
            AddBool(parameters, "OverideColors", schComp.OverrideColors);
            // Altium writes AreaColor before Color
            AddNonZero(parameters, "AreaColor", schComp.AreaColor);
            AddNonZero(parameters, "Color", schComp.Color);
            AddBool(parameters, "DesignatorLocked", schComp.DesignatorLocked);
            AddBool(parameters, "PartIDLocked", schComp.PartIdLocked);
            if (!string.IsNullOrEmpty(schComp.SymbolReference))
                parameters["SymbolReference"] = schComp.SymbolReference;
            if (!string.IsNullOrEmpty(schComp.SheetPartFileName) && schComp.SheetPartFileName != "*")
                parameters["SheetPartFileName"] = schComp.SheetPartFileName;
            if (!string.IsNullOrEmpty(schComp.TargetFileName) && schComp.TargetFileName != "*")
                parameters["TargetFileName"] = schComp.TargetFileName;
            if (!string.IsNullOrEmpty(schComp.AliasList))
                parameters["AliasList"] = schComp.AliasList;
            AddNonZero(parameters, "AllPinCount", schComp.AllPinCount);
            AddBool(parameters, "GraphicallyLocked", schComp.GraphicallyLocked);
            if (!string.IsNullOrEmpty(schComp.DatabaseLibraryName))
                parameters["DatabaseLibraryName"] = schComp.DatabaseLibraryName;
            if (!string.IsNullOrEmpty(schComp.DatabaseTableName))
                parameters["DatabaseTableName"] = schComp.DatabaseTableName;
            if (!string.IsNullOrEmpty(schComp.LibraryIdentifier))
                parameters["LibraryIdentifier"] = schComp.LibraryIdentifier;
            if (!string.IsNullOrEmpty(schComp.VaultGuid))
                parameters["VaultGUID"] = schComp.VaultGuid;
            if (!string.IsNullOrEmpty(schComp.ItemGuid))
                parameters["ItemGUID"] = schComp.ItemGuid;
            if (!string.IsNullOrEmpty(schComp.RevisionGuid))
                parameters["RevisionGUID"] = schComp.RevisionGuid;
            AddBool(parameters, "PinsMoveable", schComp.PinsMoveable);
            AddNonZero(parameters, "PinColor", schComp.PinColor);
            if (!string.IsNullOrEmpty(schComp.NotUsedBTableName))
                parameters["NotUsedBTableName"] = schComp.NotUsedBTableName;
            if (!string.IsNullOrEmpty(schComp.ConfigurationParameters))
                parameters["ConfigurationParameters"] = schComp.ConfigurationParameters;
            if (!string.IsNullOrEmpty(schComp.ConfiguratorName))
                parameters["ConfiguratorName"] = schComp.ConfiguratorName;
            AddBool(parameters, "Disabled", schComp.Disabled);
            AddBool(parameters, "Dimmed", schComp.Dimmed);
            AddBool(parameters, "DisplayFieldNames", schComp.DisplayFieldNames);
            AddBool(parameters, "IsMirrored", schComp.IsMirrored);
            AddBool(parameters, "IsUnmanaged", schComp.IsUnmanaged);
            AddBool(parameters, "IsUserConfigurable", schComp.IsUserConfigurable);
            AddNonZero(parameters, "LibIdentifierKind", schComp.LibIdentifierKind);
            AddNonZero(parameters, "OwnerPartDisplayMode", schComp.OwnerPartDisplayMode);
            if (!string.IsNullOrEmpty(schComp.RevisionDetails))
                parameters["RevisionDetails"] = schComp.RevisionDetails;
            if (!string.IsNullOrEmpty(schComp.RevisionHrid))
                parameters["RevisionHRID"] = schComp.RevisionHrid;
            if (!string.IsNullOrEmpty(schComp.RevisionState))
                parameters["RevisionState"] = schComp.RevisionState;
            if (!string.IsNullOrEmpty(schComp.RevisionStatus))
                parameters["RevisionStatus"] = schComp.RevisionStatus;
            if (!string.IsNullOrEmpty(schComp.SymbolItemGuid))
                parameters["SymbolItemGUID"] = schComp.SymbolItemGuid;
            if (!string.IsNullOrEmpty(schComp.SymbolItemsGuid))
                parameters["SymbolItemsGUID"] = schComp.SymbolItemsGuid;
            if (!string.IsNullOrEmpty(schComp.SymbolRevisionGuid))
                parameters["SymbolRevisionGUID"] = schComp.SymbolRevisionGuid;
            if (!string.IsNullOrEmpty(schComp.SymbolVaultGuid))
                parameters["SymbolVaultGUID"] = schComp.SymbolVaultGuid;
            AddBool(parameters, "UseDBTableName", schComp.UseDbTableName);
            AddBool(parameters, "UseLibraryName", schComp.UseLibraryName);
            AddNonZero(parameters, "VariantOption", schComp.VariantOption);
            if (!string.IsNullOrEmpty(schComp.VaultHrid))
                parameters["VaultHRID"] = schComp.VaultHrid;
            if (!string.IsNullOrEmpty(schComp.GenericComponentTemplateGuid))
                parameters["GenericComponentTemplateGUEID"] = schComp.GenericComponentTemplateGuid;
            // Altium doesn't write Location for components in SchLib (only SchDoc)
        }

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WritePinRecord(BinaryFormatWriter writer, SchPin pin, int pinIndex,
        Dictionary<int, (int x, int y, int length)> pinsFrac,
        Dictionary<int, Dictionary<string, string>> pinsSymbolLineWidth)
    {
        // Convert coordinates to DXP format (internal units with fractional part)
        var locationX = CoordToDxpFrac(pin.Location.X);
        var locationY = CoordToDxpFrac(pin.Location.Y);
        var length = CoordToDxpFrac(pin.Length);

        writer.WriteBlock(w =>
        {
            w.Write(2); // Pin record type (Int32)
            w.Write((byte)0); // Unknown
            w.Write((short)1); // OwnerPartId
            w.Write((byte)0); // OwnerPartDisplayMode
            w.Write((byte)pin.SymbolInnerEdge);
            w.Write((byte)pin.SymbolOuterEdge);
            w.Write((byte)pin.SymbolInside);
            w.Write((byte)pin.SymbolOutside);
            w.WritePascalShortString(pin.Description ?? string.Empty);
            w.Write((byte)pin.FormalType);
            w.Write((byte)pin.ElectricalType);
            w.Write((byte)GetPinConglomerate(pin));
            w.Write((short)length.num);
            w.Write((short)locationX.num);
            w.Write((short)locationY.num);
            w.Write(pin.Color);
            w.WritePascalShortString(pin.Name ?? string.Empty);
            w.WritePascalShortString(pin.Designator ?? string.Empty);
            w.WritePascalShortString(string.Empty); // SwapIdGroup (always empty in binary format)
            // PartAndSequence: format is "Part|&|Sequence", Altium always writes "|&|" even when empty
            var partAndSequence = string.Empty;
            if (pin.SwapIdPart != null && pin.SwapIdPart != "0")
                partAndSequence = $"{pin.SwapIdPart}|&|";
            else
                partAndSequence = "|&|"; // Altium always writes this delimiter
            w.WritePascalShortString(partAndSequence);
            w.WritePascalShortString(pin.DefaultValue ?? string.Empty);
        }, 0x01); // Flag = 1 for pin records

        // Store fractional parts if non-zero
        if (locationX.frac != 0 || locationY.frac != 0 || length.frac != 0)
        {
            pinsFrac[pinIndex] = (locationX.frac, locationY.frac, length.frac);
        }

        // Store symbol line width only when non-default
        if (pin.SymbolLineWidth != 0)
        {
            pinsSymbolLineWidth[pinIndex] = new Dictionary<string, string>
            {
                ["SYMBOL_LINEWIDTH"] = pin.SymbolLineWidth.ToString()
            };
        }
    }

    internal static byte GetPinConglomerate(SchPin pin)
    {
        // Conglomerate byte: orientation in lower 2 bits, visibility flags in upper bits
        byte conglomerate = (byte)pin.Orientation;

        if (pin.IsHidden) conglomerate |= 0x04;
        // Bit 3: show name, Bit 4: show designator (set = visible)
        if (pin.ShowName) conglomerate |= 0x08;
        if (pin.ShowDesignator) conglomerate |= 0x10;
        // Bit 5: unknown, always set by Altium
        conglomerate |= 0x20;
        if (pin.GraphicallyLocked) conglomerate |= 0x40;

        return conglomerate;
    }

    internal static void WriteLineRecord(BinaryFormatWriter writer, SchLine line, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "13",
        };

        AddCommonProperties(parameters, line.OwnerIndex, line.IsNotAccessible, line.IndexInSheet,
            line.OwnerPartId, line.OwnerPartDisplayMode, line.GraphicallyLocked,
            line.Disabled, line.Dimmed, line.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", line.Start.X);
        AddCoordParam(parameters, "Location.Y", line.Start.Y);
        AddCoordParam(parameters, "Corner.X", line.End.X);
        AddCoordParam(parameters, "Corner.Y", line.End.Y);
        parameters["LineWidth"] = LineWidthToIndex(line.Width).ToString();
        AddNonZero(parameters, "LineStyle", line.LineStyle);
        AddNonZero(parameters, "LineStyleExt", line.LineStyle); // Altium writes both
        AddNonZero(parameters, "Color", line.Color);
        AddNonZero(parameters, "AreaColor", line.AreaColor);
        AddUniqueId(parameters, line.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteRectangleRecord(BinaryFormatWriter writer, SchRectangle rect, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "14",
        };

        AddCommonProperties(parameters, rect.OwnerIndex, rect.IsNotAccessible, rect.IndexInSheet,
            rect.OwnerPartId, rect.OwnerPartDisplayMode, rect.GraphicallyLocked,
            rect.Disabled, rect.Dimmed, rect.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", rect.Corner1.X);
        AddCoordParam(parameters, "Location.Y", rect.Corner1.Y);
        AddCoordParam(parameters, "Corner.X", rect.Corner2.X);
        AddCoordParam(parameters, "Corner.Y", rect.Corner2.Y);
        parameters["LineWidth"] = LineWidthToIndex(rect.LineWidth).ToString();
        AddNonZero(parameters, "LineStyle", rect.LineStyle);
        AddNonZero(parameters, "Color", rect.Color);
        parameters["AreaColor"] = rect.FillColor.ToString();
        AddBool(parameters, "IsSolid", rect.IsFilled);
        AddBool(parameters, "Transparent", rect.IsTransparent);
        AddUniqueId(parameters, rect.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteLabelRecord(BinaryFormatWriter writer, SchLabel label, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "4",
        };

        AddCommonProperties(parameters, label.OwnerIndex, label.IsNotAccessible, label.IndexInSheet,
            label.OwnerPartId, label.OwnerPartDisplayMode, label.GraphicallyLocked,
            label.Disabled, label.Dimmed, label.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", label.Location.X);
        AddCoordParam(parameters, "Location.Y", label.Location.Y);
        AddNonZero(parameters, "Justification", (int)label.Justification);
        parameters["FontID"] = label.FontId.ToString();
        parameters["Text"] = label.Text;
        AddNonZero(parameters, "Color", label.Color);
        AddNonZero(parameters, "AreaColor", label.AreaColor);
        AddBool(parameters, "IsHidden", label.IsHidden);
        AddBool(parameters, "IsMirrored", label.IsMirrored);
        AddNonZero(parameters, "Orientation", (int)(label.Rotation / 90) % 4);
        AddUniqueId(parameters, label.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteArcRecord(BinaryFormatWriter writer, SchArc arc, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "12",
        };

        AddCommonProperties(parameters, arc.OwnerIndex, arc.IsNotAccessible, arc.IndexInSheet,
            arc.OwnerPartId, arc.OwnerPartDisplayMode, arc.GraphicallyLocked,
            arc.Disabled, arc.Dimmed, arc.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", arc.Center.X);
        AddCoordParam(parameters, "Location.Y", arc.Center.Y);
        AddCoordParam(parameters, "Radius", arc.Radius);
        parameters["LineWidth"] = arc.LineWidth.ToString();
        if (arc.StartAngle != 0)
            parameters["StartAngle"] = FormatAngle(arc.StartAngle);
        parameters["EndAngle"] = FormatAngle(arc.EndAngle);
        AddNonZero(parameters, "Color", arc.Color);
        AddNonZero(parameters, "AreaColor", arc.AreaColor);
        AddUniqueId(parameters, arc.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WritePolygonRecord(BinaryFormatWriter writer, SchPolygon polygon, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "7",
        };

        AddCommonProperties(parameters, polygon.OwnerIndex, polygon.IsNotAccessible, polygon.IndexInSheet,
            polygon.OwnerPartId, polygon.OwnerPartDisplayMode, polygon.GraphicallyLocked,
            polygon.Disabled, polygon.Dimmed, polygon.UniqueId, ownerIndex);

        parameters["LineWidth"] = polygon.LineWidth.ToString();
        AddNonZero(parameters, "Color", polygon.Color);
        parameters["AreaColor"] = polygon.FillColor.ToString();
        AddBool(parameters, "IsSolid", polygon.IsFilled);
        AddBool(parameters, "Transparent", polygon.IsTransparent);
        parameters["LocationCount"] = polygon.Vertices.Count.ToString();

        for (var i = 0; i < polygon.Vertices.Count; i++)
        {
            var v = polygon.Vertices[i];
            parameters[$"X{i + 1}"] = CoordToSchematicUnits(v.X);
            parameters[$"Y{i + 1}"] = CoordToSchematicUnits(v.Y);
        }
        AddUniqueId(parameters, polygon.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WritePolylineRecord(BinaryFormatWriter writer, SchPolyline polyline, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "6",
        };

        AddCommonProperties(parameters, polyline.OwnerIndex, polyline.IsNotAccessible, polyline.IndexInSheet,
            polyline.OwnerPartId, polyline.OwnerPartDisplayMode, polyline.GraphicallyLocked,
            polyline.Disabled, polyline.Dimmed, polyline.UniqueId, ownerIndex);

        parameters["LineWidth"] = polyline.LineWidth.ToString();
        AddNonZero(parameters, "Color", polyline.Color);
        AddNonZero(parameters, "LineShapeSize", polyline.LineShapeSize);
        AddNonZero(parameters, "StartLineShape", polyline.StartLineShape);
        AddNonZero(parameters, "EndLineShape", polyline.EndLineShape);
        AddNonZero(parameters, "LineStyle", (int)polyline.LineStyle);
        AddBool(parameters, "Transparent", polyline.IsTransparent);
        AddNonZero(parameters, "AreaColor", polyline.AreaColor);
        AddBool(parameters, "IsSolid", polyline.IsSolid);
        parameters["LocationCount"] = polyline.Vertices.Count.ToString();

        for (var i = 0; i < polyline.Vertices.Count; i++)
        {
            var v = polyline.Vertices[i];
            parameters[$"X{i + 1}"] = CoordToSchematicUnits(v.X);
            parameters[$"Y{i + 1}"] = CoordToSchematicUnits(v.Y);
        }
        AddUniqueId(parameters, polyline.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteBezierRecord(BinaryFormatWriter writer, SchBezier bezier, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "5",
        };

        AddCommonProperties(parameters, bezier.OwnerIndex, bezier.IsNotAccessible, bezier.IndexInSheet,
            bezier.OwnerPartId, bezier.OwnerPartDisplayMode, bezier.GraphicallyLocked,
            bezier.Disabled, bezier.Dimmed, bezier.UniqueId, ownerIndex);

        parameters["LineWidth"] = bezier.LineWidth.ToString();
        AddNonZero(parameters, "Color", bezier.Color);
        AddNonZero(parameters, "AreaColor", bezier.AreaColor);
        parameters["LocationCount"] = bezier.ControlPoints.Count.ToString();

        for (var i = 0; i < bezier.ControlPoints.Count; i++)
        {
            var cp = bezier.ControlPoints[i];
            parameters[$"X{i + 1}"] = CoordToSchematicUnits(cp.X);
            parameters[$"Y{i + 1}"] = CoordToSchematicUnits(cp.Y);
        }
        AddUniqueId(parameters, bezier.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteEllipseRecord(BinaryFormatWriter writer, SchEllipse ellipse, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "8",
        };

        AddCommonProperties(parameters, ellipse.OwnerIndex, ellipse.IsNotAccessible, ellipse.IndexInSheet,
            ellipse.OwnerPartId, ellipse.OwnerPartDisplayMode, ellipse.GraphicallyLocked,
            ellipse.Disabled, ellipse.Dimmed, ellipse.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", ellipse.Center.X);
        AddCoordParam(parameters, "Location.Y", ellipse.Center.Y);
        AddCoordParam(parameters, "Radius", ellipse.RadiusX);
        AddCoordParam(parameters, "SecondaryRadius", ellipse.RadiusY);
        parameters["LineWidth"] = ellipse.LineWidth.ToString();
        AddNonZero(parameters, "Color", ellipse.Color);
        parameters["AreaColor"] = ellipse.FillColor.ToString();
        AddBool(parameters, "IsSolid", ellipse.IsFilled);
        AddBool(parameters, "Transparent", ellipse.IsTransparent);
        AddUniqueId(parameters, ellipse.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteRoundedRectangleRecord(BinaryFormatWriter writer, SchRoundedRectangle roundedRect, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "10",
        };

        AddCommonProperties(parameters, roundedRect.OwnerIndex, roundedRect.IsNotAccessible, roundedRect.IndexInSheet,
            roundedRect.OwnerPartId, roundedRect.OwnerPartDisplayMode, roundedRect.GraphicallyLocked,
            roundedRect.Disabled, roundedRect.Dimmed, roundedRect.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", roundedRect.Corner1.X);
        AddCoordParam(parameters, "Location.Y", roundedRect.Corner1.Y);
        AddCoordParam(parameters, "Corner.X", roundedRect.Corner2.X);
        AddCoordParam(parameters, "Corner.Y", roundedRect.Corner2.Y);
        AddCoordParam(parameters, "CornerXRadius", roundedRect.CornerRadiusX);
        AddCoordParam(parameters, "CornerYRadius", roundedRect.CornerRadiusY);
        parameters["LineWidth"] = roundedRect.LineWidth.ToString();
        AddNonZero(parameters, "LineStyle", roundedRect.LineStyle);
        AddNonZero(parameters, "Color", roundedRect.Color);
        parameters["AreaColor"] = roundedRect.FillColor.ToString();
        AddBool(parameters, "IsSolid", roundedRect.IsFilled);
        AddBool(parameters, "Transparent", roundedRect.IsTransparent);
        AddUniqueId(parameters, roundedRect.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WritePieRecord(BinaryFormatWriter writer, SchPie pie, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "9",
        };

        AddCommonProperties(parameters, pie.OwnerIndex, pie.IsNotAccessible, pie.IndexInSheet,
            pie.OwnerPartId, pie.OwnerPartDisplayMode, pie.GraphicallyLocked,
            pie.Disabled, pie.Dimmed, pie.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", pie.Center.X);
        AddCoordParam(parameters, "Location.Y", pie.Center.Y);
        AddCoordParam(parameters, "Radius", pie.Radius);
        parameters["LineWidth"] = pie.LineWidth.ToString();
        if (pie.StartAngle != 0)
            parameters["StartAngle"] = FormatAngle(pie.StartAngle);
        parameters["EndAngle"] = FormatAngle(pie.EndAngle);
        AddNonZero(parameters, "Color", pie.Color);
        parameters["AreaColor"] = pie.FillColor.ToString();
        AddBool(parameters, "IsSolid", pie.IsFilled);
        AddBool(parameters, "Transparent", pie.IsTransparent);
        AddUniqueId(parameters, pie.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteEllipticalArcRecord(BinaryFormatWriter writer, SchEllipticalArc ellipticalArc, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "11",
        };

        AddCommonProperties(parameters, ellipticalArc.OwnerIndex, ellipticalArc.IsNotAccessible, ellipticalArc.IndexInSheet,
            ellipticalArc.OwnerPartId, ellipticalArc.OwnerPartDisplayMode, ellipticalArc.GraphicallyLocked,
            ellipticalArc.Disabled, ellipticalArc.Dimmed, ellipticalArc.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", ellipticalArc.Center.X);
        AddCoordParam(parameters, "Location.Y", ellipticalArc.Center.Y);
        AddCoordParam(parameters, "Radius", ellipticalArc.PrimaryRadius);
        AddCoordParam(parameters, "SecondaryRadius", ellipticalArc.SecondaryRadius);
        AddCoordParam(parameters, "LineWidth", ellipticalArc.LineWidth);
        if (ellipticalArc.StartAngle != 0)
            parameters["StartAngle"] = FormatAngle(ellipticalArc.StartAngle);
        parameters["EndAngle"] = FormatAngle(ellipticalArc.EndAngle);
        AddNonZero(parameters, "Color", ellipticalArc.Color);
        AddNonZero(parameters, "AreaColor", ellipticalArc.AreaColor);
        AddUniqueId(parameters, ellipticalArc.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteParameterRecord(BinaryFormatWriter writer, SchParameter param, ref int index, int ownerIndex = -1)
    {
        // Altium uses RECORD=34 for Designator only, RECORD=41 for all other parameters including Comment
        var recordType = param.Name == "Designator" ? "34" : "41";
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = recordType,
        };

        AddCommonProperties(parameters, param.OwnerIndex, param.IsNotAccessible, param.IndexInSheet,
            param.OwnerPartId, param.OwnerPartDisplayMode, param.GraphicallyLocked,
            param.Disabled, param.Dimmed, param.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", param.Location.X);
        AddCoordParam(parameters, "Location.Y", param.Location.Y);
        AddNonZero(parameters, "Color", param.Color);
        parameters["FontID"] = param.FontId.ToString();
        parameters["Text"] = param.Value;
        parameters["Name"] = param.Name;
        if (param.IsReadOnly) parameters["ReadOnlyState"] = "1";
        AddNonZero(parameters, "ParamType", param.ParamType);
        AddNonZero(parameters, "Orientation", param.Orientation);
        AddNonZero(parameters, "Justification", (int)param.Justification);
        AddBool(parameters, "ShowName", param.ShowName);
        AddBool(parameters, "IsMirrored", param.IsMirrored);
        if (!param.IsVisible) parameters["IsHidden"] = "T";
        AddNonZero(parameters, "AreaColor", param.AreaColor);
        AddNonZero(parameters, "AutoPosition", param.AutoPosition);
        AddBool(parameters, "IsConfigurable", param.IsConfigurable);
        AddBool(parameters, "IsRule", param.IsRule);
        AddBool(parameters, "IsSystemParameter", param.IsSystemParameter);
        AddNonZero(parameters, "TextHorzAnchor", param.TextHorzAnchor);
        AddNonZero(parameters, "TextVertAnchor", param.TextVertAnchor);
        AddBool(parameters, "HideName", param.HideName);
        AddBool(parameters, "AllowDatabaseSynchronize", param.AllowDatabaseSynchronize);
        AddBool(parameters, "AllowLibrarySynchronize", param.AllowLibrarySynchronize);
        AddBool(parameters, "NameIsReadOnly", param.NameIsReadOnly);
        if (!string.IsNullOrEmpty(param.PhysicalDesignator))
            parameters["PhysicalDesignator"] = param.PhysicalDesignator;
        AddBool(parameters, "ValueIsReadOnly", param.ValueIsReadOnly);
        if (!string.IsNullOrEmpty(param.VariantOption))
            parameters["VariantOption"] = param.VariantOption;
        if (!string.IsNullOrEmpty(param.Description))
            parameters["Description"] = param.Description;
        AddUniqueId(parameters, param.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteWireRecord(BinaryFormatWriter writer, SchWire wire, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "27",
            ["LOCATIONCOUNT"] = wire.Vertices.Count.ToString()
        };

        AddCommonProperties(parameters, wire.OwnerIndex, wire.IsNotAccessible, wire.IndexInSheet,
            wire.OwnerPartId, wire.OwnerPartDisplayMode, wire.GraphicallyLocked,
            wire.Disabled, wire.Dimmed, wire.UniqueId, ownerIndex);

        for (var i = 0; i < wire.Vertices.Count; i++)
        {
            parameters[$"X{i + 1}"] = CoordToSchematicUnits(wire.Vertices[i].X);
            parameters[$"Y{i + 1}"] = CoordToSchematicUnits(wire.Vertices[i].Y);
        }

        AddNonZero(parameters, "Color", wire.Color);
        AddNonZero(parameters, "LineWidth", wire.LineWidth);
        AddNonZero(parameters, "LineStyle", (int)wire.LineStyle);
        AddNonZero(parameters, "AreaColor", wire.AreaColor);
        AddBool(parameters, "IsSolid", wire.IsSolid);
        AddBool(parameters, "Transparent", wire.IsTransparent);
        AddBool(parameters, "AutoWire", wire.AutoWire);
        AddNonZero(parameters, "UnderlineColor", wire.UnderlineColor);
        AddUniqueId(parameters, wire.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteNetLabelRecord(BinaryFormatWriter writer, SchNetLabel netLabel, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "25",
        };

        AddCommonProperties(parameters, netLabel.OwnerIndex, netLabel.IsNotAccessible, netLabel.IndexInSheet,
            netLabel.OwnerPartId, netLabel.OwnerPartDisplayMode, netLabel.GraphicallyLocked,
            netLabel.Disabled, netLabel.Dimmed, netLabel.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", netLabel.Location.X);
        AddCoordParam(parameters, "Location.Y", netLabel.Location.Y);
        AddNonZero(parameters, "Color", netLabel.Color);
        parameters["FontID"] = netLabel.FontId.ToString();
        parameters["Text"] = netLabel.Text;
        AddNonZero(parameters, "Orientation", netLabel.Orientation);
        AddNonZero(parameters, "Justification", (int)netLabel.Justification);
        AddBool(parameters, "IsMirrored", netLabel.IsMirrored);
        AddNonZero(parameters, "AreaColor", netLabel.AreaColor);
        AddUniqueId(parameters, netLabel.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteJunctionRecord(BinaryFormatWriter writer, SchJunction junction, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "29",
        };

        AddCommonProperties(parameters, junction.OwnerIndex, junction.IsNotAccessible, junction.IndexInSheet,
            junction.OwnerPartId, junction.OwnerPartDisplayMode, junction.GraphicallyLocked,
            junction.Disabled, junction.Dimmed, junction.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", junction.Location.X);
        AddCoordParam(parameters, "Location.Y", junction.Location.Y);
        AddNonZero(parameters, "Color", junction.Color);
        AddBool(parameters, "Locked", junction.Locked);
        var sizeRaw = junction.Size.ToRaw();
        var sizeDxp = (int)(sizeRaw / 100_000);
        AddNonZero(parameters, "Size", sizeDxp);
        AddUniqueId(parameters, junction.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteTextFrameRecord(BinaryFormatWriter writer, SchTextFrame textFrame, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "28",
        };

        AddCommonProperties(parameters, textFrame.OwnerIndex, textFrame.IsNotAccessible, textFrame.IndexInSheet,
            textFrame.OwnerPartId, textFrame.OwnerPartDisplayMode, textFrame.GraphicallyLocked,
            textFrame.Disabled, textFrame.Dimmed, textFrame.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", textFrame.Corner1.X);
        AddCoordParam(parameters, "Location.Y", textFrame.Corner1.Y);
        AddCoordParam(parameters, "Corner.X", textFrame.Corner2.X);
        AddCoordParam(parameters, "Corner.Y", textFrame.Corner2.Y);
        AddNonZero(parameters, "Color", textFrame.BorderColor);
        parameters["AreaColor"] = textFrame.FillColor.ToString();
        AddNonZero(parameters, "TextColor", textFrame.TextColor);
        AddNonZero(parameters, "TextMargin", textFrame.TextMargin);
        parameters["FontID"] = textFrame.FontId.ToString();
        parameters["LineWidth"] = textFrame.LineWidth.ToString();
        AddNonZero(parameters, "LineStyle", textFrame.LineStyle);
        AddBool(parameters, "Transparent", textFrame.IsTransparent);
        parameters["Text"] = textFrame.Text;
        AddNonZero(parameters, "Orientation", textFrame.Orientation);
        AddNonZero(parameters, "Alignment", (int)textFrame.Alignment);
        AddBool(parameters, "ShowBorder", textFrame.ShowBorder);
        AddBool(parameters, "WordWrap", textFrame.WordWrap);
        AddBool(parameters, "ClipToRect", textFrame.ClipToRect);
        AddBool(parameters, "IsSolid", textFrame.IsFilled);
        AddUniqueId(parameters, textFrame.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteImageRecord(BinaryFormatWriter writer, SchImage image, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "30",
        };

        AddCommonProperties(parameters, image.OwnerIndex, image.IsNotAccessible, image.IndexInSheet,
            image.OwnerPartId, image.OwnerPartDisplayMode, image.GraphicallyLocked,
            image.Disabled, image.Dimmed, image.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", image.Corner1.X);
        AddCoordParam(parameters, "Location.Y", image.Corner1.Y);
        AddCoordParam(parameters, "Corner.X", image.Corner2.X);
        AddCoordParam(parameters, "Corner.Y", image.Corner2.Y);
        AddNonZero(parameters, "Color", image.BorderColor);
        parameters["LineWidth"] = image.LineWidth.ToString();
        AddBool(parameters, "KeepAspect", image.KeepAspect);
        AddBool(parameters, "EmbedImage", image.EmbedImage);
        if (!string.IsNullOrEmpty(image.Filename))
            parameters["Filename"] = image.Filename;
        AddNonZero(parameters, "AreaColor", image.AreaColor);
        AddBool(parameters, "IsSolid", image.IsSolid);
        AddNonZero(parameters, "LineStyle", image.LineStyle);
        AddBool(parameters, "Transparent", image.IsTransparent);
        AddBool(parameters, "ShowBorder", image.ShowBorder);
        AddUniqueId(parameters, image.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteSymbolRecord(BinaryFormatWriter writer, SchSymbol symbol, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "3",
        };

        AddCommonProperties(parameters, symbol.OwnerIndex, symbol.IsNotAccessible, symbol.IndexInSheet,
            symbol.OwnerPartId, symbol.OwnerPartDisplayMode, symbol.GraphicallyLocked,
            symbol.Disabled, symbol.Dimmed, symbol.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", symbol.Location.X);
        AddCoordParam(parameters, "Location.Y", symbol.Location.Y);
        AddNonZero(parameters, "Color", symbol.Color);
        parameters["Symbol"] = symbol.SymbolType.ToString();
        AddBool(parameters, "IsMirrored", symbol.IsMirrored);
        AddNonZero(parameters, "Orientation", symbol.Orientation);
        parameters["LineWidth"] = symbol.LineWidth.ToString();
        AddNonZero(parameters, "ScaleFactor", symbol.ScaleFactor);
        AddUniqueId(parameters, symbol.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WritePowerObjectRecord(BinaryFormatWriter writer, SchPowerObject powerObj, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "17",
        };

        AddCommonProperties(parameters, powerObj.OwnerIndex, powerObj.IsNotAccessible, powerObj.IndexInSheet,
            powerObj.OwnerPartId, powerObj.OwnerPartDisplayMode, powerObj.GraphicallyLocked,
            powerObj.Disabled, powerObj.Dimmed, powerObj.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", powerObj.Location.X);
        AddCoordParam(parameters, "Location.Y", powerObj.Location.Y);
        AddNonZero(parameters, "Color", powerObj.Color);
        parameters["Text"] = powerObj.Text;
        parameters["Style"] = ((int)powerObj.Style).ToString();
        parameters["Orientation"] = ((int)(powerObj.Rotation / 90.0)).ToString();
        parameters["ShowNetName"] = powerObj.ShowNetName ? "T" : "F";
        parameters["IsCrossSheetConnector"] = powerObj.IsCrossSheetConnector ? "T" : "F";
        parameters["FontID"] = powerObj.FontId.ToString();
        AddNonZero(parameters, "AreaColor", powerObj.AreaColor);
        AddBool(parameters, "IsCustomStyle", powerObj.IsCustomStyle);
        AddBool(parameters, "IsMirrored", powerObj.IsMirrored);
        AddNonZero(parameters, "Justification", powerObj.Justification);
        AddUniqueId(parameters, powerObj.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteNoErcRecord(BinaryFormatWriter writer, SchNoErc noErc, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "22",
        };

        AddCommonProperties(parameters, noErc.OwnerIndex, noErc.IsNotAccessible, noErc.IndexInSheet,
            noErc.OwnerPartId, noErc.OwnerPartDisplayMode, noErc.GraphicallyLocked,
            noErc.Disabled, noErc.Dimmed, noErc.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", noErc.Location.X);
        AddCoordParam(parameters, "Location.Y", noErc.Location.Y);
        AddNonZero(parameters, "Orientation", noErc.Orientation);
        AddNonZero(parameters, "Color", noErc.Color);
        if (noErc.IsActive) parameters["IsActive"] = "T";
        AddNonZero(parameters, "Symbol", noErc.Symbol);
        AddNonZero(parameters, "AreaColor", noErc.AreaColor);
        AddUniqueId(parameters, noErc.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteBusRecord(BinaryFormatWriter writer, SchBus bus, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "26",
        };

        AddCommonProperties(parameters, bus.OwnerIndex, bus.IsNotAccessible, bus.IndexInSheet,
            bus.OwnerPartId, bus.OwnerPartDisplayMode, bus.GraphicallyLocked,
            bus.Disabled, bus.Dimmed, bus.UniqueId, ownerIndex);

        AddNonZero(parameters, "LineWidth", bus.LineWidth);
        AddNonZero(parameters, "LineStyle", bus.LineStyle);
        AddNonZero(parameters, "Color", bus.Color);
        AddNonZero(parameters, "AreaColor", bus.AreaColor);
        parameters["LocationCount"] = bus.Vertices.Count.ToString();
        for (var i = 0; i < bus.Vertices.Count; i++)
        {
            parameters[$"X{i + 1}"] = CoordToSchematicUnits(bus.Vertices[i].X);
            parameters[$"Y{i + 1}"] = CoordToSchematicUnits(bus.Vertices[i].Y);
        }
        AddUniqueId(parameters, bus.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteBusEntryRecord(BinaryFormatWriter writer, SchBusEntry busEntry, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "37",
        };

        AddCommonProperties(parameters, busEntry.OwnerIndex, busEntry.IsNotAccessible, busEntry.IndexInSheet,
            busEntry.OwnerPartId, busEntry.OwnerPartDisplayMode, busEntry.GraphicallyLocked,
            busEntry.Disabled, busEntry.Dimmed, busEntry.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", busEntry.Location.X);
        AddCoordParam(parameters, "Location.Y", busEntry.Location.Y);
        AddCoordParam(parameters, "Corner.X", busEntry.Corner.X);
        AddCoordParam(parameters, "Corner.Y", busEntry.Corner.Y);
        AddNonZero(parameters, "LineWidth", busEntry.LineWidth);
        AddNonZero(parameters, "Color", busEntry.Color);
        AddUniqueId(parameters, busEntry.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WritePortRecord(BinaryFormatWriter writer, SchPort port, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "18",
        };

        AddCommonProperties(parameters, port.OwnerIndex, port.IsNotAccessible, port.IndexInSheet,
            port.OwnerPartId, port.OwnerPartDisplayMode, port.GraphicallyLocked,
            port.Disabled, port.Dimmed, port.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", port.Location.X);
        AddCoordParam(parameters, "Location.Y", port.Location.Y);
        parameters["Name"] = port.Name;
        AddNonZero(parameters, "IOType", port.IoType);
        AddNonZero(parameters, "Style", port.Style);
        AddNonZero(parameters, "Alignment", port.Alignment);
        AddCoordParam(parameters, "Width", port.Width);
        AddCoordParam(parameters, "Height", port.Height);
        AddNonZero(parameters, "BorderWidth", port.BorderWidth);
        AddBool(parameters, "AutoSize", port.AutoSize);
        AddNonZero(parameters, "ConnectedEnd", port.ConnectedEnd);
        if (!string.IsNullOrEmpty(port.CrossReference))
            parameters["CrossReference"] = port.CrossReference;
        AddBool(parameters, "ShowNetName", port.ShowNetName);
        if (!string.IsNullOrEmpty(port.HarnessType))
            parameters["HarnessType"] = port.HarnessType;
        AddNonZero(parameters, "HarnessColor", port.HarnessColor);
        AddBool(parameters, "IsCustomStyle", port.IsCustomStyle);
        AddNonZero(parameters, "FontID", port.FontId);
        AddNonZero(parameters, "Color", port.Color);
        AddNonZero(parameters, "AreaColor", port.AreaColor);
        AddNonZero(parameters, "TextColor", port.TextColor);
        AddUniqueId(parameters, port.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteSheetSymbolRecord(BinaryFormatWriter writer, SchSheetSymbol sheetSymbol, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "15",
        };

        AddCommonProperties(parameters, sheetSymbol.OwnerIndex, sheetSymbol.IsNotAccessible, sheetSymbol.IndexInSheet,
            sheetSymbol.OwnerPartId, sheetSymbol.OwnerPartDisplayMode, sheetSymbol.GraphicallyLocked,
            sheetSymbol.Disabled, sheetSymbol.Dimmed, sheetSymbol.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", sheetSymbol.Location.X);
        AddCoordParam(parameters, "Location.Y", sheetSymbol.Location.Y);
        AddCoordParam(parameters, "XSize", sheetSymbol.XSize);
        AddCoordParam(parameters, "YSize", sheetSymbol.YSize);
        AddBool(parameters, "IsMirrored", sheetSymbol.IsMirrored);
        if (!string.IsNullOrEmpty(sheetSymbol.FileName))
            parameters["FileName"] = sheetSymbol.FileName;
        if (!string.IsNullOrEmpty(sheetSymbol.SheetName))
            parameters["SheetName"] = sheetSymbol.SheetName;
        parameters["LineWidth"] = sheetSymbol.LineWidth.ToString();
        AddNonZero(parameters, "Color", sheetSymbol.Color);
        AddNonZero(parameters, "AreaColor", sheetSymbol.AreaColor);
        AddBool(parameters, "IsSolid", sheetSymbol.IsSolid);
        AddBool(parameters, "ShowHiddenFields", sheetSymbol.ShowHiddenFields);
        AddNonZero(parameters, "SymbolType", sheetSymbol.SymbolType);
        if (!string.IsNullOrEmpty(sheetSymbol.DesignItemId))
            parameters["DesignItemId"] = sheetSymbol.DesignItemId;
        if (!string.IsNullOrEmpty(sheetSymbol.ItemGuid))
            parameters["ItemGUID"] = sheetSymbol.ItemGuid;
        AddNonZero(parameters, "LibIdentifierKind", sheetSymbol.LibIdentifierKind);
        if (!string.IsNullOrEmpty(sheetSymbol.LibraryIdentifier))
            parameters["LibraryIdentifier"] = sheetSymbol.LibraryIdentifier;
        if (!string.IsNullOrEmpty(sheetSymbol.RevisionGuid))
            parameters["RevisionGUID"] = sheetSymbol.RevisionGuid;
        if (!string.IsNullOrEmpty(sheetSymbol.SourceLibraryName))
            parameters["SOURCELIBNAME"] = sheetSymbol.SourceLibraryName;
        if (!string.IsNullOrEmpty(sheetSymbol.VaultGuid))
            parameters["VaultGUID"] = sheetSymbol.VaultGuid;
        AddUniqueId(parameters, sheetSymbol.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteSheetEntryRecord(BinaryFormatWriter writer, SchSheetEntry entry, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "16",
        };

        AddCommonProperties(parameters, entry.OwnerIndex, entry.IsNotAccessible, entry.IndexInSheet,
            entry.OwnerPartId, entry.OwnerPartDisplayMode, entry.GraphicallyLocked,
            entry.Disabled, entry.Dimmed, entry.UniqueId, ownerIndex);

        AddNonZero(parameters, "Side", entry.Side);
        AddCoordParam(parameters, "DistanceFromTop", entry.DistanceFromTop);
        parameters["Name"] = entry.Name;
        AddNonZero(parameters, "IOType", entry.IoType);
        AddNonZero(parameters, "Style", entry.Style);
        AddNonZero(parameters, "ArrowKind", entry.ArrowKind);
        if (!string.IsNullOrEmpty(entry.HarnessType))
            parameters["HarnessType"] = entry.HarnessType;
        AddNonZero(parameters, "HarnessColor", entry.HarnessColor);
        AddNonZero(parameters, "FontID", entry.FontId);
        AddNonZero(parameters, "Color", entry.Color);
        AddNonZero(parameters, "AreaColor", entry.AreaColor);
        AddNonZero(parameters, "TextColor", entry.TextColor);
        AddNonZero(parameters, "TextStyle", entry.TextStyle);
        AddUniqueId(parameters, entry.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteParameterSetRecord(BinaryFormatWriter writer, SchParameterSet paramSet, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "43",
        };

        AddCommonProperties(parameters, paramSet.OwnerIndex, paramSet.IsNotAccessible, paramSet.IndexInSheet,
            paramSet.OwnerPartId, paramSet.OwnerPartDisplayMode, paramSet.GraphicallyLocked,
            paramSet.Disabled, paramSet.Dimmed, paramSet.UniqueId, ownerIndex);

        AddCoordParam(parameters, "Location.X", paramSet.Location.X);
        AddCoordParam(parameters, "Location.Y", paramSet.Location.Y);
        AddNonZero(parameters, "Orientation", paramSet.Orientation);
        AddNonZero(parameters, "Style", paramSet.Style);
        AddNonZero(parameters, "Color", paramSet.Color);
        AddNonZero(parameters, "AreaColor", paramSet.AreaColor);
        if (!string.IsNullOrEmpty(paramSet.Name))
            parameters["Name"] = paramSet.Name;
        AddBool(parameters, "ShowHiddenFields", paramSet.ShowHiddenFields);
        AddNonZero(parameters, "BorderWidth", paramSet.BorderWidth);
        AddBool(parameters, "IsSolid", paramSet.IsSolid);
        AddUniqueId(parameters, paramSet.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteBlanketRecord(BinaryFormatWriter writer, SchBlanket blanket, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "225",
        };

        AddCommonProperties(parameters, blanket.OwnerIndex, blanket.IsNotAccessible, blanket.IndexInSheet,
            blanket.OwnerPartId, blanket.OwnerPartDisplayMode, blanket.GraphicallyLocked,
            blanket.Disabled, blanket.Dimmed, blanket.UniqueId, ownerIndex);

        AddBool(parameters, "IsCollapsed", blanket.IsCollapsed);
        AddNonZero(parameters, "LineWidth", blanket.LineWidth);
        AddNonZero(parameters, "LineStyle", blanket.LineStyle);
        AddBool(parameters, "IsSolid", blanket.IsSolid);
        AddBool(parameters, "Transparent", blanket.IsTransparent);
        AddNonZero(parameters, "Color", blanket.Color);
        AddNonZero(parameters, "AreaColor", blanket.AreaColor);
        parameters["LocationCount"] = blanket.Vertices.Count.ToString();
        for (var i = 0; i < blanket.Vertices.Count; i++)
        {
            parameters[$"X{i + 1}"] = CoordToSchematicUnits(blanket.Vertices[i].X);
            parameters[$"Y{i + 1}"] = CoordToSchematicUnits(blanket.Vertices[i].Y);
        }
        AddUniqueId(parameters, blanket.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteImplementationRecords(BinaryFormatWriter writer, SchComponent component, ref int index)
    {
        if (component.Implementations.Count == 0)
        {
            // Write empty implementation list container (record 44) — Altium always writes this
            var parameters = new Dictionary<string, string> { ["RECORD"] = "44" };
            writer.WriteCStringParameterBlock(parameters);
            index++;
            return;
        }

        // Record 44: ImplementationList container
        var implListParams = new Dictionary<string, string> { ["RECORD"] = "44" };
        writer.WriteCStringParameterBlock(implListParams);
        index++;

        foreach (var impl in component.Implementations.Cast<SchImplementation>())
        {
            // Record 45: Implementation
            WriteImplementationRecord(writer, impl, ref index);

            if (impl.MapDefiners.Count > 0)
            {
                // Record 46: MapDefinerList container
                var mdlParams = new Dictionary<string, string> { ["RECORD"] = "46" };
                writer.WriteCStringParameterBlock(mdlParams);
                index++;

                foreach (var mapDefiner in impl.MapDefiners.Cast<SchMapDefiner>())
                {
                    // Record 47: MapDefiner
                    WriteMapDefinerRecord(writer, mapDefiner, ref index);
                }
            }

            // Record 48: ImplementationParameters (empty container, Altium always writes it)
            var ipParams = new Dictionary<string, string> { ["RECORD"] = "48" };
            writer.WriteCStringParameterBlock(ipParams);
            index++;
        }
    }

    internal static void WriteImplementationRecord(BinaryFormatWriter writer, SchImplementation impl, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "45"
        };

        AddCommonProperties(parameters, impl.OwnerIndex, impl.IsNotAccessible, impl.IndexInSheet,
            impl.OwnerPartId, impl.OwnerPartDisplayMode, impl.GraphicallyLocked,
            impl.Disabled, impl.Dimmed, impl.UniqueId, ownerIndex);

        if (!string.IsNullOrEmpty(impl.Description))
            parameters["DESCRIPTION"] = impl.Description;
        if (!string.IsNullOrEmpty(impl.ModelName))
            parameters["MODELNAME"] = impl.ModelName;
        if (!string.IsNullOrEmpty(impl.ModelType))
            parameters["MODELTYPE"] = impl.ModelType;

        parameters["DATAFILECOUNT"] = impl.DataFileKinds.Count.ToString();
        for (var i = 0; i < impl.DataFileKinds.Count; i++)
        {
            parameters[$"MODELDATAFILEKIND{i + 1}"] = impl.DataFileKinds[i];
        }

        if (impl.IsCurrent)
            parameters["ISCURRENT"] = "T";
        AddUniqueId(parameters, impl.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WriteMapDefinerRecord(BinaryFormatWriter writer, SchMapDefiner mapDefiner, ref int index, int ownerIndex = -1)
    {
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "47"
        };

        AddCommonProperties(parameters, mapDefiner.OwnerIndex, mapDefiner.IsNotAccessible, mapDefiner.IndexInSheet,
            mapDefiner.OwnerPartId, mapDefiner.OwnerPartDisplayMode, mapDefiner.GraphicallyLocked,
            mapDefiner.Disabled, mapDefiner.Dimmed, mapDefiner.UniqueId, ownerIndex);

        if (!string.IsNullOrEmpty(mapDefiner.DesignatorInterface))
            parameters["DESINTF"] = mapDefiner.DesignatorInterface;

        parameters["DESIMPCOUNT"] = mapDefiner.DesignatorImplementations.Count.ToString();
        for (var i = 0; i < mapDefiner.DesignatorImplementations.Count; i++)
        {
            parameters[$"DESIMP{i}"] = mapDefiner.DesignatorImplementations[i];
        }

        if (mapDefiner.IsTrivial)
            parameters["ISTRIVIAL"] = "T";
        AddUniqueId(parameters, mapDefiner.UniqueId);

        writer.WriteCStringParameterBlock(parameters);
        index++;
    }

    internal static void WritePinFrac(CFStorage storage, Dictionary<int, (int x, int y, int length)> data)
    {
        if (data.Count == 0) return;

        var pinFracStream = storage.AddStream("PinFrac");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Write header - Altium uses mixed case keys
        var headerParams = new Dictionary<string, string>
        {
            ["HEADER"] = "PinFrac",
            ["Weight"] = data.Count.ToString()
        };
        writer.WriteCStringParameterBlock(headerParams);

        // Write each pin's fractional data
        foreach (var kvp in data)
        {
            WriteCompressedStorage(writer, kvp.Key.ToString(), w =>
            {
                w.Write(kvp.Value.x);
                w.Write(kvp.Value.y);
                w.Write(kvp.Value.length);
            });
        }

        writer.Flush();
        pinFracStream.SetData(ms.ToArray());
    }

    internal static void WritePinSymbolLineWidth(CFStorage storage, Dictionary<int, Dictionary<string, string>> data)
    {
        if (data.Count == 0) return;

        var lineWidthStream = storage.AddStream("PinSymbolLineWidth");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Write header (C-string format for section-level blocks)
        var headerParams = new Dictionary<string, string>
        {
            ["HEADER"] = "PinSymbolLineWidth",
            ["Weight"] = data.Count.ToString()
        };
        writer.WriteCStringParameterBlock(headerParams);

        // Write each pin's symbol line width
        foreach (var kvp in data)
        {
            WriteCompressedStorage(writer, kvp.Key.ToString(), w =>
            {
                w.WriteUnicodeParameterBlock(kvp.Value);
            });
        }

        writer.Flush();
        lineWidthStream.SetData(ms.ToArray());
    }

    private static void WriteCompressedStorage(BinaryFormatWriter writer, string name, Action<BinaryFormatWriter> writeContent)
    {
        // Altium compressed storage format:
        //   Int32: block size (of inner data)
        //   byte: 0xD0 tag
        //   byte: name length (Pascal string)
        //   bytes: name
        //   Int32: compressed data length
        //   bytes: compressed data

        // Prepare uncompressed content
        using var contentMs = new MemoryStream();
        using var contentWriter = new BinaryFormatWriter(contentMs, leaveOpen: true);
        writeContent(contentWriter);
        contentWriter.Flush();
        var uncompressed = contentMs.ToArray();

        // Compress
        using var compressedMs = new MemoryStream();
        using (var zlibStream = new ZLibStream(compressedMs, CompressionMode.Compress, leaveOpen: true))
        {
            zlibStream.Write(uncompressed, 0, uncompressed.Length);
        }
        var compressed = compressedMs.ToArray();

        // Calculate block size: tag(1) + nameLen(1) + name(N) + compressedSize(4) + compressed(N)
        var nameBytes = AltiumEncoding.Windows1252.GetBytes(name);
        var blockSize = 1 + 1 + nameBytes.Length + 4 + compressed.Length;

        // Write block
        writer.Write(blockSize);
        writer.Write((byte)0xD0);
        writer.Write((byte)nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write(compressed.Length);
        writer.Write(compressed);
    }

    private static void WriteStorage(CompoundFile cf, SchLibrary library)
    {
        // Collect all embedded images across all components
        var embeddedImages = new List<byte[]>();
        foreach (var component in library.Components)
        {
            foreach (var image in component.Images)
            {
                if (image is SchImage img && img.EmbedImage && img.ImageData != null)
                    embeddedImages.Add(img.ImageData);
            }
        }

        WriteStorageStream(cf.RootStorage, embeddedImages);
    }

    internal static void WriteStorageStream(CFStorage rootStorage, List<byte[]> embeddedImages)
    {
        var storageStream = rootStorage.AddStream("Storage");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        var parameters = new Dictionary<string, string>
        {
            ["HEADER"] = "Icon storage",
            ["WEIGHT"] = embeddedImages.Count.ToString()
        };
        writer.WriteCStringParameterBlock(parameters);

        // Write each embedded image as a compressed storage entry
        for (var i = 0; i < embeddedImages.Count; i++)
        {
            WriteCompressedStorage(writer, i.ToString(), w =>
            {
                w.Write(embeddedImages[i]);
            });
        }

        writer.Flush();
        storageStream.SetData(ms.ToArray());
    }

    internal static (int num, int frac) CoordToDxpFrac(Coord coord)
    {
        // DXP units: 1 DXP unit = 10 mils = 100,000 raw internal units
        // The 'num' is stored as Int16, the 'frac' captures sub-10-mil precision
        var mils = coord.ToMils();
        var num = (int)(mils / 10.0);
        var frac = (int)Math.Round((mils / 10.0 - Math.Truncate(mils / 10.0)) * 100_000);
        return (num, frac);
    }

    /// <summary>
    /// Converts a Coord to schematic units string (raw / 1000, i.e., 10 units per mil).
    /// Used for vertex coordinates in polygon, polyline, and bezier records
    /// where the reader's TryParseCoord() expects schematic units.
    /// </summary>
    internal static string CoordToSchematicUnits(Coord coord) =>
        (coord.ToRaw() / 1000).ToString();

    /// <summary>
    /// Adds a coordinate parameter in DXP units with optional _FRAC suffix.
    /// DXP units: 1 DXP = 10 mils = 100,000 raw internal units.
    /// The reader uses CoordFromDxp(dxpValue, frac) = Coord.FromRaw(dxpValue * 100_000 + frac).
    /// </summary>
    internal static void AddCoordParam(Dictionary<string, string> parameters, string name, Coord coord)
    {
        var raw = coord.ToRaw();
        var dxp = raw / 100_000;
        var frac = raw % 100_000;
        // Altium omits zero coordinate values
        if (dxp != 0)
            parameters[name] = dxp.ToString();
        if (frac != 0)
            parameters[name + "_Frac"] = frac.ToString();
    }

    /// <summary>
    /// Converts a Coord line width back to a LineWidth index (0=Small/1mil, 1=Medium/2mil, 2=Large/4mil).
    /// </summary>
    internal static int LineWidthToIndex(Coord width)
    {
        var mils = width.ToMils();
        if (mils >= 3.0) return 2; // Large (4 mil)
        if (mils >= 1.5) return 1; // Medium (2 mil)
        return 0; // Small (1 mil)
    }

    /// <summary>
    /// Formats an angle value the way Altium does (e.g., "90.000", "360.000").
    /// </summary>
    private static string FormatAngle(double angle)
    {
        // Altium writes angles with 3 decimal places using period separator
        return angle.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Adds common Sch primitive properties (ownership, state, unique ID) to a parameter dictionary.
    /// Uses Altium's exact mixed-case key convention. Only adds non-default values.
    /// </summary>
    private static void AddCommonProperties(Dictionary<string, string> parameters,
        int ownerIdx, bool isNotAccessible, int indexInSheet,
        int ownerPartId, int ownerPartDisplayMode,
        bool graphicallyLocked, bool disabled, bool dimmed,
        string? uniqueId, int ownerIndexOverride = -1)
    {
        // Note: Altium spells this "IsNotAccesible" (single 's') - match their typo
        if (isNotAccessible) parameters["IsNotAccesible"] = "T";

        if (ownerIndexOverride >= 0)
            parameters["OwnerIndex"] = ownerIndexOverride.ToString();
        // Note: when ownerIndexOverride is -1 (default), we intentionally do NOT write
        // the primitive's own OwnerIndex. All callers that need OWNERINDEX pass it explicitly.
        // Writing primitive.OwnerIndex would cause document-level primitives with leftover
        // values from reading to be re-parented on the next read (the SchDoc OWNERINDEX bug).

        AddNonZero(parameters, "IndexInSheet", indexInSheet);
        AddNonZero(parameters, "OwnerPartId", ownerPartId);
        AddNonZero(parameters, "OwnerPartDisplayMode", ownerPartDisplayMode);
        if (graphicallyLocked) parameters["GraphicallyLocked"] = "T";
        if (disabled) parameters["Disabled"] = "T";
        if (dimmed) parameters["Dimmed"] = "T";
        // Note: UniqueID is NOT added here - Altium puts it at the END of each record
    }

    private static void AddUniqueId(Dictionary<string, string> parameters, string? uniqueId)
    {
        if (!string.IsNullOrEmpty(uniqueId)) parameters["UniqueID"] = uniqueId;
    }

    /// <summary>
    /// Adds an integer parameter only if its value is non-zero.
    /// </summary>
    private static void AddNonZero(Dictionary<string, string> parameters, string key, int value)
    {
        if (value != 0) parameters[key] = value.ToString();
    }

    /// <summary>
    /// Adds a boolean parameter only if true.
    /// </summary>
    private static void AddBool(Dictionary<string, string> parameters, string key, bool value)
    {
        if (value) parameters[key] = "T";
    }

    private static string GetSectionKeyFromName(string name) =>
        WriterUtilities.GetSectionKeyFromName(name);
}
