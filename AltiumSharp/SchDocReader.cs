namespace OriginalCircuit.AltiumSharp
{
    /// <summary>
    /// Schematic document reader.
    /// </summary>
    public sealed class SchDocReader : SchReader<SchDoc>
    {
        public SchDocReader() : base()
        {
        }

        protected override void DoRead()
        {
            ReadFileHeader();

            var embeddedImages = ReadStorageEmbeddedImages();
            SetEmbeddedImages(Data.Items, embeddedImages);
        }

        /// <summary>
        /// Reads the "FileHeader" section which contains the primitives that
        /// exist in the current document.
        /// </summary>
        private void ReadFileHeader()
        {
            BeginContext("FileHeader");

            using (var reader = Cf.GetStream("FileHeader").GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var weight = parameters["WEIGHT"].AsIntOrDefault();

                var primitives = ReadPrimitives(reader, null, null, null, null);
                Data.Items.AddRange(primitives);

                AssignOwners(primitives);
            }

            EndContext();
        }
    }
}
