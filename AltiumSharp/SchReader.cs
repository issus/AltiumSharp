using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;

namespace AltiumSharp
{
    public abstract class SchReader : CompoundFileReader
    {
        protected SchReader(string fileName) : base(fileName)
        {
            EmbeddedImages = new Dictionary<string, Image>();
        }

        /// <summary>
        /// Mapping of image file names to the actual image data for
        /// embedded images.
        /// </summary>
        public Dictionary<string, Image> EmbeddedImages { get; }

        protected sealed override void DoClear()
        {
            EmbeddedImages.Clear();
            DoClearSch();
        }

        protected abstract void DoClearSch();

        protected sealed override void DoRead()
        {
            ReadStorageEmbeddedImages();
            DoReadSch();
        }

        protected abstract void DoReadSch();

        /// <summary>
        /// Reads embedded images from the "Storage" section of the file.
        /// </summary>
        protected void ReadStorageEmbeddedImages()
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

        protected List<SchPrimitive> ReadPrimitives(BinaryReader reader, Dictionary<int, ParameterCollection> pinsWideText,
            Dictionary<int, byte[]> pinsTextData, Dictionary<int, ParameterCollection> pinsSymbolLineWidth)
        {
            BeginContext("ReadPrimitives");

            var primitives = new List<SchPrimitive>();
            int pinIndex = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var primitiveStartPosition = reader.BaseStream.Position;
                var primitive = ReadRecord(reader,
                    size => ReadAsciiRecord(reader, size),
                    size => {
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

                primitives.Add(primitive);
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
                    record = new SheetRecord();
                    break;
                case 34:
                    record = new DesignatorLabelRecord();
                    break;
                case 39:
                    record = new SchTemplate();
                    break;
                case 41:
                    record = new SchParameter();
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

        protected SchPin ReadPinRecord(BinaryReader reader, int size, ParameterCollection pinWideText, byte[] pinTextData, ParameterCollection pinSymbolLineWidth)
        {
            int recordType = (size >> 24);

            BeginContext($"Binary Record {recordType}");

            var pin = new SchPin();
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

        protected static void AssignOwners(IList<SchPrimitive> primitives, IList<SchPrimitive> rootList)
        {
            if (primitives == null) return;

            foreach (var primitive in primitives)
            {
                var owner = primitives.ElementAtOrDefault(primitive.OwnerIndex);
                if (owner == primitive) owner = null;
                primitive.Owner = owner;

                var containingList = owner?.Primitives ?? rootList;
                if (containingList != primitive.Primitives)
                {
                    containingList?.Add(primitive);
                }
            }
        }
    }
}
