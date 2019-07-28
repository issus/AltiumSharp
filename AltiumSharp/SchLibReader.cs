using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;
using OpenMcdf;

namespace AltiumSharp
{
    /// <summary>
    /// Schematic library reader.
    /// </summary>
    public sealed class SchLibReader : CompoundFileReader
    {
        /// <summary>
        /// Header information for the schematic library file.
        /// </summary>
        public SchLibHeader Header { get; private set; }

        /// <summary>
        /// List of component symbols read from the file.
        /// </summary>
        public List<SchComponent> Components { get; }

        /// <summary>
        /// Mapping of image file names to the actual image data for
        /// embedded images.
        /// </summary>
        public Dictionary<string, Image> EmbeddedImages { get; }

        public SchLibReader(string fileName) : base(fileName)
        {
            Components = new List<SchComponent>();
            EmbeddedImages = new Dictionary<string, Image>();
        }

        protected override void DoClear()
        {
            Components.Clear();
            EmbeddedImages.Clear();
        }

        protected override void DoReadSectionKeys(Dictionary<string, string> sectionKeys)
        {
            var data = Cf.TryGetStream("SectionKeys");
            if (data == null) return;

            BeginContext("SectionKeys");

            using (var reader = data.GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var keyCount = parameters["KEYCOUNT"].AsIntOrDefault();
                for (int i = 0; i < keyCount; ++i)
                {
                    var libRef = parameters[$"LIBREF{i}"].AsString();
                    var sectionKey = parameters[$"SECTIONKEY{i}"].AsString();
                    sectionKeys.Add(libRef, sectionKey);
                }
            }

            EndContext();
        }

        protected override void DoRead()
        {
            ReadStorage();
            var refNames = ReadFileHeader();

            foreach (var componentRefName in refNames)
            {
                var sectionKey = GetSectionKeyFromRefName(componentRefName);
                Components.Add(ReadComponent(sectionKey));
            }
        }

        /// <summary>
        /// Reads embedded images from the "Storage" section of the file.
        /// </summary>
        private void ReadStorage()
        {
            var storage = Cf.TryGetStream("Storage");
            if (storage == null) return;

            BeginContext("Storage");

            using (var reader = storage.GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = parameters["HEADER"].AsStringOrDefault("");
                var weight = parameters["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "Icon storage");

                EmbeddedImages.Clear();
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (filename, image) = ReadCompressedStorage(reader, Image.FromStream);
                    EmbeddedImages.Add(filename, image);
                }
                CheckValue(nameof(weight), weight, EmbeddedImages.Count);
            }

            EndContext();
        }

        /// <summary>
        /// Reads the "FileHeader" section which contains the list of components that
        /// exist in the current library file.
        /// </summary>
        /// <returns></returns>
        private List<string> ReadFileHeader()
        {
            var refNames = new List<string>();

            BeginContext("FileHeader");

            using (var reader = Cf.GetStream("FileHeader").GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                Header = new SchLibHeader();
                Header.ImportFromParameters(parameters);

                if (reader.BaseStream.Position == reader.BaseStream.Length)
                {
                    // If we're at the end of the stream then read components
                    // from the parameters list
                    refNames.AddRange(Header.Comp.Select(c => c.LibRef));
                }
                else
                {
                    // Otherwise we can read the binary list of components
                    var count = reader.ReadUInt32();
                    for (var i = 0; i < count; ++i)
                    {
                        var componentRefName = ReadStringBlock(reader);
                        refNames.Add(componentRefName);
                    }
                }
            }

            EndContext();

            return refNames;
        }

        /// <summary>
        /// Reads a so-called Record entry. This can be a parameter list, or a binary form
        /// of the record, depending on the last byte of the block size.
        /// </summary>
        /// <typeparam name="T">Type of the record instance to be returned.</typeparam>
        /// <param name="reader">Reader used for reading the record.</param>
        /// <param name="paramInterpreter">
        /// Interpreter for records defined as a parameter collection.
        /// </param>
        /// <param name="binaryInterpreter">
        /// Interpreter callback for binary records.
        /// </param>
        /// <param name="onEmpty">
        /// Callback for empty records.
        /// </param>
        /// <returns>Returns object containing the interpreted record information.</returns>
        internal static T ReadRecord<T>(BinaryReader reader,
            Func<int, T> paramInterpreter,
            Func<int, T> binaryInterpreter,
            Func<T> onEmpty = null)
        {
            return ReadBlock<T>(reader, size =>
            {
                var isBinary = (size & 0xff000000) != 0;
                if (isBinary)
                {
                    return binaryInterpreter(size);
                }
                else
                {
                    return paramInterpreter(size);
                }
            }, onEmpty);
        }

