using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp
{
    /// <summary>
    /// Schematic library reader.
    /// </summary>
    public sealed class SchLibReader : SchReader<SchLib>
    {
        public SchLibReader() : base()
        {

        }

        protected override void DoRead()
        {
            ReadSectionKeys();
            var refNames = ReadFileHeader();

            foreach (var componentRefName in refNames)
            {
                var sectionKey = GetSectionKeyFromRefName(componentRefName);
                Data.Items.Add(ReadComponent(sectionKey));
            }

            var embeddedImages = ReadStorageEmbeddedImages();
            SetEmbeddedImages(Data.Items, embeddedImages);
        }

        /// <summary>
        /// Reads section keys information which can be used to match "ref lib" component names into
        /// usable compound storage section names.
        /// <para>
        /// Data read can be accessed through the <see cref="GetSectionKeyFromRefName"/> method.
        /// </para>
        /// </summary>
        private void ReadSectionKeys()
        {
            SectionKeys.Clear();

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
                    SectionKeys.Add(libRef, sectionKey);
                }
            }

            EndContext();
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
                Data.Header.ImportFromParameters(parameters);

                if (reader.BaseStream.Position == reader.BaseStream.Length)
                {
                    // If we're at the end of the stream then read components
                    // from the parameters list
                    var comps = Enumerable.Range(0, parameters["COMPCOUNT"].AsIntOrDefault())
                        .Select(i => parameters[$"LIBREF{i}"].AsStringOrDefault());
                    refNames.AddRange(comps);
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

            var pinsFrac = ReadPinFrac(componentStorage);
            var pinsWideText = ReadPinWideText(componentStorage);
            var pinsTextData = ReadPinTextData(componentStorage);
            var pinsSymbolLineWidth = ReadPinSymbolLineWidth(componentStorage);

            using (var reader = componentStorage.GetStream("Data").GetBinaryReader())
            {
                var primitives = ReadPrimitives(reader, pinsFrac, pinsWideText, pinsTextData, pinsSymbolLineWidth).ToList();

                // First primitive read must be the component SchComponent
                var component = (SchComponent)primitives.First();

                AssignOwners(primitives);

                EndContext();

                return component;
            }
        }

        /// <summary>
        /// Reads pin fractional locations for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private Dictionary<int, (int x, int y, int length)> ReadPinFrac(CFStorage componentStorage)
        {
            if (!componentStorage.TryGetStream("PinFrac", out var storage)) return null;

            BeginContext("PinFrac");

            var result = new Dictionary<int, (int, int, int)>();
            using (var reader = storage.GetBinaryReader())
            {
                var headerParams = ReadBlock(reader, size => ReadParameters(reader, size));
                var header = headerParams["HEADER"].AsStringOrDefault();
                var weight = headerParams["WEIGHT"].AsIntOrDefault();
                AssertValue(nameof(header), header, "PinFrac");

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var (id, (fracX, fracY, fracLength)) = ReadCompressedStorage(reader, stream =>
                    {
                        using (var r = new BinaryReader(stream))
                        {
                            var x = r.ReadInt32();
                            var y = r.ReadInt32();
                            var l = r.ReadInt32();
                            return (x, y, l);
                        }
                    });
                    result.Add(int.Parse(id, CultureInfo.InvariantCulture), (fracX, fracY, fracLength));
                }
                CheckValue(nameof(weight), weight, result.Count);
            }

            EndContext();

            return result;
        }

        /// <summary>
        /// Reads a pin text data for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private Dictionary<int, byte[]> ReadPinTextData(CFStorage componentStorage)
        {
            if (!componentStorage.TryGetStream("PinTextData", out var storage)) return null;

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
            if (!componentStorage.TryGetStream("PinWideText", out var storage)) return null;

            BeginContext("PinWideText");

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
            if (!componentStorage.TryGetStream("PinSymbolLineWidth", out var storage)) return null;

            BeginContext("PinSymbolLineWidth");

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
