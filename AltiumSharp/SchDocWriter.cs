using System.IO;
using OriginalCircuit.AltiumSharp.BasicTypes;

namespace OriginalCircuit.AltiumSharp
{
    /// <summary>
    /// Schematic document writer.
    /// </summary>
    public sealed class SchDocWriter : SchWriter<SchDoc>
    {
        public SchDocWriter() : base()
        {

        }

        protected override void DoWrite(string fileName)
        {
            WriteFileHeader();
            WriteAdditional();

            var embeddedImages = GetEmbeddedImages(Data.Items);
            WriteStorageEmbeddedImages(embeddedImages);
        }

        /// <summary>
        /// Writes the "FileHeader" section which contains the primitives that
        /// exist in the current document.
        /// </summary>
        private void WriteFileHeader()
        {
            Cf.RootStorage.GetOrAddStream("FileHeader").Write(writer =>
            {
                var parameters = new ParameterCollection
                {
                    { "HEADER", "Protel for Windows - Schematic Capture Binary File Version 5.0" },
                    { "WEIGHT", Data.Items.Count }
                };
                WriteBlock(writer, w => WriteParameters(w, parameters));

                WritePrimitives(writer);
            });
        }

        private void WritePrimitives(BinaryWriter writer)
        {
            var index = 0;
            var pinIndex = 0;
            WritePrimitive(writer, Data.Header, false, 0, ref index, ref pinIndex,
                null, null, null, null);
        }

        private void WriteAdditional()
        {
            Cf.RootStorage.GetOrAddStream("Additional").Write(writer =>
            {
                var parameters = new ParameterCollection
                {
                    { "HEADER", "Protel for Windows - Schematic Capture Binary File Version 5.0" }
                };
                WriteBlock(writer, w => WriteParameters(w, parameters));
            });
        }
    }
}
