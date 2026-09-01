using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Serialization.Compound;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using System.Globalization;
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
        await WriteAsync(library, stream, cancellationToken).ConfigureAwait(false);
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
        await ms.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a SchLib file to a stream synchronously.
    /// </summary>
    /// <param name="library">The schematic library to write.</param>
    /// <param name="stream">Destination stream.</param>
    /// <remarks>This instance is stateless and thread-safe.</remarks>
    public void Write(SchLibrary library, Stream stream, CancellationToken cancellationToken = default)
    {
        using var cf = CompoundFileAccessor.Create();
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

    private static void WriteFileHeader(CompoundFileAccessor cf, SchLibrary library)
    {
        var headerStream = cf.RootStorage.AddStream("FileHeader");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Re-emit the full header parameter list verbatim (font table, UniqueID, SheetStyle, MBCS
        // flags, etc.). Weight is PRESERVED — it is a record-count metric, not the component count.
        // Real Altium FileHeaders end at this param block; the reader enumerates components via the
        // CompCount/LibRefN parameters, so no component count+names tail is appended.
        if (library.HeaderParameters is { Count: > 0 } headerParams)
        {
            var sb = new System.Text.StringBuilder();
            var hasCompCount = false;
            foreach (var kvp in headerParams)
            {
                sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
                if (string.Equals(kvp.Key, "COMPCOUNT", StringComparison.OrdinalIgnoreCase))
                    hasCompCount = true;
            }
            writer.WriteCStringParameterBlockRaw(sb.ToString());

            // Real Altium headers enumerate components via CompCount/LibRefN params, so the reader
            // discovers them from the param block (no tail). But a library originally written from
            // scratch and reloaded has captured params that only carry HEADER + Weight — no CompCount.
            // Without a discovery mechanism the reader would find zero components, so append the same
            // binary count + name tail the from-scratch path uses. Headers that already carry
            // CompCount are left byte-for-byte unchanged.
            if (!hasCompCount)
            {
                writer.Write(library.Components.Count);
                foreach (var component in library.Components)
                    writer.WriteStringBlock(component.Name);
            }
        }
        else
        {
            // From-scratch libraries have no captured header params: synthesize a valid header with a
            // fresh UniqueID and a font table (so text referencing a FontID has a definition), then
            // append the component count + name string blocks the reader reads from the trailing data.
            var sb = new System.Text.StringBuilder();
            void Add(string key, string value) => sb.Append('|').Append(key).Append('=').Append(value);

            Add("HEADER", "Protel for Windows - Schematic Library Editor Binary File Version 5.0");
            Add("Weight", library.Components.Count.ToString(CultureInfo.InvariantCulture));
            Add("MinorVersion", "2");
            Add("UniqueID", GenerateUniqueId());
            AppendFontTable(sb, library.Fonts);
            Add("UseMBCS", "T");
            Add("IsBOC", "T");
            Add("SheetStyle", "9");
            Add("BorderOn", "T");
            Add("Display_Unit", "0");
            writer.WriteCStringParameterBlockRaw(sb.ToString());

            writer.Write(library.Components.Count);
            foreach (var component in library.Components)
                writer.WriteStringBlock(component.Name);
        }

        writer.Flush();
        headerStream.SetData(ms.ToArray());
    }

    private static void WriteSectionKeys(CompoundFileAccessor cf, SchLibrary library, Dictionary<string, string> sectionKeys)
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
            ["KeyCount"] = componentsNeedingKeys.Count.ToString(CultureInfo.InvariantCulture)
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

    /// <summary>
    /// Appends a FontID table (1-based FontName{i}/Size{i}, with Bold/Italic/Underline{i} only when
    /// true — required for parity with Altium) to a from-scratch FileHeader param block. Emits a
    /// single default Times New Roman entry when the library carries no fonts, so any text record that
    /// references FontID 1 still resolves to a definition.
    /// </summary>
    private static void AppendFontTable(System.Text.StringBuilder sb, IReadOnlyList<Models.Sch.SchFontDefinition> fonts)
    {
        void Add(string key, string value) => sb.Append('|').Append(key).Append('=').Append(value);

        if (fonts.Count == 0)
        {
            Add("FontIdCount", "1");
            Add("FontName1", "Times New Roman");
            Add("Size1", "10");
            return;
        }

        Add("FontIdCount", fonts.Count.ToString(CultureInfo.InvariantCulture));
        for (var i = 0; i < fonts.Count; i++)
        {
            var n = (i + 1).ToString(CultureInfo.InvariantCulture);
            var font = fonts[i];
            Add("FontName" + n, font.Name);
            Add("Size" + n, ((int)Math.Round(font.Size)).ToString(CultureInfo.InvariantCulture));
            if (font.Bold) Add("Bold" + n, "T");
            if (font.Italic) Add("Italic" + n, "T");
            if (font.Underline) Add("Underline" + n, "T");
        }
    }

    private static string GenerateUniqueId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return string.Create(8, Random.Shared, (span, rng) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = chars[rng.Next(chars.Length)];
        });
    }

    /// <summary>Rebuilds a record's pipe-delimited parameter string from its captured ordered list.</summary>
    private static string BuildOrderedParamString(List<KeyValuePair<string, string>> ordered)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in ordered)
            sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
        return sb.ToString();
    }

    private static void WriteComponent(CompoundFileAccessor cf, SchComponent component, Dictionary<string, string> sectionKeys)
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

        // Write the component record (the root primitive) from the typed model. The typed serialization
        // is byte-exact for the whole corpus, so no captured-parameter replay is needed.
        WriteComponentRecord(writer, component, ref index);

        // Dispatch a single primitive (or opaque record) to its record writer.
        void EmitPrimitive(object prim)
        {
            switch (prim)
            {
                case SchPin pin:
                    WritePinRecord(writer, pin, pinIndex, pinsFrac, pinsSymbolLineWidth);
                    pinIndex++; index++;
                    break;
                case SchLine line: WriteLineRecord(writer, line, ref index); break;
                case SchRectangle rect: WriteRectangleRecord(writer, rect, ref index); break;
                case SchLabel label: WriteLabelRecord(writer, label, ref index); break;
                case SchArc arc: WriteArcRecord(writer, arc, ref index); break;
                case SchPolygon polygon: WritePolygonRecord(writer, polygon, ref index); break;
                case SchPolyline polyline: WritePolylineRecord(writer, polyline, ref index); break;
                case SchWire wire: WriteWireRecord(writer, wire, ref index); break;
                case SchBezier bezier: WriteBezierRecord(writer, bezier, ref index); break;
                case SchEllipse ellipse: WriteEllipseRecord(writer, ellipse, ref index); break;
                case SchRoundedRectangle roundedRect: WriteRoundedRectangleRecord(writer, roundedRect, ref index); break;
                case SchPie pie: WritePieRecord(writer, pie, ref index); break;
                case SchEllipticalArc ellipticalArc: WriteEllipticalArcRecord(writer, ellipticalArc, ref index); break;
                case SchParameter param: WriteParameterRecord(writer, param, ref index); break;
                case SchNetLabel netLabel: WriteNetLabelRecord(writer, netLabel, ref index); break;
                case SchJunction junction: WriteJunctionRecord(writer, junction, ref index); break;
                case SchTextFrame textFrame: WriteTextFrameRecord(writer, textFrame, ref index); break;
                case SchImage image: WriteImageRecord(writer, image, ref index); break;
                case SchSymbol symbol: WriteSymbolRecord(writer, symbol, ref index); break;
                case SchPowerObject powerObj: WritePowerObjectRecord(writer, powerObj, ref index); break;
                case SchOpaqueRecord opaque:
                    if (opaque.OrderedParameters is { Count: > 0 } op)
                        writer.WriteCStringParameterBlockRaw(BuildOrderedParamString(op));
                    else
                        writer.WriteCStringParameterBlock(opaque.Parameters);
                    index++;
                    break;
            }
        }

        // Emit the modeled primitive collections grouped by type (the canonical from-scratch order).
        // include() lets the loaded path skip primitives already emitted from the captured order list.
        void EmitModeledPrimitives(Func<object, bool> include)
        {
            foreach (var pin in component.Pins) if (include(pin)) EmitPrimitive(pin);
            foreach (var line in component.Lines) if (include(line)) EmitPrimitive(line);
            foreach (var rect in component.Rectangles) if (include(rect)) EmitPrimitive(rect);
            foreach (var label in component.Labels) if (include(label)) EmitPrimitive(label);
            foreach (var arc in component.Arcs) if (include(arc)) EmitPrimitive(arc);
            foreach (var polygon in component.Polygons) if (include(polygon)) EmitPrimitive(polygon);
            foreach (var polyline in component.Polylines) if (include(polyline)) EmitPrimitive(polyline);
            foreach (var wire in component.Wires) if (include(wire)) EmitPrimitive(wire);
            foreach (var bezier in component.Beziers) if (include(bezier)) EmitPrimitive(bezier);
            foreach (var ellipse in component.Ellipses) if (include(ellipse)) EmitPrimitive(ellipse);
            foreach (var roundedRect in component.RoundedRectangles) if (include(roundedRect)) EmitPrimitive(roundedRect);
            foreach (var pie in component.Pies) if (include(pie)) EmitPrimitive(pie);
            foreach (var ellipticalArc in component.EllipticalArcs) if (include(ellipticalArc)) EmitPrimitive(ellipticalArc);
            foreach (var param in component.Parameters) if (include(param)) EmitPrimitive(param);
            foreach (var netLabel in component.NetLabels) if (include(netLabel)) EmitPrimitive(netLabel);
            foreach (var junction in component.Junctions) if (include(junction)) EmitPrimitive(junction);
            foreach (var textFrame in component.TextFrames) if (include(textFrame)) EmitPrimitive(textFrame);
            foreach (var image in component.Images) if (include(image)) EmitPrimitive(image);
            foreach (var symbol in component.Symbols) if (include(symbol)) EmitPrimitive(symbol);
            foreach (var powerObj in component.PowerObjects) if (include(powerObj)) EmitPrimitive(powerObj);
        }

        if (component.ReadOrderedPrimitives.Count > 0)
        {
            // Reproduce the exact on-disk record order captured on read, then emit any primitive that
            // was added to the modeled collections after load (and so is absent from the captured
            // order). Without the second pass, edits to a loaded component are silently dropped on
            // save; with it, an unmodified component re-emits byte-for-byte (nothing new to append).
            var written = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var prim in component.ReadOrderedPrimitives)
            {
                written.Add(prim);
                EmitPrimitive(prim);
            }
            EmitModeledPrimitives(written.Add);
        }
        else
        {
            // Components built from scratch: emit records grouped by type.
            EmitModeledPrimitives(static _ => true);
        }

        // Write implementation records (records 44-48) — Altium writes these at the end
        WriteImplementationRecords(writer, component, ref index);

        writer.Flush();
        dataStream2.SetData(ms.ToArray());

        // Write optional pin-related streams
        WritePinFrac(componentStorage, pinsFrac);
        WritePinSymbolLineWidth(componentStorage, pinsSymbolLineWidth);
    }

    internal static void WriteComponentRecord(BinaryFormatWriter writer, SchComponent component, ref int index, bool writeLocation = false)
    {
        // Parameter keys match Altium's exact mixed-case convention
        var parameters = new Dictionary<string, string>
        {
            ["RECORD"] = "1",
            ["LibReference"] = component.Name,
        };
        // Altium omits ComponentDescription entirely when a component has no description. Writing an
        // empty value perturbs the byte-faithful record and reads back as "" instead of null, so only
        // emit it (in its original position) when a description is actually present.
        if (component.Description != null)
            parameters["ComponentDescription"] = component.Description;
        parameters["PartCount"] = (component.PartCount + 1).ToString(CultureInfo.InvariantCulture);
        parameters["DisplayModeCount"] = "1";
        parameters["IndexInSheet"] = "-1";
        parameters["OwnerPartId"] = "-1";
        parameters["CurrentPartId"] = "1";
        parameters["LibraryPath"] = "*";
        parameters["SourceLibraryName"] = "*";
        parameters["SheetPartFileName"] = "*";
        parameters["TargetFileName"] = "*";

        if (component is SchComponent schComp)
        {
            if (!string.IsNullOrEmpty(schComp.UniqueId))
                parameters["UniqueID"] = schComp.UniqueId;

            if (schComp.DisplayModeCount > 1)
                parameters["DisplayModeCount"] = schComp.DisplayModeCount.ToString(CultureInfo.InvariantCulture);
            AddNonZero(parameters, "DisplayMode", schComp.DisplayMode);
            AddNonZero(parameters, "Orientation", schComp.Orientation);
            if (schComp.CurrentPartId > 1)
                parameters["CurrentPartId"] = schComp.CurrentPartId.ToString(CultureInfo.InvariantCulture);
            AddBool(parameters, "ShowHiddenPins", schComp.ShowHiddenPins);
            AddBool(parameters, "ShowHiddenFields", schComp.ShowHiddenFields);
            if (!string.IsNullOrEmpty(schComp.LibraryPath) && schComp.LibraryPath != "*")
                parameters["LibraryPath"] = schComp.LibraryPath;
            if (!string.IsNullOrEmpty(schComp.SourceLibraryName) && schComp.SourceLibraryName != "*")
                parameters["SourceLibraryName"] = schComp.SourceLibraryName;
            if (!string.IsNullOrEmpty(schComp.LibReference))
                parameters["LibReference"] = schComp.LibReference;
            // Note: DesignatorPrefix is NOT written to RECORD=1 (Altium derives it from the child Designator parameter text)
            AddNonZero(parameters, "ComponentKind", schComp.ComponentKind);
            AddBool(parameters, "OverideColors", schComp.OverrideColors);
            // Altium writes AreaColor before Color
            AddNonZero(parameters, "AreaColor", schComp.AreaColor);
            AddNonZero(parameters, "Color", schComp.Color);
            AddBool(parameters, "DesignatorLocked", schComp.DesignatorLocked);
            AddBool(parameters, "PartIDLocked", schComp.PartIdLocked);
            // Altium emits DesignItemId after PartIDLocked (before SymbolReference).
            if (!string.IsNullOrEmpty(schComp.DesignItemId))
                parameters["DesignItemId"] = schComp.DesignItemId;
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
            // Altium writes Location for components in SchDoc but not SchLib
            if (writeLocation)
            {
                AddCoordParam(parameters, "Location.X", schComp.Location.X);
                AddCoordParam(parameters, "Location.Y", schComp.Location.Y);
            }
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
            // OwnerPartId: which part of a multi-part component this pin belongs to.
            // Parts are 1-based; default to part 1 when unset (a from-scratch single-part pin).
            w.Write((short)(pin.OwnerPartId > 0 ? pin.OwnerPartId : 1));
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
            // PartAndSequence: "{Part}|&|{Sequence}" when the pin has a swap-id field, else "".
            // HasSwapIdField distinguishes the two (e.g. vendor MCU pins omit the field entirely).
            var partAndSequence = string.Empty;
            if (pin.HasSwapIdField)
            {
                var part = pin.SwapIdPart != null && pin.SwapIdPart != "0" ? pin.SwapIdPart : string.Empty;
                partAndSequence = $"{part}|&|{pin.SwapIdSequence ?? string.Empty}";
            }
            w.WritePascalShortString(partAndSequence);
            w.WritePascalShortString(pin.DefaultValue ?? string.Empty);
        }, 0x01); // Flag = 1 for pin records

        CollectPinAuxData(pin, pinIndex, pinsFrac, pinsSymbolLineWidth);
    }

    /// <summary>
    /// Collects a pin's fractional-coordinate and symbol-line-width data for the auxiliary streams.
    /// Called both when re-encoding a pin and when replaying its captured payload verbatim.
    /// </summary>
    private static void CollectPinAuxData(SchPin pin, int pinIndex,
        Dictionary<int, (int x, int y, int length)> pinsFrac,
        Dictionary<int, Dictionary<string, string>> pinsSymbolLineWidth)
    {
        var locationX = CoordToDxpFrac(pin.Location.X);
        var locationY = CoordToDxpFrac(pin.Location.Y);
        var length = CoordToDxpFrac(pin.Length);

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
                ["SYMBOL_LINEWIDTH"] = pin.SymbolLineWidth.ToString(CultureInfo.InvariantCulture)
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
        // Bit 5: IsNotAccessible, Bit 6: graphically locked.
        if (pin.IsNotAccessible) conglomerate |= 0x20;
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
        parameters["LineWidth"] = LineWidthToIndex(line.Width).ToString(CultureInfo.InvariantCulture);
        AddNonZero(parameters, "LineStyle", line.LineStyle);
        AddNonZero(parameters, "Color", line.Color);
        AddNonZero(parameters, "LineStyleExt", line.LineStyle); // Altium writes both, LineStyleExt after Color
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
        AddNonZero(parameters, "LineStyleExt", rect.LineStyle); // rectangles store the style in LineStyleExt only, before LineWidth
        AddNonZero(parameters, "LineWidth", LineWidthToIndex(rect.LineWidth)); // omitted when 0 (Altium)
        AddNonZero(parameters, "Color", rect.Color);
        AddNonZero(parameters, "AreaColor", rect.FillColor); // omitted when 0
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
        AddNonZero(parameters, "Orientation", (int)(label.Rotation / 90) % 4); // Orientation precedes Justification
        AddNonZero(parameters, "Justification", (int)label.Justification);
        AddNonZero(parameters, "Color", label.Color); // Color precedes FontID
        parameters["FontID"] = label.FontId.ToString(CultureInfo.InvariantCulture);
        parameters["Text"] = label.Text;
        AddNonZero(parameters, "AreaColor", label.AreaColor);
        AddBool(parameters, "IsHidden", label.IsHidden);
        AddBool(parameters, "IsMirrored", label.IsMirrored);
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
        parameters["LineWidth"] = arc.LineWidth.ToString(CultureInfo.InvariantCulture);
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

        parameters["LineWidth"] = polygon.LineWidth.ToString(CultureInfo.InvariantCulture);
        AddNonZero(parameters, "Color", polygon.Color);
        parameters["AreaColor"] = polygon.FillColor.ToString(CultureInfo.InvariantCulture);
        AddBool(parameters, "IsSolid", polygon.IsFilled);
        AddBool(parameters, "Transparent", polygon.IsTransparent);
        parameters["LocationCount"] = polygon.Vertices.Count.ToString(CultureInfo.InvariantCulture);

        for (var i = 0; i < polygon.Vertices.Count; i++)
        {
            var v = polygon.Vertices[i];
            AddSchVertex(parameters, i + 1, v.X, v.Y);
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

        // Altium order: LineWidth, LineStyle, StartLineShape, EndLineShape, LineShapeSize, Color,
        // [Transparent, AreaColor, IsSolid], LocationCount, vertices, then LineStyleExt at the end.
        parameters["LineWidth"] = polyline.LineWidth.ToString(CultureInfo.InvariantCulture);
        AddNonZero(parameters, "LineStyle", (int)polyline.LineStyle);
        AddNonZero(parameters, "StartLineShape", polyline.StartLineShape);
        AddNonZero(parameters, "EndLineShape", polyline.EndLineShape);
        AddNonZero(parameters, "LineShapeSize", polyline.LineShapeSize);
        AddNonZero(parameters, "Color", polyline.Color);
        AddBool(parameters, "Transparent", polyline.IsTransparent);
        AddNonZero(parameters, "AreaColor", polyline.AreaColor);
        AddBool(parameters, "IsSolid", polyline.IsSolid);
        parameters["LocationCount"] = polyline.Vertices.Count.ToString(CultureInfo.InvariantCulture);

        for (var i = 0; i < polyline.Vertices.Count; i++)
        {
            var v = polyline.Vertices[i];
            AddSchVertex(parameters, i + 1, v.X, v.Y);
        }
        AddNonZero(parameters, "LineStyleExt", (int)polyline.LineStyle); // LineStyleExt follows the vertices
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

        parameters["LineWidth"] = bezier.LineWidth.ToString(CultureInfo.InvariantCulture);
        AddNonZero(parameters, "Color", bezier.Color);
        AddNonZero(parameters, "AreaColor", bezier.AreaColor);
        parameters["LocationCount"] = bezier.ControlPoints.Count.ToString(CultureInfo.InvariantCulture);

        for (var i = 0; i < bezier.ControlPoints.Count; i++)
        {
            var cp = bezier.ControlPoints[i];
            AddSchVertex(parameters, i + 1, cp.X, cp.Y);
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
        parameters["LineWidth"] = ellipse.LineWidth.ToString(CultureInfo.InvariantCulture);
        AddNonZero(parameters, "Color", ellipse.Color);
        parameters["AreaColor"] = ellipse.FillColor.ToString(CultureInfo.InvariantCulture);
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
        parameters["LineWidth"] = roundedRect.LineWidth.ToString(CultureInfo.InvariantCulture);
        AddNonZero(parameters, "LineStyle", roundedRect.LineStyle);
        AddNonZero(parameters, "Color", roundedRect.Color);
        parameters["AreaColor"] = roundedRect.FillColor.ToString(CultureInfo.InvariantCulture);
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
        parameters["LineWidth"] = pie.LineWidth.ToString(CultureInfo.InvariantCulture);
        if (pie.StartAngle != 0)
            parameters["StartAngle"] = FormatAngle(pie.StartAngle);
        parameters["EndAngle"] = FormatAngle(pie.EndAngle);
        AddNonZero(parameters, "Color", pie.Color);
        parameters["AreaColor"] = pie.FillColor.ToString(CultureInfo.InvariantCulture);
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

        // Parameters owned by a pin (or other primitive) carry that owner's record index;
        // component-level parameters (Designator/Comment) have OwnerIndex 0 and omit it.
        var effectiveOwnerIndex = ownerIndex >= 0 ? ownerIndex : (param.OwnerIndex > 0 ? param.OwnerIndex : -1);
        AddCommonProperties(parameters, param.OwnerIndex, param.IsNotAccessible, param.IndexInSheet,
            param.OwnerPartId, param.OwnerPartDisplayMode, param.GraphicallyLocked,
            param.Disabled, param.Dimmed, param.UniqueId, effectiveOwnerIndex);

        AddCoordParam(parameters, "Location.X", param.Location.X);
        AddCoordParam(parameters, "Location.Y", param.Location.Y);
        AddNonZero(parameters, "Color", param.Color);
        parameters["FontID"] = param.FontId.ToString(CultureInfo.InvariantCulture);
        if (!param.IsVisible) parameters["IsHidden"] = "T"; // Altium emits IsHidden right after FontID
        // Preserve the %UTF8% prefix for round-trip fidelity, and auto-promote any value that
        // Windows-1252 cannot represent (otherwise the block encoder would replace those chars
        // with '?'). When UTF-8, the value is encoded so the block emits its UTF-8 byte sequence.
        // Altium omits the Text key entirely when the value is empty (e.g. a power port's designator).
        if (!string.IsNullOrEmpty(param.Value))
        {
            var useUtf8 = param.TextIsUtf8 || RequiresUtf8(param.Value);
            parameters[useUtf8 ? "%UTF8%Text" : "Text"] =
                useUtf8 ? AltiumEncoding.EncodeUtf8ParameterValue(param.Value) : param.Value;
        }
        parameters["Name"] = param.Name;
        if (param.IsReadOnly) parameters["ReadOnlyState"] = "1";
        AddNonZero(parameters, "ParamType", param.ParamType);
        AddNonZero(parameters, "Orientation", param.Orientation);
        AddNonZero(parameters, "Justification", (int)param.Justification);
        AddBool(parameters, "ShowName", param.ShowName);
        AddBool(parameters, "IsMirrored", param.IsMirrored);
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
            ["LOCATIONCOUNT"] = wire.Vertices.Count.ToString(CultureInfo.InvariantCulture)
        };

        AddCommonProperties(parameters, wire.OwnerIndex, wire.IsNotAccessible, wire.IndexInSheet,
            wire.OwnerPartId, wire.OwnerPartDisplayMode, wire.GraphicallyLocked,
            wire.Disabled, wire.Dimmed, wire.UniqueId, ownerIndex);

        for (var i = 0; i < wire.Vertices.Count; i++)
        {
            AddSchVertex(parameters, i + 1, wire.Vertices[i].X, wire.Vertices[i].Y);
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
        parameters["FontID"] = netLabel.FontId.ToString(CultureInfo.InvariantCulture);
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
        // Altium order: [LineWidth] [Color] [LineStyle] AreaColor [TextColor] FontID IsSolid ShowBorder
        // [Orientation] Alignment WordWrap ClipToRect Text TextMargin[_Frac] [Transparent]. LineWidth
        // leads (before Color), and IsSolid follows FontID (both omitted when default).
        AddNonZero(parameters, "LineWidth", textFrame.LineWidth); // omitted when 0
        AddNonZero(parameters, "Color", textFrame.BorderColor);
        AddNonZero(parameters, "LineStyle", textFrame.LineStyle);
        parameters["AreaColor"] = textFrame.FillColor.ToString(CultureInfo.InvariantCulture);
        AddNonZero(parameters, "TextColor", textFrame.TextColor);
        parameters["FontID"] = textFrame.FontId.ToString(CultureInfo.InvariantCulture);
        AddBool(parameters, "IsSolid", textFrame.IsFilled);
        AddBool(parameters, "ShowBorder", textFrame.ShowBorder);
        AddNonZero(parameters, "Orientation", textFrame.Orientation);
        AddNonZero(parameters, "Alignment", (int)textFrame.Alignment);
        AddBool(parameters, "WordWrap", textFrame.WordWrap);
        AddBool(parameters, "ClipToRect", textFrame.ClipToRect);
        parameters["Text"] = textFrame.Text;
        AddNonZero(parameters, "TextMargin", textFrame.TextMargin);
        AddNonZero(parameters, "TextMargin_Frac", textFrame.TextMarginFrac);
        AddBool(parameters, "Transparent", textFrame.IsTransparent);
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
        parameters["LineWidth"] = image.LineWidth.ToString(CultureInfo.InvariantCulture);
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
        parameters["Symbol"] = symbol.SymbolType.ToString(CultureInfo.InvariantCulture);
        AddBool(parameters, "IsMirrored", symbol.IsMirrored);
        AddNonZero(parameters, "Orientation", symbol.Orientation);
        parameters["LineWidth"] = symbol.LineWidth.ToString(CultureInfo.InvariantCulture);
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
        parameters["Style"] = ((int)powerObj.Style).ToString(CultureInfo.InvariantCulture);
        parameters["Orientation"] = ((int)(powerObj.Rotation / 90.0)).ToString(CultureInfo.InvariantCulture);
        parameters["ShowNetName"] = powerObj.ShowNetName ? "T" : "F";
        parameters["IsCrossSheetConnector"] = powerObj.IsCrossSheetConnector ? "T" : "F";
        parameters["FontID"] = powerObj.FontId.ToString(CultureInfo.InvariantCulture);
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
        AddBool(parameters, "SuppressAll", noErc.SuppressAll);
        if (!string.IsNullOrEmpty(noErc.ErrorKindSetToSuppress))
            parameters["ErrorKindSet_ToSuppress"] = noErc.ErrorKindSetToSuppress;
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
        parameters["LocationCount"] = bus.Vertices.Count.ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < bus.Vertices.Count; i++)
        {
            AddSchVertex(parameters, i + 1, bus.Vertices[i].X, bus.Vertices[i].Y);
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
        parameters["LineWidth"] = sheetSymbol.LineWidth.ToString(CultureInfo.InvariantCulture);
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
        // DistanceFromTop is stored in 100-mil steps (1 step = 100 mils = 1,000,000 raw units), with any
        // sub-step remainder in DistanceFromTop_Frac1 (raw coord units). NOT the DXP convention.
        var dftRaw = entry.DistanceFromTop.ToRaw();
        if (dftRaw / 1_000_000 != 0) parameters["DistanceFromTop"] = (dftRaw / 1_000_000).ToString(CultureInfo.InvariantCulture);
        if (dftRaw % 1_000_000 != 0) parameters["DistanceFromTop_Frac1"] = (dftRaw % 1_000_000).ToString(CultureInfo.InvariantCulture);
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
        parameters["LocationCount"] = blanket.Vertices.Count.ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < blanket.Vertices.Count; i++)
        {
            AddSchVertex(parameters, i + 1, blanket.Vertices[i].X, blanket.Vertices[i].Y);
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

            // Record 46: MapDefinerList container — always write even when empty
            var mdlParams = new Dictionary<string, string> { ["RECORD"] = "46" };
            writer.WriteCStringParameterBlock(mdlParams);
            index++;

            foreach (var mapDefiner in impl.MapDefiners.Cast<SchMapDefiner>())
            {
                // Record 47: MapDefiner
                WriteMapDefinerRecord(writer, mapDefiner, ref index);
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

        parameters["DATAFILECOUNT"] = impl.DataFileKinds.Count.ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < impl.DataFileKinds.Count; i++)
        {
            parameters[$"MODELDATAFILEKIND{i + 1}"] = impl.DataFileKinds[i];
            if (i < impl.DataFileEntities.Count)
                parameters[$"MODELDATAFILEENTITY{i + 1}"] = impl.DataFileEntities[i];
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

        parameters["DESIMPCOUNT"] = mapDefiner.DesignatorImplementations.Count.ToString(CultureInfo.InvariantCulture);
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

    internal static void WritePinFrac(CompoundStorage storage, Dictionary<int, (int x, int y, int length)> data)
    {
        if (data.Count == 0) return;

        var pinFracStream = storage.AddStream("PinFrac");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Write header - Altium uses mixed case keys
        var headerParams = new Dictionary<string, string>
        {
            ["HEADER"] = "PinFrac",
            ["Weight"] = data.Count.ToString(CultureInfo.InvariantCulture)
        };
        writer.WriteCStringParameterBlock(headerParams);

        // Write each pin's fractional data
        foreach (var kvp in data)
        {
            WriteCompressedStorage(writer, kvp.Key.ToString(CultureInfo.InvariantCulture), w =>
            {
                w.Write(kvp.Value.x);
                w.Write(kvp.Value.y);
                w.Write(kvp.Value.length);
            });
        }

        writer.Flush();
        pinFracStream.SetData(ms.ToArray());
    }

    internal static void WritePinSymbolLineWidth(CompoundStorage storage, Dictionary<int, Dictionary<string, string>> data)
    {
        if (data.Count == 0) return;

        var lineWidthStream = storage.AddStream("PinSymbolLineWidth");
        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Write header (C-string format for section-level blocks)
        var headerParams = new Dictionary<string, string>
        {
            ["HEADER"] = "PinSymbolLineWidth",
            ["Weight"] = data.Count.ToString(CultureInfo.InvariantCulture)
        };
        writer.WriteCStringParameterBlock(headerParams);

        // Write each pin's symbol line width
        foreach (var kvp in data)
        {
            WriteCompressedStorage(writer, kvp.Key.ToString(CultureInfo.InvariantCulture), w =>
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

        // The size is a 3-byte little-endian value; the high byte is a flag set to 0x01 by Altium.
        // Guard the 24-bit limit: a larger block would overflow into the flag byte and corrupt both.
        if (blockSize > 0x00FFFFFF)
            throw new AltiumFileException(
                $"Embedded image '{name}' is too large ({blockSize} bytes) for the 3-byte storage size field (max {0x00FFFFFF}).");
        writer.Write(blockSize | 0x01000000);
        writer.Write((byte)0xD0);
        writer.Write((byte)nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write(compressed.Length);
        writer.Write(compressed);
    }

    private static void WriteStorage(CompoundFileAccessor cf, SchLibrary library)
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

    internal static void WriteStorageStream(CompoundStorage rootStorage, List<byte[]> embeddedImages)
    {
        var storageStream = rootStorage.AddStream("Storage");

        using var ms = new MemoryStream();
        using var writer = new BinaryFormatWriter(ms, leaveOpen: true);

        // Header param block: "|HEADER=Icon storage" with "|Weight=N" only when images are present
        // (Altium omits Weight entirely when the storage is empty; the key is "Weight", not "WEIGHT").
        var parameters = new Dictionary<string, string> { ["HEADER"] = "Icon storage" };
        if (embeddedImages.Count > 0)
            parameters["Weight"] = embeddedImages.Count.ToString(CultureInfo.InvariantCulture);
        writer.WriteCStringParameterBlock(parameters);

        // Write each embedded image as a compressed storage entry
        for (var i = 0; i < embeddedImages.Count; i++)
        {
            WriteCompressedStorage(writer, i.ToString(CultureInfo.InvariantCulture), w =>
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
    /// Converts a Coord to whole DXP units (10 mils per unit) used by SchDoc and SchLib
    /// vertex records. Coordinates outside the native 10-mil grid are rounded to the nearest
    /// representable point, with midpoint values rounded away from zero.
    /// </summary>
    internal static string CoordToDxpUnits(Coord coord) =>
        Math.Round(coord.ToRaw() / 100_000d, MidpointRounding.AwayFromZero)
            .ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Writes a vertex's X{n}/Y{n} parameters, omitting either when its value is 0
    /// (Altium does not emit zero-valued vertex coordinates).
    /// </summary>
    internal static void AddSchVertex(Dictionary<string, string> parameters, int index, Coord x, Coord y)
    {
        var sx = CoordToDxpUnits(x);
        var sy = CoordToDxpUnits(y);
        if (sx != "0") parameters[$"X{index}"] = sx;
        if (sy != "0") parameters[$"Y{index}"] = sy;
    }

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
            parameters[name] = dxp.ToString(CultureInfo.InvariantCulture);
        if (frac != 0)
            parameters[name + "_Frac"] = frac.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a Coord line width back to a LineWidth index (0=Small/1mil, 1=Medium/2mil, 2=Large/4mil).
    /// </summary>
    internal static int LineWidthToIndex(Coord width)
    {
        var mils = width.ToMils();
        if (mils >= 5.0) return 3; // Largest (6 mil)
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
            parameters["OwnerIndex"] = ownerIndexOverride.ToString(CultureInfo.InvariantCulture);
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
        if (value != 0) parameters[key] = value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns true when <paramref name="value"/> contains characters that Windows-1252 cannot
    /// represent (so the value must be written as a %UTF8% parameter to avoid lossy '?' substitution).
    /// </summary>
    private static bool RequiresUtf8(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var enc = AltiumEncoding.Windows1252;
        return enc.GetString(enc.GetBytes(value)) != value;
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
