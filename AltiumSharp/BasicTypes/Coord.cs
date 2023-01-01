using System;

namespace OriginalCircuit.AltiumSharp.BasicTypes
{
    /// <summary>
    /// Internal coordinate value, stored as a fixed point integer.
    /// <seealso cref="Utils.InternalUnits"/>
    /// </summary>
    public readonly struct Coord : IEquatable<Coord>, IComparable<Coord>
    {
        /// <summary>
        /// Length of 1 inch as a coordinate value.
        /// </summary>
        public static readonly Coord OneInch = Utils.MilsToCoord(1000);

        private readonly int _value;
        private Coord(int value) => _value = value;

        public static Coord FromMils(double mils) => Utils.MilsToCoord(mils);
        public double ToMils() => Utils.CoordToMils(this);

        public static Coord FromMMs(double mms) => Utils.MMsToCoord(mms);
        public double ToMMs() => Utils.CoordToMMs(this);

        /// <summary>
        /// Creates a coordinate value from an integer.
        /// </summary>
        /// <param name="value">Value to be used as coordenate</param>
        /// <returns></returns>
        public static Coord FromInt32(int value) => new Coord(value);

        /// <summary>
        /// Gets the integer value of this coordinate.
        /// </summary>
        /// <returns></returns>
        public int ToInt32() => _value;

        /// <summary>
        /// Gets the string representation of this coordinate using the <see cref="Unit.Mil"/> unit.
        /// </summary>
        public override string ToString() => Utils.CoordUnitToString(_value, Unit.Mil);

        /// <summary>
        /// Gets the string representation of this coordinate using the given <paramref name="unit"/>.
        /// </summary>
        public string ToString(Unit unit) => Utils.CoordUnitToString(_value, unit);

        /// <summary>
        /// Gets the string representation of this coordinate using the given <paramref name="unit"/>,
        /// and snapping the coordinate to the <paramref name="grid"/> size .
        /// </summary>
        public string ToString(Unit unit, Coord grid) => Utils.CoordUnitToString(_value, unit, grid);

        /// <summary>
        /// Implicit conversion operator so we can use integers and coordinates transparently.
        /// </summary>
        /// <param name="value"></param>
        static public implicit operator Coord(int value) => FromInt32(value);

        /// <summary>
        /// Implicit conversion operator so we can use integers and coordinates transparently.
        /// </summary>
        static public implicit operator int(Coord coord) => coord._value;

        #region 'boilerplate'
        public override bool Equals(object obj) => obj is Coord other && Equals(other);
        public bool Equals(Coord other) => _value == other._value;
        public int CompareTo(Coord other) => _value < other._value ? -1 : _value > other._value ? 1 : 0;
        public override int GetHashCode() => _value.GetHashCode();
        public static bool operator ==(Coord left, Coord right) => left.Equals(right);
        public static bool operator !=(Coord left, Coord right) => !left.Equals(right);
        public static bool operator <(Coord left, Coord right) => left.CompareTo(right) < 0;
        public static bool operator <=(Coord left, Coord right) => left.CompareTo(right) <= 0;
        public static bool operator >(Coord left, Coord right) => left.CompareTo(right) > 0;
        public static bool operator >=(Coord left, Coord right) => left.CompareTo(right) >= 0;
        #endregion
    }
}
