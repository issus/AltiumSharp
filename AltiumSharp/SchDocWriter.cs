using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Schematic document writer.
    /// </summary>
    public sealed class SchDocWriter : SchWriter<SchDoc>
    {
        public SchDocWriter() : base()
        {

        }

        protected override void DoWriteSch()
        {
            WriteFileHeader();
            WriteAdditional();
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
            WritePrimitive(writer, Data.Header, false, 0, ref index, ref pinIndex, null, null, null);
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
