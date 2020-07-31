using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace AltiumSharp.BasicTypes
{
    /// <summary>
    /// Information about the possible layers.
    /// </summary>
    internal class LayerMetadata
    {
        private static Dictionary<Layer, LayerMetadata> _info = new Dictionary<Layer, LayerMetadata>();

        private static void RegisterLayerInfo(int id, string name, int drawPriority, Color color)
        {
            _info.Add((byte)id, new LayerMetadata((byte)id, name, drawPriority, color));
        }

        static LayerMetadata()
        {
            /* from Default.PCBSysColors */
            RegisterLayerInfo(_info.Count + 1, "TopLayer", 3, ColorTranslator.FromWin32(0x000000FF));
            RegisterLayerInfo(_info.Count + 1, "MidLayer1", 3, ColorTranslator.FromWin32(0x00008EBC));
            RegisterLayerInfo(_info.Count + 1, "MidLayer2", 3, ColorTranslator.FromWin32(0x00FADB70));
            RegisterLayerInfo(_info.Count + 1, "MidLayer3", 3, ColorTranslator.FromWin32(0x0066CC00));
            RegisterLayerInfo(_info.Count + 1, "MidLayer4", 3, ColorTranslator.FromWin32(0x00FF6699));
            RegisterLayerInfo(_info.Count + 1, "MidLayer5", 3, ColorTranslator.FromWin32(0x00FFFF00));
            RegisterLayerInfo(_info.Count + 1, "MidLayer6", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer7", 3, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(_info.Count + 1, "MidLayer8", 3, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer9", 3, ColorTranslator.FromWin32(0x0000FFFF));
            RegisterLayerInfo(_info.Count + 1, "MidLayer10", 3, ColorTranslator.FromWin32(0x00808080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer11", 3, ColorTranslator.FromWin32(0x00FFFFFF));
            RegisterLayerInfo(_info.Count + 1, "MidLayer12", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer13", 3, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(_info.Count + 1, "MidLayer14", 3, ColorTranslator.FromWin32(0x00C0C0C0));
            RegisterLayerInfo(_info.Count + 1, "MidLayer15", 3, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer16", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "MidLayer17", 3, ColorTranslator.FromWin32(0x0000FF00));
            RegisterLayerInfo(_info.Count + 1, "MidLayer18", 3, ColorTranslator.FromWin32(0x00800000));
            RegisterLayerInfo(_info.Count + 1, "MidLayer19", 3, ColorTranslator.FromWin32(0x00FFFF00));
            RegisterLayerInfo(_info.Count + 1, "MidLayer20", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer21", 3, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(_info.Count + 1, "MidLayer22", 3, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer23", 3, ColorTranslator.FromWin32(0x0000FFFF));
            RegisterLayerInfo(_info.Count + 1, "MidLayer24", 3, ColorTranslator.FromWin32(0x00808080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer25", 3, ColorTranslator.FromWin32(0x00FFFFFF));
            RegisterLayerInfo(_info.Count + 1, "MidLayer26", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer27", 3, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(_info.Count + 1, "MidLayer28", 3, ColorTranslator.FromWin32(0x00C0C0C0));
            RegisterLayerInfo(_info.Count + 1, "MidLayer29", 3, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(_info.Count + 1, "MidLayer30", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "BottomLayer", 2, ColorTranslator.FromWin32(0x00FF0000));
            RegisterLayerInfo(_info.Count + 1, "TopOverlay", 2, ColorTranslator.FromWin32(0x0000FFFF));
            RegisterLayerInfo(_info.Count + 1, "BottomOverlay", 2, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(_info.Count + 1, "TopPaste", 2, ColorTranslator.FromWin32(0x00808080));
            RegisterLayerInfo(_info.Count + 1, "BottomPaste", 2, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(_info.Count + 1, "TopSolder", 2, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "BottomSolder", 2, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane1", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane2", 3, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane3", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane4", 3, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane5", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane6", 3, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane7", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane8", 3, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane9", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane10", 3, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane11", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane12", 3, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane13", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane14", 3, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane15", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "InternalPlane16", 3, ColorTranslator.FromWin32(0x00808000));
            RegisterLayerInfo(_info.Count + 1, "DrillGuide", 2, ColorTranslator.FromWin32(0x00000080));
            RegisterLayerInfo(_info.Count + 1, "KeepOutLayer", 3, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(_info.Count + 1, "Mechanical1", 3, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(_info.Count + 1, "Mechanical2", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "Mechanical3", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "Mechanical4", 3, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(_info.Count + 1, "Mechanical5", 3, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(_info.Count + 1, "Mechanical6", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "Mechanical7", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "Mechanical8", 3, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(_info.Count + 1, "Mechanical9", 3, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(_info.Count + 1, "Mechanical10", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "Mechanical11", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "Mechanical12", 3, ColorTranslator.FromWin32(0x00008080));
            RegisterLayerInfo(_info.Count + 1, "Mechanical13", 3, ColorTranslator.FromWin32(0x00FF00FF));
            RegisterLayerInfo(_info.Count + 1, "Mechanical14", 3, ColorTranslator.FromWin32(0x00800080));
            RegisterLayerInfo(_info.Count + 1, "Mechanical15", 3, ColorTranslator.FromWin32(0x00008000));
            RegisterLayerInfo(_info.Count + 1, "Mechanical16", 3, ColorTranslator.FromWin32(0x00000000));
            RegisterLayerInfo(_info.Count + 1, "DrillDrawing", 3, ColorTranslator.FromWin32(0x002A00FF));
            RegisterLayerInfo(_info.Count + 1, "MultiLayer", 1, ColorTranslator.FromWin32(0x00C0C0C0));
            RegisterLayerInfo(_info.Count + 1, "ConnectLayer", 2, ColorTranslator.FromWin32(0x0075A19E));
            RegisterLayerInfo(_info.Count + 1, "BackGroundLayer", 5, ColorTranslator.FromWin32(0x00000000));
            RegisterLayerInfo(_info.Count + 1, "DRCErrorLayer", 3, ColorTranslator.FromWin32(0x0000FF00));
            RegisterLayerInfo(_info.Count + 1, "HighlightLayer", 0, ColorTranslator.FromWin32(0x00FFFFFF));
            RegisterLayerInfo(_info.Count + 1, "GridColor1", 5, ColorTranslator.FromWin32(0x005C4D4D));
            RegisterLayerInfo(_info.Count + 1, "GridColor10", 5, ColorTranslator.FromWin32(0x00908D91));
            RegisterLayerInfo(_info.Count + 1, "PadHoleLayer", 2, ColorTranslator.FromWin32(0x00909100));
            RegisterLayerInfo(_info.Count + 1, "ViaHoleLayer", 2, ColorTranslator.FromWin32(0x00006281));
        }

        public static LayerMetadata Get(Layer layer) =>
            _info.TryGetValue(layer, out var result) ? result : null;

        public static LayerMetadata Get(string layerName) =>
            _info.Values.FirstOrDefault(li => layerName.Equals(li.Name, StringComparison.InvariantCultureIgnoreCase));

        public static string GetName(Layer layer) =>
            Get(layer)?.Name ?? $"UnknownLayer{layer.ToByte()}";

        public static Color GetColor(Layer layer) =>
            Get(layer)?.Color ?? Color.Empty;

        public static Color GetColor(string layerName) =>
            Get(layerName)?.Color ?? Color.Empty;

        /// <summary>
        /// Layer identifier as a byte.
        /// </summary>
        public byte Id { get; }

        /// <summary>
        /// Layer internal name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Layer draw priority. the lower, the higher priority.
        /// </summary>
        public int DrawPriority { get; }

        /// <summary>
        /// Color for the layer.
        /// </summary>
        public Color Color { get; }

        private LayerMetadata(byte id, string name, int drawPriority, Color color)
        {
            Id = id;
            Name = name;
            DrawPriority = drawPriority;
            Color = color;
        }
    }


    /// <summary>
    /// Layer data type used for ease of handling PCB layer references.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    public readonly struct Layer : IEquatable<Layer>, IComparable<Layer>
    {
        private readonly byte _value;
        public Layer(byte value) => _value = value;
        public byte ToByte() => _value;

        internal LayerMetadata Metadata => LayerMetadata.Get(_value);

        /// <summary>
        /// Gets the internal name of a PCB layer.
        /// </summary>
        public string Name => Metadata?.Name ?? "Unknown";

        /// <summary>
        /// Gets the color to be used for this PCB layer.
        /// </summary>
        public Color Color => Metadata?.Color ?? Color.Empty;

        /// <summary>
        /// Gets the drawing priority for this PCB layer.
        /// <para>
        /// The lower this number, the higher the priority.
        /// </para>
        /// </summary>
        public int DrawPriority => Metadata?.DrawPriority ?? 0;

        public override string ToString() => Name;

        /// <summary>
        /// Get a layer color from it's internal layer name.
        /// </summary>
        /// <param name="layerName">Internal layer name.</param>
        /// <returns>Color attributed to the given layer.</returns>
        public static Color GetLayerColor(string layerName) => LayerMetadata.Get(layerName)?.Color ?? Color.Empty;

        /// <summary>
        /// Creates a layer reference from a byte value with the layer identifier.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Layer FromByte(byte value) => new Layer(value);

        /// <summary>
        /// Implicit conversion operator so we can use bytes and layers transparently.
        /// </summary>
        static public implicit operator Layer(byte value) => new Layer(value);

        /// <summary>
        /// Implicit conversion operator so we can use bytes and layers transparently.
        /// </summary>
        static public implicit operator byte(Layer coord) => coord._value;

        #region 'boilerplate'
        public override bool Equals(object obj) => obj is Layer other && Equals(other);
        public bool Equals(Layer other) => _value == other._value;
        public override int GetHashCode() => _value.GetHashCode();
        public int CompareTo(Layer other) => _value < other._value ? -1 : _value > other._value ? 1 : 0;
        public static bool operator ==(Layer left, Layer right) => left.Equals(right);
        public static bool operator !=(Layer left, Layer right) => !(left.Equals(right));
        public static bool operator <(Layer left, Layer right) => left.CompareTo(right) < 0;
        public static bool operator <=(Layer left, Layer right) => left.CompareTo(right) <= 0;
        public static bool operator >(Layer left, Layer right) => left.CompareTo(right) > 0;
        public static bool operator >=(Layer left, Layer right) => left.CompareTo(right) >= 0;
        #endregion
    }
}
