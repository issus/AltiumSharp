using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace AltiumSharp.BasicTypes
{
    /// <summary>
    /// Layer data type used for ease of handling PCB layer references.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    public readonly struct Layer : IEquatable<Layer>, IComparable<Layer>
    {
        public static Layer NoLayer = 0;
        public static Layer TopLayer = 1;
        public static Layer MidLayer1 = 2;
        public static Layer MidLayer2 = 3;
        public static Layer MidLayer3 = 4;
        public static Layer MidLayer4 = 5;
        public static Layer MidLayer5 = 6;
        public static Layer MidLayer6 = 7;
        public static Layer MidLayer7 = 8;
        public static Layer MidLayer8 = 9;
        public static Layer MidLayer9 = 10;
        public static Layer MidLayer10 = 11;
        public static Layer MidLayer11 = 12;
        public static Layer MidLayer12 = 13;
        public static Layer MidLayer13 = 14;
        public static Layer MidLayer14 = 15;
        public static Layer MidLayer15 = 16;
        public static Layer MidLayer16 = 17;
        public static Layer MidLayer17 = 18;
        public static Layer MidLayer18 = 19;
        public static Layer MidLayer19 = 20;
        public static Layer MidLayer20 = 21;
        public static Layer MidLayer21 = 22;
        public static Layer MidLayer22 = 23;
        public static Layer MidLayer23 = 24;
        public static Layer MidLayer24 = 25;
        public static Layer MidLayer25 = 26;
        public static Layer MidLayer26 = 27;
        public static Layer MidLayer27 = 28;
        public static Layer MidLayer28 = 29;
        public static Layer MidLayer29 = 30;
        public static Layer MidLayer30 = 31;
        public static Layer BottomLayer = 32;
        public static Layer TopOverlay = 33;
        public static Layer BottomOverlay = 34;
        public static Layer TopPaste = 35;
        public static Layer BottomPaste = 36;
        public static Layer TopSolder = 37;
        public static Layer BottomSolder = 38;
        public static Layer InternalPlane1 = 39;
        public static Layer InternalPlane2 = 40;
        public static Layer InternalPlane3 = 41;
        public static Layer InternalPlane4 = 42;
        public static Layer InternalPlane5 = 43;
        public static Layer InternalPlane6 = 44;
        public static Layer InternalPlane7 = 45;
        public static Layer InternalPlane8 = 46;
        public static Layer InternalPlane9 = 47;
        public static Layer InternalPlane10 = 48;
        public static Layer InternalPlane11 = 49;
        public static Layer InternalPlane12 = 50;
        public static Layer InternalPlane13 = 51;
        public static Layer InternalPlane14 = 52;
        public static Layer InternalPlane15 = 53;
        public static Layer InternalPlane16 = 54;
        public static Layer DrillGuide = 55;
        public static Layer KeepOutLayer = 56;
        public static Layer Mechanical1 = 57;
        public static Layer Mechanical2 = 58;
        public static Layer Mechanical3 = 59;
        public static Layer Mechanical4 = 60;
        public static Layer Mechanical5 = 61;
        public static Layer Mechanical6 = 62;
        public static Layer Mechanical7 = 63;
        public static Layer Mechanical8 = 64;
        public static Layer Mechanical9 = 65;
        public static Layer Mechanical10 = 66;
        public static Layer Mechanical11 = 67;
        public static Layer Mechanical12 = 68;
        public static Layer Mechanical13 = 69;
        public static Layer Mechanical14 = 70;
        public static Layer Mechanical15 = 71;
        public static Layer Mechanical16 = 72;
        public static Layer DrillDrawing = 73;
        public static Layer MultiLayer = 74;
        public static Layer ConnectLayer = 75;
        public static Layer BackGroundLayer = 76;
        public static Layer DRCErrorLayer = 77;
        public static Layer HighlightLayer = 78;
        public static Layer GridColor1 = 79;
        public static Layer GridColor10 = 80;
        public static Layer PadHoleLayer = 81;
        public static Layer ViaHoleLayer = 82;

        public static Layer TopPadMaster = 83;
        public static Layer BottomPadMaster = 84;
        public static Layer DRCDetailLayer = 85;
        public static Layer Unknown = byte.MaxValue;

        public static IEnumerable<string> Names => LayerMetadata.Names;
        public static IEnumerable<Layer> Values => LayerMetadata.Values;

        private readonly byte _value;
        public Layer(byte value) => _value = value;
        public byte ToByte() => _value;

        internal LayerMetadata Metadata => LayerMetadata.Get(_value);

        /// <summary>
        /// Gets the internal name of a PCB layer.
        /// </summary>
        public string Name => Metadata?.InternalName ?? nameof(Unknown);

        /// <summary>
        /// Gets the long name of a PCB layer.
        /// </summary>
        public string LongName => Metadata?.LongName ?? nameof(Unknown);

        /// <summary>
        /// Gets the medium name of a PCB layer.
        /// </summary>
        public string MediumName => Metadata?.MediumName ?? nameof(Unknown);

        /// <summary>
        /// Gets the short name of a PCB layer.
        /// </summary>
        public string ShortName => Metadata?.ShortName ?? nameof(Unknown);

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

        /// <summary>
        /// Creates a layer reference from an internal layer name.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Layer FromString(string layerName) => LayerMetadata.Get(layerName)?.Id ?? Unknown;

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
