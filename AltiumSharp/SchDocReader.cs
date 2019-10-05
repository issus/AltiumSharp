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
    /// Schematic document reader.
    /// </summary>
    public sealed class SchDocReader : CompoundFileReader
    {
        /// <summary>
        /// Schematic root document.
        /// </summary>
        public SchPrimitive Root { get; private set; }

        /// <summary>
        /// Mapping of image file names to the actual image data for
        /// embedded images.
        /// </summary>
        public Dictionary<string, Image> EmbeddedImages { get; }

        public SchDocReader(string fileName) : base(fileName)
        {
            EmbeddedImages = new Dictionary<string, Image>();
        }

        protected override void DoClear()
        {
            Root = null;
            EmbeddedImages.Clear();
        }

        protected override void DoReadSectionKeys(Dictionary<string, string> sectionKeys)
        {
        }

        protected override void DoRead()
        {
            ReadStorage();
            ReadFileHeader();
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
        /// Reads the "FileHeader" section which contains the primitives that
        /// exist in the current document.
        /// </summary>
        /// <returns></returns>
        private void ReadFileHeader()
        {
            BeginContext("FileHeader");

            using (var reader = Cf.GetStream("FileHeader").GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var weight = parameters["WEIGHT"].AsIntOrDefault();

                ReadPrimitives(reader);
            }

            EndContext();
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

        private void ReadPrimitives(BinaryReader reader)
        {
            BeginContext("ReadPrimitives");

            Root = new SchPrimitive();
            Root.Record = -1;

            var primitives = new List<SchPrimitive>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var primitiveStartPosition = reader.BaseStream.Position;
                var primitive = ReadRecord(reader,
                    size => ReadAsciiRecord(reader, size),
                    size => ReadPinRecord(reader, size));

                primitive.SetRawData(ExtractStreamData(reader, primitiveStartPosition, reader.BaseStream.Position));

                /*
                if (Sheet == null)
                {
                    // First primitive read must be the board Record31
                    AssertValue(nameof(primitive), primitive.GetType().Name, typeof(SheetRecord).Name);
                    Sheet = (SheetRecord)primitive;
                }
                */
                primitives.Add(primitive);
            }

            AssignOwners(primitives);

            EndContext();
        }

        // TODO: Unify with SchLibReader
        internal void AssignOwners(List<SchPrimitive> primitives)
        {
            foreach (var primitive in primitives)
            {
                var owner = primitives.ElementAtOrDefault(primitive.OwnerIndex);
                if (owner != null)
                {
                    primitive.Owner = owner;
                }
                else
                {
                    owner = Root;
                }
                owner.Primitives.Add(primitive);
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
                case 17:
                    record = new PowerPortRecord();
                    break;
                case 25:
                    record = new NetLabelRecord();
                    break;
                case 27:
                    record = new WireRecord();
                    break;
                case 29:
                    record = new JunctionRecord();
                    break;
                case 28:
                case 209:
                    record = new TextFrameRecord();
                    break;
                case 30:
                    record = new ImageRecord();
                    break;
                case 31:
                    record = new SheetRecord();
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

        private PinRecord ReadPinRecord(BinaryReader reader, int size)
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

            EndContext();

            return pin;
        }
    }
}
