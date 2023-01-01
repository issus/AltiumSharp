using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;

namespace OriginalCircuit.AltiumSharp
{
    public abstract class SchWriter<TData> : CompoundFileWriter<TData>
        where TData : SchData, new()
    {
        protected SchWriter() : base()
        {

        }

        protected IDictionary<string, Image> GetEmbeddedImages(IEnumerable<SchPrimitive> dataItems)
        {
            return dataItems
                .SelectMany(item => item.GetPrimitivesOfType<SchImage>().Where(p => p.EmbedImage))
                .Select(p => (p.Filename, p.Image))
                .GroupBy(p => p.Filename).Select(g => g.First()) // only one image per file name
                .ToDictionary(kv => kv.Filename, kv => kv.Image);
        }

        /// <summary>
        /// Writes embedded images from the "Storage" section of the file.
        /// </summary>
        protected void WriteStorageEmbeddedImages(IDictionary<string, Image> embeddedImages)
        {
            byte[] ImageToBytes(Image image)
            {
                // we need to make a copy of the image because otherwise Save() fails with a generic GDI+ error
                // Source: https://social.microsoft.com/Forums/en-US/b15357f1-ad9d-4c80-9ec1-92c786cca4e6/bitmapsave-a-generic-error-occurred-in-gdi#c780991d-50c3-4bdc-8c90-a5474f4739a2
                using (var stream = new MemoryStream())
                using (var bitmap = new Bitmap(image))
                {
                    bitmap.Save(stream, ImageFormat.Bmp);
                    return stream.ToArray();
                }
            }

            Cf.RootStorage.GetOrAddStream("Storage").Write(writer =>
            {
                var parameters = new ParameterCollection
                {
                    { "HEADER", "Icon storage" },
                    { "WEIGHT", embeddedImages.Count }
                };
                WriteBlock(writer, w => WriteParameters(w, parameters));

                foreach (var ei in embeddedImages)
                {
                    WriteCompressedStorage(writer, ei.Key, ImageToBytes(ei.Value));
                }
            });
        }

        protected static void WritePrimitive(BinaryWriter writer, SchPrimitive primitive, bool pinAsBinary,
            int ownerIndex, ref int index, ref int pinIndex,
            Dictionary<int, (int x, int y, int length)> pinsFrac,
            Dictionary<int, ParameterCollection> pinsWideText, Dictionary<int, byte[]> pinsTextData,
            Dictionary<int, ParameterCollection> pinsSymbolLineWidth)
        {
            if (primitive == null)
                throw new ArgumentNullException(nameof(primitive));

            if (pinsSymbolLineWidth == null)
                throw new ArgumentNullException(nameof(pinsSymbolLineWidth));

            primitive.OwnerIndex = ownerIndex;

            if (pinAsBinary && primitive is SchPin pin)
            {
                WritePinRecord(writer, pin, out var pinFrac, out var pinWideText, out var pinTextData, out var pinSymbolLineWidth);

                if (pinFrac.x != 0 || pinFrac.y != 0 || pinFrac.length != 0)
                {
                    pinsFrac?.Add(pinIndex, pinFrac);
                }
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

            foreach (var child in primitive.GetAllPrimitives())
            {
                WritePrimitive(writer, child, pinAsBinary, currentIndex, ref index, ref pinIndex,
                    pinsFrac, pinsWideText, pinsTextData, pinsSymbolLineWidth);
            }
        }

        /// <summary>
        /// Writes an ASCII parameter based Record entry. 
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="primitive">Primitive to be serialized as a record.</param>
        protected static void WriteAsciiRecord(BinaryWriter writer, SchPrimitive primitive)
        {
            if (primitive == null)
                throw new ArgumentNullException(nameof(primitive));
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

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
        protected static void WritePinRecord(BinaryWriter writer, SchPin pin, out (int x, int y, int length) pinFrac,
            out ParameterCollection pinWideText, out byte[] pinTextData, out ParameterCollection pinSymbolLineWidth)
        {
            if (pin == null)
                throw new ArgumentNullException(nameof(pin));
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var pinLocationX = Utils.CoordToDxpFrac(pin.Location.X);
            var pinLocationY = Utils.CoordToDxpFrac(pin.Location.Y);
            var pinLength = Utils.CoordToDxpFrac(pin.PinLength);

            WriteBlock(writer, w =>
            {
                pin.SymbolLineWidth = LineWidth.Smallest;

                w.Write(pin.Record);
                w.Write((byte)0); // TODO: unknown
                w.Write((short)pin.OwnerPartId);
                w.Write((byte)pin.OwnerPartDisplayMode);
                w.Write((byte)pin.SymbolInnerEdge);
                w.Write((byte)pin.SymbolOuterEdge);
                w.Write((byte)pin.SymbolInside);
                w.Write((byte)pin.SymbolOutside);
                WritePascalShortString(w, pin.Description);
                w.Write((byte)0); // TODO: unknown
                w.Write((byte)pin.Electrical);
                w.Write((byte)pin.PinConglomerate);
                w.Write((short)pinLength.num);
                w.Write((short)pinLocationX.num);
                w.Write((short)pinLocationY.num);
                w.Write(ColorTranslator.ToWin32(pin.Color));
                WritePascalShortString(w, pin.Name);
                WritePascalShortString(w, pin.Designator);
                WritePascalShortString(w, pin.SwapIdGroup);
                var partAndSequence = string.Empty;
                if (!(pin.SwapIdPart == 0 && string.IsNullOrEmpty(pin.SwapIdSequence)))
                {
                    partAndSequence = pin.SwapIdPart != 0
                        ? $"{pin.SwapIdPart}|&|{pin.SwapIdSequence}"
                        : $"|&|{pin.SwapIdSequence}";
                }
                WritePascalShortString(w, partAndSequence);
                WritePascalShortString(w, pin.DefaultValue);
            }, (byte)0x01); // flag needs to be 1

            pinFrac = (pinLocationX.frac, pinLocationY.frac, pinLength.frac);

            pinWideText = new ParameterCollection();
            if (!IsAscii(pin.Description)) pinWideText.Add("DESC", pin.Description);
            if (!IsAscii(pin.Name)) pinWideText.Add("NAME", pin.Name);
            if (!IsAscii(pin.Designator)) pinWideText.Add("DESIG", pin.Designator);

            pinTextData = Array.Empty<byte>();

            pinSymbolLineWidth = new ParameterCollection
            {
                { "SYMBOL_LINEWIDTH", pin.SymbolLineWidth }
            };
        }
    }
}
