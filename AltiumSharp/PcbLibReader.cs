using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;
using OpenMcdf;

namespace AltiumSharp
{
    /// <summary>
    /// PCB footprint library file reader.
    /// </summary>
    public sealed class PcbLibReader : CompoundFileReader
    {
        /// <summary>
        /// Contents of the PcbLib Header.
        /// </summary>
        public PcbLibHeader Header { get; private set; }

        /// <summary>
        /// UniqueId from the binary FileHeader entry
        /// </summary>
        public string UniqueId { get; private set; }

        /// <summary>
        /// List of components read from the library.
        /// </summary>
        public List<PcbComponent> Components { get; }

        /// <summary>
        /// List of model information read from the library, including positioning parameters
        /// and the raw model data.
        /// </summary>
        public Dictionary<string, (ParameterCollection positioning, string step)> Models { get; private set; }

        public PcbLibReader(string fileName) : base(fileName)
        {
            Components = new List<PcbComponent>();
        }

        protected override void DoClear()
        {
            Components.Clear();
        }

        protected override void DoReadSectionKeys(Dictionary<string, string> sectionKeys)
        {
            if (sectionKeys == null) throw new ArgumentNullException(nameof(sectionKeys));

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
                    sectionKeys.Add(libRef, sectionKey);
                }
            }

