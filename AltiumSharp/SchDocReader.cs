using System.Collections.Generic;
using System.Linq;
using AltiumSharp.Records;

namespace AltiumSharp
{
    /// <summary>
    /// Schematic document reader.
    /// </summary>
    public sealed class SchDocReader : SchReader
    {
        /// <summary>
        /// Header information for the schematic document file.
        /// </summary>
        public SheetRecord Header { get; private set; }

        /// <summary>
        /// List of primitives read from the file.
        /// </summary>
        public List<SchPrimitive> Primitives { get; private set; }

        public SchDocReader(string fileName) : base(fileName)
        {
        }

        protected override void DoReadSectionKeys(Dictionary<string, string> sectionKeys)
        {
        }

        protected override void DoClearSch()
        {
            Header = null;
            Primitives = null;
        }

        protected override void DoReadSch()
        {
            ReadFileHeader();
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

                Primitives = ReadPrimitives(reader, null, null, null);

                Header = Primitives.OfType<SheetRecord>().Single();

                AssignOwners(Primitives, null);
            }

            EndContext();
        }
    }
}
