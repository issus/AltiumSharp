using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OriginalCircuit.AltiumSharp.BasicTypes
{
    /// <summary>
    /// Information about the possible layers.
    /// </summary>
    internal class LayerMetadata
    {
        private static Dictionary<Layer, LayerMetadata> _info = new Dictionary<Layer, LayerMetadata>();

        private static void RegisterLayerInfo(Layer layer, int drawPriority, Color color)
        {
            _info.Add(layer, new LayerMetadata(layer, drawPriority, color));
        }

        public static string[] InternalNames = new string[86]
        {
            nameof(Layer.NoLayer),
            nameof(Layer.TopLayer),
            nameof(Layer.MidLayer1),
            nameof(Layer.MidLayer2),
            nameof(Layer.MidLayer3),
            nameof(Layer.MidLayer4),
            nameof(Layer.MidLayer5),
            nameof(Layer.MidLayer6),
            nameof(Layer.MidLayer7),
            nameof(Layer.MidLayer8),
            nameof(Layer.MidLayer9),
            nameof(Layer.MidLayer10),
            nameof(Layer.MidLayer11),
            nameof(Layer.MidLayer12),
            nameof(Layer.MidLayer13),
            nameof(Layer.MidLayer14),
            nameof(Layer.MidLayer15),
            nameof(Layer.MidLayer16),
            nameof(Layer.MidLayer17),
            nameof(Layer.MidLayer18),
            nameof(Layer.MidLayer19),
            nameof(Layer.MidLayer20),
            nameof(Layer.MidLayer21),
            nameof(Layer.MidLayer22),
            nameof(Layer.MidLayer23),
            nameof(Layer.MidLayer24),
            nameof(Layer.MidLayer25),
            nameof(Layer.MidLayer26),
            nameof(Layer.MidLayer27),
            nameof(Layer.MidLayer28),
            nameof(Layer.MidLayer29),
            nameof(Layer.MidLayer30),
            nameof(Layer.BottomLayer),
            nameof(Layer.TopOverlay),
            nameof(Layer.BottomOverlay),
            nameof(Layer.TopPaste),
            nameof(Layer.BottomPaste),
            nameof(Layer.TopSolder),
            nameof(Layer.BottomSolder),
            nameof(Layer.InternalPlane1),
            nameof(Layer.InternalPlane2),
            nameof(Layer.InternalPlane3),
            nameof(Layer.InternalPlane4),
            nameof(Layer.InternalPlane5),
            nameof(Layer.InternalPlane6),
            nameof(Layer.InternalPlane7),
            nameof(Layer.InternalPlane8),
            nameof(Layer.InternalPlane9),
            nameof(Layer.InternalPlane10),
            nameof(Layer.InternalPlane11),
            nameof(Layer.InternalPlane12),
            nameof(Layer.InternalPlane13),
            nameof(Layer.InternalPlane14),
            nameof(Layer.InternalPlane15),
            nameof(Layer.InternalPlane16),
            nameof(Layer.DrillGuide),
            nameof(Layer.KeepOutLayer),
            nameof(Layer.Mechanical1),
            nameof(Layer.Mechanical2),
            nameof(Layer.Mechanical3),
            nameof(Layer.Mechanical4),
            nameof(Layer.Mechanical5),
            nameof(Layer.Mechanical6),
            nameof(Layer.Mechanical7),
            nameof(Layer.Mechanical8),
            nameof(Layer.Mechanical9),
            nameof(Layer.Mechanical10),
            nameof(Layer.Mechanical11),
            nameof(Layer.Mechanical12),
            nameof(Layer.Mechanical13),
            nameof(Layer.Mechanical14),
            nameof(Layer.Mechanical15),
            nameof(Layer.Mechanical16),
            nameof(Layer.DrillDrawing),
            nameof(Layer.MultiLayer),
            nameof(Layer.ConnectLayer),
            nameof(Layer.BackGroundLayer),
            nameof(Layer.DRCErrorLayer),
            nameof(Layer.HighlightLayer),
            nameof(Layer.GridColor1),
            nameof(Layer.GridColor10),
            nameof(Layer.PadHoleLayer),
            nameof(Layer.ViaHoleLayer),

            nameof(Layer.TopPadMaster),
            nameof(Layer.BottomPadMaster),
            nameof(Layer.DRCDetailLayer)
        };

        public static string[] ShortNames = new string[86]
        {
            "NoLayer",
            "TL",
            "M1",
            "M2",
            "M3",
            "M4",
            "M5",
            "M6",
            "M7",
            "M8",
            "M9",
            "M10",
            "M11",
            "M12",
            "M13",
            "M14",
            "M15",
            "M16",
            "M17",
            "M18",
            "M19",
            "M20",
            "M21",
            "M22",
            "M23",
            "M24",
            "M25",
            "M26",
            "M27",
            "M28",
            "M29",
            "M30",
            "BL",
            "TO",
            "BO",
            "TP",
            "BP",
            "TS",
            "BS",
            "P1",
            "P2",
            "P3",
            "P4",
            "P5",
            "P6",
            "P7",
            "P8",
            "P9",
            "P10",
            "P11",
            "P12",
            "P13",
            "P14",
            "P15",
            "P16",
            "DG",
            "KO",
            "M1",
            "M2",
            "M3",
            "M4",
            "M5",
            "M6",
            "M7",
            "M8",
            "M9",
            "M10",
            "M11",
            "M12",
            "M13",
            "M14",
            "M15",
            "M16",
            "DD",
            "ML",
            "CL",
            "BR",
            "DRC",
            "HL",
            "GC1",
            "GC2",
            "PH",
            "VH",

            "TM",
            "BM",
            "DRCD"
        };

        public static string[] MediumNames = new string[86]
        {
            "NoLayer",
            "Top",
            "Mid-1",
            "Mid-2",
            "Mid-3",
            "Mid-4",
            "Mid-5",
            "Mid-6",
            "Mid-7",
            "Mid-8",
            "Mid-9",
            "Mid-10",
            "Mid-11",
            "Mid-12",
            "Mid-13",
            "Mid-14",
            "Mid-15",
            "Mid-16",
            "Mid-17",
            "Mid-18",
            "Mid-19",
            "Mid-20",
            "Mid-21",
            "Mid-22",
            "Mid-23",
            "Mid-24",
            "Mid-25",
            "Mid-26",
            "Mid-27",
            "Mid-28",
            "Mid-29",
            "Mid-30",
            "Bottom",
            "T-Silk",
            "B-Silk",
            "T-Paste",
            "B-Paste",
            "T-Solder",
            "B-Solder",
            "Plane-1",
            "Plane-2",
            "Plane-3",
            "Plane-4",
            "Plane-5",
            "Plane-6",
            "Plane-7",
            "Plane-8",
            "Plane-9",
            "Plane-10",
            "Plane-11",
            "Plane-12",
            "Plane-13",
            "Plane-14",
            "Plane-15",
            "Plane-16",
            "D-Guide",
            "KeepOut",
            "Mech-1",
            "Mech-2",
            "Mech-3",
            "Mech-4",
            "Mech-5",
            "Mech-6",
            "Mech-7",
            "Mech-8",
            "Mech-9",
            "Mech-10",
            "Mech-11",
            "Mech-12",
            "Mech-13",
            "Mech-14",
            "Mech-15",
            "Mech-16",
            "D-Draw",
            "Multi",
            "CL",
            "BR",
            "DRC",
            "Highlight",
            "Grid1",
            "Grid2",
            "PadHole",
            "ViaHole",

            "TPMaster",
            "BPMaster",
            "DRC Detail"
        };

        public static string[] LongNames = new string[86]
        {
            "NoLayer",
            "Top Layer",
            "Mid-Layer 1",
            "Mid-Layer 2",
            "Mid-Layer 3",
            "Mid-Layer 4",
            "Mid-Layer 5",
            "Mid-Layer 6",
            "Mid-Layer 7",
            "Mid-Layer 8",
            "Mid-Layer 9",
            "Mid-Layer 10",
            "Mid-Layer 11",
            "Mid-Layer 12",
            "Mid-Layer 13",
            "Mid-Layer 14",
            "Mid-Layer 15",
            "Mid-Layer 16",
            "Mid-Layer 17",
            "Mid-Layer 18",
            "Mid-Layer 19",
            "Mid-Layer 20",
            "Mid-Layer 21",
            "Mid-Layer 22",
            "Mid-Layer 23",
            "Mid-Layer 24",
            "Mid-Layer 25",
            "Mid-Layer 26",
            "Mid-Layer 27",
            "Mid-Layer 28",
            "Mid-Layer 29",
            "Mid-Layer 30",
            "Bottom Layer",
            "Top Overlay",
            "Bottom Overlay",
            "Top Paste",
            "Bottom Paste",
            "Top Solder",
            "Bottom Solder",
            "Internal Plane 1",
            "Internal Plane 2",
            "Internal Plane 3",
            "Internal Plane 4",
            "Internal Plane 5",
            "Internal Plane 6",
            "Internal Plane 7",
            "Internal Plane 8",
            "Internal Plane 9",
            "Internal Plane 10",
            "Internal Plane 11",
            "Internal Plane 12",
            "Internal Plane 13",
            "Internal Plane 14",
            "Internal Plane 15",
            "Internal Plane 16",
            "Drill Guide",
            "Keep-Out Layer",
            "Mechanical 1",
            "Mechanical 2",
            "Mechanical 3",
            "Mechanical 4",
            "Mechanical 5",
            "Mechanical 6",
            "Mechanical 7",
            "Mechanical 8",
            "Mechanical 9",
            "Mechanical 10",
            "Mechanical 11",
            "Mechanical 12",
            "Mechanical 13",
            "Mechanical 14",
            "Mechanical 15",
            "Mechanical 16",
            "Drill Drawing",
            "Multi-Layer",
            "Connections",
            "Background",
            "DRC Error Markers",
            "Selections",
            "Visible Grid 1",
            "Visible Grid 2",
            "Pad Holes",
            "Via Holes",

            "Top Pad Master",
            "Bottom Pad Master",
            "DRC Detail Markers"
        };

        static LayerMetadata()
        {
            RegisterLayerInfo(Layer.NoLayer, 0, Color.Empty);
            RegisterLayerInfo(Layer.TopLayer, 6, ColorTranslator.FromWin32(0x000000FF));
            RegisterLayerInfo(Layer.MidLayer1, 6, ColorTranslator.FromWin32(0x00008EBC));
            RegisterLayerInfo(Layer.MidLayer2, 6, ColorTranslator.FromWin32(0x00FADB70));
            RegisterLayerInfo(Layer.MidLayer3, 6, ColorTranslator.FromWin32(0x0066CC00));
            RegisterLayerInfo(Layer.MidLayer4, 6, ColorTranslator.FromWin32(0x00FF6699));
            RegisterLayerInfo(Layer.MidLayer5, 6, ColorTranslator.FromWin32(0x00FFFF00));
            RegisterLayerInfo(Layer.MidLayer6, 6, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.MidLayer7, 6, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(Layer.MidLayer8, 6, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(Layer.MidLayer9, 6, ColorTranslator.FromWin32(0x0000FFFF));
            RegisterLayerInfo(Layer.MidLayer10, 6, ColorTranslator.FromWin32(0x00808080));
            RegisterLayerInfo(Layer.MidLayer11, 6, ColorTranslator.FromWin32(0x00FFFFFF));
            RegisterLayerInfo(Layer.MidLayer12, 6, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.MidLayer13, 6, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(Layer.MidLayer14, 6, ColorTranslator.FromWin32(0x00C0C0C0));
            RegisterLayerInfo(Layer.MidLayer15, 6, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(Layer.MidLayer16, 6, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.MidLayer17, 6, ColorTranslator.FromWin32(0x0000FF00));
            RegisterLayerInfo(Layer.MidLayer18, 6, ColorTranslator.FromWin32(0x00800000));
            RegisterLayerInfo(Layer.MidLayer19, 6, ColorTranslator.FromWin32(0x00FFFF00));
            RegisterLayerInfo(Layer.MidLayer20, 6, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.MidLayer21, 6, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(Layer.MidLayer22, 6, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(Layer.MidLayer23, 6, ColorTranslator.FromWin32(0x0000FFFF));
            RegisterLayerInfo(Layer.MidLayer24, 6, ColorTranslator.FromWin32(0x00808080));
            RegisterLayerInfo(Layer.MidLayer25, 6, ColorTranslator.FromWin32(0x00FFFFFF));
            RegisterLayerInfo(Layer.MidLayer26, 6, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.MidLayer27, 6, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(Layer.MidLayer28, 6, ColorTranslator.FromWin32(0x00C0C0C0));
            RegisterLayerInfo(Layer.MidLayer29, 6, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(Layer.MidLayer30, 6, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.BottomLayer, 6, ColorTranslator.FromWin32(0x00FF0000));
            RegisterLayerInfo(Layer.TopOverlay, 2, ColorTranslator.FromWin32(0x0000FFFF));
            RegisterLayerInfo(Layer.BottomOverlay, 3, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(Layer.TopPaste, 7, ColorTranslator.FromWin32(0x00808080));
            RegisterLayerInfo(Layer.BottomPaste, 8, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(Layer.TopSolder, 9, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.BottomSolder, 10, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(Layer.InternalPlane1, 11, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.InternalPlane2, 11, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(Layer.InternalPlane3, 11, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.InternalPlane4, 11, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(Layer.InternalPlane5, 11, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.InternalPlane6, 11, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(Layer.InternalPlane7, 11, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.InternalPlane8, 11, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(Layer.InternalPlane9, 11, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.InternalPlane10, 11, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(Layer.InternalPlane11, 11, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.InternalPlane12, 11, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(Layer.InternalPlane13, 11, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.InternalPlane14, 11, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(Layer.InternalPlane15, 11, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.InternalPlane16, 11, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(Layer.DrillGuide, 12, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(Layer.KeepOutLayer, 13, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(Layer.Mechanical1, 14, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(Layer.Mechanical2, 14, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.Mechanical3, 14, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.Mechanical4, 14, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(Layer.Mechanical5, 14, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(Layer.Mechanical6, 14, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.Mechanical7, 14, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.Mechanical8, 14, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(Layer.Mechanical9, 14, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(Layer.Mechanical10, 14, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.Mechanical11, 14, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.Mechanical12, 14, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(Layer.Mechanical13, 14, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(Layer.Mechanical14, 14, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(Layer.Mechanical15, 14, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(Layer.Mechanical16, 14, ColorTranslator.FromWin32(0x00000000));
            RegisterLayerInfo(Layer.DrillDrawing, 15, ColorTranslator.FromWin32(0x002A00FF));
            RegisterLayerInfo(Layer.MultiLayer, 1, ColorTranslator.FromWin32(0x00C0C0C0));
            RegisterLayerInfo(Layer.ConnectLayer, 4, ColorTranslator.FromWin32(0x0075A19E));
            RegisterLayerInfo(Layer.BackGroundLayer, 5, ColorTranslator.FromWin32(0x00000000));
            RegisterLayerInfo(Layer.DRCErrorLayer, 0, ColorTranslator.FromWin32(0x0000FF00));
            RegisterLayerInfo(Layer.HighlightLayer, 0, ColorTranslator.FromWin32(0x00FFFFFF));
            RegisterLayerInfo(Layer.GridColor1, 101, ColorTranslator.FromWin32(0x005C4D4D));
            RegisterLayerInfo(Layer.GridColor10, 100, ColorTranslator.FromWin32(0x00908D91));
            RegisterLayerInfo(Layer.PadHoleLayer, 1, ColorTranslator.FromWin32(0x00909100));
            RegisterLayerInfo(Layer.ViaHoleLayer, 1, ColorTranslator.FromWin32(0x00006281));
        }

        public static IEnumerable<string> Names =>
            _info.Values.OrderBy(m => m.Id).Select(m => m.InternalName);

        public static IEnumerable<Layer> Values =>
            _info.Keys.OrderBy(l => l.ToByte());

        public static LayerMetadata Get(Layer layer) =>
            _info.TryGetValue(layer, out var result) ? result : null;

        public static LayerMetadata Get(string layerName) =>
            _info.Values.FirstOrDefault(li => layerName.Equals(li.InternalName, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        /// Layer identifier as a byte.
        /// </summary>
        public byte Id { get; }

        /// <summary>
        /// Layer internal name.
        /// </summary>
        public string InternalName => InternalNames[Id];

        /// <summary>
        /// Layer long name.
        /// </summary>
        public string LongName => LongNames[Id];

        /// <summary>
        /// Layer medium name.
        /// </summary>
        public string MediumName => MediumNames[Id];

        /// <summary>
        /// Layer short name.
        /// </summary>
        public string ShortName => ShortNames[Id];

        /// <summary>
        /// Layer draw priority. the lower, the higher priority.
        /// </summary>
        public int DrawPriority { get; }

        /// <summary>
        /// Color for the layer.
        /// </summary>
        public Color Color { get; }

        internal string Name => InternalName;
        internal static string GetName(Layer layer) => layer.Name;
        internal static Color GetColor(Layer layer) => layer.Color;
        internal static Color GetColor(string layerName) => Layer.GetLayerColor(layerName);

        private LayerMetadata(byte id, int drawPriority, Color color)
        {
            Id = id;
            DrawPriority = drawPriority;
            Color = color;
        }
    }
}