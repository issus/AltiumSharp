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
    /// Schematic library reader.
    /// </summary>
    public sealed class SchLibReader : SchReader
    {
        /// <summary>
        /// Header information for the schematic library file.
        /// </summary>
        public SchLibHeader Header { get; private set; }

        /// <summary>
        /// List of component symbols read from the file.
        /// </summary>
        public List<SchComponent> Components { get; }

        public SchLibReader(string fileName) : base(fileName)
        {
            Components = new List<SchComponent>();
        }

        protected override void DoReadSectionKeys(Dictionary<string, string> sectionKeys)
        {
            var data = Cf.TryGetStream("SectionKeys");
            if (data == null) return;

            BeginContext("SectionKeys");

            using (var reader = data.GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var keyCount = parameters["KEYCOUNT"].AsIntOrDefault();
                for (int i = 0; i < keyCount; ++i)
                {
                    var libRef = parameters[$"LIBREF{i}"].AsString();
                    var sectionKey = parameters[$"SECTIONKEY{i}"].AsString();
                    sectionKeys.Add(libRef, sectionKey);
                }
            }

            EndContext();
        }

        protected override void DoClearSch()
        {
            Header = null;
            Components.Clear();
        }

        protected override void DoReadSch()
        {
            var refNames = ReadFileHeader();

            foreach (var componentRefName in refNames)
            {
                var sectionKey = GetSectionKeyFromRefName(componentRefName);
                Components.Add(ReadComponent(sectionKey));
            }
        }

        /// <summary>
        /// Reads the "FileHeader" section which contains the list of components that
        /// exist in the current library file.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> ReadFileHeader()
        {
            var refNames = new List<string>();

            BeginContext("FileHeader");

            using (var reader = Cf.GetStream("FileHeader").GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                Header = new SchLibHeader();
                Header.ImportFromParameters(parameters);

                if (reader.BaseStream.Position == reader.BaseStream.Length)
                {
                    // If we're at the end of the stream then read components
                    // from the parameters list
                    refNames.AddRange(Header.Comp.Select(c => c.LibRef));
                }
                else
                {
                    // Otherwise we can read the binary list of components
                    var count = reader.ReadUInt32();
                    for (var i = 0; i < count; ++i)
                    {
                        var componentRefName = ReadStringBlock(reader);
                        refNames.Add(componentRefName);
                    }
                }
            }

            EndContext();

            return refNames;
        }

        /// <summary>
        /// Reads a component stored in the <paramref name="resourceName"/> section key
        /// in the current file.
        /// </summary>
        /// <param name="resourceName">
        /// Section key where to look for the schematic component symbol data.
        /// </param>
        /// <returns>Component instance.</returns>
        private SchComponent ReadComponent(string resourceName)
        {
            var componentStorage = Cf.TryGetStorage(resourceName) ?? throw new ArgumentException($"Symbol resource not found: {resourceName}");

            BeginContext(resourceName);

            var pinsWideText = ReadPinWideText(componentStorage);
            var pinsTextData = ReadPinTextData(componentStorage);
            var pinsSymbolLineWidth = ReadPinSymbolLineWidth(componentStorage);

            using (var reader = componentStorage.GetStream("Data").GetBinaryReader())
            {
                var primitives = ReadPrimitives(reader, pinsWideText, pinsTextData, pinsSymbolLineWidth).ToList();

                // First primitive read must be the component SchComponent
                var component = (SchComponent)primitives.First();

                AssignOwners(primitives, component.Primitives);

                EndContext();

                return component;
            }
        }

        /// <summary>
        /// Reads a pin text data for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private Dictionary<int, byte[]> ReadPinTextData(CFStorage componentStorage)
        {
            var storage = componentStorage.TryGetStream("PinTextData");
            if (storage == null) return null;

            BeginContext("PinTextData");

            var result = new Dictionary<int, byte[]>();
            using (var reader = storage.GetBinaryReader())
            {
                var parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = parameters["HEADER"].AsStringOrDefault();
                var weight = parameters["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "PinTextData");

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (id, data) = ReadCompressedStorage(reader);
                    result.Add(int.Parse(id, CultureInfo.InvariantCulture), data);
                }
                CheckValue(nameof(weight), weight, result.Count);
            }

            EndContext();

            return result;
        }

        /// <summary>
        /// Reads a pin Unicode text for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private Dictionary<int, ParameterCollection> ReadPinWideText(CFStorage componentStorage)
        {
            var storage = componentStorage.TryGetStream("PinWideText");
            if (storage == null) return null;

            BeginContext("PinTextData");

            var result = new Dictionary<int, ParameterCollection>();
            using (var reader = storage.GetBinaryReader())
            {
                var headerParams = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = headerParams["HEADER"].AsStringOrDefault();
                var weight = headerParams["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "PinWideText");

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (id, parameters) = ReadCompressedStorage(reader, stream =>
                    {
                        using (var r = new BinaryReader(stream))
                        {
                            return ReadBlock(r, s => ReadParameters(r, s, true, Encoding.Unicode));
                        }
                    });
                    result.Add(int.Parse(id, CultureInfo.InvariantCulture), parameters);
                }
                CheckValue(nameof(weight), weight, result.Count);
            }

            EndContext();

            return result;
        }

        /// <summary>
        /// Reads a pin line widths for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private Dictionary<int, ParameterCollection> ReadPinSymbolLineWidth(CFStorage componentStorage)
        {
            var storage = componentStorage.TryGetStream("PinSymbolLineWidth");
            if (storage == null) return null;

            BeginContext("PinTextData");

            var result = new Dictionary<int, ParameterCollection>();
            using (var reader = storage.GetBinaryReader())
            {
                var headerParams = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = headerParams["HEADER"].AsStringOrDefault();
                var weight = headerParams["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "PinSymbolLineWidth");

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (id, parameters) = ReadCompressedStorage(reader, stream =>
                    {
                        using (var r = new BinaryReader(stream))
                        {
                            return ReadBlock(r, s => ReadParameters(r, s, true, Encoding.Unicode));
                        }
                    });
                    result.Add(int.Parse(id, CultureInfo.InvariantCulture), parameters);
                }
                CheckValue(nameof(weight), weight, result.Count);
            }

            EndContext();

            return result;
        }
    }
}