            EndContext();
        }

        protected override void DoRead()
        {
            ReadFileHeader();
            ReadLibrary();
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

            var unicodeText = ReadWideStrings(footprintStorage);
            var ndxUnicodeText = 0;

            using (var reader = footprintStorage.GetStream("Data").GetBinaryReader())
            {
                AssertValue(nameof(component.Name), component.Name, ReadStringBlock(reader));

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

                        case PcbPrimitiveObjectId.Track:
                            element = ReadFootprintTrack(reader);
                            break;

                        case PcbPrimitiveObjectId.Text:
                            element = ReadFootprintString(reader, unicodeText[ndxUnicodeText++]);
                            break;

                        case PcbPrimitiveObjectId.Fill:
                            element = ReadFootprintRectangle(reader);
                            break;

                        case PcbPrimitiveObjectId.Region:
                            element = ReadFootprintPolygon(reader);
                            break;

                        case PcbPrimitiveObjectId.Via:
                        case PcbPrimitiveObjectId.ComponentBody:
                        default:
                            // otherwise we attempt to skip the actual primitive data but still
                            // create a basic instance with just the raw data for debugging
                            element = SkipPrimitive(reader);
                            break;
                    }

                    element.SetRawData(ExtractStreamData(reader, primitiveStartPosition, reader.BaseStream.Position));
                    element.ObjectId = objectId;

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
            var uniqueIdPrimitiveInformation = componentStorage.TryGetStorage("UniqueIdPrimitiveInformation");
            if (uniqueIdPrimitiveInformation == null) return;

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
        private static PcbPrimitive SkipPrimitive(BinaryReader reader)
        {
            ReadBlock(reader);
            return new PcbPrimitive();
        }

        private PcbArc ReadFootprintArc(BinaryReader reader)
        {
            return ReadBlock(reader, recordSize =>
            {
                CheckValue(nameof(recordSize), recordSize, 45, 56);
                var arc = new PcbArc();
                arc.Layer = reader.ReadByte();
                arc.Flags = reader.ReadUInt16();
                Assert10FFbytes(reader);
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
            ReadBlock(reader); // TODO: Unknown // 0
            pad.UnknownString = ReadStringBlock(reader);
            ReadBlock(reader); // TODO: Unknown

            ReadBlock(reader, blockSize =>
            {
                pad.Layer = reader.ReadByte();
                pad.Flags = reader.ReadUInt16();
                Assert10FFbytes(reader);
                pad.Location = ReadCoordPoint(reader);
                pad.SizeTop = ReadCoordPoint(reader);
                pad.SizeMiddle = ReadCoordPoint(reader);
                pad.SizeBottom = ReadCoordPoint(reader);
                pad.HoleSize = reader.ReadInt32();
                pad.ShapeTop = (PcbPadShape)reader.ReadByte(); //72
                pad.ShapeMiddle = (PcbPadShape)reader.ReadByte();
                pad.ShapeBottom = (PcbPadShape)reader.ReadByte();
                pad.Rotation = reader.ReadDouble();
                CheckValue("#83", reader.ReadInt64(), 1L);
                reader.ReadInt32(); // TODO: Unknown
                CheckValue("#95", reader.ReadInt16(), 4);
                reader.ReadUInt32(); // TODO: Unknown
                reader.ReadUInt32(); // TODO: Unknown 
                reader.ReadUInt32(); // TODO: Unknown 
                reader.ReadUInt32(); // TODO: Unknown 
                reader.ReadUInt32(); // TODO: Unknown
                reader.ReadUInt32(); // TODO: Unknown
                reader.ReadUInt32(); // TODO: Unknown 
                reader.ReadUInt32(); // TODO: Unknown 
                AssertValue<uint>("#129", reader.ReadUInt32(), 0);
                if (blockSize > 114)
                {
                    reader.ReadUInt32(); // TODO: Unknown 
                    pad.ToLayer = reader.ReadByte();
                    reader.ReadByte(); // TODO: Unknown 
                    reader.ReadByte(); // TODO: Unknown 
                    pad.FromLayer = reader.ReadByte();
                    reader.ReadByte(); // TODO: Unknown 
                    reader.ReadByte(); // TODO: Unknown 
                }
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
                    pad.SizeMiddleLayers.Add(new CoordPoint(padXSizes[i], padYSizes[i]));
                }
                for (int i = 0; i < 29; ++i)
                {
                    pad.ShapeMiddleLayers.Add((PcbPadShape)reader.ReadByte());
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
                    pad.OffsetsFromHoleCenter.Add(new CoordPoint(offsetXFromHoleCenter[i], offsetYFromHoleCenter[i]));
                }
                reader.ReadByte(); // TODO: Unknown
                reader.ReadBytes(32); // TODO: Unknown
                for (int i = 0; i < 32; ++i)
                {
                    pad.CornerRadiusPercentage.Add(reader.ReadByte());
                }
            });

            return pad;
        }

        private PcbTrack ReadFootprintTrack(BinaryReader reader)
        {
            return ReadBlock(reader, recordSize =>
            {
                CheckValue(nameof(recordSize), recordSize, 36, 41, 45);
                var track = new PcbTrack();
                track.Layer = reader.ReadByte();
                track.Flags = reader.ReadUInt16();
                Assert10FFbytes(reader);
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

        private PcbString ReadFootprintString(BinaryReader reader, string unicodeText)
        {
            var result = ReadBlock(reader, recordSize =>
            {
                CheckValue(nameof(recordSize), recordSize, 43, 123, 226, 230);
                var @string = new PcbString();
                @string.Layer = reader.ReadByte();
                @string.Flags = reader.ReadUInt16();
                Assert10FFbytes(reader);
                @string.Location = ReadCoordPoint(reader);
                @string.Height = reader.ReadInt32();
                reader.ReadInt16(); // TODO: Unknown
                @string.Rotation = reader.ReadDouble();
                @string.Mirrored = reader.ReadBoolean();
                @string.Width = reader.ReadInt32();
                if (recordSize >= 123)
                {
                    reader.ReadUInt16(); // TODO: Unknown
                    reader.ReadByte(); // TODO: Unknown
                    @string.Font = (PcbStringFont)reader.ReadByte();
                    @string.FontBold = reader.ReadBoolean();
                    @string.FontItalic = reader.ReadBoolean();
                    @string.FontName = ReadStringFontName(reader); // TODO: check size and string format
                    @string.BarcodeLRMargin = reader.ReadInt32();
                    @string.BarcodeTBMargin = reader.ReadInt32();
                    reader.ReadInt32(); // TODO: Unknown - Coord?
                    reader.ReadInt32(); // TODO: Unknown - Coord?
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadInt32(); // TODO: Unknown - Coord?
                    reader.ReadUInt16(); // TODO: Unknown
                    reader.ReadInt32(); // TODO: Unknown - Coord?
                    reader.ReadInt32(); // TODO: Unknown
                    @string.FontInverted = reader.ReadBoolean();
                    @string.FontInvertedBorder = reader.ReadInt32();
                    reader.ReadInt32(); // TODO: Unknown
                    reader.ReadInt32(); // TODO: Unknown
                    @string.FontInvertedRect = reader.ReadBoolean();
                    @string.FontInvertedRectWidth = reader.ReadInt32();
                    @string.FontInvertedRectHeight = reader.ReadInt32();
                    @string.FontInvertedRectJustification = (PcbStringJustification)reader.ReadByte();
                    @string.FontInvertedRectTextOffset = reader.ReadInt32();
                }
                return @string;
            });

            result.AsciiText = ReadStringBlock(reader);
            result.Text = unicodeText;
            return result;
        }

        private PcbRectangle ReadFootprintRectangle(BinaryReader reader)
        {
            return ReadBlock(reader, recordSize =>
            {
                CheckValue(nameof(recordSize), recordSize, 38, 42, 46);
                var rectangle = new PcbRectangle();
                rectangle.Layer = reader.ReadByte();
                rectangle.Flags = reader.ReadUInt16();
                Assert10FFbytes(reader);
                var corner1X = reader.ReadInt32();
                var corner1Y = reader.ReadInt32();
                rectangle.Corner1 = new CoordPoint(corner1X, corner1Y);
                var corner2X = reader.ReadInt32();
                var corner2Y = reader.ReadInt32();
                rectangle.Corner2 = new CoordPoint(corner2X, corner2Y);
                rectangle.Rotation = reader.ReadDouble();
                if (recordSize >= 42)
                {
                    reader.ReadUInt32(); // TODO: Unknown
                }
                if (recordSize >= 46)
                {
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadInt32(); // TODO: Unknown - Coord?
                }
                return rectangle;
            });
        }

        private PcbPolygon ReadFootprintPolygon(BinaryReader reader)
        {
            return ReadBlock(reader, recordSize =>
            {
                var polygon = new PcbPolygon();
                polygon.Layer = reader.ReadByte();
                polygon.Flags = reader.ReadUInt16();
                Assert10FFbytes(reader);
                reader.ReadUInt32(); // TODO: Unknown
                reader.ReadByte(); // TODO: Unknown
                polygon.Attributes = ReadBlock(reader, size => ReadParameters(reader, size));
                var outlineSize = reader.ReadUInt32();
                for (int i = 0; i < outlineSize; ++i)
                {
                    // oddly enough polygonal features are stored using double precision
                    // but still employ the same units as standard Coords, which means
                    // the extra precision is not needed
                    Coord x = (int)reader.ReadDouble();
                    Coord y = (int)reader.ReadDouble();
                    polygon.Outline.Add(new CoordPoint(x, y));
                }
                return polygon;
            });
        }

        /// <summary>
        /// Reads model information from the current file.
        /// </summary>
        /// <param name="library">Storage where to look for the models data.</param>
        /// <returns>Returns model positioning parameters and its raw binary data.</returns>
        private Dictionary<string, (ParameterCollection, string)> ReadLibraryModels(CFStorage library)
        {
            BeginContext("Models");

            var result = new Dictionary<string, (ParameterCollection, string)>();
            var models = library.GetStorage("Models");
            var recordCount = ReadHeader(models);
            using (var reader = models.GetStream("Data").GetBinaryReader())
            {
                for (var i = 0; i < recordCount; ++i)
                {
                    var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                    var modelName = parameters["NAME"].AsString();
                    var modelCompressedData = models.GetStream($"{i}").GetData();

                    // models are stored as ASCII STEP files but using zlib compression
                    var stepModel = ParseCompressedZlibData(modelCompressedData, stream =>
                    {
                        using (var modelReader = new StreamReader(stream, Encoding.ASCII))
                        {
                            return modelReader.ReadToEnd();
                        }
                    });

                    // TODO: parse STEP models
                    if (!result.ContainsKey(modelName))
                    {
                        result.Add(parameters["NAME"].AsString(), (parameters, stepModel));
                    }
                    else
                    {
                        EmitWarning($"Duplicated model name: {modelName}");
                    }
                }
            }

            EndContext();

            return result;
        }

        /// <summary>
        /// Reads the library data from the current file which contains the PCB library
        /// header information parameters and also a list of the existing components.
        /// footprints that exist.
        /// </summary>
        /// <param name="library"></param>
        private void ReadLibraryData(CFStorage library)
        {
            using (var reader = library.GetStream("Data").GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                Header = new PcbLibHeader();
                Header.ImportFromParameters(parameters);

                var footprintCount = reader.ReadUInt32();
                for (var i = 0; i < footprintCount; ++i)
                {
                    var refName = ReadStringBlock(reader);
                    var sectionKey = GetSectionKeyFromRefName(refName);
                    Components.Add(ReadFootprint(sectionKey));
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
                    UniqueId = ReadPascalShortString(header);
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

            Models = ReadLibraryModels(library);
            ReadLibraryData(library);
        }
    }
}
