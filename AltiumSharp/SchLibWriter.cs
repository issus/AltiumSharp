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
    /// Schematic library reader.
    /// </summary>
    public sealed class SchLibWriter : SchWriter
    {
        /// <summary>
        /// Header information for the schematic library file.
        /// </summary>
        public SchLibHeader Header { get; set; }

        /// <summary>
        /// List of component to be serialized to the file.
        /// </summary>
        public List<SchComponent> Components { get; }

        public SchLibWriter(string fileName) : base(fileName)
        {
            Components = new List<SchComponent>();
        }

        protected override void DoWriteSch()
        {
            WriteFileHeader();
            WriteSectionKeys();

            foreach (var component in Components)
            {
                WriteComponent(component);
            }
        }

        private void WriteSectionKeys()
        {
            // only write section keys for components that need them
            var components = Components.Where(c => GetSectionKeyFromRefName(c.Name) != c.Name).ToList();
            if (components.Count == 0) return;

            var parameters = new ParameterCollection
            {
                { "KEYCOUNT", components.Count }
            };

            for (int i = 0; i < components.Count; ++i)
            {
                var component = components[i];
                var componentRefName = component.Name;
                var sectionKey = GetSectionKeyFromRefName(componentRefName);

                parameters.Add($"LIBREF{i}", componentRefName);
                parameters.Add($"SECTIONKEY{i}", sectionKey);
            }

            Cf.RootStorage.GetOrAddStream("SectionKeys").Write(writer =>
            {
                WriteBlock(writer, w => WriteParameters(w, parameters));
            });
        }

        /// <summary>
        /// Writes the "FileHeader" section which contains the list of components that
        /// exist in the current library file.
        /// </summary>
        /// <returns></returns>
        private void WriteFileHeader()
        {
            Cf.RootStorage.GetOrAddStream("FileHeader").Write(writer =>
            {
                // add the components to the header
                Header.Comp.Clear();
                Header.Comp.AddRange(Components.Select(c => (c.Name, c.Description, c.PartCount + 1)));

                // write header
                var parameters = Header.ExportToParameters();
                WriteBlock(writer, w => WriteParameters(w, parameters));

                // write the binary list of component ref names
                writer.Write(Components.Count);
                foreach (var component in Components)
                {
                    WriteStringBlock(writer, component.Name);
                }
            });
        }

        /// <summary>
        /// Writes a component to the adequate section key in the current file.
        /// </summary>
        /// <param name="component">Component to be serialized.</param>
        private void WriteComponent(SchComponent component)
        {
            var resourceName = GetSectionKeyFromRefName(component.Name);
            var componentStorage = Cf.RootStorage.GetOrAddStorage(resourceName);

            var pinsWideText = new Dictionary<int, ParameterCollection>();
            var pinsTextData = new Dictionary<int, byte[]>();
            var pinsSymbolLineWidth = new Dictionary<int, ParameterCollection>();

            component.AllPinCount = component.GetPrimitivesOfType<SchPin>().Count();

            componentStorage.GetOrAddStream("Data").Write(writer =>
            {
                WriteComponentPrimitives(writer, component, pinsWideText, pinsTextData, pinsSymbolLineWidth);
            });

            WritePinTextData(componentStorage, pinsTextData);
            WriteComponentExtendedParameters(componentStorage, "PinWideText", pinsWideText);
            WriteComponentExtendedParameters(componentStorage, "PinSymbolLineWidth", pinsSymbolLineWidth);
        }

        /// <summary>
        /// Writes a pin text data for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private void WritePinTextData(CFStorage componentStorage, Dictionary<int, byte[]> data)
        {
            if (data.Count == 0) return;

            componentStorage.GetOrAddStream("PinTextData").Write(writer =>
            {
                var parameters = new ParameterCollection
                {
                    { "HEADER", "PinTextData" },
                    { "WEIGHT", data.Count }
                };
                WriteBlock(writer, w => WriteParameters(w, parameters));

                foreach (var kv in data)
                {
                    WriteCompressedStorage(writer, kv.Key.ToString(), kv.Value);
                }
            });
        }

        /// <summary>
        /// Writes a component extended list of parameter at <paramref name="componentStorage"/>.
        /// </summary>
        /// <param name="componentStorage">Component storage.</param>
        /// <param name="streamName">Name of the stream inside the component storage.</param>
        /// <param name="data">Key, value set of parameters to store.</param>
        private void WriteComponentExtendedParameters(CFStorage componentStorage, string streamName, Dictionary<int, ParameterCollection> data)
        {
            if (data.Count == 0) return;

            componentStorage.GetOrAddStream(streamName).Write(writer =>
            {
                var parameters = new ParameterCollection
                {
                    { "HEADER", streamName },
                    { "WEIGHT", data.Count }
                };
                WriteBlock(writer, w => WriteParameters(w, parameters));

                foreach (var kv in data)
                {
                    WriteCompressedStorage(writer, kv.Key.ToString(), ws =>
                        WriteBlock(ws, wb => WriteParameters(wb, kv.Value, true, Encoding.Unicode, false)));
                }
            });
        }
    }
}
