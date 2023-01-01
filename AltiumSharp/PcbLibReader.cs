using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp
{
    /// <summary>
    /// PCB footprint library file reader.
    /// </summary>
    public sealed class PcbLibReader : CompoundFileReader<PcbLib>
    {
        public PcbLibReader() : base()
        {

        }

        protected override void DoRead()
        {
            ReadFileHeader();
            ReadSectionKeys();
            ReadLibrary();
        }

        /// <summary>
        /// Reads section keys information which can be used to match "ref lib" component names into
        /// usable compound storage section names.
        /// <para>
        /// Data read can be accessed through the <see cref="GetSectionKeyFromRefName"/> method.
        /// </para>
        /// </summary>
        private void ReadSectionKeys()
        {
            SectionKeys.Clear();

            var data = Cf.TryGetStream("SectionKeys");
            if (data == null) return;

            BeginContext("SectionKeys");

            using (var reader = data.GetBinaryReader())
            {
                var keyCount = reader.ReadInt32();
                for (int i = 0; i < keyCount; ++i)
                {
                    var libRef = ReadPascalString(reader);
                    var sectionKey = ReadStringBlock(reader);
                    SectionKeys.Add(libRef, sectionKey);
                }
            }

            EndContext();
        }

        /// <summary>
        /// Reads a PCB component footprint stored in the given <paramref name="sectionKey"/>,
        /// </summary>
        /// <param name="sectionKey">
        /// Section storage key where to look for the component footprint parameters and data.
        /// </param>
        /// <returns></returns>
        private PcbComponent ReadFootprint(string sectionKey)
        {
            var footprintStorage = Cf.TryGetStorage(sectionKey) ?? throw new ArgumentException($"Footprint resource not found: {sectionKey}");

            BeginContext(sectionKey);

            var recordCount = ReadHeader(footprintStorage);

            var component = new PcbComponent();
            ReadFootprintParameters(footprintStorage, component);

            var wideStrings = ReadWideStrings(footprintStorage);

            using (var reader = footprintStorage.GetStream("Data").GetBinaryReader())
            {
                AssertValue(nameof(component.Pattern), component.Pattern, ReadStringBlock(reader));

                int ndxRecord = 0;
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    if (ndxRecord > recordCount)
                    {
                        EmitWarning("Number of existing records exceed the header's record count");
                    }

                    // save the stream position so we can later recover the raw component data
                    var primitiveStartPosition = reader.BaseStream.Position;

                    PcbPrimitive element = null;
                    var objectId = (PcbPrimitiveObjectId)reader.ReadByte();
                    BeginContext(objectId.ToString());
                    switch (objectId)
                    {
                        case PcbPrimitiveObjectId.Arc:
                            element = ReadFootprintArc(reader);
                            break;

                        case PcbPrimitiveObjectId.Pad:
                            element = ReadFootprintPad(reader);
                            break;

                        case PcbPrimitiveObjectId.Via:
                            element = ReadFootprintVia(reader);
                            break;

                        case PcbPrimitiveObjectId.Track:
                            element = ReadFootprintTrack(reader);
                            break;

                        case PcbPrimitiveObjectId.Text:
                            element = ReadFootprintString(reader, wideStrings);
                            break;

                        case PcbPrimitiveObjectId.Fill:
                            element = ReadFootprintFill(reader);
                            break;

                        case PcbPrimitiveObjectId.Region:
                            element = ReadFootprintRegion(reader);
                            break;

                        case PcbPrimitiveObjectId.ComponentBody:
                            element = ReadFootprintComponentBody(reader);
                            break;

                        default:
                            // otherwise we attempt to skip the actual primitive data but still
                            // create a basic instance with just the raw data for debugging
                            element = ReadFootprintUknown(reader, objectId);
                            break;
                    }

                    element.SetRawData(ExtractStreamData(reader, primitiveStartPosition, reader.BaseStream.Position));

                    component.Primitives.Add(element);

                    EndContext();
                    ndxRecord++;
                }
            }

            ReadUniqueIdPrimitiveInformation(footprintStorage, component);

            EndContext();

            return component;
        }

        /// <summary>
        /// Reads the component parameter information.
        /// </summary>
        /// <param name="componentStorage">Component footprint storage key.</param>
        /// <param name="component">Component instance where the parameters will be imported into.</param>
        private void ReadFootprintParameters(CFStorage componentStorage, PcbComponent component)
        {
            BeginContext("Parameters");
            try
            {
                using (var reader = componentStorage.GetStream("Parameters").GetBinaryReader())
                {
                    var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                    component.ImportFromParameters(parameters);
                }
            }
            finally
            {
                EndContext();
            }
        }

        private void ReadUniqueIdPrimitiveInformation(CFStorage componentStorage, PcbComponent component)
        {
            if (!componentStorage.TryGetStorage("UniqueIdPrimitiveInformation", out var uniqueIdPrimitiveInformation)) return;

            BeginContext("UniqueIdPrimitiveInformation");
            try
            {
                var recordCount = ReadHeader(uniqueIdPrimitiveInformation);

                using (var reader = uniqueIdPrimitiveInformation.GetStream("Data").GetBinaryReader())
                {
                    uint actualRecordCount = 0;
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                        var primitiveIndex = parameters["PRIMITIVEINDEX"].AsIntOrDefault();
                        var primitiveObjectId = parameters["PRIMITIVEOBJECTID"].AsStringOrDefault();
                        var uniqueId = parameters["UNIQUEID"].AsStringOrDefault();

                        if (!CheckValue("PRIMITIVEINDEX < Primitives.Count", primitiveIndex < component.Primitives.Count, true))
                        {
                            return;
                        }

                        var primitive = component.Primitives[primitiveIndex];

                        if (!CheckValue(nameof(primitiveObjectId), primitiveObjectId, primitive.ObjectId.ToString()))
                        {
                            return;
                        }

                        primitive.UniqueId = uniqueId;
                        actualRecordCount++;
                    }
                    AssertValue(nameof(actualRecordCount), actualRecordCount, recordCount);
                }
            }
            finally
            {
                EndContext();
            }
        }


        /// <summary>
        /// Asserts that the next bytes are a sequence of 10 bytes with the <c>0xFF</c> value.
        /// </summary>
        /// <param name="reader">Binary reader to be used.</param>
        private void Assert10FFbytes(BinaryReader reader)
        {
            AssertValue("10 0xFF bytes", reader.ReadBytes(10).All(b => b == 0xFF), true);
        }

        /// <summary>
        /// Attempts to skip the current primitive by reading it as a block.
        /// </summary>
        /// <param name="reader">Binary reader to be used.</param>
        /// <returns>
        /// Creates a simple <see cref="PcbPrimitive"/> instance as such that even
        /// if we cannot read the data yet, we already have it and are able to inspect it
        /// and list it alongside the other component primitives.
        /// </returns>
        private static PcbPrimitive ReadFootprintUknown(BinaryReader reader, PcbPrimitiveObjectId objectId)
        {
            ReadBlock(reader);
            return new PcbUnknown(objectId);
        }

        private void ReadFootprintCommon(BinaryReader reader, PcbPrimitive primitive)
        {
            primitive.Layer = reader.ReadByte();
            primitive.Flags = (PcbFlags)reader.ReadUInt16();
            Assert10FFbytes(reader);
        }

        private PcbArc ReadFootprintArc(BinaryReader reader)
        {
            return ReadBlock(reader, recordSize =>
            {
                CheckValue(nameof(recordSize), recordSize, 45, 56);
                var arc = new PcbArc();
                ReadFootprintCommon(reader, arc);
                arc.Location = ReadCoordPoint(reader);
                arc.Radius = reader.ReadInt32();
                arc.StartAngle = reader.ReadDouble();
                arc.EndAngle = reader.ReadDouble();
                arc.Width = reader.ReadInt32();
                if (recordSize >= 56)
                {
                    reader.ReadUInt32(); // TODO: Unknown - ordering?
                    reader.ReadUInt16(); // TODO: Unknown
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadUInt32(); // TODO: Unknown
                }
                return arc;
            });
        }

        private PcbPad ReadFootprintPad(BinaryReader reader)
        {
            var pad = new PcbPad();
            pad.Designator = ReadStringBlock(reader);
            ReadBlock(reader); // TODO: Unknown 
            ReadStringBlock(reader); // constant: |&|0
            ReadBlock(reader); // TODO: Unknown

            ReadBlock(reader, blockSize =>
            {
                ReadFootprintCommon(reader, pad);
                pad.Location = ReadCoordPoint(reader);
                pad.SizeTop = ReadCoordPoint(reader);
                pad.SizeMiddle = ReadCoordPoint(reader);
                pad.SizeBottom = ReadCoordPoint(reader);
                pad.HoleSize = reader.ReadInt32();
                pad.ShapeTop = (PcbPadShape)reader.ReadByte(); //72
                pad.ShapeMiddle = (PcbPadShape)reader.ReadByte();
                pad.ShapeBottom = (PcbPadShape)reader.ReadByte();
                pad.Rotation = reader.ReadDouble();
                pad.IsPlated = reader.ReadBoolean();
                CheckValue("#91", reader.ReadByte(), 0);
                pad.StackMode = (PcbStackMode)reader.ReadByte();
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadInt32(); // TODO: Unknown 0
                reader.ReadInt32(); // TODO: Unknown 10mil?
                CheckValue("#102", reader.ReadInt16(), 4);
                reader.ReadUInt32(); // TODO: Unknown 10mil?
                reader.ReadUInt32(); // TODO: Unknown 20mil?
                reader.ReadUInt32(); // TODO: Unknown 20mil?
                pad.PasteMaskExpansion = Coord.FromInt32(reader.ReadInt32());
                pad.SolderMaskExpansion = Coord.FromInt32(reader.ReadInt32());
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadByte(); // TODO: Unknown 0/1
                reader.ReadByte(); // TODO: Unknown 0/1
                reader.ReadByte(); // TODO: Unknown 0/1
                reader.ReadByte(); // TODO: Unknown 0/1
                pad.PasteMaskExpansionManual = reader.ReadByte() == 2;
                pad.SolderMaskExpansionManual = reader.ReadByte() == 2;
                reader.ReadByte(); // TODO: Unknown 0/1
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadUInt32(); // TODO: Unknown
                pad.JumperId = reader.ReadInt16();
                reader.ReadInt16();
                /*
                if (blockSize > 114)
                {
                    reader.ReadUInt32(); // TODO: Unknown 
                    pad.Layer = reader.ReadByte(); // Layer again?
                    reader.ReadByte(); // TODO: Unknown 
                    reader.ReadByte(); // TODO: Unknown 
                    pad.FromLayer = reader.ReadByte();
                    reader.ReadByte(); // TODO: Unknown 
                    reader.ReadByte(); // TODO: Unknown 
                }
                */
            });

            // Read size and shape and parts of hole information
            ReadBlock(reader, blockSize =>
            {
                CheckValue(nameof(blockSize), blockSize, 596, 628);
                var padXSizes = new Coord[29];
                var padYSizes = new Coord[29];
                for (int i = 0; i < 29; ++i) padXSizes[i] = reader.ReadInt32();
                for (int i = 0; i < 29; ++i) padYSizes[i] = reader.ReadInt32();
                for (int i = 0; i < 29; ++i)
                {
                    pad.SizeMiddleLayers[i] = new CoordPoint(padXSizes[i], padYSizes[i]);
                }
                for (int i = 1; i < 30; ++i)
                {
                    pad.ShapeLayers[i] = (PcbPadShape)reader.ReadByte();
                }
                reader.ReadByte(); // TODO: Unknown
                pad.HoleShape = (PcbPadHoleShape)reader.ReadByte();
                pad.HoleSlotLength = reader.ReadInt32();
                pad.HoleRotation = reader.ReadDouble();
                var offsetXFromHoleCenter = new int[32];
                var offsetYFromHoleCenter = new int[32];
                for (int i = 0; i < 32; ++i)
                {
                    offsetXFromHoleCenter[i] = reader.ReadInt32();
                }
                for (int i = 0; i < 32; ++i)
                {
                    offsetYFromHoleCenter[i] = reader.ReadInt32();
                }
                for (int i = 0; i < 32; ++i)
                {
                    pad.OffsetsFromHoleCenter[i] = new CoordPoint(offsetXFromHoleCenter[i], offsetYFromHoleCenter[i]);
                }
                var hasRoundedRect = reader.ReadBoolean();
                if (hasRoundedRect)
                {
                    for (int i = 0; i < 32; ++i) pad.ShapeLayers[i] = (PcbPadShape)reader.ReadByte();
                }
                else
                {
                    for (int i = 0; i < 32; ++i) reader.ReadByte(); // ignore values
                }
                for (int i = 0; i < 32; ++i)
                {
                    pad.CornerRadiusPercentage[i] = reader.ReadByte();
                }
            });

            return pad;
        }

        private PcbVia ReadFootprintVia(BinaryReader reader)
        {
            return ReadBlock(reader, recordSize =>
            {
                //CheckValue(nameof(recordSize), recordSize, , );
                var via = new PcbVia();
                ReadFootprintCommon(reader, via);
                via.Location = ReadCoordPoint(reader);
                via.Diameter = Coord.FromInt32(reader.ReadInt32());
                via.HoleSize = Coord.FromInt32(reader.ReadInt32());
                via.FromLayer = reader.ReadByte();
                via.ToLayer = reader.ReadByte();
                reader.ReadByte(); // TODO: Unknown 0
                via.ThermalReliefAirGapWidth = Coord.FromInt32(reader.ReadInt32());
                via.ThermalReliefConductors = reader.ReadByte();
                reader.ReadByte(); // TODO: Unknown 0
                via.ThermalReliefConductorsWidth = Coord.FromInt32(reader.ReadInt32());
                reader.ReadInt32(); // TODO: Unknown - Coord 20mils?
                reader.ReadInt32(); // TODO: Unknown - Coord 20mils?
                reader.ReadInt32(); // TODO: Unknown 0
                via.SolderMaskExpansion = Coord.FromInt32(reader.ReadInt32());
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadByte(); // TODO: Unknown 0
                reader.ReadByte(); // TODO: Unknown 1
                reader.ReadByte(); // TODO: Unknown 1
                reader.ReadByte(); // TODO: Unknown 1
                reader.ReadByte(); // TODO: Unknown 1
                reader.ReadByte(); // TODO: Unknown 0
                via.SolderMaskExpansionManual = reader.ReadByte() == 2;
                reader.ReadByte(); // TODO: Unknown 1
                reader.ReadInt16(); // TODO: Unknown 0
                reader.ReadInt32(); // TODO: Unknown 0
                via.DiameterStackMode = (PcbStackMode)reader.ReadByte();
                for (int i = 0; i < 32; ++i)
                {
                    via.Diameters[i] = Coord.FromInt32(reader.ReadInt32());
                }
                reader.ReadInt16(); // TODO: Unknown 15
                reader.ReadInt32(); // TODO: Unknown 259

                return via;
            });
        }

        private PcbTrack ReadFootprintTrack(BinaryReader reader)
        {
            return ReadBlock(reader, recordSize =>
            {
                CheckValue(nameof(recordSize), recordSize, 36, 41, 45);
                var track = new PcbTrack();
                ReadFootprintCommon(reader, track);
                var startX = reader.ReadInt32();
                var startY = reader.ReadInt32();
                track.Start = new CoordPoint(startX, startY);
                var endX = reader.ReadInt32();
                var endY = reader.ReadInt32();
                track.End = new CoordPoint(endX, endY);
                track.Width = reader.ReadInt32();
                reader.ReadByte(); // TODO: Unknown
                reader.ReadByte(); // TODO: Unknown
                reader.ReadByte(); // TODO: Unknown
                if (recordSize >= 41)
                {
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadUInt32(); // TODO: Unknown
                }
                if (recordSize >= 45)
                {
                    reader.ReadUInt32(); // TODO: Unknown
                }
                return track;
            });
        }

        private PcbText ReadFootprintString(BinaryReader reader, List<string> wideStrings)
        {
            var result = ReadBlock(reader, recordSize =>
            {
                CheckValue(nameof(recordSize), recordSize, 43, 123, 226, 230);
                var text = new PcbText();
                ReadFootprintCommon(reader, text);
                text.Corner1 = ReadCoordPoint(reader);
                var height = reader.ReadInt32();
                text.Corner2 = new CoordPoint(
                    Coord.FromInt32(text.Corner1.X.ToInt32()),
                    Coord.FromInt32(text.Corner1.Y.ToInt32() + height));
                text.StrokeFont = (PcbTextStrokeFont)reader.ReadInt16();
                text.Rotation = reader.ReadDouble();
                text.Mirrored = reader.ReadBoolean();
                text.StrokeWidth = reader.ReadInt32();
                if (recordSize >= 123)
                {
                    reader.ReadUInt16(); // TODO: Unknown
                    reader.ReadByte(); // TODO: Unknown
                    text.TextKind = (PcbTextKind)reader.ReadByte();
                    text.FontBold = reader.ReadBoolean();
                    text.FontItalic = reader.ReadBoolean();
                    text.FontName = ReadStringFontName(reader); // TODO: check size and string format
                    text.BarcodeLRMargin = reader.ReadInt32();
                    text.BarcodeTBMargin = reader.ReadInt32();
                    reader.ReadInt32(); // TODO: Unknown - Coord 0?
                    reader.ReadInt32(); // TODO: Unknown - Coord 4mil?
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadInt32(); // TODO: Unknown - Coord?
                    reader.ReadUInt16(); // TODO: Unknown
                    reader.ReadInt32(); // TODO: Unknown 1
                    reader.ReadInt32(); // TODO: Unknown
                    text.FontInverted = reader.ReadBoolean();
                    text.FontInvertedBorder = reader.ReadInt32();
                    text.WideStringsIndex = reader.ReadInt32();
                    reader.ReadInt32(); // TODO: Unknown
                    text.FontInvertedRect = reader.ReadBoolean();
                    text.FontInvertedRectWidth = reader.ReadInt32();
                    text.FontInvertedRectHeight = reader.ReadInt32();
                    text.FontInvertedRectJustification = (PcbTextJustification)reader.ReadByte();
                    text.FontInvertedRectTextOffset = reader.ReadInt32();
                }
                return text;
            });

            var asciiText = ReadStringBlock(reader); // non-unicode Text
            if (result.WideStringsIndex < wideStrings?.Count)
            {
                result.Text = wideStrings[result.WideStringsIndex];
            }
            else
            { 
                result.Text = asciiText;
            }
            return result;
        }

        private PcbFill ReadFootprintFill(BinaryReader reader)
        {
            return ReadBlock(reader, recordSize =>
            {
                CheckValue(nameof(recordSize), recordSize, 37, 41, 46);
                var fill = new PcbFill();
                ReadFootprintCommon(reader, fill);
                fill.Corner1 = ReadCoordPoint(reader);
                fill.Corner2 = ReadCoordPoint(reader);
                fill.Rotation = reader.ReadDouble();
                if (recordSize >= 41)
                {
                    reader.ReadUInt32(); // TODO: Unknown
                }
                if (recordSize >= 46)
                {
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadInt32(); // TODO: Unknown - Coord?
                }
                return fill;
            });
        }

        private ParameterCollection ReadFootprintCommonParametersAndOutline(BinaryReader reader, PcbPrimitive primitive,
            List<CoordPoint> outline)
        {
            ParameterCollection parameters = null;
            ReadBlock(reader, recordSize =>
            {
                ReadFootprintCommon(reader, primitive);
                reader.ReadUInt32(); // TODO: Unknown
                reader.ReadByte(); // TODO: Unknown
                parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var outlineSize = reader.ReadUInt32();
                for (int i = 0; i < outlineSize; ++i)
                {
                    // oddly enough polygonal features are stored using double precision
                    // but still employ the same units as standard Coords, which means
                    // the extra precision is not needed
                    Coord x = (int)reader.ReadDouble();
                    Coord y = (int)reader.ReadDouble();
                    outline.Add(new CoordPoint(x, y));
                }
            });
            return parameters;
        }

        private PcbRegion ReadFootprintRegion(BinaryReader reader)
        {
            var region = new PcbRegion();
            var parameters = ReadFootprintCommonParametersAndOutline(reader, region, region.Outline);
            region.Parameters = parameters;
            return region;
        }

        private PcbComponentBody ReadFootprintComponentBody(BinaryReader reader)
        {
            var body = new PcbComponentBody();
            var parameters = ReadFootprintCommonParametersAndOutline(reader, body, body.Outline);
            body.ImportFromParameters(parameters);
            return body;
        }

        /// <summary>
        /// Reads model information from the current file, and assigns it to their respective
        /// component bodies.
        /// Model information includes the model positioning parameters and its STEP data.
        /// </summary>
        /// <param name="library">Storage where to look for the models data.</param>
        private void ReadLibraryModels(CFStorage library)
        {
            BeginContext("Models");

            var models = library.GetStorage("Models");
            var recordCount = ReadHeader(models);
            using (var reader = models.GetStream("Data").GetBinaryReader())
            {
                for (var i = 0; i < recordCount; ++i)
                {
                    var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                    var modelId = parameters["ID"].AsString();
                    var modelCompressedData = models.GetStream($"{i}").GetData();

                    // models are stored as ASCII STEP files but using zlib compression
                    var stepModel = ParseCompressedZlibData(modelCompressedData, stream =>
                    {
                        using (var modelReader = new StreamReader(stream, Encoding.ASCII))
                        {
                            return modelReader.ReadToEnd();
                        }
                    });

                    // assign STEP data to component bodies
                    var bodies = Data.Items.SelectMany(c => c.GetPrimitivesOfType<PcbComponentBody>(false))
                        .Where(body => body.ModelId.ToUpperInvariant() == modelId.ToUpperInvariant());
                    foreach (var body in bodies)
                    {
                        body.StepModel = stepModel;
                    }
                }
            }

            EndContext();
        }

        /// <summary>
        /// Reads the library data from the current file which contains the PCB library
        /// header information parameters and also a list of the existing components.
        /// </summary>
        /// <param name="library"></param>
        private void ReadLibraryData(CFStorage library)
        {
            using (var reader = library.GetStream("Data").GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                Data.Header.ImportFromParameters(parameters);

                var footprintCount = reader.ReadUInt32();
                for (var i = 0; i < footprintCount; ++i)
                {
                    var refName = ReadStringBlock(reader);
                    var sectionKey = GetSectionKeyFromRefName(refName);
                    Data.Items.Add(ReadFootprint(sectionKey));
                }
            }
        }

        private void ReadFileHeader()
        {
            var data = Cf.TryGetStream("FileHeader");
            if (data == null) return;

            using (var header = data.GetBinaryReader())
            {
                // for some reason this is different than a StringBlock as the
                // initial block length is the same as the short string length
                var pcbBinaryFileVersionTextLength = header.ReadInt32();
                var pcbBinaryFileVersionText = ReadPascalShortString(header);
                if (header.BaseStream.Position < header.BaseStream.Length)
                {
                    ReadPascalShortString(header); // TODO: Unknown
                    ReadPascalShortString(header); // TODO: Unknown
                    Data.UniqueId = ReadPascalShortString(header);
                }
            }
        }

        /// <summary>
        /// Main method that reads the contents of the PCB library file.
        /// </summary>
        private void ReadLibrary()
        {
            var library = Cf.GetStorage("Library");
            var recordCount = ReadHeader(library);

            ReadLibraryData(library);
            ReadLibraryModels(library);
        }
    }
}
