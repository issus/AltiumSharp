﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;
using OpenMcdf;

namespace AltiumSharp
{
    /// <summary>
    /// PCB footprint library file writer.
    /// </summary>
    public sealed class PcbLibWriter : CompoundFileWriter<PcbLib>
    {
        public PcbLibWriter() : base()
        {

        }

        protected override void DoWrite(string fileName)
        {
            Data.Header.Filename = fileName;

            WriteFileHeader();
            WriteSectionKeys();
            WriteLibrary();
        }

        private void WriteSectionKeys()
        {
            // only write section keys for components that need them
            var components = Data.Items.Where(c => GetSectionKeyFromRefName(c.Name) != c.Name).ToList();
            if (components.Count == 0) return;

            Cf.RootStorage.GetOrAddStream("SectionKeys").Write(writer =>
            {
                writer.Write(components.Count);
                foreach (var component in components)
                {
                    WritePascalString(writer, component.Name);
                    WriteStringBlock(writer, GetSectionKeyFromRefName(component.Name));
                }
            });
        }

        private void WriteFileHeader()
        {
            Cf.RootStorage.GetOrAddStream("FileHeader").Write(writer =>
            {
                // for some reason this is different than a StringBlock as the
                // initial block length is the same as the short string length
                var pcbBinaryFileVersionText = "PCB 6.0 Binary Library File";
                writer.Write(pcbBinaryFileVersionText.Length);
                WritePascalShortString(writer, pcbBinaryFileVersionText);
            });
        }

        /// <summary>
        /// Main method that writes the contents of the PCB library file.
        /// </summary>
        private void WriteLibrary()
        {
            var library = Cf.RootStorage.GetOrAddStorage("Library");
            WriteHeader(library, 1);
            WriteLibraryModels(library);
            WriteLibraryData(library);
        }

        /// <summary>
        /// Writes the library data from the current file which contains the PCB library
        /// header information parameters and also a list of the existing components.
        /// </summary>
        /// <param name="library"></param>
        private void WriteLibraryData(CFStorage library)
        {
            library.GetOrAddStream("Data").Write(writer =>
            {
                var parameters = Data.Header.ExportToParameters();
                WriteBlock(writer, w => WriteParameters(w, parameters));

                writer.Write(Data.Items.Count);
                foreach (var component in Data.Items)
                {
                    WriteStringBlock(writer, component.Name);
                    WriteFootprint(component);
                }
            });
        }

        /// <summary>
        /// Writes a PCB component footprint.
        /// </summary>
        /// <param name="footprint">
        /// Component footsprint to be serialized.
        /// </param>
        private void WriteFootprint(PcbComponent component)
        {
            var sectionKey = GetSectionKeyFromRefName(component.Name);
            var footprintStorage = Cf.RootStorage.GetOrAddStorage(sectionKey);

            var primitives = component.Primitives.Where(p => !(p is PcbUnknown));

            WriteHeader(footprintStorage, primitives.Count()); // record count
            WriteFootprintParameters(footprintStorage, component);

            footprintStorage.GetOrAddStream("Data").Write(writer =>
            {
                WriteStringBlock(writer, component.Name);

                foreach (var primitive in primitives) 
                {
                    writer.Write((byte)primitive.ObjectId);
                    switch (primitive)
                    {
                        case PcbArc arc:
                            WriteFootprintArc(writer, arc);
                            break;

                        case PcbPad pad:
                            WriteFootprintPad(writer, pad);
                            break;

                        case PcbTrack track:
                            WriteFootprintTrack(writer, track);
                            break;

                        case PcbString text:
                            WriteFootprintString(writer, text);
                            break;

                        case PcbRectangle rectangle:
                            WriteFootprintRectangle(writer, rectangle);
                            break;

                        case PcbPolygon polygon:
                            WriteFootprintPolygon(writer, polygon);
                            break;

                        //case PcbPrimitiveObjectId.Via:
                        //case PcbPrimitiveObjectId.ComponentBody:
                        default:
                            // otherwise we attempt to skip the actual primitive data but still
                            // create a basic instance with just the raw data for debugging
                            //element = SkipPrimitive(reader);
                            break;
                    }
                }
            });

            WriteWideStrings(footprintStorage, component);
            WriteUniqueIdPrimitiveInformation(footprintStorage, component);
        }

        /// <summary>
        /// Writes the component parameter information.
        /// </summary>
        /// <param name="componentStorage">Component footprint storage key.</param>
        /// <param name="component">Component instance to have its parameters serialized.</param>
        private void WriteFootprintParameters(CFStorage componentStorage, PcbComponent component)
        {
            var parameters = component.ExportToParameters();

            componentStorage.GetOrAddStream("Parameters").Write(writer =>
            {
                WriteBlock(writer, w => WriteParameters(w, parameters));
            });
        }

