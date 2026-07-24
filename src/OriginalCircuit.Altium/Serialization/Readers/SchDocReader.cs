using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Eda.Primitives;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using OriginalCircuit.Altium.Serialization.Compound;
using OriginalCircuit.Altium.Serialization.Dto.Sch;
using System.Globalization;
using System.Text;
using PinElectricalType = OriginalCircuit.Altium.Models.Sch.PinElectricalType;

namespace OriginalCircuit.Altium.Serialization.Readers;

/// <summary>
/// Reads schematic document (.SchDoc) files.
/// SchDoc files contain a flat list of primitives in the FileHeader stream.
/// Components (record type 1) own their children via OWNERINDEX.
/// </summary>
public sealed class SchDocReader
{
    private List<AltiumDiagnostic> _diagnostics = new();

    /// <summary>
    /// Reads a SchDoc file from the specified path.
    /// </summary>
    /// <param name="path">Path to the .SchDoc file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed schematic document.</returns>
    /// <exception cref="AltiumCorruptFileException">Thrown when the file cannot be parsed.</exception>
    /// <remarks>This method is not thread-safe. Create a new reader instance per thread.</remarks>
    public async ValueTask<SchDocument> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var accessor = await CompoundFileAccessor.OpenAsync(path, writable: false, cancellationToken).ConfigureAwait(false);
            var document = Read(accessor, cancellationToken);
            document.FileName = Path.GetFileName(path);
            document.FilePath = path;
            return document;
        }
        catch (Exception ex) when (ex is not AltiumFileException and not OperationCanceledException
            and not OutOfMemoryException and not FileNotFoundException and not UnauthorizedAccessException
            and not DirectoryNotFoundException)
        {
            throw new AltiumCorruptFileException($"Failed to read SchDoc file: {ex.Message}", filePath: path, innerException: ex);
        }
    }

    /// <summary>
    /// Reads a SchDoc file from a stream.
    /// </summary>
    /// <param name="stream">A readable stream containing compound file data. The stream is not closed.</param>
    /// <returns>The parsed schematic document.</returns>
    /// <exception cref="AltiumCorruptFileException">Thrown when the stream cannot be parsed.</exception>
    /// <remarks>This method is not thread-safe. Create a new reader instance per thread.</remarks>
    public SchDocument Read(Stream stream)
    {
        try
        {
            using var accessor = CompoundFileAccessor.Open(stream, leaveOpen: true);
            return Read(accessor);
        }
        catch (Exception ex) when (ex is not AltiumFileException and not OutOfMemoryException)
        {
            throw new AltiumCorruptFileException($"Failed to read SchDoc file: {ex.Message}", innerException: ex);
        }
    }

    private SchDocument Read(CompoundFileAccessor accessor, CancellationToken cancellationToken = default)
    {
        _diagnostics = new List<AltiumDiagnostic>();
        var document = new SchDocument();

        ReadFileHeader(accessor, document, cancellationToken);
        ReadStorageImageData(accessor, document);
        ReadAdditionalStreams(accessor, document);
        ReadAdditionalHarnesses(accessor, document);

        // Snapshot the primitive count AFTER all population so the writer can tell whether the
        // document was edited after load and the byte-faithful captured-order fast path is still valid.
        document.LoadedPrimitiveCount = document.CountModeledPrimitives();

        document.Diagnostics = _diagnostics;
        return document;
    }

    private static void ReadStorageImageData(CompoundFileAccessor accessor, SchDocument document)
    {
        var stream = accessor.TryGetStream("Storage");
        if (stream == null)
            return;

        var data = stream.GetData();
        if (data.Length == 0)
            return;

        var imageDataList = SchLibReader.ParseStorageImageData(data);
        if (imageDataList == null || imageDataList.Count == 0)
            return;

        // Match embedded image data to SchImage objects in order
        var embeddedImages = CollectEmbeddedImages(document);
        for (var i = 0; i < Math.Min(imageDataList.Count, embeddedImages.Count); i++)
        {
            embeddedImages[i].ImageData = imageDataList[i];
        }
    }

    private static List<SchImage> CollectEmbeddedImages(SchDocument document)
    {
        var result = new List<SchImage>();

        // Template-owned images (e.g. the title-block logo) come first: templates sit near the top of the
        // record stream, so their embedded blobs lead the Storage stream. Must stay in step with the
        // writer's collection order so each blob pairs with the right image.
        foreach (var template in document.Templates)
        {
            foreach (var primitive in template.OwnedPrimitives)
            {
                if (primitive is SchImage timg && timg.EmbedImage)
                    result.Add(timg);
            }
        }

        // Collect document-level images
        foreach (var image in document.Images)
        {
            if (image is SchImage img && img.EmbedImage)
                result.Add(img);
        }

        // Collect component-level images
        foreach (var component in document.Components)
        {
            foreach (var image in component.Images)
            {
                if (image is SchImage img && img.EmbedImage)
                    result.Add(img);
            }
        }

        return result;
    }

    private void ReadFileHeader(CompoundFileAccessor accessor, SchDocument document, CancellationToken cancellationToken = default)
    {
        var stream = accessor.TryGetStream("FileHeader");
        if (stream == null)
            return;

        var data = stream.GetData();
        using var ms = new MemoryStream(data);
        using var reader = new BinaryFormatReader(ms, leaveOpen: true);

        // Read document header parameters and store them for round-trip
        var headerParams = ReadParameterBlock(reader, out var rawHeader);
        document.HeaderParameters = headerParams;
        document.HeaderParametersOrdered = SchLibReader.ParseParametersOrdered(rawHeader);

        // Capture every record in original order, each linked to the typed model object it produces, for
        // a byte-faithful round-trip (preserving unmodeled params + ordering). This is the document-level
        // analogue of the SchLib per-component ordered-primitive capture: the params live with the model,
        // not in a detached parallel list. A binary-pin record cannot be re-emitted from a textual param
        // string, so it disables the fast path.
        var orderedRecords = new List<SchOrderedRecord>();
        var orderedRecordsUsable = true;

        // Build a flat list of all primitives with their indices
        var allPrimitives = new List<(int index, int ownerIndex, object primitive)>();
        var components = new Dictionary<int, SchComponent>();
        var parameterSets = new Dictionary<int, SchParameterSet>();
        var blankets = new Dictionary<int, SchBlanket>();
        var sheetSymbols = new Dictionary<int, SchSheetSymbol>();
        // Sheet-template records (type 39) own their border/title-block graphics via OWNERINDEX.
        var templates = new Dictionary<int, SchTemplate>();
        // Implementation hierarchy tracking: ImplementationList→component, Implementation→impl, MapDefinerList→impl
        var implementationLists = new Dictionary<int, SchComponent>();  // record 44 index → owning component
        var implementations = new Dictionary<int, SchImplementation>(); // record 45 index → impl object
        var mapDefinerLists = new Dictionary<int, SchImplementation>(); // record 46 index → owning impl
        var primitiveIndex = 0;

        while (reader.HasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = SchLibReader.ReadRecordParameters(reader, out var orderedParams, out var isBinaryRecord);
            if (parameters == null || parameters.Count == 0)
                continue;

            // The typed model object this record maps to. Records with no first-class model (the sheet
            // record, the container markers and unknown types) get a SchRawRecord holder so the captured
            // order list references a real object rather than a detached parameter blob.
            object model;

            if (!parameters.TryGetValue("RECORD", out var recordTypeStr) ||
                !int.TryParse(recordTypeStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var recordType))
            {
                model = new SchRawRecord(parameters);
            }
            else
            {
                var ownerIndex = -1;
                if (parameters.TryGetValue("OWNERINDEX", out var ownerStr) && int.TryParse(ownerStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var oi))
                    ownerIndex = oi;

                if (recordType == 31)
                {
                    // Record 31: Sheet settings (font definitions, grid, border, title-block)
                    document.SheetSettings = parameters;
                    // The system (default) font is a 1-based FontID; objects without their own font use it.
                    if (parameters.TryGetValue("SYSTEMFONT", out var sysFontStr) && int.TryParse(sysFontStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sysFont) && sysFont >= 1)
                        document.SystemFont = sysFont;
                    model = new SchRawRecord(parameters);
                }
                else if (recordType == (int)SchRecordType.Component)
                {
                    var component = CreateComponent(parameters);
                    components[primitiveIndex] = component;
                    allPrimitives.Add((primitiveIndex, ownerIndex, component));
                    model = component;
                }
                else
                {
                    var primitive = CreatePrimitive((SchRecordType)recordType, parameters);
                    if (primitive != null)
                    {
                        allPrimitives.Add((primitiveIndex, ownerIndex, primitive));

                        // Track container types for child ownership
                        if (primitive is SchParameterSet ps)
                            parameterSets[primitiveIndex] = ps;
                        else if (primitive is SchBlanket bl)
                            blankets[primitiveIndex] = bl;
                        else if (primitive is SchSheetSymbol ss)
                            sheetSymbols[primitiveIndex] = ss;
                        else if (primitive is SchImplementation impl)
                            implementations[primitiveIndex] = impl;
                        else if (primitive is SchTemplate tmpl)
                            templates[primitiveIndex] = tmpl;

                        // Container markers (record 44/46/48) are returned as interned strings; hold them
                        // so the captured order list keys on a distinct object, not a shared literal.
                        model = primitive is string ? new SchRawRecord(parameters) : primitive;
                    }
                    else
                    {
                        if (ownerIndex < 0)
                        {
                            // Unknown top-level record — store as opaque for round-trip
                            document.OpaqueRecords.Add(parameters);
                        }
                        model = new SchRawRecord(parameters);
                    }
                }

                primitiveIndex++;
            }

            // Link the captured ordered params to the model object (in original record order).
            if (isBinaryRecord || orderedParams == null)
                orderedRecordsUsable = false;
            else
                orderedRecords.Add(new SchOrderedRecord { Model = model, OrderedParams = orderedParams });
        }

        document.ReadOrderedRecords = orderedRecordsUsable ? orderedRecords : null;

        // Font table for rendering: SchDoc stores it in the RECORD=31 sheet settings (header fallback).
        document.Fonts = SchLibReader.ParseFontTable(document.SheetSettings ?? headerParams);
        if (document.Fonts.Count == 0 && document.SheetSettings != null)
            document.Fonts = SchLibReader.ParseFontTable(headerParams);

        // Assign children to their owner components/containers via OWNERINDEX
        foreach (var (index, ownerIndex, primitive) in allPrimitives)
        {
            if (primitive is SchComponent comp)
            {
                document.AddComponent(comp);
            }
            else if (primitive is string marker)
            {
                // Container markers for implementation hierarchy
                if (marker == "ImplementationList" && ownerIndex >= 0 && components.TryGetValue(ownerIndex, out var implListOwner))
                    implementationLists[index] = implListOwner;
                else if (marker == "MapDefinerList" && ownerIndex >= 0 && implementations.TryGetValue(ownerIndex, out var mdlOwner))
                    mapDefinerLists[index] = mdlOwner;
                // ImplementationParameters (record 48) is ignored — empty container
            }
            else if (ownerIndex >= 0 && components.TryGetValue(ownerIndex, out var ownerComponent))
            {
                AddPrimitiveToComponent(ownerComponent, primitive);
            }
            else if (primitive is SchImplementation impl && ownerIndex >= 0 && implementationLists.TryGetValue(ownerIndex, out var implOwnerComp))
            {
                implOwnerComp.AddImplementation(impl);
            }
            else if (primitive is SchMapDefiner md && ownerIndex >= 0 && mapDefinerLists.TryGetValue(ownerIndex, out var mdOwnerImpl))
            {
                mdOwnerImpl.AddMapDefiner(md);
            }
            else if (ownerIndex >= 0 && parameterSets.TryGetValue(ownerIndex, out var ownerParamSet) && primitive is SchParameter param)
            {
                ownerParamSet.AddParameter(param);
            }
            else if (ownerIndex >= 0 && blankets.TryGetValue(ownerIndex, out var ownerBlanket) && primitive is SchParameter blanketParam)
            {
                ownerBlanket.AddParameter(blanketParam);
            }
            else if (ownerIndex >= 0 && sheetSymbols.TryGetValue(ownerIndex, out var ownerSheetSymbol) && primitive is SchSheetEntry sheetEntry)
            {
                ownerSheetSymbol.AddEntry(sheetEntry);
            }
            else if (ownerIndex >= 0 && sheetSymbols.TryGetValue(ownerIndex, out var labelOwnerSymbol) && primitive is SheetLabelMarker sheetLabel)
            {
                if (sheetLabel.IsFileName)
                {
                    labelOwnerSymbol.FileNameLabel = sheetLabel.Label;
                    labelOwnerSymbol.FileName ??= sheetLabel.Label.Text;
                }
                else
                {
                    labelOwnerSymbol.NameLabel = sheetLabel.Label;
                    labelOwnerSymbol.SheetName ??= sheetLabel.Label.Text;
                }
            }
            else if (ownerIndex >= 0 && templates.TryGetValue(ownerIndex, out var ownerTemplate))
            {
                // Border/title-block graphics owned by a sheet template (record 39). Attach them to the
                // template rather than the document so they aren't rendered/netlisted as regular content
                // (which duplicates the sheet frame). They still round-trip via the template on write.
                ownerTemplate.AddPrimitive(primitive);
            }
            else if (primitive is not string and not SheetLabelMarker)
            {
                // Top-level primitive (not owned by a component); skip string/label markers
                document.AddPrimitive(primitive);
            }
        }
    }

    private static SchComponent CreateComponent(Dictionary<string, string> parameters)
    {
        var paramCollection = SchLibReader.ToParameterCollection(parameters);
        var dto = SchComponentDto.FromParameters(paramCollection);

        var component = new SchComponent
        {
            Name = dto.LibReference ?? dto.DesignItemId ?? string.Empty,
            Description = dto.ComponentDescription,
            Comment = dto.DesignItemId,
            PartCount = Math.Max(0, dto.PartCount - 1),
            UniqueId = dto.UniqueId,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Orientation = dto.Orientation,
            CurrentPartId = dto.CurrentPartId,
            DisplayModeCount = dto.DisplayModeCount,
            DisplayMode = dto.DisplayMode,
            ShowHiddenPins = dto.ShowHiddenPins,
            ShowHiddenFields = dto.ShowHiddenFields,
            LibraryPath = dto.LibraryPath,
            SourceLibraryName = dto.SourceLibraryName,
            LibReference = dto.LibReference,
            DesignItemId = dto.DesignItemId,
            ComponentKind = dto.ComponentKind,
            OverrideColors = dto.OverrideColors,
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            DesignatorLocked = dto.DesignatorLocked,
            PartIdLocked = dto.PartIdLocked,
            SymbolReference = dto.SymbolReference,
            SheetPartFileName = dto.SheetPartFileName,
            TargetFileName = dto.TargetFileName,
            AliasList = dto.AliasList,
            AllPinCount = dto.AllPinCount,
            GraphicallyLocked = dto.GraphicallyLocked,
            PinsMoveable = dto.PinsMoveable,
            PinColor = dto.PinColor,
            DatabaseLibraryName = dto.DatabaseLibraryName,
            DatabaseTableName = dto.DatabaseTableName,
            LibraryIdentifier = dto.LibraryIdentifier,
            VaultGuid = dto.VaultGuid,
            ItemGuid = dto.ItemGuid,
            RevisionGuid = dto.RevisionGuid,
            Disabled = dto.Disabled,
            Dimmed = dto.Dimmed,
            ConfigurationParameters = dto.ConfigurationParameters,
            ConfiguratorName = dto.ConfiguratorName,
            NotUsedBTableName = dto.NotUsedBTableName,
            OwnerPartId = dto.OwnerPartId,
            DisplayFieldNames = dto.DisplayFieldNames,
            IsMirrored = dto.IsMirrored,
            IsUnmanaged = dto.IsUnmanaged,
            IsUserConfigurable = dto.IsUserConfigurable,
            LibIdentifierKind = dto.LibIdentifierKind,
            OwnerPartDisplayMode = dto.OwnerPartDisplayMode,
            RevisionDetails = dto.RevisionDetails,
            RevisionHrid = dto.RevisionHrid,
            RevisionState = dto.RevisionState,
            RevisionStatus = dto.RevisionStatus,
            SymbolItemGuid = dto.SymbolItemGuid,
            SymbolItemsGuid = dto.SymbolItemsGuid,
            SymbolRevisionGuid = dto.SymbolRevisionGuid,
            SymbolVaultGuid = dto.SymbolVaultGuid,
            UseDbTableName = dto.UseDbTableName,
            UseLibraryName = dto.UseLibraryName,
            VariantOption = dto.VariantOption,
            VaultHrid = dto.VaultHrid,
            GenericComponentTemplateGuid = dto.GenericComponentTemplateGuid
        };

        if (parameters.TryGetValue("DESIGNATORPREFIX", out var prefix))
            component.DesignatorPrefix = prefix;

        return component;
    }

    private static object? CreatePrimitive(SchRecordType recordType, Dictionary<string, string> parameters)
    {
        // Delegate to the shared primitive creation logic in SchLibReader
        // We reuse the same method by calling it through reflection-free approach
        var paramCollection = SchLibReader.ToParameterCollection(parameters);

        return recordType switch
        {
            SchRecordType.Pin => CreatePin(parameters, paramCollection),
            SchRecordType.Line => CreateLine(paramCollection),
            SchRecordType.Rectangle => CreateRectangle(paramCollection),
            SchRecordType.Label => CreateLabel(paramCollection),
            SchRecordType.Wire => CreateWire(parameters, paramCollection),
            SchRecordType.Polyline => CreatePolyline(parameters, paramCollection),
            SchRecordType.Polygon => CreatePolygon(parameters, paramCollection),
            SchRecordType.Arc => CreateArc(paramCollection),
            SchRecordType.Bezier => CreateBezier(parameters, paramCollection),
            SchRecordType.Ellipse => CreateEllipse(paramCollection),
            SchRecordType.RoundedRectangle => CreateRoundedRectangle(paramCollection),
            SchRecordType.Pie => CreatePie(paramCollection),
            SchRecordType.NetLabel => CreateNetLabel(paramCollection),
            SchRecordType.Junction => CreateJunction(paramCollection),
            SchRecordType.Designator => CreateParameter(paramCollection, parameters),
            SchRecordType.Parameter => CreateParameter(paramCollection, parameters),
            SchRecordType.TextFrame => CreateTextFrame(paramCollection),
            SchRecordType.Image => CreateImage(paramCollection),
            SchRecordType.Symbol => CreateSymbol(paramCollection),
            SchRecordType.EllipticalArc => CreateEllipticalArc(paramCollection),
            SchRecordType.PowerObject => CreatePowerObject(paramCollection),
            SchRecordType.NoErc => CreateNoErc(paramCollection),
            SchRecordType.BusEntry => CreateBusEntry(paramCollection),
            SchRecordType.Bus => CreateBus(parameters, paramCollection),
            SchRecordType.Port => CreatePort(paramCollection),
            SchRecordType.SheetSymbol => CreateSheetSymbol(paramCollection),
            SchRecordType.SheetEntry => CreateSheetEntry(paramCollection),
            SchRecordType.SheetName => CreateSheetSymbolLabel(paramCollection, isFileName: false),
            SchRecordType.SheetFileName => CreateSheetSymbolLabel(paramCollection, isFileName: true),
            SchRecordType.Blanket => CreateBlanket(parameters, paramCollection),
            SchRecordType.ParameterSet => CreateParameterSet(paramCollection),
            SchRecordType.ImplementationList => "ImplementationList", // Container marker
            SchRecordType.Implementation => CreateImplementation(parameters),
            SchRecordType.MapDefinerList => "MapDefinerList", // Container marker
            SchRecordType.MapDefiner => CreateMapDefiner(parameters),
            SchRecordType.ImplementationParameters => "ImplementationParameters", // Empty container
            SchRecordType.Template => CreateTemplate(paramCollection),
            SchRecordType.Note => CreateNote(paramCollection),
            SchRecordType.Hyperlink => CreateHyperlink(paramCollection),
            SchRecordType.CompileMask => CreateCompileMask(paramCollection),
            SchRecordType.HarnessConnector => CreateHarnessConnector(paramCollection),
            SchRecordType.HarnessEntry => CreateHarnessEntry(paramCollection),
            SchRecordType.HarnessType => CreateHarnessType(paramCollection),
            SchRecordType.SignalHarness => CreateSignalHarness(parameters, paramCollection),
            _ => null
        };
    }

    private static SchSignalHarness CreateSignalHarness(Dictionary<string, string> parameters, ParameterCollection p)
    {
        var dto = Dto.Sch.SchSignalHarnessDto.FromParameters(p);
        var harness = new SchSignalHarness
        {
            Color = dto.Color,
            LineWidth = dto.LineWidth,
            OwnerIndex = dto.OwnerIndex,
            OwnerPartId = dto.OwnerPartId,
            IndexInSheet = dto.IndexInSheet,
            IsNotAccessible = dto.IsNotAccessible,
            UniqueId = dto.UniqueId,
        };
        for (var i = 1; i <= dto.LocationCount; i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY)
                harness.Vertices.Add(new CoordPoint(hasX ? x : default, hasY ? y : default));
        }
        return harness;
    }

    private static SchHarnessConnector CreateHarnessConnector(ParameterCollection p)
    {
        var dto = Dto.Sch.SchHarnessConnectorDto.FromParameters(p);
        return new SchHarnessConnector
        {
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            OwnerPartId = dto.OwnerPartId,
            IndexInSheet = dto.IndexInSheet,
            IsNotAccessible = dto.IsNotAccessible,
            UniqueId = dto.UniqueId,
        };
    }

    private static SchHarnessEntry CreateHarnessEntry(ParameterCollection p)
    {
        var dto = Dto.Sch.SchHarnessEntryDto.FromParameters(p);
        return new SchHarnessEntry
        {
            Text = dto.Text ?? string.Empty,
            Side = dto.Side,
            DistanceFromTop = CoordFromDxp(dto.DistanceFromTop, dto.DistanceFromTopFrac),
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            TextColor = dto.TextColor,
            TextFontId = dto.TextFontId,
            OwnerIndex = dto.OwnerIndex,
            OwnerPartId = dto.OwnerPartId,
            IndexInSheet = dto.IndexInSheet,
            IsNotAccessible = dto.IsNotAccessible,
            UniqueId = dto.UniqueId,
        };
    }

    private static SchHarnessType CreateHarnessType(ParameterCollection p)
    {
        var dto = Dto.Sch.SchHarnessTypeDto.FromParameters(p);
        return new SchHarnessType
        {
            Text = dto.Text ?? string.Empty,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Color = dto.Color,
            TextColor = dto.TextColor,
            FontId = dto.FontId,
            OwnerIndex = dto.OwnerIndex,
            OwnerPartId = dto.OwnerPartId,
            IndexInSheet = dto.IndexInSheet,
            IsNotAccessible = dto.IsNotAccessible,
            UniqueId = dto.UniqueId,
        };
    }

    private static SchCompileMask CreateCompileMask(ParameterCollection p)
    {
        var dto = Dto.Sch.SchCompileMaskDto.FromParameters(p);
        return new SchCompileMask
        {
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            OwnerPartId = dto.OwnerPartId,
            IndexInSheet = dto.IndexInSheet,
            IsNotAccessible = dto.IsNotAccessible,
            UniqueId = dto.UniqueId,
        };
    }

    private static SchNote CreateNote(ParameterCollection p)
    {
        var dto = Dto.Sch.SchNoteDto.FromParameters(p);
        return new SchNote
        {
            Text = dto.Text ?? string.Empty,
            Author = dto.Author ?? string.Empty,
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            Color = dto.Color,
            AreaColor = dto.AreaColor,
            TextColor = dto.TextColor,
            FontId = dto.FontId,
            Alignment = dto.Alignment,
            WordWrap = dto.WordWrap,
            ClipToRect = dto.ClipToRect,
            OwnerIndex = dto.OwnerIndex,
            OwnerPartId = dto.OwnerPartId,
            IndexInSheet = dto.IndexInSheet,
            IsNotAccessible = dto.IsNotAccessible,
            UniqueId = dto.UniqueId,
        };
    }

    private static SchHyperlink CreateHyperlink(ParameterCollection p)
    {
        var dto = Dto.Sch.SchHyperlinkDto.FromParameters(p);
        return new SchHyperlink
        {
            Text = dto.Text ?? string.Empty,
            Url = dto.Url ?? string.Empty,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Color = dto.Color,
            FontId = dto.FontId,
            Orientation = dto.Orientation,
            Justification = dto.Justification,
            AreaColor = dto.AreaColor,
            OwnerIndex = dto.OwnerIndex,
            OwnerPartId = dto.OwnerPartId,
            IndexInSheet = dto.IndexInSheet,
            IsNotAccessible = dto.IsNotAccessible,
            UniqueId = dto.UniqueId,
        };
    }

    private static SchTemplate CreateTemplate(ParameterCollection paramCollection)
    {
        var dto = Dto.Sch.SchTemplateDto.FromParameters(paramCollection);
        return new SchTemplate
        {
            FileName = dto.FileName,
            IsNotAccessible = dto.IsNotAccessible,
            OwnerPartId = dto.OwnerPartId,
            OwnerIndex = dto.OwnerIndex,
            IndexInSheet = dto.IndexInSheet,
        };
    }

    #region Primitive creation methods (reusing DTO pattern)

    private static SchPin CreatePin(Dictionary<string, string> parameters, ParameterCollection paramCollection)
    {
        var dto = Dto.Sch.SchPinDto.FromParameters(paramCollection);
        var pin = new SchPin
        {
            Name = dto.Name,
            Designator = dto.Designator,
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Length = CoordFromDxp(dto.PinLength, dto.PinLengthFrac),
            ElectricalType = (PinElectricalType)dto.Electrical
        };
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
        pin.SwapIdPart = dto.SwapIdPart.ToString(CultureInfo.InvariantCulture);
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

    private static SchLine CreateLine(ParameterCollection p)
    {
        var dto = Dto.Sch.SchLineDto.FromParameters(p);
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

    private static SchRectangle CreateRectangle(ParameterCollection p)
    {
        var dto = Dto.Sch.SchRectangleDto.FromParameters(p);
        return new SchRectangle
        {
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            LineWidth = LineWidthFromIndex(dto.LineWidth),
            LineStyle = dto.LineStyle,
            IsFilled = dto.IsSolid,
            IsTransparent = dto.Transparent,
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

    private static SchLabel CreateLabel(ParameterCollection p)
    {
        var dto = Dto.Sch.SchLabelDto.FromParameters(p);
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

    private static SchWire CreateWire(Dictionary<string, string> parameters, ParameterCollection p)
    {
        var dto = Dto.Sch.SchWireDto.FromParameters(p);
        var wire = SchWire.Create()
            .LineWidth(dto.LineWidth)
            .Color(dto.Color)
            .Style((SchLineStyle)dto.LineStyle);

        var wireVertexCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(wireVertexCount, 10); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= wireVertexCount)
            {
                if (i == 1) wire.From(hasX ? x : default, hasY ? y : default); else wire.To(hasX ? x : default, hasY ? y : default);
            }
            else break;
        }
        var wireResult = wire.Build();
        wireResult.AreaColor = dto.AreaColor;
        wireResult.IsSolid = dto.IsSolid;
        wireResult.IsTransparent = dto.Transparent;
        wireResult.AutoWire = dto.AutoWire;
        wireResult.UnderlineColor = dto.UnderlineColor;
        wireResult.OwnerIndex = dto.OwnerIndex;
        wireResult.IsNotAccessible = dto.IsNotAccessible;
        wireResult.IndexInSheet = dto.IndexInSheet;
        wireResult.OwnerPartId = dto.OwnerPartId;
        wireResult.OwnerPartDisplayMode = dto.OwnerPartDisplayMode;
        wireResult.GraphicallyLocked = dto.GraphicallyLocked;
        wireResult.Disabled = dto.Disabled;
        wireResult.Dimmed = dto.Dimmed;
        wireResult.UniqueId = dto.UniqueId;
        return wireResult;
    }

    private static SchPolyline CreatePolyline(Dictionary<string, string> parameters, ParameterCollection p)
    {
        var dto = Dto.Sch.SchPolylineDto.FromParameters(p);
        var polyline = SchPolyline.Create()
            .LineWidth(dto.LineWidth)
            .Color(dto.Color)
            .Style((SchLineStyle)dto.LineStyle);

        var polylineVertexCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(polylineVertexCount, 50); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= polylineVertexCount)
            {
                if (i == 1) polyline.From(hasX ? x : default, hasY ? y : default); else polyline.To(hasX ? x : default, hasY ? y : default);
            }
            else break;
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

    private static SchPolygon CreatePolygon(Dictionary<string, string> parameters, ParameterCollection p)
    {
        var dto = Dto.Sch.SchPolygonDto.FromParameters(p);
        var polygon = SchPolygon.Create()
            .LineWidth(dto.LineWidth)
            .Color(dto.Color)
            .FillColor(dto.AreaColor)
            .Filled(dto.IsSolid)
            .Transparent(dto.Transparent);

        var polygonVertexCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(polygonVertexCount, 50); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= polygonVertexCount)
            {
                polygon.AddVertex(hasX ? x : default, hasY ? y : default);
            }
            else break;
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

    private static SchArc CreateArc(ParameterCollection p)
    {
        var dto = Dto.Sch.SchArcDto.FromParameters(p);
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

    private static SchBezier CreateBezier(Dictionary<string, string> parameters, ParameterCollection p)
    {
        var dto = Dto.Sch.SchBezierDto.FromParameters(p);
        var bezier = SchBezier.Create()
            .LineWidth(dto.LineWidth)
            .Color(dto.Color);

        var bezierPointCount = dto.LocationCount;
        for (var i = 1; i <= Math.Max(bezierPointCount, 50); i++)
        {
            Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= bezierPointCount)
            {
                bezier.AddPoint(hasX ? x : default, hasY ? y : default);
            }
            else break;
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

    private static SchEllipse CreateEllipse(ParameterCollection p)
    {
        var dto = Dto.Sch.SchEllipseDto.FromParameters(p);
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

    private static SchRoundedRectangle CreateRoundedRectangle(ParameterCollection p)
    {
        var dto = Dto.Sch.SchRoundedRectangleDto.FromParameters(p);
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

    private static SchPie CreatePie(ParameterCollection p)
    {
        var dto = Dto.Sch.SchPieDto.FromParameters(p);
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

    private static SchNetLabel CreateNetLabel(ParameterCollection p)
    {
        var dto = Dto.Sch.SchNetLabelDto.FromParameters(p);
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

    private static SchJunction CreateJunction(ParameterCollection p)
    {
        var dto = Dto.Sch.SchJunctionDto.FromParameters(p);
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

    private static SchParameter CreateParameter(ParameterCollection p, Dictionary<string, string>? rawParameters = null)
    {
        var dto = Dto.Sch.SchParameterDto.FromParameters(p);
        var isUtf8 = rawParameters?.ContainsKey("%UTF8%TEXT") == true;
        var value = dto.Text ?? string.Empty;
        if (isUtf8) value = AltiumEncoding.DecodeUtf8ParameterValue(value);
        return new SchParameter
        {
            Name = dto.Name ?? string.Empty,
            Value = value,
            TextIsUtf8 = isUtf8,
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

    private static SchTextFrame CreateTextFrame(ParameterCollection p)
    {
        var dto = Dto.Sch.SchTextFrameDto.FromParameters(p);
        return new SchTextFrame
        {
            Text = dto.Text ?? string.Empty,
            Corner1 = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            Orientation = dto.Orientation,
            Alignment = (SchTextFrameAlignment)dto.Alignment,
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

    private static SchImage CreateImage(ParameterCollection p)
    {
        var dto = Dto.Sch.SchImageDto.FromParameters(p);
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

    private static SchSymbol CreateSymbol(ParameterCollection p)
    {
        var dto = Dto.Sch.SchSymbolDto.FromParameters(p);
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

    private static SchEllipticalArc CreateEllipticalArc(ParameterCollection p)
    {
        var dto = Dto.Sch.SchEllipticalArcDto.FromParameters(p);
        return new SchEllipticalArc
        {
            Center = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            PrimaryRadius = CoordFromDxp(dto.Radius, dto.RadiusFrac),
            SecondaryRadius = CoordFromDxp(dto.SecondaryRadius, dto.SecondaryRadiusFrac),
            StartAngle = dto.StartAngle,
            EndAngle = dto.EndAngle,
            LineWidth = CoordFromDxp(dto.LineWidth),
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

    private static SchPowerObject CreatePowerObject(ParameterCollection p)
    {
        var dto = Dto.Sch.SchPowerObjectDto.FromParameters(p);
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

    private static SchNoErc CreateNoErc(ParameterCollection p)
    {
        var dto = Dto.Sch.SchNoErcDto.FromParameters(p);
        return new SchNoErc
        {
            Location = new CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Orientation = dto.Orientation,
            Color = dto.Color,
            IsActive = dto.IsActive,
            Symbol = dto.Symbol,
            AreaColor = dto.AreaColor,
            SuppressAll = dto.SuppressAll,
            ErrorKindSetToSuppress = dto.ErrorKindSetToSuppress,
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

    private static SchBusEntry CreateBusEntry(ParameterCollection p)
    {
        var dto = Dto.Sch.SchBusEntryDto.FromParameters(p);
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

    private static SchBus CreateBus(Dictionary<string, string> parameters, ParameterCollection p)
    {
        var dto = Dto.Sch.SchBusDto.FromParameters(p);
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

    private static SchPort CreatePort(ParameterCollection p)
    {
        var dto = Dto.Sch.SchPortDto.FromParameters(p);
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

    private static SchSheetSymbol CreateSheetSymbol(ParameterCollection p)
    {
        var dto = Dto.Sch.SchSheetSymbolDto.FromParameters(p);
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

    private static SchSheetEntry CreateSheetEntry(ParameterCollection p)
    {
        var dto = Dto.Sch.SchSheetEntryDto.FromParameters(p);
        return new SchSheetEntry
        {
            Side = dto.Side,
            // DistanceFromTop is stored in 100-mil steps (1 = 100 mils, = 10 DXP), NOT DXP — using DXP
            // here clustered every entry against the top edge. The _Frac1 remainder is raw coord units.
            DistanceFromTop = CoordFromDxp(dto.DistanceFromTop * 10, dto.DistanceFromTopFrac1),
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

    /// <summary>
    /// A sheet symbol's name (RECORD=32) and file name (RECORD=33) are stored as separate, positioned
    /// child records that share the label layout. This marker carries the parsed label plus which slot
    /// it fills so the ownership pass can attach it to the owning sheet symbol.
    /// </summary>
    private sealed class SheetLabelMarker
    {
        public required SchLabel Label { get; init; }
        public required bool IsFileName { get; init; }
    }

    private static SheetLabelMarker CreateSheetSymbolLabel(ParameterCollection p, bool isFileName) =>
        new() { Label = CreateLabel(p), IsFileName = isFileName };

    private static SchBlanket CreateBlanket(Dictionary<string, string> parameters, ParameterCollection p)
    {
        var dto = Dto.Sch.SchBlanketDto.FromParameters(p);

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

    private static SchParameterSet CreateParameterSet(ParameterCollection p)
    {
        var dto = Dto.Sch.SchParameterSetDto.FromParameters(p);

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
            var entity = TryGetString(parameters, $"MODELDATAFILEENTITY{i}");
            if (entity != null)
                impl.DataFileEntities.Add(entity);
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
        parameters.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;

    private static bool TryGetBool(Dictionary<string, string> parameters, string key) =>
        parameters.TryGetValue(key, out var value) && string.Equals(value, "T", StringComparison.OrdinalIgnoreCase);

    #endregion

    private static void AddPrimitiveToComponent(SchComponent component, object primitive)
    {
        switch (primitive)
        {
            case SchPin pin: component.AddPin(pin); break;
            case SchLine line: component.AddLine(line); break;
            case SchRectangle rect: component.AddRectangle(rect); break;
            case SchLabel label: component.AddLabel(label); break;
            case SchWire wire: component.AddWire(wire); break;
            case SchPolyline polyline: component.AddPolyline(polyline); break;
            case SchPolygon polygon: component.AddPolygon(polygon); break;
            case SchArc arc: component.AddArc(arc); break;
            case SchBezier bezier: component.AddBezier(bezier); break;
            case SchEllipse ellipse: component.AddEllipse(ellipse); break;
            case SchRoundedRectangle roundedRect: component.AddRoundedRectangle(roundedRect); break;
            case SchPie pie: component.AddPie(pie); break;
            case SchNetLabel netLabel: component.AddNetLabel(netLabel); break;
            case SchJunction junction: component.AddJunction(junction); break;
            case SchParameter param: component.AddParameter(param); break;
            case SchTextFrame textFrame: component.AddTextFrame(textFrame); break;
            case SchImage image: component.AddImage(image); break;
            case SchSymbol symbol: component.AddSymbol(symbol); break;
            case SchEllipticalArc ellipticalArc: component.AddEllipticalArc(ellipticalArc); break;
            case SchPowerObject powerObject: component.AddPowerObject(powerObject); break;
        }
    }

    /// <summary>
    /// Converts a DXP coordinate value to raw internal units.
    /// DXP units are 10-mil increments; 1 mil = 10,000 raw units.
    /// So 1 DXP = 100,000 raw. The optional frac parameter adds sub-DXP precision.
    /// </summary>
    private static Coord CoordFromDxp(int dxpValue, int frac = 0) => Coord.FromRaw(dxpValue * 100_000 + frac);

    private static readonly Coord[] LineWidths =
    {
        Coord.FromMils(1),
        Coord.FromMils(2),
        Coord.FromMils(4)
    };

    private static Coord LineWidthFromIndex(int index) =>
        index >= 0 && index < LineWidths.Length ? LineWidths[index] : LineWidths[0];

    private static bool TryParseCoord(string value, out Coord result)
    {
        result = default;
        if (string.IsNullOrEmpty(value)) return false;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            // SchDoc vertex coords (wires, polylines, polygons, beziers, buses) are stored in DXP
            // units — the same scale as Locations (CoordFromDxp), NOT the SchLib "schematic units"
            // (x1000). Using x1000 here made every vertex 100x too small.
            result = Coord.FromRaw(intValue * 100_000);
            return true;
        }
        return false;
    }

    private static readonly HashSet<string> KnownStreamNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "FileHeader", "Storage"
    };

    private void ReadAdditionalStreams(CompoundFileAccessor accessor, SchDocument document)
    {
        var additional = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in accessor.EnumerateChildren(accessor.RootStorage))
        {
            if (KnownStreamNames.Contains(item.Name))
                continue;

            if (item.IsStorage)
            {
                var childStorage = item.AsStorage();
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
            else
            {
                var rootStream = item.AsStream();
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

    /// <summary>
    /// Parses harness connectors (215), entries (216), type labels (217) and signal harnesses (218)
    /// from the "Additional" OLE stream — Altium stores these separately from the FileHeader records.
    /// Entries/types reference their owning connector by OwnerIndex (the connector's 0-based position
    /// among the stream's records, after the stream header).
    /// </summary>
    private void ReadAdditionalHarnesses(CompoundFileAccessor accessor, SchDocument document)
    {
        var stream = accessor.TryGetStream("Additional");
        if (stream == null) return;
        var data = stream.GetData();
        if (data.Length == 0) return;

        try
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryFormatReader(ms, leaveOpen: true);
            ReadParameterBlock(reader); // consume the stream header (HEADER|WEIGHT=…)

            var connectorsByIndex = new Dictionary<int, SchHarnessConnector>();
            var pendingEntries = new List<(int owner, SchHarnessEntry entry)>();
            var pendingTypes = new List<(int owner, SchHarnessType type)>();
            var signalHarnesses = new List<SchSignalHarness>();

            var index = 0;
            while (reader.HasMore)
            {
                var p = SchLibReader.ReadRecordParameters(reader);
                if (p == null || p.Count == 0) { index++; continue; }
                if (!p.TryGetValue("RECORD", out var rts) || !int.TryParse(rts, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rt)) { index++; continue; }
                var owner = p.TryGetValue("OWNERINDEX", out var os) && int.TryParse(os, NumberStyles.Integer, CultureInfo.InvariantCulture, out var oi) ? oi : 0;
                switch (rt)
                {
                    case 215: connectorsByIndex[index] = CreateAdditionalHarnessConnector(p); break;
                    case 216: pendingEntries.Add((owner, CreateAdditionalHarnessEntry(p))); break;
                    case 217: pendingTypes.Add((owner, CreateAdditionalHarnessType(p))); break;
                    case 218: signalHarnesses.Add(CreateAdditionalSignalHarness(p)); break;
                }
                index++;
            }

            foreach (var (owner, entry) in pendingEntries)
            {
                entry.OwnerIndex = owner;
                if (connectorsByIndex.TryGetValue(owner, out var c)) c.Entries.Add(entry);
                document.AddPrimitive(entry);
            }
            foreach (var (owner, type) in pendingTypes)
            {
                type.OwnerIndex = owner;
                if (connectorsByIndex.TryGetValue(owner, out var c)) c.TypeLabel = type;
                document.AddPrimitive(type);
            }
            foreach (var c in connectorsByIndex.Values) document.AddPrimitive(c);
            foreach (var sh in signalHarnesses) document.AddPrimitive(sh);

            // These harnesses live in the verbatim-preserved Additional stream; tell the writer not to
            // also emit them to FileHeader (which would duplicate them on round-trip).
            if (connectorsByIndex.Count > 0 || pendingEntries.Count > 0 || signalHarnesses.Count > 0)
                document.HarnessesInAdditionalStream = true;
        }
        catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException or IOException)
        {
            _diagnostics.Add(new AltiumDiagnostic(DiagnosticSeverity.Warning,
                $"Failed to parse harnesses from Additional stream: {ex.Message}"));
        }
    }

    private static SchHarnessConnector CreateAdditionalHarnessConnector(Dictionary<string, string> p)
    {
        var loc = new CoordPoint(CoordFromDxp(GetInt(p, "LOCATION.X"), GetInt(p, "LOCATION.X_FRAC")),
                                 CoordFromDxp(GetInt(p, "LOCATION.Y"), GetInt(p, "LOCATION.Y_FRAC")));
        var xs = CoordFromDxp(GetInt(p, "XSIZE"));
        var ys = CoordFromDxp(GetInt(p, "YSIZE"));
        return new SchHarnessConnector
        {
            Location = loc,
            XSize = xs,
            YSize = ys,
            // Corner1/Corner2 keep the existing rectangle contract (bottom-left .. top-right).
            Corner1 = new CoordPoint(loc.X, loc.Y - ys),
            Corner2 = new CoordPoint(loc.X + xs, loc.Y),
            PrimaryConnectionPosition = CoordFromDxp(GetInt(p, "PRIMARYCONNECTIONPOSITION")),
            LineWidth = GetInt(p, "LINEWIDTH"),
            Color = GetInt(p, "COLOR"),
            AreaColor = GetInt(p, "AREACOLOR"),
            UniqueId = GetStr(p, "UNIQUEID"),
        };
    }

    private static SchHarnessEntry CreateAdditionalHarnessEntry(Dictionary<string, string> p) => new()
    {
        Text = GetStr(p, "NAME") ?? string.Empty,
        Side = GetInt(p, "SIDE"),
        // DistanceFromTop is in 100-mil steps (1 = 100 mils = 10 DXP), as for sheet entries.
        DistanceFromTop = CoordFromDxp(GetInt(p, "DISTANCEFROMTOP") * 10, GetInt(p, "DISTANCEFROMTOP_FRAC1")),
        Color = GetInt(p, "COLOR"),
        AreaColor = GetInt(p, "AREACOLOR"),
        TextColor = GetInt(p, "TEXTCOLOR"),
        TextFontId = GetInt(p, "TEXTFONTID"),
        UniqueId = GetStr(p, "UNIQUEID"),
    };

    private static SchHarnessType CreateAdditionalHarnessType(Dictionary<string, string> p) => new()
    {
        Text = GetStr(p, "TEXT") ?? string.Empty,
        Location = new CoordPoint(CoordFromDxp(GetInt(p, "LOCATION.X"), GetInt(p, "LOCATION.X_FRAC")),
                                  CoordFromDxp(GetInt(p, "LOCATION.Y"), GetInt(p, "LOCATION.Y_FRAC"))),
        Color = GetInt(p, "COLOR"),
        TextColor = GetInt(p, "TEXTCOLOR"),
        FontId = GetInt(p, "FONTID"),
        UniqueId = GetStr(p, "UNIQUEID"),
    };

    private static SchSignalHarness CreateAdditionalSignalHarness(Dictionary<string, string> p)
    {
        var sh = new SchSignalHarness
        {
            Color = GetInt(p, "COLOR"),
            LineWidth = GetInt(p, "LINEWIDTH"),
            UniqueId = GetStr(p, "UNIQUEID"),
        };
        var count = GetInt(p, "LOCATIONCOUNT");
        for (var i = 1; i <= count; i++)
        {
            if (p.TryGetValue($"X{i}", out var xStr) && p.TryGetValue($"Y{i}", out var yStr)
                && TryParseCoord(xStr, out var x) && TryParseCoord(yStr, out var y))
                sh.Vertices.Add(new CoordPoint(x, y));
        }
        return sh;
    }

    private static int GetInt(Dictionary<string, string> p, string key) =>
        p.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;

    private static string? GetStr(Dictionary<string, string> p, string key) =>
        p.TryGetValue(key, out var v) ? v : null;

    private static Dictionary<string, string> ReadParameterBlock(BinaryFormatReader reader)
        => ReadParameterBlock(reader, out _);

    private static Dictionary<string, string> ReadParameterBlock(BinaryFormatReader reader, out string rawString)
    {
        rawString = string.Empty;
        var size = reader.ReadInt32();
        var sanitizedSize = size & 0x00FFFFFF;

        if (sanitizedSize <= 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // SchDoc parameter blocks are C-strings (null-terminated, no length prefix)
        // — same format as SchLib and PCB parameter blocks.
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

        var nullIndex = Array.IndexOf(buffer, (byte)0);
        var length = nullIndex >= 0 ? nullIndex : sanitizedSize;
        var paramString = AltiumEncoding.Windows1252.GetString(buffer, 0, length);
        rawString = paramString;
        return SchLibReader.ParseParameters(paramString);
    }
}
