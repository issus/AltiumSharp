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
    public abstract class SchWriter : CompoundFileWriter
    {
        protected SchWriter(string fileName) : base(fileName)
        {
            EmbeddedImages = new Dictionary<string, Image>();
        }

        /// <summary>
        /// Mapping of image file names to the actual image data for
        /// embedded images.
        /// </summary>
        public Dictionary<string, Image> EmbeddedImages { get; }

        protected sealed override void DoWrite()
        {
            DoWriteSch();
            WriteStorageEmbeddedImages();
        }

        protected abstract void DoWriteSch();

        /// <summary>
        /// Writes embedded images from the "Storage" section of the file.
        /// </summary>
        protected void WriteStorageEmbeddedImages()
        {
            Cf.RootStorage.GetOrAddStream("Storage").Write(writer =>
            {
                var parameters = new ParameterCollection
                {
                    { "HEADER", "Icon storage" },
                    { "WEIGHT", EmbeddedImages.Count }
                };
                WriteBlock(writer, w => WriteParameters(w, parameters));

                foreach (var ei in EmbeddedImages)
                {
                    using (var imageData = new MemoryStream())
                    {
                        ei.Value.Save(imageData, ei.Value.RawFormat);
                        WriteCompressedStorage(writer, ei.Key, imageData.ToArray());
                    }
                }
            });
        }

        private static void WritePrimitive(BinaryWriter writer, SchPrimitive primitive,
            int ownerIndex, ref int index, ref int pinIndex,
            Dictionary<int, ParameterCollection> pinsWideText, Dictionary<int, byte[]> pinsTextData,
            Dictionary<int, ParameterCollection> pinsSymbolLineWidth)
        {
            primitive.OwnerIndex = ownerIndex;

            if (primitive is SchPin pin)
            {
                WritePinRecord(writer, pin, out var pinWideText, out var pinTextData, out var pinSymbolLineWidth);

                if (pinTextData?.Length > 0)
                {
                    pinsTextData?.Add(pinIndex, pinTextData);
                }
                if (pinWideText?.Count > 0)
                {
                    pinsWideText?.Add(pinIndex, pinWideText);
                }
                if (pinSymbolLineWidth?.Count > 0)
                {
                    pinsSymbolLineWidth.Add(pinIndex, pinSymbolLineWidth);
                }

                pinIndex++;
            }
            else
            {
                WriteAsciiRecord(writer, primitive);
            }

            var currentIndex = index++;

            foreach (var child in primitive.Primitives)
            {
                WritePrimitive(writer, child, currentIndex, ref index, ref pinIndex,
                    pinsWideText, pinsTextData, pinsSymbolLineWidth);
            }
        }

        protected static void WriteComponentPrimitives(BinaryWriter writer, SchComponent component,
            Dictionary<int, ParameterCollection> pinsWideText, Dictionary<int, byte[]> pinsTextData,
            Dictionary<int, ParameterCollection> pinsSymbolLineWidth)
        {
            var index = 0;
            var pinIndex = 0;
            WritePrimitive(writer, component, 0, ref index, ref pinIndex, pinsWideText, pinsTextData, pinsSymbolLineWidth);
        }

        /// <summary>
        /// Writes an ASCII parameter based Record entry. 
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="primitive">Primitive to be serialized as a record.</param>
        protected static void WriteAsciiRecord(BinaryWriter writer, SchPrimitive primitive)
        {
            var parameters = primitive.ExportToParameters();
            WriteBlock(writer, w => WriteParameters(w, parameters), 0);
        }

        /// <summary>Checks if the string can be represented as ASCII</summary>
        private static bool IsAscii(string s) => s == null || Encoding.UTF8.GetByteCount(s) == s.Length;

        /// <summary>
        /// Writes a binary SchPin Record entry. 
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="pin">Pin primitive to be serialized as a record.</param>
        protected static void WritePinRecord(BinaryWriter writer, SchPin pin, out ParameterCollection pinWideText, out byte[] pinTextData, out ParameterCollection pinSymbolLineWidth)
        {
            WriteBlock(writer, w =>
            {
                pin.SymbolLineWidth = LineWidth.Smallest;

                w.Write(pin.Record);
                w.Write((byte)0); // TODO: unknown
                w.Write((short)pin.OwnerPartId);
                w.Write((byte)0); // TODO: unknown
                w.Write((byte)pin.SymbolInnerEdge);
                w.Write((byte)pin.SymbolOuterEdge);
                w.Write((byte)pin.SymbolInside);
                w.Write((byte)pin.SymbolOutside);
                WritePascalShortString(w, pin.Description);
                w.Write((byte)0); // TODO: unknown
                w.Write((byte)pin.Electrical);
                w.Write((byte)pin.PinConglomerate);
                w.Write((short)Utils.CoordToDxpFrac(pin.PinLength).num);
                w.Write((short)Utils.CoordToDxpFrac(pin.Location.X).num);
                w.Write((short)Utils.CoordToDxpFrac(pin.Location.Y).num);
                w.Write(ColorTranslator.ToWin32(pin.Color));
                WritePascalShortString(w, pin.Name);
                WritePascalShortString(w, pin.Designator);
                w.Write((byte)0); // TODO: unknown
                w.Write((byte)0); // TODO: unknown
                w.Write((byte)0); // TODO: unknown
            }, (byte)0x01); // flag needs to be 1

            pinWideText = new ParameterCollection();
            if (!IsAscii(pin.Description)) pinWideText.Add("DESC", pin.Description);
            if (!IsAscii(pin.Name)) pinWideText.Add("NAME", pin.Name);
            if (!IsAscii(pin.Designator)) pinWideText.Add("DESIG", pin.Designator);
            
            pinTextData = new byte[0];

            pinSymbolLineWidth = new ParameterCollection
            {
                { "SYMBOL_LINEWIDTH", pin.SymbolLineWidth }
            };
        }
    }
}