        private static void WriteUniqueIdPrimitiveInformation(CFStorage componentStorage, PcbComponent component)
        {
            var uniqueIdPrimitiveInformation = componentStorage.GetOrAddStorage("UniqueIdPrimitiveInformation");

            var primitives = component.Primitives.Where(p => !(p is PcbUnknown)).ToList();
            WriteHeader(uniqueIdPrimitiveInformation, primitives.Count);

            uniqueIdPrimitiveInformation.GetOrAddStream("Data").Write(writer =>
            {
                for (int i = 0; i < primitives.Count; ++i)
                {
                    var primitive = primitives[i];
                    var parameters = new ParameterCollection
                    {
                        { "PRIMITIVEINDEX", i },
                        { "PRIMITIVEOBJECTID", primitive.ObjectId.ToString() },
                        { "UNIQUEID", primitive.UniqueId }
                    };
                    WriteBlock(writer, w => WriteParameters(w, parameters));
                }
            });
        }

        private static void WriteFootprintCommon(BinaryWriter writer, PcbPrimitive primitive, CoordPoint? location = null)
        {
            writer.Write(primitive.Layer.ToByte());
            writer.Write((ushort)primitive.Flags);
            writer.Write(Enumerable.Repeat((byte)0xff, 10).ToArray());
            if (location != null)
            {
                WriteCoordPoint(writer, location.Value);
            }
        }

