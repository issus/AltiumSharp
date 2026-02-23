using OriginalCircuit.Altium.Diagnostics;
using OriginalCircuit.Altium.Models.Sch;
using OriginalCircuit.Altium.Primitives;
using OriginalCircuit.Altium.Serialization.Binary;
using OriginalCircuit.Altium.Serialization.Compound;
using OriginalCircuit.Altium.Serialization.Dto.Sch;
using System.Text;

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
            await using var accessor = await CompoundFileAccessor.OpenAsync(path, writable: false, cancellationToken);
            return Read(accessor, cancellationToken);
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
        var headerParams = ReadParameterBlock(reader);
        document.HeaderParameters = headerParams;

        // Build a flat list of all primitives with their indices
        var allPrimitives = new List<(int index, int ownerIndex, object primitive)>();
        var components = new Dictionary<int, SchComponent>();
        var parameterSets = new Dictionary<int, SchParameterSet>();
        var blankets = new Dictionary<int, SchBlanket>();
        var sheetSymbols = new Dictionary<int, SchSheetSymbol>();
        // Implementation hierarchy tracking: ImplementationList→component, Implementation→impl, MapDefinerList→impl
        var implementationLists = new Dictionary<int, SchComponent>();  // record 44 index → owning component
        var implementations = new Dictionary<int, SchImplementation>(); // record 45 index → impl object
        var mapDefinerLists = new Dictionary<int, SchImplementation>(); // record 46 index → owning impl
        var primitiveIndex = 0;

        while (reader.HasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = SchLibReader.ReadRecordParameters(reader);
            if (parameters == null || parameters.Count == 0)
                continue;

            if (!parameters.TryGetValue("RECORD", out var recordTypeStr) ||
                !int.TryParse(recordTypeStr, out var recordType))
                continue;

            var ownerIndex = -1;
            if (parameters.TryGetValue("OWNERINDEX", out var ownerStr) && int.TryParse(ownerStr, out var oi))
                ownerIndex = oi;

            if (recordType == (int)SchRecordType.Component)
            {
                var component = CreateComponent(parameters);
                components[primitiveIndex] = component;
                allPrimitives.Add((primitiveIndex, ownerIndex, component));
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
                }
            }

            primitiveIndex++;
        }

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
            else if (primitive is not string)
            {
                // Top-level primitive (not owned by a component), skip string markers
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            SchRecordType.Parameter => CreateParameter(paramCollection),
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
            SchRecordType.Blanket => CreateBlanket(parameters, paramCollection),
            SchRecordType.ParameterSet => CreateParameterSet(paramCollection),
            SchRecordType.ImplementationList => "ImplementationList", // Container marker
            SchRecordType.Implementation => CreateImplementation(parameters),
            SchRecordType.MapDefinerList => "MapDefinerList", // Container marker
            SchRecordType.MapDefiner => CreateMapDefiner(parameters),
            SchRecordType.ImplementationParameters => "ImplementationParameters", // Empty container
            _ => null
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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

    private static SchLine CreateLine(ParameterCollection p)
    {
        var dto = Dto.Sch.SchLineDto.FromParameters(p);
        return new SchLine
        {
            Start = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            End = new Primitives.CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
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
            Corner1 = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new Primitives.CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            FontId = dto.FontId,
            Justification = (SchTextJustification)dto.Justification,
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
            Center = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            Center = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            Corner1 = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new Primitives.CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
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
            Center = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Orientation = dto.Orientation,
            Justification = (SchTextJustification)dto.Justification,
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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

    private static SchParameter CreateParameter(ParameterCollection p)
    {
        var dto = Dto.Sch.SchParameterDto.FromParameters(p);
        return new SchParameter
        {
            Name = dto.Name ?? string.Empty,
            Value = dto.Text ?? string.Empty,
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Orientation = dto.Orientation,
            Justification = (SchTextJustification)dto.Justification,
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
            Corner1 = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new Primitives.CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
            Orientation = dto.Orientation,
            Alignment = (SchTextJustification)dto.Alignment,
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
            Corner1 = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner2 = new Primitives.CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            Center = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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

    private static SchBusEntry CreateBusEntry(ParameterCollection p)
    {
        var dto = Dto.Sch.SchBusEntryDto.FromParameters(p);
        return new SchBusEntry
        {
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
            Corner = new Primitives.CoordPoint(CoordFromDxp(dto.CornerX, dto.CornerXFrac), CoordFromDxp(dto.CornerY, dto.CornerYFrac)),
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
            Primitives.Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= vertexCount)
            {
                bus.AddVertex(new Primitives.CoordPoint(hasX ? x : default, hasY ? y : default));
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
            Primitives.Coord x = default, y = default;
            var hasX = parameters.TryGetValue($"X{i}", out var xStr) && TryParseCoord(xStr, out x);
            var hasY = parameters.TryGetValue($"Y{i}", out var yStr) && TryParseCoord(yStr, out y);
            if (hasX || hasY || i <= vertexCount)
            {
                blanket.AddVertex(new Primitives.CoordPoint(hasX ? x : default, hasY ? y : default));
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
            Location = new Primitives.CoordPoint(CoordFromDxp(dto.LocationX, dto.LocationXFrac), CoordFromDxp(dto.LocationY, dto.LocationYFrac)),
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
    private static Primitives.Coord CoordFromDxp(int dxpValue, int frac = 0) => Primitives.Coord.FromRaw(dxpValue * 100_000 + frac);

    private static readonly Primitives.Coord[] LineWidths =
    {
        Primitives.Coord.FromMils(1),
        Primitives.Coord.FromMils(2),
        Primitives.Coord.FromMils(4)
    };

    private static Primitives.Coord LineWidthFromIndex(int index) =>
        index >= 0 && index < LineWidths.Length ? LineWidths[index] : LineWidths[0];

    private static bool TryParseCoord(string value, out Primitives.Coord result)
    {
        result = default;
        if (string.IsNullOrEmpty(value)) return false;
        if (int.TryParse(value, out var intValue))
        {
            result = Primitives.Coord.FromRaw(intValue * 1000);
            return true;
        }
        return false;
    }

    private static Dictionary<string, string> ReadParameterBlock(BinaryFormatReader reader)
    {
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
        return SchLibReader.ParseParameters(paramString);
    }
}
