using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace OriginalCircuit.AltiumSharp.BasicTypes
{
    /// <summary>
    /// Layer data type used for ease of handling PCB layer references.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    public readonly struct Layer : IEquatable<Layer>, IComparable<Layer>
    {
        public static readonly Layer NoLayer = 0;
        public static readonly Layer TopLayer = 1;
        public static readonly Layer MidLayer1 = 2;
        public static readonly Layer MidLayer2 = 3;
        public static readonly Layer MidLayer3 = 4;
        public static readonly Layer MidLayer4 = 5;
        public static readonly Layer MidLayer5 = 6;
        public static readonly Layer MidLayer6 = 7;
        public static readonly Layer MidLayer7 = 8;
        public static readonly Layer MidLayer8 = 9;
        public static readonly Layer MidLayer9 = 10;
        public static readonly Layer MidLayer10 = 11;
        public static readonly Layer MidLayer11 = 12;
        public static readonly Layer MidLayer12 = 13;
        public static readonly Layer MidLayer13 = 14;
        public static readonly Layer MidLayer14 = 15;
        public static readonly Layer MidLayer15 = 16;
        public static readonly Layer MidLayer16 = 17;
        public static readonly Layer MidLayer17 = 18;
        public static readonly Layer MidLayer18 = 19;
        public static readonly Layer MidLayer19 = 20;
        public static readonly Layer MidLayer20 = 21;
        public static readonly Layer MidLayer21 = 22;
        public static readonly Layer MidLayer22 = 23;
        public static readonly Layer MidLayer23 = 24;
        public static readonly Layer MidLayer24 = 25;
        public static readonly Layer MidLayer25 = 26;
        public static readonly Layer MidLayer26 = 27;
        public static readonly Layer MidLayer27 = 28;
        public static readonly Layer MidLayer28 = 29;
        public static readonly Layer MidLayer29 = 30;
        public static readonly Layer MidLayer30 = 31;
        public static readonly Layer BottomLayer = 32;
        public static readonly Layer TopOverlay = 33;
        public static readonly Layer BottomOverlay = 34;
        public static readonly Layer TopPaste = 35;
        public static readonly Layer BottomPaste = 36;
        public static readonly Layer TopSolder = 37;
        public static readonly Layer BottomSolder = 38;
        public static readonly Layer InternalPlane1 = 39;
        public static readonly Layer InternalPlane2 = 40;
        public static readonly Layer InternalPlane3 = 41;
        public static readonly Layer InternalPlane4 = 42;
        public static readonly Layer InternalPlane5 = 43;
        public static readonly Layer InternalPlane6 = 44;
        public static readonly Layer InternalPlane7 = 45;
        public static readonly Layer InternalPlane8 = 46;
        public static readonly Layer InternalPlane9 = 47;
        public static readonly Layer InternalPlane10 = 48;
        public static readonly Layer InternalPlane11 = 49;
        public static readonly Layer InternalPlane12 = 50;
        public static readonly Layer InternalPlane13 = 51;
        public static readonly Layer InternalPlane14 = 52;
        public static readonly Layer InternalPlane15 = 53;
        public static readonly Layer InternalPlane16 = 54;
        public static readonly Layer DrillGuide = 55;
        public static readonly Layer KeepOutLayer = 56;
        public static readonly Layer Mechanical1 = 57;
        public static readonly Layer Mechanical2 = 58;
        public static readonly Layer Mechanical3 = 59;
        public static readonly Layer Mechanical4 = 60;
        public static readonly Layer Mechanical5 = 61;
        public static readonly Layer Mechanical6 = 62;
        public static readonly Layer Mechanical7 = 63;
        public static readonly Layer Mechanical8 = 64;
        public static readonly Layer Mechanical9 = 65;
        public static readonly Layer Mechanical10 = 66;
        public static readonly Layer Mechanical11 = 67;
        public static readonly Layer Mechanical12 = 68;
        public static readonly Layer Mechanical13 = 69;
        public static readonly Layer Mechanical14 = 70;
        public static readonly Layer Mechanical15 = 71;
        public static readonly Layer Mechanical16 = 72;
        public static readonly Layer DrillDrawing = 73;
        public static readonly Layer MultiLayer = 74;
        public static readonly Layer ConnectLayer = 75;
        public static readonly Layer BackGroundLayer = 76;
        public static readonly Layer DRCErrorLayer = 77;
        public static readonly Layer HighlightLayer = 78;
        public static readonly Layer GridColor1 = 79;
        public static readonly Layer GridColor10 = 80;
        public static readonly Layer PadHoleLayer = 81;
        public static readonly Layer ViaHoleLayer = 82;

        public static readonly Layer TopPadMaster = 83;
        public static readonly Layer BottomPadMaster = 84;
        public static readonly Layer DRCDetailLayer = 85;
        public static readonly Layer Unknown = byte.MaxValue;

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