        /// <summary>
        /// Reads a component stored in the <paramref name="resourceName"/> section key
        /// in the current file.
        /// </summary>
        /// <param name="resourceName">
        /// Section key where to look for the schematic component symbol data.
        /// </param>
        /// <returns>Component instance.</returns>
        private SchComponent ReadComponent(string resourceName)
        {
            var componentStorage = Cf.TryGetStorage(resourceName) ?? throw new ArgumentException($"Symbol resource not found: {resourceName}");

            BeginContext(resourceName);

            SchComponent component = null;

            var pinsWideText = ReadPinWideText(componentStorage);
            var pinsTextData = ReadPinTextData(componentStorage);
            var pinsSymbolLineWidth = ReadPinSymbolLineWidth(componentStorage);
            int pinIndex = 0;

            using (var reader = componentStorage.GetStream("Data").GetBinaryReader())
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var primitiveStartPosition = reader.BaseStream.Position;
                    var primitive = ReadRecord(reader,
                        size => ReadAsciiRecord(reader, size),
                        size =>
                        {
                            ParameterCollection pinWideText = null;
                            byte[] pinTextData = null;
                            ParameterCollection pinSymbolLineWidth = null;
                            pinsWideText?.TryGetValue(pinIndex, out pinWideText);
                            pinsTextData?.TryGetValue(pinIndex, out pinTextData);
                            pinsSymbolLineWidth?.TryGetValue(pinIndex, out pinSymbolLineWidth);
                            pinIndex++;
                            return ReadPinRecord(reader, size, pinWideText, pinTextData, pinSymbolLineWidth);
                        });

                    primitive.SetRawData(ExtractStreamData(reader, primitiveStartPosition, reader.BaseStream.Position));

                    if (component == null)
                    {
                        // First primitive read must be the component SchComponent
                        AssertValue(nameof(primitive), primitive.GetType().Name, typeof(SchComponent).Name);
                        component = (SchComponent)primitive;
                    }
                    else
                    {
                        component.Primitives.Add(primitive);
                    }
                }
            }

            AssignOwners(component);

            EndContext();

