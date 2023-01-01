using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp
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
            var components = Data.Items.Where(c => GetSectionKeyFromComponentPattern(c.Pattern) != c.Pattern).ToList();
            if (components.Count == 0) return;

            Cf.RootStorage.GetOrAddStream("SectionKeys").Write(writer =>
            {
                writer.Write(components.Count);
                foreach (var component in components)
                {
                    WritePascalString(writer, component.Pattern);
                    WriteStringBlock(writer, GetSectionKeyFromComponentPattern(component.Pattern));
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
            WriteLibraryData(library);
            WriteLibraryModels(library);
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
                    WriteStringBlock(writer, component.Pattern);
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
            var sectionKey = GetSectionKeyFromComponentPattern(component.Pattern);
            var footprintStorage = Cf.RootStorage.GetOrAddStorage(sectionKey);

            var primitives = component.Primitives.Where(p => !(p is PcbUnknown));

            WriteHeader(footprintStorage, primitives.Count()); // record count
            WriteFootprintParameters(footprintStorage, component);
            WriteWideStrings(footprintStorage, component);

            footprintStorage.GetOrAddStream("Data").Write(writer =>
            {
                WriteStringBlock(writer, component.Pattern);

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

                        case PcbVia via:
                            WriteFootprintVia(writer, via);
                            break;

                        case PcbTrack track:
                            WriteFootprintTrack(writer, track);
                            break;

                        case PcbText text:
                            WriteFootprintText(writer, text);
                            break;

                        case PcbFill fill:
                            WriteFootprintFill(writer, fill);
                            break;

                        case PcbRegion region:
                            WriteFootprintRegion(writer, region);
                            break;

                        case PcbComponentBody body:
                            WriteFootprintComponentBody(writer, body);
                            break;

                        default:
                            break;
                    }
                }
            });

            WriteUniqueIdPrimitiveInformation(footprintStorage, component);
        }

        /// <summary>
        /// Writes the component parameter information.
        /// </summary>
        /// <param name="componentStorage">Component footprint storage key.</param>
        /// <param name="component">Component instance to have its parameters serialized.</param>
        private static void WriteFootprintParameters(CFStorage componentStorage, PcbComponent component)
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

        private static void WriteFootprintArc(BinaryWriter writer, PcbArc arc)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, arc, arc.Location);
                w.Write(arc.Radius.ToInt32());
                w.Write((double)arc.StartAngle);
                w.Write((double)arc.EndAngle);
                w.Write(arc.Width.ToInt32());
            });
        }

        private static void WriteFootprintPad(BinaryWriter writer, PcbPad pad)
        {
            WriteStringBlock(writer, pad.Designator);
            WriteBlock(writer, new byte[] { 0 }); // TODO: Unknown
            WriteStringBlock(writer, "|&|0");
            WriteBlock(writer, new byte[] { 0 }); // TODO: Unknown

            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, pad, pad.Location);
                WriteCoordPoint(w, pad.SizeTop);
                WriteCoordPoint(w, pad.SizeMiddle);
                WriteCoordPoint(w, pad.SizeBottom);
                w.Write(pad.HoleSize.ToInt32());
                w.Write((byte)(pad.ShapeTop == PcbPadShape.RoundedRectangle ? PcbPadShape.Round : pad.ShapeTop)); // 72
                w.Write((byte)(pad.ShapeMiddle == PcbPadShape.RoundedRectangle ? PcbPadShape.Round : pad.ShapeMiddle));
                w.Write((byte)(pad.ShapeBottom == PcbPadShape.RoundedRectangle ? PcbPadShape.Round : pad.ShapeBottom));
                w.Write((double)pad.Rotation);
                w.Write(pad.IsPlated);
                w.Write((byte)0); // 91 constant value?
                w.Write((byte)pad.StackMode);
                w.Write((byte)0); // TODO: Unknown 0
                w.Write(0); // TODO: Unknown 0
                w.Write(Coord.FromMils(10)); // TODO: Unknown 10mil?
                w.Write((short)4); // 102 constant value?
                w.Write(Coord.FromMils(10)); // TODO: Unknown 10mil?
                w.Write(Coord.FromMils(20)); // TODO: Unknown 20mil?
                w.Write(Coord.FromMils(20)); // TODO: Unknown 20mil?
                w.Write(pad.PasteMaskExpansion.ToInt32());
                w.Write(pad.SolderMaskExpansion.ToInt32());
                w.Write((byte)0); // TODO: Unknown 0
                w.Write((byte)0); // TODO: Unknown 0
                w.Write((byte)0); // TODO: Unknown 0
                w.Write((byte)0); // TODO: Unknown 0/1
                w.Write((byte)0); // TODO: Unknown 0/1
                w.Write((byte)0); // TODO: Unknown 0/1
                w.Write((byte)0); // TODO: Unknown 0/1
                w.Write((byte)(pad.PasteMaskExpansionManual ? 2 : 0));
                w.Write((byte)(pad.SolderMaskExpansionManual ? 2 : 1));
                w.Write((byte)0); // TODO: Unknown 0/1
                w.Write((byte)0); // TODO: Unknown 0
                w.Write((byte)0); // TODO: Unknown 0
                w.Write(0); // TODO: Unknown
                w.Write((short)pad.JumperId);
                w.Write((short)0);
            });

            // Write size and shape and parts of hole information
            if (pad.NeedsFullStackData)
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

                    w.Write(pad.HasRoundedRectangles);
                    if (pad.HasRoundedRectangles)
                    {
                        foreach (var padShape in pad.ShapeLayers) w.Write((byte)padShape);
                    }
                    else
                    {
                        foreach (var padShape in pad.ShapeLayers) w.Write((byte)PcbPadShape.Round); // write dummy value
                    }

                    // 32 items
                    foreach (var crp in pad.CornerRadiusPercentage) w.Write((byte)crp);
                });
            }
            else
            {
                WriteBlock(writer, Array.Empty<byte>());
            }
        }

        private static void WriteFootprintVia(BinaryWriter writer, PcbVia via)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, via, via.Location);
                w.Write(via.Diameter.ToInt32());
                w.Write(via.HoleSize.ToInt32());
                w.Write(via.FromLayer.ToByte());
                w.Write(via.ToLayer.ToByte());
                w.Write((byte)0); // TODO: Unknown 0
                w.Write(via.ThermalReliefAirGapWidth.ToInt32());
                w.Write((byte)via.ThermalReliefConductors);
                w.Write((byte)0); // TODO: Unknown 0
                w.Write(via.ThermalReliefConductorsWidth.ToInt32());
                w.Write(Coord.FromMils(20).ToInt32()); // TODO: Unknown - Coord 20mils?
                w.Write(Coord.FromMils(20).ToInt32()); // TODO: Unknown - Coord 20mils?
                w.Write(0); // TODO: Unknown 0
                w.Write(via.SolderMaskExpansion.ToInt32());
                w.Write(Enumerable.Repeat((byte)0, 3).ToArray()); // TODO: Unknown 0s
                w.Write(Enumerable.Repeat((byte)1, 4).ToArray()); // TODO: Unknown 1s
                w.Write((byte)0); // TODO: Unknown 0
                w.Write((byte)(via.SolderMaskExpansionManual ? 2 : 1));
                w.Write((byte)1); // TODO: Unknown 1
                w.Write((short)0); // TODO: Unknown 0
                w.Write(0); // TODO: Unknown 0
                w.Write((byte)via.DiameterStackMode);
                foreach (var diameter in via.Diameters) // 32 items
                {
                    w.Write(diameter.ToInt32());
                }
                w.Write((short)15); // TODO: Unknown 15
                w.Write(259); // TODO: Unknown 259
            });
        }

        private static void WriteFootprintTrack(BinaryWriter writer, PcbTrack track)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, track, track.Start);
                WriteCoordPoint(w, track.End);
                w.Write(track.Width.ToInt32());
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
            });
        }

        private static void WriteFootprintText(BinaryWriter writer, PcbText text)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, text, text.Corner1);
                w.Write(text.Height.ToInt32());
                w.Write((short)text.StrokeFont);
                w.Write((double)text.Rotation);
                w.Write(text.Mirrored);
                w.Write(text.StrokeWidth.ToInt32());

                //recordSize >= 123
                w.Write((short)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)text.TextKind);
                w.Write(text.FontBold);
                w.Write(text.FontItalic);
                WriteStringFontName(w, text.FontName); // TODO: check size and string format
                w.Write(text.BarcodeLRMargin.ToInt32());
                w.Write(text.BarcodeTBMargin.ToInt32());
                w.Write(0); // TODO: Unknown - Coord 0?
                w.Write(Coord.FromMils(4)); // TODO: Unknown - Coord 4mil?
                w.Write((byte)0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                w.Write(0); // TODO: Unknown - Coord?
                w.Write((short)0); // TODO: Unknown
                w.Write(1); // TODO: Unknown 1?
                w.Write(0); // TODO: Unknown
                w.Write(text.FontInverted);
                w.Write(text.FontInvertedBorder.ToInt32());
                w.Write(text.WideStringsIndex);
                w.Write(0); // TODO: Unknown
                w.Write(text.FontInvertedRect);
                w.Write(text.FontInvertedRectWidth.ToInt32());
                w.Write(text.FontInvertedRectHeight.ToInt32());
                w.Write((byte)text.FontInvertedRectJustification);
                w.Write(text.FontInvertedRectTextOffset.ToInt32());
            });

            WriteStringBlock(writer, text.Text);
        }

        private static void WriteFootprintFill(BinaryWriter writer, PcbFill fill)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, fill, fill.Corner1);
                WriteCoordPoint(w, fill.Corner2);
                w.Write((double)fill.Rotation);
            });
        }

        private static void WriteFootprintCommonParametersAndOutline(BinaryWriter writer, PcbPrimitive primitive,
            ParameterCollection parameters, IList<CoordPoint> outline)
        {
            WriteBlock(writer, w =>
            {
                WriteFootprintCommon(w, primitive);
                w.Write(0); // TODO: Unknown
                w.Write((byte)0); // TODO: Unknown
                WriteBlock(w, wb => WriteParameters(wb, parameters));
                w.Write(outline.Count);
                foreach (var coord in outline)
                {
                    // oddly enough polygonal features are stored using double precision
                    // but still employ the same units as standard Coords, which means
                    // the extra precision is not needed
                    w.Write((double)coord.X.ToInt32());
                    w.Write((double)coord.Y.ToInt32());
                }
            });
        }

        private static void WriteFootprintRegion(BinaryWriter writer, PcbRegion region)
        {
            WriteFootprintCommonParametersAndOutline(writer, region, region.Parameters, region.Outline);
        }

        private static void WriteFootprintComponentBody(BinaryWriter writer, PcbComponentBody body)
        {
            var parameters = body.ExportToParameters();
            WriteFootprintCommonParametersAndOutline(writer, body, parameters, body.Outline);
        }

        /// <summary>
        /// Writes model information from component bodies.
        /// </summary>
        /// <param name="library">Storage where serialize the model data.</param>
        private void WriteLibraryModels(CFStorage library)
        {
            var models = library.GetOrAddStorage("Models");
            var bodies = Data.Items.SelectMany(c => c.GetPrimitivesOfType<PcbComponentBody>(false))
                .Where(c => c.ModelEmbed).ToList();

            WriteHeader(models, bodies.Count);
            models.GetOrAddStream("Data").Write(writer =>
            {
                for (var i = 0; i < bodies.Count; ++i)
                {
                    var body = bodies[i];
                    var parameters = new ParameterCollection
                    {
                        { "ID", body.ModelId },
                        { "ROTX", body.Model3DRotX },
                        { "ROTY", body.Model3DRotY },
                        { "ROTZ", body.Model3DRotZ },
                        { "DZ", body.Model3DDz.ToInt32() },
                        { "CHECKSUM", body.ModelChecksum },
                        { "EMBED", body.ModelEmbed },
                        { "NAME", body.Identifier + ".STEP" },
                    };

                    WriteBlock(writer, w => WriteParameters(w, parameters));

                    // models are stored as ASCII STEP files but using zlib compression
                    var modelCompressedData = CompressZlibData(Encoding.ASCII.GetBytes(body.StepModel));
                    models.GetOrAddStream($"{i}").SetData(modelCompressedData);
                }
            });
        }
    }
}
