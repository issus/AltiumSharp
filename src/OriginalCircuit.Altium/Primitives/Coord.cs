using System.Globalization;
using System.Runtime.CompilerServices;

namespace OriginalCircuit.Altium.Primitives;

/// <summary>
/// Internal coordinate value, stored as a fixed point integer.
/// Altium uses 10,000 units per mil (1/1000 inch).
/// </summary>
public readonly struct Coord : IEquatable<Coord>, IComparable<Coord>, IFormattable
{
    /// <summary>
    /// Internal units per mil (1/1000 inch).
    /// </summary>
    public const int UnitsPerMil = 10000;

    /// <summary>
    /// Internal units per millimeter.
    /// </summary>
    public const double UnitsPerMm = UnitsPerMil * 1000.0 / 25.4;

    /// <summary>
    /// Internal units per inch.
    /// </summary>
    public const int UnitsPerInch = UnitsPerMil * 1000;

    /// <summary>
    /// Length of 1 inch as a coordinate value.
    /// </summary>
    public static readonly Coord OneInch = new(UnitsPerInch);

    /// <summary>
    /// Length of 1 mil (1/1000 inch) as a coordinate value.
    /// </summary>
    public static readonly Coord OneMil = new(UnitsPerMil);

    /// <summary>
    /// Length of 1 millimeter as a coordinate value.
    /// </summary>
    public static readonly Coord OneMm = FromMm(1.0);

    /// <summary>
    /// Zero coordinate.
    /// </summary>
    public static readonly Coord Zero = new(0);

    private readonly int _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Coord(int value) => _value = value;

    /// <summary>
    /// Creates a coordinate from a value in mils (1/1000 inch).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord FromMils(double mils) => new(checked((int)(mils * UnitsPerMil)));

    /// <summary>
    /// Gets the value in mils (1/1000 inch).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ToMils() => _value / (double)UnitsPerMil;

    /// <summary>
    /// Creates a coordinate from a value in millimeters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord FromMm(double mm) => new(checked((int)(mm * UnitsPerMm)));

    /// <summary>
    /// Gets the value in millimeters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ToMm() => _value / UnitsPerMm;

    /// <summary>
    /// Creates a coordinate from a value in inches.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord FromInches(double inches) => new(checked((int)(inches * UnitsPerInch)));

    /// <summary>
    /// Gets the value in inches.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ToInches() => _value / (double)UnitsPerInch;

    /// <summary>
    /// Creates a coordinate from the raw internal integer value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord FromRaw(int value) => new(value);

    /// <summary>
    /// Gets the raw internal integer value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ToRaw() => _value;

    /// <summary>
    /// Parses a coordinate from a span, handling unit suffixes (mil, mm, in).
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> span, out Coord result)
    {
        span = span.Trim();

        if (span.EndsWith("mil", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(span[..^3], NumberStyles.Float, CultureInfo.InvariantCulture, out var mils))
            {
                result = FromMils(mils);
                return true;
            }
        }
        else if (span.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(span[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var mm))
            {
                result = FromMm(mm);
                return true;
            }
        }
        else if (span.EndsWith("in", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(span[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var inches))
            {
                result = FromInches(inches);
                return true;
            }
        }
        else if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var mils))
        {
            // Default to mils if no suffix
            result = FromMils(mils);
            return true;
        }

        result = Zero;
        return false;
    }

    public static Coord Parse(ReadOnlySpan<char> span)
    {
        if (!TryParse(span, out var result))
            throw new FormatException($"Cannot parse '{span.ToString()}' as a coordinate");
        return result;
    }

    public override string ToString() => ToString("mil", CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        format ??= "mil";
        formatProvider ??= CultureInfo.InvariantCulture;

        return format.ToLowerInvariant() switch
        {
            "mil" or "mils" => $"{ToMils().ToString("0.#####", formatProvider)}mil",
            "mm" => $"{ToMm().ToString("0.#####", formatProvider)}mm",
            "in" or "inch" or "inches" => $"{ToInches().ToString("0.######", formatProvider)}in",
            "raw" => _value.ToString(formatProvider),
            _ => throw new FormatException($"Unknown format specifier: {format}")
        };
    }

    // Arithmetic operators
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord operator +(Coord a, Coord b) => new(a._value + b._value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord operator -(Coord a, Coord b) => new(a._value - b._value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord operator -(Coord a) => new(-a._value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord operator *(Coord a, double scalar) => new(checked((int)(a._value * scalar)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord operator *(double scalar, Coord a) => new(checked((int)(a._value * scalar)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord operator /(Coord a, double scalar) => new(checked((int)(a._value / scalar)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double operator /(Coord a, Coord b) => (double)a._value / b._value;

    // Comparison
    public bool Equals(Coord other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Coord other && Equals(other);
    public override int GetHashCode() => _value;
    public int CompareTo(Coord other) => _value.CompareTo(other._value);

    public static bool operator ==(Coord left, Coord right) => left._value == right._value;
    public static bool operator !=(Coord left, Coord right) => left._value != right._value;
    public static bool operator <(Coord left, Coord right) => left._value < right._value;
    public static bool operator <=(Coord left, Coord right) => left._value <= right._value;
    public static bool operator >(Coord left, Coord right) => left._value > right._value;
    public static bool operator >=(Coord left, Coord right) => left._value >= right._value;

    // Math helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord Abs(Coord value) => new(Math.Abs(value._value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord Min(Coord a, Coord b) => new(Math.Min(a._value, b._value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord Max(Coord a, Coord b) => new(Math.Max(a._value, b._value));
}