            return component;
        }

        private static void AssignOwners(SchComponent component)
        {
            foreach (var primitive in component.Primitives)
            {
                var owner = component.Primitives.ElementAtOrDefault(primitive.OwnerIndex - 1);
                primitive.Owner = owner;
            }
        }

        /// <summary>
        /// Creates a record primitive with the appropriate type and import its parameter list.
        /// </summary>
        /// <param name="reader">Reader from where to read the ASCII component parameter list.</param>
        /// <param name="size">Length of the parameter list.</param>
        /// <returns>New schematic primitive as read from the parameter list.</returns>
        private SchPrimitive ReadAsciiRecord(BinaryReader reader, int size)
        {
            var parameters = ReadParameters(reader, size);
            var recordType = parameters["RECORD"].AsIntOrDefault(-1);

            BeginContext($"ASCII Record {recordType}");

            var record = CreateRecord(recordType);
            record.ImportFromParameters(parameters);

            EndContext();

            return record;
        }

        /// <summary>
        /// Instantiates a record according to its record type number.
        /// </summary>
        /// <param name="recordType">Integer representing the record type.</param>
        /// <returns>A new empty instance of a record primitive.</returns>
        private SchPrimitive CreateRecord(int recordType)
        {
            SchPrimitive record;
            switch (recordType)
            {
                case 1:
                    record = new SchComponent();
                    break;
                case 2:
                    record = new PinRecord();
                    break;
                case 3:
                    record = new SymbolRecord();
                    break;
                case 4:
                    record = new TextStringRecord();
                    break;
                case 5:
                    record = new BezierRecord();
                    break;
                case 6:
                    record = new PolylineRecord();
                    break;
                case 7:
                    record = new PolygonRecord();
                    break;
                case 8:
                    record = new EllipseRecord();
                    break;
                case 9:
                    record = new PieChartRecord();
                    break;
                case 10:
                    record = new RoundedRectangleRecord();
                    break;
                case 11:
                    record = new EllipticalArcRecord();
                    break;
                case 12:
                    record = new ArcRecord();
                    break;
                case 13:
                    record = new LineRecord();
                    break;
                case 14:
                    record = new RectangleRecord();
                    break;
                case 28:
                case 209:
                    record = new TextFrameRecord();
                    break;
                case 30:
                    record = new ImageRecord();
                    break;
                case 34:
                    record = new Record34();
                    break;
                case 41:
                    record = new Record41();
                    break;
                case 44:
                    record = new Record44();
                    break;
                case 45:
                    record = new Record45();
                    break;
                case 46:
                    record = new Record46();
                    break;
                case 48:
                    record = new Record48();
                    break;
                default:
                    EmitWarning($"Record {recordType} not supported");
                    record = new SchPrimitive();
                    break;
            }

            return record;
        }

        private PinRecord ReadPinRecord(BinaryReader reader, int size, ParameterCollection pinWideText, byte[] pinTextData, ParameterCollection pinSymbolLineWidth)
        {
            int recordType = (size >> 24);

            BeginContext($"Binary Record {recordType}");

            var pin = new PinRecord();
            pin.Record = reader.ReadInt32();
            AssertValue(nameof(pin.Record), pin.Record, 2);
            reader.ReadByte(); // TODO: unknown
            pin.OwnerPartId = reader.ReadInt16();
            reader.ReadByte(); // TODO: unknown
            pin.SymbolInnerEdge = (PinSymbol)reader.ReadByte();
            pin.SymbolOuterEdge = (PinSymbol)reader.ReadByte();
            pin.SymbolInside = (PinSymbol)reader.ReadByte();
            pin.SymbolOutside = (PinSymbol)reader.ReadByte();
            pin.SymbolLineWidth = LineWidth.Smallest;
            pin.Description = ReadPascalShortString(reader);
            reader.ReadByte(); // TODO: unknown
            pin.Electrical = (PinElectricalType)reader.ReadByte();
            pin.PinConglomerate = (PinConglomerateFlags)reader.ReadByte();
            pin.PinLength = Utils.DxpFracToCoord(reader.ReadInt16(), 0);
            var locationX = Utils.DxpFracToCoord(reader.ReadInt16(), 0);
            var locationY = Utils.DxpFracToCoord(reader.ReadInt16(), 0);
            pin.Location = new CoordPoint(locationX, locationY);
            pin.Color = ColorTranslator.FromWin32(reader.ReadInt32());
            pin.Name = ReadPascalShortString(reader);
            pin.Designator = ReadPascalShortString(reader);
            reader.ReadByte(); // TODO: unknown
            reader.ReadByte(); // TODO: unknown
            reader.ReadByte(); // TODO: unknown

            if (pinWideText != null)
            {
                pin.Description = pinWideText["DESC"].AsStringOrDefault(pin.Description);
                pin.Name = pinWideText["NAME"].AsStringOrDefault(pin.Name);
                pin.Designator = pinWideText["DESIG"].AsStringOrDefault(pin.Designator);
            }

            if (pinTextData != null)
            {

            }

            if (pinSymbolLineWidth != null)
            {
                pin.SymbolLineWidth = pinSymbolLineWidth["SYMBOL_LINEWIDTH"].AsEnumOrDefault(pin.SymbolLineWidth);
            }

            EndContext();

            return pin;
        }

        /// <summary>
        /// Reads a pin text data for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private Dictionary<int, byte[]> ReadPinTextData(CFStorage componentStorage)
        {
            var storage = componentStorage.TryGetStream("PinTextData");
            if (storage == null) return null;

            BeginContext("PinTextData");

            var result = new Dictionary<int, byte[]>();
            using (var reader = storage.GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = parameters["HEADER"].AsStringOrDefault();
                var weight = parameters["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "PinTextData");

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (id, data) = ReadCompressedStorage(reader);
                    result.Add(int.Parse(id, CultureInfo.InvariantCulture), data);
                }
                CheckValue(nameof(weight), weight, result.Count);
            }

            EndContext();

            return result;
        }

        /// <summary>
        /// Reads a pin Unicode text for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private Dictionary<int, ParameterCollection> ReadPinWideText(CFStorage componentStorage)
        {
            var storage = componentStorage.TryGetStream("PinWideText");
            if (storage == null) return null;

            BeginContext("PinTextData");

            var result = new Dictionary<int, ParameterCollection>();
            using (var reader = storage.GetBinaryReader())
            {
                var headerParams = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = headerParams["HEADER"].AsStringOrDefault();
                var weight = headerParams["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "PinWideText");

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (id, parameters) = ReadCompressedStorage(reader, stream =>
                    {
                        using (var r = new BinaryReader(stream))
                        {
                            return ReadBlock(r, s => ReadParameters(r, s, true, Encoding.Unicode));
                        }
                    });
                    result.Add(int.Parse(id, CultureInfo.InvariantCulture), parameters);
                }
                CheckValue(nameof(weight), weight, result.Count);
            }

            EndContext();

            return result;
        }

        /// <summary>
        /// Reads a pin line widths for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private Dictionary<int, ParameterCollection> ReadPinSymbolLineWidth(CFStorage componentStorage)
        {
            var storage = componentStorage.TryGetStream("PinSymbolLineWidth");
            if (storage == null) return null;

            BeginContext("PinTextData");

            var result = new Dictionary<int, ParameterCollection>();
            using (var reader = storage.GetBinaryReader())
            {
                var headerParams = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = headerParams["HEADER"].AsStringOrDefault();
                var weight = headerParams["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "PinSymbolLineWidth");

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (id, parameters) = ReadCompressedStorage(reader, stream =>
                    {
                        using (var r = new BinaryReader(stream))
                        {
                            return ReadBlock(r, s => ReadParameters(r, s, true, Encoding.Unicode));
                        }
                    });
                    result.Add(int.Parse(id, CultureInfo.InvariantCulture), parameters);
                }
                CheckValue(nameof(weight), weight, result.Count);
            }

            EndContext();

            return result;
        }
    }
}