        private void WriteFootprintArc(BinaryWriter writer, PcbArc arc)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, arc, arc.Location);
                w.Write(arc.Radius.ToInt32());
                w.Write((double)arc.StartAngle);
                w.Write((double)arc.EndAngle);
                w.Write(arc.Width.ToInt32());
                /*
                if (recordSize >= 56)
                {
                    reader.ReadUInt32(); // TODO: Unknown - ordering?
                    reader.ReadUInt16(); // TODO: Unknown
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadUInt32(); // TODO: Unknown
                }
                */
            });
        }

        private void WriteFootprintPad(BinaryWriter writer, PcbPad pad)
        {
            WriteStringBlock(writer, pad.Designator);
            WriteBlock(writer, new byte[] { 0 }); // TODO: Unknown
            WriteStringBlock(writer, pad.UnknownString);
            WriteBlock(writer, new byte[] { 0 }); // TODO: Unknown

            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, pad, pad.Location);
                WriteCoordPoint(w, pad.SizeTop);
                WriteCoordPoint(w, pad.SizeMiddle);
                WriteCoordPoint(w, pad.SizeBottom);
                w.Write(pad.HoleSize.ToInt32());
                w.Write((byte)pad.ShapeTop); // 72
                w.Write((byte)pad.ShapeMiddle);
                w.Write((byte)pad.ShapeBottom);
                w.Write((double)pad.Rotation);
                w.Write(1L); // 83 constant value?
                w.Write(0); // TODO: Unknown
                w.Write((short)4); // 95 constant value?
                w.Write(0); // TODO: Unknown
                w.Write(0); // TODO: Unknown
                w.Write(0); // TODO: Unknown
                w.Write(0); // TODO: Unknown
                w.Write(0); // TODO: Unknown
                w.Write(0); // TODO: Unknown
                w.Write(0); // TODO: Unknown
                w.Write(0); // TODO: Unknown
                w.Write(0); // 129 constant value?
                
                // blockSize > 114
                w.Write(0); // TODO: Unknown
                w.Write(pad.ToLayer.ToByte());
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                w.Write(pad.FromLayer.ToByte());
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
            });

            // Write size and shape and parts of hole information
            if (pad.SizeMiddleLayers.Count == 29 && pad.ShapeMiddleLayers.Count == 29 &&
                pad.OffsetsFromHoleCenter.Count == 32 && pad.CornerRadiusPercentage.Count == 32)
            {
                WriteBlock(writer, w =>
                {
                // 29 items
                foreach (var padSize in pad.SizeMiddleLayers) w.Write(padSize.X.ToInt32());
                    foreach (var padSize in pad.SizeMiddleLayers) w.Write(padSize.Y.ToInt32());
                    foreach (var padShape in pad.ShapeMiddleLayers) w.Write((byte)padShape);

                    w.Write((byte)0); // TODO: Unknown
                w.Write((byte)pad.HoleShape);
                    w.Write(pad.HoleSlotLength);
                    w.Write((double)pad.HoleRotation);

                // 32 items
                foreach (var offset in pad.OffsetsFromHoleCenter) w.Write(offset.X.ToInt32());
                    foreach (var offset in pad.OffsetsFromHoleCenter) w.Write(offset.Y.ToInt32());

                    w.Write((byte)0); // TODO: Unknown
                w.Write(Enumerable.Repeat((byte)0, 32).ToArray()); // TODO: Unknown

                // 32 items
                foreach (var crp in pad.CornerRadiusPercentage) w.Write((byte)crp);
                });
            }
            else
            {
                WriteBlock(writer, Array.Empty<byte>());
            }
        }

        private void WriteFootprintTrack(BinaryWriter writer, PcbTrack track)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, track, track.Start);
                WriteCoordPoint(w, track.End);
                w.Write(track.Width.ToInt32());
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown

                /*
                if (recordSize >= 41)
                {
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadUInt32(); // TODO: Unknown
                }
                if (recordSize >= 45)
                {
                    reader.ReadUInt32(); // TODO: Unknown
                }
                */
            });
        }

        private void WriteFootprintString(BinaryWriter writer, PcbString @string)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, @string, @string.Location);
                w.Write(@string.Height.ToInt32());
                w.Write((short)0); // TODO: Unknown
                w.Write((double)@string.Rotation);
                w.Write(@string.Mirrored);
                w.Write(@string.Width.ToInt32());

                //recordSize >= 123
                w.Write((short)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)@string.Font);
                w.Write(@string.FontBold);
                w.Write(@string.FontItalic);
                WriteStringFontName(w, @string.FontName); // TODO: check size and string format
                w.Write(@string.BarcodeLRMargin.ToInt32());
                w.Write(@string.BarcodeTBMargin.ToInt32());
                w.Write(0); // TODO: Unknown - Coord?
                w.Write(0); // TODO: Unknown - Coord?
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                w.Write(0); // TODO: Unknown - Coord?
                w.Write((short)0); // TODO: Unknown
                w.Write(0); // TODO: Unknown - Coord?
                w.Write(0); // TODO: Unknown
                w.Write(@string.FontInverted);
                w.Write(@string.FontInvertedBorder.ToInt32());
                w.Write(0); // TODO: Unknown
                w.Write(0); // TODO: Unknown
                w.Write(@string.FontInvertedRect);
                w.Write(@string.FontInvertedRectWidth.ToInt32());
                w.Write(@string.FontInvertedRectHeight.ToInt32());
                w.Write((byte)@string.FontInvertedRectJustification);
                w.Write(@string.FontInvertedRectTextOffset.ToInt32());
            });

            WriteStringBlock(writer, @string.Text);
        }

        private void WriteFootprintRectangle(BinaryWriter writer, PcbRectangle rectangle)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, rectangle, rectangle.Corner1);
                WriteCoordPoint(w, rectangle.Corner2);
                w.Write((double)rectangle.Rotation);

                /*
                if (recordSize >= 42)
                {
                    reader.ReadUInt32(); // TODO: Unknown
                }
                if (recordSize >= 46)
                {
                    reader.ReadByte(); // TODO: Unknown
                    reader.ReadInt32(); // TODO: Unknown - Coord?
                }
                */
            });
        }

        private void WriteFootprintPolygon(BinaryWriter writer, PcbPolygon polygon)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, polygon);
                w.Write(0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                WriteBlock(w, wb => WriteParameters(wb, polygon.Attributes));
                w.Write(polygon.Outline.Count);
                foreach (var coord in polygon.Outline)
                {
                    // oddly enough polygonal features are stored using double precision
                    // but still employ the same units as standard Coords, which means
                    // the extra precision is not needed
                    w.Write((double)coord.X.ToInt32());
                    w.Write((double)coord.Y.ToInt32());
                }
            });
        }

        /// <summary>
        /// Writes model information.
        /// </summary>
        /// <param name="library">Storage where serialize the model data.</param>
        /// <param name="data">List of model data.</param>
        private void WriteLibraryModels(CFStorage library)
        {
            var models = library.GetOrAddStorage("Models");
            var data = Data.Models.Values.ToList();
            WriteHeader(models, data.Count);
            models.GetOrAddStream("Data").Write(writer =>
            {
                for (var i = 0; i < data.Count; ++i)
                {
                    var parameters = data[i].positioning;
                    var stepModel = data[i].step;

                    WriteBlock(writer, w => WriteParameters(w, parameters));

                    // models are stored as ASCII STEP files but using zlib compression
                    var modelCompressedData = CompressZlibData(Encoding.ASCII.GetBytes(stepModel));
                    models.GetOrAddStream($"{i}").SetData(modelCompressedData);
                }
            });
        }
    }
}