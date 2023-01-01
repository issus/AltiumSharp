using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OriginalCircuit.AltiumSharp.BasicTypes
{
    /// <summary>
    /// Information about the supported coordinate types.
    /// </summary>
    internal class UnitMetadata
    {
        public UnitMetadata(Unit instance, string name, string suffix, string format, bool isMetric,
            Func<double, Coord> unitValueToCoord, Func<Coord, double> coordToUnitValue)
        {
            Instance = instance;
            Name = name;
            Suffix = suffix;
            Format = format;
            IsMetric = isMetric;
            UnitValueToCoord = unitValueToCoord;
            CoordToUnitValue = coordToUnitValue;
        }

        /// <summary>
        /// Instance of the current unit.
        /// </summary>
        public Unit Instance { get; }

        /// <summary>
        /// Display name of the unit.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Textual suffix of this unit.
        /// </summary>
        public string Suffix { get; }

        /// <summary>
        /// Format used for converting to text a desired unit value.
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// Is the unit metric or imperial derived.
        /// </summary>
        public bool IsMetric { get; }

        /// <summary>
        /// Function used for converting a value from this unit to coordinates.
        /// </summary>
        public Func<double, Coord> UnitValueToCoord { get; }

        /// <summary>
        /// Function used for converting a coordinate to a value of this unit.
        /// </summary>
        public Func<Coord, double> CoordToUnitValue { get; }
    }

    /// <summary>
    /// Unit structure used for storing unit types, and unifying conversion between different units.
    /// <para>
    /// This works similarly to a richer enumeration.
    /// </para>
    /// </summary>
    public struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Mil = new Unit(0);
        public static readonly Unit Millimeter = new Unit(1);
        public static readonly Unit Inch = new Unit(2);
        public static readonly Unit Centimeter = new Unit(3);
        public static readonly Unit DxpDefault = new Unit(4);
        public static readonly Unit Meter = new Unit(5);

        private static readonly UnitMetadata[] _metadata = new[] {
            new UnitMetadata(Mil, "Mils", "mil", "#####0.0##mil", false, Utils.MilsToCoord, Utils.CoordToMils),
            new UnitMetadata(Millimeter, "Millimeters", "mm", "#####0.0##mm", true, Utils.MMsToCoord, Utils.CoordToMMs),
            new UnitMetadata(Inch, "Inches", "in", "#####0.00#in", false, Utils.InchesToCoord, Utils.CoordToInches),
            new UnitMetadata(Centimeter, "Centimeters", "cm", "#####0.0##cm", true, Utils.CMsToCoord, Utils.CoordToCMs),
            new UnitMetadata(DxpDefault, "Dxp Defaults", "", "#####0.###", false, Utils.DxpToCoord, Utils.CoordToDxp),
            new UnitMetadata(Meter, "Meters", "m", "#####0.0##m", true, Utils.MetersToCoord, Utils.CoordToMeters),
        };

        private readonly int _value;

        private Unit(int value) => _value = value;
        public static Unit FromInt32(int value) => new Unit(value);
        public int ToInt32() => _value;

        public static explicit operator Unit(int value) => FromInt32(value);
        public static explicit operator int(Unit unit) => unit.ToInt32();

        /// <summary>
        /// Is the given value convertible to the unit with <paramref name="suffix"/>?
        /// </summary>
        /// <param name="input">Input text.</param>
        /// <param name="suffix">Suffix of the unit to be tested.</param>
        /// <returns></returns>
        private static bool TestIsUnitValue(string input, string suffix) =>
            Regex.IsMatch(input, $@"^\s*[+-]?\s*\d+\.?\d*\s*{suffix}\s*$");

        /// <summary>
        /// Attempts to convert a string to a coordinate, if successful returns
        /// the <paramref name="unit"/> used.
        /// </summary>
        /// <param name="input">String to be converted.</param>
        /// <param name="result">Resulting coordinate.</param>
        /// <param name="unit">Unit of the resulting coordinate.</param>
        /// <returns>
        /// Returns true if string to coordinate conversion was possible.
        /// </returns>
        public static bool TryStringToCoordUnit(string input, out Coord result, out Unit unit)
        {
            result = default;
            unit = default;

            input = input?.Trim() ?? "";
            foreach (var m in _metadata)
            {
                if (TestIsUnitValue(input, m.Suffix))
                {
                    if (Utils.TryStringToDouble(input.Substring(0, input.Length - m.Suffix.Length), out var value))
                    {
                        unit = m.Instance;
                        result = m.UnitValueToCoord(value);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Attempts to convert a string to a coordinate, if successful returns
        /// the <paramref name="unit"/> used.
        /// </summary>
        /// <param name="input">String to be converted.</param>
        /// <param name="result">Resulting coordinate.</param>
        /// <returns>
        /// Returns true if string to coordinate conversion was possible.
        /// </returns>
        public static bool TryStringToCoordUnit(string input, out Coord result)
        {
            result = default;

            input = input?.Trim() ?? "";
            foreach (var m in _metadata)
            {
                if (TestIsUnitValue(input, m.Suffix))
                {
                    if (Utils.TryStringToDouble(input.Substring(0, input.Length - m.Suffix.Length), out var value))
                    {
                        result = m.UnitValueToCoord(value);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Converts a string to a coordinate and it's unit.
        /// </summary>
        /// <param name="input">Text to be converted</param>
        /// <param name="unit">Unit of the resulting coordinate.</param>
        /// <returns>Returns the output coordinate.</returns>
        public static Coord StringToCoordUnit(string input, out Unit unit) =>
            TryStringToCoordUnit(input, out var result, out unit) ? result : throw new FormatException($"Invalid coordinate: {input}");

        /// <summary>
        /// Converts a string to a coordinate and it's unit.
        /// </summary>
        /// <param name="input">Text to be converted</param>
        /// <returns>Returns the output coordinate.</returns>
        public static Coord StringToCoordUnit(string input) =>
            TryStringToCoordUnit(input, out var result) ? result : throw new FormatException($"Invalid coordinate: {input}");

        /// <summary>
        /// Converts a coordinate and unit to its string representation.
        /// </summary>
        /// <param name="coord">Coordinate to be converted.</param>
        /// <param name="unit">Unit to be used in the string result.</param>
        /// <param name="grid">
        /// Grid spacing. If <paramref name="grid"/> &gt; 1 then snaps
        /// coordinate values to the nearest coordinate according to the grid spacing.</param>
        /// <returns></returns>
        public static string CoordUnitToString(Coord coord, Unit unit, Coord grid)
        {
            if (unit._value < 0 || unit._value >= _metadata.Length)
            {
                throw new ArgumentException("Unsupported unit", nameof(unit));
            }
            Coord gridSnappedCoord = (int)(Math.Round((double)coord / grid) * grid);
            var m = _metadata[unit._value];
            var value = m.CoordToUnitValue(gridSnappedCoord);
            return value.ToString(m.Format, CultureInfo.InvariantCulture);
        }

        #region 'boilerplate'
        public override bool Equals(object obj) => obj is Unit other && Equals(other);
        public bool Equals(Unit other) => _value == other._value;
        public override int GetHashCode() => _value.GetHashCode();
        public static bool operator ==(Unit left, Unit right) => left.Equals(right);
        public static bool operator !=(Unit left, Unit right) => !(left.Equals(right));
        #endregion
    }
}
