using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OriginalCircuit.AltiumSharp.BasicTypes
{
    public static class Utils
    {
        public static readonly Encoding Win1252Encoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);

        public const double InternalUnits = 10000.0;

        public static double MilsToMMs(double mils) => mils * 0.0254;

        public static double MMsToMils(double mms) => mms / 0.0254;

        public static double CoordToMils(Coord coord) => (coord / InternalUnits);

        public static Coord MilsToCoord(double mils) => (int)(mils * InternalUnits);

        public static double CoordToMMs(Coord coord) => MilsToMMs(CoordToMils(coord));

        public static Coord MMsToCoord(double mms) => MilsToCoord(MMsToMils(mms));

        public static double CoordToCMs(Coord coord) => CoordToMMs(coord) * 0.1;

        public static Coord CMsToCoord(double cms) => MMsToCoord(cms * 10.0);

        public static double CoordToInches(Coord coord) => CoordToMils(coord) * 0.001;

        internal static Coord InchesToCoord(double inches) => MilsToCoord(inches * 1000.0);

        public static double CoordToDxp(Coord coord) =>
            CoordToMils(coord) / 10.0;

        public static Coord DxpToCoord(double value) =>
            MilsToCoord(value * 10.0);

        public static double DxpFracToMils(int num, int frac) =>
            num * 10.0 + frac / 10000.0;

        public static (int num, int frac) MilsToDxpFrac(double mils) =>
            ((int)mils / 10, (int)Math.Round((mils / 10.0 - Math.Truncate(mils / 10.0)) * 100000));

        public static (int num, int frac) CoordToDxpFrac(Coord coord) =>
            MilsToDxpFrac(CoordToMils(coord));

        public static Coord DxpFracToCoord(int num, int frac) =>
            MilsToCoord(DxpFracToMils(num, frac));

        public static Coord MetersToCoord(double meters) => MMsToCoord(meters * 1000.0);

        public static double CoordToMeters(Coord coord) => CoordToMMs(coord) * 0.001;

        public static Coord StringToCoordUnit(string input, out Unit unit) =>
            Unit.StringToCoordUnit(input, out unit);

        public static bool TryStringToCoordUnit(string input, out Coord result, out Unit unit) =>
            Unit.TryStringToCoordUnit(input, out result, out unit);

        public static string CoordUnitToString(Coord coord, Unit unit) =>
            CoordUnitToString(coord, unit, 1);

        public static string CoordUnitToString(Coord coord, Unit unit, Coord grid) =>
            Unit.CoordUnitToString(coord, unit, grid);

        public static string LayerToString(Layer layer) => LayerMetadata.GetName(layer);

        public static Layer StringToLayer(string layer) => LayerMetadata.Get(layer ?? "").Id;

        public static string UnitToString(Unit unit) => unit.ToString();

        internal static double StringToDouble(string str) =>
            double.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);

        /// <summary>
        /// Makes sure angle is between [0, 360]
        /// </summary>
        public static double NormalizeAngle(double degrees) =>
            (degrees > 0 && degrees % 360.0 == 0) ? 360.0 : (degrees % 360.0 + 360.0) % 360.0;

        internal static bool TryStringToDouble(string str, out double value) =>
            double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value);

        internal static string Format(string format, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, format, args);

        internal static ref CoordPoint[] TranslatePoints(ref CoordPoint[] points, in CoordPoint value)
        {
            if (value == CoordPoint.Zero) return ref points;

            for (var i = 0; i < points.Length; ++i)
            {
                var p = points[i];
                points[i] = new CoordPoint(p.X + value.X, p.Y + value.Y);
            }

            return ref points;
        }

        internal static ref CoordPoint[] RotatePoints(ref CoordPoint[] points,
            in CoordPoint anchor, double angleDegrees)
        {
            var angleRadians = -angleDegrees * Math.PI / 180.0;
            var cosAngle = Math.Cos(angleRadians);
            var sinAngle = Math.Sin(angleRadians);
            for (var i = 0; i < points.Length; ++i)
            {
                var (x, y) = points[i];
                double localX = x - anchor.X;
                double localY = y - anchor.Y;
                var rotatedX = localX * cosAngle + localY * sinAngle;
                var rotatedY = localY * cosAngle - localX * sinAngle;
                points[i] = new CoordPoint(anchor.X + (int)rotatedX, anchor.Y + (int)rotatedY);
            }
            return ref points;
        }

        private static Random rng = new Random();

        public static string GenerateUniqueId()
        {
            var result = new char[8];
            for (int i = 0; i < 8; ++i)
            {
                result[i] = (char)rng.Next('A', 'Z');
            }
            return new string(result);
        }

        private static Regex _designatorParser = new Regex(@"^(?<Prefix>.*?)(?<Number>\d*)\s*$");

        /// <summary>
        /// Generates a new designator by taking the last designator in order and then incrementing any ending integer.
        /// </summary>
        /// <remarks>
        /// This mimicks the behavior of AD's Schematic Editor context menu "Place > Pin", which works very differently
        /// from AD's Properties pin list "Add" button, and the context menu behavior was chosen as it seemed more intuitive.
        /// </remarks>
        public static string GenerateDesignator(IEnumerable<string> existingDesignators)
        {
            var largestDesignator = existingDesignators
                .Select(s => _designatorParser.Match(s ?? ""))
                .Select(m => (m.Groups["Prefix"]?.Value ?? "", int.TryParse(m.Groups["Number"]?.Value ?? "", out int n) ? n : (int?)null))
                .OrderBy(pn => pn)
                .LastOrDefault();
            if (largestDesignator.Item2 != null)
            {
                return $"{largestDesignator.Item1}{largestDesignator.Item2 + 1}";
            }
            else if (largestDesignator.Item1 != null)
            {
                return largestDesignator.Item1;
            }
            else
            {
                return "1";
            }
        }
    }
}
