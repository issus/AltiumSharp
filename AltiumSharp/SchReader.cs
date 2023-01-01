using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp
{
    public abstract class SchReader<TData> : CompoundFileReader<TData>
        where TData : SchData, new()
    {
        protected SchReader() : base()
        {
        }

        protected void SetEmbeddedImages(IEnumerable<SchPrimitive> dataItems, IDictionary<string, Image> embeddedImages)
        {
            if (embeddedImages == null)
                throw new ArgumentNullException(nameof(embeddedImages));

            var primitives = dataItems.SelectMany(item =>
                    item.GetPrimitivesOfType<SchImage>().Where(p => p.EmbedImage));

            foreach (var p in primitives)
            {
                if (embeddedImages.TryGetValue(p.Filename, out var image))
                {
                    p.Image = image;
                }
            }
        }

        /// <summary>
        /// Reads embedded images from the "Storage" section of the file.
        /// </summary>
        protected IDictionary<string, Image> ReadStorageEmbeddedImages()
        {
            var storage = Cf.TryGetStream("Storage");
            if (storage == null) return null;

            BeginContext("Storage");

            var embeddedImages = new Dictionary<string, Image>();
            using (var reader = storage.GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = parameters["HEADER"].AsStringOrDefault("");
                var weight = parameters["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "Icon storage");

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (filename, image) = ReadCompressedStorage(reader, Image.FromStream);
                    embeddedImages.Add(filename, image);
                }
                CheckValue(nameof(weight), weight, embeddedImages.Count);
            }
            EndContext();

            return embeddedImages;
        }

        protected List<SchPrimitive> ReadPrimitives(BinaryReader reader,
            Dictionary<int, (int x, int y, int length)> pinsFrac,
            Dictionary<int, ParameterCollection> pinsWideText, Dictionary<int, byte[]> pinsTextData,
            Dictionary<int, ParameterCollection> pinsSymbolLineWidth)
        {
            if (reader == null) 
                throw new ArgumentNullException(nameof(reader));

            BeginContext("ReadPrimitives");

            var primitives = new List<SchPrimitive>();
            int pinIndex = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var primitiveStartPosition = reader.BaseStream.Position;
                BeginContext($"{primitives.Count} {primitiveStartPosition}");

                var primitive = ReadRecord(reader,
                    size => ReadAsciiRecord(reader, size),
                    size =>
                    {
                        (int x, int y, int length) pinFrac = default;
                        ParameterCollection pinWideText = null;
                        byte[] pinTextData = null;
                        ParameterCollection pinSymbolLineWidth = null;
                        pinsFrac?.TryGetValue(pinIndex, out pinFrac);
                        pinsWideText?.TryGetValue(pinIndex, out pinWideText);
                        pinsTextData?.TryGetValue(pinIndex, out pinTextData);
                        pinsSymbolLineWidth?.TryGetValue(pinIndex, out pinSymbolLineWidth);
                        pinIndex++;
                        return ReadPinRecord(reader, size, pinFrac, pinWideText, pinTextData, pinSymbolLineWidth);
                    });
                primitive.SetRawData(ExtractStreamData(reader, primitiveStartPosition, reader.BaseStream.Position));
                primitives.Add(primitive);

                EndContext();
            }

            EndContext();

            return primitives;
        }

        /// <summary>
        /// Creates a record primitive with the appropriate type and import its parameter list.
        /// </summary>
        /// <param name="reader">Reader from where to read the ASCII component parameter list.</param>
        /// <param name="size">Length of the parameter list.</param>
        /// <returns>New schematic primitive as read from the parameter list.</returns>
        protected SchPrimitive ReadAsciiRecord(BinaryReader reader, int size)
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
                    record = new SchPin();
                    break;
                case 3:
                    record = new SchSymbol();
                    break;
                case 4:
                    record = new SchLabel();
                    break;
                case 5:
                    record = new SchBezier();
                    break;
                case 6:
                    record = new SchPolyline();
                    break;
                case 7:
                    record = new SchPolygon();
                    break;
                case 8:
                    record = new SchEllipse();
                    break;
                case 9:
                    record = new SchPie();
                    break;
                case 10:
                    record = new SchRoundedRectangle();
                    break;
                case 11:
                    record = new SchEllipticalArc();
                    break;
                case 12:
                    record = new SchArc();
                    break;
                case 13:
                    record = new SchLine();
                    break;
                case 14:
                    record = new SchRectangle();
                    break;
                case 17:
                    record = new SchPowerObject();
                    break;
                case 25:
                    record = new SchNetLabel();
                    break;
                case 27:
                    record = new SchWire();
                    break;
                case 29:
                    record = new SchJunction();
                    break;
                case 28:
                case 209:
                    record = new SchTextFrame();
                    break;
                case 30:
                    record = new SchImage();
                    break;
                case 31:
                    record = new SchSheetHeader();
                    break;
                case 34:
                    record = new SchDesignator();
                    break;
                case 39:
                    record = new SchTemplate();
                    break;
                case 41:
                    record = new SchParameter();
                    break;
                case 44:
                    record = new SchImplementationList();
                    break;
                case 45:
                    record = new SchImplementation();
                    break;
                case 46:
                    record = new SchMapDefinerList();
                    break;
                case 47:
                    record = new SchMapDefiner();
                    break;
                case 48:
                    record = new SchImplementationParameters();
                    break;
                default:
                    EmitWarning($"Record {recordType} not supported");
                    record = new SchPrimitive();
                    break;
            }

            return record;
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
        protected static T ReadRecord<T>(BinaryReader reader,
            Func<int, T> paramInterpreter,
            Func<int, T> binaryInterpreter,
            Func<T> onEmpty = null)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return ReadBlock(reader, size =>
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

        protected SchPin ReadPinRecord(BinaryReader reader, int size, (int x, int y, int length) pinFrac,
            ParameterCollection pinWideText, byte[] pinTextData, ParameterCollection pinSymbolLineWidth)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            BeginContext("Binary Record");

            var pin = new SchPin();
            var pinRecord = reader.ReadInt32();
            AssertValue(nameof(pinRecord), pinRecord, 2);
            reader.ReadByte(); // TODO: unknown
            pin.OwnerPartId = reader.ReadInt16();
            pin.OwnerPartDisplayMode = reader.ReadByte();
            pin.SymbolInnerEdge = (PinSymbol)reader.ReadByte();
            pin.SymbolOuterEdge = (PinSymbol)reader.ReadByte();
            pin.SymbolInside = (PinSymbol)reader.ReadByte();
            pin.SymbolOutside = (PinSymbol)reader.ReadByte();
            pin.SymbolLineWidth = LineWidth.Smallest;
            pin.Description = ReadPascalShortString(reader);
            reader.ReadByte(); // TODO: unknown
            pin.Electrical = (PinElectricalType)reader.ReadByte();
            pin.PinConglomerate = (PinConglomerateFlags)reader.ReadByte();
            pin.PinLength = Utils.DxpFracToCoord(reader.ReadInt16(), pinFrac.length);
            var locationX = Utils.DxpFracToCoord(reader.ReadInt16(), pinFrac.x);
            var locationY = Utils.DxpFracToCoord(reader.ReadInt16(), pinFrac.y);
            pin.Location = new CoordPoint(locationX, locationY);
            pin.Color = ColorTranslator.FromWin32(reader.ReadInt32());
            pin.Name = ReadPascalShortString(reader);
            pin.Designator = ReadPascalShortString(reader);
            pin.SwapIdGroup = ReadPascalShortString(reader);
            var partAndSequence = ReadPascalShortString(reader)?.Split(new[] { '|' }, 3); // format is Part|&|Sequence
            if (partAndSequence?.Length == 3)
            {
                if (int.TryParse(partAndSequence[0], out var partId))
                {
                    pin.SwapIdPart = partId;
                }
                pin.SwapIdSequence = partAndSequence[2];
            }
            pin.DefaultValue = ReadPascalShortString(reader);

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

        protected static void AssignOwners(IList<SchPrimitive> primitives)
        {
            if (primitives == null) return;

            foreach (var primitive in primitives)
            {
                var owner = primitives.ElementAtOrDefault(primitive.OwnerIndex);
                owner?.Add(primitive);
            }
        }
    }
}
